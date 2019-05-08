using System;
using SqlKata;

namespace DotNetCore.Collections.Paginable.Internal
{
    internal static class PaginableSqlKataCollFactory
    {
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
        /// first parameter(m) means real member count, which has been getten from <see cref="GetRealMemberCountFunc"/><br />,
        /// second parameter(s) means page size.
        /// </summary>
        /// <returns></returns>
        private static Func<int, Func<int, int>> GetRealPageCountFunc()
            => m => s => (int)Math.Ceiling((double)m / (double)s);

        /// <summary>
        /// Make SqlKata.Query source to SqlKataPage collection.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="query">SqlKata.Query</param>
        /// <param name="pageSize">page size</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <returns></returns>
        public static PaginableSqlKataQuery<T> CreatePageSet<T>(Query query, int? pageSize = null, int? limitedMemberCount = null)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (pageSize == null)
            {
                pageSize = PaginableSettingsManager.Settings.DefaultPageSize;
            }

            var size = pageSize.Value;
            var realMemberCount = GetRealMemberCountFunc()(limitedMemberCount)(SqlKataHelper.Count(query));
            var realPageCount = GetRealPageCountFunc()(realMemberCount)(size);

            return limitedMemberCount.IsValid() && limitedMemberCount.HasValue
                ? new PaginableSqlKataQuery<T>(query, size, realPageCount, realMemberCount, limitedMemberCount.Value)
                : new PaginableSqlKataQuery<T>(query, size, realPageCount, realMemberCount);
        }
    }
}
