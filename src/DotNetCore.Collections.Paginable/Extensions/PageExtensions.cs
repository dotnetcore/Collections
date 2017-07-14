using System;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable
{
    public static class PageExtensions
    {
        public static bool IsFirst(this IPage page)
        {
            if (page == null)
            {
                throw new ArgumentNullException(nameof(page), $"{nameof(page)} can not be null.");
            }

            return !page.HasPrevious;
        }

        public static bool IsLast(this IPage page)
        {
            if (page == null)
            {
                throw new ArgumentNullException(nameof(page), $"{nameof(page)} can not be null.");
            }

            return !page.HasNext;
        }

        public static int FirstMemberNumber(this IPage page)
        {
            if (page == null)
            {
                throw new ArgumentNullException(nameof(page), $"{nameof(page)} can not be null.");
            }

            return (page.CurrentPageNumber - 1) * page.PageSize + 1;
        }

        public static int LastMemberNumber(this IPage page)
        {
            if (page == null)
            {
                throw new ArgumentNullException(nameof(page), $"{nameof(page)} can not be null.");
            }

            return (page.CurrentPageNumber - 1) * page.PageSize + page.CurrentPageSize;
        }

        public static int FromMemberNumber(this IPage page)
        {
            if (page == null)
            {
                throw new ArgumentNullException(nameof(page), $"{nameof(page)} can not be null.");
            }

            if (page.TotalMemberCount == 0)
            {
                return 0;
            }

            if (!page.HasPrevious)
            {
                return 1;
            }

            return (page.CurrentPageNumber - 1) * page.PageSize + 1;
        }

        public static int ToMemberNumber(this IPage page)
        {
            if (page == null)
            {
                throw new ArgumentNullException(nameof(page), $"{nameof(page)} can not be null.");
            }

            if (page.TotalMemberCount == 0)
            {
                return 0;
            }

            if (!page.HasNext)
            {
                return (page.CurrentPageNumber - 1) * page.PageSize + page.CurrentPageSize;
            }

            return page.CurrentPageNumber * page.PageSize;
        }

        public static PaginableQueryable<T> AsQueryable<T>(this PaginableEnumerable<T> paginable)
        {
            return new PaginableQueryable<T>(paginable.ExportEnumerable().AsQueryable(), paginable.PageSize, paginable.PageCount, paginable.MemberCount);
        }

        public static QueryablePage<T> AsQueryable<T>(this EnumerablePage<T> page)
        {
            return new QueryablePage<T>(page.ExportEnumerable(), page.CurrentPageNumber, page.PageSize, page.TotalMemberCount);
        }

        public static PaginableEnumerable<T> AsEnumerable<T>(this PaginableQueryable<T> paginable)
        {
            return new PaginableEnumerable<T>(paginable.ExportQueryable().AsEnumerable(), paginable.PageSize, paginable.PageCount, paginable.MemberCount);
        }

        public static EnumerablePage<T> AsEnumerable<T>(this QueryablePage<T> page)
        {
            return new EnumerablePage<T>(page.ExportQueryable().AsEnumerable(), page.CurrentPageNumber, page.PageSize, page.TotalMemberCount);
        }
    }
}
