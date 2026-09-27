using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;

namespace EldritchGames.EldritchLogger.Sinks.Files
{
    /// <summary>
    /// Writes serialized entries to one file from a single dedicated thread.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Entries are written in the order they were enqueued.</item>
    /// <item>The queue is bounded: when full, the oldest pending entry is dropped and counted
    /// (<see cref="DroppedCount"/>), so logging never blocks the game.</item>
    /// <item>Serialization runs on the writer thread, not the logging thread.</item>
    /// <item>On platforms without threads (WebGL) writes happen synchronously.</item>
    /// </list>
    /// </remarks>
    public sealed class BackgroundLogWriter : IDisposable
    {
        public const int DefaultCapacity = 10_000;

        private static readonly bool ThreadsSupported =
#if UNITY_WEBGL && !UNITY_EDITOR
            false;
#else
            true;
#endif

        private readonly BlockingCollection<LogEntryDto> queue;
        private readonly Func<LogEntryDto, string> serialize;
        private readonly string footer;
        private readonly TimeSpan drainTimeout;
        private readonly StreamWriter writer;
        private readonly Thread thread;
        private readonly object writeLock = new();
        private long dropped;
        private long reportedDropped;
        private int disposed;

        public string Path { get; }

        /// <summary>Entries discarded because the queue was full.</summary>
        public long DroppedCount => Interlocked.Read(ref dropped);

        /// <param name="path">Target file. Created (with its directory) if missing, truncated otherwise.</param>
        /// <param name="serialize">Turns an entry into the exact text to append (include line breaks).</param>
        /// <param name="header">Written once when the file is opened.</param>
        /// <param name="footer">Written once on dispose.</param>
        public BackgroundLogWriter(string path,
                                   Func<LogEntryDto, string> serialize,
                                   string header = null,
                                   string footer = null,
                                   int capacity = DefaultCapacity,
                                   TimeSpan? drainTimeout = null)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("Path must be provided.", nameof(path));
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));

            Path = path;
            this.serialize = serialize ?? throw new ArgumentNullException(nameof(serialize));
            this.footer = footer;
            this.drainTimeout = drainTimeout ?? TimeSpan.FromSeconds(2);

            var directory = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            writer = new StreamWriter(path, append: false, new UTF8Encoding(false));
            if (header != null)
            {
                writer.Write(header);
                writer.Flush();
            }

            queue = new BlockingCollection<LogEntryDto>(new ConcurrentQueue<LogEntryDto>(), capacity);

            if (ThreadsSupported)
            {
                thread = new Thread(Run)
                {
                    IsBackground = true,
                    Name = $"EldritchLogger: {System.IO.Path.GetFileName(path)}"
                };
                thread.Start();
            }
        }

        /// <summary>Queues an entry. Never blocks; drops the oldest entry when the queue is full.</summary>
        public void Enqueue(LogEntryDto entry)
        {
            if (Volatile.Read(ref disposed) != 0 || entry == null) return;

            if (thread == null)
            {
                WriteNow(entry);
                return;
            }

            try
            {
                while (!queue.TryAdd(entry))
                {
                    if (queue.TryTake(out _))
                        Interlocked.Increment(ref dropped);
                }
            }
            catch (InvalidOperationException)
            {
                // Adding was completed concurrently by Dispose; the entry is discarded.
            }
        }

        /// <summary>Blocks until everything queued so far has been written, or the timeout elapses.</summary>
        public bool Flush(TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (queue.Count > 0 && DateTime.UtcNow < deadline)
                Thread.Sleep(1);

            lock (writeLock)
            {
                if (Volatile.Read(ref disposed) == 0) writer.Flush();
            }
            return queue.Count == 0;
        }

        private void Run()
        {
            try
            {
                foreach (var entry in queue.GetConsumingEnumerable())
                {
                    WriteNow(entry);
                    if (queue.Count == 0)
                    {
                        lock (writeLock) writer.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                SelfLog.Report($"File writer for '{Path}' stopped", ex);
            }
        }

        private void WriteNow(LogEntryDto entry)
        {
            string text;
            try
            {
                text = serialize(entry);
            }
            catch (Exception ex)
            {
                SelfLog.Report($"Failed to serialize entry for '{Path}'", ex);
                return;
            }

            lock (writeLock)
            {
                long droppedNow = Interlocked.Read(ref dropped);
                if (droppedNow != reportedDropped)
                {
                    SelfLog.Report($"'{Path}': {droppedNow - reportedDropped} entries dropped (queue full).");
                    reportedDropped = droppedNow;
                }

                writer.Write(text);
                if (thread == null) writer.Flush();
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0) return;

            queue.CompleteAdding();
            if (thread != null && !thread.Join(drainTimeout))
                SelfLog.Report($"'{Path}': writer did not drain within {drainTimeout.TotalSeconds:0.#}s; pending entries lost.");

            lock (writeLock)
            {
                if (footer != null) writer.Write(footer);
                writer.Dispose();
            }
            queue.Dispose();
        }
    }
}
