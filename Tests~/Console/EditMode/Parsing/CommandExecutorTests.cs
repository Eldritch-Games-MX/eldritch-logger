using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Core;
using EldritchGames.EldritchLogger.Console.Domain;
using EldritchGames.EldritchLogger.Console.Parsing;
using EldritchGames.EldritchLogger.Console.Registry;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode.Parsing
{
    [TestFixture]
    public class CommandExecutorTests
    {
        private CommandRegistry registry;
        private CommandHistory history;
        private CommandExecutor executor;
        private List<string> logMessages;

        [SetUp]
        public void SetUp()
        {
            registry = new CommandRegistry();
            history = new CommandHistory(10);
            executor = new CommandExecutor(registry, history);

            logMessages = new List<string>();
            Application.logMessageReceived += CaptureLog;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= CaptureLog;
        }

        private void CaptureLog(string condition, string stackTrace, LogType type)
        {
            logMessages.Add(condition);
        }

        [Test]
        public void Execute_ParseError_LogsWarning()
        {
            var result = ParseResult.Fail("bad input");
            executor.Execute(result);

            Assert.That(logMessages.Any(m => m.Contains("Parse error")));
        }

        [Test]
        public void Execute_SimpleCommand_ExecutesAndRecordsHistory()
        {
            var cmdMock = new Mock<IConsoleCommand>();
            cmdMock.SetupGet(c => c.Name).Returns("echo");

            registry.Register(cmdMock.Object);

            var parsed = new ParsedCommand("echo", new List<string>(), new Dictionary<string, string>(), "echo");
            var result = ParseResult.Ok(parsed, "echo");

            executor.Execute(result);

            cmdMock.Verify(c => c.Execute(It.IsAny<string[]>()), Times.Once);
            Assert.AreEqual(1, history.Count);
        }

        [Test]
        public void Execute_AdvancedCommand_ExecutesAndRecordsHistory()
        {
            var advMock = new Mock<IAdvancedConsoleCommand>();
            advMock.SetupGet(c => c.Name).Returns("adv");

            registry.Register(advMock.Object);

            var parsed = new ParsedCommand("adv", new List<string>(), new Dictionary<string, string>(), "adv");
            var result = ParseResult.Ok(parsed, "adv");

            executor.Execute(result);

            advMock.Verify(c => c.Execute(It.IsAny<List<string>>(), It.IsAny<Dictionary<string, string>>()), Times.Once);
            Assert.AreEqual(1, history.Count);
        }

        [Test]
        public void Execute_UnknownCommand_LogsWarning()
        {
            var parsed = new ParsedCommand("unknown", new List<string>(), new Dictionary<string, string>(), "unknown");
            var result = ParseResult.Ok(parsed, "unknown");

            executor.Execute(result);

            Assert.That(logMessages.Any(m => m.Contains("Unknown command")));
        }
    }
}