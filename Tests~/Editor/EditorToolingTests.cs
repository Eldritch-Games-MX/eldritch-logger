using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.EditorTools.CodeGen;
using EldritchGames.EldritchLogger.EditorTools.LogViewer;
using EldritchGames.EldritchLogger.EditorTools.ProjectSettings;
using EldritchGames.EldritchLogger.Sinks.Files;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EldritchGames.EldritchLogger.Tests.Editor
{
    public class CategoryCodeGeneratorTests
    {
        [TestCase("Loot", "Loot")]
        [TestCase("loot drops", "LootDrops")]
        [TestCase("AI-Nav/Path", "AINavPath")]
        [TestCase("3D", "_3D")]
        [TestCase("class", "Class")]
        [TestCase("!!!", "Category")]
        public void ToIdentifier(string name, string expected)
        {
            Assert.That(CategoryCodeGenerator.ToIdentifier(name), Is.EqualTo(expected));
        }

        [Test]
        public void Generate_WritesOneFieldPerCategory_InANamespace()
        {
            var code = CategoryCodeGenerator.Generate(new[] { "General", "loot drops", "Loot-Drops", "LOOT DROPS", "All", "Say \"hi\"" },
                                                      "Game.Logging", "GameCategories");

            StringAssert.Contains("namespace Game.Logging", code);
            StringAssert.Contains("public static class GameCategories", code);
            StringAssert.Contains("public static readonly LogCategory General = new LogCategory(\"General\");", code);
            StringAssert.Contains("public static readonly LogCategory LootDrops = new LogCategory(\"loot drops\");", code);
            StringAssert.Contains("public static readonly LogCategory LootDrops2 = new LogCategory(\"Loot-Drops\");", code);
            StringAssert.DoesNotContain("\"LOOT DROPS\"", code, "names differing only by case are the same category");
            StringAssert.Contains("public static readonly LogCategory All2 = new LogCategory(\"All\");", code);
            StringAssert.Contains("new LogCategory(\"Say \\\"hi\\\"\")", code);
            StringAssert.Contains("public static readonly LogCategory[] All = { General, LootDrops, LootDrops2, All2, SayHi };", code);
        }

        [Test]
        public void Generate_WithoutNamespace_UsesTheGlobalNamespace()
        {
            var code = CategoryCodeGenerator.Generate(Array.Empty<string>(), "", "");

            StringAssert.DoesNotContain("namespace", code);
            StringAssert.Contains("public static class LogCategories", code);
            StringAssert.Contains("public static readonly LogCategory[] All = { };", code);
        }
    }

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
    }

    public class LogFileToolingTests
    {
        private string path;

        [SetUp]
        public void SetUp() => path = Path.Combine(Path.GetTempPath(), $"eldritch_viewer_{Guid.NewGuid():N}.jsonl");

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(path)) File.Delete(path);
        }

        [Test]
        public void Reader_RoundTripsTheJsonLinesSink_AndCountsBadLines()
        {
            using (var sink = new JsonLinesFileSink(path))
            {
                sink.Emit(new LogEntryDto { Timestamp = DateTime.UtcNow, Level = LogLevel.Warning, Category = "Net", Message = "lost",
                                            Metadata = new List<MetadataEntry> { new() { Key = "Peer", Value = "7" } } });
                sink.Emit(new LogEntryDto { Timestamp = DateTime.UtcNow, Level = LogLevel.Info, Category = "UI", Message = "click" });
            }
            File.AppendAllText(path, "{\"Level\": \"Info\", \"Mess");

            var entries = JsonLinesLogReader.Read(path, out int invalid);

            Assert.That(invalid, Is.EqualTo(1));
            Assert.That(entries.Count, Is.EqualTo(2));
            Assert.That(entries[0].Level, Is.EqualTo(LogLevel.Warning));
            Assert.That(entries[0].GetMetadata("Peer"), Is.EqualTo("7"));
            Assert.That(entries[0].Timestamp.Kind, Is.EqualTo(DateTimeKind.Utc));
        }

        [Test]
        public void EditorSink_ReturnsEntriesSinceACursor_AndSurvivesOverflow()
        {
            var sink = new EditorLogSink(capacity: 5);
            for (int i = 0; i < 3; i++) sink.Emit(new LogEntryDto { Message = "a" + i });

            var batch = new List<LogViewerEntry>();
            long cursor = sink.CopySince(0, batch);
            Assert.That(batch.Select(e => e.Entry.Message), Is.EqualTo(new[] { "a0", "a1", "a2" }));

            for (int i = 0; i < 10; i++) sink.Emit(new LogEntryDto { Message = "b" + i });
            batch.Clear();
            cursor = sink.CopySince(cursor, batch);

            Assert.That(batch.Select(e => e.Entry.Message), Is.EqualTo(new[] { "b5", "b6", "b7", "b8", "b9" }), "only the retained window");
            Assert.That(cursor, Is.EqualTo(13));
        }

        [TestCase("Assets/Resources/LogSettings.asset", true)]
        [TestCase("Assets/Game/Resources/LogSettings.asset", true)]
        [TestCase("Assets/Settings/LogSettings.asset", false)]
        [TestCase("Assets/Resources/OtherSettings.asset", false)]
        public void LoadablePath(string assetPath, bool expected)
        {
            Assert.That(LogSettingsAssets.IsLoadablePath(assetPath), Is.EqualTo(expected));
        }
    }
}
