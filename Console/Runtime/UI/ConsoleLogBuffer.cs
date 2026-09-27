using System;

namespace EldritchGames.EldritchLogger.Console.UI
{
    /// <summary>
    /// Fixed-capacity ring buffer of console lines. Appending never allocates once full:
    /// the oldest line is overwritten.
    /// </summary>
    public sealed class ConsoleLogBuffer
    {
        private readonly string[] lines;
        private int head;

        public int Count { get; private set; }
        public int Capacity => lines.Length;

        /// <summary>Incremented on every change; lets views skip redundant redraws.</summary>
        public int Version { get; private set; }

        public ConsoleLogBuffer(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            lines = new string[capacity];
        }

        public void Add(string line)
        {
            lines[head] = line ?? string.Empty;
            head = (head + 1) % lines.Length;
            if (Count < lines.Length) Count++;
            Version++;
        }

        public void Clear()
        {
            Array.Clear(lines, 0, lines.Length);
            head = 0;
            Count = 0;
            Version++;
        }

        /// <summary>Line <paramref name="index"/> where 0 is the oldest retained line.</summary>
        public string this[int index]
        {
            get
            {
                if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
                return lines[(head - Count + index + lines.Length) % lines.Length];
            }
        }
    }
}
