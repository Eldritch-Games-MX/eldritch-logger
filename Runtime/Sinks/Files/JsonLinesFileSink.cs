using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;

namespace EldritchGames.EldritchLogger.Sinks.Files
{
    /// <summary>
    /// JSON Lines file (<c>.jsonl</c>): one JSON object per line. Every line is valid on its own,
    /// so the file stays readable after a crash.
    /// </summary>
    public sealed class JsonLinesFileSink : FileLogSink
    {
        public JsonLinesFileSink(string path,
                                 LogLevel minimumLevel = LogLevel.Debug,
                                 int queueCapacity = BackgroundLogWriter.DefaultCapacity)
            : base(path, minimumLevel, queueCapacity) { }

        protected override string Serialize(LogEntryDto entry) => LogJson.Serialize(entry) + "\n";
    }
}
