using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable.Internal;

namespace DotNetCore.Collections.Paginable
{
    public static class PaginableCollections
    {
        public static PaginableEnumerable<T> Of<T>(IEnumerable<T> enumerable, int? pageSize = null, int? limitedMembersCount = null)
        {
            if (pageSize == null)
            {
                pageSize = PaginableSettingsManager.Settings.DefaultPageSize;
            }

            var size = pageSize.Value;
            var list = enumerable.ToList();
            var listCount = list.Count();

            var realMemberCount = limitedMembersCount != null && limitedMembersCount.HasValue
                ? limitedMembersCount.Value > listCount
                    ? listCount
                    : limitedMembersCount.Value
                : listCount;

            var realPageCount = (int)Math.Ceiling((double)realMemberCount / (double)size);

            return limitedMembersCount != null && limitedMembersCount.HasValue
                ? new PaginableEnumerable<T>(list, size, realPageCount, realMemberCount, limitedMembersCount.Value)
                : new PaginableEnumerable<T>(list, size, realPageCount, realMemberCount);
        }

        public static IPage<T> OfPage<T>(IQueryable<T> queryable, int pageNumber)
        {
            return OfPage(queryable, pageNumber, PaginableSettingsManager.Settings.DefaultPageSize);
        }

        public static IPage<T> OfPage<T>(IQueryable<T> queryable, int pageNumber, int pageSize)
        {
            return queryable.GetPage(pageNumber, pageSize);
        }
    }
}
