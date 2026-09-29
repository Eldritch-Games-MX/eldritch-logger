using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Settings;
using EldritchGames.EldritchLogger.Sinks.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
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
                Headers(),
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

        public override string Preview(LogEntryDto sample, LogSettings settings) =>
            HttpLogSink.Serialize(new[] { sample }, format).TrimEnd('\n');

        /// <summary>
        /// Posts <paramref name="entry"/> once with the current settings (no batching, no retries) and describes the
        /// result, e.g. <c>"200 OK"</c>, <c>"401 Unauthorized"</c> or <c>"Timed out after 10s"</c>. Blocks: call it
        /// off the main thread. Used by the inspector's "Send Test Entry" button.
        /// </summary>
        public bool TrySendTest(LogEntryDto entry, out string result) => TrySendTest(entry, null, out result);

        internal bool TrySendTest(LogEntryDto entry, HttpMessageHandler handler, out string result)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var endpoint) ||
                (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
            {
                result = $"Invalid URL '{url}': it must be an absolute http(s) URL.";
                return false;
            }

            using var client = handler != null ? new HttpClient(handler, disposeHandler: false) : new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(requestTimeoutSeconds);
            try
            {
                var outcome = HttpLogSink.Send(client, endpoint, format, Headers(), new[] { entry }, CancellationToken.None);
                result = outcome.Description;
                return outcome.Result == SendResult.Success;
            }
            catch (Exception ex) when (ex is OperationCanceledException)
            {
                result = $"Timed out after {requestTimeoutSeconds:0.#}s.";
                return false;
            }
            catch (Exception ex)
            {
                result = ex.GetBaseException().Message;
                return false;
            }
        }

        private KeyValuePair<string, string>[] Headers() =>
            headers.Where(h => !string.IsNullOrWhiteSpace(h.name))
                   .Select(h => new KeyValuePair<string, string>(h.name, h.value))
                   .ToArray();
    }
}
