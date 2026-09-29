using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Domain;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Pipeline;
using EldritchGames.EldritchLogger.Sinks;
using System;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Tests
{
    internal sealed class RecordingSink : ILogSink, IDisposable
    {
        public readonly List<LogEntryDto> Entries = new();
        public bool Disposed;

        public RecordingSink(LogLevel minimumLevel = LogLevel.Debug, string name = "Recording")
        {
            MinimumLevel = minimumLevel;
            Name = name;
        }

        public string Name { get; }
        public LogLevel MinimumLevel { get; }
        public void Emit(LogEntryDto entry)
        {
            lock (Entries) Entries.Add(entry);
        }
        public void Dispose() => Disposed = true;
    }

    internal sealed class ThrowingSink : ILogSink
    {
        public string Name => "Throwing";
        public LogLevel MinimumLevel => LogLevel.Debug;
        public void Emit(LogEntryDto entry) => throw new InvalidOperationException("boom");
    }

    internal sealed class FakeClock : IClock
    {
        public DateTime UtcNow { get; set; } = new(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
    }

    internal sealed class DelegateEnricher : ILogEnricher
    {
        private readonly Action<LogEntry, IDictionary<string, object>> action;
        public DelegateEnricher(Action<LogEntry, IDictionary<string, object>> action) => this.action = action;
        public void Enrich(LogEntry entry, IDictionary<string, object> properties) => action(entry, properties);
    }

    internal sealed class DelegateFilter : ILogFilter
    {
        private readonly Func<LogLevel, LogCategory, bool> predicate;
        public DelegateFilter(Func<LogLevel, LogCategory, bool> predicate) => this.predicate = predicate;
        public bool IsEnabled(LogLevel level, LogCategory category) => predicate(level, category);
    }

    /// <summary>Captures SelfLog output for the duration of a test.</summary>
    internal sealed class SelfLogCapture : IDisposable
    {
        public readonly List<string> Messages = new();

        public SelfLogCapture()
        {
            SelfLog.Reset();
            SelfLog.Output = Messages.Add;
        }

        public void Dispose()
        {
            SelfLog.Output = null;
            SelfLog.Reset();
        }
    }
}
