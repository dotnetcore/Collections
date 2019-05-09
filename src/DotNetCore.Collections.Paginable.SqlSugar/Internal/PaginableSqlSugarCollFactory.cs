using System;
using System.Collections.Generic;
using System.Text;
using SqlSugar;

namespace DotNetCore.Collections.Paginable.Internal
{
    internal static class PaginableSqlSugarCollFactory
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
        /// Make SqlSugarQueryable source to SqlSugarPage collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="pageSize"></param>
        /// <param name="limitedMemberCount"></param>
        /// <returns></returns>
        public static PaginableSqlSugarQuery<T> CreatePageSet<T>(ISugarQueryable<T> query, int? pageSize = null, int? limitedMemberCount = null)
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
            var realMemberCount = GetRealMemberCountFunc()(limitedMemberCount)(SqlSugarHelper.Count(query));
            var realPageCount = GetRealPageCountFunc()(realMemberCount)(size);

            return limitedMemberCount.IsValid() && limitedMemberCount.HasValue
                ? new PaginableSqlSugarQuery<T>(query, size, realPageCount, realMemberCount, limitedMemberCount.Value)
                : new PaginableSqlSugarQuery<T>(query, size, realPageCount, realMemberCount);
        }
    }
}
