using System;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger
{
    public class LogEntry
    {
        public DateTime Timestamp { get; }
        public LogLevel Level { get; }
        public LogCategory Category { get; }
        public string Message { get; }
        public Dictionary<string, object> Metadata { get; }
        public Exception Exception { get; }

        public LogEntry(DateTime timestamp,
                        LogLevel level,
                        LogCategory category,
                        string message,
                        Dictionary<string, object> metadata = null,
                        Exception exception = null)
        {
            Timestamp = timestamp;
            Level = level;
            Category = category;
            Message = message;
            Metadata = metadata ?? new Dictionary<string, object>();
            Exception = exception;
        }
    }
}
