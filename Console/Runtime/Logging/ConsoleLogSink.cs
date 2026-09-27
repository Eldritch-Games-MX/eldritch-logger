using EldritchGames.EldritchLogger.Console.Loader;
using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Format;
using EldritchGames.EldritchLogger.Settings;
using System;
using System.Collections.Concurrent;

namespace EldritchGames.EldritchLogger.Console.Logging
{
    /// <summary>
    /// Log sink that forwards EldritchLogger entries to the in-game console.
    /// </summary>
    /// <remarks>
    /// Sinks can be invoked from any thread, so entries are formatted and queued here
    /// and only written to the view when <see cref="Flush"/> is called from the main thread.
    /// </remarks>
    public class ConsoleLogSink : ILogSink
    {
        private readonly LogEntryFormatter formatter;
        private readonly ConcurrentQueue<string> pending = new();

        public SinkCategory Category => SinkCategory.Runtime;

        public ConsoleLogSink(LogSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            formatter = new LogEntryFormatter(settings);
        }

        public void OnLogReceived(LogEntryDto logEntry)
        {
            pending.Enqueue(formatter.Format(logEntry));
        }

        /// <summary>
        /// Writes every queued entry to <paramref name="view"/>. Must be called on the main thread.
        /// </summary>
        public void Flush(IConsoleView view)
        {
            while (pending.TryDequeue(out var line))
                view.AppendLog(line);
        }
    }
}
