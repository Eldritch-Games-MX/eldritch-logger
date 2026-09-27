using EldritchGames.EldritchLogger.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace EldritchGames.EldritchLogger.Dto
{
    /// <summary>
    /// A single metadata key/value pair in serialization-friendly form.
    /// </summary>
    public class MetadataEntry
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// Serialization-friendly view of a processed log entry. This is what sinks receive.
    /// Sinks must treat it as read-only: the same instance is shared by every sink.
    /// </summary>
    public class LogEntryDto
    {
        /// <summary>Timestamp in UTC.</summary>
        public DateTime Timestamp { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public LogLevel Level { get; set; }

        public string Category { get; set; }

        public string Message { get; set; }

        public List<MetadataEntry> Metadata { get; set; } = new();

        /// <summary>Exception type, message and (filtered) stack trace, or null.</summary>
        public string Exception { get; set; }

        /// <summary>Unity object the entry relates to. Main-thread only; never serialized.</summary>
        [XmlIgnore, JsonIgnore]
        public UnityEngine.Object Context { get; set; }

        /// <summary>Returns the value of metadata <paramref name="key"/>, or null.</summary>
        public string GetMetadata(string key)
        {
            if (Metadata == null) return null;
            foreach (var entry in Metadata)
                if (entry.Key == key) return entry.Value;
            return null;
        }
    }
}
