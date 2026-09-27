using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Pipeline;
using EldritchGames.EldritchLogger.Settings;
using EldritchGames.EldritchLogger.Sinks.Config;
using NUnit.Framework;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Tests
{
    public class LogSettingsTests
    {
        private LogSettings settings;

        [SetUp]
        public void SetUp() => settings = ScriptableObject.CreateInstance<LogSettings>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(settings);

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

            var copy = Object.Instantiate(settings);
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
                Object.DestroyImmediate(copy);
            }
        }

        [Test]
        public void LegacyAsset_IsMigrated()
        {
            const string folder = "Assets/__EldritchLoggerTests";
            const string path = folder + "/LegacyLogSettings.asset";
            var scriptGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(settings)));
            Directory.CreateDirectory(folder);
            File.WriteAllText(path, LegacyYaml(scriptGuid));

            try
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var legacy = AssetDatabase.LoadAssetAtPath<LogSettings>(path);

                Assert.That(legacy, Is.Not.Null);
                Assert.That(legacy.minimumLevel, Is.EqualTo(LogLevel.Warning));
                Assert.That(legacy.categories.Select(c => c.name).Take(9), Is.EqualTo(LogCategory.BuiltIn.Select(c => c.Name)));
                Assert.That(legacy.IsCategoryEnabled(LogCategory.General), Is.True);
                Assert.That(legacy.IsCategoryEnabled(LogCategory.UI), Is.True);
                Assert.That(legacy.IsCategoryEnabled(LogCategory.Gameplay), Is.False);
                Assert.That(legacy.GetCategoryColor(LogCategory.UI), Is.EqualTo(Color.red));
                Assert.That(legacy.IsCategoryEnabled("Loot"), Is.True);
                Assert.That(legacy.GetCategoryColor("Loot"), Is.EqualTo(Color.blue));

                var console = legacy.sinks.OfType<UnityConsoleSinkConfig>().Single();
                Assert.That(console.suppressUnityStackTrace, Is.False);
                Assert.That(console.useContextObjects, Is.False);
            }
            finally
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }

        [Test]
        public void LegacyAsset_WithoutColorList_GetsDefaultColors()
        {
            const string folder = "Assets/__EldritchLoggerTests";
            const string path = folder + "/NoColors.asset";
            var scriptGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(settings)));
            Directory.CreateDirectory(folder);
            var yaml = LegacyYaml(scriptGuid);
            yaml = yaml.Substring(0, yaml.IndexOf("  categoryColors:")) + "  customCategories: []\n";
            File.WriteAllText(path, yaml);

            try
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var legacy = AssetDatabase.LoadAssetAtPath<LogSettings>(path);

                Assert.That(legacy.GetCategoryColor(LogCategory.Gameplay), Is.EqualTo(Color.green));
                Assert.That(legacy.IsCategoryEnabled(LogCategory.UI), Is.True);
            }
            finally
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }

        private static string LegacyYaml(string scriptGuid) => $@"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {scriptGuid}, type: 3}}
  m_Name: LegacyLogSettings
  m_EditorClassIdentifier:
  logLevel: 2
  enabledCategories: 0000000002000000
  clearOnStartup: 1
  timestampFormat: HH:mm:ss
  messagePrefix:
  enableExport: 0
  exportFileName: eldritch_logs
  exportDirectory:
  useContextObjects: 0
  suppressUnityStackTrace: 0
  filterLoggerFrames: 1
  useCategoryColors: 1
  categoryColors:
  - category: 0
    color: {{r: 1, g: 1, b: 1, a: 1}}
  - category: 2
    color: {{r: 1, g: 0, b: 0, a: 1}}
  customCategories:
  - name: Loot
    color: {{r: 0, g: 0, b: 1, a: 1}}
    enabled: 1
";
    }
}
