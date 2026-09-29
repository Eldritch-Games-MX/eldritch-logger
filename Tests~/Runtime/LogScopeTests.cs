using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Pipeline;
using EldritchGames.EldritchLogger.Settings;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Tests
{
    public class LogScopeTests
    {
        private RecordingSink sink;
        private Core.EldritchLogger logger;

        [SetUp]
        public void SetUp()
        {
            sink = new RecordingSink();
            logger = new EldritchLoggerBuilder().AddSink(sink).Build();
        }

        [TearDown]
        public void TearDown() => logger.Dispose();

        [Test]
        public void Scopes_AddPropertiesUntilDisposed()
        {
            using (logger.BeginScope("MatchId", 42))
            {
                logger.AtInfo().Log("inside");
            }
            logger.AtInfo().Log("outside");

            Assert.That(sink.Entries[0].GetMetadata("MatchId"), Is.EqualTo("42"));
            Assert.That(sink.Entries[1].GetMetadata("MatchId"), Is.Null);
            Assert.That(LogScope.HasActiveScopes, Is.False);
        }

        [Test]
        public void NestedScopes_InnerWins_EntryPropertiesWinOverScopes()
        {
            using (logger.BeginScope(("Map", "Forest"), ("Mode", "Ranked")))
            using (logger.BeginScope("Mode", "Casual"))
            {
                logger.AtInfo().AddKeyValue("Map", "Override").Log("m");
            }

            var entry = sink.Entries[0];
            Assert.That(entry.GetMetadata("Mode"), Is.EqualTo("Casual"));
            Assert.That(entry.GetMetadata("Map"), Is.EqualTo("Override"));
        }

        [Test]
        public void Scopes_FlowIntoTasks()
        {
            using (logger.BeginScope("RequestId", "r-1"))
            {
                Task.Run(() => logger.AtInfo().Log("from a task")).Wait();
            }

            Assert.That(sink.Entries[0].GetMetadata("RequestId"), Is.EqualTo("r-1"));
        }

        [Test]
        public void Scopes_AreIsolatedBetweenConcurrentFlows()
        {
            var a = Task.Run(() => { using (logger.BeginScope("Flow", "A")) { Thread.Sleep(20); logger.AtInfo().Log("a"); } });
            var b = Task.Run(() => { using (logger.BeginScope("Flow", "B")) { Thread.Sleep(20); logger.AtInfo().Log("b"); } });
            Task.WaitAll(a, b);

            lock (sink.Entries)
                foreach (var entry in sink.Entries)
                    Assert.That(entry.GetMetadata("Flow"), Is.EqualTo(entry.Message.ToUpperInvariant()));
        }

        [Test]
        public void DisposingAnOuterScopeFirst_ClosesInnerScopesToo()
        {
            var outer = logger.BeginScope("A", 1);
            var inner = logger.BeginScope("B", 2);

            outer.Dispose();
            logger.AtInfo().Log("after");
            inner.Dispose(); // no effect, already closed

            Assert.That(sink.Entries[0].Metadata, Is.Empty);
            Assert.That(LogScope.HasActiveScopes, Is.False);
        }

        // 7 & 8 ------------------------------------------------------------------------------------------

        [Test]
        public void Scopes_StillApply_AfterClearEnrichers()
        {
            var sink = new RecordingSink();
            var settings = ScriptableObject.CreateInstance<LogSettings>();
            settings.sinks.Clear();
            try
            {
                using var logger = EldritchLoggerBuilder.FromSettings(settings).ClearEnrichers().AddSink(sink).Build();
                using (logger.BeginScope("MatchId", 7))
                    logger.AtInfo().Log("m");

                Assert.That(sink.Entries[0].GetMetadata("MatchId"), Is.EqualTo("7"));
                Assert.That(sink.Entries[0].GetMetadata(LogPropertyKeys.Scene), Is.Null, "enrichers were cleared");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void Scopes_DuplicateKeysInOneScope_LastWins()
        {
            var sink = new RecordingSink();
            using var logger = new EldritchLoggerBuilder().AddSink(sink).Build();

            using (logger.BeginScope(("K", "first"), ("K", "second")))
                logger.AtInfo().Log("m");

            Assert.That(sink.Entries[0].GetMetadata("K"), Is.EqualTo("second"));
        }

        [Test]
        public void ScopeWithANullKey_IsRejected_AndLoggingKeepsWorking()
        {
            Assert.Throws<ArgumentException>(() =>
                LogScope.Push(new[] { new KeyValuePair<string, object>(null, 1) }));

            Assert.That(LogScope.HasActiveScopes, Is.False);
            logger.Info("still logging");
            Assert.That(sink.Entries.Last().Message, Is.EqualTo("still logging"));
        }

        [Test]
        public void BeginScope_RejectsANullArrayAndNullKeys()
        {
            Assert.Throws<ArgumentNullException>(() => logger.BeginScope(((string, object)[])null));
            Assert.Throws<ArgumentException>(() => logger.BeginScope((null, 1)));
            Assert.That(LogScope.HasActiveScopes, Is.False);
        }
    }
}
