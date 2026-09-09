using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FreeSql;

namespace DotNetCore.Collections.Paginable.Internal
{
    internal static class PaginableFreeSqlCollFactory
    {
        /// <summary>
        /// Make FreeSql.Select`1 source to FreeSqlPage collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="select"></param>
        /// <param name="pageSize"></param>
        /// <param name="limitedMemberCount"></param>
        /// <param name="includeNestedMembers"></param>
        /// <returns></returns>
        public static PaginableFreeSqlQuery<T> CreatePageSet<T>(ISelect<T> select, int? pageSize = null, int? limitedMemberCount = null, bool? includeNestedMembers = null)
            where T : class
        {
            if (select is null)
                throw new ArgumentNullException(nameof(select));

            pageSize ??= PaginableSettingsManager.Settings.DefaultPageSize;
            includeNestedMembers ??= false;

            var size = pageSize.Value;
            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than one");
            var realMemberCount = PaginableCalc.GetRealMemberCount(limitedMemberCount, FreeSqlHelper.Count(select).AsInt32());
            var realPageCount = PaginableCalc.GetRealPageCount(realMemberCount, size);

            return limitedMemberCount.IsValid() && limitedMemberCount.HasValue
                ? new PaginableFreeSqlQuery<T>(select, size, realPageCount, realMemberCount, limitedMemberCount.Value, includeNestedMembers.Value)
                : new PaginableFreeSqlQuery<T>(select, size, realPageCount, realMemberCount, includeNestedMembers.Value);
        }

        /// <summary>
        /// Make FreeSql.Select`1 source to FreeSqlPage collection asynchronously,
        /// with the total member count obtained via provider-native <c>CountAsync</c>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="select"></param>
        /// <param name="pageSize"></param>
        /// <param name="limitedMemberCount"></param>
        /// <param name="includeNestedMembers"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<PaginableFreeSqlQuery<T>> CreatePageSetAsync<T>(ISelect<T> select, int? pageSize = null, int? limitedMemberCount = null, bool? includeNestedMembers = null, CancellationToken cancellationToken = default)
            where T : class
        {
            if (select is null)
                throw new ArgumentNullException(nameof(select));

            pageSize ??= PaginableSettingsManager.Settings.DefaultPageSize;
            includeNestedMembers ??= false;

            var size = pageSize.Value;
            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than one");
            var realMemberCount = PaginableCalc.GetRealMemberCount(limitedMemberCount, (await FreeSqlHelper.CountAsync(select, cancellationToken)).AsInt32());
            var realPageCount = PaginableCalc.GetRealPageCount(realMemberCount, size);

            return limitedMemberCount.IsValid() && limitedMemberCount.HasValue
                ? new PaginableFreeSqlQuery<T>(select, size, realPageCount, realMemberCount, limitedMemberCount.Value, includeNestedMembers.Value)
                : new PaginableFreeSqlQuery<T>(select, size, realPageCount, realMemberCount, includeNestedMembers.Value);
        }
    }
}