using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Loader;
using NUnit.Framework;
using Moq;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode.Command
{
    [TestFixture]
    public class ClearCommandTests
    {
        private Mock<IConsoleView> viewMock;
        private ClearCommand clearCommand;

        [SetUp]
        public void SetUp()
        {
            viewMock = new Mock<IConsoleView>();
            clearCommand = new ClearCommand(viewMock.Object);
        }

        [Test]
        public void Name_ReturnsClear()
        {
            Assert.AreEqual("clear", clearCommand.Name);
        }

        [Test]
        public void Description_ReturnsExpectedText()
        {
            Assert.AreEqual("Clears the console output.", clearCommand.Description);
        }

        [Test]
        public void Execute_CallsViewClear()
        {
            clearCommand.Execute(new string[0]);
            viewMock.Verify(v => v.Clear(), Times.Once);
        }

        private static readonly object[] ArgCases =
        {
            new object[] { new string[0] },
            new object[] { new[] { "extraArg" } },
            new object[] { new[] { "arg1", "arg2" } }
        };

        [TestCaseSource(nameof(ArgCases))]
        public void Execute_IgnoresArgsAndStillClears(string[] args)
        {
            clearCommand.Execute(args);
            viewMock.Verify(v => v.Clear(), Times.Once);
        }
    }
}
