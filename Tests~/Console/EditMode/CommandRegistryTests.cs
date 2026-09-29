using EldritchGames.EldritchLogger.Console.Commands;
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

        [Test]
        public void Registry_RejectsNulls()
        {
            var registry = new CommandRegistry();
            Assert.Throws<ArgumentNullException>(() => registry.Register(null));
            Assert.That(registry.Unregister(null), Is.False);
            Assert.That(registry.TryGet(null, out _), Is.False);
        }
    }
}
