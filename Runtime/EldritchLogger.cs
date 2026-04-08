using EldritchGames.EldritchLogger;
using EldritchGames.EldritchLogger.Builder;
using EldritchGames.EldritchLogger.Exporting;
using EldritchGames.EldritchLogger.Mapper;
using EldritchGames.EldritchLogger.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace EldritchGames.EldritchLogger
{
    public class EldritchLogger : IEldritchLogger, IDisposable
    {
        private readonly LogSettings settings;
        private readonly IEnumerable<ILogExporter> exporters;
        private readonly ILogEntryMapper mapper;

        public IEnumerable<ILogExporter> Exporters => exporters;

        public static EldritchLogger Instance { get; private set; }

        public EldritchLogger(LogSettings settings)
        {
            Instance = this;
            this.settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
            mapper = new LogEntryMapper(this.settings);

            exporters = settings.exportFormats
                .Select<ExportFormat, ILogExporter>(fmt => fmt switch
                {
                    ExportFormat.Json => new JsonLogExporter(),
                    ExportFormat.Xml => new XmlLogExporter(),
                    ExportFormat.Text => new TextLogExporter(settings),
                    ExportFormat.None => throw new NotImplementedException(),
                    _ => throw new NotSupportedException($"Unsupported format {fmt}")
                })
                .Append(new UnityConsoleExporter(settings))
                .ToList();
            DeletePreviousSession();
        }

        public void Dispose()
        {
            Instance = null;
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

        public void Log(LogLevel level, LogCategory category, string message,
                        Dictionary<string, object> metadata = null, Exception exception = null)
        {
            if (!ShouldLog(level, category)) return;

            var entry = BuildLogEntry(level, category, message, metadata, exception);
            var mappedEntry = mapper.ToDto(entry);
            foreach (var fmt in settings.exportFormats)
            {
                var exporter = exporters.First(e => e.GetType().Name.StartsWith(fmt.ToString(), StringComparison.OrdinalIgnoreCase));
                var path = ResolvePathForFormat(fmt);
                exporter.Export(mappedEntry, path);
            }

            // Always export to Unity console
            var consoleExporter = exporters.OfType<UnityConsoleExporter>().FirstOrDefault();
            consoleExporter?.Export(mappedEntry, null);
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
    }
}