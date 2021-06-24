using System;
using System.Linq;

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// QueryablePage collection
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PaginableQueryable<T> : PaginableSetBase<T>
    {
        private readonly IQueryable<T> _queryable;

        // ReSharper disable once UnusedMember.Local
        private PaginableQueryable() { }

        internal PaginableQueryable(IQueryable<T> queryable, int pageSize, int realPageCount, int realMemberCount)
            : base(pageSize, realPageCount, realMemberCount)
        {
            _queryable = queryable;
        }

        internal PaginableQueryable(IQueryable<T> queryable, int pageSize, int realPageCount, int realMemberCount, int limitedMembersCount)
            : base(pageSize, realPageCount, realMemberCount, limitedMembersCount)
        {
            _queryable = queryable;
        }

        /// <summary>
        /// Get special page
        /// </summary>
        /// <param name="currentPageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="realMemberCount"></param>
        /// <returns></returns>
        protected override Lazy<IPage<T>> GetSpecifiedPage(int currentPageNumber, int pageSize, int realMemberCount)
        {
            return new(() => new EnumerablePage<T>(_queryable, currentPageNumber, pageSize, realMemberCount));
        }
    }
}