using EldritchGames.EldritchLogger.Dto;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace EldritchGames.EldritchLogger.EditorTools.LogViewer
{
    /// <summary>Reads files written by the JSON Lines sink.</summary>
    public static class JsonLinesLogReader
    {
        /// <param name="invalidLines">Lines that could not be parsed (e.g. a line cut off by a crash).</param>
        public static List<LogEntryDto> Read(string path, out int invalidLines)
        {
            invalidLines = 0;
            var entries = new List<LogEntryDto>();

            // The logger may still be writing the file; open it with shared access.
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    var entry = LogJson.Deserialize(line);
                    if (entry != null) entries.Add(entry);
                    else invalidLines++;
                }
                catch (JsonException)
                {
                    invalidLines++;
                }
            }

            return entries;
        }

        public static bool IsJsonLines(string path) =>
            string.Equals(Path.GetExtension(path), ".jsonl", StringComparison.OrdinalIgnoreCase);
    }
}
