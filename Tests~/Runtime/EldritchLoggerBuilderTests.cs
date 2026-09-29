using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Settings;
using EldritchGames.EldritchLogger.Sinks;
using EldritchGames.EldritchLogger.Sinks.Config;
using NUnit.Framework;
using System;
using System.Linq;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Tests
{
    public class EldritchLoggerBuilderTests
    {
        [Serializable]
        private sealed class FailingSinkConfig : LogSinkConfig
        {
            public override string DisplayName => "Failing";
            public override ILogSink CreateSink(SinkBuildContext context) => throw new InvalidOperationException("nope");
        }

        [Serializable]
        private sealed class RecordingSinkConfig : LogSinkConfig
        {
            public override string DisplayName => "Recording";
            public override ILogSink CreateSink(SinkBuildContext context) => new RecordingSink(minimumLevel);
        }

        private LogSettings settings;

        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<LogSettings>();
            settings.sinks.Clear();
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(settings);

        [Test]
        public void FromSettings_CreatesEnabledSinks_AndReportsFailures()
        {
            using var capture = new SelfLogCapture();
            settings.sinks.Add(new RecordingSinkConfig());
            settings.sinks.Add(new RecordingSinkConfig { enabled = false });
            settings.sinks.Add(new FailingSinkConfig());

            using var logger = EldritchLoggerBuilder.FromSettings(settings).Build();

            Assert.That(logger.Sinks.Count, Is.EqualTo(1));
            Assert.That(capture.Messages, Has.Some.Contains("Failing"));
        }

        [Test]
        public void FromSettings_AddsSceneAndBuildVersion_AndFiltersBySettings()
        {
            var sink = new RecordingSink();
            settings.minimumLevel = LogLevel.Info;
            using var logger = EldritchLoggerBuilder.FromSettings(settings).AddSink(sink).Build();

            logger.Log(LogLevel.Debug, LogCategory.General, "filtered");
            logger.Log(LogLevel.Info, "Unregistered", "filtered");
            logger.Log(LogLevel.Info, LogCategory.General, "kept");

            Assert.That(sink.Entries.Select(e => e.Message), Is.EqualTo(new[] { "kept" }));
            Assert.That(sink.Entries[0].GetMetadata(LogPropertyKeys.BuildVersion), Is.EqualTo(Application.version));
            Assert.That(sink.Entries[0].Metadata.Any(m => m.Key == LogPropertyKeys.Scene), Is.True);
        }

        [Test]
        public void Build_WithoutSettings_AcceptsEverything()
        {
            var sink = new RecordingSink();
            using var logger = new EldritchLoggerBuilder().AddSink(sink).WithClock(new FakeClock()).Build();

            logger.Log(LogLevel.Debug, "Anything", "m");

            Assert.That(sink.Entries, Has.Count.EqualTo(1));
        }

        [Test]
        public void Builder_RejectsNulls()
        {
            var builder = new EldritchLoggerBuilder();
            Assert.Throws<ArgumentNullException>(() => builder.WithFilter(null));
            Assert.Throws<ArgumentNullException>(() => builder.WithMapper(null));
            Assert.Throws<ArgumentNullException>(() => builder.WithDispatcher(null));
            Assert.Throws<ArgumentNullException>(() => builder.WithClock(null));
            Assert.Throws<ArgumentNullException>(() => builder.AddEnricher(null));
            Assert.Throws<ArgumentNullException>(() => builder.AddSink((ILogSink)null));
            Assert.Throws<ArgumentNullException>(() => builder.AddSink((LogSinkConfig)null));
            Assert.Throws<ArgumentNullException>(() => EldritchLoggerBuilder.FromSettings(null));
        }

        [Test]
        public void ClearSinks_RemovesSinksAndConfigs()
        {
            var settings = ScriptableObject.CreateInstance<LogSettings>();
            try
            {
                using var logger = EldritchLoggerBuilder.FromSettings(settings).AddSink(new RecordingSink()).ClearSinks().Build();
                Assert.That(logger.Sinks.Count, Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }
    }
}
