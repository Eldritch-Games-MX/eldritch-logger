using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Commands.BuiltIn;
using EldritchGames.EldritchLogger.Console.Execution;
using EldritchGames.EldritchLogger.Console.Registry;
using EldritchGames.EldritchLogger.Console.Services;
using NUnit.Framework;
using System;
using System.Linq;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode
{
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
