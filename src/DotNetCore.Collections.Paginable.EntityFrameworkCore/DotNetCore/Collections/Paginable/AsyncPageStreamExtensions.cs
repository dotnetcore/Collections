using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.Collections.Paginable.Internal;
using Microsoft.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// EntityFrameworkCore extensions that stream a query as an asynchronous sequence of pages with
    /// a real, provider-native fetch per page.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These are the second tier of the streaming face. The first tier - <c>ToPagesAsync</c> over
    /// an <see cref="IPaginable{T}"/> or a plain <see cref="IEnumerable{T}"/> - is an asynchronous
    /// <i>shape</i> over a synchronous fetch: the page set offers only a synchronous
    /// <see cref="IPaginable{T}.GetPage"/>, so a page is materialised on the calling thread. These
    /// overloads take an <see cref="IQueryable{T}"/> that can be executed asynchronously, so the
    /// member count and every page are read with provider-native asynchronous calls and the
    /// cancellation token travels into the provider rather than stopping between pages.
    /// </para>
    /// <para>
    /// Same name, more specific receiver, same parameter shapes: this is the mechanism the package
    /// already uses for <c>GetPageAsync</c> (core over <see cref="IEnumerable{T}"/>, EF Core over
    /// <see cref="IQueryable{T}"/>), so a call site upgrades to the asynchronous implementation by
    /// referencing this package, without changing a character. The page-size parameter is typed
    /// <see cref="int"/> on purpose - matching the core overload exactly keeps overload resolution
    /// decided by the receiver alone. A nullable page size would make
    /// <c>query.ToPagesAsync(50)</c> ambiguous, which is the trap the synchronous builder already
    /// documents.
    /// </para>
    /// <para>
    /// One round trip for the member count, then one per page. Nothing is prefetched: breaking out
    /// of the loop stops the walk where it stands, so a consumer that reads two of ten pages issues
    /// three queries, not eleven. Pages are built from the materialised members of each page and
    /// are not the cached instances <see cref="PaginableSetBase{T}.GetPage"/> would return, because
    /// the page set's cache can only be filled by its own synchronous fetch.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// await foreach (var page in context.Samples.OrderBy(x => x.Id).ToPagesAsync(50, cancellationToken))
    /// {
    ///     foreach (var member in page)
    ///     {
    ///         Console.WriteLine(member.Value);
    ///     }
    /// }
    /// </code>
    /// </example>
    public static class AsyncPageStreamExtensions
    {
        /// <summary>
        /// Stream every page of a queryable source asynchronously, using the configured default
        /// page size.
        /// </summary>
        /// <typeparam name="T">element type of your queryable source</typeparam>
        /// <param name="queryable">original queryable source</param>
        /// <param name="cancellationToken">observed before the member count and before each page</param>
        /// <returns>a lazy, cancellable sequence of pages fetched asynchronously</returns>
        /// <exception cref="ArgumentNullException"><paramref name="queryable"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// await foreach (var page in query.ToPagesAsync(cancellationToken))
        /// {
        ///     Console.WriteLine(page.CurrentPageNumber);
        /// }
        /// </code>
        /// </example>
        public static IAsyncEnumerable<IPage<T>> ToPagesAsync<T>(this IQueryable<T> queryable, CancellationToken cancellationToken = default)
            => ToPagesAsync(queryable, PaginableSettingsManager.Settings.DefaultPageSize, cancellationToken);

        /// <summary>
        /// Stream every page of a queryable source asynchronously.
        /// </summary>
        /// <typeparam name="T">element type of your queryable source</typeparam>
        /// <param name="queryable">original queryable source</param>
        /// <param name="pageSize">page size (must be greater than or equal to one)</param>
        /// <param name="cancellationToken">observed before the member count and before each page</param>
        /// <returns>a lazy, cancellable sequence of pages fetched asynchronously</returns>
        /// <exception cref="ArgumentNullException"><paramref name="queryable"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="pageSize"/> is less than one.</exception>
        /// <example>
        /// <code>
        /// await foreach (var page in query.ToPagesAsync(50, cancellationToken))
        /// {
        ///     Console.WriteLine(page.CurrentPageNumber);
        /// }
        /// </code>
        /// </example>
        public static IAsyncEnumerable<IPage<T>> ToPagesAsync<T>(this IQueryable<T> queryable, int pageSize, CancellationToken cancellationToken = default)
        {
            if (queryable is null)
                throw new ArgumentNullException(nameof(queryable), $"{nameof(queryable)} can not be null.");

            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than one");

            // Validation is synchronous, exactly like the GetPage family: the exceptions leave the
            // call itself, not a faulted task and not the first MoveNextAsync.
            return Stream(queryable, pageSize, knownMemberCount: null, cancellationToken);
        }

        /// <summary>
        /// Stream every page of an existing EF Core backed page set asynchronously, reusing its page
        /// size and its already known member count.
        /// </summary>
        /// <typeparam name="T">element type of your page members</typeparam>
        /// <param name="paginable">page set to stream</param>
        /// <param name="cancellationToken">observed before each page</param>
        /// <returns>a lazy, cancellable sequence of pages fetched asynchronously</returns>
        /// <remarks>
        /// The member count is not read again: the page set already counted the query when it was
        /// built, so the stream reuses that number and a page set of <c>PageCount</c> pages issues
        /// exactly <c>PageCount</c> queries. Pages are equal in content to the page set's own, with
        /// the same page size and member count, but they are not its cached instances.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="paginable"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// var paginable = await query.ToPaginableAsync(50, cancellationToken: cancellationToken);
        ///
        /// await foreach (var page in paginable.ToPagesAsync(cancellationToken))
        /// {
        ///     Console.WriteLine(page.CurrentPageNumber);   // one ToListAsync per page
        /// }
        /// </code>
        /// </example>
        public static IAsyncEnumerable<IPage<T>> ToPagesAsync<T>(this PaginableQueryable<T> paginable, CancellationToken cancellationToken = default)
        {
            if (paginable is null)
                throw new ArgumentNullException(nameof(paginable), $"{nameof(paginable)} can not be null.");

            return Stream(paginable.Queryable, paginable.PageSize, paginable.MemberCount, cancellationToken);
        }

        // The cancellation check sits BEFORE each provider call, not after: an already cancelled
        // token must stop the stream without running a single query, and cancelling between pages
        // must not fetch the next one. Written as an explicit loop so the check and the call cannot
        // be reordered by accident.
        private static async IAsyncEnumerable<IPage<T>> Stream<T>(
            IQueryable<T> queryable,
            int pageSize,
            int? knownMemberCount,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var totalMemberCount = knownMemberCount
                                   ?? await queryable.CountAsync(cancellationToken).ConfigureAwait(false);

            EnsureMemberCountAllowed(totalMemberCount);

            var realPageCount = PaginableCalc.GetRealPageCount(totalMemberCount, pageSize);

            for (var pageNumber = 1; pageNumber <= realPageCount; pageNumber++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var skip = (pageNumber - 1) * pageSize;
                var members = await queryable
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                yield return new EnumerablePage<T>(members, pageNumber, pageSize, totalMemberCount, sourceIsFull: false);
            }
        }

        private static void EnsureMemberCountAllowed(int totalMemberCount)
        {
            // The same ceiling a page set enforces when it is constructed: a streaming walk must
            // not become the way around it.
            if (totalMemberCount > PaginableSettingsManager.Settings.MaxMemberItems)
                throw new ArgumentOutOfRangeException(nameof(totalMemberCount), "Paginable does not support large size result");
        }
    }
}
