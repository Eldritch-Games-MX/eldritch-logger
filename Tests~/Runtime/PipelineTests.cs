using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Domain;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Mapper;
using EldritchGames.EldritchLogger.Pipeline;
using EldritchGames.EldritchLogger.Sinks;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Tests
{
    public class LogDispatcherTests
    {
        private sealed class ProbeSink : ILogSink
        {
            public bool SawDispatching;
            public string Name => "Probe";
            public LogLevel MinimumLevel => LogLevel.Debug;
            public void Emit(LogEntryDto entry) => SawDispatching = LogDispatcher.IsDispatching;
        }

        [Test]
        public void IsDispatching_IsTrueOnlyWhileSinksRun()
        {
            var probe = new ProbeSink();

            new LogDispatcher().Dispatch(new LogEntryDto(), new ILogSink[] { probe });

            Assert.That(probe.SawDispatching, Is.True);
            Assert.That(LogDispatcher.IsDispatching, Is.False);
        }

        [Test]
        public void ThrowingSink_IsReported_AndStateIsReset()
        {
            using var capture = new SelfLogCapture();
            var after = new RecordingSink();

            new LogDispatcher().Dispatch(new LogEntryDto(), new ILogSink[] { new ThrowingSink(), after });

            Assert.That(after.Entries, Has.Count.EqualTo(1));
            Assert.That(capture.Messages, Has.Count.EqualTo(1));
            Assert.That(LogDispatcher.IsDispatching, Is.False);
        }

        [Test]
        public void SinksBelowTheirMinimumLevel_AreSkipped()
        {
            var errors = new RecordingSink(LogLevel.Error);

            new LogDispatcher().Dispatch(new LogEntryDto { Level = LogLevel.Warning }, new ILogSink[] { errors });

            Assert.That(errors.Entries, Is.Empty);
        }
    }

    public class SinkCollectionTests
    {
        [Test]
        public void AddRemove_IgnoresDuplicatesAndUnknownSinks()
        {
            var a = new RecordingSink();
            var collection = new SinkCollection();

            collection.AddSink(a);
            collection.AddSink(a);
            collection.RemoveSink(new RecordingSink());

            Assert.That(collection.Snapshot.Count, Is.EqualTo(1));
            collection.RemoveSink(a);
            Assert.That(collection.Snapshot, Is.Empty);
        }

        [Test]
        public void Snapshot_IsNotAffectedByLaterChanges()
        {
            var collection = new SinkCollection(new ILogSink[] { new RecordingSink() });
            var snapshot = collection.Snapshot;

            collection.AddSink(new RecordingSink());

            Assert.That(snapshot.Count, Is.EqualTo(1));
            Assert.That(collection.Snapshot.Count, Is.EqualTo(2));
        }

        [Test]
        public void LowestMinimumLevel_ReflectsSinks()
        {
            var collection = new SinkCollection();
            Assert.That(collection.LowestMinimumLevel, Is.GreaterThan(LogLevel.Critical));

            collection.AddSink(new RecordingSink(LogLevel.Error));
            collection.AddSink(new RecordingSink(LogLevel.Info));
            Assert.That(collection.LowestMinimumLevel, Is.EqualTo(LogLevel.Info));
        }
    }

    public class LogEntryMapperTests
    {
        private static Exception Thrown()
        {
            try { throw new InvalidOperationException("broken"); }
            catch (Exception ex) { return ex; }
        }

        [Test]
        public void ToDto_CopiesFieldsAndStringifiesProperties()
        {
            var entry = new LogEntry(LogLevel.Error, "Loot", "msg",
                new Dictionary<string, object> { ["Count"] = 3, ["Null"] = null },
                timestampUtc: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            var dto = new LogEntryMapper().ToDto(entry);

            Assert.That(dto.Level, Is.EqualTo(LogLevel.Error));
            Assert.That(dto.Category, Is.EqualTo("Loot"));
            Assert.That(dto.GetMetadata("Count"), Is.EqualTo("3"));
            Assert.That(dto.GetMetadata("Null"), Is.Null);
            Assert.That(dto.Exception, Is.Null);
            Assert.That(dto.Timestamp, Is.EqualTo(entry.TimestampUtc));
        }

        [Test]
        public void ToDto_DescribesException_AndFiltersLoggerFrames()
        {
            // Test frames live in EldritchGames.EldritchLogger.Tests, so filtering removes them.
            var filtered = new LogEntryMapper(filterLoggerFrames: true)
                .ToDto(new LogEntry(LogLevel.Error, default, "m", exception: Thrown()));
            var unfiltered = new LogEntryMapper(filterLoggerFrames: false)
                .ToDto(new LogEntry(LogLevel.Error, default, "m", exception: Thrown()));

            Assert.That(filtered.Exception, Is.EqualTo("InvalidOperationException: broken"));
            Assert.That(unfiltered.Exception, Does.StartWith("InvalidOperationException: broken\n"));
            Assert.That(unfiltered.Exception, Does.Contain("LogEntryMapperTests"));
        }
    }
}
