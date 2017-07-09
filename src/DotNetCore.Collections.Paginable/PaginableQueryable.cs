using System.Linq;

namespace DotNetCore.Collections.Paginable
{
    public class PaginableQueryable<T> : PaginableSetBase<T>
    {
        private PaginableQueryable() { }

        internal PaginableQueryable(IQueryable<T> queryable, int pageSize, int realPageCount, int realMemberCount)
            : base(queryable, pageSize, realMemberCount, realPageCount)
        {

        }

        internal PaginableQueryable(IQueryable<T> queryable, int pageSize, int realPageCount, int realMemberCount, int limitedMemberCount)
            : base(queryable, pageSize, realPageCount, realMemberCount, limitedMemberCount)
        {

        }
    }
}
