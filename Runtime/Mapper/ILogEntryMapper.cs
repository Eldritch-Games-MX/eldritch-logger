using EldritchGames.EldritchLogger.Domain;
using EldritchGames.EldritchLogger.Dto;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Mapper
{
    /// <summary>
    /// Provides an abstraction for converting a <see cref="LogEntry"/> domain object
    /// into a serialization-friendly <see cref="LogEntryDto"/>.
    /// This ensures exporters do not depend on internal <see cref="LogEntry"/> structure.
    /// </summary>
    public interface ILogEntryMapper
    {
        /// <summary>
        /// Maps a <see cref="LogEntry"/> into a <see cref="LogEntryDto"/> with normalized fields
        /// (timestamp, level, category, message, metadata, exception).
        /// </summary>
        /// <param name="entry">The log entry to map.</param>
        /// <returns>A DTO representation of the log entry.</returns>
        LogEntryDto ToDto(LogEntry entry);
    }

}

