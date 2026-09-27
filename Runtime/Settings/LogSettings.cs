using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Sinks.Config;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Settings
{
    /// <summary>
    /// Logger configuration asset: filtering, formatting and the list of sinks.
    /// Presets live in <see cref="LogSettingsPresets"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "LogSettings", menuName = "Eldritch Logger/Log Settings", order = 0)]
    public class LogSettings : ScriptableObject, ISerializationCallbackReceiver
    {
        [Tooltip("Entries below this level are discarded before reaching any sink.")]
        public LogLevel minimumLevel = LogLevel.Debug;

        [Tooltip("Built-in and custom categories. Entries in unknown or disabled categories are discarded.")]
        public List<CategorySetting> categories = CreateDefaultCategories();

        [Header("Formatting")]
        public bool useCategoryColors = true;
        public string timestampFormat = "HH:mm:ss";
        public string messagePrefix = "";

        [Tooltip("Remove EldritchLogger internal frames from exception stack traces.")]
        public bool filterLoggerFrames = true;

        [Header("Sinks")]
        [SerializeReference]
        public List<LogSinkConfig> sinks = new() { new UnityConsoleSinkConfig() };

        [Header("Bootstrap")]
        [Tooltip("Create the logger automatically before the first scene loads.")]
        public bool autoInitialize = true;

        [NonSerialized] private Dictionary<string, CategorySetting> lookup;

        public static List<CategorySetting> CreateDefaultCategories()
        {
            var defaults = new[]
            {
                Color.white, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan,
                new Color(1f, 0.5f, 0f), new Color(0.5f, 0f, 0.5f), new Color(0f, 0.5f, 0f)
            };

            var list = new List<CategorySetting>();
            for (int i = 0; i < LogCategory.BuiltIn.Count; i++)
                list.Add(new CategorySetting(LogCategory.BuiltIn[i].Name, defaults[i]));
            return list;
        }

        public CategorySetting FindCategory(LogCategory category)
        {
            var map = lookup;
            if (map == null)
            {
                map = new Dictionary<string, CategorySetting>(StringComparer.OrdinalIgnoreCase);
                if (categories != null)
                    foreach (var entry in categories)
                        if (entry != null && !string.IsNullOrWhiteSpace(entry.name))
                            map[entry.name] = entry;
                lookup = map;
            }

            return map.TryGetValue(category.Name, out var setting) ? setting : null;
        }

        public bool IsCategoryEnabled(LogCategory category) => FindCategory(category)?.enabled ?? false;

        public Color GetCategoryColor(LogCategory category) => FindCategory(category)?.color ?? Color.white;

        /// <summary>Registers a category. Returns false if the name is empty or already registered.</summary>
        public bool AddCategory(string name, Color color)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (FindCategory(new LogCategory(name)) != null) return false;

            categories.Add(new CategorySetting(name.Trim(), color));
            InvalidateCache();
            return true;
        }

        /// <summary>Removes a custom category. Built-in categories cannot be removed.</summary>
        public bool RemoveCategory(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            int removed = categories.RemoveAll(c =>
                !c.IsBuiltIn && string.Equals(c.name, name, StringComparison.OrdinalIgnoreCase));
            InvalidateCache();
            return removed > 0;
        }

        public void SetAllCategoriesEnabled(bool enabled)
        {
            foreach (var c in categories) c.enabled = enabled;
        }

        /// <summary>Call after editing <see cref="categories"/> directly (adding, removing or renaming entries).</summary>
        public void InvalidateCache() => lookup = null;

        private void OnValidate() => InvalidateCache();

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize() => InvalidateCache();
    }
}
