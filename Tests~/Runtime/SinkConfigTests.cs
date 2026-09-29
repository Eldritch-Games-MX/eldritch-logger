using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Sinks;
using EldritchGames.EldritchLogger.Sinks.Config;
using EldritchGames.EldritchLogger.Sinks.Files;
using EldritchGames.EldritchLogger.Sinks.Network;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Tests
{
    public class SinkConfigTests
    {
        private string directory;
        private SinkBuildContext context;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "EldritchConfigs_" + Guid.NewGuid().ToString("N"));
            context = new SinkBuildContext(null, new FakeClock());
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        private static LogEntryDto Entry(int i, LogLevel level = LogLevel.Info) => new()
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Category = "Gameplay",
            Message = "message " + i
        };

        [TestCase(typeof(TextFileSinkConfig), typeof(TextFileSink), ".txt", "Text File")]
        [TestCase(typeof(JsonLinesFileSinkConfig), typeof(JsonLinesFileSink), ".jsonl", "JSON Lines File")]
        [TestCase(typeof(XmlFileSinkConfig), typeof(XmlFileSink), ".xml", "XML File")]
        public void FileConfigs_CreateTheirSink(Type configType, Type sinkType, string extension, string displayName)
        {
            var config = (FileSinkConfig)Activator.CreateInstance(configType);
            config.directory = directory;
            config.minimumLevel = LogLevel.Warning;

            using var sink = (FileLogSink)config.CreateSink(context);

            Assert.That(sink, Is.InstanceOf(sinkType));
            Assert.That(sink.MinimumLevel, Is.EqualTo(LogLevel.Warning));
            Assert.That(sink.Path, Does.EndWith(extension));
            Assert.That(sink.Name, Does.Contain(Path.GetFileName(sink.Path)));
            Assert.That(config.DisplayName, Is.EqualTo(displayName));
        }

        [Test]
        public void UnityConsoleConfig_CreatesAConsoleSink()
        {
            var config = new UnityConsoleSinkConfig { minimumLevel = LogLevel.Error, suppressUnityStackTrace = false };

            var sink = config.CreateSink(context);

            Assert.That(sink, Is.InstanceOf<UnityConsoleSink>());
            Assert.That(sink.MinimumLevel, Is.EqualTo(LogLevel.Error));
            Assert.That(config.DisplayName, Is.EqualTo("Unity Console"));
        }

        [Test]
        public void SuppressingStackTraces_ConfiguresUnity()
        {
            var previous = Application.GetStackTraceLogType(LogType.Log);
            try
            {
                new UnityConsoleSinkConfig { suppressUnityStackTrace = true }.CreateSink(context);
                Assert.That(Application.GetStackTraceLogType(LogType.Log), Is.EqualTo(StackTraceLogType.None));
            }
            finally
            {
                Application.SetStackTraceLogType(LogType.Log, previous);
                Application.SetStackTraceLogType(LogType.Warning, previous);
                Application.SetStackTraceLogType(LogType.Error, previous);
                Application.SetStackTraceLogType(LogType.Exception, previous);
            }
        }

        [Test]
        public void BuildContext_RequiresAClock_AndRecordsTheSessionStart()
        {
            var clock = new FakeClock();
            Assert.That(new SinkBuildContext(null, clock).SessionStartUtc, Is.EqualTo(clock.UtcNow));
            Assert.Throws<ArgumentNullException>(() => new SinkBuildContext(null, null));
        }

        [Test]
        public void FileSinkConfig_CreatesSessionFile_AndAppliesRetention()
        {
            var clock = new FakeClock();
            var config = new JsonLinesFileSinkConfig { directory = directory, fileName = "session", maxSessionFiles = 2 };

            for (int i = 0; i < 4; i++)
            {
                clock.UtcNow = clock.UtcNow.AddMinutes(1);
                using var sink = (JsonLinesFileSink)config.CreateSink(new SinkBuildContext(null, clock));
                sink.Emit(Entry(i));
            }

            Assert.That(Directory.GetFiles(directory, "session_*.jsonl"), Has.Length.EqualTo(2));
        }

        [Test]
        public void SingleFileMode_OverwritesTheSameFileEachRun()
        {
            var config = new TextFileSinkConfig { directory = directory, fileName = "single", newFilePerSession = false };
            var context = new SinkBuildContext(null, new FakeClock());

            for (int run = 0; run < 2; run++)
                using (var sink = (FileLogSink)config.CreateSink(context))
                    sink.Emit(new LogEntryDto { Timestamp = DateTime.UtcNow, Level = LogLevel.Info, Category = "C", Message = "run " + run });

            Assert.That(Directory.GetFiles(directory).Select(Path.GetFileName), Is.EqualTo(new[] { "single.txt" }));
            var text = File.ReadAllText(Path.Combine(directory, "single.txt"));
            Assert.That(text, Does.Contain("run 1").And.Not.Contain("run 0"));
        }

        [Test]
        public void HttpConfig_BuildsASink_OrRejectsABadUrl()
        {
            var context = new SinkBuildContext(null, new FakeClock());
            var config = new HttpSinkConfig { url = "http://localhost:5341/ingest" };
            config.headers.Add(new HttpSinkConfig.Header { name = "X-Key", value = "k" });

            using (var sink = (HttpLogSink)config.CreateSink(context))
                Assert.That(sink.Location, Is.EqualTo("http://localhost:5341/ingest"));

            Assert.Throws<ArgumentException>(() => new HttpSinkConfig { url = "not a url" }.CreateSink(context));
        }
    }
}
