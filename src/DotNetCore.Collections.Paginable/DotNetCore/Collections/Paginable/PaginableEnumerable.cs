using System;
using System.Collections.Generic;

namespace DotNetCore.Collections.Paginable {
    /// <summary>
    /// EnumerablePage collection
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PaginableEnumerable<T> : PaginableSetBase<T> {
        private readonly IEnumerable<T> _enumerable;

        // ReSharper disable once UnusedMember.Local
        private PaginableEnumerable() { }

        internal PaginableEnumerable(IEnumerable<T> enumerable, int pageSize, int realPageCount, int realMemberCount)
            : base(pageSize, realPageCount, realMemberCount) {
            _enumerable = enumerable;
        }

        internal PaginableEnumerable(IEnumerable<T> enumerable, int pageSize, int realPageCount, int realMemberCount, int limitedMembersCount)
            : base(pageSize, realPageCount, realMemberCount, limitedMembersCount) {
            _enumerable = enumerable;
        }

        /// <summary>
        /// Get special page
        /// </summary>
        /// <param name="currentPageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="realMemberCount"></param>
        /// <returns></returns>
        protected override Lazy<IPage<T>> GetSpecifiedPage(int currentPageNumber, int pageSize, int realMemberCount) {
            return new Lazy<IPage<T>>(() => new EnumerablePage<T>(_enumerable, currentPageNumber, pageSize, realMemberCount));
        }
    }
}