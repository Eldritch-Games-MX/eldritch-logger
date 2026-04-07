using EldritchGames.EldritchLogger;
using EldritchGames.EldritchLogger.Visuals;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EldritchGames.EldritchLogger
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public LogCategory Category { get; set; }
        public string Message { get; set; }
        public Dictionary<string, object> Metadata { get; set; }

        public Exception Exception { get; set; }

        public override string ToString()
        {
            var settings = EldritchLogger.CurrentSettings;
            string format = settings != null ? settings.timestampFormat : "HH:mm:ss";
            string prefix = settings != null ? settings.messagePrefix : "";

            string categoryColor = LogColors.GetColorString(Category);
            string categoryText = $"<color={categoryColor}>{Category}</color>";

            string baseMessage = $"[{Timestamp.ToString(format)}] [{Level}] {categoryText} {prefix}{Message} {FormatMetadata()}";

            if (Exception != null)
            {
                string trace = Exception.StackTrace ?? "";
                if (settings != null && settings.filterLoggerFrames)
                {
                    trace = CleanStackTrace(trace);
                }
                baseMessage += $"\n{Exception.GetType().Name}: {Exception.Message}\n{trace}";
            }

            return baseMessage;
        }

        private string FormatMetadata()
        {
            if (Metadata == null || Metadata.Count == 0)
                return "";

            return string.Join(" ", Metadata.Select(kv =>
            {
                return kv.Key switch
                {
                    "ComponentContext" => $"[Component={kv.Value}]",
                    "CSharpEvent" => $"[C#Event={kv.Value}]",
                    "UnityEvent" => $"[UnityEvent={kv.Value}]",
                    "GameObject" => $"[GameObject={kv.Value}]",
                    _ => $"{kv.Key}={kv.Value}"
                };
            }));
        }

        private string CleanStackTrace(string raw)
        {
            var lines = raw.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var filtered = lines.Where(line => !line.Contains("EldritchGames.EldritchLogger")).Select(line => line.Trim());
            return string.Join("\n", filtered);
        }
    }
}