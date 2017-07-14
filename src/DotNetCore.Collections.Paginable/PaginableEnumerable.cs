using System;
using System.Collections.Generic;

namespace DotNetCore.Collections.Paginable
{
    public class PaginableEnumerable<T> : PaginableSetBase<T>
    {
        private readonly IEnumerable<T> m_enumerable;

        private PaginableEnumerable() { }

        internal PaginableEnumerable(IEnumerable<T> enumerable, int pageSize, int realPageCount, int realMemberCount)
            : base(pageSize, realMemberCount, realPageCount)
        {
            m_enumerable = enumerable;

            InitializeEnumerablePagesCache(pageSize, realMemberCount, realPageCount);
        }

        internal PaginableEnumerable(IEnumerable<T> enumerable, int pageSize, int realPageCount, int realMemberCount, int limitedMemberCount)
            : base(pageSize, realPageCount, realMemberCount, limitedMemberCount)
        {
            m_enumerable = enumerable;

            InitializeEnumerablePagesCache(pageSize, realMemberCount, realPageCount);
        }

        private void InitializeEnumerablePagesCache(int pageSize, int realMemberCount, int realPageCount)
        {
            for (var i = 0; i < realPageCount; i++)
            {
                var currentPageNumber = i + 1;
                m_lazyPinedPagesCache.Insert(i, new Lazy<IPage<T>>(() =>
                    new EnumerablePage<T>(m_enumerable, currentPageNumber, pageSize, realMemberCount)));
            }
        }

        internal IEnumerable<T> ExportEnumerable() => m_enumerable;
    }
}
