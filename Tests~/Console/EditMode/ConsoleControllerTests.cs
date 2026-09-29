using EldritchGames.EldritchLogger.Console.Autocompletion;
using EldritchGames.EldritchLogger.Console.Execution;
using EldritchGames.EldritchLogger.Console.Registry;
using EldritchGames.EldritchLogger.Console.UI;
using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Pipeline;
using EldritchGames.EldritchLogger.Sinks;
using Moq;
using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode
{
    public class ConsoleControllerTests
    {
        private sealed class EchoSink : ILogSink
        {
            public string Name => "Echo";
            public LogLevel MinimumLevel => LogLevel.Debug;
            public void Emit(LogEntryDto entry) => Debug.Log(entry.Message);
        }

        private FakeView view;
        private Mock<ICommandExecutor> executor;
        private Mock<IAutocompleteProvider> autocomplete;
        private InputAction toggle;
        private InputAction accept;
        private ConsoleController controller;

        [SetUp]
        public void SetUp()
        {
            view = new FakeView();
            executor = new Mock<ICommandExecutor>();
            autocomplete = new Mock<IAutocompleteProvider>();
            toggle = new InputAction(type: InputActionType.Button);
            accept = new InputAction(type: InputActionType.Button);
            controller = new ConsoleController(view, executor.Object, autocomplete.Object, toggle, accept);
        }

        [TearDown]
        public void TearDown()
        {
            controller.Dispose();
            toggle.Dispose();
            accept.Dispose();
        }

        [Test]
        public void SubmittedInput_IsExecuted()
        {
            view.Submit("help");
            executor.Verify(e => e.Execute("help"), Times.Once);
        }

        [Test]
        public void InputChanges_ShowTheFirstSuggestion()
        {
            autocomplete.Setup(a => a.Suggest("he")).Returns(new[] { "help", "hello" });

            view.Type("he");

            Assert.That(view.Ghost, Is.EqualTo("help"));
        }

        [Test]
        public void ToggleConsole_TogglesVisibility()
        {
            controller.ToggleConsole();
            Assert.That(view.IsVisible, Is.False);
        }

        [Test]
        public void UnityLogs_AreForwarded()
        {
            Debug.Log("unity log");
            Assert.That(view.Lines, Does.Contain("unity log"));
        }

        [Test]
        public void LoggerEchoes_AreFilteredOnlyWhenRequested()
        {
            var sinks = new ILogSink[] { new EchoSink() };

            controller.FilterLoggerEchoes = true;
            new LogDispatcher().Dispatch(new LogEntryDto { Message = "echo one" }, sinks);
            controller.FilterLoggerEchoes = false;
            new LogDispatcher().Dispatch(new LogEntryDto { Message = "echo two" }, sinks);

            Assert.That(view.Lines, Does.Not.Contain("echo one"));
            Assert.That(view.Lines, Does.Contain("echo two"));
        }

        [Test]
        public void UnityLogCapture_CanBeDisabled()
        {
            controller.Dispose();
            controller = new ConsoleController(view, executor.Object, autocomplete.Object, null, null, captureUnityLogs: false);

            Debug.Log("not captured");

            Assert.That(view.Lines, Is.Empty);
        }

        [Test]
        public void Dispose_StopsForwarding()
        {
            controller.Dispose();

            Debug.Log("after dispose");
            view.Submit("help");

            Assert.That(view.Lines, Is.Empty);
            executor.Verify(e => e.Execute(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Controller_AcceptsSuggestions_OnlyWhileVisible()
        {
            var view = new FakeView { IsVisible = false };
            var accept = new UnityEngine.InputSystem.InputAction(type: UnityEngine.InputSystem.InputActionType.Button);
            var executor = ConsoleTestHelpers.CreateExecutor(new CommandRegistry(), new RecordingOutput());
            using var controller = new ConsoleController(view, executor, new EldritchGames.EldritchLogger.Console.Autocompletion.AutocompleteProvider(new CommandRegistry()),
                                                         null, accept, captureUnityLogs: false);
            try
            {
                var handler = typeof(ConsoleController).GetMethod("HandleAcceptSuggestion",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                handler.Invoke(controller, new object[] { default(UnityEngine.InputSystem.InputAction.CallbackContext) });
                Assert.That(view.Accepted, Is.False);

                view.IsVisible = true;
                handler.Invoke(controller, new object[] { default(UnityEngine.InputSystem.InputAction.CallbackContext) });
                Assert.That(view.Accepted, Is.True);
            }
            finally
            {
                accept.Dispose();
            }
        }
    }
}
