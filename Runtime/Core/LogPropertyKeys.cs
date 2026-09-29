namespace EldritchGames.EldritchLogger.Core
{
    /// <summary>
    /// Well-known property keys written by the logger, its builder and its enrichers.
    /// </summary>
    public static class LogPropertyKeys
    {
        public const string Logger = "Logger";
        public const string GameObject = "GameObject";
        public const string Component = "Component";
        public const string CSharpEvent = "CSharpEvent";
        public const string UnityEvent = "UnityEvent";
        public const string Scene = "Scene";
        public const string BuildVersion = "BuildVersion";

        /// <summary>The message template, when the entry was logged with one (<c>"Player {Name} joined"</c>).</summary>
        public const string MessageTemplate = "MessageTemplate";

        /// <summary>
        /// Where the entry came from when not logged through the logger API (e.g. <see cref="UnitySource"/>).
        /// Namespaced so a user property called "Source" is never mistaken for it.
        /// </summary>
        public const string Source = "EldritchLogger.Source";

        /// <summary>Stack trace text for entries without an Exception object (e.g. captured Unity errors).</summary>
        public const string StackTrace = "StackTrace";

        /// <summary><see cref="Source"/> value of entries forwarded from Unity's own log.</summary>
        public const string UnitySource = "Unity";
    }
}
