using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DotNetCore.Collections.Paginable
{
    public abstract class PageBase<T> : IPage<T>
    {
        protected IList<IPageMember<T>> m_memberList;

        protected PageBase()
        {
        }

        public IEnumerator<IPageMember<T>> GetEnumerator() => m_memberList.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => m_memberList.GetEnumerator();

        public int TotalPageCount { get; protected set; }

        public int TotalMemberCount { get; protected set; }

        public int CurrentPageNumber { get; protected set; }

        public int PageSize { get; protected set; }

        public int CurrentPageSize { get; protected set; }

        public bool HasPrevious { get; protected set; }

        public bool HasNext { get; protected set; }

        public IPageMember<T> this[int index] => m_memberList[index];
    }
}
