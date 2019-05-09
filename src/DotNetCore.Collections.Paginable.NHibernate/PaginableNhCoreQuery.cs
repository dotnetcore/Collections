using System;
using NHibernate;

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// NHibernatePage collection
    /// </summary>
    public class PaginableNhCoreQuery<T> : PaginableSetBase<T>
    {
        private readonly IQueryOver<T> _nhibernateQueryOver;
        private PaginableNhCoreQuery() { }

        internal PaginableNhCoreQuery(IQueryOver<T> queryOver, int pageSize, int realPageCount, int realMemberCount)
            : base(pageSize, realPageCount, realMemberCount)
        {
            _nhibernateQueryOver = queryOver;
        }

        internal PaginableNhCoreQuery(IQueryOver<T> select, int pageSize, int realPageCount, int realMemberCount, int limitedMembersCount)
            : base(pageSize, realPageCount, realMemberCount, limitedMembersCount)
        {
            _nhibernateQueryOver = select;
        }

        protected override Lazy<IPage<T>> GetSpecialPage(int currentPageNumber, int pageSize, int realMemberCount)
        {
            return new Lazy<IPage<T>>(() => new NhCorePage<T>(_nhibernateQueryOver, currentPageNumber, pageSize, realMemberCount));
        }
    }
}
