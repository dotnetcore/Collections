using System;
using System.Collections;
using System.Collections.Generic;

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Abstract page base
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class PageBase<T> : IPage<T>
    {
        protected IList<IPageMember<T>> _memberList;
        protected Action _initializeAction;
        private bool _mHasInitialized = false;

        protected PageBase() { }

        public IEnumerator<IPageMember<T>> GetEnumerator()
        {
            CheckOrInitializePage();

            return _memberList.GetEnumerator();
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
                CheckOrInitializePage();

                return _memberList[index];
            }
        }

        private void CheckOrInitializePage()
        {
            if (!_mHasInitialized)
            {
                _initializeAction?.Invoke();
                _mHasInitialized = true;
            }
        }
    }
}
