using EldritchGames.EldritchLogger.Builder;
using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Exporting;
using EldritchGames.EldritchLogger.Mapper;
using EldritchGames.EldritchLogger.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public IEnumerable<ILogSink> Sinks => sinkManager.GetAllSinks();

        public EldritchLogger(LogSettings settings)
            : this(settings, new LogSinkFactory(settings)) { }

        internal EldritchLogger(LogSettings settings, LogSinkFactory sinkFactory)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

            mapper = new LogEntryMapper(settings);
            entryFactory = new LogEntryFactory();
            sinkManager = new SinkManager(sinkFactory);
            dispatcher = new LogDispatcher();
            cleaner = new LogFileCleaner(settings);

            if (settings.clearOnStartup)
                cleaner.DeletePreviousFiles();
        }

        public void Log(LogLevel level, LogCategory category, string message,
                        Dictionary<string, object> metadata = null, Exception exception = null)
        {
            if (!ShouldLog(level, category)) return;

            var entry = entryFactory.Create(level, category, message, metadata, exception);
            var dto = mapper.ToDto(entry);

            dispatcher.Dispatch(dto, sinkManager.GetAllSinks());
        }

        public void Log(LogLevel level, string categoryName, string message,
                        Dictionary<string, object> metadata = null, Exception exception = null)
        {
            if (!ShouldLog(level, categoryName)) return;

            var dto = new LogEntryDto
            {
                Timestamp = DateTime.Now,
                Level = level.ToString(),
                Category = categoryName,
                Message = message,
                Metadata = metadata?.Select(kv => new MetadataEntry { Key = kv.Key, Value = kv.Value?.ToString() }).ToList()
                           ?? new List<MetadataEntry>(),
                Exception = exception != null ? $"{exception.GetType().Name}: {exception.Message}" : null
            };

            dispatcher.Dispatch(dto, sinkManager.GetAllSinks());
        }

        public void Log(LogLevel level, Enum category, string message,
                        Dictionary<string, object> metadata = null, Exception exception = null) =>
            Log(level, category.ToString(), message, metadata, exception);

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

        private bool ShouldLog(LogLevel level, string categoryName)
        {
            if (level < settings.logLevel) return false;
            if (Enum.TryParse<LogCategory>(categoryName, out var cat))
                return settings.IsCategoryEnabled(cat);
            return settings.IsCustomCategoryEnabled(categoryName);
        }

        public void Dispose()
        {
            foreach (var sink in sinkManager.GetAllSinks())
                if (sink is IDisposable disposable)
                    disposable.Dispose();
        }
    }
}
