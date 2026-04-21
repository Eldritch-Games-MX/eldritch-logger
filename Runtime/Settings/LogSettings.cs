using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Visuals;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Settings
{
    [CreateAssetMenu(fileName = "LogSettings", menuName = "Eldritch Logger/Log Settings", order = 0)]
    public class LogSettings : ScriptableObject
    {
        public LogLevel logLevel = LogLevel.Debug;
        public List<LogCategory> enabledCategories = new List<LogCategory>
        {
            LogCategory.General,
            LogCategory.Network,
            LogCategory.UI,
            LogCategory.Audio,
            LogCategory.Physics,
            LogCategory.AI,
            LogCategory.Animation,
            LogCategory.Input,
        };

        [Tooltip("If enabled, previous session logs will be cleared on startup.")]
        public bool clearOnStartup = true;

        public string timestampFormat = "HH:mm:ss";   // default format
        public string messagePrefix = "";             // optional prefix

        // Export options
        public bool enableExport = false;
        public List<ExportFormat> exportFormats { get; set; } = new();
        public string exportFileName = "eldritch_logs";
        public string exportDirectory = ""; // default to Application.persistentDataPath if empty

        [Header("Context Objects")]
        public bool useContextObjects = true;

        [Header("Stack Trace")]
        [Tooltip("Suppress Unity's automatic stack trace output. EldritchLogger will handle exception traces itself.")]
        public bool suppressUnityStackTrace = true;

        [Header("Exception Filtering")]
        [Tooltip("Remove EldritchLogger internal frames from exception stack traces.")]
        public bool filterLoggerFrames = true;

        [Header("Category Colors")]
        public bool useCategoryColors = true;
        public List<CategoryColor> categoryColors = new List<CategoryColor>
        {
            new CategoryColor(LogCategory.General, Color.white),
            new CategoryColor(LogCategory.Gameplay, Color.green),
            new CategoryColor(LogCategory.UI, Color.blue),
            new CategoryColor(LogCategory.Audio, Color.yellow),
            new CategoryColor(LogCategory.Network, Color.magenta),
            new CategoryColor(LogCategory.AI, Color.cyan),
            new CategoryColor(LogCategory.Physics, new Color(1f, 0.5f, 0f)),
            new CategoryColor(LogCategory.Animation, new Color(0.5f, 0f, 0.5f)),
            new CategoryColor(LogCategory.Input, new Color(0f, 0.5f, 0f))
        };

        public Color GetCategoryColor(LogCategory category)
        {
            var entry = categoryColors.Find(c => c.category == category);
            return entry != null ? entry.color : Color.white;
        }

        public bool IsCategoryEnabled(LogCategory category) => enabledCategories.Contains(category);

        public void ApplyVerbosePreset()
        {
            logLevel = LogLevel.Debug;
            EnableAllCategories();
        }

        public void ApplyNormalPreset()
        {
            logLevel = LogLevel.Info;
            enabledCategories = new List<LogCategory>
            {
                LogCategory.Gameplay,
                LogCategory.UI,
                LogCategory.Network
            };
        }

        public void ApplyProductionPreset()
        {
            logLevel = LogLevel.Warning;
            enabledCategories = new List<LogCategory>
            {
                LogCategory.Gameplay,
                LogCategory.Network
            };
        }

        public void EnableAllCategories()
        {
            enabledCategories = new List<LogCategory>(
                (LogCategory[])Enum.GetValues(typeof(LogCategory)));
        }

        public void DisableAllCategories()
        {
            enabledCategories.Clear();
        }
    }

    public enum ExportFormat
    {
        None,
        Json,
        Xml,
        Text
    }
}
