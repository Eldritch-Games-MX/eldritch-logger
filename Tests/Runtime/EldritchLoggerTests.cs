using EldritchGames.EldritchLogger;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Exporting;
using EldritchGames.EldritchLogger.Settings;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Tests
{
    // Stub exporters with names matching the real ones
    public class JsonLogExporterStub : ILogExporter
    {
        public List<(object dto, string path)> Calls = new();
        public void Export(LogEntryDto dto, string path) => Calls.Add((dto, path));
    }

    public class XmlLogExporterStub : ILogExporter
    {
        public List<(object dto, string path)> Calls = new();
        public void Export(LogEntryDto dto, string path) => Calls.Add((dto, path));
    }

    public class TextLogExporterStub : ILogExporter
    {
        public List<(object dto, string path)> Calls = new();
        public void Export(LogEntryDto dto, string path) => Calls.Add((dto, path));
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

        private void InjectStubExporters(EldritchLogger logger, params ILogExporter[] stubs)
        {
            var exportersField = typeof(EldritchLogger)
                .GetField("exporters", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            exportersField.SetValue(logger, stubs.ToList());
        }

        [Test]
        public void Constructor_ShouldInitializeExporters()
        {
            var settings = CreateSettings(ExportFormat.Json, ExportFormat.Xml);
            using var logger = new EldritchLogger(settings);

            Assert.That(logger.Exporters.Count(), Is.EqualTo(2));
            Assert.That(EldritchLogger.Instance, Is.SameAs(logger));
        }

        [Test]
        public void Dispose_ShouldClearInstance()
        {
            var settings = CreateSettings(ExportFormat.Json);
            var logger = new EldritchLogger(settings);

            logger.Dispose();

            Assert.That(EldritchLogger.Instance, Is.Null);
        }

        [Test]
        public void Log_ShouldCallStubExporter()
        {
            var settings = CreateSettings(ExportFormat.Json);
            using var logger = new EldritchLogger(settings);

            var stub = new JsonLogExporterStub();
            InjectStubExporters(logger, stub);

            logger.Log(LogLevel.Info, LogCategory.General, "Test message");

            Assert.That(stub.Calls.Count, Is.EqualTo(1));
            Assert.That(stub.Calls[0].path, Does.Contain("session.json"));
        }

        [Test]
        public void Log_ShouldRespectLogLevel()
        {
            var settings = CreateSettings(ExportFormat.Json);
            settings.logLevel = LogLevel.Warning;

            using var logger = new EldritchLogger(settings);

            var stub = new JsonLogExporterStub();
            InjectStubExporters(logger, stub);

            logger.Log(LogLevel.Debug, LogCategory.General, "Should not log");

            Assert.That(stub.Calls.Count, Is.EqualTo(0));
        }

        [Test]
        public void AtDebug_ShouldReturnLogBuilder()
        {
            var settings = CreateSettings(ExportFormat.Json);
            using var logger = new EldritchLogger(settings);

            var builder = logger.AtDebug(LogCategory.General);

            Assert.That(builder, Is.Not.Null);
            Assert.That(builder.GetType().Name, Is.EqualTo("LogBuilder"));
        }

        [Test]
        public void AtInfo_ShouldReturnLogBuilder()
        {
            var settings = CreateSettings(ExportFormat.Json);
            using var logger = new EldritchLogger(settings);

            var builder = logger.AtInfo(LogCategory.General);

            Assert.That(builder, Is.Not.Null);
        }

        [Test]
        public void AtWarning_ShouldReturnLogBuilder()
        {
            var settings = CreateSettings(ExportFormat.Json);
            using var logger = new EldritchLogger(settings);

            var builder = logger.AtWarning(LogCategory.General);

            Assert.That(builder, Is.Not.Null);
        }

        [Test]
        public void AtError_ShouldReturnLogBuilder()
        {
            var settings = CreateSettings(ExportFormat.Json);
            using var logger = new EldritchLogger(settings);

            var builder = logger.AtError(LogCategory.General);

            Assert.That(builder, Is.Not.Null);
        }

        [Test]
        public void AtCritical_ShouldReturnLogBuilder()
        {
            var settings = CreateSettings(ExportFormat.Json);
            using var logger = new EldritchLogger(settings);

            var builder = logger.AtCritical(LogCategory.General);

            Assert.That(builder, Is.Not.Null);
        }
    }
}
