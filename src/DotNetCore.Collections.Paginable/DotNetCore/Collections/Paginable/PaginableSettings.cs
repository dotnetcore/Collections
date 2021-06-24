using DotNetCore.Collections.Paginable.Internal;

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Paginable settings
    /// </summary>
    public class PaginableSettings
    {
        /// <summary>
        /// Gets or sets default page size
        /// </summary>
        public int DefaultPageSize { get; set; } = PaginableConstants.DEFAULT_PAGE_SIZE;

        /// <summary>
        /// Gets or sets max member items
        /// </summary>
        public long MaxMemberItems { get; set; } = PaginableConstants.MAX_MEMBER_ITEMS_SUPPORT;
    }
}