#if !NET451
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.Collections.Paginable.Internal;
using SqlSugar;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// SqlSugar extensions that stream a query as an asynchronous sequence of pages with a real,
    /// provider-native fetch per page.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These are the second tier of the streaming face. The first tier - <c>ToPagesAsync</c> over
    /// an <see cref="IPaginable{T}"/> or a plain <see cref="IEnumerable{T}"/> - is an asynchronous
    /// <i>shape</i> over a synchronous fetch. These overloads take a SqlSugar query that can be
    /// executed asynchronously, so the member count and every page are read with <c>CountAsync</c>
    /// / <c>ToPageListAsync</c>: the fetch itself no longer blocks the calling thread. One
    /// limitation is the provider's, not this package's: SqlSugar 5.1.3 exposes no
    /// <see cref="CancellationToken"/> overload on either call, so the token is honoured at the
    /// boundary this library owns - before each provider call - rather than inside the provider.
    /// FreeSql and EF Core do forward it all the way down.
    /// </para>
    /// <para>
    /// Same name, more specific receiver, same parameter shapes as the core overloads: a call site
    /// upgrades to the asynchronous implementation by referencing this package, without changing a
    /// character. The page-size parameter is typed <see cref="int"/> on purpose - a nullable one
    /// would make <c>query.ToPagesAsync(50)</c> ambiguous between the better receiver and the
    /// better argument conversion.
    /// </para>
    /// <para>
    /// The stream is compiled out on <c>net451</c>, where <see cref="IAsyncEnumerable{T}"/> has no
    /// reachable definition at all; the synchronous face is unaffected there.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var query = db.Queryable&lt;ExampleModel&gt;().Where(x =&gt; x.Id &gt; 0).OrderBy(x =&gt; x.Id);
    ///
    /// await foreach (var page in query.ToPagesAsync(50, cancellationToken))
    /// {
    ///     Console.WriteLine(page.CurrentPageNumber);
    /// }
    /// </code>
    /// </example>
    public static class AsyncPageStreamExtensions
    {
        /// <summary>
        /// Stream every page of a SqlSugar query asynchronously, using the configured default page
        /// size.
        /// </summary>
        /// <typeparam name="T">element type of your SqlSugar query</typeparam>
        /// <param name="query">original SqlSugar query</param>
        /// <param name="cancellationToken">observed before the member count and before each page</param>
        /// <returns>a lazy, cancellable sequence of pages fetched asynchronously</returns>
        /// <exception cref="ArgumentNullException"><paramref name="query"/> is <c>null</c>.</exception>
        public static IAsyncEnumerable<IPage<T>> ToPagesAsync<T>(this ISugarQueryable<T> query, CancellationToken cancellationToken = default)
            => ToPagesAsync(query, PaginableSettingsManager.Settings.DefaultPageSize, cancellationToken);

        /// <summary>
        /// Stream every page of a SqlSugar query asynchronously.
        /// </summary>
        /// <typeparam name="T">element type of your SqlSugar query</typeparam>
        /// <param name="query">original SqlSugar query</param>
        /// <param name="pageSize">page size (must be greater than or equal to one)</param>
        /// <param name="cancellationToken">observed before the member count and before each page</param>
        /// <returns>a lazy, cancellable sequence of pages fetched asynchronously</returns>
        /// <exception cref="ArgumentNullException"><paramref name="query"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="pageSize"/> is less than one.</exception>
        /// <example>
        /// <code>
        /// await foreach (var page in query.ToPagesAsync(50, cancellationToken))
        /// {
        ///     Console.WriteLine(page.CurrentPageNumber);
        /// }
        /// </code>
        /// </example>
        public static IAsyncEnumerable<IPage<T>> ToPagesAsync<T>(this ISugarQueryable<T> query, int pageSize, CancellationToken cancellationToken = default)
        {
            if (query is null)
                throw new ArgumentNullException(nameof(query), $"{nameof(query)} can not be null.");

            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than one");

            return Stream(query, pageSize, knownMemberCount: null, cancellationToken);
        }

        /// <summary>
        /// Stream every page of an existing SqlSugar backed page set asynchronously, reusing its page
        /// size and its already known member count.
        /// </summary>
        /// <typeparam name="T">element type of your page members</typeparam>
        /// <param name="paginable">page set to stream</param>
        /// <param name="cancellationToken">observed before each page</param>
        /// <returns>a lazy, cancellable sequence of pages fetched asynchronously</returns>
        /// <remarks>
        /// The member count is not read again: the page set already counted the query when it was
        /// built, so a page set of <c>PageCount</c> pages issues exactly <c>PageCount</c> queries.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="paginable"/> is <c>null</c>.</exception>
        public static IAsyncEnumerable<IPage<T>> ToPagesAsync<T>(this PaginableSqlSugarQuery<T> paginable, CancellationToken cancellationToken = default)
        {
            if (paginable is null)
                throw new ArgumentNullException(nameof(paginable), $"{nameof(paginable)} can not be null.");

            return Stream(paginable.Queryable, paginable.PageSize, paginable.MemberCount, cancellationToken);
        }

        // The cancellation check sits BEFORE each provider call: an already cancelled token must
        // stop the stream without running a single query.
        private static async IAsyncEnumerable<IPage<T>> Stream<T>(
            ISugarQueryable<T> query,
            int pageSize,
            int? knownMemberCount,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var totalMemberCount = knownMemberCount
                                   ?? await SqlSugarHelper.CountAsync(query, cancellationToken).ConfigureAwait(false);

            EnsureMemberCountAllowed(totalMemberCount);

            var realPageCount = PaginableCalc.GetRealPageCount(totalMemberCount, pageSize);

            for (var pageNumber = 1; pageNumber <= realPageCount; pageNumber++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var members = await SqlSugarHelper
                    .FetchPageAsync(query, pageNumber, pageSize, cancellationToken)
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
#endif
