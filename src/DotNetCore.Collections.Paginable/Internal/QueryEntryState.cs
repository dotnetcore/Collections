using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetCore.Collections.Paginable.Internal
{
    /// <summary>
    /// Query entry state
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class QueryEntryState<T>
    {
        private readonly Lazy<IEnumerable<T>> _mLazyQueryableMembers;

        /// <summary>
        /// Query entry state
        /// </summary>
        /// <param name="queryable">Orgin queryable result</param>
        /// <param name="skip">skip number</param>
        /// <param name="take">take number</param>
        public QueryEntryState(IQueryable<T> queryable, int skip, int take)
        {
            if (skip < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(skip), $"{nameof(skip)} can not be less than zero");
            }

            if (take < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(take), $"{nameof(take)} can not be less than zero");
            }

            _mLazyQueryableMembers = new Lazy<IEnumerable<T>>(() => queryable.Skip(skip).Take(take).AsEnumerable());
        }

        /// <summary>
        /// Get all value.
        /// </summary>
        public IEnumerable<T> AllValue => _mLazyQueryableMembers.Value;
    }
}
