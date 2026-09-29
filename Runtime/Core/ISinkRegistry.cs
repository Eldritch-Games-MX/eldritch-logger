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

        /// <summary>The sinks currently attached (a snapshot).</summary>
        System.Collections.Generic.IReadOnlyList<ILogSink> All { get; }
    }

    public static class SinkRegistryExtensions
    {
        /// <summary>
        /// When no sink accepts <paramref name="level"/> (every sink's minimum level is higher), the lowest level a
        /// sink does accept; otherwise null. Entries below it are discarded whatever the filter allows.
        /// </summary>
        public static LogLevel? LevelHiddenBySinks(this ISinkRegistry registry, LogLevel level)
        {
            var sinks = registry?.All;
            if (sinks == null || sinks.Count == 0) return null;

            var lowest = LogLevel.Critical;
            foreach (var sink in sinks)
                if (sink.MinimumLevel < lowest) lowest = sink.MinimumLevel;
            return lowest > level ? lowest : null;
        }
    }
}
