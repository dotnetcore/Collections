using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable.Internal;

// ReSharper disable RedundantBaseQualifier

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Queryable page
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class QueryablePage<T> : PageBase<T>
    {
        /// <summary>
        /// Queryable page
        /// </summary>
        /// <param name="queryable"></param>
        /// <param name="currentPageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="totalMemberCount"></param>
        /// <param name="sourceIsFull"></param>
        public QueryablePage(IQueryable<T> queryable, int currentPageNumber, int pageSize, int totalMemberCount, bool sourceIsFull = true) : base(sourceIsFull)
        {
            var skip = (currentPageNumber - 1) * pageSize;
            var state = new QueryEntryState<T>(queryable, skip, pageSize);
            InitializeMetaInfo(currentPageNumber, pageSize, totalMemberCount, skip);
            base._initializeAction = InitializeMemberList()(state)(CurrentPageSize)(skip);
        }

        /// <summary>
        /// Queryable page
        /// </summary>
        /// <param name="enumerable"></param>
        /// <param name="currentPageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="totalMembersCount"></param>
        public QueryablePage(IEnumerable<T> enumerable, int currentPageNumber, int pageSize, int totalMembersCount)
            : this(enumerable.AsQueryable(), currentPageNumber, pageSize, totalMembersCount) { }

        /// <summary>
        /// Get empty page
        /// </summary>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// var page = EmptyPage&lt;ExampleModel&gt;.Empty();
        /// </code>
        /// </example>
        public static EmptyPage<T> Empty() => new();

        private Func<QueryEntryState<T>, Func<int, Func<int, Action>>> InitializeMemberList() => state => s => k => () =>
        {
            // s = page size
            // k = skip
            base._memberList = new List<IPageMember<T>>(s);
            for (var i = 0; i < s; i++)
            {
                base._memberList.Add(new PageMember<T>(state, i, ref k));
            }
        };
    }
}