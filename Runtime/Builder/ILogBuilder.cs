using EldritchGames.EldritchLogger.Core;
using System;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Builder
{
    /// <summary>
    /// Fluent, single-use builder for one log entry. Obtain it from
    /// <see cref="EldritchLoggerExtensions.At"/> (or <c>AtInfo()</c> etc.) and finish with <see cref="Log"/>.
    /// Do not keep or reuse a builder after calling <see cref="Log"/>.
    /// </summary>
    public interface ILogBuilder
    {
        ILogBuilder Category(LogCategory category);
        ILogBuilder AddKeyValue(string key, object value);
        ILogBuilder WithException(Exception ex);

        /// <summary>Records the name of a C# delegate or UnityEvent that triggered the entry.</summary>
        ILogBuilder WithEvent(object eventObj, string eventName);

        /// <summary>Attaches a component: records its type and GameObject, and uses it as context.</summary>
        ILogBuilder WithComponent(Component component);

        /// <summary>Attaches a Unity object as context (click-to-select in the Unity Console).</summary>
        ILogBuilder WithContext(UnityEngine.Object context);

        void Log(string message);

        /// <summary>
        /// Logs a message template: <c>.Log("Player {Name} took {Damage} damage", name, damage)</c>.
        /// Each hole becomes a property. Nothing is rendered when the entry is disabled.
        /// </summary>
        void Log(string template, params object[] args);
    }
}
