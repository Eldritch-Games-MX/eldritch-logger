using EldritchGames.EldritchLogger.Setttings;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchGames.EldritchLogger
{
    public static class EldritchLogger
    {
        private static LogSettings logSettings;

        public static void Initialize(LogSettings settings)
        {
            logSettings = settings;
        }

        public static void Log(LogLevel level, LogCategory category, string message, Dictionary<string, object>? metadata = null)
        {
            if (!IsInitialized()) return;
            if (!ShouldLog(level, category)) return;

            var entry = BuildLogEntry(level, category, message, metadata);
            OutputLog(entry);
        }

        private static bool IsInitialized()
        {
            if (logSettings == null)
            {
                Debug.LogWarning("EldritchLogger not initialized with LogSettings!");
                return false;
            }
            return true;
        }

        private static bool ShouldLog(LogLevel level, LogCategory category)
        {
            return !(level < logSettings.logLevel || !logSettings.IsCategoryEnabled(category));
        }

        private static LogEntry BuildLogEntry(LogLevel level, LogCategory category, string message, Dictionary<string, object>? metadata)
        {
            return new LogEntry
            {
                Timestamp = DateTime.Now,
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

        public static LogBuilder AtDebug() => new (LogLevel.Debug);
        public static LogBuilder AtInfo() => new (LogLevel.Info);
        public static LogBuilder AtWarning() => new(LogLevel.Warning);
        public static LogBuilder AtError() => new (LogLevel.Error);
        public static LogBuilder AtCritical() => new (LogLevel.Critical);
    }
}