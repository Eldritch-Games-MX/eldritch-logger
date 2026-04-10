using EldritchGames.EldritchLogger.Builder;
using EldritchGames.EldritchLogger.Domain;
using EldritchGames.EldritchLogger.Exporting;
using EldritchGames.EldritchLogger.Mapper;
using EldritchGames.EldritchLogger.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Core
{
    public class EldritchLogger : IEldritchLogger, IDisposable
    {
        private readonly LogSettings settings;
        private readonly ILogEntryMapper mapper;
        private readonly object syncRoot = new();
        private IDictionary<SinkCategory, List<ILogSink>> sinks;

        public static EldritchLogger Instance { get; private set; }
        public IEnumerable<ILogSink> Sinks => sinks.Values.SelectMany(list => list);

        public EldritchLogger(LogSettings settings)
        {
            Instance = this;
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            mapper = new LogEntryMapper(this.settings);
            sinks = LogSinkFactory.CreateSinks(settings, this);
            if (settings.clearOnStartup)
                DeletePreviousSession();
        }

        public void Dispose()
        {
            Instance = null;
            lock (syncRoot)
            {
                foreach (var sinkList in sinks.Values)
                    foreach (var sink in sinkList)
                        if (sink is IDisposable disposable)
                            disposable.Dispose();
            }
        }

        private void DeletePreviousSession()
        {
            foreach (var fmt in settings.exportFormats)
            {
                var path = ResolvePathForFormat(fmt);
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
        public async Task Log(LogLevel level, LogCategory category, string message,
                      Dictionary<string, object> metadata = null, Exception exception = null)
        {
            if (!ShouldLog(level, category)) return;

            var entry = BuildLogEntry(level, category, message, metadata, exception);
            var dto = mapper.ToDto(entry);

            foreach (var sinkList in sinks.Values)
            {
                foreach (var sink in sinkList)
                {
                    switch (sink)
                    {
                        case IAsyncLogExporter asyncSink:
                            // Await async file exporters (Text, JSON, XML)
                            await asyncSink.Export(dto, asyncSink.TargetPath);
                            break;

                        case ILogSink syncSink:
                            // Run UnityConsoleExporter synchronously
                            syncSink.OnLogReceived(dto);
                            break;
                    }
                }
            }
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
        private LogEntry BuildLogEntry(LogLevel level, LogCategory category, string message,
                                       Dictionary<string, object> metadata, Exception exception) =>
            new(
                DateTime.Now,
                level,
                category,
                message,
                metadata,
                exception
            );
        private string ResolvePathForFormat(ExportFormat fmt)
        {
            string dir = string.IsNullOrEmpty(settings.exportDirectory)
                ? Application.persistentDataPath
                : settings.exportDirectory;

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string ext = fmt switch
            {
                ExportFormat.Json => ".json",
                ExportFormat.Xml => ".xml",
                ExportFormat.Text => ".txt",
                _ => ".log"
            };

            return Path.Combine(dir, settings.exportFileName + ext);
        }

        public void AddSink(SinkCategory category, ILogSink sink)
        {
            lock (syncRoot)
            {
                if (!sinks.ContainsKey(category))
                    sinks[category] = new List<ILogSink>();
                sinks[category].Add(sink);
            }
        }

        public void RemoveSink(SinkCategory category, ILogSink sink)
        {
            lock (syncRoot)
            {
                if (sinks.ContainsKey(category))
                    sinks[category].Remove(sink);
            }
        }

        public IEnumerable<ILogSink> GetSinks(SinkCategory category)
        {
            lock (syncRoot)
            {
                return sinks.ContainsKey(category) ? sinks[category] : Array.Empty<ILogSink>();
            }
        }


        public string GetExportPath(ExportFormat fmt)
        {
            string directory = string.IsNullOrEmpty(settings.exportDirectory)
                ? Application.persistentDataPath
                : settings.exportDirectory;

            string extension = fmt switch
            {
                ExportFormat.Json => ".json",
                ExportFormat.Xml => ".xml",
                ExportFormat.Text => ".txt",
                _ => ".log"
            };

            return Path.Combine(directory, settings.exportFileName + extension);
        }
    }
}
