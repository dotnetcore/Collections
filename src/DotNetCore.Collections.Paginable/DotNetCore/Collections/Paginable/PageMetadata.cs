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