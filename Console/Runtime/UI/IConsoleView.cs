using System;

namespace EldritchGames.EldritchLogger.Console.UI
{
    /// <summary>Displays console output.</summary>
    public interface IConsoleOutputView
    {
        void AppendLog(string message);

        void Clear();
    }

    /// <summary>The command input line.</summary>
    public interface IConsoleInputView
    {
        /// <summary>Raised when the user submits a non-empty command line.</summary>
        event Action<string> OnCommandSubmitted;

        /// <summary>Raised when the input text changes.</summary>
        event Action<string> OnInputChanged;

        /// <summary>Shows <paramref name="suggestion"/> as a completion of the current token (null clears it).</summary>
        void ShowGhostSuggestion(string suggestion);

        /// <summary>Replaces the input with the ghost suggestion, if any.</summary>
        void AcceptGhostSuggestion();
    }

    public interface IConsoleVisibility
    {
        bool IsVisible { get; }

        void ToggleVisibility();
    }

    /// <summary>The complete console UI.</summary>
    public interface IConsoleView : IConsoleOutputView, IConsoleInputView, IConsoleVisibility
    {
    }
}
