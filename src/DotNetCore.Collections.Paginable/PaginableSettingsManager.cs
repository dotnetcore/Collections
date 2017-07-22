using System;

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Paginable settings manager
    /// </summary>
    public static class PaginableSettingsManager
    {
        private static PaginableSettings m_settingsCache { get; set; }

        static PaginableSettingsManager()
            => m_settingsCache = new PaginableSettings();

        /// <summary>
        /// Get paginable settings
        /// </summary>
        public static PaginableSettings Settings
            => m_settingsCache;

        /// <summary>
        /// Update paginable settings
        /// </summary>
        /// <param name="settings"></param>
        public static void UpdateSettings(PaginableSettings settings)
            => m_settingsCache = settings ?? throw new ArgumentNullException(nameof(settings));
    }
}
