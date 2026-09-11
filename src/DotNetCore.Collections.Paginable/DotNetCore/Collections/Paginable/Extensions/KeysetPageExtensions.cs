using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Public query builder for keyset (seek) pagination, shared by the core library and
    /// provider integrations (e.g. EF Core async keyset).
    /// </summary>
    public static class PaginableKeyset
    {
        /// <summary>
        /// Composes a keyset continuation query: filter by the ordering key, order, and
        /// take <c>pageSize + 1</c> members (the extra member only signals <c>HasNext</c>
        /// and must be trimmed by the caller).
        /// </summary>
        /// <typeparam name="T">element type</typeparam>
        /// <typeparam name="TKey">ordering key type; must support the &gt; / &lt; operators
        /// (int, long, DateTime, decimal, DateTimeOffset, ...) so that the comparison can
        /// be expressed as an expression tree and translated by LINQ providers</typeparam>
        /// <param name="source">source queryable</param>
        /// <param name="keySelector">ordering key selector (must be stable and unique-ish:
        /// a monotonic id or (created, id) composite is typical)</param>
        /// <param name="lastKey">keyset anchor: the ordering key of the last member of the
        /// previous page</param>
        /// <param name="pageSize">requested page size</param>
        /// <param name="descending">ordering direction</param>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// var ordered = PaginableKeyset.BuildKeysetQuery(source, x =&gt; x.Id, lastId, pageSize: 50);
        /// var page = ordered.ToList();
        /// </code>
        /// </example>
        public static IQueryable<T> BuildKeysetQuery<T, TKey>(
            IQueryable<T> source,
            Expression<Func<T, TKey>> keySelector,
            TKey lastKey,
            int pageSize,
            bool descending)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));

            var boundary = descending
                ? Expression.LessThan(keySelector.Body, Expression.Constant(lastKey, typeof(TKey)))
                : Expression.GreaterThan(keySelector.Body, Expression.Constant(lastKey, typeof(TKey)));
            var predicate = Expression.Lambda<Func<T, bool>>(boundary, keySelector.Parameters);

            var filtered = source.Where(predicate);
            return descending
                ? filtered.OrderByDescending(keySelector).Take(pageSize + 1)
                : filtered.OrderBy(keySelector).Take(pageSize + 1);
        }
    }

    /// <summary>
    /// Keyset (seek) pagination extensions.
    /// <para>
    /// Offset-based pagination degrades linearly with page number (<c>OFFSET 100000</c>
    /// forces the provider to scan and discard 100000 rows). Keyset pagination avoids
    /// both the <c>OFFSET</c> scan and the <c>COUNT(*)</c> round trip: each request is a
    /// single <c>WHERE key &gt; @last ORDER BY key LIMIT @size</c> query. Use it for
    /// infinite-scroll / cursor APIs; use <see cref="SolidPageExtensions"/> when you need
    /// total page counts.
    /// </para>
    /// </summary>
    public static class KeysetPageExtensions
    {
        /// <summary>
        /// Gets the first keyset page from a queryable source (no keyset anchor applied).
        /// </summary>
        /// <typeparam name="T">element type</typeparam>
        /// <typeparam name="TKey">ordering key type; must support the &gt; / &lt; operators</typeparam>
        /// <param name="source">source queryable</param>
        /// <param name="keySelector">ordering key selector</param>
        /// <param name="pageSize">page size</param>
        /// <param name="descending">ordering direction</param>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="pageSize"/> is less than one.
        /// </exception>
        /// <example>
        /// <code>
        /// var first = query.GetFirstPageByKeyset(x =&gt; x.Id, pageSize: 50);
        /// var lastId = first.LastMember.Id;
        /// </code>
        /// </example>
        public static KeysetPage<T> GetFirstPageByKeyset<T, TKey>(
            this IQueryable<T> source,
            Expression<Func<T, TKey>> keySelector,
            int pageSize,
            bool descending = false)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));
            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than one");

            var ordered = descending
                ? source.OrderByDescending(keySelector).Take(pageSize + 1)
                : source.OrderBy(keySelector).Take(pageSize + 1);
            var candidates = ordered.ToList();

            return BuildResult(candidates, pageSize, isFirstPage: true);
        }

        /// <summary>
        /// Gets the keyset page that follows <paramref name="lastKey"/> from a queryable source.
        /// </summary>
        /// <typeparam name="T">element type</typeparam>
        /// <typeparam name="TKey">ordering key type; must support the &gt; / &lt; operators</typeparam>
        /// <param name="source">source queryable</param>
        /// <param name="keySelector">ordering key selector</param>
        /// <param name="lastKey">ordering key of the last member of the previous page</param>
        /// <param name="pageSize">page size</param>
        /// <param name="descending">ordering direction</param>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="pageSize"/> is less than one.
        /// </exception>
        /// <example>
        /// <code>
        /// var next = query.GetPageByKeyset(x =&gt; x.Id, lastId, pageSize: 50);
        /// var hasMore = next.HasNext;
        /// </code>
        /// </example>
        public static KeysetPage<T> GetPageByKeyset<T, TKey>(
            this IQueryable<T> source,
            Expression<Func<T, TKey>> keySelector,
            TKey lastKey,
            int pageSize,
            bool descending = false)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));
            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than one");

            var query = PaginableKeyset.BuildKeysetQuery(source, keySelector, lastKey, pageSize, descending);
            var candidates = query.ToList();

            return BuildResult(candidates, pageSize, isFirstPage: false);
        }

        /// <summary>
        /// Gets the first keyset page from an in-memory source (no keyset anchor applied).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="pageSize"/> is less than one.
        /// </exception>
        /// <example>
        /// <code>
        /// var first = list.GetFirstPageByKeyset(x =&gt; x.Id, pageSize: 50);
        /// var lastId = first.LastMember.Id;
        /// </code>
        /// </example>
        public static KeysetPage<T> GetFirstPageByKeyset<T, TKey>(
            this IEnumerable<T> source,
            Func<T, TKey> keySelector,
            int pageSize,
            bool descending = false)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));
            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than one");

            var ordered = descending
                ? source.OrderByDescending(keySelector).Take(pageSize + 1)
                : source.OrderBy(keySelector).Take(pageSize + 1);
            var candidates = ordered.ToList();

            return BuildResult(candidates, pageSize, isFirstPage: true);
        }

        /// <summary>
        /// Gets the keyset page that follows <paramref name="lastKey"/> from an in-memory source.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="pageSize"/> is less than one.
        /// </exception>
        /// <example>
        /// <code>
        /// var next = list.GetPageByKeyset(x =&gt; x.Id, lastId, pageSize: 50);
        /// var hasMore = next.HasNext;
        /// </code>
        /// </example>
        public static KeysetPage<T> GetPageByKeyset<T, TKey>(
            this IEnumerable<T> source,
            Func<T, TKey> keySelector,
            TKey lastKey,
            int pageSize,
            bool descending = false)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));
            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than one");

            var comparer = Comparer<TKey>.Default;
            var filtered = descending
                ? source.Where(x => comparer.Compare(keySelector(x), lastKey) < 0)
                : source.Where(x => comparer.Compare(keySelector(x), lastKey) > 0);
            var ordered = descending
                ? filtered.OrderByDescending(keySelector).Take(pageSize + 1)
                : filtered.OrderBy(keySelector).Take(pageSize + 1);
            var candidates = ordered.ToList();

            return BuildResult(candidates, pageSize, isFirstPage: false);
        }

        private static KeysetPage<T> BuildResult<T>(List<T> candidates, int pageSize, bool isFirstPage)
        {
            var hasNext = candidates.Count > pageSize;
            var members = hasNext ? candidates.Take(pageSize).ToList() : candidates;
            return new KeysetPage<T>(members, pageSize, hasNext, isFirstPage);
        }
    }
}
