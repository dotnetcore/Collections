using System;
using System.Collections;
using System.Collections.Concurrent;
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
        /// Lazy pined paged cache. Concurrent: parallel paging must not corrupt the cache.
        /// </summary>
        protected readonly ConcurrentDictionary<int, Lazy<IPage<T>>> _lazyPinedPagesCache;

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
            if (realMemberCount > PaginableSettingsManager.Settings.MaxMemberItems)
            {
                throw new ArgumentOutOfRangeException(nameof(realMemberCount), "Paginable does not support large size result");
            }

            PageSize = pageSize;
            PageCount = realPageCount;
            _lazyPinedPagesCache = new ConcurrentDictionary<int, Lazy<IPage<T>>>(Environment.ProcessorCount, realPageCount);

            _realMemberCount = realMemberCount;
            _limitedMemberCount = 0;
            _limitedType = LimitedMembersTypes.Unlimited;
        }

        /// <inheritdoc />
        protected PaginableSetBase(int pageSize, int realPageCount, int realMemberCount, int limitedMembersCount)
        {
            PageSize = pageSize;
            PageCount = realPageCount;
            _lazyPinedPagesCache = new ConcurrentDictionary<int, Lazy<IPage<T>>>(Environment.ProcessorCount, realPageCount);

            _realMemberCount = limitedMembersCount <= realMemberCount
                ? limitedMembersCount
                : realMemberCount;
            _limitedMemberCount = _realMemberCount;
            _limitedType = LimitedMembersTypes.Customize;
        }

        /// <inheritdoc />
        /// <example>
        /// <code>
        /// foreach (var member in page)
        /// {
        ///     var value = member.Value;
        /// }
        /// </code>
        /// </example>
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
                    // GetOrAdd guarantees a single Lazy instance per page number across
                    // concurrent enumerators; Lazy itself serializes first-time materialization.
                    var lazyValue = _lazyPinedPagesCache.GetOrAdd(i, _ => GetSpecifiedPage(i, PageSize, _realMemberCount));
                    yield return lazyValue.Value;
                }            }
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
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="pageNumber"/> is out of its allowed range.</exception>
        /// <example>
        /// <code>
        /// var page = paginable.GetPage(15);
        /// </code>
        /// </example>
        public IPage<T> GetPage(int pageNumber)
        {
            if (PageCount == 0)
                return new EmptyPage<T>();

            if (pageNumber < 1 || pageNumber > PageCount)
                throw new ArgumentOutOfRangeException(nameof(pageNumber), $"{nameof(pageNumber)} can not be less than 1 or greater than pages count.");

            if (HasInitializeSpecialPage(pageNumber, out var lazyPage))
                return lazyPage.Value;

            var lazyValue = _lazyPinedPagesCache.GetOrAdd(pageNumber, _ => GetSpecifiedPage(pageNumber, PageSize, _realMemberCount));
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