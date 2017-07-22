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
    }
}
