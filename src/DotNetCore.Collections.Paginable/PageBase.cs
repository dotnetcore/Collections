using System;
using System.Collections;
using System.Collections.Generic;

namespace DotNetCore.Collections.Paginable
{
    // ReSharper disable InconsistentNaming
    public abstract class PageBase<T> : IPage<T>
    {
        protected IList<IPageMember<T>> m_memberList;
        protected Action m_initializeAction;
        private bool m_hasInitialized = false;

        protected PageBase() { }

        public IEnumerator<IPageMember<T>> GetEnumerator()
        {
            //CheckOrInitializePage();

            return m_memberList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int TotalPageCount { get; protected set; }

        public int TotalMemberCount { get; protected set; }

        public int CurrentPageNumber { get; protected set; }

        public int PageSize { get; protected set; }

        public int CurrentPageSize { get; protected set; }

        public bool HasPrevious { get; protected set; }

        public bool HasNext { get; protected set; }

        public IPageMember<T> this[int index]
        {
            get
            {
                //CheckOrInitializePage();

                return m_memberList[index];
            }
        }

        private void CheckOrInitializePage()
        {
            if (!m_hasInitialized)
            {
                m_initializeAction?.Invoke();
                m_hasInitialized = true;
            }
        }
    }
}
