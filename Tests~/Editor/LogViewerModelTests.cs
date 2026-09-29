using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.EditorTools.LogViewer;
using NUnit.Framework;
using System;
using System.Linq;

namespace EldritchGames.EldritchLogger.Tests.Editor
{
    public class LogViewerModelTests
    {
        private static LogViewerEntry Entry(long seq, LogLevel level, string category, string message, string logger = null,
                                            params (string key, string value)[] metadata)
        {
            var dto = new LogEntryDto
            {
                Timestamp = DateTime.UtcNow,
                Level = level,
                Category = category,
                Message = message,
                Metadata = metadata.Select(m => new MetadataEntry { Key = m.key, Value = m.value }).ToList()
            };
            if (logger != null) dto.Metadata.Add(new MetadataEntry { Key = LogPropertyKeys.Logger, Value = logger });
            return new LogViewerEntry(dto, seq);
        }

        private LogViewerModel model;
        private int changes;

        [SetUp]
        public void SetUp()
        {
            model = new LogViewerModel();
            model.Changed += () => changes++;
            model.AddRange(new[]
            {
                Entry(0, LogLevel.Info, "Gameplay", "Player spawned", "Player"),
                Entry(1, LogLevel.Warning, "Network", "Packet lost", "Net", ("Peer", "42")),
                Entry(2, LogLevel.Error, "Gameplay", "Boss failed", "Boss"),
                Entry(3, LogLevel.Debug, "AI", "Path found")
            });
            changes = 0;
        }

        [Test]
        public void TracksCountsCategoriesAndLoggers()
        {
            Assert.That(model.Visible.Count, Is.EqualTo(4));
            Assert.That(model.CountOf(LogLevel.Info), Is.EqualTo(1));
            Assert.That(model.CountOf(LogLevel.Critical), Is.EqualTo(0));
            Assert.That(model.Categories, Is.EqualTo(new[] { "AI", "Gameplay", "Network" }));
            Assert.That(model.Loggers, Is.EqualTo(new[] { "Boss", "Net", "Player" }));
        }

        [Test]
        public void LevelAndCategoryFilters()
        {
            model.SetLevelVisible(LogLevel.Debug, false);
            model.SetCategoryVisible("gameplay", false);

            Assert.That(model.Visible.Select(e => e.Sequence), Is.EqualTo(new[] { 1L }));
            Assert.That(changes, Is.EqualTo(2));

            model.ShowAllCategories();
            Assert.That(model.Visible.Count, Is.EqualTo(3));
            model.HideAllCategories();
            Assert.That(model.Visible, Is.Empty);
        }

        [Test]
        public void Search_MatchesMessageCategoryAndMetadata_CaseInsensitively()
        {
            model.SetSearch("PLAYER");
            Assert.That(model.Visible.Select(e => e.Sequence), Is.EqualTo(new[] { 0L }));

            model.SetSearch("42");
            Assert.That(model.Visible.Select(e => e.Sequence), Is.EqualTo(new[] { 1L }));

            model.SetSearch("ai");
            Assert.That(model.Visible.Select(e => e.Sequence), Is.EqualTo(new[] { 2L, 3L }), "'Boss failed' matches via 'failed'");
        }

        [Test]
        public void LoggerFilter()
        {
            model.SetLoggerFilter("Net");
            Assert.That(model.Visible.Single().Sequence, Is.EqualTo(1));
            model.SetLoggerFilter(null);
            Assert.That(model.Visible.Count, Is.EqualTo(4));
        }

        [Test]
        public void NewEntries_RespectActiveFilters()
        {
            model.SetLevelVisible(LogLevel.Info, false);
            model.Add(Entry(4, LogLevel.Info, "UI", "hidden"));
            model.Add(Entry(5, LogLevel.Error, "UI", "shown"));

            Assert.That(model.Visible.Last().Sequence, Is.EqualTo(5));
            Assert.That(model.Visible.Any(e => e.Sequence == 4), Is.False);
            Assert.That(model.CountOf(LogLevel.Info), Is.EqualTo(2), "counts include filtered entries");
        }

        [Test]
        public void Capacity_DropsOldestEntries_AndKeepsCountsInSync()
        {
            var small = new LogViewerModel(capacity: 10);
            small.AddRange(Enumerable.Range(0, 25).Select(i => Entry(i, LogLevel.Info, "C", "m" + i)));

            Assert.That(small.Entries.Count, Is.LessThanOrEqualTo(10));
            Assert.That(small.Entries.Last().Sequence, Is.EqualTo(24));
            Assert.That(small.CountOf(LogLevel.Info), Is.EqualTo(small.Entries.Count));
        }

