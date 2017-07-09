using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable.Internal;

namespace DotNetCore.Collections.Paginable
{
    public abstract class PaginableSetBase<T> : IPaginable<T>
    {
        private readonly IList<Lazy<IPage<T>>> m_lazyPinedPagesCache;
        private readonly int m_realPageCount;
        private readonly IEnumerable<T> m_enumerable;
        private readonly IQueryable<T> m_queryable;

        private LimitedMembersTypes LimitedType { get; } = LimitedMembersTypes.Unlimited;//as default, unlimited.
        protected int LimitedMemberCount { get; set; } = 0;//magical number, as default, zero means unlimited.
        private readonly int m_realMemberCount;//if LimitedType is customize mode, real_member_count equals to limited_member_count, otherwise, not. 

        protected PaginableSetBase() { }

        protected PaginableSetBase(IEnumerable<T> enumerable, int pageSize, int realPageCount, int realMemberCount)
        {
            m_realPageCount = realPageCount;
            m_lazyPinedPagesCache = new List<Lazy<IPage<T>>>(realPageCount);
            m_enumerable = enumerable;
            m_queryable = null;

            m_realMemberCount = realPageCount;
            LimitedType = LimitedMembersTypes.Unlimited;

            MemberCount = realPageCount;
            PageSize = pageSize;

            InitializeEnumerablePagesCache(pageSize, realMemberCount, realPageCount);
        }

        protected PaginableSetBase(IEnumerable<T> enumerable, int pageSize, int realPageCount, int realMemberCount,
            int limitedMembersCount)
        {

            m_realPageCount = realPageCount;
            m_lazyPinedPagesCache = new List<Lazy<IPage<T>>>(realPageCount);
            m_enumerable = enumerable;
            m_queryable = null;

            m_realMemberCount = limitedMembersCount <= realMemberCount
                ? limitedMembersCount
                : realMemberCount;
            LimitedMemberCount = m_realMemberCount;
            LimitedType = LimitedMembersTypes.Customize;

            MemberCount = LimitedMemberCount;
            PageSize = pageSize;

            InitializeEnumerablePagesCache(pageSize, limitedMembersCount, realPageCount);
        }

        private void InitializeEnumerablePagesCache(int pageSize, int realMemberCount, int realPageCount)
        {
            for (var i = 0; i < realPageCount; i++)
            {
                var pageNumber = i + 1;
                m_lazyPinedPagesCache[i] = new Lazy<IPage<T>>(() =>
                    new EnumerablePage<T>(m_enumerable.Skip((pageNumber - 1) * pageSize).Take(pageSize), pageNumber,
                        pageSize, realMemberCount));
            }
        }

        private void InitializeQueryablePagesCache(int pageSize, int realMemberCount, int realPageCount)
        {

        }

        public IEnumerator<IPage<T>> GetEnumerator()
        {
            for (var i = 0; i < m_realPageCount; i++)
            {
                yield return m_lazyPinedPagesCache[i].Value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int PageSize { get; }

        public int MemberCount { get; }
    }
}
