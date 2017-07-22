using DotNetCore.Collections.Paginable.Internal;

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Paginable settings
    /// </summary>
    public class PaginableSettings
    {
        public int DefaultPageSize { get; set; } = PaginableConstants.DEFAULT_PAGE_SIZE;
    }
}
