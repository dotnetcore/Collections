using System;
using System.Diagnostics.CodeAnalysis;
using SqlKata;

namespace DotNetCore.Collections.Paginable.Internal
{
    internal static class PaginableSqlKataCollFactory
    {
        /// <summary>
        /// Make SqlKata.Query source to SqlKataPage collection.
        /// </summary>
        /// <typeparam name="T">element type of your enumerable result</typeparam>
        /// <param name="query">SqlKata.Query</param>
        /// <param name="pageSize">page size</param>
        /// <param name="limitedMemberCount">limited member count</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="query"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="pageSize"/> is out of its allowed range.</exception>
        public static PaginableSqlKataQuery<T> CreatePageSet<T>(Query query, int? pageSize = null, int? limitedMemberCount = null)
        {
            if (query is null)
                throw new ArgumentNullException(nameof(query));

            pageSize ??= PaginableSettingsManager.Settings.DefaultPageSize;

            var size = pageSize.Value;
            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than one");
            var realMemberCount = PaginableCalc.GetRealMemberCount(limitedMemberCount, SqlKataHelper.Count(query));
            var realPageCount = PaginableCalc.GetRealPageCount(realMemberCount, size);

            return limitedMemberCount.IsValid() && limitedMemberCount.HasValue
                ? new PaginableSqlKataQuery<T>(query, size, realPageCount, realMemberCount, limitedMemberCount.Value)
                : new PaginableSqlKataQuery<T>(query, size, realPageCount, realMemberCount);
        }
    }
}