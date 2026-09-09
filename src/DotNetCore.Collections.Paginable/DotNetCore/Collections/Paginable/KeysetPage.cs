using System;
using System.Collections.Generic;

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Result of a keyset (seek) pagination request.
    /// <para>
    /// Unlike offset-based <see cref="IPage{T}"/>, a keyset page does NOT carry total
    /// member/page counts - that is the whole point: no <c>COUNT(*)</c> round trip and
    /// no <c>OFFSET</c> scan, so deep pages stay as cheap as shallow ones. The caller
    /// chains requests by feeding <see cref="LastMember"/> (of the ordering key) into
    /// the next <c>GetPageByKeyset</c> call.
    /// </para>
    /// </summary>
    /// <typeparam name="T">element type</typeparam>
    public sealed class KeysetPage<T>
    {
        /// <summary>
        /// Members of the current page (at most <c>pageSize</c> items).
        /// </summary>
        public IReadOnlyList<T> Members { get; }

        /// <summary>
        /// Requested page size.
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// Actual member count of this page (<c>0</c> - <c>pageSize</c>).
        /// </summary>
        public int CurrentPageSize => Members.Count;

        /// <summary>
        /// Whether more members exist AFTER this page in the ordering direction.
        /// </summary>
        public bool HasNext { get; }

        /// <summary>
        /// Whether this is the first page of the keyset traversal (no lower/upper bound
        /// was applied, i.e. no "last key" was given).
        /// </summary>
        public bool IsFirstPage { get; }

        /// <summary>
        /// Gets the last member of this page; feed it back as the keyset anchor of the
        /// next request. Returns <c>default</c> when the page is empty.
        /// </summary>
        public T LastMember => Members.Count > 0 ? Members[Members.Count - 1] : default;

        /// <summary>
        /// Create a keyset page result.
        /// </summary>
        /// <param name="members">materialized members of this page</param>
        /// <param name="pageSize">requested page size</param>
        /// <param name="hasNext">whether more members exist after this page</param>
        /// <param name="isFirstPage">whether no keyset anchor was applied</param>
        public KeysetPage(IReadOnlyList<T> members, int pageSize, bool hasNext, bool isFirstPage)
        {
            Members = members ?? throw new ArgumentNullException(nameof(members));
            PageSize = pageSize;
            HasNext = hasNext;
            IsFirstPage = isFirstPage;
        }
    }
}
