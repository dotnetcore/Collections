using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable.Internal;

namespace DotNetCore.Collections.Paginable
{
    public class QueryablePage<T> : PageBase<T>
    {
        public QueryablePage(IQueryable<T> queryable, int currentPageNumber, int pageSize, int totalMembersCount) : base()
        {
            var skip = (currentPageNumber - 1) * pageSize;

            var state = new QueryEntryState<T>(queryable, skip, pageSize);

            base.TotalPageCount = (int)Math.Ceiling((double)totalMembersCount / (double)pageSize);
            base.TotalMemberCount = totalMembersCount;
            base.CurrentPageNumber = currentPageNumber;
            base.PageSize = pageSize;
            base.CurrentPageSize = currentPageNumber == TotalPageCount
                ? totalMembersCount % skip
                : pageSize;

            base.HasPrevious = currentPageNumber > 1;
            base.HasNext = currentPageNumber < totalMembersCount;

            base.m_initializeAction = InitializeMemberList()(state)(CurrentPageSize)(skip);

            base.m_initializeAction();
        }

        public QueryablePage(IEnumerable<T> enumerable, int currentPageNumber, int pageSize, int totalMembersCount)
            : this(enumerable.AsQueryable(), currentPageNumber, pageSize, totalMembersCount) { }

        public static EmptyPage<T> Empty() => new EmptyPage<T>();

        private Func<QueryEntryState<T>, Func<int, Func<int, Action>>> InitializeMemberList()
            => state => size => skip => () =>
            {
                base.m_memberList = new List<IPageMember<T>>(size);
                for (var i = 0; i < size; i++)
                {
                    base.m_memberList.Add(new PageMember<T>(state, i, skip));
                }
            };

    }
}


