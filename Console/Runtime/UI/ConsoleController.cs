using EldritchGames.EldritchLogger.Console.Autocompletion;
using EldritchGames.EldritchLogger.Console.Parsing;
using EldritchGames.EldritchLogger.Console.Registry;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EldritchGames.EldritchLogger.Console.Loader
{
    public class ConsoleController : IDisposable
    {
        private readonly IConsoleView view;
        private readonly ICommandParser parser;
        private readonly ICommandExecutor executor;
        private readonly IAutocompleteProvider autocompleteProvider;
        private readonly Lexer lexer;
        private readonly InputAction toggleAction;
        private readonly InputAction acceptSuggestion;
        private readonly bool captureUnityLogs;
        private bool disposed;

        /// <summary>
        /// When true, Unity log messages emitted while the EldritchLogger is dispatching
        /// (i.e. the echo produced by <c>UnityConsoleExporter</c>) are ignored, because the
        /// console already receives those entries through its own log sink.
        /// </summary>
        public bool FilterLoggerEchoes { get; set; }

        public ConsoleController(IConsoleView view,
                                 ICommandParser parser,
                                 ICommandExecutor executor,
                                 InputAction toggleAction,
                                 ICommandRegistry registry,
                                 bool captureUnityLogs = true)
        {
            this.view = view;
            this.parser = parser;
            this.executor = executor;
            this.lexer = new Lexer();
            this.toggleAction = toggleAction;
            this.captureUnityLogs = captureUnityLogs;

            view.OnCommandSubmitted += HandleCommand;
            view.OnInputChanged += HandleInputChanged;
            if (captureUnityLogs)
                Application.logMessageReceived += HandleUnityLog;

            toggleAction.performed += HandleToggle;
            toggleAction.Enable();

            acceptSuggestion = new InputAction(binding: "<Keyboard>/tab");
            acceptSuggestion.performed += HandleAcceptSuggestion;
            acceptSuggestion.Enable();
            autocompleteProvider = new AutocompleteProvider(registry);
        }

        private void HandleCommand(string input)
        {
            var tokens = lexer.Tokenize(input);
            var result = parser.Parse(tokens, input);
            executor.Execute(result);
        }
        private void HandleInputChanged(string text)
        {
            var suggestion = autocompleteProvider.Suggest(text).FirstOrDefault();
            view.ShowGhostSuggestion(suggestion);
        }

        private void HandleUnityLog(string condition, string stackTrace, LogType type)
        {
            if (FilterLoggerEchoes && LogDispatcher.IsDispatching)
                return;

            view.AppendLog(condition);
        }

        private void HandleToggle(InputAction.CallbackContext ctx) => ToggleConsole();

        private void HandleAcceptSuggestion(InputAction.CallbackContext ctx) => view.AcceptGhostSuggestion();

        public void ToggleConsole()
        {
            view.SetVisibility();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            view.OnCommandSubmitted -= HandleCommand;
            view.OnInputChanged -= HandleInputChanged;
            if (captureUnityLogs)
                Application.logMessageReceived -= HandleUnityLog;

            toggleAction.performed -= HandleToggle;
            acceptSuggestion.performed -= HandleAcceptSuggestion;
            acceptSuggestion.Disable();
            acceptSuggestion.Dispose();
        }
    }
}
