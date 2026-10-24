using System;
using FreeSql;

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// FreeSqlPage collection
    /// </summary>
    public class PaginableFreeSqlQuery<T> : PaginableSetBase<T> where T : class
    {
        private readonly ISelect<T> _freeSqlQuery;
        private readonly bool _includeNestedMembers;

        internal PaginableFreeSqlQuery(ISelect<T> select, int pageSize, int realPageCount, int realMemberCount, bool includeNestedMembers)
            : base(pageSize, realPageCount, realMemberCount)
        {
            _freeSqlQuery = select;
            _includeNestedMembers = includeNestedMembers;
        }

        internal PaginableFreeSqlQuery(ISelect<T> select, int pageSize, int realPageCount, int realMemberCount, int limitedMembersCount, bool includeNestedMembers)
            : base(pageSize, realPageCount, realMemberCount, limitedMembersCount)
        {
            _freeSqlQuery = select;
            _includeNestedMembers = includeNestedMembers;
        }

        /// <summary>
        /// Gets the FreeSql query this page set pages over.
        /// </summary>
        /// <remarks>
        /// Read-only, and it hands back the very instance the page set was built from. It is
        /// exposed so the asynchronous streaming entry can fetch each page through FreeSql's own
        /// asynchronous API over the same query, which is what lets the cancellation token reach
        /// the provider instead of stopping at the page boundary.
        /// </remarks>
        /// <example>
        /// <code>
        /// var paginable = await freeSql.Select&lt;ExampleModel&gt;().ToPaginableAsync(50);
        /// var sameQuery = paginable.Queryable;
        /// </code>
        /// </example>
        public ISelect<T> Queryable => _freeSqlQuery;

        /// <summary>
        /// Gets whether this page set materialises nested members with each page member.
        /// </summary>
        /// <remarks>
        /// Exposed alongside <see cref="Queryable"/> so an asynchronous fetch over the same query
        /// reproduces the pages this page set produces: the flag changes what a page member holds,
        /// so a streaming path that guessed it would silently return different page contents.
        /// </remarks>
        /// <example>
        /// <code>
        /// var paginable = await freeSql.Select&lt;ExampleModel&gt;().ToPaginableAsync(50, includeNestedMembers: true);
        /// var nested = paginable.IncludeNestedMembers;   // true
        /// </code>
        /// </example>
        public bool IncludeNestedMembers => _includeNestedMembers;

        /// <inheritdoc />
        protected override Lazy<IPage<T>> GetSpecifiedPage(int currentPageNumber, int pageSize, int realMemberCount)
        {
            return new(() => new FreeSqlPage<T>(_freeSqlQuery, currentPageNumber, pageSize, realMemberCount, _includeNestedMembers));
        }
    }
}