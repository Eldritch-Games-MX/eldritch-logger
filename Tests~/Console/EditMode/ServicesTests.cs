using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Registry;
using EldritchGames.EldritchLogger.Console.Services;
using EldritchGames.EldritchLogger.Console.UI;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode
{
    public class ServicesTests
    {
        // Discovery test types. They are nested/private to the test assembly and only registered explicitly.
        public sealed class NeedsView : ICommandGroup
        {
            private readonly IConsoleView view;
            public NeedsView(IConsoleView view) => this.view = view;
            public string Name => "NeedsView";
            public void Register(ICommandRegistry registry) => registry.Register(new SpyCommand("viewcmd"));
        }

        public sealed class NeedsMissingService : ICommandGroup
        {
            public NeedsMissingService(IDisposable missing) { }
            public string Name => "Missing";
            public void Register(ICommandRegistry registry) => registry.Register(new SpyCommand("never"));
        }

        public sealed class OptionalDependency : ICommandGroup
        {
            public readonly IDisposable Optional;
            public OptionalDependency(IDisposable optional = null) => Optional = optional;
            public string Name => "Optional";
            public void Register(ICommandRegistry registry) => registry.Register(new SpyCommand("optional"));
        }

        public sealed class ThrowingConstructor : ICommandGroup
        {
            public ThrowingConstructor() => throw new InvalidOperationException("ctor failed");
            public string Name => "Throws";
            public void Register(ICommandRegistry registry) { }
        }

        [ConsoleCommand]
        public sealed class AttributedCommand : IConsoleCommand
        {
            public CommandDescriptor Descriptor { get; } = new("attributed", "");
            public void Execute(CommandContext context) { }
        }

        [Test]
        public void Provider_ResolvesServicesRegisteredByInterface()
        {
            var view = new FakeView();
            var services = new ConsoleServiceProvider().Register<IConsoleView>(view);

            Assert.That(services.GetService(typeof(IConsoleView)), Is.SameAs(view));
            Assert.That(services.GetService(typeof(FakeView)), Is.Null);
        }

        [Test]
        public void Provider_IgnoresNullAndDestroyedUnityObjects()
        {
            var services = new ConsoleServiceProvider().Register<IConsoleView>(null);
            Assert.That(services.IsRegistered(typeof(IConsoleView)), Is.False);
        }

        [Test]
        public void Discovery_CreatesGroupsFromServices_AndSkipsUnresolvableOnes()
        {
            var warnings = new List<string>();
            var services = new ConsoleServiceProvider().Register<IConsoleView>(new FakeView());
            var registry = new CommandRegistry();

            int registered = new CommandDiscovery(services, warnings.Add).RegisterAll(registry,
                new[] { typeof(NeedsView), typeof(NeedsMissingService), typeof(OptionalDependency), typeof(ThrowingConstructor) },
                new[] { typeof(AttributedCommand) });

            Assert.That(registered, Is.EqualTo(3));
            Assert.That(registry.TryGet("viewcmd", out _), Is.True);
            Assert.That(registry.TryGet("optional", out _), Is.True);
            Assert.That(registry.TryGet("attributed", out _), Is.True);
            Assert.That(registry.TryGet("never", out _), Is.False);
            Assert.That(warnings, Has.Some.Contains("NeedsMissingService").And.Contains("IDisposable"));
            Assert.That(warnings, Has.Some.Contains("ThrowingConstructor").And.Contains("ctor failed"));
        }

        [Test]
        public void Discovery_FindsTheBuiltInGroupAndAttributedCommands()
        {
            Assert.That(CommandDiscovery.GroupTypes, Does.Contain(typeof(EldritchGames.EldritchLogger.Console.Commands.BuiltIn.CoreCommandGroup)));
            Assert.That(CommandDiscovery.CommandTypes, Does.Contain(typeof(AttributedCommand)));
        }
    }
}
