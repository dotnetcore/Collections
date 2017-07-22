using System;
using System.Collections.Generic;

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// EnumerablePage collection
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PaginableEnumerable<T> : PaginableSetBase<T>
    {
        private readonly IEnumerable<T> m_enumerable;

        private PaginableEnumerable() { }

        internal PaginableEnumerable(IEnumerable<T> enumerable, int pageSize, int realPageCount, int realMemberCount)
            : base(pageSize, realMemberCount, realPageCount)
        {
            m_enumerable = enumerable;
        }

        internal PaginableEnumerable(IEnumerable<T> enumerable, int pageSize, int realPageCount, int realMemberCount, int limitedMemberCount)
            : base(pageSize, realPageCount, realMemberCount, limitedMemberCount)
        {
            m_enumerable = enumerable;
        }

        protected override Lazy<IPage<T>> GetSpecialPage(int currentPageNumber, int pageSize, int realMemberCount)
        {
            return new Lazy<IPage<T>>(() => new EnumerablePage<T>(m_enumerable, currentPageNumber, pageSize, realMemberCount));
        }
    }
}
