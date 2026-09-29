using EldritchGames.EldritchLogger.Dto;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EldritchGames.EldritchLogger.EditorTools.LogViewer
{
    /// <summary>Reads files written by the JSON Lines sink.</summary>
    public static class JsonLinesLogReader
    {
        /// <param name="invalidLines">Lines that could not be parsed (e.g. a line cut off by a crash).</param>
        public static List<LogEntryDto> Read(string path, out int invalidLines)
        {
            using var tail = new JsonLinesTail(path);
            var entries = tail.ReadNew(out invalidLines, includePartialLastLine: true);
            return entries;
        }

        public static bool IsJsonLines(string path) =>
            string.Equals(Path.GetExtension(path), ".jsonl", StringComparison.OrdinalIgnoreCase);

        internal static bool TryParse(string line, out LogEntryDto entry)
        {
            entry = null;
            if (string.IsNullOrWhiteSpace(line)) return true;
            try
            {
                entry = LogJson.Deserialize(line);
                return entry != null;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Follows a JSON Lines file that is still being written: each <see cref="ReadNew"/> returns the entries
    /// appended since the last call. An incomplete last line is kept until its line break arrives.
    /// If the file shrinks (a new session overwrote it), reading starts over and <see cref="Restarted"/> is set.
    /// </summary>
    public sealed class JsonLinesTail : IDisposable
    {
        private readonly FileStream stream;
        private readonly StringBuilder partial = new();
        private readonly Decoder decoder = new UTF8Encoding(false).GetDecoder();
        private readonly byte[] buffer = new byte[64 * 1024];
        private readonly char[] chars;

        public string Path { get; }

        /// <summary>True when the last <see cref="ReadNew"/> found the file truncated and started from the beginning.</summary>
        public bool Restarted { get; private set; }

        public JsonLinesTail(string path)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            // The logger may still be writing the file; open it with shared access.
            stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            chars = new char[Encoding.UTF8.GetMaxCharCount(buffer.Length)];
        }

        /// <param name="invalidLines">Complete lines that could not be parsed.</param>
        /// <param name="includePartialLastLine">Also parse a last line without a line break (for a one-off read).</param>
        public List<LogEntryDto> ReadNew(out int invalidLines, bool includePartialLastLine = false)
        {
            invalidLines = 0;
            Restarted = false;
            var entries = new List<LogEntryDto>();

            if (stream.Length < stream.Position)
            {
                stream.Position = 0;
                partial.Clear();
                decoder.Reset();
                Restarted = true;
            }

            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                int count = decoder.GetChars(buffer, 0, read, chars, 0);
                int start = 0;
                for (int i = 0; i < count; i++)
                {
                    if (chars[i] != '\n') continue;
                    partial.Append(chars, start, i - start);
                    Parse(partial.ToString().TrimEnd('\r'), entries, ref invalidLines);
                    partial.Clear();
                    start = i + 1;
                }
                partial.Append(chars, start, count - start);
            }

            if (includePartialLastLine && partial.Length > 0)
            {
                Parse(partial.ToString(), entries, ref invalidLines);
                partial.Clear();
            }
            return entries;
        }

        private static void Parse(string line, List<LogEntryDto> into, ref int invalid)
        {
            if (!JsonLinesLogReader.TryParse(line, out var entry)) invalid++;
            else if (entry != null) into.Add(entry);
        }

        public void Dispose() => stream.Dispose();
    }
}
