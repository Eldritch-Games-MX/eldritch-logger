using System;

namespace EldritchGames.EldritchLogger.Core
{
    /// <summary>
    /// Source of timestamps for log entries. Replace it in tests to get deterministic output.
    /// </summary>
    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    public sealed class SystemClock : IClock
    {
        public static readonly SystemClock Instance = new();

        private SystemClock() { }

        public DateTime UtcNow => DateTime.UtcNow;
    }
}
