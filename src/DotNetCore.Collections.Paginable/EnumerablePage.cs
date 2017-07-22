using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetCore.Collections.Paginable
{
    public class EnumerablePage<T> : PageBase<T>
    {
        public EnumerablePage(IEnumerable<T> enumerable, int currentPageNumber, int pageSize, int totalMemberCount) : base()
        {
            var skip = (currentPageNumber - 1) * pageSize;

            InitializeMetaInfo()(currentPageNumber)(pageSize)(totalMemberCount)(skip)();

            base.m_initializeAction = InitializeMemberList()(enumerable)(CurrentPageSize)(skip);

            //base.m_initializeAction();
        }

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

        private Func<IEnumerable<T>, Func<int, Func<int, Action>>> InitializeMemberList()
            => array => size => skip => () =>
            {
                base.m_memberList = new List<IPageMember<T>>(size);
                for (var i = 0; i < size; i++)
                {
                    base.m_memberList.Add(new PageMember<T>(array.ElementAt(skip + i), i, skip));
                }
            };

    }
}
