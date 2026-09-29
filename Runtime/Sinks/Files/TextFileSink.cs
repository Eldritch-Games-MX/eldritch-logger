using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Formatting;
using System;

namespace EldritchGames.EldritchLogger.Sinks.Files
{
    /// <summary>Plain-text log file, one formatted entry per line.</summary>
    public sealed class TextFileSink : FileLogSink
    {
        private readonly ILogFormatter formatter;

        public TextFileSink(string path, ILogFormatter formatter,
                            LogLevel minimumLevel = LogLevel.Debug,
                            int queueCapacity = BackgroundLogWriter.DefaultCapacity)
            : base(path, minimumLevel, queueCapacity)
        {
            this.formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        }

        protected override string Serialize(LogEntryDto entry) => formatter.Format(entry) + "\n";
    }
}
