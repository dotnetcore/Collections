using System;
using NHibernate;

// ReSharper disable RedundantCast

namespace DotNetCore.Collections.Paginable.Internal
{
    internal static class PaginableNhCoreCollFactory
    {
        /// <summary>
        /// Make NHibernate QueryOver`1 source to NHibernatePage collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queryOver"></param>
        /// <param name="pageSize"></param>
        /// <param name="limitedMemberCount"></param>
        /// <returns></returns>
        public static PaginableNhCoreQuery<T> CreatePageSet<T>(IQueryOver<T> queryOver, int? pageSize = null, int? limitedMemberCount = null)
        {
            if (queryOver is null)
                throw new ArgumentNullException(nameof(queryOver));

            pageSize ??= PaginableSettingsManager.Settings.DefaultPageSize;

            var size = pageSize.Value;
            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than one");
            var realMemberCount = PaginableCalc.GetRealMemberCount(limitedMemberCount, NhQueryOverHelper.Count(queryOver));
            var realPageCount = PaginableCalc.GetRealPageCount(realMemberCount, size);

            return limitedMemberCount.IsValid() && limitedMemberCount.HasValue
                ? new PaginableNhCoreQuery<T>(queryOver, size, realPageCount, realMemberCount, limitedMemberCount.Value)
                : new PaginableNhCoreQuery<T>(queryOver, size, realPageCount, realMemberCount);
        }
    }
}