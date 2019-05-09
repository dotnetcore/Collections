using System;
using System.Collections.Generic;
using DotNetCore.Collections.Paginable.Abstractions;
using SqlSugar;

namespace DotNetCore.Collections.Paginable.Internal
{
    /// <summary>
    /// SqlSugar query state
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SqlSugarQueryState<T> : IQueryEntryState<T>
    {
        private readonly Lazy<IEnumerable<T>> _mLazySqlSugarQueryMembers;

        /// <summary>
        /// SqlSugar query state
        /// </summary>
        /// <param name="query"></param>
        /// <param name="currentPageNumber"></param>
        /// <param name="pageSize"></param>
        public SqlSugarQueryState(ISugarQueryable<T> query, int currentPageNumber, int pageSize)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (currentPageNumber < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentPageNumber), $"{nameof(currentPageNumber)} can not be less than zero");
            }

            if (pageSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than zero");
            }

            _mLazySqlSugarQueryMembers = new Lazy<IEnumerable<T>>(() => query.ToPageList(currentPageNumber, pageSize));
        }

        /// <summary>
        /// Get all value.
        /// </summary>
        public IEnumerable<T> AllValue => _mLazySqlSugarQueryMembers.Value;
    }
}
