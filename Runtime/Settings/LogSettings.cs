using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Sinks.Config;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("logLevel")]
        public LogLevel minimumLevel = LogLevel.Debug;

        [Tooltip("Built-in and custom categories. Entries in unknown or disabled categories are discarded.")]
        [FormerlySerializedAs("customCategories")]
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

        // ---- 2.x fields, read once for migration and then cleared ----
        [SerializeField, HideInInspector, FormerlySerializedAs("enabledCategories")]
        private List<int> legacyEnabledCategories = new();
        [SerializeField, HideInInspector, FormerlySerializedAs("categoryColors")]
        private List<LegacyCategoryColor> legacyCategoryColors = new();
        [SerializeField, HideInInspector, FormerlySerializedAs("suppressUnityStackTrace")]
        private int legacySuppressUnityStackTrace = -1;
        [SerializeField, HideInInspector, FormerlySerializedAs("useContextObjects")]
        private int legacyUseContextObjects = -1;

        [Serializable]
        private class LegacyCategoryColor
        {
            public int category;
            public Color color = Color.white;
        }

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

        public void OnAfterDeserialize()
        {
            InvalidateCache();
            MigrateLegacyFields();
        }

        private void MigrateLegacyFields()
        {
            categories ??= new List<CategorySetting>();
            sinks ??= new List<LogSinkConfig>();

            if (legacyCategoryColors.Count > 0 || legacyEnabledCategories.Count > 0)
            {
                // 2.x assets saved before `categoryColors` existed used its initializer, i.e. the default colors.
                // Assets that saved the list showed white for any category missing from it.
                var defaults = CreateDefaultCategories();
                bool hadColorList = legacyCategoryColors.Count > 0;

                var builtIns = new List<CategorySetting>();
                for (int i = 0; i < LogCategory.BuiltIn.Count; i++)
                {
                    var color = hadColorList ? Color.white : defaults[i].color;
                    foreach (var legacy in legacyCategoryColors)
                        if (legacy.category == i) color = legacy.color;

                    builtIns.Add(new CategorySetting(LogCategory.BuiltIn[i].Name, color,
                                                     legacyEnabledCategories.Contains(i)));
                }

                // Custom categories were loaded into `categories` via FormerlySerializedAs.
                categories.RemoveAll(c => c == null || c.IsBuiltIn);
                categories.InsertRange(0, builtIns);

                legacyCategoryColors.Clear();
                legacyEnabledCategories.Clear();
            }

            if (legacySuppressUnityStackTrace >= 0 || legacyUseContextObjects >= 0)
            {
                foreach (var sink in sinks)
                {
                    if (sink is not UnityConsoleSinkConfig console) continue;
                    if (legacySuppressUnityStackTrace >= 0) console.suppressUnityStackTrace = legacySuppressUnityStackTrace != 0;
                    if (legacyUseContextObjects >= 0) console.useContextObjects = legacyUseContextObjects != 0;
                }

                legacySuppressUnityStackTrace = -1;
                legacyUseContextObjects = -1;
            }
        }
    }
}
