using System;
using System.Collections.Generic;
using Chloe;
using DotNetCore.Collections.Paginable.Abstractions;

namespace DotNetCore.Collections.Paginable.Internal {
    /// <summary>
    /// Chloe query state
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ChloeQueryState<T> : IQueryEntryState<T> {
        private readonly Lazy<IEnumerable<T>> _mLazyChloeQueryMembers;

        /// <summary>
        /// Chloe query state
        /// </summary>
        /// <param name="query"></param>
        /// <param name="currentPageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="additionalQueryFunc"></param>
        public ChloeQueryState(IQuery<T> query, int currentPageNumber, int pageSize, Func<IQuery<T>, IQuery<T>> additionalQueryFunc = null) {
            if (query is null) {
                throw new ArgumentNullException(nameof(query));
            }

            if (currentPageNumber < 0) {
                throw new ArgumentOutOfRangeException(nameof(currentPageNumber), $"{nameof(currentPageNumber)} can not be less than zero");
            }

            if (pageSize < 0) {
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than zero");
            }

            InternalChloeQuery = query.TakePage(currentPageNumber, pageSize);
            _mLazyChloeQueryMembers = new Lazy<IEnumerable<T>>(() => (additionalQueryFunc?.Invoke(InternalChloeQuery) ?? InternalChloeQuery).ToList());
        }

        private IQuery<T> InternalChloeQuery { get; set; }

        /// <summary>
        /// Get all value.
        /// </summary>
        public IEnumerable<T> AllValues => _mLazyChloeQueryMembers.Value;
    }
}