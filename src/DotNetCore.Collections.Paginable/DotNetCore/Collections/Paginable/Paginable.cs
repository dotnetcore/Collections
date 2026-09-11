using System;
using System.Collections.Generic;

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Creates paging objects directly from materialized fragments plus paging metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this entry point when the data of one page is already in hand - a hand-written
    /// Dapper/ADO.NET query with <c>OFFSET</c>/<c>FETCH</c>, a cached page, a gRPC response, or
    /// an upstream API that answers with <c>items</c> plus <c>totalCount</c>. The fragment is
    /// <b>never re-sliced</b>: it is taken to be the exact content of the page it is paired with.
    /// </para>
    /// <para>
    /// This is the opposite input shape of <c>IEnumerable&lt;T&gt;.GetPage(pageNumber, pageSize)</c>,
    /// which takes the <b>whole</b> source and slices the requested page out of it. The two are
    /// deliberately kept apart - a differently named extension (<c>ToPage</c>) rather than an
    /// overload of <c>GetPage</c> - because feeding a pre-sliced fragment to <c>GetPage</c> would
    /// silently skip into it a second time.
    /// </para>
    /// <para>
    /// The result is an ordinary <see cref="IPage{T}"/> built by <see cref="EnumerablePage{T}"/>,
    /// so <see cref="IPage.GetMetadata"/>, <c>IsFirst()</c>/<c>IsLast()</c>,
    /// <c>FromMemberNumber()</c>/<c>ToMemberNumber()</c> and <c>IPageMember&lt;T&gt;.ItemNumber</c>
    /// all behave exactly as they do for a page sliced out of a full source - including the
    /// global member numbering.
    /// </para>
    /// <para>
    /// When the caller's signature wants an <see cref="IPaginable{T}"/> (or an
    /// <c>IEnumerable&lt;IPage&lt;T&gt;&gt;</c>) rather than a single page,
    /// <see cref="CreateSinglePageSet{T}(System.Collections.Generic.IEnumerable{T},PageFragmentInfo)"/>
    /// wraps the same page in a <see cref="PaginableSinglePage{T}"/> whose
    /// <see cref="PaginableSinglePage{T}.PageCount"/> is one.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // items already holds one page (5 rows), total comes from COUNT(*) or the upstream API
    /// var items = connection.Query&lt;Order&gt;(sql, new { offset = 10, fetch = 5 });
    /// var total = connection.ExecuteScalar&lt;int&gt;(countSql);            // 12
    ///
    /// IPage&lt;Order&gt; page = Paginable.CreatePage(items, pageNumber: 3, pageSize: 5, totalMemberCount: total);
    ///
    /// page.TotalPageCount;    // 3
    /// page.CurrentPageSize;   // 2  (a short last page)
    /// page.HasNext;           // false
    /// page[0].ItemNumber;     // 11 (the same global row number full-source paging would give)
    /// </code>
    /// </example>
    public static class Paginable
    {
        /// <summary>
        /// Create a page from an already-sliced fragment plus paging metadata.
        /// </summary>
        /// <typeparam name="T">element type of the fragment</typeparam>
        /// <param name="fragment">the fragment, taken to be the exact content of the requested page</param>
        /// <param name="pageNumber">page number of the fragment, starting at one</param>
        /// <param name="pageSize">page size</param>
        /// <param name="totalMemberCount">total member count of the whole source, not of the fragment</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="fragment"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="pageNumber"/> is less than one; <paramref name="pageSize"/> is less than
        /// one; <paramref name="totalMemberCount"/> is negative or exceeds the configured
        /// <c>MaxMemberItems</c>; or <paramref name="pageNumber"/> points past the last page.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="fragment"/> carries more members than <paramref name="pageSize"/>, or more
        /// than the metadata says the page holds.
        /// </exception>
        /// <example>
        /// <code>
        /// IPage&lt;Order&gt; page = Paginable.CreatePage(items, pageNumber: 3, pageSize: 5, totalMemberCount: 12);
        /// </code>
        /// </example>
        public static IPage<T> CreatePage<T>(IEnumerable<T> fragment, int pageNumber, int pageSize, int totalMemberCount)
        {
            if (fragment is null)
            {
                throw new ArgumentNullException(nameof(fragment), $"{nameof(fragment)} can not be null.");
            }

            return CreatePage(fragment, new PageFragmentInfo(pageNumber, pageSize, totalMemberCount));
        }

        /// <summary>
        /// Create a page from an already-sliced fragment plus a caller-supplied
        /// <see cref="PageFragmentInfo"/>.
        /// </summary>
        /// <typeparam name="T">element type of the fragment</typeparam>
        /// <param name="fragment">the fragment, taken to be the exact content of the requested page</param>
        /// <param name="metadata">paging metadata of the fragment; validated when it was constructed</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="fragment"/> or <paramref name="metadata"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="fragment"/> carries more members than the page size, or more than the
        /// metadata says the page holds. A fragment that is <b>shorter</b> than the metadata says is
        /// tolerated (a concurrent delete upstream must not make the page unbuildable);
        /// <see cref="IPage.CurrentPageSize"/> keeps reporting the metadata value in that case.
        /// </exception>
        /// <example>
        /// <code>
        /// var info = new PageFragmentInfo(pageNumber: 3, pageSize: 5, totalMemberCount: 12);
        /// IPage&lt;Order&gt; page = Paginable.CreatePage(items, info);
        /// </code>
        /// </example>
        public static IPage<T> CreatePage<T>(IEnumerable<T> fragment, PageFragmentInfo metadata)
        {
            return CreatePage(fragment, metadata, PageCreationOptions.Lenient);
        }

        /// <summary>
        /// Create a page from an already-sliced fragment plus a caller-supplied
        /// <see cref="PageFragmentInfo"/>, selecting the fragment checking strictness.
        /// </summary>
        /// <typeparam name="T">element type of the fragment</typeparam>
        /// <param name="fragment">the fragment, taken to be the exact content of the requested page</param>
        /// <param name="metadata">paging metadata of the fragment; validated when it was constructed</param>
        /// <param name="options"><see cref="PageCreationOptions.Lenient"/> (the 6.1 behaviour) or
        /// <see cref="PageCreationOptions.Strict"/></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="fragment"/>, <paramref name="metadata"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="fragment"/> carries more members than the page size, or more than the
        /// metadata says the page holds. In <see cref="PageCreationOptions.Strict"/> mode, a
        /// fragment that is <b>shorter</b> than the metadata says throws as well - under
        /// <see cref="PageCreationOptions.Lenient"/> (the 6.1 default) it is tolerated and
        /// <see cref="IPage.CurrentPageSize"/> keeps reporting the metadata value.
        /// </exception>
        /// <example>
        /// <code>
        /// var info = new PageFragmentInfo(pageNumber: 3, pageSize: 5, totalMemberCount: 12);
        /// IPage&lt;Order&gt; page = Paginable.CreatePage(items, info, PageCreationOptions.Strict);
        /// </code>
        /// </example>
        public static IPage<T> CreatePage<T>(IEnumerable<T> fragment, PageFragmentInfo metadata, PageCreationOptions options)
        {
            if (fragment is null)
            {
                throw new ArgumentNullException(nameof(fragment), $"{nameof(fragment)} can not be null.");
            }

            if (metadata is null)
            {
                throw new ArgumentNullException(nameof(metadata), $"{nameof(metadata)} can not be null.");
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options), $"{nameof(options)} can not be null.");
            }

            // Computed in long: PageFragmentInfo has already range-checked the same expression,
            // so the two must not disagree because of an int overflow.
            var skip = ((long) metadata.PageNumber - 1) * metadata.PageSize;
            var expected = (int) Math.Min(Math.Max(metadata.TotalMemberCount - skip, 0L), metadata.PageSize);

            // A single pass over the fragment, stopped at pageSize + 1 members: the extra member is
            // only ever read to prove that the fragment is longer than the page it claims to be.
            // Materializing here defends the page against the caller mutating its collection later.
            var materialized = new List<T>();
            foreach (var item in fragment)
            {
                materialized.Add(item);
                if (materialized.Count > metadata.PageSize)
                {
                    throw new ArgumentException(
                        $"Fragment carries more than {metadata.PageSize} member(s), which is the page size of page {metadata.PageNumber}.",
                        nameof(fragment));
                }
            }

            if (materialized.Count > expected)
            {
                throw new ArgumentException(
                    $"Fragment carries {materialized.Count} member(s) but page {metadata.PageNumber} (page size {metadata.PageSize}, " +
                    $"total member count {metadata.TotalMemberCount}) holds at most {expected}.",
                    nameof(fragment));
            }

            if (options.IsStrict && materialized.Count < expected)
            {
                throw new ArgumentException(
                    $"Strict mode: fragment carries {materialized.Count} member(s) but page {metadata.PageNumber} " +
                    $"(page size {metadata.PageSize}, total member count {metadata.TotalMemberCount}) holds {expected}. " +
                    "A short fragment usually means the total member count is stale or the fragment was sliced by a different query.",
                    nameof(fragment));
            }

            return new EnumerablePage<T>(materialized, metadata.PageNumber, metadata.PageSize, metadata.TotalMemberCount, sourceIsFull: false);
        }

        /// <summary>
        /// Create a page from an already-sliced fragment plus paging metadata, selecting the
        /// fragment checking strictness.
        /// </summary>
        /// <typeparam name="T">element type of the fragment</typeparam>
        /// <param name="fragment">the fragment, taken to be the exact content of the requested page</param>
        /// <param name="pageNumber">page number of the fragment, starting at one</param>
        /// <param name="pageSize">page size</param>
        /// <param name="totalMemberCount">total member count of the whole source, not of the fragment</param>
        /// <param name="options"><see cref="PageCreationOptions.Lenient"/> (the 6.1 behaviour) or
        /// <see cref="PageCreationOptions.Strict"/></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="fragment"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="pageNumber"/> is less than one; <paramref name="pageSize"/> is less than
        /// one; <paramref name="totalMemberCount"/> is negative or exceeds the configured
        /// <c>MaxMemberItems</c>; or <paramref name="pageNumber"/> points past the last page.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="fragment"/> carries more members than <paramref name="pageSize"/> or more
        /// than the metadata says the page holds; in <see cref="PageCreationOptions.Strict"/> mode
        /// also when it carries fewer than the metadata says.
        /// </exception>
        /// <example>
        /// <code>
        /// IPage&lt;Order&gt; page = Paginable.CreatePage(items, pageNumber: 3, pageSize: 5, totalMemberCount: 12, PageCreationOptions.Strict);
        /// </code>
        /// </example>
        public static IPage<T> CreatePage<T>(IEnumerable<T> fragment, int pageNumber, int pageSize, int totalMemberCount, PageCreationOptions options)
        {
            if (fragment is null)
            {
                throw new ArgumentNullException(nameof(fragment), $"{nameof(fragment)} can not be null.");
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options), $"{nameof(options)} can not be null.");
            }

            return CreatePage(fragment, new PageFragmentInfo(pageNumber, pageSize, totalMemberCount), options);
        }

        /// <summary>
        /// Create an empty page, i.e. a page whose source has no members at all.
        /// </summary>
        /// <typeparam name="T">element type</typeparam>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// IPage&lt;Order&gt; page = Paginable.CreateEmptyPage&lt;Order&gt;();
        /// page.TotalPageCount;   // 1
        /// page.CurrentPageSize;  // 0
        /// </code>
        /// </example>
        public static IPage<T> CreateEmptyPage<T>() => new EmptyPage<T>();

        /// <summary>
        /// Wrap an already-sliced fragment in a single-page <see cref="IPaginable{T}"/>.
        /// </summary>
        /// <typeparam name="T">element type of the fragment</typeparam>
        /// <param name="fragment">the fragment, taken to be the exact content of the requested page</param>
        /// <param name="metadata">paging metadata of the fragment; validated when it was constructed</param>
        /// <returns>a set holding exactly the one page the fragment describes; a
        /// <see cref="PaginableSinglePage{T}"/> rather than the bare interface, so the caller can
        /// read <see cref="PaginableSinglePage{T}.PageCount"/> - <see cref="IPaginable"/> itself
        /// exposes only <see cref="IPaginable.PageSize"/> and <see cref="IPaginable.MemberCount"/>
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="fragment"/> or <paramref name="metadata"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="metadata"/> carries a page number, page size or total member count out of
        /// range; see <see cref="PageFragmentInfo"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="fragment"/> carries more members than the metadata says the page holds.
        /// </exception>
        /// <example>
        /// <code>
        /// var info = new PageFragmentInfo(pageNumber: 3, pageSize: 5, totalMemberCount: 12);
        /// PaginableSinglePage&lt;Order&gt; set = Paginable.CreateSinglePageSet(items, info);
        ///
        /// set.PageCount;   // 1
        /// </code>
        /// </example>
        public static PaginableSinglePage<T> CreateSinglePageSet<T>(IEnumerable<T> fragment, PageFragmentInfo metadata)
        {
            return CreateSinglePageSet(fragment, metadata, PageCreationOptions.Lenient);
        }

        /// <summary>
        /// Wrap an already-sliced fragment in a single-page <see cref="IPaginable{T}"/>, selecting
        /// the fragment checking strictness.
        /// </summary>
        /// <typeparam name="T">element type of the fragment</typeparam>
        /// <param name="fragment">the fragment, taken to be the exact content of the requested page</param>
        /// <param name="metadata">paging metadata of the fragment; validated when it was constructed</param>
        /// <param name="options"><see cref="PageCreationOptions.Lenient"/> (the 6.1 behaviour) or
        /// <see cref="PageCreationOptions.Strict"/></param>
        /// <returns>a set holding exactly the one page the fragment describes; a
        /// <see cref="PaginableSinglePage{T}"/> rather than the bare interface, so the caller can
        /// read <see cref="PaginableSinglePage{T}.PageCount"/> - <see cref="IPaginable"/> itself
        /// exposes only <see cref="IPaginable.PageSize"/> and <see cref="IPaginable.MemberCount"/>
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="fragment"/>, <paramref name="metadata"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="metadata"/> carries a page number, page size or total member count out of
        /// range; see <see cref="PageFragmentInfo"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="fragment"/> carries more members than the metadata says the page holds; in
        /// <see cref="PageCreationOptions.Strict"/> mode also when it carries fewer.
        /// </exception>
        /// <example>
        /// <code>
        /// var info = new PageFragmentInfo(pageNumber: 3, pageSize: 5, totalMemberCount: 12);
        /// PaginableSinglePage&lt;Order&gt; set = Paginable.CreateSinglePageSet(items, info, PageCreationOptions.Strict);
        /// </code>
        /// </example>
        public static PaginableSinglePage<T> CreateSinglePageSet<T>(IEnumerable<T> fragment, PageFragmentInfo metadata, PageCreationOptions options)
        {
            if (fragment is null)
            {
                throw new ArgumentNullException(nameof(fragment), $"{nameof(fragment)} can not be null.");
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options), $"{nameof(options)} can not be null.");
            }

            return new PaginableSinglePage<T>(CreatePage(fragment, metadata, options));
        }

        /// <summary>
        /// Wrap an already-sliced fragment in a single-page <see cref="IPaginable{T}"/>.
        /// </summary>
        /// <typeparam name="T">element type of the fragment</typeparam>
        /// <param name="fragment">the fragment, taken to be the exact content of the requested page</param>
        /// <param name="pageNumber">page number of the fragment, starting at one</param>
        /// <param name="pageSize">page size</param>
        /// <param name="totalMemberCount">total member count of the whole source, not of the fragment</param>
        /// <returns>a set holding exactly the one page the fragment describes; a
        /// <see cref="PaginableSinglePage{T}"/> rather than the bare interface, so the caller can
        /// read <see cref="PaginableSinglePage{T}.PageCount"/> - <see cref="IPaginable"/> itself
        /// exposes only <see cref="IPaginable.PageSize"/> and <see cref="IPaginable.MemberCount"/>
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="fragment"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="pageNumber"/> is less than one; <paramref name="pageSize"/> is less than
        /// one; <paramref name="totalMemberCount"/> is negative or exceeds the configured
        /// <c>MaxMemberItems</c>; or <paramref name="pageNumber"/> points past the last page.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="fragment"/> carries more members than <paramref name="pageSize"/>, or more
        /// than the metadata says the page holds.
        /// </exception>
        /// <example>
        /// <code>
        /// PaginableSinglePage&lt;Order&gt; set = Paginable.CreateSinglePageSet(items, pageNumber: 3, pageSize: 5, totalMemberCount: 12);
        /// </code>
        /// </example>
        public static PaginableSinglePage<T> CreateSinglePageSet<T>(IEnumerable<T> fragment, int pageNumber, int pageSize, int totalMemberCount)
        {
            return CreateSinglePageSet(fragment, pageNumber, pageSize, totalMemberCount, PageCreationOptions.Lenient);
        }

        /// <summary>
        /// Wrap an already-sliced fragment in a single-page <see cref="IPaginable{T}"/>, selecting
        /// the fragment checking strictness.
        /// </summary>
        /// <typeparam name="T">element type of the fragment</typeparam>
        /// <param name="fragment">the fragment, taken to be the exact content of the requested page</param>
        /// <param name="pageNumber">page number of the fragment, starting at one</param>
        /// <param name="pageSize">page size</param>
        /// <param name="totalMemberCount">total member count of the whole source, not of the fragment</param>
        /// <param name="options"><see cref="PageCreationOptions.Lenient"/> (the 6.1 behaviour) or
        /// <see cref="PageCreationOptions.Strict"/></param>
        /// <returns>a set holding exactly the one page the fragment describes; a
        /// <see cref="PaginableSinglePage{T}"/> rather than the bare interface, so the caller can
        /// read <see cref="PaginableSinglePage{T}.PageCount"/> - <see cref="IPaginable"/> itself
        /// exposes only <see cref="IPaginable.PageSize"/> and <see cref="IPaginable.MemberCount"/>
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="fragment"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="pageNumber"/> is less than one; <paramref name="pageSize"/> is less than
        /// one; <paramref name="totalMemberCount"/> is negative or exceeds the configured
        /// <c>MaxMemberItems</c>; or <paramref name="pageNumber"/> points past the last page.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="fragment"/> carries more members than <paramref name="pageSize"/> or more
        /// than the metadata says the page holds; in <see cref="PageCreationOptions.Strict"/> mode
        /// also when it carries fewer than the metadata says.
        /// </exception>
        /// <example>
        /// <code>
        /// PaginableSinglePage&lt;Order&gt; set = Paginable.CreateSinglePageSet(
        ///     items, pageNumber: 3, pageSize: 5, totalMemberCount: 12, PageCreationOptions.Strict);
        /// </code>
        /// </example>
        public static PaginableSinglePage<T> CreateSinglePageSet<T>(IEnumerable<T> fragment, int pageNumber, int pageSize, int totalMemberCount, PageCreationOptions options)
        {
            if (fragment is null)
            {
                throw new ArgumentNullException(nameof(fragment), $"{nameof(fragment)} can not be null.");
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options), $"{nameof(options)} can not be null.");
            }

            return new PaginableSinglePage<T>(CreatePage(fragment, pageNumber, pageSize, totalMemberCount, options));
        }
    }
}
