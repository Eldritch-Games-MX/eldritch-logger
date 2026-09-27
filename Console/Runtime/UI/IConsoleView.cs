using System;

namespace EldritchGames.EldritchLogger.Console.Loader
{
    /// <summary>
    /// Defines the contract for the console view, responsible for user interaction and display.
    /// </summary>
    public interface IConsoleView
    {
        /// <summary>
        /// Raised when the user submits a command.
        /// </summary>
        event Action<string> OnCommandSubmitted;

        /// <summary>
        /// Appends a message to the console log.
        /// </summary>
        void AppendLog(string message);

        /// <summary>
        /// Clears all log entries from the console view.
        /// </summary>
        void Clear();

        /// <summary>
        /// Toggles the visibility of the console UI.
        /// </summary>
        void SetVisibility();

        /// <summary>
        /// Raised when the user modifies the input text.
        /// </summary>
        event Action<string> OnInputChanged;

        /// <summary>
        /// Displays a ghost suggestion for autocomplete inline with the input field.
        /// </summary>
        void ShowGhostSuggestion(string suggestion);

        /// <summary>
        /// Accepts the current ghost suggestion and replaces the input text.
        /// </summary>
        void AcceptGhostSuggestion();
    }
}