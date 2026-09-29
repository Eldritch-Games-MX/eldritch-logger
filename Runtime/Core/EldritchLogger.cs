using EldritchGames.EldritchLogger.Domain;
using EldritchGames.EldritchLogger.Mapper;
using EldritchGames.EldritchLogger.Pipeline;
using EldritchGames.EldritchLogger.Sinks;
using System;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Core
{
    /// <summary>
    /// The root logger: filter → enrich → map → dispatch to sinks.
    /// Build it with <see cref="EldritchLoggerBuilder"/>; every collaborator is injected.
    /// </summary>
    public sealed class EldritchLogger : IEldritchLogger, ISinkRegistry, IDisposable
    {
        private readonly ILogFilter filter;
        private readonly ILogEnricher[] enrichers;
        private readonly ILogEntryMapper mapper;
        private readonly ILogDispatcher dispatcher;
        private readonly IClock clock;
        private readonly SinkCollection sinks;
        private readonly List<IDisposable> owned = new();
        private bool disposed;

        public IReadOnlyList<ILogSink> Sinks => sinks.Snapshot;

        IReadOnlyList<ILogSink> ISinkRegistry.All => sinks.Snapshot;

        /// <summary>
        /// Runtime overrides (level, categories) when the logger filters with a <see cref="SettingsLogFilter"/>
        /// (the default for loggers built from settings); otherwise null.
        /// </summary>
        public ILogControl Control { get; }

        public EldritchLogger(ILogFilter filter,
                              IEnumerable<ILogEnricher> enrichers,
                              ILogEntryMapper mapper,
                              ILogDispatcher dispatcher,
                              IClock clock,
                              IEnumerable<ILogSink> sinks)
        {
            this.filter = filter ?? throw new ArgumentNullException(nameof(filter));
            this.enrichers = enrichers != null ? new List<ILogEnricher>(enrichers).ToArray() : Array.Empty<ILogEnricher>();
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.sinks = new SinkCollection(sinks);
            Control = filter is SettingsLogFilter settingsFilter ? new SettingsLogControl(settingsFilter, Flush) : null;
        }

        public bool IsEnabled(LogLevel level, LogCategory category) =>
            !disposed && level >= sinks.LowestMinimumLevel && filter.IsEnabled(level, category);

        public void Log(LogEntry entry)
        {
            if (entry == null || !IsEnabled(entry.Level, entry.Category)) return;

            try
            {
                if (entry.TimestampUtc == default)
                    entry = entry.WithTimestamp(clock.UtcNow);

                // Copy the properties only when something will add to them.
                if (enrichers.Length > 0 || LogScope.HasActiveScopes)
                    entry = Enrich(entry);

                // Entries captured from Unity's log skip sinks that already show it (IShowsUnityLog). The logger picks
                // the targets and marks the thread, so any ILogDispatcher gets both protections.
                var targets = IsFromUnity(entry) ? sinks.SnapshotForUnityEntries : sinks.Snapshot;
                var dto = mapper.ToDto(entry);
                bool wasDispatching = LogDispatcher.BeginDispatch();
                try
                {
                    dispatcher.Dispatch(dto, targets);
                }
                finally
                {
                    LogDispatcher.EndDispatch(wasDispatching);
                }
            }
            catch (Exception ex)
            {
                SelfLog.Report("Failed to process log entry", ex);
            }
        }

        private static bool IsFromUnity(LogEntry entry) =>
            entry.Properties.TryGetValue(LogPropertyKeys.Source, out var source) &&
            source as string == LogPropertyKeys.UnitySource;

        /// <summary>Entry properties, then scope properties (only missing keys), then enrichers.</summary>
        private LogEntry Enrich(LogEntry entry)
        {
            var properties = new Dictionary<string, object>(entry.Properties.Count + enrichers.Length + 4);
            foreach (var kv in entry.Properties)
                properties[kv.Key] = kv.Value;

            LogScope.CopyTo(properties);

            foreach (var enricher in enrichers)
            {
                try
                {
                    enricher.Enrich(entry, properties);
                }
                catch (Exception ex)
                {
                    SelfLog.Report($"Enricher '{enricher.GetType().Name}' failed", ex);
                }
            }

            return entry.WithProperties(properties);
        }

        public void AddSink(ILogSink sink) => sinks.AddSink(sink);

        public void RemoveSink(ILogSink sink) => sinks.RemoveSink(sink);

        /// <summary>Flushes every buffering sink. Never throws (it also runs from the unhandled-exception handler).</summary>
        public void Flush()
        {
            foreach (var sink in sinks.Snapshot)
            {
                if (sink is not IFlushableSink flushable) continue;
                try
                {
                    flushable.Flush();
                }
                catch (Exception ex)
                {
                    SelfLog.Report($"Sink '{sink.Name}' failed to flush", ex);
                }
            }
        }

        /// <summary>Ties <paramref name="resource"/> (e.g. a log forwarder) to this logger's lifetime.</summary>
        internal void Own(IDisposable resource)
        {
            lock (owned) owned.Add(resource);
        }

        /// <summary>Stops owned forwarders, then disposes every sink and enricher. Further entries are ignored.</summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            lock (owned)
                foreach (var resource in owned)
                    DisposeQuietly(resource);

            foreach (var sink in sinks.Snapshot)
                DisposeQuietly(sink);
            foreach (var enricher in enrichers)
                DisposeQuietly(enricher);
        }

        private static void DisposeQuietly(object candidate)
        {
            if (candidate is not IDisposable disposable) return;
            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                SelfLog.Report($"Failed to dispose '{candidate.GetType().Name}'", ex);
            }
        }
    }
}
