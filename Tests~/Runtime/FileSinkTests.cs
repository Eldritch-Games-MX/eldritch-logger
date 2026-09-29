using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Formatting;
using EldritchGames.EldritchLogger.Settings;
using EldritchGames.EldritchLogger.Sinks.Files;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Tests
{
    public class FileSinkTests
    {
        private string directory;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "EldritchLoggerTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
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

        [Test]
        public void JsonLinesSink_WritesOneParsableObjectPerLine()
        {
            var path = Path.Combine(directory, "log.jsonl");
            using (var sink = new JsonLinesFileSink(path))
            {
                sink.Emit(Entry(1, LogLevel.Warning));
                sink.Emit(Entry(2));
            }

            var lines = File.ReadAllLines(path);
            Assert.That(lines, Has.Length.EqualTo(2));
            var first = JObject.Parse(lines[0]);
            Assert.That((string)first["Level"], Is.EqualTo("Warning"));
            Assert.That((string)first["Message"], Is.EqualTo("message 1"));
            Assert.That(first["Context"], Is.Null);
        }

        [Test]
        public void JsonLinesSink_IsValidBeforeDispose()
        {
            var path = Path.Combine(directory, "live.jsonl");
            using var sink = new JsonLinesFileSink(path);
            sink.Emit(Entry(1));
            sink.Flush();

            string content;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
                content = reader.ReadToEnd();

            Assert.DoesNotThrow(() => JObject.Parse(content.Trim()));
        }

        [Test]
        public void XmlSink_ProducesWellFormedDocument()
        {
            var path = Path.Combine(directory, "log.xml");
            using (var sink = new XmlFileSink(path))
            {
                sink.Emit(Entry(1));
                sink.Emit(Entry(2));
            }

            var doc = XDocument.Load(path);
            Assert.That(doc.Root.Name.LocalName, Is.EqualTo("Logs"));
            Assert.That(doc.Root.Elements().Count(), Is.EqualTo(2));
        }

        [Test]
        public void TextSink_WritesPlainText()
        {
            var settings = ScriptableObject.CreateInstance<LogSettings>();
            var path = Path.Combine(directory, "log.txt");
            try
            {
                using (var sink = new TextFileSink(path, new TextLogFormatter(settings, richText: false)))
                    sink.Emit(Entry(1));

                var text = File.ReadAllText(path);
                Assert.That(text, Does.Contain("message 1"));
                Assert.That(text, Does.Not.Contain("<color"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }
    }
}
