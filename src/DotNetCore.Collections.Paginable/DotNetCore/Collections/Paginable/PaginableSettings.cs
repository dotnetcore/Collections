using System;
using DotNetCore.Collections.Paginable.Internal;

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Paginable settings
    /// </summary>
    /// <remarks>
    /// Values are validated on assignment, so any <see cref="PaginableSettings"/> instance
    /// obtained from this library is always in a valid state (immutable-by-convention:
    /// configure the snapshot once at startup, then treat it as read-only).
    /// </remarks>
    /// <example>
    /// <code>
    /// PaginableSettingsManager.UpdateSettings(new PaginableSettings
    /// {
    ///     DefaultPageSize = 50,
    ///     MaxMemberItems = 10_000_000
    /// });
    /// </code>
    /// </example>
    public class PaginableSettings
    {
        private int _defaultPageSize = PaginableConstants.DEFAULT_PAGE_SIZE;
        private long _maxMemberItems = PaginableConstants.MAX_MEMBER_ITEMS_SUPPORT;

        /// <summary>
        /// Gets or sets default page size (must be greater than or equal to one)
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The assigned value is less than one.</exception>
        public int DefaultPageSize
        {
            get => _defaultPageSize;
            set => _defaultPageSize = value >= 1
                ? value
                : throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(DefaultPageSize)} can not be less than one");
        }

        /// <summary>
        /// Gets or sets max member items (must be greater than or equal to one)
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The assigned value is less than one.</exception>
        public long MaxMemberItems
        {
            get => _maxMemberItems;
            set => _maxMemberItems = value >= 1
                ? value
                : throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(MaxMemberItems)} can not be less than one");
        }
    }
}
