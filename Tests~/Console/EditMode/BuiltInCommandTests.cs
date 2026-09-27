using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Commands.BuiltIn;
using EldritchGames.EldritchLogger.Console.Execution;
using EldritchGames.EldritchLogger.Console.Output;
using EldritchGames.EldritchLogger.Console.Registry;
using EldritchGames.EldritchLogger.Console.Themes;
using Moq;
using NUnit.Framework;
using System.Linq;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode
{
    public class BuiltInCommandTests
    {
        private CommandRegistry registry;
        private RecordingOutput output;
        private CommandHistory history;
        private CommandExecutor executor;
        private Mock<IConsoleThemeApplier> applier;
        private Mock<IThemeLoader> loader;
        private ConsoleTheme dark;

        [SetUp]
        public void SetUp()
        {
            registry = new CommandRegistry();
            output = new RecordingOutput();
            history = new CommandHistory(50);
            executor = ConsoleTestHelpers.CreateExecutor(registry, output, history);

            dark = ScriptableObject.CreateInstance<ConsoleTheme>();
            dark.name = "Dark";
            applier = new Mock<IConsoleThemeApplier>();
            loader = new Mock<IThemeLoader>();
            loader.Setup(l => l.LoadAllThemes()).Returns(new[] { dark });
            loader.Setup(l => l.LoadTheme(It.Is<string>(n => n.ToLower() == "dark"))).Returns(dark);

            new CoreCommandGroup(history, executor, applier.Object, loader.Object).Register(registry);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(dark);

        [Test]
        public void CoreGroup_RegistersAllBuiltIns()
        {
            Assert.That(registry.All.Select(c => c.Descriptor.Name),
                Is.EquivalentTo(new[] { "help", "clear", "history", "repeat", "theme" }));
        }

        [Test]
        public void CoreGroup_WithoutThemeServices_SkipsThemeCommand()
        {
            var other = new CommandRegistry();
            new CoreCommandGroup(history, executor).Register(other);
            Assert.That(other.TryGet("theme", out _), Is.False);
        }

        [Test]
        public void Help_ListsCommandsWithUsage()
        {
            executor.Execute("help");

            Assert.That(output.Texts.First(), Is.EqualTo("Available commands:"));
            Assert.That(output.Texts, Has.Some.StartsWith("repeat <count:int+> <command...>"));
        }

        [Test]
        public void Help_ForOneCommand_ShowsDetails()
        {
            executor.Execute("help history");

            Assert.That(output.Texts, Has.Some.StartsWith("history [--limit=<int+>]"));
            Assert.That(output.Texts, Has.Some.Contains("--limit: Show only the last N entries."));
        }

        [Test]
        public void Help_RejectsUnknownCommand_ViaChoiceType()
        {
            executor.Execute("help nope");
            Assert.That(output.Messages.Single().message, Does.Contain("'nope' is not a valid command"));
        }

        [Test]
        public void Clear_ClearsTheOutput_AlsoByAlias()
        {
            executor.Execute("clear");
            executor.Execute("cls");
            Assert.That(output.Clears, Is.EqualTo(2));
        }

        [Test]
        public void History_HonoursLimitFlag()
        {
            executor.Execute("help");
            executor.Execute("clear");
            output.Messages.Clear();

            executor.Execute("history --limit 2");

            Assert.That(output.Texts, Is.EqualTo(new[] { "2: clear", "3: history --limit 2" }));
        }

        [Test]
        public void Repeat_RunsTheInnerCommandNTimes_WithInnerFlagsIntact()
        {
            var echo = new SpyCommand();
            registry.Register(echo);

            executor.Execute("repeat 3 --silent echo \"a b\" --loud");

            Assert.That(echo.Calls, Has.Count.EqualTo(3));
            Assert.That(echo.Calls.All(c => c.HasFlag("loud")), Is.True);
            Assert.That(echo.Calls[0].GetAll<string>("words"), Is.EqualTo(new[] { "a b" }));
            Assert.That(output.Messages, Is.Empty, "--silent suppresses start/finish messages");
            Assert.That(history.Count, Is.EqualTo(1), "only the outer command is recorded");
        }

        [Test]
        public void Repeat_StopsWhenTheInnerCommandFails()
        {
            executor.Execute("repeat 5 nope");

            Assert.That(output.Texts, Has.Some.Contains("Unknown command: nope"));
            Assert.That(output.Texts, Has.Some.Contains("Stopped after 0 of 5 runs"));
        }

        [Test]
        public void Repeat_CannotRepeatItself()
        {
            executor.Execute("repeat 2 repeat 2 help");
            Assert.That(output.Texts, Has.Some.Contains("cannot repeat itself"));
        }

        [Test]
        public void Theme_ListsAndAppliesThemes()
        {
            executor.Execute("theme");
            Assert.That(output.Texts, Is.EqualTo(new[] { "Available themes:", "- Dark" }));

            executor.Execute("theme dark");
            applier.Verify(a => a.ApplyTheme(dark), Times.Once);
        }

        [Test]
        public void Theme_RejectsUnknownNames()
        {
            executor.Execute("theme neon");

            Assert.That(output.Messages.Single().message, Does.Contain("'neon' is not a valid theme"));
            applier.Verify(a => a.ApplyTheme(It.IsAny<ConsoleTheme>()), Times.Never);
        }
    }
}
