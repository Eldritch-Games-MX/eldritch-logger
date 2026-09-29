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
        private static readonly IReadOnlyList<string> NoKeys = Array.Empty<string>();

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
        /// Keys of <see cref="Properties"/> whose values are already rendered into <see cref="Message"/>
        /// (the filled holes of a message template). Text output leaves these out to avoid repeating them.
        /// </summary>
        public IReadOnlyList<string> RenderedProperties { get; }

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
                        DateTime timestampUtc = default,
                        IReadOnlyList<string> renderedProperties = null)
        {
            Level = level;
            Category = category;
            Message = message ?? string.Empty;
            Properties = properties ?? NoProperties;
            Exception = exception;
            Context = context;
            TimestampUtc = timestampUtc;
            RenderedProperties = renderedProperties ?? NoKeys;
        }

        public LogEntry WithTimestamp(DateTime timestampUtc) =>
            new(Level, Category, Message, Properties, Exception, Context, timestampUtc, RenderedProperties);

        public LogEntry WithProperties(IReadOnlyDictionary<string, object> properties) =>
            new(Level, Category, Message, properties, Exception, Context, TimestampUtc, RenderedProperties);

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
