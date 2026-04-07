using EldritchGames.EldritchLogger.Settings;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchGames.EldritchLogger
{
    public static class EldritchLogger
    {
        private static LogSettings logSettings;
        public static LogSettings CurrentSettings => logSettings;
        public static void Initialize(LogSettings settings)
        {
            if (settings == null)
            {
                logSettings = null;
                Debug.LogWarning("EldritchLogger not initialized with LogSettings!");
                return;
            }

            logSettings = settings;

            Application.SetStackTraceLogType(LogType.Log, Map(settings.infoTrace));
            Application.SetStackTraceLogType(LogType.Warning, Map(settings.warningTrace));
            Application.SetStackTraceLogType(LogType.Error, Map(settings.errorTrace));
        }

        public static void Log(LogLevel level, LogCategory category, string message, Dictionary<string, object> metadata = null, Exception exception = null)
        {
            if (!IsInitialized()) return;
            if (!ShouldLog(level, category)) return;

            var entry = BuildLogEntry(level, category, message, metadata, exception);
            OutputLog(entry);
        }

        private static StackTraceLogType Map(StackTraceMode mode) => mode switch
        {
            StackTraceMode.None => StackTraceLogType.None,
            StackTraceMode.ScriptOnly => StackTraceLogType.ScriptOnly,
            StackTraceMode.Full => StackTraceLogType.Full,
            _ => StackTraceLogType.ScriptOnly
        };

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
        private static LogEntry BuildLogEntry(LogLevel level, LogCategory category, string message, Dictionary<string, object> metadata, Exception exception)
        {
            return new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Category = category,
                Message = message,
                Metadata = metadata,
                Exception = exception
            };
        }

        private static void OutputLog(LogEntry entry)
        {
            UnityEngine.Object context = null;

            // Attach context if enabled
            if (logSettings.useContextObjects && entry.Metadata != null &&
                entry.Metadata.TryGetValue("GameObject", out var goName))
            {
                var obj = GameObject.Find(goName as string);
                if (obj != null) context = obj;
            }

            string message = entry.ToString().Trim();

            // Handle exceptions with filtering
            if (entry.Metadata != null && entry.Metadata.TryGetValue("Exception", out var exObj) && exObj is Exception ex)
            {
                string cleanedTrace = CleanStackTrace(ex);
                message += $"\n{ex.GetType().Name}: {ex.Message}\n{cleanedTrace}";
            }

            switch (entry.Level)
            {
                case LogLevel.Debug:
                case LogLevel.Info:
                    Debug.Log(message, context);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(message, context);
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    Debug.LogError(message, context);
                    break;
            }
        }

        private static string CleanStackTrace(Exception ex)
        {
            var raw = ex.StackTrace?.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (raw == null) return string.Empty;

            var filtered = new List<string>();
            foreach (var line in raw)
            {
                if (logSettings.filterLoggerFrames && line.Contains("EldritchGames.EldritchLogger"))
                    continue;
                filtered.Add(line.Trim());
            }

            return string.Join("\n", filtered);
        }

        public static LogBuilder AtDebug() => new(LogLevel.Debug);
        public static LogBuilder AtInfo() => new(LogLevel.Info);
        public static LogBuilder AtWarning() => new(LogLevel.Warning);
        public static LogBuilder AtError() => new(LogLevel.Error);
        public static LogBuilder AtCritical() => new(LogLevel.Critical);
    }
}