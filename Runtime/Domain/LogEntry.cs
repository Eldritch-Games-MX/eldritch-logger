using EldritchGames.EldritchLogger.Core;
using System;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Domain
{
    /// <summary>
    /// An immutable log event, as produced by callers and processed by the logger pipeline.
    /// </summary>
    public sealed class LogEntry
    {
        private static readonly IReadOnlyDictionary<string, object> NoProperties =
            new Dictionary<string, object>();

        /// <summary>
        /// When the entry was logged. <c>default</c> until the logger stamps it.
        /// </summary>
        public DateTime TimestampUtc { get; }
        public LogLevel Level { get; }
        public LogCategory Category { get; }
        public string Message { get; }
        public IReadOnlyDictionary<string, object> Properties { get; }
        public Exception Exception { get; }

        /// <summary>
        /// Optional Unity object the entry relates to (used for click-to-select in the Unity Console).
        /// Never serialized.
        /// </summary>
        public UnityEngine.Object Context { get; }

        public LogEntry(LogLevel level,
                        LogCategory category,
                        string message,
                        IReadOnlyDictionary<string, object> properties = null,
                        Exception exception = null,
                        UnityEngine.Object context = null,
                        DateTime timestampUtc = default)
        {
            Level = level;
            Category = category;
            Message = message ?? string.Empty;
            Properties = properties ?? NoProperties;
            Exception = exception;
            Context = context;
            TimestampUtc = timestampUtc;
        }

        public LogEntry WithTimestamp(DateTime timestampUtc) =>
            new(Level, Category, Message, Properties, Exception, Context, timestampUtc);

        public LogEntry WithProperties(IReadOnlyDictionary<string, object> properties) =>
            new(Level, Category, Message, properties, Exception, Context, TimestampUtc);

        /// <summary>Returns a copy with <paramref name="key"/> set to <paramref name="value"/>.</summary>
        public LogEntry WithProperty(string key, object value)
        {
            var copy = new Dictionary<string, object>(Properties.Count + 1);
            foreach (var kv in Properties)
                copy[kv.Key] = kv.Value;
            copy[key] = value;
            return WithProperties(copy);
        }
    }
}
