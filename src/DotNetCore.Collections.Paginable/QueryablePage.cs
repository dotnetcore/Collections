using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable.Internal;

namespace DotNetCore.Collections.Paginable
{
    public class QueryablePage<T> : PageBase<T>
    {
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        // ReSharper disable once InconsistentNaming
        private readonly QueryEntryState<T> m_pinedEntryState;

        public QueryablePage(IQueryable<T> queryable, int currentPageNumber, int pageSize, int totalMembersCount) : base()
        {
            m_pinedEntryState = new QueryEntryState<T>(queryable, (currentPageNumber - 1) * pageSize, pageSize);

            base.TotalPagesCount = (int)Math.Ceiling((double)totalMembersCount / (double)pageSize);
            base.TotalMembersCount = totalMembersCount;
            base.CurrentPageNumber = currentPageNumber;
            base.PageSize = pageSize;
            base.CurrentPageSize = currentPageNumber == TotalPagesCount
                ? totalMembersCount % ((currentPageNumber - 1) * pageSize)
                : pageSize;

            base.HasPrevious = currentPageNumber > 1;
            base.HasNext = currentPageNumber < totalMembersCount;

            for (var i = 0; i < CurrentPageSize; i++)
            {
                base.m_MemberList.Add(new PageMember<T>(m_pinedEntryState, i));
            }
        }

        public QueryablePage(IEnumerable<T> enumerable, int currentPageNumber, int pageSize, int totalMembersCount)
            : this(enumerable.AsQueryable(), currentPageNumber, pageSize, totalMembersCount) { }

        public static EmptyPage<T> Empty() => new EmptyPage<T>();
    }
}


