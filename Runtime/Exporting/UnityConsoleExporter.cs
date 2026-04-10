using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Format;
using EldritchGames.EldritchLogger.Settings;
using System;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Exporting
{
    public class UnityConsoleExporter : ILogSink
    {
        private readonly LogSettings settings;
        private readonly LogEntryFormatter formatter;

        public SinkCategory Category => SinkCategory.Runtime;

        public UnityConsoleExporter(LogSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            formatter = new LogEntryFormatter(settings);
        }

        public void OnLogReceived(LogEntryDto entry)
        {
            string formatted = formatter.Format(entry);
            UnityEngine.Object context = ResolveContext(entry);

            if (settings.suppressUnityStackTrace)
                SuppressUnityStackTraces();

            switch (entry.Level)
            {
                case "Debug":
                case "Info":
                    Debug.Log(formatted, context);
                    break;
                case "Warning":
                    Debug.LogWarning(formatted, context);
                    break;
                case "Error":
                case "Critical":
                    Debug.LogError(formatted, context);
                    break;
            }
        }

        private void SuppressUnityStackTraces()
        {
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.None);
        }

        private UnityEngine.Object ResolveContext(LogEntryDto dto)
        {
            if (!settings.useContextObjects || dto.Metadata == null)
                return null;

            // Try GameObject first
            var goMeta = dto.Metadata.Find(m => m.Key == "GameObject");
            if (goMeta != null && !string.IsNullOrEmpty(goMeta.Value))
            {
                var go = GameObject.Find(goMeta.Value);
                if (go != null)
                    return go;
            }

            // Try Component
            var compMeta = dto.Metadata.Find(m => m.Key == "Component");
            if (compMeta != null && !string.IsNullOrEmpty(compMeta.Value))
            {
                var allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                foreach (var go in allObjects)
                {
                    var comp = go.GetComponent(compMeta.Value);
                    if (comp != null)
                        return comp;
                }
            }

            return null;
        }
    }
}
