using EldritchGames.EldritchLogger.Console.Core;
using EldritchGames.EldritchLogger.Console.Domain;
using EldritchGames.EldritchLogger.Console.Parsing;
using EldritchGames.EldritchLogger.Console.Registry;
using EldritchGames.EldritchLogger.Console.Loader;
using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode.UI
{
    [TestFixture]
    public class ConsoleControllerTests
    {
        private Mock<IConsoleView> viewMock;
        private Mock<ICommandParser> parserMock;
        private Mock<ICommandExecutor> executorMock;
        private Mock<ICommandRegistry> registryMock;
        private ConsoleController controller;

        [SetUp]
        public void SetUp()
        {
            viewMock = new Mock<IConsoleView>();
            parserMock = new Mock<ICommandParser>();
            executorMock = new Mock<ICommandExecutor>();
            registryMock = new Mock<ICommandRegistry>();

            controller = new ConsoleController(
                viewMock.Object,
                parserMock.Object,
                executorMock.Object,
                new UnityEngine.InputSystem.InputAction(type: UnityEngine.InputSystem.InputActionType.Button),
                registryMock.Object
            );
        }

        [TearDown]
        public void TearDown()
        {
            controller.Dispose();
        }

        [Test]
        public void HandleCommand_ParsesAndExecutes()
        {
            var parsed = new ParsedCommand("echo", new List<string>(), new Dictionary<string, string>(), "echo");
            var result = ParseResult.Ok(parsed, "echo");

            parserMock.Setup(p => p.Parse(It.IsAny<TokenList>(), "echo")).Returns(result);

            // Simulate user submitting "echo"
            viewMock.Raise(v => v.OnCommandSubmitted += null, "echo");

            executorMock.Verify(e => e.Execute(It.Is<ParseResult>(r => r.Command.Name == "echo")), Times.Once);
        }

        [Test]
        public void ToggleConsole_CallsSetVisibility()
        {
            controller.ToggleConsole();
            viewMock.Verify(v => v.SetVisibility(), Times.Once);
        }

        [Test]
        public void UnityLog_IsForwardedToView()
        {
            // Act: simulate a Unity log
            Debug.Log("test log");

            // Assert: verify the controller forwarded it to the view
            viewMock.Verify(v => v.AppendLog("test log"), Times.AtLeastOnce);
        }

        [Test]
        public void UnityLog_DuringLoggerDispatch_IsIgnoredWhenFilteringEchoes()
        {
            controller.FilterLoggerEchoes = true;

            DispatchWithUnityEcho("echoed log");

            viewMock.Verify(v => v.AppendLog("echoed log"), Times.Never);
        }

        [Test]
        public void UnityLog_DuringLoggerDispatch_IsShownWhenNotFiltering()
        {
            controller.FilterLoggerEchoes = false;

            DispatchWithUnityEcho("echoed log");

            viewMock.Verify(v => v.AppendLog("echoed log"), Times.AtLeastOnce);
        }

        [Test]
        public void UnityLog_WhenCaptureDisabled_IsNotForwarded()
        {
            controller.Dispose();
            controller = new ConsoleController(
                viewMock.Object,
                parserMock.Object,
                executorMock.Object,
                new UnityEngine.InputSystem.InputAction(type: UnityEngine.InputSystem.InputActionType.Button),
                registryMock.Object,
                captureUnityLogs: false);

            Debug.Log("ignored log");

            viewMock.Verify(v => v.AppendLog("ignored log"), Times.Never);
        }

        [Test]
        public void Dispose_StopsForwardingUnityLogs()
        {
            controller.Dispose();

            Debug.Log("after dispose");

            viewMock.Verify(v => v.AppendLog("after dispose"), Times.Never);
        }

        // Mimics UnityConsoleExporter: a sink that writes to Debug.Log while the dispatcher is running.
        private static void DispatchWithUnityEcho(string message)
        {
            var echoSink = new Mock<ILogSink>();
            echoSink.Setup(s => s.OnLogReceived(It.IsAny<LogEntryDto>()))
                    .Callback<LogEntryDto>(dto => Debug.Log(dto.Message));

            new LogDispatcher().Dispatch(new LogEntryDto { Message = message }, new[] { echoSink.Object });
        }
    }
}
