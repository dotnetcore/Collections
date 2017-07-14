using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable
{
    public static class SolidPageExtensions
    {
        public static PaginableEnumerable<T> ToPaginable<T>(this IEnumerable<T> enumerable, int? limitedMemberCount = null)
        {
            return PaginableCollections.Of(enumerable, limitedMemberCount);
        }

        public static PaginableEnumerable<T> ToPaginable<T>(this IEnumerable<T> enumerable, int pageSize, int? limitedMemberCount = null)
        {
            return PaginableCollections.Of(enumerable, pageSize, limitedMemberCount);
        }

        public static PaginableQueryable<T> ToPaginable<T>(this IQueryable<T> queryable, int? limitedMemberCount = null)
        {
            return PaginableCollections.Of(queryable, limitedMemberCount);
        }

        public static PaginableQueryable<T> ToPaginable<T>(this IQueryable<T> queryable, int pageSize, int? limitedMemberCount = null)
        {
            return PaginableCollections.Of(queryable, pageSize, limitedMemberCount);
        }

        public static IPage<T> GetPage<T>(this IQueryable<T> queryable, int pageNumber)
        {
            return GetPage(queryable, pageNumber, PaginableSettingsManager.Settings.DefaultPageSize);
        }

        public static IPage<T> GetPage<T>(this IQueryable<T> queryable, int pageNumber, int pageSize)
        {
            if (queryable == null)
            {
                throw new ArgumentNullException(nameof(queryable), $"{nameof(queryable)} can not be null.");
            }

            if (pageNumber < 0)
            {
                throw new IndexOutOfRangeException($"{nameof(pageNumber)} can not be less than zero");
            }

            if (pageSize < 0)
            {
                throw new IndexOutOfRangeException($"{nameof(pageSize)} can not be less than zero");
            }

            return new QueryablePage<T>(queryable, pageNumber, pageSize, queryable.Count());
        }

        public static IPage<T> GetPage<T>(this IEnumerable<T> enumerable, int pageNumber)
        {
            return GetPage(enumerable, pageNumber, PaginableSettingsManager.Settings.DefaultPageSize);
        }

        public static IPage<T> GetPage<T>(this IEnumerable<T> enumerable, int pageNumber, int pageSize)
        {
            if (enumerable == null)
            {
                throw new ArgumentNullException(nameof(enumerable), $"{nameof(enumerable)} can not be null.");
            }

            if (pageNumber < 0)
            {
                throw new IndexOutOfRangeException($"{nameof(pageNumber)} can not be less than zero");
            }

            if (pageSize < 0)
            {
                throw new IndexOutOfRangeException($"{nameof(pageSize)} can not be less than zero");
            }

            // ReSharper disable PossibleMultipleEnumeration
            return new EnumerablePage<T>(enumerable, pageNumber, pageSize, enumerable.ToList().Count());
        }
    }
}
