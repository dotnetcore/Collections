using System.Collections;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable {
    public interface IPage : IEnumerable {
        int TotalPageCount { get; }
        int TotalMemberCount { get; }
        int CurrentPageNumber { get; }
        int PageSize { get; }
        int CurrentPageSize { get; }
        bool HasPrevious { get; }
        bool HasNext { get; }
        PageMetadata GetMetadata();
    }
}