using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Format;
using EldritchGames.EldritchLogger.Settings;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Exporting
{
    public class UnityConsoleExporter : ILogExporter
    {
        private readonly LogSettings settings;

        public UnityConsoleExporter(LogSettings settings)
        {
            this.settings = settings;
        }

        public void Export(LogEntryDto dto, string path)
        {
            var formatter = new LogEntryFormatter(settings);
            string formatted = formatter.Format(dto);

            Object context = ResolveContext(dto);

            // If suppression is enabled, disable Unity's automatic stack trace
            if (settings.suppressUnityStackTrace)
            {
                Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
                Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
                Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);
            }

            switch (dto.Level)
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


        private Object ResolveContext(LogEntryDto dto)
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
                var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
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
