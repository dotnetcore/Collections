using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable.Internal;

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Abstract page base
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class PageBase<T> : IPage<T>
    {
        /// <summary>
        /// Member list
        /// </summary>
        // ReSharper disable once InconsistentNaming
        protected IList<IPageMember<T>> _memberList;

        /// <summary>
        /// Initialize action
        /// </summary>
        // ReSharper disable once InconsistentNaming
        protected Action _initializeAction;

        private volatile bool _mHasInitialized;
        private readonly object _mInitializeLock = new();

        /// <summary>
        /// Page base
        /// </summary>
        /// <param name="sourceIsFull"></param>
        protected PageBase(bool sourceIsFull) => SourceIsFull = sourceIsFull;

        /// <inheritdoc />
        /// <example>
        /// <code>
        /// foreach (var member in page)
        /// {
        ///     var value = member.Value;
        /// }
        /// </code>
        /// </example>
        public IEnumerator<IPageMember<T>> GetEnumerator()
        {
            CheckOrInitializePage();
            return _memberList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Offset mode
        /// </summary>
        protected bool SourceIsFull { get; private set; }

        /// <inheritdoc />
        public int TotalPageCount { get; protected set; }

        /// <inheritdoc />
        public int TotalMemberCount { get; protected set; }

        /// <inheritdoc />
        public int CurrentPageNumber { get; protected set; }

        /// <inheritdoc />
        public int PageSize { get; protected set; }

        /// <inheritdoc />
        public int CurrentPageSize { get; protected set; }

        /// <inheritdoc />
        public bool HasPrevious { get; protected set; }

        /// <inheritdoc />
        public bool HasNext { get; protected set; }

        /// <inheritdoc />
        public IPageMember<T> this[int index]
        {
            get
            {
                CheckOrInitializePage();
                return _memberList[index];
            }
        }

        /// <inheritdoc />
        /// <example>
        /// <code>
        /// PageMetadata metadata = page.GetMetadata();
        /// </code>
        /// </example>
        public PageMetadata GetMetadata() => new(this);

        /// <inheritdoc />
        /// <example>
        /// <code>
        /// IEnumerable&lt;T&gt; items = page.ToOriginalItems();
        /// </code>
        /// </example>
        public IEnumerable<T> ToOriginalItems()
        {
            CheckOrInitializePage();
            return _memberList.Select(x => x.Value);
        }

        /// <summary>
        /// Initialize page meta info.
        /// </summary>
        /// <param name="currentPageNumber">current page number</param>
        /// <param name="pageSize">page size</param>
        /// <param name="totalMemberCount">total member count</param>
        /// <param name="skip">skip count</param>
        protected void InitializeMetaInfo(int currentPageNumber, int pageSize, int totalMemberCount, int skip)
        {
            // The derivation lives in PageMetaInfo so that metadata produced without a page
            // (see PageMetadata's internal constructor) can not drift from this one.
            var info = PageMetaInfo.Calculate(currentPageNumber, pageSize, totalMemberCount, skip);

            TotalPageCount = info.TotalPageCount;
            TotalMemberCount = totalMemberCount;
            CurrentPageNumber = currentPageNumber;
            PageSize = pageSize;
            CurrentPageSize = info.CurrentPageSize;
            HasPrevious = info.HasPrevious;
            HasNext = info.HasNext;
        }

        private void CheckOrInitializePage()
        {
            // Thread-safe lazy initialization: a shared page instance may be enumerated
            // concurrently (pages are cached in PaginableSetBase), so the initialize action
            // must run exactly once and complete before any other thread reads _memberList.
            if (_mHasInitialized)
                return;

            lock (_mInitializeLock)
            {
                if (_mHasInitialized)
                    return;

                _initializeAction?.Invoke();
                _mHasInitialized = true;
            }
        }
    }
}