using System;
using System.Collections;
using System.Collections.Generic;

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// A single-page <see cref="IPaginable{T}"/> wrapper around one page that already exists.
    /// Built by <see cref="Paginable.CreateSinglePageSet{T}(System.Collections.Generic.IEnumerable{T},PageFragmentInfo)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exists for callers whose signature wants an <see cref="IPaginable{T}"/> - or an
    /// <c>IEnumerable&lt;IPage&lt;T&gt;&gt;</c> - while the data in hand is a single page that was
    /// assembled elsewhere (a cached page, a gRPC response carrying <c>items</c> plus
    /// <c>totalCount</c>). Wrapping it here avoids either inventing the remaining pages or
    /// re-fetching the source just to satisfy the shape.
    /// </para>
    /// <para>
    /// <see cref="PageCount"/> is <b>always one</b>, and that is a deliberate divergence from the
    /// wrapped page's own <see cref="IPage.TotalPageCount"/>. A set must be able to serve every
    /// page it claims, and this one physically holds a single page: it has no source to slice the
    /// others out of. So the <b>set</b> layer answers "how many pages am I handing you" (one),
    /// while the <b>page</b> layer keeps the source-wide numbering - <see cref="IPage.CurrentPageNumber"/>
    /// and <see cref="IPage.TotalPageCount"/> are untouched, so a fragment of page 3 of 12 still
    /// reports itself as page 3 of 12.
    /// </para>
    /// <para>
    /// <see cref="MemberCount"/> follows the same rule as <see cref="PaginableSetBase{T}"/>: it is
    /// the member count of the whole <b>source</b>, not of the pages this set happens to hold, so
    /// it matches the wrapped page's <see cref="IPage.TotalMemberCount"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var items = connection.Query&lt;Order&gt;(sql, new { offset = 10, fetch = 5 });   // page 3 of 12
    /// var total = connection.ExecuteScalar&lt;int&gt;(countSql);                       // 12
    ///
    /// PaginableSinglePage&lt;Order&gt; set = Paginable.CreateSinglePageSet(
    ///     items, pageNumber: 3, pageSize: 5, totalMemberCount: total);
    ///
    /// IPaginable&lt;Order&gt; asInterface = set;              // it is an IPaginable&lt;Order&gt; too
    /// set.PageCount;                  // 1  (the set holds exactly the page you handed it)
    /// set.MemberCount;                // 12 (source-wide, as the page reports it)
    /// set.GetPage(1).CurrentPageNumber; // 3  (the page keeps its global number)
    /// set.GetPage(2);                 // ArgumentOutOfRangeException - the set has one page
    /// </code>
    /// </example>
    public sealed class PaginableSinglePage<T> : IPaginable<T>
    {
        private readonly IPage<T> _page;

        /// <summary>
        /// Create a new instance of <see cref="PaginableSinglePage{T}"/> around an existing page.
        /// </summary>
        /// <param name="page">the page this set is to hold; it is served as-is, never re-built</param>
        /// <exception cref="ArgumentNullException"><paramref name="page"/> is <c>null</c>.</exception>
        internal PaginableSinglePage(IPage<T> page)
        {
            _page = page ?? throw new ArgumentNullException(nameof(page), $"{nameof(page)} can not be null.");
        }

        /// <inheritdoc />
        public int PageSize => _page.PageSize;

        /// <inheritdoc />
        public int MemberCount => _page.TotalMemberCount;

        /// <summary>
        /// Gets the page count, which is always one: the set holds exactly the page it was given.
        /// </summary>
        /// <remarks>
        /// Not the wrapped page's <see cref="IPage.TotalPageCount"/> - see the type remarks. Use
        /// <see cref="GetPage"/> with one to reach the page itself and read the source-wide numbers
        /// from its metadata.
        /// </remarks>
        public int PageCount => 1;

        /// <inheritdoc />
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="pageNumber"/> is not one - the set has exactly one page.
        /// </exception>
        /// <example>
        /// <code>
        /// IPage&lt;Order&gt; page = set.GetPage(1);
        /// </code>
        /// </example>
        public IPage<T> GetPage(int pageNumber)
        {
            if (pageNumber != 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageNumber), $"{nameof(pageNumber)} can not be less than 1 or greater than pages count.");
            }

            return _page;
        }

        /// <inheritdoc />
        /// <example>
        /// <code>
        /// foreach (var page in set)
        /// {
        ///     // runs exactly once
        /// }
        /// </code>
        /// </example>
        public IEnumerator<IPage<T>> GetEnumerator()
        {
            yield return _page;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
