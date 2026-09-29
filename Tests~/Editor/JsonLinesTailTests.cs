using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.EditorTools.LogViewer;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace EldritchGames.EldritchLogger.Tests.Editor
{
    public class JsonLinesTailTests
    {
        private string path;

        [SetUp]
        public void SetUp() => path = Path.Combine(Path.GetTempPath(), $"eldritch_tail_{Guid.NewGuid():N}.jsonl");

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(path)) File.Delete(path);
        }

        private static string Line(string message) =>
            LogJson.Serialize(new LogEntryDto { Timestamp = DateTime.UtcNow, Level = LogLevel.Info, Category = "C", Message = message }) + "\n";

        private void Append(string text)
        {
            using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using var writer = new StreamWriter(stream);
            writer.Write(text);
        }

        [Test]
        public void ReadNew_ReturnsOnlyAppendedEntries_AndWaitsForIncompleteLines()
        {
            Append(Line("one"));
            using var tail = new JsonLinesTail(path);
            Assert.That(tail.ReadNew(out _).Select(e => e.Message), Is.EqualTo(new[] { "one" }));

            var two = Line("two");
            Append(two.Substring(0, 10)); // the writer is mid-line
            Assert.That(tail.ReadNew(out var invalid), Is.Empty);
            Assert.That(invalid, Is.Zero, "an incomplete line is not an error yet");

            Append(two.Substring(10) + Line("three"));
            Assert.That(tail.ReadNew(out _).Select(e => e.Message), Is.EqualTo(new[] { "two", "three" }));
            Assert.That(tail.ReadNew(out _), Is.Empty);
        }

        [Test]
        public void ReadNew_StartsOver_WhenTheFileIsTruncated()
        {
            Append(Line("old session") + Line("more"));
            using var tail = new JsonLinesTail(path);
            tail.ReadNew(out _);

            using (var stream = new FileStream(path, FileMode.Truncate, FileAccess.Write, FileShare.ReadWrite)) { }
            Append(Line("new"));

            var entries = tail.ReadNew(out _);
            Assert.That(tail.Restarted, Is.True);
            Assert.That(entries.Select(e => e.Message), Is.EqualTo(new[] { "new" }));
        }

        [Test]
        public void Read_CountsACutOffLastLine()
        {
            Append(Line("complete") + "{\"Message\":\"cut");
            var entries = JsonLinesLogReader.Read(path, out var invalid);
            Assert.That(entries.Single().Message, Is.EqualTo("complete"));
            Assert.That(invalid, Is.EqualTo(1));
        }
    }
}
