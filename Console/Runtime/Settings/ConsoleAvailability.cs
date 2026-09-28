namespace EldritchGames.EldritchLogger.Console.Settings
{
    /// <summary>Where the in-game console is allowed to run.</summary>
    public enum ConsoleAvailability
    {
        /// <summary>Editor and every build, including release builds.</summary>
        Always,
        /// <summary>Editor and Development Builds only.</summary>
        DevelopmentBuilds,
        /// <summary>Editor only.</summary>
        EditorOnly,
        /// <summary>Never; the console removes itself.</summary>
        Never
    }

    public static class ConsoleAvailabilityPolicy
    {
        /// <summary>
        /// Scripting define that disables the console in every environment, overriding
        /// <see cref="CommandConsoleSettings.availability"/>.
        /// </summary>
        public const string DisableDefine = "ELDRITCH_CONSOLE_DISABLED";

        /// <summary>True when <see cref="DisableDefine"/> is set for the current compilation.</summary>
        public static bool DisabledByDefine =>
#if ELDRITCH_CONSOLE_DISABLED
            true;
#else
            false;
#endif

        public static bool IsAvailable(ConsoleAvailability availability, bool isEditor, bool isDevelopmentBuild) =>
            availability switch
            {
                ConsoleAvailability.Always => true,
                ConsoleAvailability.DevelopmentBuilds => isEditor || isDevelopmentBuild,
                ConsoleAvailability.EditorOnly => isEditor,
                _ => false
            };

        /// <summary>Evaluates the policy for the running player or editor.</summary>
        public static bool IsAvailableHere(CommandConsoleSettings settings)
        {
            if (DisabledByDefine) return false;
            var availability = settings != null ? settings.availability : ConsoleAvailability.DevelopmentBuilds;
            return IsAvailable(availability, UnityEngine.Application.isEditor, UnityEngine.Debug.isDebugBuild);
        }
    }
}
