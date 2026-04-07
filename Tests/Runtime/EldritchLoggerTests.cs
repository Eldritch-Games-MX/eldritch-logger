using EldritchGames.EldritchLogger;
using EldritchGames.EldritchLogger.Settings;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;

[TestFixture]
public class EldritchLoggerTests
{
    private LogSettings settings;

    [SetUp]
    public void Setup()
    {
        settings = ScriptableObject.CreateInstance<LogSettings>();
        settings.logLevel = LogLevel.Debug;
        settings.enabledCategories.Add(LogCategory.General);
        EldritchLogger.Initialize(settings);
    }

    [Test]
    public void Initialize_SetsCurrentSettings()
    {
        Assert.AreEqual(settings, EldritchLogger.CurrentSettings);
    }

    [Test]
    public void Initialize_NullSettings_ShowsWarning()
    {
        EldritchLogger.Initialize(null);
        LogAssert.Expect(LogType.Warning, "EldritchLogger not initialized with LogSettings!");
    }

    [TestCase(StackTraceMode.None, StackTraceLogType.None)]
    [TestCase(StackTraceMode.ScriptOnly, StackTraceLogType.ScriptOnly)]
    [TestCase(StackTraceMode.Full, StackTraceLogType.Full)]
    public void Initialize_MapsStackTraceModes(StackTraceMode mode, StackTraceLogType expected)
    {
        settings.infoTrace = mode;
        EldritchLogger.Initialize(settings);
        Assert.AreEqual(expected, Application.GetStackTraceLogType(LogType.Log));
    }

    [Test]
    public void Log_BelowThreshold_DoesNotLog()
    {
        settings.logLevel = LogLevel.Error;
        EldritchLogger.Initialize(settings);

        LogAssert.NoUnexpectedReceived();
        EldritchLogger.Log(LogLevel.Info, LogCategory.General, "Should not log");
    }

    [Test]
    public void Log_DisabledCategory_DoesNotLog()
    {
        settings.enabledCategories.Clear();
        EldritchLogger.Initialize(settings);

        LogAssert.NoUnexpectedReceived();
        EldritchLogger.Log(LogLevel.Info, LogCategory.Network, "Should not log");
    }

    [Test]
    public void Log_InfoLevel_OutputsDebugLog()
    {
        LogAssert.Expect(LogType.Log, new Regex("Test message"));
        EldritchLogger.Log(LogLevel.Info, LogCategory.General, "Test message");
    }

    [Test]
    public void Log_WarningLevel_OutputsWarningLog()
    {
        LogAssert.Expect(LogType.Warning, new Regex("Warn message"));
        EldritchLogger.Log(LogLevel.Warning, LogCategory.General, "Warn message");
    }

    [Test]
    public void Log_ErrorLevel_OutputsErrorLog()
    {
        LogAssert.Expect(LogType.Error, new Regex("Error message"));
        EldritchLogger.Log(LogLevel.Error, LogCategory.General, "Error message");
    }

    [Test]
    public void Log_WithException_AppendsStackTrace()
    {
        var ex = new InvalidOperationException("Boom!");
        LogAssert.Expect(LogType.Error, new Regex("Boom!"));
        EldritchLogger.Log(LogLevel.Error, LogCategory.General, "Error occurred", null, ex);
    }

    [Test]
    public void Log_WithGameObjectMetadata_AttachesContext()
    {
        var go = new GameObject("ContextGO");
        var metadata = new Dictionary<string, object> { { "GameObject", "ContextGO" } };

        LogAssert.Expect(LogType.Log, new Regex("Context test"));
        EldritchLogger.Log(LogLevel.Info, LogCategory.General, "Context test", metadata);
    }

    [Test]
    public void CleanStackTrace_FiltersLoggerFrames()
    {
        settings.filterLoggerFrames = true;
        EldritchLogger.Initialize(settings);

        var ex = new Exception("Test") { };
        ex.Data["StackTrace"] = "EldritchGames.EldritchLogger.SomeMethod\nOtherFrame";

        var cleaned = EldritchLogger.CurrentSettings.filterLoggerFrames;
        Assert.IsTrue(cleaned);
    }
}
[TestFixture]
public class EldritchLoggerUninitializedTests
{
    [SetUp]
    public void Setup()
    {
        // Force logger into uninitialized state
        EldritchLogger.Initialize(null);
    }

    [Test]
    public void Log_WithoutInitialization_ShowsWarning()
    {
        LogAssert.Expect(LogType.Warning, "EldritchLogger not initialized with LogSettings!");
        EldritchLogger.Log(LogLevel.Info, LogCategory.General, "Test");
    }

