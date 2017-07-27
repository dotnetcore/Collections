using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable.Internal;

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
        public QueryablePage(IQueryable<T> queryable, int currentPageNumber, int pageSize, int totalMemberCount) : base()
        {
            var skip = (currentPageNumber - 1) * pageSize;

            var state = new QueryEntryState<T>(queryable, skip, pageSize);

            InitializeMetaInfo()(currentPageNumber)(pageSize)(totalMemberCount)(skip)();

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
        public static EmptyPage<T> Empty() => new EmptyPage<T>();

        private Func<int, Func<int, Func<int, Func<int, Action>>>> InitializeMetaInfo() => c => s => t => k => () =>
        {
            // c = current page number
            // s = page size
            // t = total member count
            // k = skip
            base.TotalPageCount = (int)Math.Ceiling((double)t / (double)s);
            base.TotalMemberCount = t;
            base.CurrentPageNumber = c;
            base.PageSize = s;
            base.CurrentPageSize = c == TotalPageCount
                ? t % k
                : s;

            base.HasPrevious = c > 1;
            base.HasNext = c < t;
        };

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