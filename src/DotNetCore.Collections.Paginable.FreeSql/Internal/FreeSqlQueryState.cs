using System;
using System.Collections.Generic;
using System.Text;
using DotNetCore.Collections.Paginable.Abstractions;
using FreeSql;

namespace DotNetCore.Collections.Paginable.Internal
{
    /// <summary>
    /// FreeSql query state
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FreeSqlQueryState<T> : IQueryEntryState<T> where T : class
    {
        private readonly Lazy<IEnumerable<T>> _mLazyFreeSqlQueryMembers;

        /// <summary>
        /// FreeSql query state
        /// </summary>
        /// <param name="select"></param>
        /// <param name="currentPageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="includeNestedMembers"></param>
        public FreeSqlQueryState(ISelect<T> select, int currentPageNumber, int pageSize, bool includeNestedMembers)
        {
            if (select == null)
            {
                throw new ArgumentNullException(nameof(select));
            }

            if (currentPageNumber < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentPageNumber), $"{nameof(currentPageNumber)} can not be less than zero");
            }

            if (pageSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than zero");
            }

            _mLazyFreeSqlQueryMembers = new Lazy<IEnumerable<T>>(() => select.Page(currentPageNumber, pageSize).ToList(includeNestedMembers));
        }

        /// <summary>
        /// Get all value.
        /// </summary>
        public IEnumerable<T> AllValue => _mLazyFreeSqlQueryMembers.Value;
    }
}
