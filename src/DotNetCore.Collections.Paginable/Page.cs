using System;
using System.Collections;
using System.Collections.Generic;

namespace DotNetCore.Collections.Paginable
{
    public abstract class Page<T> : IPage<T>
    {
        protected readonly IList<IPageMember<T>> m_MemberList;

        protected Page()
        {
            m_MemberList = new List<IPageMember<T>>();
        }

        public IEnumerator<IPageMember<T>> GetEnumerator() => m_MemberList.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => m_MemberList.GetEnumerator();

        public int TotalPagesCount { get; protected set; }

        public int TotalMembersCount { get; protected set; }

        public int CurrentPageNumber { get; protected set; }

        public int PageSize { get; protected set; }

        public int CurrentPageSize { get; protected set; }

        public bool HasPrevious { get; protected set; }

        public bool HasNext { get; protected set; }

        public IPageMember<T> this[int index] => m_MemberList[index];
    }
}
