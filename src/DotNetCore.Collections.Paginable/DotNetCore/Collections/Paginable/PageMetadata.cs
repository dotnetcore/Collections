using DotNetCore.Collections.Paginable.Internal;

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Page metadata
    /// </summary>
    public class PageMetadata
    {
        /// <summary>
        /// Create a new instance of <see cref="PageMetadata"/>.
        /// </summary>
        /// <param name="page"></param>
        public PageMetadata(IPage page)
        {
            TotalPageCount = page.TotalPageCount;
            RealPageCount = page.TotalMemberCount == 0 ? 0 : page.TotalPageCount;
            TotalMemberCount = page.TotalMemberCount;
            PageSize = page.PageSize;

            CurrentPageNumber = page.CurrentPageNumber;
            CurrentPageSize = page.CurrentPageSize;

            HasPrevious = page.HasPrevious;
            HasNext = page.HasNext;
        }

        /// <summary>
        /// Create a new instance of <see cref="PageMetadata"/> from explicit paging metadata,
        /// without an <see cref="IPage"/> instance to copy from. Used when a caller supplies the
        /// metadata itself (see <see cref="PageFragmentInfo"/>).
        /// </summary>
        /// <param name="currentPageNumber">current page number</param>
        /// <param name="pageSize">page size</param>
        /// <param name="totalMemberCount">total member count</param>
        internal PageMetadata(int currentPageNumber, int pageSize, int totalMemberCount)
        {
            var skip = (currentPageNumber - 1) * pageSize;
            var info = PageMetaInfo.Calculate(currentPageNumber, pageSize, totalMemberCount, skip);

            TotalPageCount = info.TotalPageCount;
            RealPageCount = totalMemberCount == 0 ? 0 : info.TotalPageCount;
            TotalMemberCount = totalMemberCount;
            PageSize = pageSize;

            CurrentPageNumber = currentPageNumber;
            CurrentPageSize = info.CurrentPageSize;

            HasPrevious = info.HasPrevious;
            HasNext = info.HasNext;
        }

        /// <summary>
        /// Gets total page count
        /// </summary>
        public int TotalPageCount { get; }

        /// <summary>
        /// Gets real page count
        /// </summary>
        public int RealPageCount { get; }

        /// <summary>
        /// Gets total member count
        /// </summary>
        public int TotalMemberCount { get; }

        /// <summary>
        /// Gets current page number
        /// </summary>
        public int CurrentPageNumber { get; }

        /// <summary>
        /// Gets page size
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// Gets current page size
        /// </summary>
        public int CurrentPageSize { get; }

        /// <summary>
        /// Has previous. If this page is the first page, then returns false.
        /// </summary>
        public bool HasPrevious { get; }

        /// <summary>
        /// Has next. If this page is the last page, then returns false.
        /// </summary>
        public bool HasNext { get; }

        /// <inheritdoc />
        /// <example>
        /// <code>
        /// string text = page.ToString();
        /// </code>
        /// </example>
        public override string ToString()
        {
            return $@"
=====SUMMARY=====
TotalPageCount = {TotalPageCount}
RealPageCount = {RealPageCount}
TotalMemberCount = {TotalMemberCount}
PageSize = {PageSize}

=====CURRENT=====
CurrentPageNumber = {CurrentPageNumber}
CurrentPageSize = {CurrentPageSize}

=====NAVIGATOR=====
HasPrevious = {(HasPrevious ? "Yes" : "No")}
HasNext = {(HasNext ? "Yes" : "No")}
";
        }
    }
}