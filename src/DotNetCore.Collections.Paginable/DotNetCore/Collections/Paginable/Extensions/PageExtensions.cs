using System;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Extensions for page
    /// </summary>
    public static class PageExtensions
    {
        /// <summary>
        /// Is current page thr first page?
        /// </summary>
        /// <param name="page">Page</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="page"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// if (page.IsFirst())
        /// {
        ///     // first page of the source
        /// }
        /// </code>
        /// </example>
        public static bool IsFirst(this IPage page)
        {
            if (page is null)
                throw new ArgumentNullException(nameof(page), $"{nameof(page)} can not be null.");
            return !page.HasPrevious;
        }

        /// <summary>
        /// Is current page the last page?
        /// </summary>
        /// <param name="page">Page</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="page"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// if (page.IsLast())
        /// {
        ///     // last page of the source
        /// }
        /// </code>
        /// </example>
        public static bool IsLast(this IPage page)
        {
            if (page is null)
                throw new ArgumentNullException(nameof(page), $"{nameof(page)} can not be null.");
            return !page.HasNext;
        }

        /// <summary>
        /// The number of the first item of current page.
        /// </summary>
        /// <param name="page">Page</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="page"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// int first = page.FromMemberNumber();
        /// // equals (CurrentPageNumber - 1) * PageSize + 1
        /// </code>
        /// </example>
        public static int FromMemberNumber(this IPage page)
        {
            if (page is null)
                throw new ArgumentNullException(nameof(page), $"{nameof(page)} can not be null.");

            if (page.TotalMemberCount == 0)
                return 0;

            if (!page.HasPrevious)
                return 1;

            return (page.CurrentPageNumber - 1) * page.PageSize + 1;
        }

        /// <summary>
        /// The number of the last item of current page.
        /// </summary>
        /// <param name="page">Page</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="page"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// int last = page.ToMemberNumber();
        /// // equals FromMemberNumber() + CurrentPageSize - 1
        /// </code>
        /// </example>
        public static int ToMemberNumber(this IPage page)
        {
            if (page is null)
                throw new ArgumentNullException(nameof(page), $"{nameof(page)} can not be null.");

            if (page.TotalMemberCount == 0)
                return 0;

            if (!page.HasNext)
                return (page.CurrentPageNumber - 1) * page.PageSize + page.CurrentPageSize;

            return page.CurrentPageNumber * page.PageSize;
        }
    }
}