namespace DotNetCore.Collections.Paginable
{
    public class PageMetadata
    {
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

        public int TotalPageCount { get; }

        public int RealPageCount { get; }

        public int TotalMemberCount { get; }

        public int CurrentPageNumber { get; }

        public int PageSize { get; }

        public int CurrentPageSize { get; }

        public bool HasPrevious { get; }

        public bool HasNext { get; }

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
