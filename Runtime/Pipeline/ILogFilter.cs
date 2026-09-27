using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Settings;
using System;

namespace EldritchGames.EldritchLogger.Pipeline
{
    /// <summary>
    /// Decides whether an entry enters the pipeline at all. Must be thread-safe and cheap.
    /// </summary>
    public interface ILogFilter
    {
        bool IsEnabled(LogLevel level, LogCategory category);
    }

    /// <summary>
    /// Filters by <see cref="LogSettings.minimumLevel"/> and the enabled categories.
    /// Reads the settings on every call, so inspector changes apply immediately.
    /// </summary>
    public sealed class SettingsLogFilter : ILogFilter
    {
        private readonly LogSettings settings;

        public SettingsLogFilter(LogSettings settings)
        {
            this.settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
        }

        public bool IsEnabled(LogLevel level, LogCategory category) =>
            level >= settings.minimumLevel && settings.IsCategoryEnabled(category);
    }
}
