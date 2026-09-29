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

        // ---- Message templates (General category; use At*(category).Log(template, args) for others) ----

        /// <summary>Logs a message template: <c>logger.Debug("Spawned {Enemy} at {Position}", enemy, pos)</c>.</summary>
        public static void Debug(this IEldritchLogger logger, string template, params object[] args) =>
            LogTemplate(logger, LogLevel.Debug, default, null, template, args);

        public static void Info(this IEldritchLogger logger, string template, params object[] args) =>
            LogTemplate(logger, LogLevel.Info, default, null, template, args);

        public static void Warning(this IEldritchLogger logger, string template, params object[] args) =>
            LogTemplate(logger, LogLevel.Warning, default, null, template, args);

        public static void Error(this IEldritchLogger logger, string template, params object[] args) =>
            LogTemplate(logger, LogLevel.Error, default, null, template, args);

        public static void Error(this IEldritchLogger logger, Exception exception, string template, params object[] args) =>
            LogTemplate(logger, LogLevel.Error, default, exception, template, args);

        public static void Critical(this IEldritchLogger logger, string template, params object[] args) =>
            LogTemplate(logger, LogLevel.Critical, default, null, template, args);

        public static void Critical(this IEldritchLogger logger, Exception exception, string template, params object[] args) =>
            LogTemplate(logger, LogLevel.Critical, default, exception, template, args);

        /// <summary>
        /// Renders <paramref name="template"/> with <paramref name="args"/>, adding one property per hole plus
        /// <see cref="LogPropertyKeys.MessageTemplate"/>. Nothing is rendered when the level/category is disabled.
        /// </summary>
        public static void LogTemplate(this IEldritchLogger logger, LogLevel level, LogCategory category,
                                       Exception exception, string template, params object[] args)
        {
            // At() returns a no-op builder when disabled; the builder owns template rendering.
            logger.At(level, category).WithException(exception).Log(template, args);
        }

        // ---- Scopes ----

        /// <summary>
        /// Adds <paramref name="key"/> = <paramref name="value"/> to every entry logged (by any logger)
        /// until the returned scope is disposed. See <see cref="Pipeline.LogScope"/>.
        /// Never keep a scope open across a coroutine <c>yield</c> (analyzer rule ELG005).
        /// </summary>
        public static IDisposable BeginScope(this IEldritchLogger logger, string key, object value) =>
            Pipeline.LogScope.Push(key, value);

        /// <summary>Opens a scope with several properties: <c>logger.BeginScope(("MatchId", id), ("Map", map))</c>.</summary>
        public static IDisposable BeginScope(this IEldritchLogger logger, params (string Key, object Value)[] properties)
        {
            if (properties == null) throw new ArgumentNullException(nameof(properties));
            var pairs = new KeyValuePair<string, object>[properties.Length];
            for (int i = 0; i < properties.Length; i++)
                pairs[i] = new KeyValuePair<string, object>(properties[i].Key, properties[i].Value);
            return Pipeline.LogScope.PushOwned(pairs);
        }
    }
}
