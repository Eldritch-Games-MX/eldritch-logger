using EldritchGames.EldritchLogger.Console.Output;
using EldritchGames.EldritchLogger.Console.UI;
using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Formatting;
using EldritchGames.EldritchLogger.Sinks;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace EldritchGames.EldritchLogger.Console.Logging
{
    /// <summary>
    /// Log sink that forwards EldritchLogger entries to the in-game console.
    /// </summary>
    /// <remarks>
    /// Sinks can be invoked from any thread, so entries are formatted and queued here and only
    /// written to the view when <see cref="Flush"/> is called on the main thread. The queue is
    /// bounded: when it is full the oldest entry is dropped and the drop is reported on the next flush.
    /// </remarks>
    public sealed class ConsoleLogSink : ILogSink, ISinkDiagnostics
    {
        private readonly ILogFormatter formatter;
        private readonly ConcurrentQueue<string> pending = new();
        private readonly int capacity;
        private int count;
        private long dropped;
        private long totalDropped;

        public string Name => "In-game Console";
        public LogLevel MinimumLevel { get; }
        public long DroppedCount => Interlocked.Read(ref totalDropped);
        string ISinkDiagnostics.Location => null;

        public ConsoleLogSink(ILogFormatter formatter, LogLevel minimumLevel = LogLevel.Debug, int capacity = 1000)
        {
            this.formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            this.capacity = capacity;
            MinimumLevel = minimumLevel;
        }

        public void Emit(LogEntryDto entry)
        {
            // Command output mirrored to the logger is already on screen.
            if (entry.GetMetadata(ConsoleOutput.MirroredPropertyKey) != null) return;

            pending.Enqueue(formatter.Format(entry));

            if (Interlocked.Increment(ref count) > capacity && pending.TryDequeue(out _))
            {
                Interlocked.Decrement(ref count);
                Interlocked.Increment(ref dropped);
                Interlocked.Increment(ref totalDropped);
            }
        }

        /// <summary>Writes every queued entry to <paramref name="view"/>. Must be called on the main thread.</summary>
        public void Flush(IConsoleOutputView view)
        {
            long droppedNow = Interlocked.Exchange(ref dropped, 0);
            if (droppedNow > 0)
                view.AppendLog($"<color=#FFC107>... {droppedNow} log entries dropped (console queue full)</color>");

            while (pending.TryDequeue(out var line))
            {
                Interlocked.Decrement(ref count);
                view.AppendLog(line);
            }
        }
    }
}
