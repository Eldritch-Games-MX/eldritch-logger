using EldritchGames.EldritchLogger.Domain;

namespace EldritchGames.EldritchLogger.Core
{
    /// <summary>
    /// The logging contract. Deliberately minimal: convenience overloads and the fluent
    /// builder live in <see cref="EldritchLoggerExtensions"/>, so implementations and
    /// decorators only need these two members.
    /// </summary>
    public interface IEldritchLogger
    {
        /// <summary>
        /// Returns false when an entry with this level and category would be discarded.
        /// Check it before doing expensive work to build a message.
        /// </summary>
        bool IsEnabled(LogLevel level, LogCategory category);

        /// <summary>
        /// Processes <paramref name="entry"/>. Never throws; file sinks write in the background.
        /// </summary>
        void Log(LogEntry entry);
    }
}
