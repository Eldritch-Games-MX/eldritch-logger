using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Exporting;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace EldritchGames.EldritchLogger.Tests
{
    [TestFixture]
    public class SinkManagerTests
    {
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
            var mockSink = new Mock<ILogSink>();
            mockSink.SetupGet(s => s.Category).Returns(SinkCategory.Persistent);

            manager.AddSink(SinkCategory.Persistent, mockSink.Object);

            var retrieved = manager.GetSinks(SinkCategory.Persistent).ToList();
            Assert.That(retrieved.Count, Is.EqualTo(1));
            Assert.That(retrieved[0], Is.EqualTo(mockSink.Object));
        }

        [Test]
        public void RemoveSink_ShouldRemoveSinkFromCategory()
        {
            var mockSink = new Mock<ILogSink>();
            mockSink.SetupGet(s => s.Category).Returns(SinkCategory.Persistent);

            var dict = new Dictionary<SinkCategory, List<ILogSink>>
            {
                { SinkCategory.Persistent, new List<ILogSink> { mockSink.Object } }
            };
            var manager = new SinkManagerStub(dict);

            manager.RemoveSink(SinkCategory.Persistent, mockSink.Object);

            var retrieved = manager.GetSinks(SinkCategory.Persistent).ToList();
            Assert.That(retrieved.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetAllSinks_ShouldReturnAll()
        {
            var mockSink1 = new Mock<ILogSink>();
            mockSink1.SetupGet(s => s.Category).Returns(SinkCategory.Persistent);

            var mockSink2 = new Mock<ILogSink>();
            mockSink2.SetupGet(s => s.Category).Returns(SinkCategory.Runtime);

            var dict = new Dictionary<SinkCategory, List<ILogSink>>
            {
                { SinkCategory.Persistent, new List<ILogSink> { mockSink1.Object } },
                { SinkCategory.Runtime, new List<ILogSink> { mockSink2.Object } }
            };

            var manager = new SinkManagerStub(dict);

            var all = manager.GetAllSinks().ToList();
            Assert.That(all.Count, Is.EqualTo(2));
            Assert.That(all.Contains(mockSink1.Object));
            Assert.That(all.Contains(mockSink2.Object));
        }
    }
}
