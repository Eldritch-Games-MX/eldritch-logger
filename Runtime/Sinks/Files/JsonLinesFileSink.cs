using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using Newtonsoft.Json;

namespace EldritchGames.EldritchLogger.Sinks.Files
{
    /// <summary>
    /// JSON Lines file (<c>.jsonl</c>): one JSON object per line. Every line is valid on its own,
    /// so the file stays readable after a crash.
    /// </summary>
    public sealed class JsonLinesFileSink : FileLogSink
    {
        private static readonly JsonSerializerSettings SerializerSettings = new()
        {
            Formatting = Newtonsoft.Json.Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };

        public JsonLinesFileSink(string path,
                                 LogLevel minimumLevel = LogLevel.Debug,
                                 int queueCapacity = BackgroundLogWriter.DefaultCapacity)
            : base(path, minimumLevel, queueCapacity) { }

        protected override string Serialize(LogEntryDto entry) =>
            JsonConvert.SerializeObject(entry, SerializerSettings) + "\n";
    }
}
