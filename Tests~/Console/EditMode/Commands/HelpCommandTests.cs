using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Core;
using EldritchGames.EldritchLogger.Console.Parsing;
using EldritchGames.EldritchLogger.Console.Registry;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode.Command
{
    [TestFixture]
    public class HelpCommandTests
    {
        private Mock<ICommandRegistry> registryMock;
        private HelpCommand helpCommand;
        private List<string> logMessages;

        [SetUp]
        public void SetUp()
        {
            registryMock = new Mock<ICommandRegistry>();

            logMessages = new List<string>();
            Application.logMessageReceived += CaptureLog;

            helpCommand = new HelpCommand(registryMock.Object);
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
        public void Constructor_NullRegistry_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new HelpCommand(null));
        }

        [Test]
        public void Name_ReturnsHelp()
        {
            Assert.AreEqual("help", helpCommand.Name);
        }

        [Test]
        public void Description_ReturnsExpectedText()
        {
            Assert.AreEqual(
                "Lists all available commands, or usage for a specific command.",
                helpCommand.Description);
        }

        [Test]
        public void Execute_LogsHeader()
        {
            registryMock.Setup(r => r.GetAllMetadata()).Returns(new List<ICommandMetadata>());

            helpCommand.Execute(Array.Empty<string>());

            Assert.That(logMessages, Does.Contain("Available commands:"));
        }

        [Test]
        public void Execute_LogsCommandsWithUsage()
        {
            var cmd = new Mock<ICommandMetadata>();
            cmd.SetupGet(c => c.Name).Returns("repeat");
            cmd.SetupGet(c => c.Description).Returns("Repeats a command.");
            cmd.SetupGet(c => c.ExpectedArgs).Returns(new[]
            {
                new ArgSpec { Name = "count", Type = "int", Required = true, MustBePositive = true },
                new ArgSpec { Name = "command", Type = "string", Required = true },
                new ArgSpec { Name = "--silent", Type = "flag", Required = false }
            });

            registryMock.Setup(r => r.GetAllMetadata()).Returns(new List<ICommandMetadata> { cmd.Object });

            helpCommand.Execute(Array.Empty<string>());

            Assert.That(logMessages, Does.Contain("repeat <count:int+> <command:string> [--silent] - Repeats a command."));
        }

        [Test]
        public void Execute_WithSpecificCommandName_LogsThatCommandUsage()
        {
            var cmd = new Mock<ICommandMetadata>();
            cmd.SetupGet(c => c.Name).Returns("echo");
            cmd.SetupGet(c => c.Description).Returns("Echoes text.");
            cmd.SetupGet(c => c.ExpectedArgs).Returns(new[]
            {
                new ArgSpec { Name = "text", Type = "string", Required = true }
            });

            registryMock.Setup(r => r.GetAllMetadata()).Returns(new List<ICommandMetadata> { cmd.Object });

            helpCommand.Execute(new[] { "echo" });

            Assert.That(logMessages, Does.Contain("echo <text:string> - Echoes text."));
        }

        [Test]
        public void Execute_WithUnknownCommandName_LogsWarning()
        {
            registryMock.Setup(r => r.GetAllMetadata()).Returns(new List<ICommandMetadata>());

            helpCommand.Execute(new[] { "unknown" });

            Assert.That(logMessages.Any(m => m.Contains("Unknown command 'unknown'")), Is.True);
        }
    }
}
