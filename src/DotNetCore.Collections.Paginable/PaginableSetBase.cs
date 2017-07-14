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
        protected readonly List<Lazy<IPage<T>>> m_lazyPinedPagesCache;

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
            m_lazyPinedPagesCache = new List<Lazy<IPage<T>>>(realPageCount);

            m_realMemberCount = realMemberCount;
            m_limitedMemberCount = 0;
            m_limitedType = LimitedMembersTypes.Unlimited;
        }

        protected PaginableSetBase(int pageSize, int realPageCount, int realMemberCount, int limitedMembersCount)
        {
            PageSize = pageSize;
            PageCount = realPageCount;
            m_lazyPinedPagesCache = new List<Lazy<IPage<T>>>(realPageCount);

            m_realMemberCount = limitedMembersCount <= realMemberCount
                ? limitedMembersCount
                : realMemberCount;
            m_limitedMemberCount = m_realMemberCount;
            m_limitedType = LimitedMembersTypes.Customize;
        }

        public IEnumerator<IPage<T>> GetEnumerator()
        {
            for (var i = 0; i < PageCount; i++)
            {
                yield return m_lazyPinedPagesCache[i].Value;
            }
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

            return m_lazyPinedPagesCache[pageNumber - 1].Value;
        }
    }
}
