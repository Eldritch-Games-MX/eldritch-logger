using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Domain;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Exporting;
using EldritchGames.EldritchLogger.Format;
using EldritchGames.EldritchLogger.Mapper;
using EldritchGames.EldritchLogger.Settings;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;

namespace EldritchGames.EldritchLogger.Tests
{
    [TestFixture]
    public class FileExporterAsyncTests
    {
        private LogEntry sampleEntry;
        private string tempDir;
        private ILogEntryMapper mapper;

        [SetUp]
        public void Setup()
        {
            tempDir = Path.Combine(Path.GetTempPath(), "FileExporterAsyncTests");
            Directory.CreateDirectory(tempDir);

            sampleEntry = new LogEntry(
                DateTime.Parse("2026-04-08T10:00:00"),
                LogLevel.Info,
                LogCategory.Gameplay,
                "Export test message",
                new Dictionary<string, object> { { "Key", "Value" } },
                new InvalidOperationException("Boom!")
            );

            mapper = new LogEntryMapper(ScriptableObject.CreateInstance<LogSettings>());
        }

        [TearDown]
        public void Cleanup()
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }

        [Test]
        public async Task TextExporter_WritesPlainText()
        {
            var settings = ScriptableObject.CreateInstance<LogSettings>();
            settings.exportDirectory = tempDir;
            settings.exportFileName = "log";

            var exporter = new TextLogExporter(settings, new LogFileWriter());
            string path = Path.Combine(tempDir, "log.txt");

            var dto = mapper.ToDto(sampleEntry);
            await exporter.Export(dto, path);

            exporter.Dispose();

            string content = File.ReadAllText(path);
            StringAssert.Contains("Export test message", content);
            StringAssert.Contains("Boom!", content);
        }

        [Test]
        public async Task JsonExporter_WritesValidJson()
        {
            string path = Path.Combine(tempDir, "log.json");
            var exporter = new JsonLogExporter(path, new LogFileWriter());

            var dto = mapper.ToDto(sampleEntry);
            await exporter.Export(dto, path);
            exporter.Dispose();

            string content = File.ReadAllText(path);
            StringAssert.Contains("\"Message\": \"Export test message\"", content);
            StringAssert.Contains("Boom!", content);
            StringAssert.Contains("[", content);
            StringAssert.Contains("]", content);
        }

        [Test]
        public async Task XmlExporter_WritesValidXml()
        {
            string path = Path.Combine(tempDir, "log.xml");
            var exporter = new XmlLogExporter(path, new LogFileWriter());

            var dto = mapper.ToDto(sampleEntry);
            await exporter.Export(dto, path);
            exporter.Dispose();

            string content = File.ReadAllText(path);
            StringAssert.Contains("<Message>Export test message</Message>", content);
            StringAssert.Contains("Boom!", content);
            StringAssert.Contains("<Logs>", content);
            StringAssert.Contains("</Logs>", content);
        }

    }

    [TestFixture]
    public class UnityConsoleExporterSyncTests
    {
        private ILogEntryMapper mapper;

        [SetUp]
        public void Setup()
        {
            mapper = new LogEntryMapper(ScriptableObject.CreateInstance<LogSettings>());
        }

        [TestCase(LogLevel.Debug, LogType.Log)]
        [TestCase(LogLevel.Info, LogType.Log)]
        [TestCase(LogLevel.Warning, LogType.Warning)]
        [TestCase(LogLevel.Error, LogType.Error)]
        [TestCase(LogLevel.Critical, LogType.Error)]
        public void UnityConsoleExporter_LogsAllLevels(LogLevel level, LogType expectedType)
        {
            var exporter = new UnityConsoleExporter(ScriptableObject.CreateInstance<LogSettings>());
            var entry = new LogEntry(DateTime.Now, level, LogCategory.General, $"{level} message",
                                     null, level == LogLevel.Error || level == LogLevel.Critical ? new InvalidOperationException("Boom!") : null);

            var dto = mapper.ToDto(entry);
            var formatter = new LogEntryFormatter(ScriptableObject.CreateInstance<LogSettings>());
            string expected = formatter.Format(dto);

            LogAssert.Expect(expectedType, expected);

            // Run synchronously on Unity’s main thread
            exporter.OnLogReceived(dto);
        }
    }
}
