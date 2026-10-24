#if !NET451
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.Collections.Paginable.Internal;
using FreeSql;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// FreeSql extensions that stream a query as an asynchronous sequence of pages with a real,
    /// provider-native fetch per page.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These are the second tier of the streaming face. The first tier - <c>ToPagesAsync</c> over
    /// an <see cref="IPaginable{T}"/> or a plain <see cref="IEnumerable{T}"/> - is an asynchronous
    /// <i>shape</i> over a synchronous fetch. These overloads take a FreeSql query that can be
    /// executed asynchronously, so the member count and every page are read with
    /// <c>CountAsync</c> / <c>ToListAsync</c> and the cancellation token travels into the provider
    /// rather than stopping between pages.
    /// </para>
    /// <para>
    /// Same name, more specific receiver, same parameter shapes as the core overloads: a call site
    /// upgrades to the asynchronous implementation by referencing this package, without changing a
    /// character. The page-size parameter is typed <see cref="int"/> on purpose - a nullable one
    /// would make <c>select.ToPagesAsync(50)</c> ambiguous between the better receiver and the
    /// better argument conversion.
    /// </para>
    /// <para>
    /// The stream is compiled out on <c>net451</c>, where <see cref="IAsyncEnumerable{T}"/> has no
    /// reachable definition at all; the synchronous face is unaffected there.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var query = freeSql.Select&lt;ExampleModel&gt;().Where(x =&gt; x.Id &gt; 0).OrderBy(x =&gt; x.Id);
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
        /// Stream every page of a FreeSql query asynchronously, using the configured default page
        /// size and without nested members.
        /// </summary>
        /// <typeparam name="T">element type of your FreeSql.Select`1 source</typeparam>
        /// <param name="select">original FreeSql.Select`1 source</param>
        /// <param name="cancellationToken">observed before the member count and before each page</param>
        /// <returns>a lazy, cancellable sequence of pages fetched asynchronously</returns>
        /// <exception cref="ArgumentNullException"><paramref name="select"/> is <c>null</c>.</exception>
        public static IAsyncEnumerable<IPage<T>> ToPagesAsync<T>(this ISelect<T> select, CancellationToken cancellationToken = default) where T : class
            => ToPagesAsync(select, PaginableSettingsManager.Settings.DefaultPageSize, includeNestedMembers: false, cancellationToken);

        /// <summary>
        /// Stream every page of a FreeSql query asynchronously, without nested members.
        /// </summary>
        /// <typeparam name="T">element type of your FreeSql.Select`1 source</typeparam>
        /// <param name="select">original FreeSql.Select`1 source</param>
        /// <param name="pageSize">page size (must be greater than or equal to one)</param>
        /// <param name="cancellationToken">observed before the member count and before each page</param>
        /// <returns>a lazy, cancellable sequence of pages fetched asynchronously</returns>
        /// <exception cref="ArgumentNullException"><paramref name="select"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="pageSize"/> is less than one.</exception>
        public static IAsyncEnumerable<IPage<T>> ToPagesAsync<T>(this ISelect<T> select, int pageSize, CancellationToken cancellationToken = default) where T : class
            => ToPagesAsync(select, pageSize, includeNestedMembers: false, cancellationToken);

        /// <summary>
        /// Stream every page of a FreeSql query asynchronously.
        /// </summary>
        /// <typeparam name="T">element type of your FreeSql.Select`1 source</typeparam>
        /// <param name="select">original FreeSql.Select`1 source</param>
        /// <param name="pageSize">page size (must be greater than or equal to one)</param>
        /// <param name="includeNestedMembers">include nested members</param>
        /// <param name="cancellationToken">observed before the member count and before each page</param>
        /// <returns>a lazy, cancellable sequence of pages fetched asynchronously</returns>
        /// <exception cref="ArgumentNullException"><paramref name="select"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="pageSize"/> is less than one.</exception>
        /// <example>
        /// <code>
        /// await foreach (var page in query.ToPagesAsync(50, includeNestedMembers: true, cancellationToken))
        /// {
        ///     Console.WriteLine(page.CurrentPageNumber);
        /// }
        /// </code>
        /// </example>
        public static IAsyncEnumerable<IPage<T>> ToPagesAsync<T>(this ISelect<T> select, int pageSize, bool includeNestedMembers, CancellationToken cancellationToken = default) where T : class
        {
            if (select is null)
                throw new ArgumentNullException(nameof(select), $"{nameof(select)} can not be null.");

            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than one");

            return Stream(select, pageSize, includeNestedMembers, knownMemberCount: null, cancellationToken);
        }

        /// <summary>
        /// Stream every page of an existing FreeSql backed page set asynchronously, reusing its page
        /// size, its nested-member setting and its already known member count.
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
        public static IAsyncEnumerable<IPage<T>> ToPagesAsync<T>(this PaginableFreeSqlQuery<T> paginable, CancellationToken cancellationToken = default) where T : class
        {
            if (paginable is null)
                throw new ArgumentNullException(nameof(paginable), $"{nameof(paginable)} can not be null.");

            return Stream(paginable.Queryable, paginable.PageSize, paginable.IncludeNestedMembers, paginable.MemberCount, cancellationToken);
        }

        // The cancellation check sits BEFORE each provider call: an already cancelled token must
        // stop the stream without running a single query.
        private static async IAsyncEnumerable<IPage<T>> Stream<T>(
            ISelect<T> select,
            int pageSize,
            bool includeNestedMembers,
            int? knownMemberCount,
            [EnumeratorCancellation] CancellationToken cancellationToken) where T : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            var totalMemberCount = knownMemberCount
                                   ?? (await FreeSqlHelper.CountAsync(select, cancellationToken).ConfigureAwait(false)).AsInt32();

            EnsureMemberCountAllowed(totalMemberCount);

            var realPageCount = PaginableCalc.GetRealPageCount(totalMemberCount, pageSize);

            for (var pageNumber = 1; pageNumber <= realPageCount; pageNumber++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var members = await select
                    .Page(pageNumber, pageSize)
                    .ToListAsync(includeNestedMembers, cancellationToken)
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
