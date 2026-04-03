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
            string categoryColor = LogColors.GetColor(Category);
            string categoryText = $"<color={categoryColor}>{Category}</color>";
            return $"[{Timestamp:HH:mm:ss}] [{Level}] {categoryText} {Message} {FormatMetadata()}";
        }

        private string FormatMetadata()
        {
            return Metadata != null
                ? string.Join(", ", Metadata.Select(kv => $"{kv.Key}={kv.Value}"))
                : "";
        }

    }
}
