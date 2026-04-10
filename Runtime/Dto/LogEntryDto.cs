using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Dto
{
    /// <summary>
    /// Represents a single metadata key/value pair for a log entry.
    /// Used in <see cref="LogEntryDto"/> to serialize metadata in XML/JSON-friendly form.
    /// </summary>
    public class MetadataEntry
    {
        /// <summary>
        /// The metadata key (e.g. "GameObject", "ComponentContext").
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The metadata value as a string.
        /// </summary>
        public string Value { get; set; }
    }

    /// <summary>
    /// A simple data transfer object (DTO) representing a log entry
    /// in a serialization-friendly format.
    /// Used by all exporters to ensure consistent output.
    /// </summary>
    public class LogEntryDto
    {
        /// <summary>
        /// The timestamp of the log entry.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The log level (e.g. Debug, Info, Warning, Error, Critical).
        /// </summary>
        public string Level { get; set; }

        /// <summary>
        /// The category of the log entry (e.g. Gameplay, UI, General).
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// The main message of the log entry.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Optional metadata entries associated with the log entry.
        /// </summary>
        public List<MetadataEntry> Metadata { get; set; } = new();

        /// <summary>
        /// The exception message if an exception was logged, otherwise null.
        /// </summary>
        public string Exception { get; set; }
    }
}
