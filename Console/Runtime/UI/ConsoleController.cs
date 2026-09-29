using EldritchGames.EldritchLogger.Console.Autocompletion;
using EldritchGames.EldritchLogger.Console.Execution;
using EldritchGames.EldritchLogger.Pipeline;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EldritchGames.EldritchLogger.Console.UI
{
    /// <summary>
    /// Connects the view to the executor and autocomplete, handles the toggle / accept-suggestion
    /// input actions and optionally forwards Unity log messages to the view.
    /// All collaborators are injected.
    /// </summary>
    public sealed class ConsoleController : IDisposable
    {
        private readonly IConsoleView view;
        private readonly ICommandExecutor executor;
        private readonly IAutocompleteProvider autocomplete;
        private readonly InputAction toggleAction;
        private readonly InputAction acceptSuggestionAction;
        private readonly bool captureUnityLogs;
        private bool disposed;

        /// <summary>
        /// When true, Unity log messages emitted while the EldritchLogger is dispatching (the echo of
        /// <c>UnityConsoleSink</c>) are ignored, because the console receives those entries through its own sink.
        /// </summary>
        public bool FilterLoggerEchoes { get; set; }

        /// <param name="toggleAction">Shows/hides the console. May be null.</param>
        /// <param name="acceptSuggestionAction">Accepts the ghost suggestion. May be null.</param>
        /// <param name="captureUnityLogs">Forward <c>Application.logMessageReceived</c> messages to the view.</param>
        public ConsoleController(IConsoleView view,
                                 ICommandExecutor executor,
                                 IAutocompleteProvider autocomplete,
                                 InputAction toggleAction,
                                 InputAction acceptSuggestionAction,
                                 bool captureUnityLogs = true)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.executor = executor ?? throw new ArgumentNullException(nameof(executor));
            this.autocomplete = autocomplete ?? throw new ArgumentNullException(nameof(autocomplete));
            this.toggleAction = toggleAction;
            this.acceptSuggestionAction = acceptSuggestionAction;
            this.captureUnityLogs = captureUnityLogs;

            view.OnCommandSubmitted += HandleCommand;
            view.OnInputChanged += HandleInputChanged;
            if (captureUnityLogs)
                Application.logMessageReceived += HandleUnityLog;

            if (toggleAction != null)
            {
                toggleAction.performed += HandleToggle;
                toggleAction.Enable();
            }

            if (acceptSuggestionAction != null)
            {
                acceptSuggestionAction.performed += HandleAcceptSuggestion;
                acceptSuggestionAction.Enable();
            }
        }

        private void HandleCommand(string input) => executor.Execute(input);

        private void HandleInputChanged(string text) =>
            view.ShowGhostSuggestion(autocomplete.Suggest(text).FirstOrDefault());

        private void HandleUnityLog(string condition, string stackTrace, LogType type)
        {
            if (FilterLoggerEchoes && LogDispatcher.IsDispatching)
                return;

            view.AppendLog(type switch
            {
                LogType.Warning => $"<color=#FFC107>{condition}</color>",
                LogType.Error or LogType.Exception or LogType.Assert => $"<color=#FF5252>{condition}</color>",
                _ => condition
            });
        }

        private void HandleToggle(InputAction.CallbackContext ctx) => ToggleConsole();

        private void HandleAcceptSuggestion(InputAction.CallbackContext ctx)
        {
            if (view.IsVisible) view.AcceptGhostSuggestion();
        }

        public void ToggleConsole() => view.ToggleVisibility();

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            view.OnCommandSubmitted -= HandleCommand;
            view.OnInputChanged -= HandleInputChanged;
            if (captureUnityLogs)
                Application.logMessageReceived -= HandleUnityLog;

            if (toggleAction != null) toggleAction.performed -= HandleToggle;
            if (acceptSuggestionAction != null) acceptSuggestionAction.performed -= HandleAcceptSuggestion;
        }
    }
}
