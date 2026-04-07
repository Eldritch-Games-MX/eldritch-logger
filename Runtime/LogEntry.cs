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

        public override string ToString()
        {
            var settings = EldritchLogger.CurrentSettings;
            string format = settings != null ? settings.timestampFormat : "HH:mm:ss";
            string prefix = settings != null ? settings.messagePrefix : "";

            string categoryColor = LogColors.GetColorString(Category);
            string categoryText = $"<color={categoryColor}>{Category}</color>";

            return $"[{Timestamp.ToString(format)}] [{Level}] {categoryText} {prefix}{Message} {FormatMetadata()}";
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
                    "ExceptionType" => $"[Exception={kv.Value}]",
                    "ExceptionMessage" => $"[Message={kv.Value}]",
                    _ => $"{kv.Key}={kv.Value}"
                };
            }));
        }
    }
}