using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Pipeline;
using EldritchGames.EldritchLogger.Settings;
using NUnit.Framework;
using System.Linq;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Tests
{
    public class LogControlTests
    {
        private LogSettings settings;

        [SetUp]
        public void SetUp() => settings = ScriptableObject.CreateInstance<LogSettings>();

        [TearDown]
        public void TearDown()
        {
            LoggerBootstrap.Shutdown();
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void Overrides_ApplyOnTopOfSettings_AndCanBeCleared()
        {
            var filter = new SettingsLogFilter(settings);

            filter.MinimumLevelOverride = LogLevel.Error;
            filter.SetCategoryOverride("Loot", true);        // not registered in settings
            filter.SetCategoryOverride(LogCategory.AI, false);

            Assert.That(filter.MinimumLevel, Is.EqualTo(LogLevel.Error));
            Assert.That(filter.IsEnabled(LogLevel.Warning, LogCategory.General), Is.False);
            Assert.That(filter.IsEnabled(LogLevel.Error, "Loot"), Is.True);
            Assert.That(filter.IsEnabled(LogLevel.Error, LogCategory.AI), Is.False);

            var states = filter.Categories;
            Assert.That(states.Single(s => s.Category == LogCategory.AI).Overridden, Is.True);
            Assert.That(states.Single(s => s.Category == new LogCategory("loot")).Enabled, Is.True);

            filter.ClearOverrides();
            Assert.That(filter.MinimumLevelOverride, Is.Null);
            Assert.That(filter.IsEnabled(LogLevel.Debug, LogCategory.AI), Is.True);
            Assert.That(filter.IsEnabled(LogLevel.Error, "Loot"), Is.False);
        }

        [Test]
        public void SettingsChanges_StillApply_WhenNotOverridden()
        {
            var filter = new SettingsLogFilter(settings);
            settings.minimumLevel = LogLevel.Warning;
            Assert.That(filter.MinimumLevel, Is.EqualTo(LogLevel.Warning));

            filter.SetCategoryOverride(LogCategory.UI, null); // removing a missing override is harmless
            Assert.That(filter.IsCategoryEnabled(LogCategory.UI), Is.True);
        }

        [Test]
        public void Control_IsExposedThroughTheFactory_OnlyForSettingsLoggers()
        {
            Assert.That(ELoggerFactory.Control, Is.Null);

            var logger = EldritchLoggerBuilder.FromSettings(settings).ClearSinks().Build();
            LoggerBootstrap.Install(logger);
            Assert.That(ELoggerFactory.Control, Is.SameAs(logger.Control));

            ELoggerFactory.Control.MinimumLevelOverride = LogLevel.Critical;
            Assert.That(logger.IsEnabled(LogLevel.Error, LogCategory.General), Is.False);
            Assert.DoesNotThrow(() => ELoggerFactory.Control.Flush());

            using var plain = new EldritchLoggerBuilder().Build();
            Assert.That(plain.Control, Is.Null, "custom filters have no settings to override");
        }
    }
}
