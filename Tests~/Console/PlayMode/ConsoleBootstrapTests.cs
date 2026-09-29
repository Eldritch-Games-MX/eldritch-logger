using EldritchGames.EldritchLogger.Console.Execution;
using EldritchGames.EldritchLogger.Console.Services;
using EldritchGames.EldritchLogger.Console.Settings;
using EldritchGames.EldritchLogger.Console.UI;
using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Settings;
using NUnit.Framework;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace EldritchGames.EldritchLogger.Console.Tests.PlayMode
{
    /// <summary>End-to-end tests of the console composition root.</summary>
    public class ConsoleBootstrapTests
    {
        private GameObject root;
        private ConsoleView view;
        private CommandConsoleSettings settings;
        private Core.EldritchLogger logger;
        private RecordingPlayModeSink loggerSink;

        private sealed class RecordingPlayModeSink : Sinks.ILogSink
        {
            public int Count;
            public string Name => "PlayMode recorder";
            public LogLevel MinimumLevel => LogLevel.Debug;
            public void Emit(Dto.LogEntryDto entry) => Count++;
        }

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

        /// <summary>Builds Root(view + canvas) / Bootstrap, configured while inactive so Awake sees the fields.</summary>
        private ConsoleBootstrap CreateConsole(Action<CommandConsoleSettings> configure = null)
        {
            settings = ScriptableObject.CreateInstance<CommandConsoleSettings>();
            settings.poolSize = 5;
            configure?.Invoke(settings);

            root = new GameObject("Console Root");
            root.SetActive(false);
            var canvas = root.AddComponent<Canvas>();
            var content = new GameObject("Content", typeof(RectTransform)).transform;
            content.SetParent(root.transform);
            var prefab = new GameObject("Line", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            prefab.transform.SetParent(root.transform);

            view = root.AddComponent<ConsoleView>();
            SetField(view, "logLinePrefab", prefab);
            SetField(view, "logContent", content);
            SetField(view, "canvas", canvas);
            SetField(view, "settings", settings);

            var child = new GameObject("Bootstrap");
            child.transform.SetParent(root.transform);
            var bootstrap = child.AddComponent<ConsoleBootstrap>();
            SetField(bootstrap, "settings", settings);
            SetField(bootstrap, "view", view);
            SetField(bootstrap, "consoleRoot", root);

            root.SetActive(true);
            return bootstrap;
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) UnityEngine.Object.Destroy(root);
            if (settings != null) UnityEngine.Object.Destroy(settings);
            if (logger != null) LoggerBootstrap.Shutdown();
            logger = null;
        }

        [UnityTest]
        public IEnumerator Bootstrap_RegistersTheCoreCommands_AndRunsThem()
        {
            var bootstrap = CreateConsole();
            yield return null;

            var names = bootstrap.Registry.All.Select(c => c.Descriptor.Name).ToArray();
            Assert.That(names, Is.SupersetOf(new[] { "help", "clear", "history", "repeat", "cvars" }));
            Assert.That(names, Is.SupersetOf(new[] { "log.level", "log.category", "log.categories", "log.sinks", "log.flush", "log.open", "log.reset", "log.test" }),
                        "the logger command group is discovered automatically");
            Assert.That(names, Has.No.Member("theme"), "no theme applier assigned, so the theme command is left out");
            Assert.That(bootstrap.Discovery.SkippedTypes, Is.Empty);
            Assert.That(bootstrap.Services.Get<ICheatPolicy>(), Is.InstanceOf<SettingsCheatPolicy>());

            bootstrap.Executor.Execute("help history");
            yield return null; // the view redraws once per frame

            var lines = Enumerable.Range(0, view.Buffer.Count).Select(i => view.Buffer[i]);
            Assert.That(lines, Has.Some.StartsWith("history [--limit=<int+>]"));
        }

        [UnityTest]
        public IEnumerator Bootstrap_ShowsLoggerEntries_Once()
        {
            loggerSink = new RecordingPlayModeSink();
            logger = new EldritchLoggerBuilder().AddSink(loggerSink).Build();
            LoggerBootstrap.Install(logger);

            CreateConsole(s => s.showUnityLogs = true);
            yield return null;

            ELoggerFactory.GetLogger("Test").AtInfo().Log("hello from the logger");
            yield return null; // Update flushes the console sink, LateUpdate redraws

            var lines = Enumerable.Range(0, view.Buffer.Count).Select(i => view.Buffer[i]).ToArray();
            Assert.That(lines.Count(l => l.Contains("hello from the logger")), Is.EqualTo(1));
            Assert.That(loggerSink.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Bootstrap_WhenUnavailable_RemovesTheWholeConsole()
        {
            var bootstrap = CreateConsole(s => s.availability = ConsoleAvailability.Never);
            yield return null;

            Assert.That(root == null, Is.True);
            Assert.That(bootstrap == null || bootstrap.Registry == null, Is.True);
        }

        [UnityTest]
        public IEnumerator ConfiguringServices_CanReplaceTheCheatPolicy()
        {
            Action<ConsoleServiceProvider> deny = services => services.Register<ICheatPolicy>(FixedCheatPolicy.Deny);
            ConsoleBootstrap.ConfiguringServices += deny;
            try
            {
                var bootstrap = CreateConsole();
                yield return null;

                Assert.That(bootstrap.Services.Get<ICheatPolicy>(), Is.SameAs(FixedCheatPolicy.Deny));
            }
            finally
            {
                ConsoleBootstrap.ConfiguringServices -= deny;
            }
        }

        [UnityTest]
        public IEnumerator ThrowingConfigurationHandlers_DoNotBreakStartup()
        {
            Action<ConsoleServiceProvider> broken = _ => throw new InvalidOperationException("broken handler");
            ConsoleBootstrap.ConfiguringServices += broken;
            try
            {
                LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("broken handler"));
                var bootstrap = CreateConsole();
                yield return null;

                Assert.That(bootstrap.Registry.All, Is.Not.Empty);
            }
            finally
            {
                ConsoleBootstrap.ConfiguringServices -= broken;
            }
        }
    }
}
