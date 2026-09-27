using EldritchGames.EldritchLogger.Builder;
using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Domain;
using EldritchGames.EldritchLogger.Mapper;
using EldritchGames.EldritchLogger.Pipeline;
using EldritchGames.EldritchLogger.Sinks;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EldritchGames.EldritchLogger.Tests
{
    public class EldritchLoggerTests
    {
        private FakeClock clock;
        private RecordingSink sink;

        [SetUp]
        public void SetUp()
        {
            clock = new FakeClock();
            sink = new RecordingSink();
        }

        private Core.EldritchLogger CreateLogger(ILogFilter filter = null,
                                                 IEnumerable<ILogEnricher> enrichers = null,
                                                 params ILogSink[] sinks) =>
            new(filter ?? new DelegateFilter((_, _) => true),
                enrichers,
                new LogEntryMapper(),
                new LogDispatcher(),
                clock,
                sinks.Length > 0 ? sinks : new ILogSink[] { sink });

        [Test]
        public void Log_StampsTimestampFromClock_AndReachesSink()
        {
            using var logger = CreateLogger();

            logger.Log(LogLevel.Info, LogCategory.Gameplay, "hello");

            Assert.That(sink.Entries, Has.Count.EqualTo(1));
            var entry = sink.Entries[0];
            Assert.That(entry.Message, Is.EqualTo("hello"));
            Assert.That(entry.Level, Is.EqualTo(LogLevel.Info));
            Assert.That(entry.Category, Is.EqualTo("Gameplay"));
            Assert.That(entry.Timestamp, Is.EqualTo(clock.UtcNow));
        }

        [Test]
        public void Log_CustomAndEnumCategories_UseTheSamePath()
        {
            using var logger = CreateLogger();
            var ex = new InvalidOperationException("bad");

            logger.Log(LogLevel.Error, "Loot", "custom", exception: ex);
            logger.Log(LogLevel.Error, DayOfWeek.Monday, "enum");

            Assert.That(sink.Entries.Select(e => e.Category), Is.EqualTo(new[] { "Loot", "Monday" }));
            Assert.That(sink.Entries[0].Exception, Does.StartWith("InvalidOperationException: bad"));
        }

        [Test]
        public void Filter_DiscardsDisabledEntries()
        {
            using var logger = CreateLogger(new DelegateFilter((level, _) => level >= LogLevel.Warning));

            logger.Log(LogLevel.Info, LogCategory.General, "dropped");
            logger.Log(LogLevel.Error, LogCategory.General, "kept");

            Assert.That(sink.Entries.Select(e => e.Message), Is.EqualTo(new[] { "kept" }));
            Assert.That(logger.IsEnabled(LogLevel.Info, LogCategory.General), Is.False);
        }

        [Test]
        public void IsEnabled_IsFalseBelowEverySinksMinimumLevel()
        {
            using var logger = CreateLogger(sinks: new ILogSink[] { new RecordingSink(LogLevel.Error), new RecordingSink(LogLevel.Warning) });

            Assert.That(logger.IsEnabled(LogLevel.Info, LogCategory.General), Is.False);
            Assert.That(logger.IsEnabled(LogLevel.Warning, LogCategory.General), Is.True);
        }

        [Test]
        public void PerSinkMinimumLevel_IsRespected()
        {
            var errorsOnly = new RecordingSink(LogLevel.Error);
            using var logger = CreateLogger(sinks: new ILogSink[] { sink, errorsOnly });

            logger.Log(LogLevel.Info, LogCategory.General, "info");
            logger.Log(LogLevel.Error, LogCategory.General, "error");

            Assert.That(sink.Entries, Has.Count.EqualTo(2));
            Assert.That(errorsOnly.Entries.Select(e => e.Message), Is.EqualTo(new[] { "error" }));
        }

        [Test]
        public void Enrichers_RunInOrder_AndDoNotMutateTheCallersEntry()
        {
            var order = new List<string>();
            var enrichers = new ILogEnricher[]
            {
                new DelegateEnricher((_, p) => { order.Add("a"); p["A"] = 1; }),
                new DelegateEnricher((_, p) => { order.Add("b"); p["B"] = p.ContainsKey("A"); })
            };
            using var logger = CreateLogger(enrichers: enrichers);
            var original = new LogEntry(LogLevel.Info, LogCategory.General, "m");

            logger.Log(original);

            Assert.That(order, Is.EqualTo(new[] { "a", "b" }));
            Assert.That(sink.Entries[0].GetMetadata("A"), Is.EqualTo("1"));
            Assert.That(sink.Entries[0].GetMetadata("B"), Is.EqualTo("True"));
            Assert.That(original.Properties, Is.Empty);
        }

        [Test]
        public void ThrowingSinkOrEnricher_DoesNotBlockOthers()
        {
            using var capture = new SelfLogCapture();
            var enrichers = new ILogEnricher[] { new DelegateEnricher((_, _) => throw new Exception("enricher")) };
            using var logger = CreateLogger(enrichers: enrichers, sinks: new ILogSink[] { new ThrowingSink(), sink });

            logger.Log(LogLevel.Info, LogCategory.General, "still delivered");

            Assert.That(sink.Entries, Has.Count.EqualTo(1));
            Assert.That(capture.Messages, Has.Some.Contains("Throwing"));
            Assert.That(capture.Messages, Has.Some.Contains("DelegateEnricher"));
        }

        [Test]
        public void AddSink_And_RemoveSink_ChangeDelivery()
        {
            using var logger = CreateLogger();
            var extra = new RecordingSink();

            logger.AddSink(extra);
            logger.Log(LogLevel.Info, LogCategory.General, "one");
            logger.RemoveSink(extra);
            logger.Log(LogLevel.Info, LogCategory.General, "two");

            Assert.That(extra.Entries.Select(e => e.Message), Is.EqualTo(new[] { "one" }));
            Assert.That(sink.Entries, Has.Count.EqualTo(2));
        }

        [Test]
        public void Dispose_DisposesSinks_AndIgnoresFurtherEntries()
        {
            var logger = CreateLogger();

            logger.Dispose();
            logger.Log(LogLevel.Critical, LogCategory.General, "ignored");

            Assert.That(sink.Disposed, Is.True);
            Assert.That(sink.Entries, Is.Empty);
        }

        [Test]
        public void At_WhenDisabled_ReturnsNullBuilder_WithoutAllocating()
        {
            using var logger = CreateLogger(new DelegateFilter((_, _) => false));

            // Warm up (JIT, statics).
            logger.AtDebug().Log("warm-up");

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++)
                logger.AtDebug(LogCategory.AI).AddKeyValue("k", "v").Log("disabled");
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(logger.AtDebug(), Is.Not.InstanceOf<LogBuilder>());
            Assert.That(allocated, Is.EqualTo(0));
            Assert.That(sink.Entries, Is.Empty);
        }
    }
}
