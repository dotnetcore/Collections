using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetCore.Collections.Paginable.Internal
{
    internal class QueryEntryState<T>
    {
        // ReSharper disable once InconsistentNaming
        private readonly Lazy<IEnumerable<T>> m_lazyQueryableMembers;

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

            m_lazyQueryableMembers = new Lazy<IEnumerable<T>>(() => queryable.Skip(skip).Take(take).AsEnumerable());
        }

        public IEnumerable<T> AllValue => m_lazyQueryableMembers.Value;
    }
}
