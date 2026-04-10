using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Settings;
using System;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Exporting
{
    public class LogSinkFactory
    {
        private readonly LogSettings settings;
        private readonly LogPathResolver pathResolver;
        private readonly ILogFileWriter fileWriter;

        public LogSinkFactory(LogSettings settings)
        {
            this.settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
            this.pathResolver = new LogPathResolver(settings);
            this.fileWriter = new LogFileWriter();
        }

        public IDictionary<SinkCategory, List<ILogSink>> CreateSinks()
        {
            var sinks = new Dictionary<SinkCategory, List<ILogSink>>();

            void AddSink(ILogSink sink)
            {
                if (!sinks.ContainsKey(sink.Category))
                    sinks[sink.Category] = new List<ILogSink>();
                sinks[sink.Category].Add(sink);
            }

            foreach (var fmt in settings.exportFormats)
            {
                string path = pathResolver.ResolvePath(fmt);

                switch (fmt)
                {
                    case ExportFormat.Json:
                        AddSink(new JsonLogExporter(path, fileWriter));
                        break;
                    case ExportFormat.Xml:
                        AddSink(new XmlLogExporter(path, fileWriter));
                        break;
                    case ExportFormat.Text:
                        AddSink(new TextLogExporter(path, settings, fileWriter));
                        break;
                }
            }

            AddSink(new UnityConsoleExporter(settings));

            return sinks;
        }
    }
}
