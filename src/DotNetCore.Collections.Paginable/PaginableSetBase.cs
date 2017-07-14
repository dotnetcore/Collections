using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using DotNetCore.Collections.Paginable.Internal;

namespace DotNetCore.Collections.Paginable
{
    public abstract class PaginableSetBase<T> : IPaginable<T>
    {
        //protected readonly List<Lazy<IPage<T>>> m_lazyPinedPagesCache;
        protected readonly Dictionary<int, Lazy<IPage<T>>> m_lazyPinedPagesCache2;
        //protected readonly Dictionary<int, bool> m_lazyPinedPagesInitiizedState;

        protected LimitedMembersTypes m_limitedType { get; } = LimitedMembersTypes.Unlimited;//as default, unlimited.
        private readonly int m_limitedMemberCount;//magical number, as default, zero means unlimited.
        private readonly int m_realMemberCount;//if LimitedType is customize mode, real_member_count equals to limited_member_count, otherwise, not. 

        protected PaginableSetBase() { }

        protected PaginableSetBase(int pageSize, int realPageCount, int realMemberCount)
        {
            if (realMemberCount >= PaginableConstants.MAX_MEMBER_ITEMS_SUPPORT)
            {
                throw new ArgumentOutOfRangeException(nameof(realMemberCount), $"Paginable does not support large size result");
            }

            PageSize = pageSize;
            PageCount = realPageCount;
            //m_lazyPinedPagesCache = new List<Lazy<IPage<T>>>(realPageCount);
            m_lazyPinedPagesCache2 = new Dictionary<int, Lazy<IPage<T>>>(realPageCount);
            //m_lazyPinedPagesInitiizedState = new Dictionary<int, bool>(realPageCount);

            m_realMemberCount = realMemberCount;
            m_limitedMemberCount = 0;
            m_limitedType = LimitedMembersTypes.Unlimited;
        }

        protected PaginableSetBase(int pageSize, int realPageCount, int realMemberCount, int limitedMembersCount)
        {
            PageSize = pageSize;
            PageCount = realPageCount;
            //m_lazyPinedPagesCache = new List<Lazy<IPage<T>>>(realPageCount);
            m_lazyPinedPagesCache2 = new Dictionary<int, Lazy<IPage<T>>>(realPageCount);
            //m_lazyPinedPagesInitiizedState = new Dictionary<int, bool>(realPageCount);

            m_realMemberCount = limitedMembersCount <= realMemberCount
                ? limitedMembersCount
                : realMemberCount;
            m_limitedMemberCount = m_realMemberCount;
            m_limitedType = LimitedMembersTypes.Customize;
        }

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
                    var lazyValue = GetSpecialPage(i, PageSize, m_realMemberCount);
                    m_lazyPinedPagesCache2[i] = lazyValue;
                    yield return lazyValue.Value;
                }
            }

            //for (int i = 1; i <= PageCount; i++)
            //{
            //    if (HasInitializeSpecialPage(i))
            //    {
            //        yield return m_lazyPinedPagesCache2[i].Value;
            //    }
            //    else
            //    {
            //        var lazyValue = GetSpecialPage(i, PageSize, m_realMemberCount);
            //        m_lazyPinedPagesCache2[i] = lazyValue;
            //        yield return lazyValue.Value;
            //    }
            //}

            //for (var i = 0; i < PageCount; i++)
            //{
            //    yield return m_lazyPinedPagesCache[i].Value;
            //}
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int PageSize { get; }

        public int MemberCount => m_realMemberCount;

        public int PageCount { get; }

        public IPage<T> GetPage(int pageNumber)
        {
            if (pageNumber < 1 || pageNumber > PageCount)
            {
                throw new ArgumentOutOfRangeException(nameof(pageNumber), $"{nameof(pageNumber)} can not be less than 1 or greater than pages count.");
            }

            if (HasInitializeSpecialPage(pageNumber, out var lazyPage))
            {
                return lazyPage.Value;
            }
            else
            {
                var lazyValue = GetSpecialPage(pageNumber, PageSize, m_realMemberCount);
                m_lazyPinedPagesCache2[pageNumber] = lazyValue;
                return lazyValue.Value;
            }

            //if (HasInitializeSpecialPage(pageNumber))
            //{
            //    return m_lazyPinedPagesCache2[pageNumber].Value;
            //}
            //else
            //{
            //    var lazyValue = GetSpecialPage(pageNumber, PageSize, m_realMemberCount);
            //    m_lazyPinedPagesCache2[pageNumber] = lazyValue;
            //    return lazyValue.Value;
            //}

            //return m_lazyPinedPagesCache[pageNumber - 1].Value;
        }

        //private bool HasInitializeSpecialPage(int pageNumber)
        //{
        //    if (pageNumber < 1 || pageNumber > PageCount)
        //    {
        //        throw new ArgumentOutOfRangeException(nameof(pageNumber));
        //    }

        //    return m_lazyPinedPagesInitiizedState.TryGetValue(pageNumber, out bool ret) && ret;
        //}

        private bool HasInitializeSpecialPage(int pageNumber, out Lazy<IPage<T>> lazyPage)
        {
            if (pageNumber < 1 || pageNumber > PageCount)
            {
                throw new ArgumentOutOfRangeException(nameof(pageNumber));
            }

            return m_lazyPinedPagesCache2.TryGetValue(pageNumber, out lazyPage);
        }

        protected abstract Lazy<IPage<T>> GetSpecialPage(int currentPageNumber, int pageSize, int realMemberCount);
    }
}
