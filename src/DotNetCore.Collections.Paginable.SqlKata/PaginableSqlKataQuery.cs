using System;
using System.Collections.Generic;
using System.Text;
using SqlKata;

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// SqlKataPage collection
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PaginableSqlKataQuery<T> : PaginableSetBase<T>
    {
        private readonly Query _sqlKataQuery;
        private PaginableSqlKataQuery() { }

        internal PaginableSqlKataQuery(Query query, int pageSize, int realPageCount, int realMemberCount)
            : base(pageSize, realPageCount, realMemberCount)
        {
            _sqlKataQuery = query;
        }

        internal PaginableSqlKataQuery(Query query, int pageSize, int realPageCount, int realMemberCount,
            int limitedMemberCount)
        {
            _sqlKataQuery = query;
        }

        protected override Lazy<IPage<T>> GetSpecialPage(int currentPageNumber, int pageSize, int realMemberCount)
        {
            return new Lazy<IPage<T>>(() => new SqlKataPage<T>(_sqlKataQuery, currentPageNumber, pageSize, realMemberCount));
        }
    }
}
