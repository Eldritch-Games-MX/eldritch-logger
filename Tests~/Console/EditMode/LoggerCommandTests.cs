using EldritchGames.EldritchLogger.Console.Commands.BuiltIn;
using EldritchGames.EldritchLogger.Console.Execution;
using EldritchGames.EldritchLogger.Console.Registry;
using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Settings;
using EldritchGames.EldritchLogger.Sinks;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode
{
    public class LoggerCommandTests
    {
        private sealed class ListSink : ILogSink
        {
            public readonly List<LogEntryDto> Entries = new();
            public string Name => "List sink";
            public LogLevel MinimumLevel => LogLevel.Debug;
            public void Emit(LogEntryDto entry) => Entries.Add(entry);
        }

        private LogSettings settings;
        private Core.EldritchLogger logger;
        private ListSink sink;
        private CommandRegistry registry;
        private RecordingOutput output;
        private CommandExecutor executor;
        private string openedFolder;

        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<LogSettings>();
            sink = new ListSink();
            logger = EldritchLoggerBuilder.FromSettings(settings).ClearSinks().ClearEnrichers().AddSink(sink).Build();

            registry = new CommandRegistry();
            output = new RecordingOutput();
            executor = ConsoleTestHelpers.CreateExecutor(registry, output);
            new LoggerCommandGroup(() => logger.Control, () => logger, _ => logger, folder => openedFolder = folder).Register(registry);
        }

        [TearDown]
        public void TearDown()
        {
            logger.Dispose();
            Object.DestroyImmediate(settings);
        }

        private string Run(string input)
        {
            output.Messages.Clear();
            executor.Execute(input);
            return string.Join("\n", output.Texts);
        }

        [Test]
        public void Level_ShowsSetsAndResets_WithoutTouchingTheAsset()
        {
            Assert.That(Run("log.level"), Is.EqualTo("Minimum level: Debug (from settings)"));

            Assert.That(Run("log.level warning"), Is.EqualTo("Minimum level: Warning (runtime override)"));
            Assert.That(logger.IsEnabled(LogLevel.Info, LogCategory.General), Is.False);
            Assert.That(settings.minimumLevel, Is.EqualTo(LogLevel.Debug), "the settings asset is not modified");

            Assert.That(Run("log.level reset"), Is.EqualTo("Minimum level: Debug (from settings)"));
            Assert.That(logger.IsEnabled(LogLevel.Info, LogCategory.General), Is.True);
        }

        [Test]
        public void Level_WarnsWhenNoSinkAcceptsTheLevel()
        {
            var infoSink = new InfoSink();
            using var infoLogger = EldritchLoggerBuilder.FromSettings(settings).ClearSinks().ClearEnrichers().AddSink(infoSink).Build();
            var infoRegistry = new CommandRegistry();
            var infoOutput = new RecordingOutput();
            var infoExecutor = ConsoleTestHelpers.CreateExecutor(infoRegistry, infoOutput);
            new LoggerCommandGroup(() => infoLogger.Control, () => infoLogger, _ => infoLogger, _ => { }).Register(infoRegistry);

            infoExecutor.Execute("log.level debug");

            Assert.That(infoOutput.Texts, Has.Some.Contains("No sink accepts entries below Info"));
            Assert.That(infoLogger.IsEnabled(LogLevel.Debug, LogCategory.General), Is.False, "the warning is accurate");

            infoOutput.Messages.Clear();
            infoExecutor.Execute("log.level info");
            Assert.That(infoOutput.Texts, Has.None.Contains("No sink accepts"));
        }

        private sealed class InfoSink : ILogSink
        {
            public string Name => "Info sink";
            public LogLevel MinimumLevel => LogLevel.Info;
            public void Emit(LogEntryDto entry) { }
        }

        [Test]
        public void Category_TogglesAtRuntime()
        {
            Assert.That(Run("log.category ai off"), Is.EqualTo("AI: off (runtime override)"));
            Assert.That(logger.IsEnabled(LogLevel.Error, LogCategory.AI), Is.False);
            Assert.That(settings.IsCategoryEnabled(LogCategory.AI), Is.True, "the settings asset is not modified");

            Assert.That(Run("log.category AI"), Is.EqualTo("AI: off (runtime override)"));
            Assert.That(Run("log.category AI reset"), Is.EqualTo("AI: on"));
            Assert.That(Run("log.category nope"), Does.Contain("'nope' is not a valid category"));
        }

        [Test]
        public void Categories_ListsEveryCategory()
        {
            Run("log.category Audio off");
            var lines = Run("log.categories").Split('\n');

            Assert.That(lines.Length, Is.EqualTo(LogCategory.BuiltIn.Count));
            Assert.That(lines, Has.Some.EqualTo("Audio: off (runtime override)"));
            Assert.That(lines, Has.Some.EqualTo("Gameplay: on"));
        }

        [Test]
        public void Reset_ClearsEveryOverride()
        {
            Run("log.level error");
            Run("log.category UI off");

            Assert.That(Run("log.reset"), Is.EqualTo("Overrides cleared. Minimum level: Debug"));
            Assert.That(logger.Control.MinimumLevelOverride, Is.Null);
            Assert.That(logger.Control.Categories.All(c => !c.Overridden), Is.True);
        }

        [Test]
        public void Test_LogsThroughTheLogger_AndWarnsWhenFiltered()
        {
            Run("log.test --level=warning hello world");
            Assert.That(sink.Entries.Single().Message, Is.EqualTo("hello world"));
            Assert.That(sink.Entries.Single().Level, Is.EqualTo(LogLevel.Warning));

            Run("log.test Something went wrong");
            Assert.That(sink.Entries[1].Message, Is.EqualTo("Something went wrong"), "a message needs no level first");
            Assert.That(sink.Entries[1].Level, Is.EqualTo(LogLevel.Info));
            Assert.That(Run("log.test 3 apples"), Is.Empty, "numbers are message text, not levels");
            Assert.That(sink.Entries[2].Message, Is.EqualTo("3 apples"));

            Run("log.level error");
            Assert.That(Run("log.test"), Does.Contain("currently filtered out"));
            Assert.That(sink.Entries.Count, Is.EqualTo(3));
        }

        [Test]
        public void Sinks_Flush_AndOpen()
        {
            Assert.That(Run("log.sinks"), Is.EqualTo("List sink  ≥Debug"));
            Assert.That(Run("log.flush"), Is.EqualTo("Sinks flushed."));

            Assert.That(Run("log.open"), Is.EqualTo(Application.persistentDataPath));
            Assert.That(openedFolder, Is.EqualTo(Application.persistentDataPath));
        }

        [Test]
        public void WithoutALogger_CommandsWarnInsteadOfFailing()
        {
            var bare = new CommandRegistry();
            var bareOutput = new RecordingOutput();
            var bareExecutor = ConsoleTestHelpers.CreateExecutor(bare, bareOutput);
            new LoggerCommandGroup(() => null, () => null, _ => NullLogger.Instance, _ => { }).Register(bare);

            foreach (var command in new[] { "log.level", "log.categories", "log.sinks", "log.flush", "log.reset" })
                Assert.That(bareExecutor.Execute(command), Is.EqualTo(ExecutionStatus.Completed), command);

            Assert.That(bareOutput.Messages.All(m => m.type == Output.ConsoleMessageType.Warning), Is.True);
        }

        [Test]
        public void DefaultConstructor_UsesTheInstalledLogger()
        {
            Assert.DoesNotThrow(() => new LoggerCommandGroup().Register(new CommandRegistry()));
        }
    }
}
