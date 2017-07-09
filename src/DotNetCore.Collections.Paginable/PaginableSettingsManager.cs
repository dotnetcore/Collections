using System;

namespace DotNetCore.Collections.Paginable
{
    public static class PaginableSettingsManager
    {
        private static PaginableSettings m_settingsCache { get; set; }

        static PaginableSettingsManager()
            => m_settingsCache = new PaginableSettings();

        public static PaginableSettings Settings
            => m_settingsCache;

        public static void UpdateSettings(PaginableSettings settings)
            => m_settingsCache = settings ?? throw new ArgumentNullException(nameof(settings));
    }
}
