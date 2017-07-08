using System.Linq;
using DotNetCore.Collections.Paginable.Internal;

namespace DotNetCore.Collections.Paginable
{
    public static class PaginableCollections
    {
        public static IPage<T> OfPage<T>(IQueryable<T> queryable, int pageNumber)
        {
            return OfPage(queryable, pageNumber, PaginableConstants.DEFAULT_PAGE_SIZE);
        }

        public static IPage<T> OfPage<T>(IQueryable<T> queryable, int pageNumber, int pageSize)
        {
            return queryable.GetPage(pageNumber, pageSize);
        }
    }
}
