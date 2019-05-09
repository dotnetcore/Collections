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
    public class NhCoreQueryState<T> : IQueryEntryState<T>
    {
        private readonly Lazy<IFutureEnumerable<T>> _mLazyChloeQueryMembers;

        /// <summary>
        /// NHibernate query state
        /// </summary>
        /// <param name="queryOver"></param>
        /// <param name="skip"></param>
        /// <param name="pageSize"></param>
        public NhCoreQueryState(IQueryOver<T> queryOver, int skip, int pageSize)
        {
            if (queryOver == null)
            {
                throw new ArgumentNullException(nameof(queryOver));
            }

            if (skip < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(skip), $"{nameof(skip)} can not be less than zero");
            }

            if (pageSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than zero");
            }

            _mLazyChloeQueryMembers = new Lazy<IFutureEnumerable<T>>(() => queryOver.Skip(skip).Take(pageSize).Future());
        }
        
        /// <summary>
        /// Get all value.
        /// </summary>
        public IEnumerable<T> AllValue => _mLazyChloeQueryMembers.Value.GetEnumerable();
    }
}
