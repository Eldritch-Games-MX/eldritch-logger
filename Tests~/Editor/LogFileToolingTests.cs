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

        [Test]
        public void FindActive_ReturnsTheLoadableAssetIfThereIsOne()
        {
            var loadable = LogSettingsAssets.FindAllPaths().Where(LogSettingsAssets.IsLoadablePath).ToArray();
            var active = LogSettingsAssets.FindActive();

            if (loadable.Length == 0)
                Assert.That(active, Is.Null);
            else
                Assert.That(loadable, Does.Contain(UnityEditor.AssetDatabase.GetAssetPath(active)));
        }

        [Test]
        public void LiveCapture_EnabledPreference_RoundTrips()
        {
            bool original = LiveLogCapture.Enabled;
            try
            {
                LiveLogCapture.Enabled = !original;
                Assert.That(LiveLogCapture.Enabled, Is.EqualTo(!original));
            }
            finally
            {
                LiveLogCapture.Enabled = original;
            }
            Assert.That(LiveLogCapture.Sink, Is.Not.Null);
        }

        [Test]
        public void EditorSink_Clear_ResetsTheSequence()
        {
            var sink = new EditorLogSink(capacity: 3);
            sink.Emit(new LogEntryDto { Message = "a" });
            sink.Clear();

            var batch = new List<LogViewerEntry>();
            Assert.That(sink.CopySince(0, batch), Is.EqualTo(0));
            Assert.That(batch, Is.Empty);
            Assert.That(sink.Name, Is.Not.Empty);
            Assert.That(sink.MinimumLevel, Is.EqualTo(LogLevel.Debug));
        }

        [Test]
        public void Reader_DetectsJsonLinesFiles()
        {
            Assert.That(JsonLinesLogReader.IsJsonLines("a/b/log.JSONL"), Is.True);
            Assert.That(JsonLinesLogReader.IsJsonLines("log.json"), Is.False);
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
