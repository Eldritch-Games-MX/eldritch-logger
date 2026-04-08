using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Builder
{
    /// <summary>
    /// Provides a fluent builder for constructing and dispatching log entries.
    /// Implements <see cref="ILogBuilder"/> to support chaining methods.
    /// </summary>
    public sealed class LogBuilder : ILogBuilder
    {
        private readonly IEldritchLogger logger;
        private readonly LogLevel level;
        private readonly LogCategory category;
        private readonly Dictionary<string, object> metadata;
        private readonly Exception exception;
        private readonly object evt; // can be Delegate or UnityEventBase
        private readonly GameObject gameObject;
        private readonly string eventName;

        public LogBuilder(IEldritchLogger logger,
                          LogLevel level,
                          LogCategory category = default,
                          Dictionary<string, object> metadata = null,
                          Exception exception = null,
                          object evt = null,
                          GameObject gameObject = null,
                          string eventName = null)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.level = level;
            this.category = category;
            this.metadata = metadata ?? new Dictionary<string, object>();
            this.exception = exception;
            this.evt = evt;
            this.gameObject = gameObject;
            this.eventName = eventName;
        }
        public ILogBuilder Category(LogCategory category) =>
            new LogBuilder(logger, level, category, new Dictionary<string, object>(metadata), exception, evt, gameObject, eventName);

        public ILogBuilder AddKeyValue(string key, object value)
        {
            var newMetadata = new Dictionary<string, object>(metadata) { [key] = value };
            return new LogBuilder(logger, level, category, newMetadata, exception, evt, gameObject, eventName);
        }

        public ILogBuilder WithException(Exception ex) =>
            new LogBuilder(logger, level, category, new Dictionary<string, object>(metadata), ex, evt, gameObject, eventName);
        
        public ILogBuilder WithEvent(object eventObj, string eventName)
        {
            var loggedEvent = new LoggedEvent(eventName, eventObj);

            return new LogBuilder(logger, level, category,
                new Dictionary<string, object>(metadata),
                exception, loggedEvent.EventObject, gameObject, loggedEvent.Name);
        }

        public ILogBuilder WithComponent(Component component)
        {
            var newMetadata = new Dictionary<string, object>(metadata)
            {
                ["ComponentContext"] = $"{component.GetType().Name}@{component.gameObject.name}"
            };
            return new LogBuilder(logger, level, category, newMetadata, exception, evt, component.gameObject, eventName);
        }
        public void Log(string message)
        {
            var dict = new Dictionary<string, object>(metadata);

            if (evt is Delegate)
                dict["CSharpEvent"] = eventName ?? "AnonymousHandler";
            else if (evt is UnityEngine.Events.UnityEventBase)
                dict["UnityEvent"] = eventName ?? "UnityEvent";

            if (gameObject != null && !dict.ContainsKey("GameObject"))
                dict["GameObject"] = gameObject.name;

            logger.Log(level, category, message, dict, exception);
        }
    }
}
