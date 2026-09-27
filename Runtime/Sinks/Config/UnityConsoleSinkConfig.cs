using EldritchGames.EldritchLogger.Formatting;
using System;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Sinks.Config
{
    [Serializable]
    public sealed class UnityConsoleSinkConfig : LogSinkConfig
    {
        [Tooltip("Pass the entry's context object to Debug.Log so clicking the message selects it.")]
        public bool useContextObjects = true;

        [Tooltip("Suppress Unity's automatic stack traces; EldritchLogger includes exception traces itself.")]
        public bool suppressUnityStackTrace = true;

        public override string DisplayName => "Unity Console";

        public override ILogSink CreateSink(SinkBuildContext context) =>
            new UnityConsoleSink(new TextLogFormatter(context.Settings, richText: true),
                                 minimumLevel, useContextObjects, suppressUnityStackTrace);
    }
}
