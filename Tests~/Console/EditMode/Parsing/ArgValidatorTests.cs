using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Core;
using EldritchGames.EldritchLogger.Console.Parsing;
using Moq;
using NUnit.Framework;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode.Parsing
{
    [TestFixture]
    public class ArgValidatorTests
    {
        private Mock<IConsoleCommand> simpleCommandMock;
        private Mock<IAdvancedConsoleCommand> advancedCommandMock;

        [SetUp]
        public void SetUp()
        {
            simpleCommandMock = new Mock<IConsoleCommand>();
            advancedCommandMock = new Mock<IAdvancedConsoleCommand>();
        }

        [Test]
        public void Validate_SimpleCommand_MissingRequiredArg_Fails()
        {
            simpleCommandMock.Setup(c => c.Name).Returns("echo");
            simpleCommandMock.Setup(c => c.ExpectedArgs).Returns(new[]
            {
                new ArgSpec { Name = "text", Type = "string", Required = true }
            });

            var success = ArgValidator.Validate(simpleCommandMock.Object, new string[0], out var error);

            Assert.False(success);
            StringAssert.Contains("expects at least 1 arguments", error);
        }

        [Test]
        public void Validate_SimpleCommand_TooManyArgs_Fails()
        {
            simpleCommandMock.Setup(c => c.Name).Returns("add");
            simpleCommandMock.Setup(c => c.ExpectedArgs).Returns(new[]
            {
                new ArgSpec { Name = "x", Type = "int", Required = true },
                new ArgSpec { Name = "y", Type = "int", Required = true }
            });

            var success = ArgValidator.Validate(simpleCommandMock.Object, new[] { "1", "2", "3" }, out var error);

            Assert.False(success);
            StringAssert.Contains("expects at most 2 arguments", error);
        }

        [Test]
        public void Validate_SimpleCommand_IntMustBePositive_Fails()
        {
            simpleCommandMock.Setup(c => c.Name).Returns("repeat");
            simpleCommandMock.Setup(c => c.ExpectedArgs).Returns(new[]
            {
                new ArgSpec { Name = "count", Type = "int", Required = true, MustBePositive = true }
            });

            var success = ArgValidator.Validate(simpleCommandMock.Object, new[] { "-5" }, out var error);

            Assert.False(success);
            StringAssert.Contains("must be positive", error);
        }

        [Test]
        public void Validate_AdvancedCommand_MissingRequiredFlag_Fails()
        {
            advancedCommandMock.Setup(c => c.Name).Returns("deploy");
            advancedCommandMock.Setup(c => c.ExpectedArgs).Returns(new[]
            {
                new ArgSpec { Name = "--force", Type = "flag", Required = true }
            });

            var success = ArgValidator.Validate(advancedCommandMock.Object, new string[0], out var error);

            Assert.False(success);
            StringAssert.Contains("requires flag '--force'", error);
        }


        [Test]
        public void Validate_SimpleCommand_AllArgsValid_Passes()
        {
            simpleCommandMock.Setup(c => c.Name).Returns("echo");
            simpleCommandMock.Setup(c => c.ExpectedArgs).Returns(new[]
            {
                new ArgSpec { Name = "text", Type = "string", Required = true }
            });

            var success = ArgValidator.Validate(simpleCommandMock.Object, new[] { "hello" }, out var error);

            Assert.True(success);
            Assert.IsNull(error);
        }
    }
}
