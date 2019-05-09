﻿using System;
using System.Linq;

namespace DotNetCore.Collections.Paginable {
    /// <summary>
    /// QueryablePage collection
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PaginableQueryable<T> : PaginableSetBase<T> {
        private readonly IQueryable<T> _queryable;

        private PaginableQueryable() { }

        internal PaginableQueryable(IQueryable<T> queryable, int pageSize, int realPageCount, int realMemberCount)
            : base(pageSize,  realPageCount, realMemberCount) {
            _queryable = queryable;
        }

        internal PaginableQueryable(IQueryable<T> queryable, int pageSize, int realPageCount, int realMemberCount, int limitedMembersCount)
            : base(pageSize, realPageCount, realMemberCount, limitedMembersCount) {
            _queryable = queryable;
        }

        protected override Lazy<IPage<T>> GetSpecialPage(int currentPageNumber, int pageSize, int realMemberCount) {
            return new Lazy<IPage<T>>(() => new EnumerablePage<T>(_queryable, currentPageNumber, pageSize, realMemberCount));
        }
    }
}