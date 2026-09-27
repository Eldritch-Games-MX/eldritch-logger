using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Core;
using EldritchGames.EldritchLogger.Console.Domain;
using EldritchGames.EldritchLogger.Console.Parsing;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode.Commands
{
    [TestFixture]
    public class RepeatCommandTests
    {
        private Mock<ICommandExecutor> executorMock;
        private Mock<ICommandParser> parserMock;
        private Lexer lexer;
        private RepeatCommand repeatCommand;

        [SetUp]
        public void SetUp()
        {
            executorMock = new Mock<ICommandExecutor>();
            parserMock = new Mock<ICommandParser>();
            lexer = new Lexer();

            repeatCommand = new RepeatCommand(executorMock.Object, parserMock.Object, lexer);
            var parsed = new ParsedCommand("echo", new List<string> { "hello" }, new Dictionary<string, string>(), "echo hello");
            parserMock.Setup(p => p.Parse(It.IsAny<TokenList>(), It.IsAny<string>()))
                      .Returns(ParseResult.Ok(parsed, "echo hello"));
        }

        [Test]
        public void Execute_RepeatsCommandSpecifiedNumberOfTimes()
        {
            repeatCommand.Execute(new[] { "3", "echo", "hello" });

            executorMock.Verify(e => e.Execute(It.IsAny<ParseResult>()), Times.Exactly(3));
        }

        [Test]
        public void Execute_InvalidCount_ShowsWarningAndDoesNotExecute()
        {
            repeatCommand.Execute(new[] { "0", "echo", "hello" });

            executorMock.Verify(e => e.Execute(It.IsAny<ParseResult>()), Times.Never);
        }

        [Test]
        public void Execute_CountAboveMax_IsCapped()
        {
            repeatCommand.Execute(new[] { "2000", "echo", "hello" });

            executorMock.Verify(e => e.Execute(It.IsAny<ParseResult>()), Times.Exactly(1000));
        }

        [Test]
        public void Execute_WithSilentFlag_SuppressesLogsButStillExecutes()
        {
            repeatCommand.Execute(new[] { "2", "echo", "hello", "--silent" });

            executorMock.Verify(e => e.Execute(It.IsAny<ParseResult>()), Times.Exactly(2));
        }

        [Test]
        public void Execute_WithDelayFlag_StillExecutesCorrectNumberOfTimes()
        {
            repeatCommand.Execute(new[] { "2", "echo", "hello", "--delay=10" });

            executorMock.Verify(e => e.Execute(It.IsAny<ParseResult>()), Times.Exactly(2));
        }

        [Test]
        public void Execute_WhenExecutorThrows_BreaksLoopEarly()
        {
            executorMock.SetupSequence(e => e.Execute(It.IsAny<ParseResult>()))
                        .Throws(new System.Exception("boom"))
                        .Pass();

            LogAssert.Expect(LogType.Error, "Repeat iteration 1 failed: boom");
            repeatCommand.Execute(new[] { "3", "echo", "hello" });

            executorMock.Verify(e => e.Execute(It.IsAny<ParseResult>()), Times.Once);
        }

    }
}
