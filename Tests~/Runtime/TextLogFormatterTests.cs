using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Formatting;
using EldritchGames.EldritchLogger.Settings;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Tests
{
    public class TextLogFormatterTests
    {
        private LogSettings settings;

        [SetUp]
        public void SetUp() => settings = ScriptableObject.CreateInstance<LogSettings>();

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(settings);

        private static LogEntryDto Entry() => new()
        {
            Timestamp = new DateTime(2026, 5, 6, 7, 8, 9, DateTimeKind.Local),
            Level = LogLevel.Warning,
            Category = "Gameplay",
            Message = "Hello",
            Metadata = new List<MetadataEntry>
            {
                new() { Key = LogPropertyKeys.GameObject, Value = "Player" },
                new() { Key = "Id", Value = "7" }
            },
            Exception = "InvalidOperationException: nope"
        };

        [Test]
        public void Plain_HasNoColorTags()
        {
            var text = new TextLogFormatter(settings, richText: false).Format(Entry());

            Assert.That(text, Does.Not.Contain("<color"));
            Assert.That(text, Is.EqualTo("[07:08:09] [Warning] Gameplay Hello [GameObject=Player] Id=7\nException: InvalidOperationException: nope"));
        }

        [Test]
        public void RichText_ColorsTheCategory()
        {
            var text = new TextLogFormatter(settings, richText: true).Format(Entry());

            Assert.That(text, Does.Contain("<color=#00FF00>Gameplay</color>"));
        }

        [Test]
        public void RichText_WithCategoryColorsDisabled_IsPlain()
        {
            settings.useCategoryColors = false;

            Assert.That(new TextLogFormatter(settings, richText: true).Format(Entry()), Does.Not.Contain("<color"));
        }

        [Test]
        public void NullSettingsOrEmptyFormat_UseDefaults()
        {
            Assert.That(new TextLogFormatter(null, richText: true).Format(Entry()), Does.StartWith("[07:08:09] [Warning] Gameplay Hello"));

            settings.timestampFormat = "";
            settings.messagePrefix = ">> ";
            Assert.That(new TextLogFormatter(settings, false).Format(Entry()), Does.StartWith("[07:08:09] [Warning] Gameplay >> Hello"));
        }

        [Test]
        public void UtcTimestamps_AreShownInLocalTime()
        {
            var entry = Entry();
            var local = entry.Timestamp;
            entry.Timestamp = local.ToUniversalTime();

            Assert.That(new TextLogFormatter(null, false).Format(entry), Does.StartWith($"[{local:HH:mm:ss}]"));
        }
    }
}
