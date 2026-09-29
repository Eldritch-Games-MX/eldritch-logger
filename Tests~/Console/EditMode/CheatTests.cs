using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Commands.BuiltIn;
using EldritchGames.EldritchLogger.Console.Execution;
using EldritchGames.EldritchLogger.Console.Registry;
using EldritchGames.EldritchLogger.Console.Settings;
using NUnit.Framework;
using System;
using System.Linq;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode
{
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
}
