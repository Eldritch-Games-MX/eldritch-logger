using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Pipeline;
using EldritchGames.EldritchLogger.Sinks;
using NUnit.Framework;

namespace EldritchGames.EldritchLogger.Tests
{
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

        private sealed class UnityViewSink : ILogSink, IShowsUnityLog
        {
            public string Name => "Unity view";
            public LogLevel MinimumLevel => LogLevel.Debug;
            public void Emit(Dto.LogEntryDto entry) { }
        }

        [Test]
        public void SnapshotForUnityEntries_LeavesOutSinksThatShowUnityLog_AndTracksChanges()
        {
            var view = new UnityViewSink();
            var plain = new RecordingSink();
            var collection = new SinkCollection(new ILogSink[] { view, plain });

            Assert.That(collection.SnapshotForUnityEntries, Is.EqualTo(new ILogSink[] { plain }));

            var another = new RecordingSink();
            collection.AddSink(another);
            collection.RemoveSink(plain);

            Assert.That(collection.Snapshot, Is.EqualTo(new ILogSink[] { view, another }));
            Assert.That(collection.SnapshotForUnityEntries, Is.EqualTo(new ILogSink[] { another }));
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
}
