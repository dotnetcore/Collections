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
            var skip = (currentPageNumber - 1) * pageSize;

            m_pinedEntryState = new QueryEntryState<T>(queryable, skip, pageSize);

            base.TotalPageCount = (int)Math.Ceiling((double)totalMembersCount / (double)pageSize);
            base.TotalMemberCount = totalMembersCount;
            base.CurrentPageNumber = currentPageNumber;
            base.PageSize = pageSize;
            base.CurrentPageSize = currentPageNumber == TotalPageCount
                ? totalMembersCount % skip
                : pageSize;

            base.HasPrevious = currentPageNumber > 1;
            base.HasNext = currentPageNumber < totalMembersCount;

            for (var i = 0; i < CurrentPageSize; i++)
            {
                base.m_memberList.Add(new PageMember<T>(m_pinedEntryState, i, skip));
            }
        }

        public QueryablePage(IEnumerable<T> enumerable, int currentPageNumber, int pageSize, int totalMembersCount)
            : this(enumerable.AsQueryable(), currentPageNumber, pageSize, totalMembersCount) { }

        public static EmptyPage<T> Empty() => new EmptyPage<T>();

        internal IQueryable<T> ExportQueryable() => m_pinedEntryState.ExporyQueryableCache();
    }
}


