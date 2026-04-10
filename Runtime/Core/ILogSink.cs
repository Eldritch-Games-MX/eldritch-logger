using EldritchGames.EldritchLogger.Dto;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Core
{
    public enum SinkCategory
    {
        Persistent,
        Runtime,
        Network
    }
    /// <summary>
    /// Represents a destination ("sink") for log entries.
    /// A sink receives log entries from the logger and processes them
    /// (e.g., writing to a file, sending to the console, or posting to a network service).
    /// </summary>
    public interface ILogSink
    {
        /// <summary>
        /// Gets the category of this sink.
        /// Categories are used to group sinks by purpose (e.g., Persistent, Runtime, Network).
        /// </summary>
        SinkCategory Category { get; }
        /// <summary>
        /// Called by the logger whenever a new log entry is produced.
        /// Implementations should handle the log entry according to their purpose,
        /// such as writing to disk, displaying in the Unity console, or sending over the network.
        /// </summary>
        /// <param name="entry">
        /// The log entry data transfer object (DTO) containing all relevant information
        /// about the log event (timestamp, level, category, message, metadata, exception).
        /// </param>
        void OnLogReceived(LogEntryDto logEntry);
    }
}