using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable.Internal;

namespace DotNetCore.Collections.Paginable
{
    public static class PaginableCollections
    {
        public static PaginableEnumerable<T> Of<T>(IEnumerable<T> enumerable, int? pageSize = null, int? limitedMemberCount = null)
        {
            if (enumerable == null)
            {
                throw new ArgumentNullException(nameof(enumerable));
            }

            if (pageSize == null)
            {
                pageSize = PaginableSettingsManager.Settings.DefaultPageSize;
            }

            var size = pageSize.Value;
            var list = enumerable.ToList();
            var count = list.Count();

            var realMemberCount = limitedMemberCount != null && limitedMemberCount.HasValue

                ? limitedMemberCount.Value > count
                    ? count
                    : limitedMemberCount.Value
                : count;

            var realPageCount = (int)Math.Ceiling((double)realMemberCount / (double)size);

            return limitedMemberCount != null && limitedMemberCount.HasValue

                ? new PaginableEnumerable<T>(list, size, realPageCount, realMemberCount, limitedMemberCount.Value)
                : new PaginableEnumerable<T>(list, size, realPageCount, realMemberCount);
        }

        public static PaginableQueryable<T> Of<T>(IQueryable<T> queryable, int? pageSize = null, int? limitedMemberCount = null)
        {
            if (queryable == null)
            {
                throw new ArgumentNullException(nameof(queryable));
            }

            if (pageSize == null)
            {
                pageSize = PaginableSettingsManager.Settings.DefaultPageSize;
            }

            var size = pageSize.Value;
            var count = queryable.Count();

            var realMemberCount = limitedMemberCount != null && limitedMemberCount.HasValue

                ? limitedMemberCount.Value > count
                    ? count
                    : limitedMemberCount.Value
                : count;

            var realPageCount = (int)Math.Ceiling((double)realMemberCount / (double)size);

            return limitedMemberCount != null && limitedMemberCount.HasValue

                ? new PaginableQueryable<T>(queryable, size, realPageCount, realMemberCount, limitedMemberCount.Value)
                : new PaginableQueryable<T>(queryable, size, realPageCount, realMemberCount);
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
