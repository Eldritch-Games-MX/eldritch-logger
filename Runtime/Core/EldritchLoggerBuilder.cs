using EldritchGames.EldritchLogger.Mapper;
using EldritchGames.EldritchLogger.Pipeline;
using EldritchGames.EldritchLogger.Settings;
using EldritchGames.EldritchLogger.Sinks;
using EldritchGames.EldritchLogger.Sinks.Config;
using System;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Core
{
    /// <summary>
    /// Composition root for <see cref="EldritchLogger"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// var logger = EldritchLoggerBuilder.FromSettings(settings)
    ///     .AddEnricher(new PlayerIdEnricher())
    ///     .AddSink(new MyAnalyticsSink())
    ///     .Build();
    /// </code>
    /// </example>
    public sealed class EldritchLoggerBuilder
    {
        private readonly List<ILogEnricher> enrichers = new();
        private readonly List<ILogSink> sinks = new();
        private readonly List<LogSinkConfig> sinkConfigs = new();
        private LogSettings settings;
        private ILogFilter filter;
        private ILogEntryMapper mapper;
        private ILogDispatcher dispatcher;
        private IClock clock = SystemClock.Instance;
        private UnityLogCapture unityLogCapture = UnityLogCapture.Off;

        /// <summary>
        /// Starts from a settings asset: settings-based filter, default enrichers (scene, build version)
        /// and one sink per enabled <see cref="LogSettings.sinks"/> entry.
        /// Must be called on the main thread.
        /// </summary>
        public static EldritchLoggerBuilder FromSettings(LogSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var builder = new EldritchLoggerBuilder
            {
                settings = settings,
                filter = new SettingsLogFilter(settings),
                mapper = new LogEntryMapper(settings.filterLoggerFrames),
                unityLogCapture = settings.captureUnityLogs
            };
            builder.AddEnricher(new SceneEnricher());
            builder.AddEnricher(new BuildVersionEnricher());

            if (settings.sinks != null)
                foreach (var config in settings.sinks)
                    if (config != null && config.enabled)
                        builder.sinkConfigs.Add(config);

            return builder;
        }

        public EldritchLoggerBuilder WithFilter(ILogFilter filter)
        {
            this.filter = filter ?? throw new ArgumentNullException(nameof(filter));
            return this;
        }

        public EldritchLoggerBuilder WithMapper(ILogEntryMapper mapper)
        {
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            return this;
        }

        public EldritchLoggerBuilder WithDispatcher(ILogDispatcher dispatcher)
        {
            this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            return this;
        }

        /// <summary>Forwards Unity's own log messages (errors, exceptions...) into the logger.</summary>
        public EldritchLoggerBuilder CaptureUnityLogs(UnityLogCapture capture)
        {
            unityLogCapture = capture;
            return this;
        }

        public EldritchLoggerBuilder WithClock(IClock clock)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            return this;
        }

        public EldritchLoggerBuilder AddEnricher(ILogEnricher enricher)
        {
            enrichers.Add(enricher ?? throw new ArgumentNullException(nameof(enricher)));
            return this;
        }

        public EldritchLoggerBuilder ClearEnrichers()
        {
            foreach (var enricher in enrichers)
                (enricher as IDisposable)?.Dispose();
            enrichers.Clear();
            return this;
        }

        public EldritchLoggerBuilder AddSink(ILogSink sink)
        {
            sinks.Add(sink ?? throw new ArgumentNullException(nameof(sink)));
            return this;
        }

        public EldritchLoggerBuilder AddSink(LogSinkConfig config)
        {
            sinkConfigs.Add(config ?? throw new ArgumentNullException(nameof(config)));
            return this;
        }

        /// <summary>Removes sinks and sink configs added so far (including those from settings).</summary>
        public EldritchLoggerBuilder ClearSinks()
        {
            sinks.Clear();
            sinkConfigs.Clear();
            return this;
        }

        public EldritchLogger Build()
        {
            var context = new SinkBuildContext(settings, clock);
            var allSinks = new List<ILogSink>(sinks);

            foreach (var config in sinkConfigs)
            {
                try
                {
                    allSinks.Add(config.CreateSink(context));
                }
                catch (Exception ex)
                {
                    SelfLog.Report($"Could not create sink '{config.DisplayName}'", ex);
                }
            }

            var logger = new EldritchLogger(
                filter ?? AcceptAllFilter.Instance,
                enrichers,
                mapper ?? new LogEntryMapper(),
                dispatcher ?? new LogDispatcher(),
                clock,
                allSinks);

            if (unityLogCapture != UnityLogCapture.Off)
                logger.Own(new UnityLogForwarder(logger, unityLogCapture));

            return logger;
        }

        private sealed class AcceptAllFilter : ILogFilter
        {
            public static readonly AcceptAllFilter Instance = new();
            public bool IsEnabled(LogLevel level, LogCategory category) => true;
        }
    }
}
