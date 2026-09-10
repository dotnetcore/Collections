using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Async keyset (seek) pagination for EF Core, executed end-to-end as provider-native
    /// async database calls (<c>ToListAsync(cancellationToken)</c>).
    /// <para>
    /// Each request is a single <c>WHERE key &gt; @last ORDER BY key LIMIT @size</c> query:
    /// no OFFSET scan, no COUNT(*) round trip - deep pages cost the same as shallow ones.
    /// </para>
    /// </summary>
    public static class KeysetPageAsyncExtensions
    {
        /// <summary>
        /// Gets the first keyset page asynchronously (no keyset anchor applied).
        /// </summary>
        /// <typeparam name="T">element type</typeparam>
        /// <typeparam name="TKey">ordering key type; must support the &gt; / &lt; operators</typeparam>
        /// <param name="source">source queryable</param>
        /// <param name="keySelector">ordering key selector</param>
        /// <param name="pageSize">page size</param>
        /// <param name="descending">ordering direction</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// var first = await query.GetFirstPageByKeysetAsync(x =&gt; x.Id, pageSize: 50, cancellationToken: cancellationToken);
        /// var lastId = first.LastMember.Id;
        /// </code>
        /// </example>
        public static async Task<KeysetPage<T>> GetFirstPageByKeysetAsync<T, TKey>(
            this IQueryable<T> source,
            Expression<Func<T, TKey>> keySelector,
            int pageSize,
            bool descending = false,
            CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));
            if (pageSize < 1)
                throw new IndexOutOfRangeException($"{nameof(pageSize)} can not be less than one");

            var ordered = descending
                ? source.OrderByDescending(keySelector).Take(pageSize + 1)
                : source.OrderBy(keySelector).Take(pageSize + 1);
            var candidates = await ordered.ToListAsync(cancellationToken).ConfigureAwait(false);

            return BuildResult(candidates, pageSize, isFirstPage: true);
        }

        /// <summary>
        /// Gets the keyset page that follows <paramref name="lastKey"/> asynchronously.
        /// </summary>
        /// <typeparam name="T">element type</typeparam>
        /// <typeparam name="TKey">ordering key type; must support the &gt; / &lt; operators</typeparam>
        /// <param name="source">source queryable</param>
        /// <param name="keySelector">ordering key selector</param>
        /// <param name="lastKey">ordering key of the last member of the previous page</param>
        /// <param name="pageSize">page size</param>
        /// <param name="descending">ordering direction</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// var next = await query.GetPageByKeysetAsync(x =&gt; x.Id, lastId, pageSize: 50, cancellationToken: cancellationToken);
        /// var hasMore = next.HasNext;
        /// </code>
        /// </example>
        public static async Task<KeysetPage<T>> GetPageByKeysetAsync<T, TKey>(
            this IQueryable<T> source,
            Expression<Func<T, TKey>> keySelector,
            TKey lastKey,
            int pageSize,
            bool descending = false,
            CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));
            if (pageSize < 1)
                throw new IndexOutOfRangeException($"{nameof(pageSize)} can not be less than one");

            var query = PaginableKeyset.BuildKeysetQuery(source, keySelector, lastKey, pageSize, descending);
            var candidates = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

            return BuildResult(candidates, pageSize, isFirstPage: false);
        }

        private static KeysetPage<T> BuildResult<T>(System.Collections.Generic.List<T> candidates, int pageSize, bool isFirstPage)
        {
            var hasNext = candidates.Count > pageSize;
            var members = hasNext ? candidates.Take(pageSize).ToList() : candidates;
            return new KeysetPage<T>(members, pageSize, hasNext, isFirstPage);
        }
    }
}
