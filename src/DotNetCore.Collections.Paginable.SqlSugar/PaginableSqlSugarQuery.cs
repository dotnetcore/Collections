using System;
using SqlSugar;

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// SqlSugarPage collection
    /// </summary>
    public class PaginableSqlSugarQuery<T> : PaginableSetBase<T>
    {
        private readonly ISugarQueryable<T> _freeSqlQuery;
        private PaginableSqlSugarQuery() { }

        internal PaginableSqlSugarQuery(ISugarQueryable<T> select, int pageSize, int realPageCount, int realMemberCount)
            : base(pageSize, realPageCount, realMemberCount)
        {
            _freeSqlQuery = select;
        }

        internal PaginableSqlSugarQuery(ISugarQueryable<T> select, int pageSize, int realPageCount, int realMemberCount, int limitedMembersCount)
            : base(pageSize, realPageCount, realMemberCount, limitedMembersCount)
        {
            _freeSqlQuery = select;
        }

        protected override Lazy<IPage<T>> GetSpecialPage(int currentPageNumber, int pageSize, int realMemberCount)
        {
            return new Lazy<IPage<T>>(() => new SqlSugarPage<T>(_freeSqlQuery, currentPageNumber, pageSize, realMemberCount));
        }
    }
}
