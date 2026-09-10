using System;

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Paginable settings manager
    /// </summary>
    /// <remarks>
    /// The settings snapshot should be configured once at application startup.
    /// <see cref="UpdateSettings"/> swaps an immutable-by-convention snapshot atomically
    /// (volatile write); readers always observe a fully valid configuration.
    /// </remarks>
    public static class PaginableSettingsManager
    {
        // volatile: guarantee that after UpdateSettings completes, all threads observe the latest snapshot
        private static volatile PaginableSettings _settingsCache;

        static PaginableSettingsManager()
            => _settingsCache = new PaginableSettings();

        /// <summary>
        /// Get paginable settings
        /// </summary>
        public static PaginableSettings Settings
            => _settingsCache;

        /// <summary>
        /// Update paginable settings with a new, fully validated settings snapshot.
        /// </summary>
        /// <param name="settings"></param>
        /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// PaginableSettingsManager.UpdateSettings(new PaginableSettings
        /// {
        ///     DefaultPageSize = 50,
        ///     MaxMemberItems = 10_000_000
        /// });
        /// </code>
        /// </example>
        public static void UpdateSettings(PaginableSettings settings)
            => _settingsCache = settings ?? throw new ArgumentNullException(nameof(settings));
    }
}
