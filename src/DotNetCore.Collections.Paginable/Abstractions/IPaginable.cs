using System.Collections;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable {
    public interface IPaginable : IEnumerable {
        int PageSize { get; }
        int MemberCount { get; }
    }
}