using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Sinks;
using System;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Pipeline
{
    /// <summary>
    /// Copy-on-write sink list: reads (every log call) take no lock and allocate nothing;
    /// writes (rare) copy the array.
    /// </summary>
    public sealed class SinkCollection : ISinkRegistry
    {
        private readonly object writeLock = new();
        private ILogSink[] snapshot;

        public SinkCollection(IEnumerable<ILogSink> initial = null)
        {
            snapshot = initial != null ? new List<ILogSink>(initial).ToArray() : Array.Empty<ILogSink>();
        }

        /// <summary>The current sinks. The returned array must not be modified.</summary>
        public IReadOnlyList<ILogSink> Snapshot => System.Threading.Volatile.Read(ref snapshot);

        /// <summary>The lowest minimum level of all sinks, used for early filtering.</summary>
        public LogLevel LowestMinimumLevel
        {
            get
            {
                var sinks = Snapshot;
                if (sinks.Count == 0) return LogLevel.Critical + 1;

                var lowest = LogLevel.Critical;
                for (int i = 0; i < sinks.Count; i++)
                    if (sinks[i].MinimumLevel < lowest) lowest = sinks[i].MinimumLevel;
                return lowest;
            }
        }

        public void AddSink(ILogSink sink)
        {
            if (sink == null) throw new ArgumentNullException(nameof(sink));
            lock (writeLock)
            {
                if (Array.IndexOf(snapshot, sink) >= 0) return;
                var next = new ILogSink[snapshot.Length + 1];
                snapshot.CopyTo(next, 0);
                next[^1] = sink;
                System.Threading.Volatile.Write(ref snapshot, next);
            }
        }

        public void RemoveSink(ILogSink sink)
        {
            if (sink == null) throw new ArgumentNullException(nameof(sink));
            lock (writeLock)
            {
                int index = Array.IndexOf(snapshot, sink);
                if (index < 0) return;
                var next = new ILogSink[snapshot.Length - 1];
                Array.Copy(snapshot, 0, next, 0, index);
                Array.Copy(snapshot, index + 1, next, index, snapshot.Length - index - 1);
                System.Threading.Volatile.Write(ref snapshot, next);
            }
        }
    }
}
