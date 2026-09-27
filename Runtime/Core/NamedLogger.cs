using EldritchGames.EldritchLogger.Domain;
using System;

namespace EldritchGames.EldritchLogger.Core
{
    /// <summary>
    /// Decorator that stamps <see cref="LogPropertyKeys.Logger"/> onto every entry.
    /// </summary>
    internal sealed class NamedLogger : IEldritchLogger
    {
        private readonly IEldritchLogger inner;

        public string Name { get; }

        internal NamedLogger(IEldritchLogger inner, string name)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
            Name = name;
        }

        public bool IsEnabled(LogLevel level, LogCategory category) => inner.IsEnabled(level, category);

        public void Log(LogEntry entry)
        {
            if (entry == null) return;
            inner.Log(entry.WithProperty(LogPropertyKeys.Logger, Name));
        }
    }
}
