using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Settings;
using EldritchGames.EldritchLogger.Sinks;
using System.Collections.Generic;
using UnityEditor;

namespace EldritchGames.EldritchLogger.EditorTools.LogViewer
{
    /// <summary>
    /// Sink that keeps the most recent entries of the current Play Mode session for the log viewer.
    /// Thread-safe; readers pull entries newer than a sequence number.
    /// </summary>
    public sealed class EditorLogSink : ILogSink
    {
        private readonly object gate = new();
        private readonly LogViewerEntry[] ring;
        private long nextSequence;

        public string Name => "Editor Log Viewer";
        public LogLevel MinimumLevel => LogLevel.Debug;

        /// <summary>Sequence number of the next entry; changes whenever an entry is captured.</summary>
        public long NextSequence
        {
            get { lock (gate) return nextSequence; }
        }

        public EditorLogSink(int capacity = 20_000) => ring = new LogViewerEntry[capacity];

        public void Emit(LogEntryDto entry)
        {
            lock (gate)
            {
                ring[nextSequence % ring.Length] = new LogViewerEntry(entry, nextSequence);
                nextSequence++;
            }
        }

        /// <summary>Appends entries with a sequence ≥ <paramref name="fromSequence"/> to <paramref name="into"/>.</summary>
        /// <returns>The sequence to pass next time.</returns>
        public long CopySince(long fromSequence, List<LogViewerEntry> into)
        {
            lock (gate)
            {
                long oldest = System.Math.Max(0, nextSequence - ring.Length);
                for (long s = System.Math.Max(fromSequence, oldest); s < nextSequence; s++)
                    into.Add(ring[s % ring.Length]);
                return nextSequence;
            }
        }

        public void Clear()
        {
            lock (gate)
            {
                System.Array.Clear(ring, 0, ring.Length);
                nextSequence = 0;
            }
        }
    }

    /// <summary>Attaches <see cref="Sink"/> to every logger installed while in Play Mode.</summary>
    [InitializeOnLoad]
    public static class LiveLogCapture
    {
        private const string EnabledPref = "EldritchLogger.LogViewer.CaptureLive";

        public static EditorLogSink Sink { get; } = new();

        /// <summary>Incremented every time a new Play Mode session starts capturing.</summary>
        public static int Session { get; private set; }

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledPref, true);
            set => EditorPrefs.SetBool(EnabledPref, value);
        }

        static LiveLogCapture()
        {
            LoggerBootstrap.Installed -= OnInstalled;
            LoggerBootstrap.Installed += OnInstalled;

            // Domain reload happened mid-play (e.g. script recompilation): re-attach to the running logger.
            if (EditorApplication.isPlaying && Enabled)
                ELoggerFactory.Sinks?.AddSink(Sink);
        }

        private static void OnInstalled(ISinkRegistry registry)
        {
            if (!Enabled || !EditorApplication.isPlayingOrWillChangePlaymode) return;

            Sink.Clear();
            Session++;
            registry.AddSink(Sink);
        }
    }
}
