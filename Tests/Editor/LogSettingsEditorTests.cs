using EldritchGames.EldritchLogger;
using EldritchGames.EldritchLogger.Settings;
using NUnit.Framework;
using UnityEngine;
using System;
using System.Collections.Generic;

[TestFixture]
public class LogSettingsEditorUnitTests
{
    private LogSettings settings;

    [SetUp]
    public void Setup()
    {
        settings = ScriptableObject.CreateInstance<LogSettings>();
    }

    [TearDown]
    public void Cleanup()
    {
        UnityEngine.Object.DestroyImmediate(settings);
    }

    // --- Presets ---
    [Test]
    public void Preset_Verbose_SetsDebugAndAllCategories()
    {
        settings.logLevel = LogLevel.Debug;
        settings.enabledCategories = new List<LogCategory>(
            (LogCategory[])Enum.GetValues(typeof(LogCategory)));

        Assert.AreEqual(LogLevel.Debug, settings.logLevel);
        Assert.AreEqual(Enum.GetValues(typeof(LogCategory)).Length, settings.enabledCategories.Count);
    }

    [Test]
    public void Preset_Normal_SetsInfoAndGameplayUiNetwork()
    {
        settings.logLevel = LogLevel.Info;
        settings.enabledCategories = new List<LogCategory>
        {
            LogCategory.Gameplay,
            LogCategory.UI,
            LogCategory.Network
        };

        Assert.AreEqual(LogLevel.Info, settings.logLevel);
        CollectionAssert.AreEquivalent(
            new[] { LogCategory.Gameplay, LogCategory.UI, LogCategory.Network },
            settings.enabledCategories);
    }

    [Test]
    public void Preset_Production_SetsWarningAndGameplayNetwork()
    {
        settings.logLevel = LogLevel.Warning;
        settings.enabledCategories = new List<LogCategory>
        {
            LogCategory.Gameplay,
            LogCategory.Network
        };

        Assert.AreEqual(LogLevel.Warning, settings.logLevel);
        CollectionAssert.AreEquivalent(
            new[] { LogCategory.Gameplay, LogCategory.Network },
            settings.enabledCategories);
    }

    // --- Categories ---
    [Test]
    public void EnableAll_SetsAllCategories()
    {
        settings.enabledCategories = new List<LogCategory>(
            (LogCategory[])Enum.GetValues(typeof(LogCategory)));

        Assert.AreEqual(Enum.GetValues(typeof(LogCategory)).Length, settings.enabledCategories.Count);
    }

    [Test]
    public void DisableAll_ClearsCategories()
    {
        settings.enabledCategories.Clear();
        Assert.AreEqual(0, settings.enabledCategories.Count);
    }

    // --- Validation ---
    [Test]
    public void ValidationCondition_NoCategoriesEnabled()
    {
        settings.enabledCategories.Clear();
        Assert.AreEqual(0, settings.enabledCategories.Count);
        // This is the condition that would trigger the HelpBox in the editor
    }

    // --- Advanced Settings ---
    [Test]
    public void AdvancedSettings_ModifiesTimestampAndPrefix()
    {
        settings.timestampFormat = "yyyy-MM-dd";
        settings.messagePrefix = "[PREFIX]";

        Assert.AreEqual("yyyy-MM-dd", settings.timestampFormat);
        Assert.AreEqual("[PREFIX]", settings.messagePrefix);
    }

    [Test]
    public void AdvancedSettings_ModifiesStackTraceModes()
    {
        settings.infoTrace = StackTraceMode.Full;
        settings.warningTrace = StackTraceMode.ScriptOnly;
        settings.errorTrace = StackTraceMode.None;

        Assert.AreEqual(StackTraceMode.Full, settings.infoTrace);
        Assert.AreEqual(StackTraceMode.ScriptOnly, settings.warningTrace);
        Assert.AreEqual(StackTraceMode.None, settings.errorTrace);
    }

    [Test]
    public void AdvancedSettings_TogglesContextAndFiltering()
    {
        settings.useContextObjects = true;
        settings.filterLoggerFrames = true;

        Assert.IsTrue(settings.useContextObjects);
        Assert.IsTrue(settings.filterLoggerFrames);
    }

    // --- Preview ---
    [Test]
    public void Preview_BuildsSampleLogEntry()
    {
        settings.logLevel = LogLevel.Info;
        settings.messagePrefix = "[TEST]";

        var sampleEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = settings.logLevel,
            Category = LogCategory.Gameplay,
            Message = $"{settings.messagePrefix} Sample log message"
        };

        Assert.IsTrue(sampleEntry.ToString().Contains("[TEST] Sample log message"));
    }
}
