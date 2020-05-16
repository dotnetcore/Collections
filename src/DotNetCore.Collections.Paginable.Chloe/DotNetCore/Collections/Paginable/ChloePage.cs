using System;
using System.Collections.Generic;
using Chloe;
using DotNetCore.Collections.Paginable.Internal;

// ReSharper disable RedundantBaseQualifier
// ReSharper disable RedundantCast

namespace DotNetCore.Collections.Paginable {
    /// <summary>
    /// Chloe page
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ChloePage<T> : PageBase<T> {
        /// <summary>
        /// Chloe page
        /// </summary>
        /// <param name="query"></param>
        /// <param name="currentPageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="totalMemberCount"></param>
        /// <param name="additionalQueryFunc"></param>
        // ReSharper disable once RedundantBaseConstructorCall
        public ChloePage(IQuery<T> query, int currentPageNumber, int pageSize, int totalMemberCount, Func<IQuery<T>, IQuery<T>> additionalQueryFunc = null) : base(false) {
            var skip = (currentPageNumber - 1) * pageSize;
            var state = new ChloeQueryState<T>(query, currentPageNumber, pageSize, additionalQueryFunc);
            InitializeMetaInfo()(currentPageNumber)(pageSize)(totalMemberCount)(skip)();
            base._initializeAction = InitializeMemberList()(state)(CurrentPageSize)(skip);
        }

        /// <summary>
        /// Get empty page
        /// </summary>
        /// <returns></returns>
        public static EmptyPage<T> Empty() => new EmptyPage<T>();

        private Func<int, Func<int, Func<int, Func<int, Action>>>> InitializeMetaInfo() => c => s => t => k => () => {
            // c = current page number
            // s = page size
            // t = total member count
            // k = skip
            var totalPageCount = (int) Math.Ceiling((double) t / (double) s);
            totalPageCount = totalPageCount < 0 ? 0 : totalPageCount;
            base.TotalPageCount = totalPageCount == 0 ? 1 : totalPageCount;
            base.TotalMemberCount = t;
            base.CurrentPageNumber = c;
            base.PageSize = s;
            base.CurrentPageSize = c == totalPageCount
                ? k == 0
                    ? t
                    : t % k
                : totalPageCount == 0
                    ? 0
                    : s;

            base.HasPrevious = c > 1;
            base.HasNext = c < base.TotalPageCount;
        };

        private Func<ChloeQueryState<T>, Func<int, Func<int, Action>>> InitializeMemberList() => state => s => k => () => {
            // s = page size
            // k = skip
            base._memberList = new List<IPageMember<T>>(s);
            for (var i = 0; i < s; i++) {
                base._memberList.Add(PageMemberFactory.Create<T>(state, i, ref k));
            }
        };
    }
}