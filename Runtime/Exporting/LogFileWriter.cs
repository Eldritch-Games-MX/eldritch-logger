using System;
using System.Collections.Concurrent;
using System.IO;

namespace EldritchGames.EldritchLogger.Exporting
{
    public class LogFileWriter : ILogFileWriter, IDisposable
    {
        // Thread-safe cache of writers per file path
        private readonly ConcurrentDictionary<string, StreamWriter> writers = new();

        public StreamWriter EnsureInitialized(string path, bool append = false)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            return writers.GetOrAdd(path, p =>
            {
                var directory = Path.GetDirectoryName(p);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                return new StreamWriter(p, append) { AutoFlush = true };
            });
        }

        public void WriteLine(string path, string line, bool append = true)
        {
            var writer = EnsureInitialized(path, append);
            writer.WriteLine(line);
        }

        public void Dispose()
        {
            foreach (var kvp in writers)
            {
                kvp.Value.Dispose();
            }
            writers.Clear();
        }
    }
}
