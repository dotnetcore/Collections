using System;
using System.Collections;
using System.Collections.Generic;
using DotNetCore.Collections.Paginable.Internal;

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Abstract PaginableSet base
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class PaginableSetBase<T> : IPaginable<T>
    {
        /// <summary>
        /// Lazy pined paged cache.
        /// </summary>
        protected readonly Dictionary<int, Lazy<IPage<T>>> _lazyPinedPagesCache;

        /// <summary>
        /// Gets limited type
        /// </summary>
        protected LimitedMembersTypes _limitedType { get; } = LimitedMembersTypes.Unlimited; //as default, unlimited.

        private readonly int _limitedMemberCount; //magical number, as default, zero means unlimited.

        private readonly int _realMemberCount;
        //if LimitedType is customize mode, real_member_count equals to limited_member_count, otherwise, not. 

        /// <inheritdoc />
        protected PaginableSetBase() { }

        /// <inheritdoc />
        protected PaginableSetBase(int pageSize, int realPageCount, int realMemberCount)
        {
            if (realMemberCount >= PaginableSettingsManager.Settings.MaxMemberItems)
            {
                throw new ArgumentOutOfRangeException(nameof(realMemberCount), "Paginable does not support large size result");
            }

            PageSize = pageSize;
            PageCount = realPageCount;
            _lazyPinedPagesCache = new Dictionary<int, Lazy<IPage<T>>>(realPageCount);

            _realMemberCount = realMemberCount;
            _limitedMemberCount = 0;
            _limitedType = LimitedMembersTypes.Unlimited;
        }

        /// <inheritdoc />
        protected PaginableSetBase(int pageSize, int realPageCount, int realMemberCount, int limitedMembersCount)
        {
            PageSize = pageSize;
            PageCount = realPageCount;
            _lazyPinedPagesCache = new Dictionary<int, Lazy<IPage<T>>>(realPageCount);

            _realMemberCount = limitedMembersCount <= realMemberCount
                ? limitedMembersCount
                : realMemberCount;
            _limitedMemberCount = _realMemberCount;
            _limitedType = LimitedMembersTypes.Customize;
        }

        /// <inheritdoc />
        public IEnumerator<IPage<T>> GetEnumerator()
        {
            for (int i = 1; i <= PageCount; i++)
            {
                if (HasInitializeSpecialPage(i, out var lazyPage))
                {
                    yield return lazyPage.Value;
                }
                else
                {
                    var lazyValue = GetSpecifiedPage(i, PageSize, _realMemberCount);
                    _lazyPinedPagesCache[i] = lazyValue;
                    yield return lazyValue.Value;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public int PageSize { get; }

        /// <inheritdoc />
        public int MemberCount => _realMemberCount;

        /// <summary>
        /// Gets limited member count
        /// </summary>
        public int LimitedMemberCount => _limitedMemberCount;

        /// <summary>
        /// Gets page count
        /// </summary>
        public int PageCount { get; }

        /// <summary>
        /// Get specific page from current PaginableSet
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <returns></returns>
        public IPage<T> GetPage(int pageNumber)
        {
            if (PageCount == 0)
                return new EmptyPage<T>();

            if (pageNumber < 1 || pageNumber > PageCount)
                throw new ArgumentOutOfRangeException(nameof(pageNumber), $"{nameof(pageNumber)} can not be less than 1 or greater than pages count.");

            if (HasInitializeSpecialPage(pageNumber, out var lazyPage))
                return lazyPage.Value;

            var lazyValue = GetSpecifiedPage(pageNumber, PageSize, _realMemberCount);
            _lazyPinedPagesCache[pageNumber] = lazyValue;
            return lazyValue.Value;
        }

        private bool HasInitializeSpecialPage(int pageNumber, out Lazy<IPage<T>> lazyPage)
        {
            if (pageNumber < 1 || pageNumber > PageCount)
                throw new ArgumentOutOfRangeException(nameof(pageNumber));
            return _lazyPinedPagesCache.TryGetValue(pageNumber, out lazyPage);
        }

        /// <summary>
        /// Get specified page
        /// </summary>
        /// <param name="currentPageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="realMemberCount"></param>
        /// <returns></returns>
        protected abstract Lazy<IPage<T>> GetSpecifiedPage(int currentPageNumber, int pageSize, int realMemberCount);
    }
}