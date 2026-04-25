using EldritchGames.EldritchLogger.Builder;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Core
{
    /// <summary>
    /// No-op implementation of <see cref="IEldritchLogger"/>.
    /// All methods are silent and allocation-free.
    /// </summary>
    /// <remarks>
    /// Returned by <see cref="ELoggerFactory.GetLogger(string)"/> when no factory has been
    /// registered yet — for example, during edit-mode execution or in tests that do not
    /// call <see cref="ELoggerFactory.SetFactory"/>.
    /// Consumers never need to null-check the result of <see cref="ELoggerFactory.GetLogger{T}"/>;
    /// if the factory is absent they silently get this object instead.
    /// </remarks>
    public sealed class NullLogger : IEldritchLogger
    {
        /// <summary>Shared singleton instance. Use this instead of constructing a new one.</summary>
        public static readonly NullLogger Instance = new NullLogger();

        private NullLogger() { }

        /// <inheritdoc/>
        public void Log(LogLevel level, LogCategory category, string message,
                        Dictionary<string, object> metadata = null, Exception exception = null) { }

        /// <inheritdoc/>
        public ILogBuilder AtDebug(LogCategory category = LogCategory.General)
            => NullLogBuilder.Instance;

        /// <inheritdoc/>
        public ILogBuilder AtInfo(LogCategory category = LogCategory.General)
            => NullLogBuilder.Instance;

        /// <inheritdoc/>
        public ILogBuilder AtWarning(LogCategory category = LogCategory.General)
            => NullLogBuilder.Instance;

        /// <inheritdoc/>
        public ILogBuilder AtError(LogCategory category = LogCategory.General)
            => NullLogBuilder.Instance;

        /// <inheritdoc/>
        public ILogBuilder AtCritical(LogCategory category = LogCategory.General)
            => NullLogBuilder.Instance;
    }

    /// <summary>
    /// No-op <see cref="ILogBuilder"/> returned by <see cref="NullLogger"/>.
    /// All chain calls return the same singleton; <see cref="Log"/> is a no-op.
    /// </summary>
    internal sealed class NullLogBuilder : ILogBuilder
    {
        internal static readonly NullLogBuilder Instance = new NullLogBuilder();

        private NullLogBuilder() { }

        public ILogBuilder Category(LogCategory category) => this;
        public ILogBuilder AddKeyValue(string key, object value) => this;
        public ILogBuilder WithException(Exception ex) => this;
        public ILogBuilder WithEvent(object eventObj, string eventName) => this;
        public ILogBuilder WithComponent(Component component) => this;
        public void Log(string message) { }
    }
}
