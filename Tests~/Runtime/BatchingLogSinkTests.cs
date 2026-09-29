using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Sinks.Files;
using EldritchGames.EldritchLogger.Sinks.Network;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Tests
{
    public class BatchingLogSinkTests
    {
        private sealed class RecordingBatchSink : BatchingLogSink
        {
            public readonly List<int> BatchSizes = new();
            public ManualResetEventSlim Gate;
            public int Started;
            public volatile bool Returned;
            public volatile bool ResourcesDisposed;

            public RecordingBatchSink(BatchingOptions options) : base("Recording batches", LogLevel.Debug, options) { }

            public override string Location => null;

            protected override SendResult SendBatch(IReadOnlyList<LogEntryDto> batch, CancellationToken cancellation)
            {
                Interlocked.Increment(ref Started);
                Gate?.Wait(TimeSpan.FromSeconds(5));
                lock (BatchSizes) BatchSizes.Add(batch.Count);
                Returned = true;
                return SendResult.Success;
            }

            protected override void DisposeResources() => ResourcesDisposed = true;
        }

        private static LogEntryDto Entry() => new() { Message = "m" };

        private static bool WaitFor(Func<bool> condition, int timeoutMs = 3000)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                if (condition()) return true;
                Thread.Sleep(10);
            }
            return condition();
        }

        [Test]
        public void SendsAsSoonAsABatchIsFull()
        {
            using var sink = new RecordingBatchSink(new BatchingOptions { BatchSize = 5, FlushInterval = TimeSpan.FromHours(1) });

            for (int i = 0; i < 5; i++) sink.Emit(Entry());

            Assert.That(WaitFor(() => { lock (sink.BatchSizes) return sink.BatchSizes.Sum() == 5; }), Is.True);
        }

        [Test]
        public void SendsPartialBatchesOnTheInterval()
        {
            using var sink = new RecordingBatchSink(new BatchingOptions { BatchSize = 100, FlushInterval = TimeSpan.FromMilliseconds(50) });

            sink.Emit(Entry());

            Assert.That(WaitFor(() => { lock (sink.BatchSizes) return sink.BatchSizes.Count == 1; }), Is.True);
        }

        [Test]
        public void DropsTheOldestEntries_WhenTheQueueIsFull()
        {
            var gate = new ManualResetEventSlim(false);
            using var capture = new SelfLogCapture();
            using var sink = new RecordingBatchSink(new BatchingOptions { BatchSize = 1, QueueCapacity = 3, FlushInterval = TimeSpan.FromMilliseconds(10) }) { Gate = gate };

            for (int i = 0; i < 20; i++) sink.Emit(Entry());
            Assert.That(sink.DroppedCount, Is.GreaterThan(0));

            gate.Set();
            sink.Flush();
            lock (sink.BatchSizes) Assert.That(sink.BatchSizes.Sum() + sink.DroppedCount, Is.EqualTo(20));
        }

        [Test]
        public void RejectsInvalidBatchSize()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RecordingBatchSink(new BatchingOptions { BatchSize = 0 }));
        }

        private sealed class CountingBatchSink : BatchingLogSink
        {
            private int sent;
            public int Sent => Volatile.Read(ref sent);

            public CountingBatchSink(BatchingOptions options) : base("Counting", LogLevel.Debug, options) { }
            public override string Location => null;

            protected override SendResult SendBatch(IReadOnlyList<LogEntryDto> batch, CancellationToken cancellation)
            {
                Interlocked.Add(ref sent, batch.Count);
                return SendResult.Success;
            }
        }

        [Test]
        public void Flush_SendsEveryBatch_WithoutWaitingForTheInterval()
        {
            using var sink = new CountingBatchSink(new BatchingOptions
            {
                BatchSize = 2,
                FlushInterval = TimeSpan.FromHours(1),
                DrainTimeout = TimeSpan.FromSeconds(5)
            });

            for (int i = 0; i < 5; i++) sink.Emit(new LogEntryDto { Message = "m" + i });
            var started = DateTime.UtcNow;
            sink.Flush();

            Assert.That(sink.Sent, Is.EqualTo(5), "the last partial batch is sent too");
            Assert.That(DateTime.UtcNow - started, Is.LessThan(TimeSpan.FromSeconds(2)), "no waiting for the drain timeout");
        }

        [Test]
        public void AfterAFlush_SteadyLoggingIsBatchedNormallyAgain()
        {
            var gate = new ManualResetEventSlim(false);
            using var sink = new RecordingBatchSink(new BatchingOptions { BatchSize = 1000, FlushInterval = TimeSpan.FromHours(1) }) { Gate = gate };

            sink.Emit(Entry());
            var flush = Task.Run(sink.Flush);
            Assert.That(WaitFor(() => Volatile.Read(ref sink.Started) == 1), Is.True, "the flushed entry is being sent");

            // Entries logged while that send is in flight, and steadily afterwards, belong to no flush.
            for (int i = 0; i < 10; i++) sink.Emit(Entry());
            gate.Set();
            Assert.That(flush.Wait(TimeSpan.FromSeconds(5)), Is.True);
            for (int i = 0; i < 30; i++) { sink.Emit(Entry()); Thread.Sleep(5); }

            lock (sink.BatchSizes)
                Assert.That(sink.BatchSizes, Is.EqualTo(new[] { 1 }), "later entries wait for a full batch or the interval");
        }

        [Test]
        public void Flush_DoesNotWaitForEntriesLoggedAfterIt()
        {
            using var sink = new RecordingBatchSink(new BatchingOptions { BatchSize = 1000, FlushInterval = TimeSpan.FromHours(1) });
            bool stop = false;
            var producer = Task.Run(() => { while (!Volatile.Read(ref stop)) { sink.Emit(Entry()); Thread.Sleep(1); } });
            Thread.Sleep(50);

            var started = DateTime.UtcNow;
            sink.Flush();
            var elapsed = DateTime.UtcNow - started;

            Volatile.Write(ref stop, true);
            producer.Wait();
            Assert.That(elapsed, Is.LessThan(TimeSpan.FromSeconds(2)), "continuous logging does not hold the flush to its timeout");
        }

        [Test]
        public void Dispose_WhileASendIsStuck_LeavesTheWorkersResourcesAlone()
        {
            var gate = new ManualResetEventSlim(false);
            var sink = new RecordingBatchSink(new BatchingOptions { BatchSize = 1, DrainTimeout = TimeSpan.FromMilliseconds(100) }) { Gate = gate };
            sink.Emit(Entry());
            Assert.That(WaitFor(() => Volatile.Read(ref sink.Started) == 1), Is.True);

            using var capture = new SelfLogCapture();
            sink.Dispose(); // gives up after the drain timeout plus a second
            Assert.That(sink.ResourcesDisposed, Is.False, "nothing is disposed underneath the running send");
            Assert.That(capture.Messages, Has.Some.Contains("did not finish sending"));

            gate.Set();
            Assert.That(WaitFor(() => sink.Returned), Is.True);
            Thread.Sleep(100);
            Assert.That(capture.Messages, Has.None.Contains("stopped"), "the worker ends cleanly once the send returns");
        }
    }
}