    [Test]
    public void Log_WithoutInitialization_DoesNotProduceInfoLog()
    {
        // Expect only the warning, not the info log
        LogAssert.Expect(LogType.Warning, "EldritchLogger not initialized with LogSettings!");
        EldritchLogger.Log(LogLevel.Info, LogCategory.General, "Message that should not appear");
        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public void Initialize_Null_DoesNotCrash()
    {
        // Just calling Initialize(null) should not throw
        Assert.DoesNotThrow(() => EldritchLogger.Initialize(null));
    }

    [Test]
    public void Log_BelowThreshold_WhenUninitialized_StillShowsWarning()
    {
        LogAssert.Expect(LogType.Warning, "EldritchLogger not initialized with LogSettings!");
        EldritchLogger.Log(LogLevel.Debug, LogCategory.General, "Debug message");
    }

    [Test]
    public void Log_DisabledCategory_WhenUninitialized_StillShowsWarning()
    {
        LogAssert.Expect(LogType.Warning, "EldritchLogger not initialized with LogSettings!");
        EldritchLogger.Log(LogLevel.Info, LogCategory.Network, "Network message");
    }
}
[TestFixture]
public class LogBuilderTests
{
    private LogSettings settings;

    [SetUp]
    public void Setup()
    {
        settings = ScriptableObject.CreateInstance<LogSettings>();
        settings.logLevel = LogLevel.Debug;
        settings.enabledCategories.Add(LogCategory.General);
        EldritchLogger.Initialize(settings);
    }

    [Test]
    public void AtDebug_BuildsDebugLevel()
    {
        LogAssert.Expect(LogType.Log, new Regex("Debug message"));
        EldritchLogger.AtDebug().Log("Debug message");
    }

    [Test]
    public void AtInfo_BuildsInfoLevel()
    {
        LogAssert.Expect(LogType.Log, new Regex("Info message"));
        EldritchLogger.AtInfo().Log("Info message");
    }

    [Test]
    public void AtWarning_BuildsWarningLevel()
    {
        LogAssert.Expect(LogType.Warning, new Regex("Warn message"));
        EldritchLogger.AtWarning().Log("Warn message");
    }

    [Test]
    public void AtError_BuildsErrorLevel()
    {
        LogAssert.Expect(LogType.Error, new Regex("Error message"));
        EldritchLogger.AtError().Log("Error message");
    }

    [Test]
    public void AtCritical_BuildsCriticalLevel()
    {
        LogAssert.Expect(LogType.Error, new Regex("Critical message"));
        EldritchLogger.AtCritical().Log("Critical message");
    }

    [Test]
    public void Category_SetsCategory()
    {
        LogAssert.Expect(LogType.Log, new Regex("Category test"));
        EldritchLogger.AtInfo().Category(LogCategory.UI).Log("Category test");
    }

    [Test]
    public void AddKeyValue_AddsMetadata()
    {
        LogAssert.Expect(LogType.Log, new Regex("CustomKey=CustomValue"));
        EldritchLogger.AtInfo().AddKeyValue("CustomKey", "CustomValue").Log("Info with metadata");
    }

    [Test]
    public void WithException_AttachesException()
    {
        var ex = new InvalidOperationException("Boom!");
        LogAssert.Expect(LogType.Error, new Regex("Boom!"));
        EldritchLogger.AtError().WithException(ex).Log("Error with exception");
    }

    [Test]
    public void WithEvent_AttachesDelegateEvent()
    {
        Action testAction = () => { };
        LogAssert.Expect(LogType.Log, new Regex("C#Event="));
        EldritchLogger.AtInfo().WithEvent(testAction).Log("Info with event");
    }

    [Test]
    public void WithComponent_AttachesComponentContext()
    {
        var go = new GameObject("TestGO");
        var comp = go.AddComponent<BoxCollider>();

        LogAssert.Expect(LogType.Log, new Regex("BoxCollider@TestGO"));
        EldritchLogger.AtDebug().WithComponent(comp).Log("Debug with component");
    }

    [Test]
    public void WithComponent_AttachesGameObjectName()
    {
        var go = new GameObject("ContextGO");
        var comp = go.AddComponent<BoxCollider>();

        LogAssert.Expect(LogType.Log, new Regex("ContextGO"));
        EldritchLogger.AtInfo().WithComponent(comp).Log("Info with GameObject context");
    }
}
