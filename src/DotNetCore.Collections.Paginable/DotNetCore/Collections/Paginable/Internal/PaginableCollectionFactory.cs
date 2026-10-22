using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable RedundantCast

namespace DotNetCore.Collections.Paginable.Internal
{
    internal static class PaginableCollectionFactory
    {
        /// <summary>
        /// Make enumerable result to EnumerablePage collection
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="enumerable">orgin enumerable result</param>
        /// <param name="pageSize">page size</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumerable"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="pageSize"/> is out of its allowed range.</exception>
        public static PaginableEnumerable<T> CreatePageSet<T>(IEnumerable<T> enumerable, int? pageSize = null, int? limitedMemberCount = null)
        {
            if (enumerable is null)
                throw new ArgumentNullException(nameof(enumerable));

            pageSize ??= PaginableSettingsManager.Settings.DefaultPageSize;

            var size = pageSize.Value;
            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than one");
            var source = PrepareSource(enumerable);
            var realMemberCount = PaginableCalc.GetRealMemberCount(limitedMemberCount, source.Count());
            var realPageCount = PaginableCalc.GetRealPageCount(realMemberCount, size);

            return limitedMemberCount.IsValid() && limitedMemberCount.HasValue
                ? new PaginableEnumerable<T>(source, size, realPageCount, realMemberCount, limitedMemberCount.Value)
                : new PaginableEnumerable<T>(source, size, realPageCount, realMemberCount);
        }

        /// <summary>
        /// Decides what sequence the paging set should hold, applying the
        /// <see cref="PaginableSettings.MemoizeLazySources"/> switch.
        /// </summary>
        /// <remarks>
        /// Without memoization a lazy source is walked once for the member count and then once more
        /// per page, each time from the beginning, so a full pass over P pages costs
        /// <c>O(P^2 * pageSize)</c> pulls. The engine's <c>Memoize</c> makes all of those walks
        /// share one cache, bringing it down to <c>O(memberCount)</c>.
        /// <para>
        /// The memoized sequence is returned here rather than wrapped later so that the count pass
        /// below is the one that fills the cache: the pages then replay it and never touch the
        /// source again.
        /// </para>
        /// </remarks>
        private static IEnumerable<T> PrepareSource<T>(IEnumerable<T> enumerable)
        {
            if (!PaginableSettingsManager.Settings.MemoizeLazySources)
                return enumerable;

            // A materialised collection already re-enumerates for free and counts in O(1), so a
            // cache would only add an allocation. An IQueryable must keep its provider
            // translation: memoizing it would evaluate the whole result set on the client and
            // throw away the server-side Skip/Take.
            if (enumerable is ICollection<T> || enumerable is IQueryable<T>)
                return enumerable;

            return enumerable.ToValueEnumerable().Memoize();
        }

        /// <summary>
        /// Make queryable source to QueryablePage collection
        /// </summary>
        /// <typeparam name="T">element type of your queryable source</typeparam>
        /// <param name="queryable">orgin queryable result</param>
        /// <param name="pageSize">page size</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="queryable"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="pageSize"/> is out of its allowed range.</exception>
        public static PaginableQueryable<T> CreatePageSet<T>(IQueryable<T> queryable, int? pageSize = null, int? limitedMemberCount = null)
        {
            if (queryable is null)
                throw new ArgumentNullException(nameof(queryable));

            pageSize ??= PaginableSettingsManager.Settings.DefaultPageSize;

            var size = pageSize.Value;
            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than one");
            var realMemberCount = PaginableCalc.GetRealMemberCount(limitedMemberCount, queryable.Count());
            var realPageCount = PaginableCalc.GetRealPageCount(realMemberCount, size);

            return limitedMemberCount.IsValid() && limitedMemberCount.HasValue
                ? new PaginableQueryable<T>(queryable, size, realPageCount, realMemberCount, limitedMemberCount.Value)
                : new PaginableQueryable<T>(queryable, size, realPageCount, realMemberCount);
        }
    }
}