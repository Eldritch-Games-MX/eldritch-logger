using EldritchGames.EldritchLogger.Sinks;

namespace EldritchGames.EldritchLogger.Core
{
    /// <summary>
    /// Allows sinks to be attached to or detached from a running logger.
    /// Used by optional modules (e.g. the in-game console) to receive log entries
    /// without replacing the logger back-end.
    /// </summary>
    public interface ISinkRegistry
    {
        void AddSink(ILogSink sink);

        void RemoveSink(ILogSink sink);
    }
}
