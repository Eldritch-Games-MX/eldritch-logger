using System;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Console.Commands
{
    public class CommandHistory
    {
        private readonly string[] buffer;
        private int head = 0;
        private int count = 0;
        private bool ignoreConsecutiveDuplicates;

        public int Count => count;

        public CommandHistory(int maxSize, bool ignoreConsecutiveDuplicates = false)
        {
            buffer = new string[maxSize];
            this.ignoreConsecutiveDuplicates = ignoreConsecutiveDuplicates;
        }
        public void EnableDuplicateFiltering() => ignoreConsecutiveDuplicates = true;
        public void Add(string command)
        {
            if (ignoreConsecutiveDuplicates && count > 0)
            {
                int lastIndex = (head - 1 + buffer.Length) % buffer.Length;
                if (buffer[lastIndex] == command)
                    return; // skip duplicate
            }

            buffer[head] = command;
            head = (head + 1) % buffer.Length;
            if (count < buffer.Length) count++;
        }

        public IEnumerable<string> GetAll()
        {
            for (int i = 0; i < count; i++)
            {
                int index = (head - count + i + buffer.Length) % buffer.Length;
                yield return buffer[index];
            }
        }

        public IEnumerable<string> GetLast(int limit)
        {
            limit = Math.Min(limit, count);
            for (int i = count - limit; i < count; i++)
            {
                int index = (head - count + i + buffer.Length) % buffer.Length;
                yield return buffer[index];
            }
        }
    }
}