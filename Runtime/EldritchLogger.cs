using EldritchGames.EldritchLogger.Setttings;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchGames.EldritchLogger
{
    public static class EldritchLogger
    {
        private static LogSettings logSettings;

        public static void Log(LogLevel level, LogCategory category, string message, Dictionary<string, object> metadata = null)
        {
            if (!IsInitialized()) return;
            if (!ShouldLog(level, category)) return;

            var entry = BuildLogEntry(level, category, message, metadata);
            OutputLog(entry);
        }

        public static void Initialize(LogSettings settings)
        {
            logSettings = settings;
        }

        private static bool IsInitialized()
        {
            if (logSettings == null)
            {
                Debug.LogWarning("GameLogger not initialized with LogSettings!");
                return false;
            }
            return true;
        }

        private static bool ShouldLog(LogLevel level, LogCategory category)
        {
            return !(level < logSettings.logLevel || !logSettings.IsCategoryEnabled(category));
        }

        private static LogEntry BuildLogEntry(LogLevel level, LogCategory category, string message, Dictionary<string, object> metadata)
        {
            return new LogEntry
            {
                Timestamp = System.DateTime.Now,
                Level = level,
                Category = category,
                Message = message,
                Metadata = metadata
            };
        }
        private static void OutputLog(LogEntry entry)
        {
            switch (entry.Level)
            {
                case LogLevel.Debug:
                case LogLevel.Info:
                    Debug.Log(entry.ToString());
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(entry.ToString());
                    break;
                case LogLevel.Error:
                    Debug.LogError(entry.ToString());
                    break;
                case LogLevel.Critical:
                    Debug.LogError($"CRITICAL: {entry}");
                    break;
            }
        }
    }
}
