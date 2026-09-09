using System;
using System.Collections.Generic;
using Chloe;
using DotNetCore.Collections.Paginable.Internal;

// ReSharper disable RedundantBaseQualifier
// ReSharper disable RedundantCast

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Chloe page
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ChloePage<T> : PageBase<T>
    {
        /// <summary>
        /// Chloe page
        /// </summary>
        /// <param name="query"></param>
        /// <param name="currentPageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="totalMemberCount"></param>
        /// <param name="additionalQueryFunc"></param>
        // ReSharper disable once RedundantBaseConstructorCall
        public ChloePage(IQuery<T> query, int currentPageNumber, int pageSize, int totalMemberCount, Func<IQuery<T>, IQuery<T>> additionalQueryFunc = null) : base(false)
        {
            var skip = (currentPageNumber - 1) * pageSize;
            var state = new ChloeQueryState<T>(query, currentPageNumber, pageSize, additionalQueryFunc);
            InitializeMetaInfo(currentPageNumber, pageSize, totalMemberCount, skip);
            base._initializeAction = InitializeMemberList()(state)(CurrentPageSize)(skip);
        }

        /// <summary>
        /// Get empty page
        /// </summary>
        /// <returns></returns>
        public static EmptyPage<T> Empty() => new();

        private Func<ChloeQueryState<T>, Func<int, Func<int, Action>>> InitializeMemberList() => state => s => k => () =>
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