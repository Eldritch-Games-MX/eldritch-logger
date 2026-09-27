using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;

namespace EldritchGames.EldritchLogger.Sinks
{
    /// <summary>
    /// A destination for log entries (Unity Console, a file, the in-game console, a server...).
    /// </summary>
    /// <remarks>
    /// <see cref="Emit"/> is called synchronously on the logging thread, which may not be the
    /// main thread. Implementations must return quickly (queue slow I/O) and must not mutate
    /// the entry, which is shared by every sink. Exceptions are caught and reported by the
    /// dispatcher so one faulty sink cannot block the others.
    /// </remarks>
    public interface ILogSink
    {
        /// <summary>Human-readable name used in diagnostics.</summary>
        string Name { get; }

        /// <summary>Entries below this level are not sent to this sink.</summary>
        LogLevel MinimumLevel { get; }

        void Emit(LogEntryDto entry);
    }

    /// <summary>Implemented by sinks that buffer entries.</summary>
    public interface IFlushableSink
    {
        void Flush();
    }
}
