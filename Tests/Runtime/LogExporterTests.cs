using EldritchGames.EldritchLogger;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Exporting;
using EldritchGames.EldritchLogger.Format;
using EldritchGames.EldritchLogger.Mapper;
using EldritchGames.EldritchLogger.Settings;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.TestTools;

namespace EldritchGames.EldritchLogger.Tests
{
    [TestFixture]
    public class LogExporterTests
    {
        private LogEntry sampleEntry;
        private string tempDir;
        private ILogEntryMapper mapper;

        [SetUp]
        public void Setup()
        {
            tempDir = Path.Combine(Path.GetTempPath(), "LoggerExporterTests");
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
        public void JsonExporter_WritesValidJson()
        {
            var exporter = new JsonLogExporter();
            string path = Path.Combine(tempDir, "log.json");

            var dto = mapper.ToDto(sampleEntry);
            exporter.Export(dto, path);
            exporter.Dispose(); // ensure closing brackets written

            string content = File.ReadAllText(path);
            StringAssert.Contains("\"Message\": \"Export test message\"", content);
            StringAssert.Contains("Boom!", content);
            StringAssert.Contains("[", content); // root array
            StringAssert.Contains("]", content);
        }

        [Test]
        public void JsonExporter_AppendsMultipleEntries()
        {
            var exporter = new JsonLogExporter();
            string path = Path.Combine(tempDir, "multi.json");

            var dto = mapper.ToDto(sampleEntry);
            exporter.Export(dto, path);
            exporter.Export(dto, path);
            exporter.Dispose();

            string content = File.ReadAllText(path);
            Assert.That(content.Split(new[] { "Export test message" }, StringSplitOptions.None).Length, Is.EqualTo(3));
        }

        [Test]
        public void TextExporter_WritesPlainText()
        {
            var exporter = new TextLogExporter(ScriptableObject.CreateInstance<LogSettings>());
            string path = Path.Combine(tempDir, "log.txt");

            var dto = mapper.ToDto(sampleEntry);
            exporter.Export(dto, path);

            string content = File.ReadAllText(path);
            StringAssert.Contains("Export test message", content);
            StringAssert.Contains("Boom!", content);
        }

        [Test]
        public void XmlExporter_WritesValidXml()
        {
            var exporter = new XmlLogExporter();
            string path = Path.Combine(tempDir, "log.xml");

            var dto = mapper.ToDto(sampleEntry);
            exporter.Export(dto, path);

            // Important: close the writer so the file is released
            exporter.Dispose();

            string content = File.ReadAllText(path);
            StringAssert.Contains("<Message>Export test message</Message>", content);
            StringAssert.Contains("Boom!", content);
            StringAssert.Contains("<Logs>", content);
            StringAssert.Contains("</Logs>", content);
        }


        [Test]
        public void UnityConsoleExporter_LogsInfo()
        {
            var exporter = new UnityConsoleExporter(ScriptableObject.CreateInstance<LogSettings>());
            var entry = new LogEntry(DateTime.Now, LogLevel.Info, LogCategory.General, "Info message");

            var dto = mapper.ToDto(entry);
            var formatter = new LogEntryFormatter(ScriptableObject.CreateInstance<LogSettings>());
            string expected = formatter.Format(dto);

            LogAssert.Expect(LogType.Log, expected);
            exporter.Export(dto, null);
        }

        [Test]
        public void UnityConsoleExporter_LogsWarning()
        {
            var exporter = new UnityConsoleExporter(ScriptableObject.CreateInstance<LogSettings>());
            var entry = new LogEntry(DateTime.Now, LogLevel.Warning, LogCategory.General, "Warning message");

            var dto = mapper.ToDto(entry);
            var formatter = new LogEntryFormatter(ScriptableObject.CreateInstance<LogSettings>());
            string expected = formatter.Format(dto);

            LogAssert.Expect(LogType.Warning, expected);
            exporter.Export(dto, null);
        }

        [Test]
        public void UnityConsoleExporter_LogsErrorWithException()
        {
            var exporter = new UnityConsoleExporter(ScriptableObject.CreateInstance<LogSettings>());
            var entry = new LogEntry(DateTime.Now,
                                     LogLevel.Error,
                                     LogCategory.General,
                                     "Error message",
                                     null,
                                     new InvalidOperationException("Boom!"));

            var dto = mapper.ToDto(entry);
            var formatter = new LogEntryFormatter(ScriptableObject.CreateInstance<LogSettings>());
            string expected = formatter.Format(dto);

            LogAssert.Expect(LogType.Error, expected);
            exporter.Export(dto, null);
        }

        [Test]
        public void UnityConsoleExporter_LogsCritical()
        {
            var exporter = new UnityConsoleExporter(ScriptableObject.CreateInstance<LogSettings>());
            var entry = new LogEntry(DateTime.Now, LogLevel.Critical, LogCategory.General, "Critical message");

            var dto = mapper.ToDto(entry);
            var formatter = new LogEntryFormatter(ScriptableObject.CreateInstance<LogSettings>());
            string expected = formatter.Format(dto);

            LogAssert.Expect(LogType.Error, expected); // Critical maps to Error
            exporter.Export(dto, null);
        }
    }
}
