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

            base.TotalPageCount = (int)Math.Ceiling((double)totalMemberCount / (double)pageSize);
            base.TotalMemberCount = totalMemberCount;
            base.CurrentPageNumber = currentPageNumber;
            base.PageSize = pageSize;
            base.CurrentPageSize = currentPageNumber == TotalPageCount
                ? totalMemberCount % skip
                : pageSize;

            base.HasPrevious = currentPageNumber > 1;
            base.HasNext = currentPageNumber < totalMemberCount;

            base.m_initializeAction = InitializeMemberList()(enumerable)(CurrentPageSize)(skip);

            base.m_initializeAction();
        }

        public static EmptyPage<T> Empty() => new EmptyPage<T>();

        private Func<IEnumerable<T>, Func<int, Func<int, Action>>> InitializeMemberList()
            => array => size => skip => () =>
            {
                base.m_memberList = new List<IPageMember<T>>(size);
                for (var i = 0; i < size; i++)
                {
                    base.m_memberList.Add(new PageMember<T>(array.ElementAt(i), i, skip));
                }
            };

    }
}
