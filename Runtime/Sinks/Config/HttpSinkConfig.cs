using EldritchGames.EldritchLogger.Sinks.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Sinks.Config
{
    /// <summary>
    /// Sends entries to an HTTP endpoint in batches. Not available on WebGL.
    /// </summary>
    /// <remarks>
    /// Header values (e.g. API keys) are stored in the settings asset and ship with builds;
    /// use a key with ingest-only permissions.
    /// </remarks>
    [Serializable]
    public sealed class HttpSinkConfig : LogSinkConfig
    {
        [Serializable]
        public sealed class Header
        {
            public string name;
            public string value;
        }

        [Tooltip("Endpoint URL, e.g. https://seq.example.com/api/events/raw?clef")]
        public string url = "http://localhost:5341/api/events/raw?clef";

        public HttpPayloadFormat format = HttpPayloadFormat.Clef;

        [Tooltip("Extra request headers, e.g. X-Seq-ApiKey. Values ship with builds.")]
        public List<Header> headers = new();

        [Header("Batching")]
        [Min(1)] public int batchSize = 100;
        [Min(0.1f)] public float flushIntervalSeconds = 2f;
        [Min(1)] public int queueCapacity = 10_000;
        [Min(0)] public int maxRetries = 3;
        [Min(1)] public float requestTimeoutSeconds = 10f;

        public override string DisplayName => "HTTP";

        public override ILogSink CreateSink(SinkBuildContext context)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var endpoint))
                throw new ArgumentException($"Invalid URL '{url}'.");

            return new HttpLogSink(
                endpoint,
                format,
                headers.Where(h => !string.IsNullOrWhiteSpace(h.name))
                       .Select(h => new KeyValuePair<string, string>(h.name, h.value)),
                minimumLevel,
                new BatchingOptions
                {
                    BatchSize = batchSize,
                    FlushInterval = TimeSpan.FromSeconds(flushIntervalSeconds),
                    QueueCapacity = queueCapacity,
                    MaxRetries = maxRetries
                },
                TimeSpan.FromSeconds(requestTimeoutSeconds));
        }
    }
}
