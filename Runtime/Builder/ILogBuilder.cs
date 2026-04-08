using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Builder
{
    /// <summary>
    /// Defines the fluent builder contract for constructing and dispatching log entries.
    /// Each method returns a new immutable builder with additional context.
    /// </summary>
    public interface ILogBuilder
    {
        /// <summary>
        /// Sets the category for the log entry.
        /// </summary>
        ILogBuilder Category(LogCategory category);

        /// <summary>
        /// Adds a metadata key/value pair to the log entry.
        /// </summary>
        ILogBuilder AddKeyValue(string key, object value);

        /// <summary>
        /// Attaches an exception to the log entry.
        /// </summary>
        ILogBuilder WithException(Exception ex);

        /// <summary>
        /// Attaches an event object (delegate or UnityEventBase) to the log entry,
        /// with its declared name (use nameof).
        /// </summary>
        ILogBuilder WithEvent(object eventObj, string eventName);

        /// <summary>
        /// Adds component context metadata and associates the log entry with the component's GameObject.
        /// </summary>
        ILogBuilder WithComponent(Component component);

        /// <summary>
        /// Finalizes and dispatches the log entry with the given message.
        /// </summary>
        void Log(string message);
    }
}
