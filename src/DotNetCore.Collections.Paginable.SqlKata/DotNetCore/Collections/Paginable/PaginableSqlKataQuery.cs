using System;
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

        // ReSharper disable once UnusedMember.Local
        private PaginableSqlKataQuery() { }

        internal PaginableSqlKataQuery(Query query, int pageSize, int realPageCount, int realMemberCount)
            : base(pageSize, realPageCount, realMemberCount)
        {
            _sqlKataQuery = query;
        }

        internal PaginableSqlKataQuery(Query query, int pageSize, int realPageCount, int realMemberCount, int limitedMembersCount)
            : base(pageSize, realPageCount, realMemberCount, limitedMembersCount)
        {
            _sqlKataQuery = query;
        }

        /// <inheritdoc />
        protected override Lazy<IPage<T>> GetSpecifiedPage(int currentPageNumber, int pageSize, int realMemberCount)
        {
            return new(() => new SqlKataPage<T>(_sqlKataQuery, currentPageNumber, pageSize, realMemberCount));
        }
    }
}