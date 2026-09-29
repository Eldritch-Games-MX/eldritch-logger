using EldritchGames.EldritchLogger.Core;
using System;

namespace EldritchGames.EldritchLogger.Settings
{
    /// <summary>
    /// One-click configurations for <see cref="LogSettings"/>.
    /// Presets change the minimum level and built-in category toggles; custom categories
    /// are only touched by <see cref="ApplyVerbose"/>.
    /// </summary>
    public static class LogSettingsPresets
    {
        public static void ApplyVerbose(LogSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            settings.minimumLevel = LogLevel.Debug;
            settings.SetAllCategoriesEnabled(true);
        }

        // Unity stays enabled in every preset: it carries engine errors and uncaught exceptions.
        public static void ApplyNormal(LogSettings settings) =>
            Apply(settings, LogLevel.Info, LogCategory.Gameplay, LogCategory.UI, LogCategory.Network, LogCategory.Unity);

        public static void ApplyProduction(LogSettings settings) =>
            Apply(settings, LogLevel.Warning, LogCategory.Gameplay, LogCategory.Network, LogCategory.Unity);

        private static void Apply(LogSettings settings, LogLevel level, params LogCategory[] enabledBuiltIns)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            settings.minimumLevel = level;

            foreach (var entry in settings.categories)
            {
                if (!entry.IsBuiltIn) continue;
                entry.enabled = Array.IndexOf(enabledBuiltIns, entry.Category) >= 0;
            }
        }
    }
}
