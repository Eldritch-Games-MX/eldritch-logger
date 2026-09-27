using EldritchGames.EldritchLogger.Console.Core;
using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Registry;
using EldritchGames.EldritchLogger.Console.Parsing;
using EldritchGames.EldritchLogger.Console.Loader;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode.Core
{
    [TestFixture]
    public class CoreCommandGroupTests
    {
        [Test]
        public void RegisterCoreCommands_RegistersAllExpectedCommands()
        {
            var registered = new List<object>();
            var registryMock = new Mock<ICommandRegistry>();

            registryMock.Setup(r => r.Register(It.IsAny<IConsoleCommand>()))
                        .Callback<IConsoleCommand>(c => registered.Add(c));
            registryMock.Setup(r => r.Register(It.IsAny<IAdvancedConsoleCommand>()))
                        .Callback<IAdvancedConsoleCommand>(c => registered.Add(c));

            CoreCommandGroup.RegisterCoreCommands(
                registryMock.Object,
                history: new CommandHistory(10),
                parser: Mock.Of<ICommandParser>(),
                lexer: new Lexer(),
                themeApplier: Mock.Of<IConsoleThemeApplier>(),
                themeLoader: Mock.Of<IThemeLoader>(),
                view: Mock.Of<IConsoleView>(),
                executor: Mock.Of<ICommandExecutor>());

            Assert.That(registered.Exists(c => c.GetType() == typeof(HelpCommand)), "HelpCommand not registered");
            Assert.That(registered.Exists(c => c.GetType() == typeof(RepeatCommand)), "RepeatCommand not registered");
            Assert.That(registered.Exists(c => c.GetType() == typeof(ThemeCommand)), "ThemeCommand not registered");
            Assert.That(registered.Exists(c => c.GetType() == typeof(HistoryCommand)), "HistoryCommand not registered");
            Assert.That(registered.Exists(c => c.GetType() == typeof(ClearCommand)), "ClearCommand not registered");
        }
    }
}
