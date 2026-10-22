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

        /// <summary>
        /// Gets or sets whether a paginable built from a lazy sequence memoizes that sequence, so
        /// that the member-count pass and every later page read share one walk of the source
        /// instead of each restarting from the beginning. Defaults to <see langword="false"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The switch is a trade, and the default is the conservative side of it: memoizing turns
        /// <c>1 + pages</c> walks of the source into one, but it pays for that with a cache fill and
        /// a cache read in place of each walk step. Whether that is a win depends entirely on how
        /// expensive a step of the source is.
        /// </para>
        /// <para>
        /// For a remote or database-backed sequence - where a walk step is a round trip - it is an
        /// easy win, and that is what the switch is for. For an in-memory iterator it depends on the
        /// shape of the read, and the measured numbers say so: reading every page of one paging set
        /// benefits (the quadratic re-walk disappears, and with it most of the source's own
        /// per-element allocations), while reading a single page of it does not (the member-count
        /// pass has already walked everything, so there is nothing left for the cache to save and
        /// only its fill cost to pay). Turn it on when the source is expensive to re-enumerate;
        /// leave it off when it is not.
        /// </para>
        /// <para>
        /// Two source shapes are never memoized regardless of this setting, because for them the
        /// cache is pure overhead or actively harmful: a materialised collection
        /// (<see cref="System.Collections.Generic.ICollection{T}"/>) already re-enumerates cheaply
        /// and exposes an <c>O(1)</c> count, and an <see cref="System.Linq.IQueryable{T}"/> has to
        /// keep its provider translation - memoizing it would pull the whole result set to the
        /// client and defeat <c>Skip</c>/<c>Take</c> on the server.
        /// </para>
        /// <para>
        /// The cache is built by the engine's <c>Memoize</c> operator, so a paginable that turns
        /// this on also snapshots its source: elements added to the underlying sequence after the
        /// first walk are not picked up by later pages.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// PaginableSettingsManager.UpdateSettings(new PaginableSettings
        /// {
        ///     MemoizeLazySources = true
        /// });
        /// </code>
        /// </example>
        public bool MemoizeLazySources { get; set; }
    }
}
