using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable.Abstractions;
using NHibernate;

namespace DotNetCore.Collections.Paginable.Internal
{
    /// <summary>
    /// NHibernate query state
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class NhCoreQueryState<T> : IQueryEntryState<T>
    {
        private readonly Lazy<IList<T>> _mLazyNhQueryMembers;

        /// <summary>
        /// NHibernate query state
        /// </summary>
        /// <param name="queryOver"></param>
        /// <param name="skip"></param>
        /// <param name="pageSize"></param>
        public NhCoreQueryState(IQueryOver<T> queryOver, int skip, int pageSize)
        {
            if (queryOver is null)
                throw new ArgumentNullException(nameof(queryOver));

            if (skip < 0)
                throw new ArgumentOutOfRangeException(nameof(skip), $"{nameof(skip)} can not be less than zero");

            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than one");

            // Materialize the future result into an IList once, so that later
            // ElementAt(offset) accesses are O(1) instead of re-enumerating
            // the IFutureEnumerable every time (O(s^2) per page).
            _mLazyNhQueryMembers = new Lazy<IList<T>>(() =>
                queryOver.Skip(skip).Take(pageSize).Future().GetEnumerable().ToList());
        }

        /// <summary>
        /// Get all value.
        /// </summary>
        public IEnumerable<T> AllValues => _mLazyNhQueryMembers.Value;
    }
}
