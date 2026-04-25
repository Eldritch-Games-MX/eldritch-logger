using EldritchGames.EldritchLogger.Builder;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Core
{
    /// <summary>
    /// Decorator over <see cref="IEldritchLogger"/> that stamps every log entry with
    /// <c>Logger = &lt;name&gt;</c> in the metadata, identifying which class produced the entry.
    /// Created exclusively by <see cref="EldritchLoggerFactory.GetLogger"/>.
    /// </summary>
    internal sealed class NamedLogger : IEldritchLogger
    {
        private readonly IEldritchLogger _inner;
        private readonly string _name;

        internal NamedLogger(IEldritchLogger inner, string name)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _name = name;
        }

        public void Log(LogLevel level, LogCategory category, string message,
                        Dictionary<string, object> metadata = null, Exception exception = null)
        {
            var enriched = metadata != null
                ? new Dictionary<string, object>(metadata)
                : new Dictionary<string, object>();
            enriched["Logger"] = _name;
            _inner.Log(level, category, message, enriched, exception);
        }

        public ILogBuilder AtDebug(LogCategory category = LogCategory.General)
            => new NamedLogBuilder(_inner.AtDebug(category), _name);

        public ILogBuilder AtInfo(LogCategory category = LogCategory.General)
            => new NamedLogBuilder(_inner.AtInfo(category), _name);

        public ILogBuilder AtWarning(LogCategory category = LogCategory.General)
            => new NamedLogBuilder(_inner.AtWarning(category), _name);

        public ILogBuilder AtError(LogCategory category = LogCategory.General)
            => new NamedLogBuilder(_inner.AtError(category), _name);

        public ILogBuilder AtCritical(LogCategory category = LogCategory.General)
            => new NamedLogBuilder(_inner.AtCritical(category), _name);
    }

    /// <summary>
    /// Fluent builder wrapper that injects the logger name into the entry at dispatch time.
    /// Delegates all chain operations to the inner <see cref="ILogBuilder"/>, re-wrapping
    /// the result so the name propagates through the entire chain.
    /// </summary>
    internal sealed class NamedLogBuilder : ILogBuilder
    {
        private readonly ILogBuilder _inner;
        private readonly string _name;

        internal NamedLogBuilder(ILogBuilder inner, string name)
        {
            _inner = inner;
            _name = name;
        }

        public ILogBuilder Category(LogCategory category)
            => new NamedLogBuilder(_inner.Category(category), _name);

        public ILogBuilder AddKeyValue(string key, object value)
            => new NamedLogBuilder(_inner.AddKeyValue(key, value), _name);

        public ILogBuilder WithException(Exception ex)
            => new NamedLogBuilder(_inner.WithException(ex), _name);

        public ILogBuilder WithEvent(object eventObj, string eventName)
            => new NamedLogBuilder(_inner.WithEvent(eventObj, eventName), _name);

        public ILogBuilder WithComponent(Component component)
            => new NamedLogBuilder(_inner.WithComponent(component), _name);

        public void Log(string message)
            => _inner.AddKeyValue("Logger", _name).Log(message);
    }
}
