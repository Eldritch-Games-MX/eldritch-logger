using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Commands.BuiltIn;
using EldritchGames.EldritchLogger.Console.Execution;
using EldritchGames.EldritchLogger.Console.Registry;
using EldritchGames.EldritchLogger.Console.Services;
using EldritchGames.EldritchLogger.Console.Settings;
using NUnit.Framework;
using System;
using System.Linq;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode
{
    public class ConsoleAvailabilityTests
    {
        [TestCase(ConsoleAvailability.Always, false, false, true)]
        [TestCase(ConsoleAvailability.DevelopmentBuilds, false, false, false)]
        [TestCase(ConsoleAvailability.DevelopmentBuilds, false, true, true)]
        [TestCase(ConsoleAvailability.DevelopmentBuilds, true, false, true)]
        [TestCase(ConsoleAvailability.EditorOnly, false, true, false)]
        [TestCase(ConsoleAvailability.EditorOnly, true, false, true)]
        [TestCase(ConsoleAvailability.Never, true, true, false)]
        public void IsAvailable(ConsoleAvailability availability, bool editor, bool development, bool expected)
        {
            Assert.That(ConsoleAvailabilityPolicy.IsAvailable(availability, editor, development), Is.EqualTo(expected));
        }

        [Test]
        public void DefaultSettings_AreDevelopmentBuildsWithCheats()
        {
            var settings = ScriptableObject.CreateInstance<CommandConsoleSettings>();
            Assert.That(settings.availability, Is.EqualTo(ConsoleAvailability.DevelopmentBuilds));
            Assert.That(settings.allowCheats, Is.True);
            Assert.That(ConsoleAvailabilityPolicy.IsAvailableHere(settings), Is.True, "the editor is always a development environment");
            UnityEngine.Object.DestroyImmediate(settings);
        }
    }

    public class CheatTests
    {
        [Cheat]
        private sealed class GodCommand : IConsoleCommand
        {
            public int Runs;
            public CommandDescriptor Descriptor { get; } = new("god", "Invulnerability.");
            public void Execute(CommandContext context) => Runs++;
        }

        private CommandRegistry registry;
        private RecordingOutput output;
        private CommandExecutor executor;
        private GodCommand god;
        private SpyCommand noclip;

        [SetUp]
        public void SetUp()
        {
            registry = new CommandRegistry();
            output = new RecordingOutput();
            executor = ConsoleTestHelpers.CreateExecutor(registry, output, new CommandHistory(10));
            god = new GodCommand();
            noclip = new SpyCommand(new CommandDescriptor("noclip", "", isCheat: true));
            registry.Register(god);
            registry.Register(noclip);
            registry.Register(new HelpCommand(registry));
        }

        [Test]
        public void Cheats_RunWhenAllowed()
        {
            executor.CheatPolicy = FixedCheatPolicy.Allow;

            executor.Execute("god");
            executor.Execute("noclip");

            Assert.That(god.Runs, Is.EqualTo(1));
            Assert.That(noclip.Calls.Count, Is.EqualTo(1));
        }

        [Test]
        public void Cheats_AreRefusedWhenDisallowed_ByAttributeOrDescriptor()
        {
            executor.CheatPolicy = FixedCheatPolicy.Deny;

            Assert.That(executor.Execute("god"), Is.EqualTo(ExecutionStatus.Failed));
            Assert.That(executor.Execute("noclip"), Is.EqualTo(ExecutionStatus.Failed));

            Assert.That(god.Runs, Is.EqualTo(0));
            Assert.That(noclip.Calls, Is.Empty);
            Assert.That(output.Texts, Has.All.Contains("cheats are disabled"));
        }

        [Test]
        public void SettingsPolicy_FollowsTheSettingsAsset()
        {
            var settings = ScriptableObject.CreateInstance<CommandConsoleSettings>();
            executor.CheatPolicy = new SettingsCheatPolicy(settings);

            settings.allowCheats = false;
            executor.Execute("god");
            settings.allowCheats = true;
            executor.Execute("god");

            Assert.That(god.Runs, Is.EqualTo(1));
            UnityEngine.Object.DestroyImmediate(settings);
        }

        [Test]
        public void Help_MarksCheats()
        {
            executor.Execute("help");

            Assert.That(output.Texts, Has.Some.StartsWith("god").And.EndsWith("[cheat]"));
            Assert.That(output.Texts, Has.Some.StartsWith("noclip").And.EndsWith("[cheat]"));
            Assert.That(output.Texts.Where(t => t.StartsWith("help")), Has.None.EndsWith("[cheat]"));
        }
    }

    public class DiscoveryReportTests
    {
        public sealed class FirstGroup : ICommandGroup
        {
            public string Name => "First";
            public void Register(ICommandRegistry registry) =>
                registry.Register(new SpyCommand(new CommandDescriptor("spawn", "", aliases: new[] { "sp" })));
        }

        public sealed class SecondGroup : ICommandGroup
        {
            public string Name => "Second";
            public void Register(ICommandRegistry registry)
            {
                registry.Register(new SpyCommand(new CommandDescriptor("spawn", "replacement")));
                registry.Register(new SpyCommand(new CommandDescriptor("stats", "", aliases: new[] { "spawn" })));
            }
        }

        [Test]
        public void ReplacedNamesAndRejectedAliases_AreReported()
        {
            var registry = new CommandRegistry();
            var report = new CommandDiscovery(new ConsoleServiceProvider())
                .RegisterAll(registry, new[] { typeof(FirstGroup), typeof(SecondGroup) }, Type.EmptyTypes);

            Assert.That(report.Conflicts, Has.Some.Contains("'spawn' from SecondGroup replaces the command registered by FirstGroup"));
            Assert.That(report.Conflicts, Has.Some.Contains("'stats' from SecondGroup was not registered"));
            Assert.That(registry.TryGet("spawn", out var spawn) && spawn.Descriptor.Description == "replacement", Is.True);
            Assert.That(report.SourceOf(spawn), Is.EqualTo(typeof(SecondGroup)));
        }

        [Test]
        public void ServiceProvider_ReportsMissingDependenciesStatically()
        {
            var missing = ConsoleServiceProvider.FindMissingDependencies(typeof(CoreCommandGroup),
                new[] { typeof(CommandHistory) });
            Assert.That(missing, Is.EqualTo(new[] { typeof(ICommandExecutor) }));

            var none = ConsoleServiceProvider.FindMissingDependencies(typeof(CoreCommandGroup),
                new[] { typeof(CommandHistory), typeof(ICommandExecutor) });
            Assert.That(none, Is.Empty, "optional parameters are not required");
        }
    }
}
