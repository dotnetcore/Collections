using System;

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Paging metadata supplied by the caller, for rebuilding a page from an already-sliced
    /// fragment. See <see cref="Paginable.CreatePage{T}(System.Collections.Generic.IEnumerable{T}, PageFragmentInfo)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every value is validated on construction, so an instance can only ever describe a page
    /// that actually exists. The fragment a <see cref="PageFragmentInfo"/> is paired with is
    /// validated separately, because a metadata object can not see it.
    /// </para>
    /// <para>
    /// Use this type when the metadata arrives on its own - typically from an upstream service
    /// that answers with <c>items</c> plus <c>totalCount</c>. When a page object already exists,
    /// prefer <see cref="FromMetadata"/> over picking the three numbers apart by hand.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var info = new PageFragmentInfo(pageNumber: 3, pageSize: 5, totalMemberCount: 12);
    /// IPage&lt;Order&gt; page = Paginable.CreatePage(items, info);
    ///
    /// // Or rebuild the info from a page that already exists (round trip):
    /// var same = PageFragmentInfo.FromMetadata(existingPage.GetMetadata());
    /// </code>
    /// </example>
    public sealed class PageFragmentInfo
    {
        /// <summary>
        /// Create a new instance of <see cref="PageFragmentInfo"/>.
        /// </summary>
        /// <param name="pageNumber">page number of the fragment, starting at one</param>
        /// <param name="pageSize">page size</param>
        /// <param name="totalMemberCount">total member count of the whole source, not of the fragment</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="pageNumber"/> is less than one;
        /// <paramref name="pageSize"/> is less than one;
        /// <paramref name="totalMemberCount"/> is negative, exceeds
        /// <see cref="PaginableSettingsManager.Settings"/>'s <c>MaxMemberItems</c>,
        /// or <paramref name="pageNumber"/> points past the last page.
        /// </exception>
        public PageFragmentInfo(int pageNumber, int pageSize, int totalMemberCount)
        {
            if (pageNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageNumber), $"{nameof(pageNumber)} can not be less than one");
            }

            if (pageSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than one");
            }

            if (totalMemberCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalMemberCount), $"{nameof(totalMemberCount)} can not be less than zero");
            }

            if (totalMemberCount > PaginableSettingsManager.Settings.MaxMemberItems)
            {
                throw new ArgumentOutOfRangeException(nameof(totalMemberCount), "Paginable does not support large size result");
            }

            // Same out-of-range rule the existing GetPage entry points use: skip must land
            // before the end of the source. Computed in long so that an extreme pageSize can
            // not overflow the check and let a non-existent page through.
            var skip = ((long) pageNumber - 1) * pageSize;
            if (totalMemberCount > 0 && skip >= totalMemberCount)
            {
                throw new ArgumentOutOfRangeException(nameof(pageNumber), $"{nameof(pageNumber)} can not be greater than pages count.");
            }

            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalMemberCount = totalMemberCount;
        }

        /// <summary>
        /// Gets the page number of the fragment, starting at one.
        /// </summary>
        public int PageNumber { get; }

        /// <summary>
        /// Gets the page size.
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// Gets the total member count of the whole source (not of the fragment).
        /// </summary>
        public int TotalMemberCount { get; }

        /// <summary>
        /// Rebuild fragment metadata from an existing page's metadata snapshot.
        /// </summary>
        /// <param name="metadata">metadata snapshot of an existing page</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="metadata"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="metadata"/> was taken from an empty page and carries no usable page size.
        /// </exception>
        public static PageFragmentInfo FromMetadata(PageMetadata metadata)
        {
            if (metadata is null)
            {
                throw new ArgumentNullException(nameof(metadata), $"{nameof(metadata)} can not be null.");
            }

            return new PageFragmentInfo(metadata.CurrentPageNumber, metadata.PageSize, metadata.TotalMemberCount);
        }

        /// <summary>
        /// Project this fragment info onto a <see cref="PageMetadata"/> snapshot, following the
        /// same derivation every page instance uses. Useful for asserting that a rebuilt page
        /// matches the page it was rebuilt from, and for handing the metadata to a serializer.
        /// </summary>
        /// <returns></returns>
        public PageMetadata ToMetadata() => new PageMetadata(PageNumber, PageSize, TotalMemberCount);
    }
}
