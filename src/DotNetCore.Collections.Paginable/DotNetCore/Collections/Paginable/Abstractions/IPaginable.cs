using System.Collections;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable {
    /// <summary>
    /// Paginable interface
    /// </summary>
    public interface IPaginable : IEnumerable {
        /// <summary>
        /// Gets page size
        /// </summary>
        int PageSize { get; }

        /// <summary>
        /// Gets member count
        /// </summary>
        int MemberCount { get; }
    }
}