using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Formatting;
using EldritchGames.EldritchLogger.Pipeline;
using EldritchGames.EldritchLogger.Sinks;
using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools;

namespace EldritchGames.EldritchLogger.Tests
{
    public class UnityConsoleSinkTests
    {
        private static LogEntryDto Entry(LogLevel level, string message) =>
            new() { Timestamp = DateTime.UtcNow, Level = level, Category = "General", Message = message };

        [Test]
        public void MapsLevelsToUnityLogTypes()
        {
            var sink = new UnityConsoleSink(new TextLogFormatter(null, richText: false));

            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("debug message$"));
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("info message$"));
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("warning message$"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("error message$"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("critical message$"));

            sink.Emit(Entry(LogLevel.Debug, "debug message"));
            sink.Emit(Entry(LogLevel.Info, "info message"));
            sink.Emit(Entry(LogLevel.Warning, "warning message"));
            sink.Emit(Entry(LogLevel.Error, "error message"));
            sink.Emit(Entry(LogLevel.Critical, "critical message"));
        }

        [Test]
        public void DoesNotReceiveEntriesForwardedFromUnity()
        {
            var sink = new UnityConsoleSink(new TextLogFormatter(null, richText: false));
            var forwarded = Entry(LogLevel.Error, "from unity");
            forwarded.Metadata.Add(new MetadataEntry { Key = LogPropertyKeys.Source, Value = LogPropertyKeys.UnitySource });

            Assert.That(sink, Is.InstanceOf<IShowsUnityLog>());
            using var logger = new EldritchLoggerBuilder().ClearEnrichers().AddSink(sink).Build();
            logger.Log(new Domain.LogEntry(LogLevel.Error, LogCategory.General, "from unity",
                new System.Collections.Generic.Dictionary<string, object> { [LogPropertyKeys.Source] = LogPropertyKeys.UnitySource }));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RequiresAFormatter()
        {
            Assert.Throws<ArgumentNullException>(() => new UnityConsoleSink(null));
        }
    }
}