        [Test]
        public void Clear_ResetsEverything()
        {
            model.Clear();
            Assert.That(model.Entries, Is.Empty);
            Assert.That(model.Categories, Is.Empty);
            Assert.That(model.CountOf(LogLevel.Info), Is.EqualTo(0));
        }

        [Test]
        public void PropertyFilters_MustAllMatch_AndCanBeRemoved()
        {
            model.AddRange(new[]
            {
                Entry(4, LogLevel.Warning, "Network", "Packet lost", "Net", ("Peer", "7")),
                Entry(5, LogLevel.Info, "Network", "Connected", "Net", ("Peer", "42"), ("Region", "EU"))
            });

            model.AddPropertyFilter("Peer", "42");
            Assert.That(model.Visible.Select(v => v.Sequence), Is.EqualTo(new long[] { 1, 5 }));

            model.AddPropertyFilter("Region", "EU");
            model.AddPropertyFilter("Region", "EU"); // no duplicates
            Assert.That(model.PropertyFilters.Count, Is.EqualTo(2));
            Assert.That(model.Visible.Select(v => v.Sequence), Is.EqualTo(new long[] { 5 }));

            model.RemovePropertyFilter(new PropertyFilter("Peer", "42"));
            model.RemovePropertyFilter(new PropertyFilter("Region", "EU"));
            Assert.That(model.Visible.Count, Is.EqualTo(6));
        }

        [Test]
        public void Groups_CollapseEntriesByTemplate_MostFrequentFirst()
        {
            const string template = "Lost {Packets} packets";
            model.Clear();
            model.AddRange(new[]
            {
                Entry(0, LogLevel.Info, "Net", "Connected"),
                Entry(1, LogLevel.Info, "Net", "Lost 3 packets", null, (LogPropertyKeys.MessageTemplate, template)),
                Entry(2, LogLevel.Warning, "Net", "Lost 9 packets", null, (LogPropertyKeys.MessageTemplate, template)),
                Entry(3, LogLevel.Info, "Net", "Connected")
            });
            model.Add(Entry(4, LogLevel.Info, "Net", "Lost 1 packets", null, (LogPropertyKeys.MessageTemplate, template)));

            var groups = model.BuildGroups();

            Assert.That(groups.Select(g => g.Key), Is.EqualTo(new[] { template, "Connected" }));
            Assert.That(groups[0].Count, Is.EqualTo(3));
            Assert.That(groups[0].HighestLevel, Is.EqualTo(LogLevel.Warning));
            Assert.That(groups[0].First.Sequence, Is.EqualTo(1));
            Assert.That(groups[0].Last.Sequence, Is.EqualTo(4));

            model.SetGroupFilter(template);
            Assert.That(model.Visible.Select(v => v.Sequence), Is.EqualTo(new long[] { 1, 2, 4 }));
        }

        [Test]
        public void SourceFilter_SeparatesGameAndUnityEntries()
        {
            model.Add(Entry(4, LogLevel.Error, "Unity", "NullReferenceException", null,
                            (LogPropertyKeys.Source, LogPropertyKeys.UnitySource)));

            model.SetSourceFilter(LogSourceFilter.Unity);
            Assert.That(model.Visible.Select(v => v.Sequence), Is.EqualTo(new long[] { 4 }));

            model.SetSourceFilter(LogSourceFilter.Game);
            Assert.That(model.Visible.Count, Is.EqualTo(4));
            Assert.That(model.Visible.Any(v => v.IsFromUnity), Is.False);
        }

        [Test]
        public void ClearFilters_RemovesEveryFilterButLevelsAndCategories()
        {
            model.SetLevelVisible(LogLevel.Debug, false);
            model.AddPropertyFilter("Peer", "42");
            model.SetLoggerFilter("Net");
            model.SetSearch("packet");
            model.SetSourceFilter(LogSourceFilter.Game);
            model.SetGroupFilter("Packet lost");

            model.ClearFilters();

            Assert.That(model.PropertyFilters, Is.Empty);
            Assert.That(model.LoggerFilter, Is.Null);
            Assert.That(model.GroupFilter, Is.Null);
            Assert.That(model.Search, Is.Empty);
            Assert.That(model.SourceFilter, Is.EqualTo(LogSourceFilter.All));
            Assert.That(model.Visible.Count, Is.EqualTo(3), "the Debug level stays hidden");
        }
    }
}
