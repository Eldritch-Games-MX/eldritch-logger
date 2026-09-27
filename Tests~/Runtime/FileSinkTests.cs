using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Formatting;
using EldritchGames.EldritchLogger.Settings;
using EldritchGames.EldritchLogger.Sinks.Config;
using EldritchGames.EldritchLogger.Sinks.Files;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        public void BackgroundWriter_PreservesOrder_ForASingleProducer()
        {
            var path = Path.Combine(directory, "ordered.txt");
            using (var writer = new BackgroundLogWriter(path, e => e.Message + "\n"))
            {
                for (int i = 0; i < 10_000; i++) writer.Enqueue(Entry(i));
            }

            var lines = File.ReadAllLines(path);
            Assert.That(lines, Has.Length.EqualTo(10_000));
            Assert.That(lines, Is.EqualTo(Enumerable.Range(0, 10_000).Select(i => "message " + i)));
        }

        [Test]
        public void BackgroundWriter_AcceptsConcurrentProducers()
        {
            var path = Path.Combine(directory, "concurrent.txt");
            using (var writer = new BackgroundLogWriter(path, e => e.Message + "\n", capacity: 100_000))
            {
                Parallel.For(0, 8, t =>
                {
                    for (int i = 0; i < 1000; i++) writer.Enqueue(Entry(t * 1000 + i));
                });
            }

            Assert.That(File.ReadAllLines(path).Distinct().Count(), Is.EqualTo(8000));
        }

        [Test]
        public void BackgroundWriter_DropsOldest_WhenFull()
        {
            using var capture = new SelfLogCapture();
            var path = Path.Combine(directory, "bounded.txt");
            var gate = new ManualResetEventSlim(false);

            using (var writer = new BackgroundLogWriter(path, e =>
                   {
                       gate.Wait(TimeSpan.FromSeconds(5)); // hold the writer thread so the queue fills
                       return e.Message + "\n";
                   }, capacity: 5))
            {
                for (int i = 0; i < 50; i++) writer.Enqueue(Entry(i));
                Assert.That(writer.DroppedCount, Is.GreaterThan(0));
                gate.Set();
            }

            var lines = File.ReadAllLines(path);
            Assert.That(lines.Last(), Is.EqualTo("message 49"), "the newest entry is never dropped");
            Assert.That(lines.Length, Is.LessThan(50));
        }

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

        [Test]
        public void Locator_KeepsOnlyTheNewestSessions()
        {
            var locator = new LogFileLocator(directory, "game", ".jsonl");
            var start = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            for (int i = 0; i < 6; i++)
                File.WriteAllText(locator.SessionFilePath(start.AddMinutes(i)), "");
            File.WriteAllText(Path.Combine(directory, "other.jsonl"), "");

            locator.DeleteOldSessions(keep: 2);

            var remaining = Directory.GetFiles(directory).Select(Path.GetFileName).OrderBy(n => n).ToArray();
            Assert.That(remaining, Is.EqualTo(new[]
            {
                Path.GetFileName(locator.SessionFilePath(start.AddMinutes(4))),
                Path.GetFileName(locator.SessionFilePath(start.AddMinutes(5))),
                "other.jsonl"
            }));
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
    }
}
