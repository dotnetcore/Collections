namespace DotNetCore.Collections.Paginable
{
    public class PageMetadata
    {
        public PageMetadata(IPage page)
        {
            TotalPageCount = page.TotalPageCount;
            TotalMemberCount = page.TotalMemberCount;
            CurrentPageNumber = page.CurrentPageNumber;
            PageSize = page.PageSize;
            CurrentPageSize = page.CurrentPageSize;
            HasPrevious = page.HasPrevious;
            HasNext = page.HasNext;
        }

        public int TotalPageCount { get; }

        public int TotalMemberCount { get; }

        public int CurrentPageNumber { get; }

        public int PageSize { get; }

        public int CurrentPageSize { get; }

        public bool HasPrevious { get; }

        public bool HasNext { get; }
    }
}
