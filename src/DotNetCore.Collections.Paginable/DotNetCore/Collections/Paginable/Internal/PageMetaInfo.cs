using System;

namespace DotNetCore.Collections.Paginable.Internal
{
    /// <summary>
    /// The derived half of a page's metadata.
    /// <para>
    /// Single source of truth for the derived page numbers. Both
    /// <see cref="PageBase{T}.InitializeMetaInfo"/> (used by every page instance) and
    /// <see cref="PageMetadata"/> (used when metadata has to be produced without a page)
    /// go through <see cref="Calculate"/>, so the two can not drift apart.
    /// </para>
    /// </summary>
    internal readonly struct PageMetaInfo
    {
        private PageMetaInfo(int totalPageCount, int currentPageSize, bool hasPrevious, bool hasNext)
        {
            TotalPageCount = totalPageCount;
            CurrentPageSize = currentPageSize;
            HasPrevious = hasPrevious;
            HasNext = hasNext;
        }

        /// <summary>
        /// Gets the reported total page count (never less than one, even for an empty source).
        /// </summary>
        public int TotalPageCount { get; }

        /// <summary>
        /// Gets the member count of the current page.
        /// </summary>
        public int CurrentPageSize { get; }

        /// <summary>
        /// Gets a value indicating whether a previous page exists.
        /// </summary>
        public bool HasPrevious { get; }

        /// <summary>
        /// Gets a value indicating whether a next page exists.
        /// </summary>
        public bool HasNext { get; }

        /// <summary>
        /// Calculate the derived metadata of a page.
        /// </summary>
        /// <param name="currentPageNumber">current page number</param>
        /// <param name="pageSize">page size</param>
        /// <param name="totalMemberCount">total member count</param>
        /// <param name="skip">skip count, i.e. <c>(currentPageNumber - 1) * pageSize</c></param>
        /// <returns></returns>
        public static PageMetaInfo Calculate(int currentPageNumber, int pageSize, int totalMemberCount, int skip)
        {
            // rawPageCount deliberately keeps the pre-substitution value: it drives both the
            // reported page count and the "is this the last page?" question, and an empty
            // source (rawPageCount == 0) must report a current page size of zero rather than
            // a full page.
            var rawPageCount = (int) Math.Ceiling((double) totalMemberCount / (double) pageSize);
            rawPageCount = rawPageCount < 0 ? 0 : rawPageCount;

            var totalPageCount = rawPageCount == 0 ? 1 : rawPageCount;

            // Items on the last page = total - skip, clamped into [0, pageSize].
            // (The previous t % skip formula yielded 0 whenever the total was evenly
            // divisible by the page size, wrongly emptying the last page.)
            var currentPageSize = rawPageCount == 0
                ? 0
                : currentPageNumber == rawPageCount
                    ? Math.Min(Math.Max(totalMemberCount - skip, 0), pageSize)
                    : pageSize;

            return new PageMetaInfo(
                totalPageCount,
                currentPageSize,
                currentPageNumber > 1,
                currentPageNumber < totalPageCount);
        }
    }
}
