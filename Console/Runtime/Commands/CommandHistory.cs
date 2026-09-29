using System;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Console.Commands
{
    /// <summary>Fixed-size ring buffer of entered command lines.</summary>
    public class CommandHistory
    {
        private readonly string[] buffer;
        private readonly bool ignoreConsecutiveDuplicates;
        private int head;
        private int count;

        public int Count => count;
        public int Capacity => buffer.Length;

        public CommandHistory(int maxSize, bool ignoreConsecutiveDuplicates = false)
        {
            if (maxSize <= 0) throw new ArgumentOutOfRangeException(nameof(maxSize), "History size must be positive.");
            buffer = new string[maxSize];
            this.ignoreConsecutiveDuplicates = ignoreConsecutiveDuplicates;
        }

        public void Add(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return;

            if (ignoreConsecutiveDuplicates && count > 0)
            {
                int lastIndex = (head - 1 + buffer.Length) % buffer.Length;
                if (buffer[lastIndex] == command)
                    return;
            }

            buffer[head] = command;
            head = (head + 1) % buffer.Length;
            if (count < buffer.Length) count++;
        }

        /// <summary>All entries, oldest first.</summary>
        public IEnumerable<string> GetAll() => GetLast(count);

        /// <summary>The newest <paramref name="limit"/> entries, oldest first.</summary>
        public IEnumerable<string> GetLast(int limit)
        {
            limit = Math.Max(0, Math.Min(limit, count));
            for (int i = count - limit; i < count; i++)
            {
                int index = (head - count + i + buffer.Length) % buffer.Length;
                yield return buffer[index];
            }
        }
    }
}
