using System;
using System.Collections.Generic;
using DotNetCore.Collections.Paginable.Internal;
using FreeSql;

// ReSharper disable RedundantBaseQualifier
// ReSharper disable RedundantCast

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// FreeSql page
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FreeSqlPage<T> : PageBase<T> where T : class
    {
        /// <summary>
        /// FreeSql page
        /// </summary>
        /// <param name="select"></param>
        /// <param name="currentPageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="totalMemberCount"></param>
        /// <param name="includeNestedMembers"></param>
        // ReSharper disable once RedundantBaseConstructorCall
        public FreeSqlPage(ISelect<T> select, int currentPageNumber, int pageSize, int totalMemberCount, bool includeNestedMembers) : base(false)
        {
            var skip = (currentPageNumber - 1) * pageSize;
            var state = new FreeSqlQueryState<T>(select, currentPageNumber, pageSize, includeNestedMembers);
            InitializeMetaInfo(currentPageNumber, pageSize, totalMemberCount, skip);
            base._initializeAction = InitializeMemberList()(state)(CurrentPageSize)(skip);
        }

        /// <summary>
        /// Get empty page
        /// </summary>
        /// <returns></returns>
        public static EmptyPage<T> Empty() => new();

        private Func<FreeSqlQueryState<T>, Func<int, Func<int, Action>>> InitializeMemberList() => state => s => k => () =>
        {
            // s = page size
            // k = skip
            base._memberList = new List<IPageMember<T>>(s);
            for (var i = 0; i < s; i++)
            {
                base._memberList.Add(PageMemberFactory.Create<T>(state, i, ref k));
            }
        };
    }
}