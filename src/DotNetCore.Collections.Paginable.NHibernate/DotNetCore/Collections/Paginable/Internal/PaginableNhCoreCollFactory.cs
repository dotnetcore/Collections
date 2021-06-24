using System;
using NHibernate;

// ReSharper disable RedundantCast

namespace DotNetCore.Collections.Paginable.Internal
{
    internal static class PaginableNhCoreCollFactory
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
        /// first parameter(m) means real member count, which has been gotten from <see cref="GetRealMemberCountFunc"/><br />,
        /// second parameter(s) means page size.
        /// </summary>
        /// <returns></returns>
        private static Func<int, Func<int, int>> GetRealPageCountFunc()
            => m => s => (int) Math.Ceiling((double) m / (double) s);

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
            var realMemberCount = GetRealMemberCountFunc()(limitedMemberCount)(NhQueryOverHelper.Count(queryOver));
            var realPageCount = GetRealPageCountFunc()(realMemberCount)(size);

            return limitedMemberCount.IsValid() && limitedMemberCount.HasValue
                ? new PaginableNhCoreQuery<T>(queryOver, size, realPageCount, realMemberCount, limitedMemberCount.Value)
                : new PaginableNhCoreQuery<T>(queryOver, size, realPageCount, realMemberCount);
        }
    }
}