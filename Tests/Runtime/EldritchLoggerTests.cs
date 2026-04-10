using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Exporting;
using EldritchGames.EldritchLogger.Settings;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Tests
{
    // Stub sinks with names matching the real ones
    public class JsonLogSinkStub : ILogSink
    {
        public List<LogEntryDto> Calls = new();
        public SinkCategory Category => SinkCategory.Persistent;
        public void OnLogReceived(LogEntryDto dto) => Calls.Add(dto);
    }

    public class XmlLogSinkStub : ILogSink
    {
        public List<LogEntryDto> Calls = new();
        public SinkCategory Category => SinkCategory.Persistent;
        public void OnLogReceived(LogEntryDto dto) => Calls.Add(dto);
    }

    public class TextLogSinkStub : ILogSink
    {
        public List<LogEntryDto> Calls = new();
        public SinkCategory Category => SinkCategory.Persistent;
        public void OnLogReceived(LogEntryDto dto) => Calls.Add(dto);
    }

    [TestFixture]
    public class EldritchLoggerTests
    {
        private LogSettings CreateSettings(params ExportFormat[] formats)
        {
            var settings = ScriptableObject.CreateInstance<LogSettings>();
            settings.name = "TestLogger";
            settings.logLevel = LogLevel.Debug;
            settings.exportFormats = formats.ToList();
            settings.exportDirectory = "unused"; // irrelevant with stubs
            settings.exportFileName = "session";
            return settings;
        }

        [Test]
        public void Constructor_ShouldInitializeSinks()
        {
            var settings = CreateSettings(ExportFormat.Json, ExportFormat.Xml);
            using var logger = new Core.EldritchLogger(settings);

            // Json + Xml + UnityConsole
            Assert.That(logger.Sinks.Count(), Is.EqualTo(3));
            Assert.That(Core.EldritchLogger.Instance, Is.SameAs(logger));
        }

        [Test]
        public void Dispose_ShouldClearInstance()
        {
            var settings = CreateSettings(ExportFormat.Json);
            var logger = new Core.EldritchLogger(settings);

            logger.Dispose();

            Assert.That(Core.EldritchLogger.Instance, Is.Null);
        }

        [Test]
        public async Task Log_ShouldCallStubSink()
        {
            var settings = CreateSettings(ExportFormat.Json);
            using var logger = new Core.EldritchLogger(settings);

            var stub = new JsonLogSinkStub();
            logger.AddSink(SinkCategory.Persistent, stub);

            await logger.Log(LogLevel.Info, LogCategory.General, "Test message");

            Assert.That(stub.Calls.Count, Is.EqualTo(1));
            Assert.That(stub.Calls[0].Message, Is.EqualTo("Test message"));
        }

        [Test]
        public async Task Log_ShouldRespectLogLevel()
        {
            var settings = CreateSettings(ExportFormat.Json);
            settings.logLevel = LogLevel.Warning;

            using var logger = new Core.EldritchLogger(settings);

            var stub = new JsonLogSinkStub();
            logger.AddSink(SinkCategory.Persistent, stub);

            await logger.Log(LogLevel.Debug, LogCategory.General, "Should not log");

            Assert.That(stub.Calls.Count, Is.EqualTo(0));
        }


        [Test]
        public void AtDebug_ShouldReturnLogBuilder()
        {
            var settings = CreateSettings(ExportFormat.Json);
            using var logger = new Core.EldritchLogger(settings);

            var builder = logger.AtDebug(LogCategory.General);

            Assert.That(builder, Is.Not.Null);
            Assert.That(builder.GetType().Name, Is.EqualTo("LogBuilder"));
        }

        [Test]
        public void AtInfo_ShouldReturnLogBuilder()
        {
            var settings = CreateSettings(ExportFormat.Json);
            using var logger = new Core.EldritchLogger(settings);

            var builder = logger.AtInfo(LogCategory.General);

            Assert.That(builder, Is.Not.Null);
        }

        [Test]
        public void AtWarning_ShouldReturnLogBuilder()
        {
            var settings = CreateSettings(ExportFormat.Json);
            using var logger = new Core.EldritchLogger(settings);

            var builder = logger.AtWarning(LogCategory.General);

            Assert.That(builder, Is.Not.Null);
        }

        [Test]
        public void AtError_ShouldReturnLogBuilder()
        {
            var settings = CreateSettings(ExportFormat.Json);
            using var logger = new Core.EldritchLogger(settings);

            var builder = logger.AtError(LogCategory.General);

            Assert.That(builder, Is.Not.Null);
        }

        [Test]
        public void AtCritical_ShouldReturnLogBuilder()
        {
            var settings = CreateSettings(ExportFormat.Json);
            using var logger = new Core.EldritchLogger(settings);

            var builder = logger.AtCritical(LogCategory.General);

            Assert.That(builder, Is.Not.Null);
        }
    }
}
