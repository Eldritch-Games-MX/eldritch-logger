using EldritchGames.EldritchLogger.Builder;
using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Exporting;
using EldritchGames.EldritchLogger.Mapper;
using EldritchGames.EldritchLogger.Settings;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EldritchGames.EldritchLogger.Core
{
    public class EldritchLogger : IEldritchLogger, IDisposable
    {
        private readonly LogSettings settings;
        private readonly ILogEntryMapper mapper;
        private readonly ILogEntryFactory entryFactory;
        private readonly ISinkManager sinkManager;
        private readonly ILogDispatcher dispatcher;
        private readonly LogFileCleaner cleaner;

        public static EldritchLogger Instance { get; private set; }
        public IEnumerable<ILogSink> Sinks => sinkManager.GetAllSinks();

        // Default constructor
        public EldritchLogger(LogSettings settings)
            : this(settings, new LogSinkFactory(settings)) { }

        // Overload for injecting a custom factory (used in tests)
        public EldritchLogger(LogSettings settings, LogSinkFactory sinkFactory)
        {
            Instance = this;
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

            mapper = new LogEntryMapper(settings);
            entryFactory = new LogEntryFactory();
            sinkManager = new SinkManager(sinkFactory);
            dispatcher = new LogDispatcher();
            cleaner = new LogFileCleaner(settings);

            if (settings.clearOnStartup)
                cleaner.DeletePreviousFiles();
        }

        public async Task Log(LogLevel level, LogCategory category, string message,
                              Dictionary<string, object> metadata = null, Exception exception = null)
        {
            if (!ShouldLog(level, category)) return;

            var entry = entryFactory.Create(level, category, message, metadata, exception);
            var dto = mapper.ToDto(entry);

            await dispatcher.Dispatch(dto, sinkManager.GetAllSinks());
        }

        public ILogBuilder AtDebug(LogCategory category = LogCategory.General) =>
            new LogBuilder(this, LogLevel.Debug, category);
        public ILogBuilder AtInfo(LogCategory category = LogCategory.General) =>
            new LogBuilder(this, LogLevel.Info, category);
        public ILogBuilder AtWarning(LogCategory category = LogCategory.General) =>
            new LogBuilder(this, LogLevel.Warning, category);
        public ILogBuilder AtError(LogCategory category = LogCategory.General) =>
            new LogBuilder(this, LogLevel.Error, category);
        public ILogBuilder AtCritical(LogCategory category = LogCategory.General) =>
            new LogBuilder(this, LogLevel.Critical, category);

        private bool ShouldLog(LogLevel level, LogCategory category) =>
            !(level < settings.logLevel || !settings.IsCategoryEnabled(category));

        public void Dispose()
        {
            Instance = null;
            foreach (var sink in sinkManager.GetAllSinks())
                if (sink is IDisposable disposable)
                    disposable.Dispose();
        }
    }
}
