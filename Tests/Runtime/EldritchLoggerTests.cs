using EldritchGames.EldritchLogger.Setttings;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

namespace EldritchGames.EldritchLogger.Tests
{
    public class EldritchLoggerTests
    {
        private LogSettings settings;

        [SetUp]
        public void Setup()
        {
            settings = ScriptableObject.CreateInstance<LogSettings>();
            settings.logLevel = LogLevel.Debug;
            settings.enabledCategories = new List<LogCategory> { LogCategory.Gameplay };
            EldritchLogger.Initialize(settings);
        }

        [Test]
        public void LogsGameplayEvent_WhenCategoryEnabled()
        {
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Test message"));

            EldritchLogger.Log(LogLevel.Info, LogCategory.Gameplay, "Test message");

            Assert.IsTrue(settings.IsCategoryEnabled(LogCategory.Gameplay));
        }

        [Test]
        public void SkipsLog_WhenCategoryDisabled()
        {
            settings.enabledCategories.Remove(LogCategory.Gameplay);

            // No log expected
            EldritchLogger.Log(LogLevel.Info, LogCategory.Gameplay, "Should not log");

            Assert.IsFalse(settings.IsCategoryEnabled(LogCategory.Gameplay));
        }

        [Test]
        public void FormatsLogEntry_WithColor()
        {
            var entry = new LogEntry
            {
                Timestamp = System.DateTime.Now,
                Level = LogLevel.Info,
                Category = LogCategory.Gameplay,
                Message = "Colored message"
            };

            string formatted = entry.ToString();
            Assert.IsTrue(formatted.Contains("<color=green>Gameplay</color>"));
        }

        [Test]
        public void FluentBuilder_LogsMessageWithoutMetadata()
        {
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Builder test message"));

            EldritchLogger.AtInfo()
                .Category(LogCategory.Gameplay)
                .Log("Builder test message");
        }

        [Test]
        public void FluentBuilder_AddsMetadataCorrectly()
        {
            LogAssert.Expect(LogType.Error,
                new System.Text.RegularExpressions.Regex("Player not Found.*PLAYER_ID=42.*SESSION_ID=abc123"));

            EldritchLogger.AtError()
                .Category(LogCategory.Gameplay)
                .AddKeyValue("PLAYER_ID", 42)
                .AddKeyValue("SESSION_ID", "abc123")
                .Log("Player not Found");
        }

        [Test]
        public void FluentBuilder_SkipsLog_WhenCategoryDisabled()
        {
            settings.enabledCategories.Remove(LogCategory.Gameplay);

            // No log expected
            EldritchLogger.AtWarning()
                .Category(LogCategory.Gameplay)
                .Log("Should not log");
        }

        [Test]
        public void FluentBuilder_IsImmutable()
        {
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Original message"));
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Message with metadata.*KEY=VALUE"));

            var builder = EldritchLogger.AtDebug().Category(LogCategory.Gameplay);
            var builderWithMetadata = builder.AddKeyValue("KEY", "VALUE");

            builder.Log("Original message");
            builderWithMetadata.Log("Message with metadata");
        }

        [Test]
        public void CriticalLog_IsPrefixedCorrectly()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("CRITICAL:.*Critical failure"));

            EldritchLogger.AtCritical()
                .Category(LogCategory.Gameplay)
                .Log("Critical failure");
        }
    }
}