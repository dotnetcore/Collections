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
            var realMemberCount = PaginableCalc.GetRealMemberCount(limitedMemberCount, enumerable.Count());
            var realPageCount = PaginableCalc.GetRealPageCount(realMemberCount, size);

            return limitedMemberCount.IsValid() && limitedMemberCount.HasValue
                ? new PaginableEnumerable<T>(enumerable, size, realPageCount, realMemberCount, limitedMemberCount.Value)
                : new PaginableEnumerable<T>(enumerable, size, realPageCount, realMemberCount);
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