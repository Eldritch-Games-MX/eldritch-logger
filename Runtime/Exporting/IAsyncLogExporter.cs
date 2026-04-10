using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using System.Threading.Tasks;

namespace EldritchGames.EldritchLogger.Exporting
{
    /// <summary>
    /// Contract for sinks that perform asynchronous I/O (e.g., file exporters).
    /// </summary>
    public interface IAsyncLogExporter : ILogSink
    {
        /// <summary>
        /// Asynchronously exports a log entry to a file.
        /// </summary>
        Task Export(LogEntryDto entry, string path);

        /// <summary>
        /// Target file path for this exporter.
        /// </summary>
        string TargetPath { get; }
    }
}
