using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Execution;
using EldritchGames.EldritchLogger.Console.Output;
using EldritchGames.EldritchLogger.Console.Registry;
using NUnit.Framework;
using System;
using System.Linq;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode
{
    public class CommandRegistryTests
    {
        [Test]
        public void ResolvesNamesAndAliases_CaseInsensitively()
        {
            var registry = new CommandRegistry();
            var command = new SpyCommand(new CommandDescriptor("clear", "", aliases: new[] { "cls" }));

            registry.Register(command);

            Assert.That(registry.TryGet("CLEAR", out var byName) && byName == command, Is.True);
            Assert.That(registry.TryGet("Cls", out var byAlias) && byAlias == command, Is.True);
            Assert.That(registry.All.Count, Is.EqualTo(1));
        }

        [Test]
        public void Register_ReplacesACommandWithTheSameName()
        {
            var registry = new CommandRegistry();
            registry.Register(new SpyCommand("echo"));
            var replacement = new SpyCommand("echo");

            registry.Register(replacement);

            Assert.That(registry.All.Single(), Is.SameAs(replacement));
        }

        [Test]
        public void Unregister_RemovesAliasesToo()
        {
            var registry = new CommandRegistry();
            registry.Register(new SpyCommand(new CommandDescriptor("clear", "", aliases: new[] { "cls" })));

            Assert.That(registry.Unregister("cls"), Is.True);
            Assert.That(registry.TryGet("clear", out _), Is.False);
            Assert.That(registry.All, Is.Empty);
            Assert.That(registry.Unregister("clear"), Is.False);
        }

        [Test]
        public void AliasConflicts_AreRejected()
        {
            var registry = new CommandRegistry();
            registry.Register(new SpyCommand(new CommandDescriptor("a", "", aliases: new[] { "x" })));

            Assert.Throws<InvalidOperationException>(() =>
                registry.Register(new SpyCommand(new CommandDescriptor("b", "", aliases: new[] { "x" }))));
        }
    }

    public class CommandExecutorTests
    {
        private CommandRegistry registry;
        private RecordingOutput output;
        private CommandHistory history;
        private CommandExecutor executor;

        [SetUp]
        public void SetUp()
        {
            registry = new CommandRegistry();
            output = new RecordingOutput();
            history = new CommandHistory(10);
            executor = ConsoleTestHelpers.CreateExecutor(registry, output, history);
        }

        [Test]
        public void Execute_BindsArgumentsAndRuns()
        {
            var echo = new SpyCommand();
            registry.Register(echo);

            var status = executor.Execute("echo hello world --loud");

            Assert.That(status, Is.EqualTo(ExecutionStatus.Completed));
            Assert.That(echo.Calls.Single().GetAll<string>("words"), Is.EqualTo(new[] { "hello", "world" }));
            Assert.That(echo.Calls.Single().HasFlag("loud"), Is.True);
            Assert.That(history.GetAll(), Is.EqualTo(new[] { "echo hello world --loud" }));
        }

        [Test]
        public void UnknownCommand_WarnsAndIsStillRecordedInHistory()
        {
            Assert.That(executor.Execute("nope"), Is.EqualTo(ExecutionStatus.Failed));
            Assert.That(output.Messages.Single().type, Is.EqualTo(ConsoleMessageType.Warning));
            Assert.That(output.Messages.Single().message, Does.Contain("Unknown command: nope"));
            Assert.That(history.Count, Is.EqualTo(1));
        }

        [Test]
        public void BindingErrors_AreReportedWithUsage()
        {
            registry.Register(new SpyCommand());

            executor.Execute("echo --unknown");

            Assert.That(output.Messages.Single().message, Does.Contain("Unknown flag").And.Contain("Usage: echo"));
        }

        [Test]
        public void CommandExceptions_AreReportedNotThrown()
        {
            registry.Register(new SpyCommand { OnExecute = _ => throw new InvalidOperationException("kaboom") });

            ExecutionStatus status = ExecutionStatus.Completed;
            Assert.DoesNotThrow(() => status = executor.Execute("echo"));

            Assert.That(status, Is.EqualTo(ExecutionStatus.Failed));
            Assert.That(output.Messages.Single().type, Is.EqualTo(ConsoleMessageType.Error));
            Assert.That(output.Messages.Single().message, Does.Contain("kaboom"));
        }

        [Test]
        public void AsyncCommands_RunThroughTheRunner()
        {
            var wait = new SpyAsyncCommand();
            registry.Register(wait);

            Assert.That(executor.Execute("wait"), Is.EqualTo(ExecutionStatus.Started));
            Assert.That(wait.Steps, Is.EqualTo(2));
        }

        [Test]
        public void BlankInput_IsIgnored()
        {
            Assert.That(executor.Execute("   "), Is.EqualTo(ExecutionStatus.Failed));
            Assert.That(output.Messages, Is.Empty);
            Assert.That(history.Count, Is.EqualTo(0));
        }

        [Test]
        public void CommandsReceiveTheOutput()
        {
            registry.Register(new SpyCommand { OnExecute = ctx => ctx.Output.Info("from command") });

            executor.Execute("echo");

            Assert.That(output.Texts, Is.EqualTo(new[] { "from command" }));
        }
    }
}
