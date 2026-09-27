using EldritchGames.EldritchLogger.Console.Autocompletion;
using EldritchGames.EldritchLogger.Console.Core;
using EldritchGames.EldritchLogger.Console.Parsing;
using EldritchGames.EldritchLogger.Console.Registry;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode.Autocompletion
{
    [TestFixture]
    public class AutocompleteProviderTests
    {
        private Mock<ICommandRegistry> registryMock;
        private AutocompleteProvider provider;

        [SetUp]
        public void SetUp()
        {
            registryMock = new Mock<ICommandRegistry>();
            provider = new AutocompleteProvider(registryMock.Object);
        }

        [Test]
        public void Suggest_PartialCommandName_ReturnsMatchingNames()
        {
            var cmd1 = new Mock<ICommandMetadata>();
            cmd1.SetupGet(c => c.Name).Returns("echo");
            cmd1.SetupGet(c => c.ExpectedArgs).Returns(new ArgSpec[0]);
            cmd1.SetupGet(c => c.Description).Returns("Echoes text.");

            var cmd2 = new Mock<ICommandMetadata>();
            cmd2.SetupGet(c => c.Name).Returns("exit");
            cmd2.SetupGet(c => c.ExpectedArgs).Returns(new ArgSpec[0]);
            cmd2.SetupGet(c => c.Description).Returns("Exits console.");

            registryMock.Setup(r => r.GetAllMetadata())
                .Returns(new List<ICommandMetadata> { cmd1.Object, cmd2.Object });

            var suggestions = provider.Suggest("ex").ToList();

            Assert.That(suggestions, Does.Contain("exit"));
            Assert.That(suggestions, Does.Not.Contain("echo"));
        }

        [Test]
        public void Suggest_CommandWithArgs_ReturnsMatchingArgs()
        {
            var cmd = new Mock<ICommandMetadata>();
            cmd.SetupGet(c => c.Name).Returns("repeat");
            cmd.SetupGet(c => c.Description).Returns("Repeats a command.");
            cmd.SetupGet(c => c.ExpectedArgs).Returns(new[]
            {
                new ArgSpec { Name = "count", Type = "int", Required = true, MustBePositive = true },
                new ArgSpec { Name = "--silent", Type = "flag", Required = false }
            });

            registryMock.Setup(r => r.GetAllMetadata())
                .Returns(new List<ICommandMetadata> { cmd.Object });

            var suggestions = provider.Suggest("repeat --").ToList();

            Assert.That(suggestions, Does.Contain("--silent"));
        }

        [Test]
        public void Suggest_UnknownCommand_ReturnsEmpty()
        {
            registryMock.Setup(r => r.GetAllMetadata())
                .Returns(new List<ICommandMetadata>());

            var suggestions = provider.Suggest("foobar").ToList();

            Assert.That(suggestions, Is.Empty);
        }
    }
}
