using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetCore.Collections.Paginable {
    /// <summary>
    /// Enumerable page
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EnumerablePage<T> : PageBase<T> {
        /// <summary>
        /// Enumerable page
        /// </summary>
        /// <param name="enumerable"></param>
        /// <param name="currentPageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="totalMemberCount"></param>
        public EnumerablePage(IEnumerable<T> enumerable, int currentPageNumber, int pageSize, int totalMemberCount) : base() {
            var skip = (currentPageNumber - 1) * pageSize;
            InitializeMetaInfo()(currentPageNumber)(pageSize)(totalMemberCount)(skip)();
            base._initializeAction = InitializeMemberList()(enumerable)(CurrentPageSize)(skip);
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
            base.TotalPageCount = (int) Math.Ceiling((double) t / (double) s);
            base.TotalMemberCount = t;
            base.CurrentPageNumber = c;
            base.PageSize = s;
            base.CurrentPageSize = c == TotalPageCount
                ? t % k
                : s;

            base.HasPrevious = c > 1;
            base.HasNext = c < t;
        };

        private Func<IEnumerable<T>, Func<int, Func<int, Action>>> InitializeMemberList()
            => array => s => k => () => {
                // s = page size
                // k = skip
                base._memberList = new List<IPageMember<T>>(s);
                if (array is IQueryable<T> query) {
                    var realQuery = query.Skip(k).Take(s).ToList();
                    var offset = 0;
                    foreach (var item in realQuery) {
                        base._memberList.Add(new PageMember<T>(item, offset++, ref k));
                    }
                } else {
                    for (var i = 0; i < s; i++) {
                        base._memberList.Add(new PageMember<T>(array.ElementAt(k + i), i, ref k));
                    }
                }
            };

    }
}