using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace EldritchGames.EldritchLogger.Sinks.Network
{
    public enum HttpPayloadFormat
    {
        /// <summary>One JSON array per request (<c>application/json</c>).</summary>
        JsonArray,
        /// <summary>One JSON object per line (<c>application/x-ndjson</c>).</summary>
        JsonLines,
        /// <summary>Compact Log Event Format, one event per line, as accepted by Seq at <c>/api/events/raw?clef</c>.</summary>
        Clef
    }

    /// <summary>
    /// Posts batches of entries to an HTTP endpoint (Seq, a log collector, your own server).
    /// Failed requests are retried with exponential backoff; 4xx responses other than 408/429 are not retried.
    /// </summary>
    public sealed class HttpLogSink : BatchingLogSink
    {
        private readonly HttpClient client;
        private readonly Uri endpoint;
        private readonly HttpPayloadFormat format;
        private readonly KeyValuePair<string, string>[] headers;

        public override string Location => endpoint.ToString();

        public HttpLogSink(Uri endpoint,
                           HttpPayloadFormat format = HttpPayloadFormat.JsonArray,
                           IEnumerable<KeyValuePair<string, string>> headers = null,
                           LogLevel minimumLevel = LogLevel.Debug,
                           BatchingOptions options = null,
                           TimeSpan? requestTimeout = null,
                           HttpMessageHandler handler = null)
            // Validated in the base() arguments so an invalid URL throws before the worker thread starts.
            : base($"HTTP ({ValidateEndpoint(endpoint).Host})", minimumLevel, options)
        {
            this.endpoint = endpoint;
            this.format = format;
            this.headers = headers != null ? new List<KeyValuePair<string, string>>(headers).ToArray() : Array.Empty<KeyValuePair<string, string>>();
            client = handler != null ? new HttpClient(handler) : new HttpClient();
            client.Timeout = requestTimeout ?? TimeSpan.FromSeconds(10);
        }

        protected override SendResult SendBatch(IReadOnlyList<LogEntryDto> batch, CancellationToken cancellation)
        {
            var outcome = Send(client, endpoint, format, headers, batch, cancellation);
            if (outcome.Result != SendResult.Success) ReportSendFailure(outcome.Description);
            if (outcome.Result == SendResult.Reject)
                SelfLog.Report($"'{endpoint}' rejected a batch: {outcome.Description}");
            return outcome.Result;
        }

        /// <summary>The result of one POST: how the batch should be treated, and a readable status.</summary>
        public readonly struct SendOutcome
        {
            public SendResult Result { get; }
            public int StatusCode { get; }

            /// <summary>For example <c>"200 OK"</c> or <c>"401 Unauthorized"</c>.</summary>
            public string Description { get; }

            public SendOutcome(SendResult result, int statusCode, string description)
            {
                Result = result;
                StatusCode = statusCode;
                Description = description;
            }
        }

        /// <summary>
        /// Posts one batch with <paramref name="client"/>. Throws on network errors and timeouts; returns
        /// <see cref="SendResult.Retry"/> for 5xx, 408 and 429, and <see cref="SendResult.Reject"/> for other 4xx.
        /// </summary>
        public static SendOutcome Send(HttpClient client, Uri endpoint, HttpPayloadFormat format,
                                       IReadOnlyList<KeyValuePair<string, string>> headers,
                                       IReadOnlyList<LogEntryDto> batch, CancellationToken cancellation)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(Serialize(batch, format), Encoding.UTF8, ContentType(format))
            };
            if (headers != null)
                foreach (var header in headers)
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);

            using var response = client.SendAsync(request, cancellation).GetAwaiter().GetResult();
            var status = (int)response.StatusCode;
            var description = $"{status} {response.ReasonPhrase}";
            if (response.IsSuccessStatusCode) return new SendOutcome(SendResult.Success, status, description);

            bool retryable = status >= 500 || status == 408 || status == 429;
            return new SendOutcome(retryable ? SendResult.Retry : SendResult.Reject, status, description);
        }

        protected override void DisposeResources() => client.Dispose();

        internal static Uri ValidateEndpoint(Uri endpoint)
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));
            if (!endpoint.IsAbsoluteUri || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
                throw new ArgumentException("Endpoint must be an absolute http(s) URL.", nameof(endpoint));
            return endpoint;
        }

        public static string ContentType(HttpPayloadFormat format) => format switch
        {
            HttpPayloadFormat.JsonLines => "application/x-ndjson",
            HttpPayloadFormat.Clef => "application/vnd.serilog.clef",
            _ => "application/json"
        };

        /// <summary>Serializes a batch in the given format (public for tests and custom senders).</summary>
        public static string Serialize(IReadOnlyList<LogEntryDto> batch, HttpPayloadFormat format)
        {
            if (format == HttpPayloadFormat.JsonArray)
                return LogJson.Serialize(batch);

            var sb = new StringBuilder();
            foreach (var entry in batch)
            {
                sb.Append(format == HttpPayloadFormat.Clef
                    ? ToClef(entry).ToString(Newtonsoft.Json.Formatting.None)
                    : LogJson.Serialize(entry));
                sb.Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>Maps an entry to a CLEF event (<c>@t</c>, <c>@m</c>/<c>@mt</c>, <c>@l</c>, <c>@x</c> plus properties).</summary>
        public static JObject ToClef(LogEntryDto entry)
        {
            var clef = new JObject
            {
                ["@t"] = entry.Timestamp.ToUniversalTime().ToString("o"),
                ["@l"] = entry.Level switch
                {
                    LogLevel.Info => "Information",
                    LogLevel.Critical => "Fatal",
                    _ => entry.Level.ToString()
                },
                ["Category"] = entry.Category
            };

            // @m is the rendered message; @mt (when available) lets Seq group events by template.
            clef["@m"] = entry.Message;
            string template = entry.GetMetadata(LogPropertyKeys.MessageTemplate);
            if (template != null) clef["@mt"] = template;

            if (!string.IsNullOrEmpty(entry.Exception)) clef["@x"] = entry.Exception;

            if (entry.Metadata != null)
                foreach (var m in entry.Metadata)
                    if (m.Key != LogPropertyKeys.MessageTemplate && !m.Key.StartsWith("@"))
                        clef[m.Key] = m.Value;

            return clef;
        }
    }
}
