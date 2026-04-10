using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Exporting;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace EldritchGames.EldritchLogger.Tests
{
    [TestFixture]
    public class SinkManagerTests
    {
        // Simple stub sink
        private class StubSink : ILogSink
        {
            public SinkCategory Category { get; }
            public StubSink(SinkCategory category) => Category = category;
            public void OnLogReceived(LogEntryDto dto) { }
        }

        // Minimal stub manager implementing ISinkManager
        private class SinkManagerStub : ISinkManager
        {
            private readonly Dictionary<SinkCategory, List<ILogSink>> sinks;

            public SinkManagerStub(Dictionary<SinkCategory, List<ILogSink>> initialSinks = null)
            {
                sinks = initialSinks ?? new Dictionary<SinkCategory, List<ILogSink>>();
            }

            public IEnumerable<ILogSink> GetAllSinks() => sinks.Values.SelectMany(list => list);

            public IEnumerable<ILogSink> GetSinks(SinkCategory category) =>
                sinks.ContainsKey(category) ? sinks[category] : Enumerable.Empty<ILogSink>();

            public void AddSink(SinkCategory category, ILogSink sink)
            {
                if (!sinks.ContainsKey(category))
                    sinks[category] = new List<ILogSink>();
                sinks[category].Add(sink);
            }

            public void RemoveSink(SinkCategory category, ILogSink sink)
            {
                if (sinks.ContainsKey(category))
                    sinks[category].Remove(sink);
            }
        }

        [Test]
        public void AddSink_ShouldAddSinkToCategory()
        {
            var manager = new SinkManagerStub();
            var sink = new StubSink(SinkCategory.Persistent);

            manager.AddSink(SinkCategory.Persistent, sink);

            var retrieved = manager.GetSinks(SinkCategory.Persistent).ToList();
            Assert.That(retrieved.Count, Is.EqualTo(1));
            Assert.That(retrieved[0], Is.EqualTo(sink));
        }

        [Test]
        public void RemoveSink_ShouldRemoveSinkFromCategory()
        {
            var sink = new StubSink(SinkCategory.Persistent);
            var dict = new Dictionary<SinkCategory, List<ILogSink>>
        {
            { SinkCategory.Persistent, new List<ILogSink> { sink } }
        };
            var manager = new SinkManagerStub(dict);

            manager.RemoveSink(SinkCategory.Persistent, sink);

            var retrieved = manager.GetSinks(SinkCategory.Persistent).ToList();
            Assert.That(retrieved.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetAllSinks_ShouldReturnAll()
        {
            var sink1 = new StubSink(SinkCategory.Persistent);
            var sink2 = new StubSink(SinkCategory.Runtime);

            var dict = new Dictionary<SinkCategory, List<ILogSink>>
        {
            { SinkCategory.Persistent, new List<ILogSink> { sink1 } },
            { SinkCategory.Runtime, new List<ILogSink> { sink2 } }
        };

            var manager = new SinkManagerStub(dict);

            var all = manager.GetAllSinks().ToList();
            Assert.That(all.Count, Is.EqualTo(2));
            Assert.That(all.Contains(sink1));
            Assert.That(all.Contains(sink2));
        }
    }
}
