using System;
using System.IO;

namespace EldritchGames.EldritchLogger.Exporting
{
    /// <summary>
    /// Defines a contract for file writers used by log exporters.
    /// A file writer ensures that a <see cref="StreamWriter"/> is properly
    /// initialized and reused for writing log entries to disk.
    /// </summary>
    public interface ILogFileWriter : IDisposable
    {
        /// <summary>
        /// Ensures that a <see cref="StreamWriter"/> is initialized for the given path.
        /// If the file does not exist, it should be created. If append mode is requested,
        /// the writer should continue writing at the end of the file.
        /// </summary>
        /// <param name="path">
        /// The file path where log entries will be written.
        /// </param>
        /// <param name="append">
        /// Whether to append to the existing file (true) or overwrite it (false).
        /// </param>
        /// <returns>
        /// A <see cref="StreamWriter"/> instance ready for writing.
        /// </returns>
        StreamWriter EnsureInitialized(string path, bool append = false);

        /// <summary>
        /// Writes a formatted log entry to the specified file.
        /// Implementations should handle stream initialization, writing, and flushing.
        /// </summary>
        /// <param name="path">The file path where the log entry will be written.</param>
        /// <param name="line">The formatted log entry line to write.</param>
        /// <param name="append">Whether to append to the existing file (true) or overwrite it (false).</param>
        void WriteLine(string path, string line, bool append = true);
    }
}
