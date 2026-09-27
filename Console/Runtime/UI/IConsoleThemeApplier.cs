using EldritchGames.EldritchLogger.Console.Settings;

public interface IConsoleThemeApplier
    {
        /// <summary>
        /// Applies the current theme to the console UI.
        /// </summary>
        void ApplyTheme();
        /// <summary>
        /// Applies the specified theme to the console UI.
        /// </summary>
        /// <param name="newTheme">The new theme to apply.</param>
        void ApplyTheme(ConsoleTheme newTheme);
    }