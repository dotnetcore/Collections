using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetCore.Collections.Paginable.Internal {
    internal static class PaginableCollectionFactory {
        /// <summary>
        /// Get real member count<br />.
        /// first parameter(l) means limitedMemberCount<br />,
        /// second parameter(c) means count.
        /// </summary>
        /// <returns></returns>
        private static Func<int?, Func<int, int>> GetRealMemberCountFunc()
            => l => c => l.IsValid() && l.HasValue ? l.Value > c ? c : l.Value : c;

        /// <summary>
        /// Get real page count<br />.
        /// first parameter(m) means real member count, which has been gotten from <see cref="GetRealMemberCountFunc"/><br />,
        /// second parameter(s) means page size.
        /// </summary>
        /// <returns></returns>
        private static Func<int, Func<int, int>> GetRealPageCountFunc()
            => m => s => (int) Math.Ceiling((double) m / (double) s);

        /// <summary>
        /// Make enumerable result to EnumerablePage collection
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="enumerable">orgin enumerable result</param>
        /// <param name="pageSize">page size</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <returns></returns>
        public static PaginableEnumerable<T> CreatePageSet<T>(IEnumerable<T> enumerable, int? pageSize = null, int? limitedMemberCount = null) {
            if (enumerable == null) {
                throw new ArgumentNullException(nameof(enumerable));
            }

            if (pageSize == null) {
                pageSize = PaginableSettingsManager.Settings.DefaultPageSize;
            }

            var size = pageSize.Value;
            var realMemberCount = GetRealMemberCountFunc()(limitedMemberCount)(enumerable.Count());
            var realPageCount = GetRealPageCountFunc()(realMemberCount)(size);

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
        public static PaginableQueryable<T> CreatePageSet<T>(IQueryable<T> queryable, int? pageSize = null, int? limitedMemberCount = null) {
            if (queryable == null) {
                throw new ArgumentNullException(nameof(queryable));
            }

            if (pageSize == null) {
                pageSize = PaginableSettingsManager.Settings.DefaultPageSize;
            }

            var size = pageSize.Value;
            var realMemberCount = GetRealMemberCountFunc()(limitedMemberCount)(queryable.Count());
            var realPageCount = GetRealPageCountFunc()(realMemberCount)(size);

            return limitedMemberCount.IsValid() && limitedMemberCount.HasValue
                ? new PaginableQueryable<T>(queryable, size, realPageCount, realMemberCount, limitedMemberCount.Value)
                : new PaginableQueryable<T>(queryable, size, realPageCount, realMemberCount);
        }
    }
}