using EldritchGames.EldritchLogger.Setttings;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEditor.PackageManager;
using UnityEngine;

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
            EldritchLogger.Log(LogLevel.Info, LogCategory.Gameplay, "Test message");
            Assert.IsTrue(settings.IsCategoryEnabled(LogCategory.Gameplay));
        }

        [Test]
        public void SkipsLog_WhenCategoryDisabled()
        {
            settings.enabledCategories.Remove(LogCategory.Gameplay);
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
    }
}