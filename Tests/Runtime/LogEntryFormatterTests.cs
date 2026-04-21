using UnityEngine;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using EldritchGames.EldritchLogger.Settings;
using EldritchGames.EldritchLogger.Format;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Core;

namespace EldritchGames.EldritchLogger.Tests
{
    [TestFixture]
    public class LogEntryFormatterTests
    {
        private LogSettings settings;
        private LogEntryFormatter formatter;

        [SetUp]
        public void Setup()
        {
            settings = ScriptableObject.CreateInstance<LogSettings>();
            settings.timestampFormat = "HH:mm:ss";
            settings.messagePrefix = "[TEST]";
            settings.filterLoggerFrames = true;

            formatter = new LogEntryFormatter(settings);
        }

        [TearDown]
        public void Cleanup()
        {
            UnityEngine.Object.DestroyImmediate(settings);
        }

        [Test]
        public void Format_IncludesTimestampPrefixAndMessage()
        {
            var dto = new LogEntryDto
            {
                Timestamp = DateTime.Parse("2026-04-08T10:00:00"),
                Level = LogLevel.Info.ToString(),
                Category = LogCategory.Gameplay.ToString(),
                Message = "Sample message"
            };

            string output = formatter.Format(dto);

            StringAssert.Contains("[10:00:00]", output);
            StringAssert.Contains("[TEST]Sample message", output);
        }

        [Test]
        public void Format_IncludesColoredCategory()
        {
            var dto = new LogEntryDto
            {
                Timestamp = DateTime.Now,
                Level = LogLevel.Debug.ToString(),
                Category = LogCategory.UI.ToString(),
                Message = "UI message"
            };

            string output = formatter.Format(dto);

            StringAssert.Contains("<color=", output);
            StringAssert.Contains("UI</color>", output);
        }

        [Test]
        public void Format_MetadataIsRenderedCorrectly()
        {
            var dto = new LogEntryDto
            {
                Timestamp = DateTime.Now,
                Level = LogLevel.Info.ToString(),
                Category = LogCategory.General.ToString(),
                Message = "Message with metadata",
                Metadata = new List<MetadataEntry>
                {
                    new MetadataEntry { Key = "GameObject", Value = "PlayerPawn" },
                    new MetadataEntry { Key = "CustomKey", Value = "CustomValue" }
                }
            };

            string output = formatter.Format(dto);

            StringAssert.Contains("[GameObject=PlayerPawn]", output);
            StringAssert.Contains("CustomKey=CustomValue", output);
        }

        [Test]
        public void Format_ExceptionIncludesMessage()
        {
            var dto = new LogEntryDto
            {
                Timestamp = DateTime.Now,
                Level = LogLevel.Error.ToString(),
                Category = LogCategory.General.ToString(),
                Message = "Error occurred",
                Exception = "InvalidOperationException: Boom!"
            };

            string output = formatter.Format(dto);

            StringAssert.Contains("InvalidOperationException", output);
            StringAssert.Contains("Boom!", output);
        }

        [Test]
        public void Format_CategoryColorRespectsToggle_Disabled()
        {
            settings.useCategoryColors = false;

            var dto = new LogEntryDto
            {
                Timestamp = DateTime.Now,
                Level = LogLevel.Info.ToString(),
                Category = LogCategory.Gameplay.ToString(),
                Message = "Gameplay message"
            };

            string output = formatter.Format(dto);

            // Should contain the category name but no color markup
            StringAssert.Contains("Gameplay", output);
            StringAssert.DoesNotContain("<color=", output);
        }

        [Test]
        public void Format_CategoryColorRespectsToggle_Enabled()
        {
            settings.useCategoryColors = true;

            var dto = new LogEntryDto
            {
                Timestamp = DateTime.Now,
                Level = LogLevel.Info.ToString(),
                Category = LogCategory.Gameplay.ToString(),
                Message = "Gameplay message"
            };

            string output = formatter.Format(dto);

            // Should wrap the category name in color tags
            StringAssert.Contains("<color=", output);
            StringAssert.Contains("Gameplay</color>", output);
        }

    }
}
