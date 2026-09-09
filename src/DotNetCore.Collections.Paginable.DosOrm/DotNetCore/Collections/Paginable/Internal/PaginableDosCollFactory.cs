using System;
using System.Diagnostics.CodeAnalysis;
using Dos.ORM;

namespace DotNetCore.Collections.Paginable.Internal
{
    internal static class PaginableDosCollFactory
    {
        /// <summary>
        /// Make Dos.ORM Query`1 source to DosePage collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="pageSize"></param>
        /// <param name="limitedMemberCount"></param>
        /// <param name="additionalQueryFunc"></param>
        /// <returns></returns>
        public static PaginableDosQuery<T> CreatePageSet<T>(FromSection<T> query, int? pageSize = null, int? limitedMemberCount = null,
            Func<FromSection<T>, FromSection<T>> additionalQueryFunc = null) where T : Entity
        {
            if (query is null)
                throw new ArgumentNullException(nameof(query));

            pageSize ??= PaginableSettingsManager.Settings.DefaultPageSize;

            var size = pageSize.Value;
            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than one");
            var realMemberCount = PaginableCalc.GetRealMemberCount(limitedMemberCount, DosHelper.Count(query));
            var realPageCount = PaginableCalc.GetRealPageCount(realMemberCount, size);

            return limitedMemberCount.IsValid() && limitedMemberCount.HasValue
                ? new PaginableDosQuery<T>(query, size, realPageCount, realMemberCount, limitedMemberCount.Value, additionalQueryFunc)
                : new PaginableDosQuery<T>(query, size, realPageCount, realMemberCount, additionalQueryFunc);
        }
    }
}