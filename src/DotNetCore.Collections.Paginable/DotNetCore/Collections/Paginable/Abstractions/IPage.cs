using System.Collections;

// ReSharper disable once CheckNamespace
namespace DotNetCore.Collections.Paginable {
    /// <summary>
    /// Page interface
    /// </summary>
    public interface IPage : IEnumerable {
        /// <summary>
        /// Gets total page count
        /// </summary>
        int TotalPageCount { get; }

        /// <summary>
        /// Gets total member count
        /// </summary>
        int TotalMemberCount { get; }

        /// <summary>
        /// Gets current page number
        /// </summary>
        int CurrentPageNumber { get; }

        /// <summary>
        /// Gets page size
        /// </summary>
        int PageSize { get; }

        /// <summary>
        /// Gets current page size, may equal to or less than page size.
        /// </summary>
        int CurrentPageSize { get; }

        /// <summary>
        /// Has previous. If this page is the first page, then returns false.
        /// </summary>
        bool HasPrevious { get; }

        /// <summary>
        /// Has next. If this page is the last page, then returns false.
        /// </summary>
        bool HasNext { get; }

        /// <summary>
        /// Get metadata of page
        /// </summary>
        /// <returns></returns>
        PageMetadata GetMetadata();
    }
}