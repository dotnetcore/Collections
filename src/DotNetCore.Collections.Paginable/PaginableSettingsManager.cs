using System;

namespace DotNetCore.Collections.Paginable {
    /// <summary>
    /// Paginable settings manager
    /// </summary>
    public static class PaginableSettingsManager {
        private static PaginableSettings _settingsCache { get; set; }

        static PaginableSettingsManager()
            => _settingsCache = new PaginableSettings();

        /// <summary>
        /// Get paginable settings
        /// </summary>
        public static PaginableSettings Settings
            => _settingsCache;

        /// <summary>
        /// Update paginable settings
        /// </summary>
        /// <param name="settings"></param>
        public static void UpdateSettings(PaginableSettings settings)
            => _settingsCache = settings ?? throw new ArgumentNullException(nameof(settings));
    }
}