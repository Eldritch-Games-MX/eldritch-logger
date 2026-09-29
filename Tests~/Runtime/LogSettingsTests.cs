using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Pipeline;
using EldritchGames.EldritchLogger.Settings;
using EldritchGames.EldritchLogger.Sinks.Config;
using NUnit.Framework;
using System;
using System.Linq;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Tests
{
    public class LogSettingsTests
    {
        private LogSettings settings;

        [SetUp]
        public void SetUp() => settings = ScriptableObject.CreateInstance<LogSettings>();

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(settings);

        [Test]
        public void Defaults_EnableEveryBuiltInCategory_AndTheUnityConsoleSink()
        {
            foreach (var category in LogCategory.BuiltIn)
                Assert.That(settings.IsCategoryEnabled(category), Is.True, category.Name);

            Assert.That(settings.sinks.Single(), Is.InstanceOf<UnityConsoleSinkConfig>());
        }

        [Test]
        public void CustomCategories_CanBeAddedAndRemoved_BuiltInsCannotBeRemoved()
        {
            Assert.That(settings.IsCategoryEnabled("Loot"), Is.False);

            Assert.That(settings.AddCategory("Loot", Color.red), Is.True);
            Assert.That(settings.AddCategory("loot", Color.red), Is.False);
            Assert.That(settings.IsCategoryEnabled("LOOT"), Is.True);
            Assert.That(settings.GetCategoryColor("Loot"), Is.EqualTo(Color.red));

            Assert.That(settings.RemoveCategory("Loot"), Is.True);
            Assert.That(settings.RemoveCategory("Gameplay"), Is.False);
            Assert.That(settings.IsCategoryEnabled("Loot"), Is.False);
            Assert.That(settings.IsCategoryEnabled(LogCategory.Gameplay), Is.True);
        }

        [Test]
        public void Filter_UsesLevelAndCategories()
        {
            var filter = new SettingsLogFilter(settings);
            settings.minimumLevel = LogLevel.Warning;
            settings.FindCategory(LogCategory.AI).enabled = false;

            Assert.That(filter.IsEnabled(LogLevel.Info, LogCategory.General), Is.False);
            Assert.That(filter.IsEnabled(LogLevel.Error, LogCategory.General), Is.True);
            Assert.That(filter.IsEnabled(LogLevel.Error, LogCategory.AI), Is.False);
        }

        [Test]
        public void Presets_SetLevelAndBuiltInCategories()
        {
            settings.AddCategory("Loot", Color.white);

            LogSettingsPresets.ApplyProduction(settings);
            Assert.That(settings.minimumLevel, Is.EqualTo(LogLevel.Warning));
            Assert.That(settings.IsCategoryEnabled(LogCategory.Gameplay), Is.True);
            Assert.That(settings.IsCategoryEnabled(LogCategory.UI), Is.False);
            Assert.That(settings.IsCategoryEnabled("Loot"), Is.True, "custom categories are left alone");

            settings.SetAllCategoriesEnabled(false);
            LogSettingsPresets.ApplyVerbose(settings);
            Assert.That(settings.minimumLevel, Is.EqualTo(LogLevel.Debug));
            Assert.That(settings.categories.All(c => c.enabled), Is.True);
        }

        [Test]
        public void SinkConfigs_SurviveSerialization()
        {
            settings.sinks.Add(new JsonLinesFileSinkConfig { fileName = "roundtrip", maxSessionFiles = 3 });
            settings.sinks.Add(new TextFileSinkConfig { enabled = false });

            var copy = UnityEngine.Object.Instantiate(settings);
            try
            {
                Assert.That(copy.sinks.Select(s => s.GetType()),
                    Is.EqualTo(new[] { typeof(UnityConsoleSinkConfig), typeof(JsonLinesFileSinkConfig), typeof(TextFileSinkConfig) }));
                var json = (JsonLinesFileSinkConfig)copy.sinks[1];
                Assert.That(json.fileName, Is.EqualTo("roundtrip"));
                Assert.That(json.maxSessionFiles, Is.EqualTo(3));
                Assert.That(copy.sinks[2].enabled, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(copy);
            }
        }

        // 3 & 4 ------------------------------------------------------------------------------------------

        [Test]
        public void Presets_KeepTheUnityCategoryEnabled()
        {
            var settings = ScriptableObject.CreateInstance<LogSettings>();
            try
            {
                LogSettingsPresets.ApplyProduction(settings);
                Assert.That(settings.IsCategoryEnabled(LogCategory.Unity), Is.True);

                LogSettingsPresets.ApplyNormal(settings);
                Assert.That(settings.IsCategoryEnabled(LogCategory.Unity), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void Deserializing_AddsMissingBuiltInCategories_AndKeepsExistingOnes()
        {
            var settings = ScriptableObject.CreateInstance<LogSettings>();
            try
            {
                JsonUtility.FromJsonOverwrite(
                    "{\"categories\":[{\"name\":\"General\",\"color\":{\"r\":1,\"g\":0,\"b\":0,\"a\":1},\"enabled\":false}," +
                    "{\"name\":\"Loot\",\"color\":{\"r\":0,\"g\":0,\"b\":1,\"a\":1},\"enabled\":true}]}",
                    settings);

                Assert.That(settings.IsCategoryEnabled(LogCategory.Unity), Is.True, "missing built-in added, enabled");
                Assert.That(settings.IsCategoryEnabled(LogCategory.General), Is.False, "existing entries are untouched");
                Assert.That(settings.GetCategoryColor(LogCategory.General), Is.EqualTo(Color.red));
                Assert.That(settings.IsCategoryEnabled("Loot"), Is.True);
                Assert.That(settings.categories.Count, Is.EqualTo(LogCategory.BuiltIn.Count + 1));
                Assert.That(settings.EnsureBuiltInCategories(), Is.False, "nothing left to add");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void CategorySetting_KnowsBuiltIns()
        {
            Assert.That(new CategorySetting("gameplay", Color.white).IsBuiltIn, Is.True);
            Assert.That(new CategorySetting("Unity", Color.white).IsBuiltIn, Is.True);
            Assert.That(new CategorySetting("Loot", Color.white).IsBuiltIn, Is.False);
            Assert.That(new CategorySetting("Loot", Color.white).Category, Is.EqualTo(new LogCategory("loot")));
        }

        [Test]
        public void SetAllCategoriesEnabled_TogglesEverything()
        {
            var settings = ScriptableObject.CreateInstance<LogSettings>();
            try
            {
                settings.AddCategory("Loot", Color.white);
                settings.SetAllCategoriesEnabled(false);
                Assert.That(settings.categories.Any(c => c.enabled), Is.False);
                Assert.That(settings.AddCategory(" ", Color.white), Is.False);
                Assert.That(settings.RemoveCategory(null), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }
    }
}
