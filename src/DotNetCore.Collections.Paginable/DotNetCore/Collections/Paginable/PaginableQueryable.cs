using System;
using System.Linq;

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// QueryablePage collection
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PaginableQueryable<T> : PaginableSetBase<T>
    {
        private readonly IQueryable<T> _queryable;

        /// <summary>
        /// Paginable queryable collection.
        /// Public for async provider integrations (e.g. EF Core <c>ToPaginableAsync</c>)
        /// which compute the real counts via provider-native async APIs.
        /// </summary>
        public PaginableQueryable(IQueryable<T> queryable, int pageSize, int realPageCount, int realMemberCount)
            : base(pageSize, realPageCount, realMemberCount)
        {
            _queryable = queryable;
        }

        /// <summary>
        /// Paginable queryable collection with limited members.
        /// Public for async provider integrations (e.g. EF Core <c>ToPaginableAsync</c>)
        /// which compute the real counts via provider-native async APIs.
        /// </summary>
        public PaginableQueryable(IQueryable<T> queryable, int pageSize, int realPageCount, int realMemberCount, int limitedMembersCount)
            : base(pageSize, realPageCount, realMemberCount, limitedMembersCount)
        {
            _queryable = queryable;
        }

        /// <summary>
        /// Gets the query this page set pages over.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Read-only, and it hands back the very instance the page set was built from.
        /// </para>
        /// <para>
        /// It is exposed so that a provider integration can offer a page fetch that is
        /// <b>actually</b> asynchronous over the same query: the synchronous path runs
        /// <c>Skip</c> / <c>Take</c> / <c>ToList</c> on the calling thread, so without the query a
        /// streaming face over this page set could only ever be an asynchronous <i>shape</i>. With
        /// it, the EF Core integration streams each page through <c>ToListAsync</c> and the
        /// cancellation token reaches the provider instead of stopping at the page boundary.
        /// </para>
        /// <para>
        /// The query is the page set's <i>source</i>, not its page cache: pages produced from it
        /// elsewhere are equal in content but are not the cached page instances
        /// <see cref="PaginableSetBase{T}.GetPage"/> returns.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var paginable = await query.ToPaginableAsync(50);
        /// var sameQuery = paginable.Queryable;
        /// </code>
        /// </example>
        public IQueryable<T> Queryable => _queryable;

        /// <summary>
        /// Get special page
        /// </summary>
        /// <param name="currentPageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="realMemberCount"></param>
        /// <returns></returns>
        protected override Lazy<IPage<T>> GetSpecifiedPage(int currentPageNumber, int pageSize, int realMemberCount)
        {
            return new(() => new EnumerablePage<T>(_queryable, currentPageNumber, pageSize, realMemberCount));
        }
    }
}