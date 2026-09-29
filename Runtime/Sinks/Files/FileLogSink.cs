using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using System;

namespace EldritchGames.EldritchLogger.Sinks.Files
{
    /// <summary>
    /// Base class for sinks that append serialized entries to a file through a
    /// <see cref="BackgroundLogWriter"/>. Subclasses only decide how an entry is serialized.
    /// </summary>
    public abstract class FileLogSink : ILogSink, IFlushableSink, ISinkDiagnostics, IDisposable
    {
        private readonly BackgroundLogWriter writer;

        public string Name { get; }
        public LogLevel MinimumLevel { get; }
        /// <summary>The file being written.</summary>
        public string Path => writer.Path;
        string ISinkDiagnostics.Location => writer.Path;
        public long DroppedCount => writer.DroppedCount;

        protected FileLogSink(string path, LogLevel minimumLevel, int queueCapacity)
        {
            MinimumLevel = minimumLevel;
            Name = $"{GetType().Name} ({System.IO.Path.GetFileName(path)})";
            writer = new BackgroundLogWriter(path, Serialize, Header, Footer, queueCapacity);
        }

        /// <summary>Text written at the start of the file.</summary>
        protected virtual string Header => null;

        /// <summary>Text written at the end of the file when the sink is disposed.</summary>
        protected virtual string Footer => null;

        /// <summary>
        /// Serializes one entry, including its trailing line break.
        /// Called on the writer thread; must not touch Unity APIs or <see cref="LogEntryDto.Context"/>.
        /// </summary>
        protected abstract string Serialize(LogEntryDto entry);

        public void Emit(LogEntryDto entry) => writer.Enqueue(entry);

        public void Flush() => writer.Flush(TimeSpan.FromSeconds(2));

        public void Dispose() => writer.Dispose();
    }
}
