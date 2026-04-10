using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Exporting;
using EldritchGames.EldritchLogger.Settings;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Exporting
{
    public static class LogSinkFactory
    {
        public static IDictionary<SinkCategory, List<ILogSink>> CreateSinks(LogSettings settings, Core.EldritchLogger logger)
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
                switch (fmt)
                {
                    case ExportFormat.Json:
                        AddSink(new JsonLogExporter(logger.GetExportPath(ExportFormat.Json), new LogFileWriter()));
                        break;
                    case ExportFormat.Xml:
                        AddSink(new XmlLogExporter(logger.GetExportPath(ExportFormat.Xml), new LogFileWriter()));
                        break;
                    case ExportFormat.Text:
                        AddSink(new TextLogExporter(settings, new LogFileWriter()));
                        break;
                }
            }

            AddSink(new UnityConsoleExporter(settings));

            return sinks;
        }
    }

}
