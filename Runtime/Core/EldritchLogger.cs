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
        private bool disposed;

        public IReadOnlyList<ILogSink> Sinks => sinks.Snapshot;

        IReadOnlyList<ILogSink> ISinkRegistry.All => sinks.Snapshot;

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

                if (enrichers.Length > 0)
                    entry = Enrich(entry);

                dispatcher.Dispatch(mapper.ToDto(entry), sinks.Snapshot);
            }
            catch (Exception ex)
            {
                SelfLog.Report("Failed to process log entry", ex);
            }
        }

        private LogEntry Enrich(LogEntry entry)
        {
            var properties = new Dictionary<string, object>(entry.Properties.Count + enrichers.Length);
            foreach (var kv in entry.Properties)
                properties[kv.Key] = kv.Value;

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

        /// <summary>Flushes every buffering sink.</summary>
        public void Flush()
        {
            foreach (var sink in sinks.Snapshot)
                if (sink is IFlushableSink flushable) flushable.Flush();
        }

        /// <summary>Disposes every sink and enricher. Further entries are ignored.</summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

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
