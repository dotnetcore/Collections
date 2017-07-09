using System;
using System.Linq;

namespace DotNetCore.Collections.Paginable
{
    public class PaginableQueryable<T> : PaginableSetBase<T>
    {
        private readonly IQueryable<T> m_queryable;

        private PaginableQueryable() { }

        internal PaginableQueryable(IQueryable<T> queryable, int pageSize, int realPageCount, int realMemberCount)
            : base(pageSize, realMemberCount, realPageCount)
        {
            m_queryable = queryable;

            InitializeQueryablePagesCache(pageSize, realMemberCount, realPageCount);
        }

        internal PaginableQueryable(IQueryable<T> queryable, int pageSize, int realPageCount, int realMemberCount, int limitedMemberCount)
            : base(pageSize, realPageCount, realMemberCount, limitedMemberCount)
        {
            m_queryable = queryable;

            InitializeQueryablePagesCache(pageSize, realMemberCount, realPageCount);
        }

        private void InitializeQueryablePagesCache(int pageSize, int realMemberCount, int realPageCount)
        {
            for (var i = 0; i < realPageCount; i++)
            {
                var currentPageNumber = i + 1;
                m_lazyPinedPagesCache[i] = new Lazy<IPage<T>>(() =>
                    new QueryablePage<T>(m_queryable, currentPageNumber, pageSize, realMemberCount));
            }
        }
    }
}
