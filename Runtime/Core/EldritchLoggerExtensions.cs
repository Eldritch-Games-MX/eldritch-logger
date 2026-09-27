using EldritchGames.EldritchLogger.Builder;
using EldritchGames.EldritchLogger.Domain;
using System;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Core
{
    /// <summary>
    /// Convenience API over <see cref="IEldritchLogger"/>: direct <c>Log</c> overloads and
    /// fluent builder entry points.
    /// </summary>
    public static class EldritchLoggerExtensions
    {
        public static void Log(this IEldritchLogger logger, LogLevel level, LogCategory category, string message,
                               IReadOnlyDictionary<string, object> metadata = null, Exception exception = null)
        {
            if (!logger.IsEnabled(level, category)) return;
            logger.Log(new LogEntry(level, category, message, metadata, exception));
        }

        /// <summary>
        /// Logs against a user-defined enum category. The enum value's name must match a
        /// category registered in <see cref="Settings.LogSettings"/>.
        /// </summary>
        public static void Log(this IEldritchLogger logger, LogLevel level, Enum category, string message,
                               IReadOnlyDictionary<string, object> metadata = null, Exception exception = null) =>
            logger.Log(level, LogCategory.From(category), message, metadata, exception);

        /// <summary>
        /// Starts a fluent entry. Returns a no-op builder when the level/category is disabled,
        /// so disabled logging allocates nothing.
        /// </summary>
        public static ILogBuilder At(this IEldritchLogger logger, LogLevel level, LogCategory category = default) =>
            logger.IsEnabled(level, category)
                ? new LogBuilder(logger, level, category)
                : NullLogBuilder.Instance;

        public static ILogBuilder AtDebug(this IEldritchLogger logger, LogCategory category = default) =>
            logger.At(LogLevel.Debug, category);

        public static ILogBuilder AtInfo(this IEldritchLogger logger, LogCategory category = default) =>
            logger.At(LogLevel.Info, category);

        public static ILogBuilder AtWarning(this IEldritchLogger logger, LogCategory category = default) =>
            logger.At(LogLevel.Warning, category);

        public static ILogBuilder AtError(this IEldritchLogger logger, LogCategory category = default) =>
            logger.At(LogLevel.Error, category);

        public static ILogBuilder AtCritical(this IEldritchLogger logger, LogCategory category = default) =>
            logger.At(LogLevel.Critical, category);
    }
}
