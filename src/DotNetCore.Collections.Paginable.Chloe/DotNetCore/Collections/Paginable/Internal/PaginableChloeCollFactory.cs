using System;
using System.Collections.Generic;
using System.Text;
using Chloe;

namespace DotNetCore.Collections.Paginable.Internal
{
    internal static class PaginableChloeCollFactory
    {
        /// <summary>
        /// Make Chloe.Query`1 source to ChloePage collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="pageSize"></param>
        /// <param name="limitedMemberCount"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="query"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="pageSize"/> is out of its allowed range.</exception>
        public static PaginableChloeQuery<T> CreatePageSet<T>(IQuery<T> query, int? pageSize = null, int? limitedMemberCount = null)
        {
            if (query is null)
                throw new ArgumentNullException(nameof(query));

            pageSize ??= PaginableSettingsManager.Settings.DefaultPageSize;

            var size = pageSize.Value;
            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than one");
            var realMemberCount = PaginableCalc.GetRealMemberCount(limitedMemberCount, ChloeHelper.Count(query));
            var realPageCount = PaginableCalc.GetRealPageCount(realMemberCount, size);

            return limitedMemberCount.IsValid() && limitedMemberCount.HasValue
                ? new PaginableChloeQuery<T>(query, size, realPageCount, realMemberCount, limitedMemberCount.Value)
                : new PaginableChloeQuery<T>(query, size, realPageCount, realMemberCount);
        }
    }
}