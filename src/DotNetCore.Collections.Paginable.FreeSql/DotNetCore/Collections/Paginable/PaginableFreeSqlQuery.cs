using System;
using FreeSql;

namespace DotNetCore.Collections.Paginable {
    /// <summary>
    /// FreeSqlPage collection
    /// </summary>
    public class PaginableFreeSqlQuery<T> : PaginableSetBase<T> where T : class {
        private readonly ISelect<T> _freeSqlQuery;
        private readonly bool _includeNestedMembers;

        // ReSharper disable once UnusedMember.Local
        private PaginableFreeSqlQuery() { }

        internal PaginableFreeSqlQuery(ISelect<T> select, int pageSize, int realPageCount, int realMemberCount, bool includeNestedMembers)
            : base(pageSize, realPageCount, realMemberCount) {
            _freeSqlQuery = select;
            _includeNestedMembers = includeNestedMembers;
        }

        internal PaginableFreeSqlQuery(ISelect<T> select, int pageSize, int realPageCount, int realMemberCount, int limitedMembersCount, bool includeNestedMembers)
            : base(pageSize, realPageCount, realMemberCount, limitedMembersCount) {
            _freeSqlQuery = select;
            _includeNestedMembers = includeNestedMembers;
        }

        /// <inheritdoc />
        protected override Lazy<IPage<T>> GetSpecifiedPage(int currentPageNumber, int pageSize, int realMemberCount) {
            return new Lazy<IPage<T>>(() => new FreeSqlPage<T>(_freeSqlQuery, currentPageNumber, pageSize, realMemberCount, _includeNestedMembers));
        }
    }
}