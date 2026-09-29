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
    /// Writes serialized entries to a file from a single dedicated thread.
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

        private static readonly UTF8Encoding Utf8 = new(false);

        private readonly BlockingCollection<LogEntryDto> queue;
        private readonly Func<LogEntryDto, string> serialize;
        private readonly string header;
        private readonly string footer;
        private readonly TimeSpan drainTimeout;
        private readonly Thread thread;
        private readonly object writeLock = new();
        private StreamWriter writer;
        private long dropped;
        // Entries are written in the order they were accepted, so "the first N accepted entries are done"
        // is simply completed >= N. Flush waits for what was accepted before it, not for later entries.
        private long accepted;  // queued by Enqueue
        private long completed; // written, failed, or dropped
        private long reportedDropped;
        private long failedWrites;
        private bool closed; // set under writeLock once Dispose has closed the file
        private int disposed;

        /// <summary>The file being written.</summary>
        public string Path { get; }

        /// <summary>Entries lost: discarded because the queue was full, or not written because of an I/O error.</summary>
        public long DroppedCount => Interlocked.Read(ref dropped) + Interlocked.Read(ref failedWrites);

        /// <param name="path">Target file. Created (with its directory) if missing, truncated otherwise.</param>
        /// <param name="serialize">Turns an entry into the exact text to append (include line breaks).</param>
        /// <param name="header">Written at the start of the file.</param>
        /// <param name="footer">Written at the end of the file when the writer is disposed.</param>
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
            this.header = header;
            this.footer = footer;
            this.drainTimeout = drainTimeout ?? TimeSpan.FromSeconds(2);

            var directory = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            writer = new StreamWriter(path, append: false, Utf8);
            WriteHeader();

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

            Interlocked.Increment(ref accepted);
            try
            {
                while (!queue.TryAdd(entry))
                {
                    if (queue.TryTake(out _))
                    {
                        Interlocked.Increment(ref dropped);
                        Interlocked.Increment(ref completed);
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // Adding was completed concurrently by Dispose; the entry is discarded.
                Interlocked.Increment(ref completed);
            }
        }

        /// <summary>
        /// Blocks until everything queued before this call has been written, or the timeout elapses. Entries
        /// queued meanwhile by other threads are not waited for, so continuous logging cannot stall a flush.
        /// </summary>
        public bool Flush(TimeSpan timeout)
        {
            // Count completed writes rather than watching the queue: the last entry leaves the queue before it is written.
            var deadline = DateTime.UtcNow + timeout;
            long target = Interlocked.Read(ref accepted);
            while (Interlocked.Read(ref completed) < target && DateTime.UtcNow < deadline)
                Thread.Sleep(1);

            lock (writeLock)
            {
                if (Volatile.Read(ref disposed) == 0) TryFlushWriter();
            }
            return Interlocked.Read(ref completed) >= target;
        }

        private void Run()
        {
            try
            {
                foreach (var entry in queue.GetConsumingEnumerable())
                {
                    try
                    {
                        WriteNow(entry);
                    }
                    finally
                    {
                        Interlocked.Increment(ref completed);
                    }
                    if (queue.Count == 0)
                    {
                        lock (writeLock) TryFlushWriter();
                    }
                }
            }
            catch (Exception ex)
            {
                SelfLog.Report($"File writer for '{Path}' stopped", ex);
            }
        }

        private static bool IsIoFailure(Exception ex) =>
            ex is IOException || ex is UnauthorizedAccessException || ex is ObjectDisposedException;

        private void TryFlushWriter()
        {
            if (closed) return;
            try
            {
                writer.Flush();
            }
            catch (Exception ex) when (IsIoFailure(ex))
            {
                SelfLog.Report($"Could not flush '{Path}'", ex);
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
                // Dispose timed out and already closed the file: leftovers are dropped quietly
                // (no per-entry reports eating the SelfLog budget).
                if (closed)
                {
                    Interlocked.Increment(ref dropped);
                    return;
                }

                long droppedNow = Interlocked.Read(ref dropped);
                if (droppedNow != reportedDropped)
                {
                    SelfLog.Report($"'{Path}': {droppedNow - reportedDropped} entries dropped (queue full).");
                    reportedDropped = droppedNow;
                }

                try
                {
                    writer.Write(text);
                    if (thread == null) writer.Flush();
                }
                catch (Exception ex) when (IsIoFailure(ex))
                {
                    // e.g. disk full: lose this entry, keep the writer alive for the next ones.
                    Interlocked.Increment(ref failedWrites);
                    SelfLog.Report($"Could not write to '{Path}'", ex);
                }
            }
        }

        private void WriteHeader()
        {
            if (header == null) return;
            try
            {
                writer.Write(header);
                writer.Flush();
            }
            catch (Exception ex) when (IsIoFailure(ex))
            {
                // e.g. disk full: keep the writer; later entries retry and are reported individually.
                SelfLog.Report($"Could not write the header of '{Path}'", ex);
            }
        }

        private void Close()
        {
            try
            {
                if (footer != null) writer.Write(footer);
            }
            catch (Exception ex) when (IsIoFailure(ex))
            {
                SelfLog.Report($"Could not finish '{Path}'", ex);
            }

            try
            {
                writer.Dispose();
            }
            catch (Exception ex) when (IsIoFailure(ex))
            {
                SelfLog.Report($"Could not close '{Path}'", ex);
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0) return;

            queue.CompleteAdding();
            bool drained = thread == null || thread.Join(drainTimeout);
            if (!drained)
                SelfLog.Report($"'{Path}': writer did not drain within {drainTimeout.TotalSeconds:0.#}s; pending entries lost.");

            lock (writeLock)
            {
                Close();
                closed = true;
            }

            // A thread that is still draining keeps using the queue; it stops on its own once the queue is empty.
            if (drained) queue.Dispose();
        }
    }
}
