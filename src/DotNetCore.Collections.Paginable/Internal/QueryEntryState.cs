using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetCore.Collections.Paginable.Internal
{
    internal class QueryEntryState<T>
    {
        public IList<T> QueryableMembersPinedCache { get; private set; } = null;
        private readonly IQueryable<T> m_localQueryableCache; //raw-full-queryable-result from your linq data source.
        private readonly int m_skip;
        private readonly int m_take;

        private readonly object m_pinLockObj = new object();

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

            m_localQueryableCache = queryable ?? throw new ArgumentNullException(nameof(queryable), $"{nameof(queryable)} can not be null");
            m_skip = skip;
            m_take = take;
        }

        public bool HasPined => QueryableMembersPinedCache != null;

        public void Pin()
        {
            if (QueryableMembersPinedCache == null)
            {
                lock (m_pinLockObj)
                {
                    if (QueryableMembersPinedCache == null)
                    {
                        QueryableMembersPinedCache = m_localQueryableCache.Skip(m_skip).Take(m_take).ToList();
                    }
                }
            }
        }

        public IList<T> SolidMembers()
        {
            if (QueryableMembersPinedCache == null)
            {
                Pin();
            }

            return QueryableMembersPinedCache;
        }

        internal IQueryable<T> ExporyQueryableCache() => m_localQueryableCache;
    }
}
