using System;
using System.Collections.Generic;
using DotNetCore.Collections.Paginable.Abstractions;
using SqlKata;
using SqlKata.Execution;

namespace DotNetCore.Collections.Paginable.Internal {
    /// <summary>
    /// SqlKata query state
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class SqlKataQueryState<T> : IQueryEntryState<T> {
        private readonly Lazy<IEnumerable<T>> _mLazySqlKataQueryMembers;

        /// <summary>
        /// SqlKata query state
        /// </summary>
        /// <param name="query"></param>
        /// <param name="currentPageNumber"></param>
        /// <param name="pageSize"></param>
        public SqlKataQueryState(Query query, int currentPageNumber, int pageSize) {
            if (query is null) {
                throw new ArgumentNullException(nameof(query));
            }

            if (currentPageNumber < 0) {
                throw new ArgumentOutOfRangeException(nameof(currentPageNumber), $"{nameof(currentPageNumber)} can not be less than zero");
            }

            if (pageSize < 0) {
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than zero");
            }

            _mLazySqlKataQueryMembers = new Lazy<IEnumerable<T>>(() => query.ForPage(currentPageNumber, pageSize).Get<T>());
        }

        /// <summary>
        /// Get all value.
        /// </summary>
        public IEnumerable<T> AllValues => _mLazySqlKataQueryMembers.Value;
    }
}