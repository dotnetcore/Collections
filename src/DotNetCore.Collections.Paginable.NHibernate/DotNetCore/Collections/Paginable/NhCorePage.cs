using System;
using System.Collections.Generic;
using DotNetCore.Collections.Paginable.Internal;
using NHibernate;

// ReSharper disable RedundantCast
// ReSharper disable RedundantBaseQualifier

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// NHibernate page
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class NhCorePage<T> : PageBase<T>
    {
        /// <summary>
        /// NHibernate page
        /// </summary>
        /// <param name="queryOver"></param>
        /// <param name="currentPageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="totalMemberCount"></param>
        // ReSharper disable once RedundantBaseConstructorCall
        public NhCorePage(IQueryOver<T> queryOver, int currentPageNumber, int pageSize, int totalMemberCount) : base(false)
        {
            var skip = (currentPageNumber - 1) * pageSize;
            var state = new NhCoreQueryState<T>(queryOver, skip, pageSize);
            InitializeMetaInfo(currentPageNumber, pageSize, totalMemberCount, skip);
            base._initializeAction = InitializeMemberList()(state)(CurrentPageSize)(skip);
        }

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

        private Func<NhCoreQueryState<T>, Func<int, Func<int, Action>>> InitializeMemberList() => state => s => k => () =>
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