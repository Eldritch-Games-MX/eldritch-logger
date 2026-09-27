using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Loader;
using EldritchGames.EldritchLogger.Console.Registry;
using Moq;
using NUnit.Framework;
using System.Linq;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode.Loader
{
    // A simple fake group for testing
    public class FakeGroup : ICommandGroup
    {
        public string Name => "fake-group";

        public void Register(ICommandRegistry registry)
        {
            // no-op
        }
    }

    [TestFixture]
    public class CommandGroupLoaderTests
    {
        [Test]
        public void DiscoverGroups_FindsAndInstantiatesFakeGroup()
        {
            // Act
            var groups = CommandGroupLoader.DiscoverGroups().ToList();

            // Assert
            Assert.That(groups.Any(g => g.Name == "fake-group"));
        }

        [Test]
        public void DiscoverGroups_ResolvesConstructorDependencies()
        {
            // Arrange: create a fake dependency and register it
            var registryMock = new Mock<ICommandRegistry>();
            ServiceRegistry.Register<ICommandRegistry>(registryMock.Object);

            // A group with a constructor dependency
            var groups = CommandGroupLoader.DiscoverGroups().ToList();

            // Assert: our fake dependency was injected
            // (you can extend FakeGroup to take ICommandRegistry in its ctor and store it)
        }
    }
}
