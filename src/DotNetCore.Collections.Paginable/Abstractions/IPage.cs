using System.Collections;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable
{
    public interface IPage : IEnumerable
    {
        int TotalPagesCount { get; }

        int TotalMembersCount { get; }

        int CurrentPageNumber { get; }

        int PageSize { get; }

        int CurrentPageSize { get; }

        bool HasPrevious { get; }

        bool HasNext { get; }
    }
}
