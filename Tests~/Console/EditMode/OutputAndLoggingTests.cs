using EldritchGames.EldritchLogger.Console.Logging;
using EldritchGames.EldritchLogger.Console.Output;
using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Domain;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Formatting;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode
{
    public class ConsoleOutputTests
    {
        private sealed class CapturingLogger : IEldritchLogger
        {
            public readonly List<LogEntry> Entries = new();
            public bool IsEnabled(LogLevel level, LogCategory category) => true;
            public void Log(LogEntry entry) => Entries.Add(entry);
        }

        [Test]
        public void Write_ColorsWarningsAndErrors()
        {
            var view = new FakeView();
            var output = new ConsoleOutput(view);

            output.Info("i");
            output.Warn("w");
            output.Error("e");

            Assert.That(view.Lines[0], Is.EqualTo("i"));
            Assert.That(view.Lines[1], Does.StartWith("<color=").And.Contain(">w</color>"));
            Assert.That(view.Lines[2], Does.StartWith("<color=").And.Contain(">e</color>"));
        }

        [Test]
        public void Mirror_SendsMarkedEntriesToTheLogger()
        {
            var logger = new CapturingLogger();
            var output = new ConsoleOutput(new FakeView(), logger);

            output.Warn("careful");

            var entry = logger.Entries.Single();
            Assert.That(entry.Level, Is.EqualTo(LogLevel.Warning));
            Assert.That(entry.Category, Is.EqualTo(new LogCategory("Console")));
            Assert.That(entry.Properties.ContainsKey(ConsoleOutput.MirroredPropertyKey), Is.True);
        }
    }

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
