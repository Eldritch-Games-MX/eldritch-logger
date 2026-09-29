using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Sinks.Files;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Tests
{
    public class BackgroundLogWriterTests
    {
        private string directory;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "EldritchWriter_" + Guid.NewGuid().ToString("N"));
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
        public void Flush_WaitsForTheEntryBeingWritten_NotJustAnEmptyQueue()
        {
            var path = Path.Combine(directory, "slow.txt");
            // The writer takes the entry off the queue at once, then spends a while serializing it.
            using var writer = new BackgroundLogWriter(path, e => { Thread.Sleep(200); return e.Message + "\n"; });
            writer.Enqueue(Entry(1));

            Assert.That(writer.Flush(TimeSpan.FromSeconds(5)), Is.True);

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            Assert.That(reader.ReadToEnd(), Is.EqualTo("message 1\n"));
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
        public void BackgroundWriter_ValidatesArguments_AndIgnoresEntriesAfterDispose()
        {
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "w.txt");
            Assert.Throws<ArgumentException>(() => new BackgroundLogWriter("", e => ""));
            Assert.Throws<ArgumentNullException>(() => new BackgroundLogWriter(path, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BackgroundLogWriter(path, e => "", capacity: 0));

            var writer = new BackgroundLogWriter(path, e => e.Message + "\n");
            writer.Dispose();
            Assert.DoesNotThrow(() => writer.Enqueue(new LogEntryDto { Message = "late" }));
            Assert.DoesNotThrow(writer.Dispose);
            Assert.That(File.ReadAllText(path), Is.Empty);
        }

        [Test]
        public void BackgroundWriter_SerializerFailures_AreReported_AndSkipped()
        {
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "w.txt");
            using var capture = new SelfLogCapture();

            using (var writer = new BackgroundLogWriter(path, e => e.Message == "bad" ? throw new FormatException("no") : e.Message + "\n"))
            {
                writer.Enqueue(new LogEntryDto { Message = "good" });
                writer.Enqueue(new LogEntryDto { Message = "bad" });
                writer.Enqueue(new LogEntryDto { Message = "also good" });
            }

            Assert.That(File.ReadAllLines(path), Is.EqualTo(new[] { "good", "also good" }));
            Assert.That(capture.Messages, Has.Some.Contains("Failed to serialize"));
        }

        [Test]
        public void TimedOutDispose_LeavesNoOpenFiles_AndDoesNotFloodSelfLog()
        {
            var directory = Path.Combine(Path.GetTempPath(), "EldritchShutdown_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "log.txt");
            using var capture = new SelfLogCapture();

            // A slow serializer keeps a backlog; a tiny drain timeout makes Dispose give up while the thread is busy.
            var writer = new BackgroundLogWriter(path, e => { Thread.Sleep(20); return e.Message + "\n"; },
                                                 drainTimeout: TimeSpan.FromMilliseconds(50));
            for (int i = 0; i < 100; i++) writer.Enqueue(new LogEntryDto { Message = "entry number " + i });

            writer.Dispose();
            var filesAtDispose = Directory.GetFiles(directory).Length;
            Thread.Sleep(500); // let the still-running thread drain its leftovers

            Assert.That(Directory.GetFiles(directory).Length, Is.EqualTo(filesAtDispose), "no file is created after shutdown");
            Assert.That(writer.DroppedCount, Is.GreaterThan(0), "leftovers are counted as dropped");
            Assert.That(capture.Messages.Count(m => m.Contains("Could not write")), Is.EqualTo(0), "leftovers are not reported one by one");
            Assert.DoesNotThrow(() => Directory.Delete(directory, true), "every file handle was closed");
        }

        [Test]
        public void Flush_DoesNotWaitForEntriesQueuedAfterIt()
        {
            var path = Path.Combine(directory, "busy.txt");
            using var writer = new BackgroundLogWriter(path, e => { Thread.Sleep(2); return e.Message + "\n"; });
            bool stop = false;
            var producer = Task.Run(() => { while (!Volatile.Read(ref stop)) writer.Enqueue(Entry(0)); });
            Thread.Sleep(50);

            var started = DateTime.UtcNow;
            bool flushed = writer.Flush(TimeSpan.FromSeconds(3));
            var elapsed = DateTime.UtcNow - started;

            Volatile.Write(ref stop, true);
            producer.Wait();
            Assert.That(flushed, Is.True, "what was queued before the call got written");
            Assert.That(elapsed, Is.LessThan(TimeSpan.FromSeconds(2.5)), "the flush did not run into its timeout");
        }
    }
}
