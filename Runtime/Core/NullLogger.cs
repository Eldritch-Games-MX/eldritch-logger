using EldritchGames.EldritchLogger.Domain;

namespace EldritchGames.EldritchLogger.Core
{
    /// <summary>
    /// A logger that discards everything. Returned by <see cref="ELoggerFactory"/> before a
    /// factory is registered.
    /// </summary>
    public sealed class NullLogger : IEldritchLogger
    {
        public static readonly NullLogger Instance = new();

        private NullLogger() { }

        public bool IsEnabled(LogLevel level, LogCategory category) => false;

        public void Log(LogEntry entry) { }
    }
}
