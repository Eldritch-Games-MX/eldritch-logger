using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using System.Threading.Tasks;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Exporting
{
    /// <summary>
    /// Defines the contract for all log exporters.
    /// Exporters are responsible for persisting or transmitting log entries to a specific destination (JSON, XML, text files)
    /// Exporters receive a <see cref="LogEntryDto"/> and handle persistence or output.
    /// </summary>
    public interface ILogExporter : ILogSink
    {
        /// <summary>
        /// Exports the given log entry DTO to the target destination.
        /// </summary>
        /// <param name="dto">The log entry data transfer object.</param>
        /// <param name="path">Optional path or identifier for the export target.</param>
        Task Export(LogEntryDto dto, string path);
    }
}
