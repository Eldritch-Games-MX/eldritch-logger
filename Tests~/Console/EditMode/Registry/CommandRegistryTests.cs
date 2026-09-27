using EldritchGames.EldritchLogger.Console.Core;
using EldritchGames.EldritchLogger.Console.Registry;
using Moq;
using NUnit.Framework;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode.Registry
{
    [TestFixture]
    public class CommandRegistryTests
    {
        [Test]
        public void RegisterAndRetrieveCommand_Works()
        {
            var registry = new CommandRegistry();
            var cmdMock = new Mock<IConsoleCommand>();
            cmdMock.SetupGet(c => c.Name).Returns("test");

            registry.Register(cmdMock.Object);

            Assert.IsTrue(registry.TryGetCommand("test", out var found));
            Assert.AreEqual(cmdMock.Object, found);
        }

        [Test]
        public void RegisterAndRetrieveAdvancedCommand_Works()
        {
            var registry = new CommandRegistry();
            var advMock = new Mock<IAdvancedConsoleCommand>();
            advMock.SetupGet(c => c.Name).Returns("adv");

            registry.Register(advMock.Object);

            Assert.IsTrue(registry.TryGetAdvancedCommand("adv", out var found));
            Assert.AreEqual(advMock.Object, found);
        }
    }
}
