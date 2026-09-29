using EldritchGames.EldritchLogger.Console.Logging;
using EldritchGames.EldritchLogger.Console.Output;
using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Formatting;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode
{
    public class ConsoleLogSinkTests
    {
        private static LogEntryDto Entry(string message, params MetadataEntry[] metadata) => new()
        {
            Timestamp = DateTime.UtcNow,
            Level = LogLevel.Info,
            Category = "General",
            Message = message,
            Metadata = metadata.ToList()
        };

        private static ConsoleLogSink CreateSink(int capacity = 1000) =>
            new(new TextLogFormatter(null, richText: false), LogLevel.Debug, capacity);

        [Test]
        public void Entries_AreShownOnlyOnFlush()
        {
            var sink = CreateSink();
            var view = new FakeView();

            sink.Emit(Entry("first"));
            Assert.That(view.Lines, Is.Empty);

            sink.Flush(view);
            sink.Flush(view);

            Assert.That(view.Lines.Single(), Does.Contain("first"));
        }

        [Test]
        public void MirroredCommandOutput_IsSkipped()
        {
            var sink = CreateSink();
            var view = new FakeView();

            sink.Emit(Entry("mirrored", new MetadataEntry { Key = ConsoleOutput.MirroredPropertyKey, Value = "True" }));
            sink.Flush(view);

            Assert.That(view.Lines, Is.Empty);
        }

        [Test]
        public void Queue_IsBounded_AndReportsDrops()
        {
            var sink = CreateSink(capacity: 3);
            var view = new FakeView();

            for (int i = 0; i < 10; i++) sink.Emit(Entry("m" + i));
            sink.Flush(view);

            Assert.That(view.Lines[0], Does.Contain("7 log entries dropped"));
            Assert.That(view.Lines.Skip(1).Select(l => l.Split(' ').Last()), Is.EqualTo(new[] { "m7", "m8", "m9" }));
        }

        [Test]
        public void EntriesFromWorkerThreads_AreFlushedOnTheCallingThread()
        {
            var sink = CreateSink();
            var view = new FakeView();

            Parallel.For(0, 100, i => sink.Emit(Entry("t" + i)));
            sink.Flush(view);

            Assert.That(view.Lines, Has.Count.EqualTo(100));
        }
    }
}
