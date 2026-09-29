using EldritchGames.EldritchLogger.Sinks.Files;
using System;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Sinks.Config
{
    /// <summary>
    /// Shared options for file sinks: location, per-session files and retention.
    /// </summary>
    [Serializable]
    public abstract class FileSinkConfig : LogSinkConfig
    {
        [Tooltip("Target directory. Leave empty to use Application.persistentDataPath.")]
        public string directory = "";

        [Tooltip("File name without extension.")]
        public string fileName = "eldritch_logs";

        [Tooltip("Create a new time-stamped file per session instead of overwriting one file.")]
        public bool newFilePerSession = true;

        [Tooltip("With per-session files, how many session files to keep (including the current one).")]
        [Min(1)]
        public int maxSessionFiles = 5;

        [Tooltip("Maximum entries waiting to be written. When full, the oldest pending entry is dropped.")]
        [Min(1)]
        public int queueCapacity = BackgroundLogWriter.DefaultCapacity;

        /// <summary>Extension including the dot, e.g. <c>.jsonl</c>.</summary>
        protected abstract string Extension { get; }

        protected abstract ILogSink CreateFileSink(string path, SinkBuildContext context);

        public sealed override ILogSink CreateSink(SinkBuildContext context)
        {
            var locator = new LogFileLocator(directory, fileName, Extension);

            if (!newFilePerSession)
                return CreateFileSink(locator.SingleFilePath, context);

            locator.DeleteOldSessions(Math.Max(0, maxSessionFiles - 1));
            return CreateFileSink(locator.SessionFilePath(context.SessionStartUtc), context);
        }
    }

    [Serializable]
    public sealed class TextFileSinkConfig : FileSinkConfig
    {
        public override string DisplayName => "Text File";
        protected override string Extension => ".txt";

        public override string Preview(Dto.LogEntryDto sample, Settings.LogSettings settings) =>
            new Formatting.TextLogFormatter(settings, richText: false).Format(sample);

        protected override ILogSink CreateFileSink(string path, SinkBuildContext context) =>
            new TextFileSink(path, new Formatting.TextLogFormatter(context.Settings, richText: false),
                             minimumLevel, queueCapacity);
    }

    [Serializable]
    public sealed class JsonLinesFileSinkConfig : FileSinkConfig
    {
        public override string DisplayName => "JSON Lines File";
        protected override string Extension => ".jsonl";

        public override string Preview(Dto.LogEntryDto sample, Settings.LogSettings settings) => Dto.LogJson.Serialize(sample);

        protected override ILogSink CreateFileSink(string path, SinkBuildContext context) =>
            new JsonLinesFileSink(path, minimumLevel, queueCapacity);
    }

    [Serializable]
    public sealed class XmlFileSinkConfig : FileSinkConfig
    {
        public override string DisplayName => "XML File";
        protected override string Extension => ".xml";

        public override string Preview(Dto.LogEntryDto sample, Settings.LogSettings settings) => XmlFileSink.Format(sample);

        protected override ILogSink CreateFileSink(string path, SinkBuildContext context) =>
            new XmlFileSink(path, minimumLevel, queueCapacity);
    }
}
