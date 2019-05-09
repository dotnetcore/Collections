using System;
using System.Collections.Generic;
using System.Text;
using FreeSql;

namespace DotNetCore.Collections.Paginable.Internal
{
    internal static class PaginableFreeSqlCollFactory
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
        /// Make FreeSql.Select`1 source to FreeSqlPage collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="select"></param>
        /// <param name="pageSize"></param>
        /// <param name="limitedMemberCount"></param>
        /// <param name="includeNestedMembers"></param>
        /// <returns></returns>
        public static PaginableFreeSqlQuery<T> CreatePageSet<T>(ISelect<T> select, int? pageSize = null, int? limitedMemberCount = null, bool? includeNestedMembers = null) where T : class
        {
            if (select == null)
            {
                throw new ArgumentNullException(nameof(select));
            }

            if (pageSize == null)
            {
                pageSize = PaginableSettingsManager.Settings.DefaultPageSize;
            }

            if (includeNestedMembers == null)
            {
                includeNestedMembers = false;
            }

            var size = pageSize.Value;
            var realMemberCount = GetRealMemberCountFunc()(limitedMemberCount)(FreeSqlHelper.Count(select).AsInt32());
            var realPageCount = GetRealPageCountFunc()(realMemberCount)(size);

            return limitedMemberCount.IsValid() && limitedMemberCount.HasValue
                ? new PaginableFreeSqlQuery<T>(select, size, realPageCount, realMemberCount, limitedMemberCount.Value, includeNestedMembers.Value)
                : new PaginableFreeSqlQuery<T>(select, size, realPageCount, realMemberCount, includeNestedMembers.Value);
        }
    }
}
