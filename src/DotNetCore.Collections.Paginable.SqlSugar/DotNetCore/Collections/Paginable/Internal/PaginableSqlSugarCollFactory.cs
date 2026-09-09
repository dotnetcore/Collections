using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using SqlSugar;

namespace DotNetCore.Collections.Paginable.Internal
{
    internal static class PaginableSqlSugarCollFactory
    {
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
            if (query is null)
                throw new ArgumentNullException(nameof(query));

            pageSize ??= PaginableSettingsManager.Settings.DefaultPageSize;

            var size = pageSize.Value;
            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than one");
            var realMemberCount = PaginableCalc.GetRealMemberCount(limitedMemberCount, SqlSugarHelper.Count(query));
            var realPageCount = PaginableCalc.GetRealPageCount(realMemberCount, size);

            return limitedMemberCount.IsValid() && limitedMemberCount.HasValue
                ? new PaginableSqlSugarQuery<T>(query, size, realPageCount, realMemberCount, limitedMemberCount.Value)
                : new PaginableSqlSugarQuery<T>(query, size, realPageCount, realMemberCount);
        }

        /// <summary>
        /// Make SqlSugarQueryable source to SqlSugarPage collection asynchronously,
        /// with the total member count obtained via provider-native <c>CountAsync</c>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="pageSize"></param>
        /// <param name="limitedMemberCount"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<PaginableSqlSugarQuery<T>> CreatePageSetAsync<T>(ISugarQueryable<T> query, int? pageSize = null, int? limitedMemberCount = null, CancellationToken cancellationToken = default)
        {
            if (query is null)
                throw new ArgumentNullException(nameof(query));

            pageSize ??= PaginableSettingsManager.Settings.DefaultPageSize;

            var size = pageSize.Value;
            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than one");
            var realMemberCount = PaginableCalc.GetRealMemberCount(limitedMemberCount, await SqlSugarHelper.CountAsync(query));
            var realPageCount = PaginableCalc.GetRealPageCount(realMemberCount, size);

            return limitedMemberCount.IsValid() && limitedMemberCount.HasValue
                ? new PaginableSqlSugarQuery<T>(query, size, realPageCount, realMemberCount, limitedMemberCount.Value)
                : new PaginableSqlSugarQuery<T>(query, size, realPageCount, realMemberCount);
        }
    }
}