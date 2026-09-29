using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Settings;
using System;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Sinks.Config
{
    /// <summary>
    /// Everything a sink config may need to build its sink.
    /// </summary>
    public sealed class SinkBuildContext
    {
        public LogSettings Settings { get; }
        public IClock Clock { get; }

        /// <summary>When the logger was created; file sinks use it to name session files.</summary>
        public DateTime SessionStartUtc { get; }

        public SinkBuildContext(LogSettings settings, IClock clock)
        {
            Settings = settings;
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            SessionStartUtc = clock.UtcNow;
        }
    }

    /// <summary>
    /// Serializable description of a sink, stored in <see cref="LogSettings.sinks"/>.
    /// Derive from this (and mark the class <c>[Serializable]</c>) to make a custom sink
    /// configurable in the inspector; it appears in the "Add Sink" menu automatically.
    /// </summary>
    [Serializable]
    public abstract class LogSinkConfig
    {
        [Tooltip("Disabled sinks are not created.")]
        public bool enabled = true;

        [Tooltip("Entries below this level are not sent to this sink.")]
        public LogLevel minimumLevel = LogLevel.Debug;

        /// <summary>Label shown in the inspector.</summary>
        public abstract string DisplayName { get; }

        public abstract ILogSink CreateSink(SinkBuildContext context);

        /// <summary>
        /// How <paramref name="sample"/> would be written by this sink, for the inspector preview, or null when the
        /// sink has no text form. Override in custom configs to preview their output.
        /// </summary>
        public virtual string Preview(Dto.LogEntryDto sample, LogSettings settings) => null;

        /// <summary>True when <see cref="Preview"/> returns Unity rich text (shown rendered rather than as source).</summary>
        public virtual bool PreviewIsRichText => false;
    }
}
