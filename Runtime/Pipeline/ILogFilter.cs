using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Settings;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

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
    /// Filters by <see cref="LogSettings.minimumLevel"/> and the enabled categories, plus runtime overrides
    /// (see <see cref="ILogControl"/>). Reads the settings on every call, so inspector changes apply immediately;
    /// overrides live in memory only and never modify the settings asset.
    /// </summary>
    public sealed class SettingsLogFilter : ILogFilter
    {
        private readonly LogSettings settings;
        private readonly ConcurrentDictionary<LogCategory, bool> categoryOverrides = new();
        private int levelOverride = -1; // -1 = none; otherwise (int)LogLevel

        public SettingsLogFilter(LogSettings settings)
        {
            this.settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
        }

        public bool IsEnabled(LogLevel level, LogCategory category) =>
            level >= MinimumLevel && IsCategoryEnabled(category);

        public LogLevel MinimumLevel
        {
            get
            {
                int overridden = System.Threading.Volatile.Read(ref levelOverride);
                return overridden >= 0 ? (LogLevel)overridden : settings.minimumLevel;
            }
        }

        public LogLevel? MinimumLevelOverride
        {
            get
            {
                int overridden = System.Threading.Volatile.Read(ref levelOverride);
                return overridden >= 0 ? (LogLevel)overridden : null;
            }
            set => System.Threading.Volatile.Write(ref levelOverride, value.HasValue ? (int)value.Value : -1);
        }

        // Checked on every log call; the dictionary is only consulted while overrides exist (rare).
        private volatile bool hasCategoryOverrides;

        public bool IsCategoryEnabled(LogCategory category) =>
            hasCategoryOverrides && categoryOverrides.TryGetValue(category, out var enabled)
                ? enabled
                : settings.IsCategoryEnabled(category);

        public void SetCategoryOverride(LogCategory category, bool? enabled)
        {
            lock (categoryOverrides)
            {
                if (enabled.HasValue) categoryOverrides[category] = enabled.Value;
                else categoryOverrides.TryRemove(category, out _);
                hasCategoryOverrides = categoryOverrides.Count > 0;
            }
        }

        public void ClearOverrides()
        {
            lock (categoryOverrides)
            {
                categoryOverrides.Clear();
                hasCategoryOverrides = false;
            }
            MinimumLevelOverride = null;
        }

        /// <summary>Categories from the settings asset and from overrides, with their effective state.</summary>
        public IReadOnlyList<CategoryState> Categories
        {
            get
            {
                var result = new List<CategoryState>();
                var seen = new HashSet<LogCategory>();

                foreach (var setting in settings.categories)
                {
                    if (setting == null || string.IsNullOrWhiteSpace(setting.name)) continue;
                    var category = setting.Category;
                    if (!seen.Add(category)) continue;
                    bool overridden = categoryOverrides.TryGetValue(category, out var enabled);
                    result.Add(new CategoryState(category, overridden ? enabled : setting.enabled, overridden));
                }

                foreach (var kv in categoryOverrides)
                    if (seen.Add(kv.Key))
                        result.Add(new CategoryState(kv.Key, kv.Value, true));

                return result;
            }
        }
    }
}
