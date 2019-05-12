using System;
using System.Collections.Generic;
using Dos.ORM;
using DotNetCore.Collections.Paginable.Internal;

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Dos.ORM page
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DosPage<T> : PageBase<T> where T : Entity
    {
        /// <summary>
        /// Dos.ORM page
        /// </summary>
        /// <param name="query"></param>
        /// <param name="currentPageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="totalMemberCount"></param>
        /// <param name="additionalQueryFunc"></param>
        public DosPage(FromSection<T> query, int currentPageNumber, int pageSize, int totalMemberCount, Func<FromSection<T>, FromSection<T>> additionalQueryFunc = null) : base()
        {
            var skip = (currentPageNumber - 1) * pageSize;
            var state = new DosQueryState<T>(query, currentPageNumber, pageSize, additionalQueryFunc);
            InitializeMetaInfo()(currentPageNumber)(pageSize)(totalMemberCount)(skip)();
            base._initializeAction = InitializeMemberList()(state)(pageSize)(skip);
        }

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
            var totalPageCount = (int)Math.Ceiling((double)t / (double)s);
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

        private Func<DosQueryState<T>, Func<int, Func<int, Action>>> InitializeMemberList() => state => s => k => () =>
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
