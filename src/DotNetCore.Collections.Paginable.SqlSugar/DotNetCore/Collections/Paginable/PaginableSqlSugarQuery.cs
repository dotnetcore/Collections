using System;
using SqlSugar;

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// SqlSugarPage collection
    /// </summary>
    public class PaginableSqlSugarQuery<T> : PaginableSetBase<T>
    {
        private readonly ISugarQueryable<T> _sqlSugarQuery;

        internal PaginableSqlSugarQuery(ISugarQueryable<T> select, int pageSize, int realPageCount, int realMemberCount)
            : base(pageSize, realPageCount, realMemberCount)
        {
            _sqlSugarQuery = select;
        }

        internal PaginableSqlSugarQuery(ISugarQueryable<T> select, int pageSize, int realPageCount, int realMemberCount, int limitedMembersCount)
            : base(pageSize, realPageCount, realMemberCount, limitedMembersCount)
        {
            _sqlSugarQuery = select;
        }

        /// <summary>
        /// Gets the SqlSugar query this page set pages over.
        /// </summary>
        /// <remarks>
        /// Read-only, and it hands back the very instance the page set was built from. It is
        /// exposed so the asynchronous streaming entry can fetch each page through SqlSugar's own
        /// asynchronous API over the same query, which is what lets the cancellation token reach
        /// the provider instead of stopping at the page boundary.
        /// </remarks>
        /// <example>
        /// <code>
        /// var paginable = await query.ToPaginableAsync(50);
        /// var sameQuery = paginable.Queryable;
        /// </code>
        /// </example>
        public ISugarQueryable<T> Queryable => _sqlSugarQuery;

        /// <inheritdoc />
        protected override Lazy<IPage<T>> GetSpecifiedPage(int currentPageNumber, int pageSize, int realMemberCount)
        {
            return new(() => new SqlSugarPage<T>(_sqlSugarQuery, currentPageNumber, pageSize, realMemberCount));
        }
    }
}