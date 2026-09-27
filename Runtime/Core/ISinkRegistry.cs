namespace EldritchGames.EldritchLogger.Core
{
    /// <summary>
    /// Allows sinks to be attached to or detached from a running logger.
    /// Used by optional modules (e.g. the in-game console) to receive log entries
    /// without replacing the logger back-end.
    /// </summary>
    public interface ISinkRegistry
    {
        /// <summary>
        /// Registers <paramref name="sink"/> under its <see cref="ILogSink.Category"/>.
        /// </summary>
        void AddSink(ILogSink sink);

        /// <summary>
        /// Unregisters a previously added <paramref name="sink"/>.
        /// </summary>
        void RemoveSink(ILogSink sink);
    }
}
