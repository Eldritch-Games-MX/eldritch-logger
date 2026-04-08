using EldritchGames.EldritchLogger;
using EldritchGames.EldritchLogger.Dto;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Exporting
{
    /// <summary>
    /// Defines the contract for all log exporters.
    /// Exporters receive a <see cref="LogEntryDto"/> and handle persistence or output.
    /// </summary>
    public interface ILogExporter
    {
        /// <summary>
        /// Exports the given log entry DTO to the target destination.
        /// </summary>
        /// <param name="dto">The log entry data transfer object.</param>
        /// <param name="path">Optional path or identifier for the export target.</param>
        void Export(LogEntryDto dto, string path);
    }
}
