using System;

namespace EldritchGames.EldritchLogger.Domain
{
    /// <summary>
    /// Wraps an event object with its declared name for logging purposes.
    /// </summary>
    public sealed class LoggedEvent
    {
        public string Name { get; }
        public object EventObject { get; }

        public LoggedEvent(string name, object eventObject)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            EventObject = eventObject ?? throw new ArgumentNullException(nameof(eventObject));
        }
    }
}
