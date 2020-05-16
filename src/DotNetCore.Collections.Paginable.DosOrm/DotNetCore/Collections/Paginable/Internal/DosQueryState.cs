using System;
using System.Collections.Generic;
using Dos.ORM;
using DotNetCore.Collections.Paginable.Abstractions;

namespace DotNetCore.Collections.Paginable.Internal {
    /// <summary>
    /// Dos.ORM query state
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DosQueryState<T> : IQueryEntryState<T> where T : Entity {
        private readonly Lazy<IEnumerable<T>> _mLazyDosQueryMembers;

        /// <summary>
        /// Dos.ORM query state
        /// </summary>
        /// <param name="query"></param>
        /// <param name="currentPageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="additionalQueryFunc"></param>
        public DosQueryState(FromSection<T> query, int currentPageNumber, int pageSize, Func<FromSection<T>, FromSection<T>> additionalQueryFunc = null) {
            if (query is null) {
                throw new ArgumentNullException(nameof(query));
            }

            if (currentPageNumber < 0) {
                throw new ArgumentOutOfRangeException(nameof(currentPageNumber), $"{nameof(currentPageNumber)} can not be less than zero");
            }

            if (pageSize < 0) {
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than zero");
            }
            
            _mLazyDosQueryMembers = new Lazy<IEnumerable<T>>(() => (additionalQueryFunc?.Invoke(query) ?? query).Page(pageSize, currentPageNumber).ToList());
        }

        /// <summary>
        /// Get all value.
        /// </summary>
        public IEnumerable<T> AllValues => _mLazyDosQueryMembers.Value;
    }
}