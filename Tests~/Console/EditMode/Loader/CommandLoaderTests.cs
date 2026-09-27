using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Core;
using EldritchGames.EldritchLogger.Console.Loader;
using EldritchGames.EldritchLogger.Console.Parsing;
using EldritchGames.EldritchLogger.Console.Registry;
using EldritchGames.EldritchLogger.Console.Settings;
using Moq;
using NUnit.Framework;
using System;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode.Loader
{
    [ConsoleCommand]
    public class FakeCommand : IConsoleCommand
    {
        public string Name => "fake";

        public string Description => "fake-description";

        public ArgSpec[] ExpectedArgs => Array.Empty<ArgSpec>();

        public void Execute(string[] args) { }
    }

    [TestFixture]
    public class CommandLoaderTests
    {
        [Test]
        public void RegisterAttributedCommands_RegistersFakeCommand()
        {
            // Arrange: mock registry
            var registryMock = new Mock<ICommandRegistry>();

            // Act: run loader
            CommandLoader.RegisterAttributedCommands(
                registryMock.Object,
                view: Mock.Of<IConsoleView>(),
                history: new CommandHistory(10),
                settings: ScriptableObject.CreateInstance<CommandConsoleSettings>(),
                parser: Mock.Of<ICommandParser>(),
                executor: Mock.Of<ICommandExecutor>(),
                lexer: new Lexer(),
                themeApplier: Mock.Of<IConsoleThemeApplier>(),
                themeLoader: Mock.Of<IThemeLoader>());

            // Assert: verify registration
            registryMock.Verify(r => r.Register(It.Is<IConsoleCommand>(c => c.Name == "fake")), Times.Once);
        }
    }
}
