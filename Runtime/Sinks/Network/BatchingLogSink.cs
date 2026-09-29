using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace EldritchGames.EldritchLogger.Sinks.Network
{
    public enum SendResult
    {
        Success,
        /// <summary>Temporary failure (timeout, 5xx, 429): the batch is retried with backoff.</summary>
        Retry,
        /// <summary>The batch will never be accepted (e.g. 400): it is dropped without retrying.</summary>
        Reject
    }

    public sealed class BatchingOptions
    {
        /// <summary>Send as soon as this many entries are waiting.</summary>
        public int BatchSize { get; set; } = 100;

        /// <summary>Send whatever is waiting at least this often.</summary>
        public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>Entries waiting to be sent. When full, the oldest is dropped.</summary>
        public int QueueCapacity { get; set; } = 10_000;

        /// <summary>Retries after the first attempt, with exponential backoff.</summary>
        public int MaxRetries { get; set; } = 3;

        public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>How long <see cref="BatchingLogSink.Dispose"/> waits for pending entries to be sent.</summary>
        public TimeSpan DrainTimeout { get; set; } = TimeSpan.FromSeconds(5);
    }

    /// <summary>
    /// Base class for sinks that ship entries in batches from a background thread
    /// (HTTP endpoints, analytics services...). Subclasses implement <see cref="SendBatch"/>.
    /// </summary>
    public abstract class BatchingLogSink : ILogSink, IFlushableSink, ISinkDiagnostics, IDisposable
    {
        private readonly BlockingCollection<LogEntryDto> queue;
        private readonly BatchingOptions options;
        private readonly Thread thread;
        private readonly CancellationTokenSource disposing = new();
        private long dropped;
        // Entries are handled in the order they were accepted, so "the first N accepted entries are done"
        // is simply completed >= N. Flush waits for the entries accepted before it was called, not for
        // entries other threads keep logging meanwhile.
        private long accepted;    // taken by Emit
        private long completed;   // delivered, rejected, or dropped
        private long flushTarget; // the worker sends partial batches until completed reaches this
        private long sent;
        private long retries;
        private string lastError;
        private long lastErrorTicks;
        private int disposed;
        private static int runningWorkers;

        /// <summary>Worker threads currently running across all batching sinks (for tests and diagnostics).</summary>
        internal static int RunningWorkers => Volatile.Read(ref runningWorkers);

        public string Name { get; }
        public LogLevel MinimumLevel { get; }
        public long DroppedCount => Interlocked.Read(ref dropped);

        /// <summary>Where entries are sent (shown by editor tooling).</summary>
        public abstract string Location { get; }

        /// <summary>Entries accepted and not yet sent or dropped.</summary>
        public long QueuedCount => Math.Max(0, Interlocked.Read(ref accepted) - Interlocked.Read(ref completed));

        /// <summary>Entries delivered successfully.</summary>
        public long SentCount => Interlocked.Read(ref sent);

        /// <summary>Send attempts that failed and were retried.</summary>
        public long RetryCount => Interlocked.Read(ref retries);

        /// <summary>The most recent send failure (status or exception), or null if none happened.</summary>
        public string LastError => Volatile.Read(ref lastError);

        /// <summary>When <see cref="LastError"/> happened (UTC), or null.</summary>
        public DateTime? LastErrorUtc
        {
            get
            {
                long ticks = Interlocked.Read(ref lastErrorTicks);
                return ticks == 0 ? null : new DateTime(ticks, DateTimeKind.Utc);
            }
        }

        /// <summary>Records why the current send failed; shown by <see cref="LastError"/>. Called from <see cref="SendBatch"/>.</summary>
        protected void ReportSendFailure(string detail)
        {
            Volatile.Write(ref lastError, detail);
            Interlocked.Exchange(ref lastErrorTicks, DateTime.UtcNow.Ticks);
        }

        protected BatchingLogSink(string name, LogLevel minimumLevel, BatchingOptions options = null)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            throw new PlatformNotSupportedException($"{GetType().Name} needs threads, which WebGL does not support.");
#else
            Name = name;
            MinimumLevel = minimumLevel;
            this.options = options ?? new BatchingOptions();
            if (this.options.BatchSize <= 0) throw new ArgumentOutOfRangeException(nameof(options), "BatchSize must be positive.");

            queue = new BlockingCollection<LogEntryDto>(new ConcurrentQueue<LogEntryDto>(), Math.Max(1, this.options.QueueCapacity));
            thread = new Thread(Run) { IsBackground = true, Name = $"EldritchLogger: {name}" };
            Interlocked.Increment(ref runningWorkers);
            thread.Start();
#endif
        }

        /// <summary>Sends one batch. Runs on the sink's background thread.</summary>
        protected abstract SendResult SendBatch(IReadOnlyList<LogEntryDto> batch, CancellationToken cancellation);

        public void Emit(LogEntryDto entry)
        {
            if (Volatile.Read(ref disposed) != 0) return;
            try
            {
                Interlocked.Increment(ref accepted);
                while (!queue.TryAdd(entry))
                    if (queue.TryTake(out _))
                    {
                        Interlocked.Increment(ref dropped);
                        Interlocked.Increment(ref completed);
                    }
            }
            catch (InvalidOperationException)
            {
                Interlocked.Increment(ref completed); // disposed concurrently: the entry is discarded
            }
        }

        /// <summary>
        /// Sends everything accepted before this call without waiting for the flush interval, waiting up to the
        /// drain timeout. Entries logged meanwhile by other threads are batched normally and not waited for.
        /// </summary>
        public void Flush()
        {
            var deadline = DateTime.UtcNow + options.DrainTimeout;
            long target = Interlocked.Read(ref accepted);
            RaiseFlushTarget(target);
            while (Interlocked.Read(ref completed) < target && DateTime.UtcNow < deadline)
                Thread.Sleep(5);
        }

        private void RaiseFlushTarget(long target)
        {
            long current;
            while ((current = Interlocked.Read(ref flushTarget)) < target &&
                   Interlocked.CompareExchange(ref flushTarget, target, current) != current)
            {
            }
        }

        private bool FlushOutstanding => Interlocked.Read(ref completed) < Interlocked.Read(ref flushTarget);

        private void Run()
        {
            var batch = new List<LogEntryDto>(options.BatchSize);
            var nextSend = DateTime.UtcNow + options.FlushInterval;

            try
            {
                while (!queue.IsCompleted)
                {
                    int wait = FlushOutstanding ? 0 : (int)Math.Max(0, Math.Min(100, (nextSend - DateTime.UtcNow).TotalMilliseconds));
                    try
                    {
                        if (queue.TryTake(out var entry, wait))
                        {
                            batch.Add(entry);
                            while (batch.Count < options.BatchSize && queue.TryTake(out entry)) batch.Add(entry);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        break; // completed
                    }

                    bool intervalDue = DateTime.UtcNow >= nextSend;
                    if (batch.Count >= options.BatchSize || (batch.Count > 0 && (intervalDue || FlushOutstanding)))
                    {
                        Deliver(batch);
                        batch.Clear();
                    }

                    if (intervalDue) nextSend = DateTime.UtcNow + options.FlushInterval;
                }

                // Drain what is left after Dispose.
                while (queue.TryTake(out var remaining)) batch.Add(remaining);
                for (int i = 0; i < batch.Count; i += options.BatchSize)
                    Deliver(batch.GetRange(i, Math.Min(options.BatchSize, batch.Count - i)));
            }
            catch (Exception ex)
            {
                SelfLog.Report($"Sink '{Name}' stopped", ex);
            }
            finally
            {
                Interlocked.Decrement(ref runningWorkers);
            }
        }

        private void Deliver(List<LogEntryDto> batch)
        {
            try
            {
                for (int attempt = 0; ; attempt++)
                {
                    SendResult result;
                    Exception error = null;
                    try
                    {
                        result = SendBatch(batch, disposing.Token);
                    }
                    catch (Exception ex)
                    {
                        result = SendResult.Retry;
                        error = ex;
                        ReportSendFailure($"{ex.GetType().Name}: {ex.GetBaseException().Message}");
                    }

                    if (result == SendResult.Success)
                    {
                        Interlocked.Add(ref sent, batch.Count);
                        return;
                    }

                    bool giveUp = result == SendResult.Reject || attempt >= options.MaxRetries || disposing.IsCancellationRequested;
                    if (giveUp)
                    {
                        Interlocked.Add(ref dropped, batch.Count);
                        SelfLog.Report($"Sink '{Name}' dropped {batch.Count} entries after {attempt + 1} attempt(s) ({result})", error);
                        return;
                    }

                    Interlocked.Increment(ref retries);
                    var delay = TimeSpan.FromMilliseconds(options.RetryBaseDelay.TotalMilliseconds * Math.Pow(2, attempt));
                    disposing.Token.WaitHandle.WaitOne(delay);
                }
            }
            finally
            {
                Interlocked.Add(ref completed, batch.Count);
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0) return;

            queue.CompleteAdding();
            if (!thread.Join(options.DrainTimeout))
            {
                disposing.Cancel(); // stop retrying
                if (!thread.Join(TimeSpan.FromSeconds(1)))
                {
                    // The worker is stuck in a send: leave its resources to it (and the GC) rather than
                    // disposing them underneath it. It stops once the send returns, since adding is complete.
                    SelfLog.Report($"Sink '{Name}' did not finish sending within {options.DrainTimeout.TotalSeconds:0.#}s; pending entries lost.");
                    return;
                }
            }

            DisposeResources();
            queue.Dispose();
            disposing.Dispose();
        }

        /// <summary>
        /// Releases subclass resources after the background thread has stopped. Not called when the thread is
        /// still stuck in a send after the drain timeout.
        /// </summary>
        protected virtual void DisposeResources() { }
    }
}
