using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Domain;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace EldritchGames.EldritchLogger.Builder
{
    /// <summary>
    /// Default <see cref="ILogBuilder"/>. Mutates itself and returns <c>this</c>,
    /// so a chain allocates one builder and at most one dictionary.
    /// </summary>
    public sealed class LogBuilder : ILogBuilder
    {
        private readonly IEldritchLogger logger;
        private readonly LogLevel level;
        private LogCategory category;
        private Dictionary<string, object> properties;
        private Exception exception;
        private UnityEngine.Object context;

        public LogBuilder(IEldritchLogger logger, LogLevel level, LogCategory category = default)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.level = level;
            this.category = category;
        }

        public ILogBuilder Category(LogCategory category)
        {
            this.category = category;
            return this;
        }

        public ILogBuilder AddKeyValue(string key, object value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            (properties ??= new Dictionary<string, object>())[key] = value;
            return this;
        }

        public ILogBuilder WithException(Exception ex)
        {
            exception = ex;
            return this;
        }

        public ILogBuilder WithEvent(object eventObj, string eventName)
        {
            if (eventObj == null) throw new ArgumentNullException(nameof(eventObj));

            return eventObj is UnityEventBase
                ? AddKeyValue(LogPropertyKeys.UnityEvent, eventName ?? "UnityEvent")
                : AddKeyValue(LogPropertyKeys.CSharpEvent, eventName ?? "AnonymousHandler");
        }

        public ILogBuilder WithComponent(Component component)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));

            AddKeyValue(LogPropertyKeys.Component, component.GetType().Name);
            AddKeyValue(LogPropertyKeys.GameObject, component.gameObject.name);
            context = component;
            return this;
        }

        public ILogBuilder WithContext(UnityEngine.Object context)
        {
            this.context = context;
            return this;
        }

        public void Log(string message)
        {
            logger.Log(new LogEntry(level, category, message, properties, exception, context));
        }
    }

    internal sealed class NullLogBuilder : ILogBuilder
    {
        internal static readonly NullLogBuilder Instance = new();

        private NullLogBuilder() { }

        public ILogBuilder Category(LogCategory category) => this;
        public ILogBuilder AddKeyValue(string key, object value) => this;
        public ILogBuilder WithException(Exception ex) => this;
        public ILogBuilder WithEvent(object eventObj, string eventName) => this;
        public ILogBuilder WithComponent(Component component) => this;
        public ILogBuilder WithContext(UnityEngine.Object context) => this;
        public void Log(string message) { }
    }
}
