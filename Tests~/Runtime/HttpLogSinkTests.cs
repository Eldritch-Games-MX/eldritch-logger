using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Sinks.Files;
using EldritchGames.EldritchLogger.Sinks.Network;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Tests
{
    public class HttpLogSinkTests
    {
        /// <summary>Records requests and answers with a scripted sequence of status codes.</summary>
        private sealed class FakeHandler : HttpMessageHandler
        {
            private readonly Queue<HttpStatusCode> responses;
            public readonly List<(HttpRequestMessage request, string body)> Requests = new();

            public FakeHandler(params HttpStatusCode[] responses) => this.responses = new Queue<HttpStatusCode>(responses);

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var body = await request.Content.ReadAsStringAsync();
                lock (Requests) Requests.Add((request, body));
                var status = responses.Count > 0 ? responses.Dequeue() : HttpStatusCode.OK;
                return new HttpResponseMessage(status);
            }
        }

        private static readonly Uri Endpoint = new("http://logs.example.test/ingest");

        private static BatchingOptions FastOptions(int batchSize = 100) => new()
        {
            BatchSize = batchSize,
            FlushInterval = TimeSpan.FromMilliseconds(50),
            RetryBaseDelay = TimeSpan.FromMilliseconds(10),
            MaxRetries = 2
        };

        private static LogEntryDto Entry(int i, string template = null)
        {
            var dto = new LogEntryDto { Timestamp = new DateTime(2026, 1, 1, 0, 0, i, DateTimeKind.Utc), Level = LogLevel.Info, Category = "Net", Message = "m" + i };
            if (template != null) dto.Metadata.Add(new MetadataEntry { Key = LogPropertyKeys.MessageTemplate, Value = template });
            return dto;
        }

        [Test]
        public void SendsBatches_WithHeaders_AndFlushesOnDispose()
        {
            var handler = new FakeHandler();
            using (var sink = new HttpLogSink(Endpoint, HttpPayloadFormat.JsonArray,
                       new[] { new KeyValuePair<string, string>("X-Api-Key", "secret") }, options: FastOptions(batchSize: 10), handler: handler))
            {
                for (int i = 0; i < 25; i++) sink.Emit(Entry(i));
            }

            var messages = handler.Requests.SelectMany(r => JArray.Parse(r.body)).Select(e => (string)e["Message"]).ToArray();
            Assert.That(messages, Is.EqualTo(Enumerable.Range(0, 25).Select(i => "m" + i)));
            Assert.That(handler.Requests.All(r => r.request.Headers.GetValues("X-Api-Key").Single() == "secret"), Is.True);
            Assert.That(handler.Requests.Max(r => JArray.Parse(r.body).Count), Is.LessThanOrEqualTo(10));
        }

        [Test]
        public void RetriesTemporaryFailures()
        {
            var handler = new FakeHandler(HttpStatusCode.InternalServerError, HttpStatusCode.TooManyRequests, HttpStatusCode.OK);
            using (var sink = new HttpLogSink(Endpoint, options: FastOptions(), handler: handler))
            {
                sink.Emit(Entry(1));
                sink.Flush();
                Assert.That(sink.DroppedCount, Is.EqualTo(0));
            }

            Assert.That(handler.Requests.Count, Is.EqualTo(3));
        }

        [Test]
        public void RejectedAndExhaustedBatches_AreDroppedAndCounted()
        {
            using var capture = new SelfLogCapture();

            var rejecting = new FakeHandler(HttpStatusCode.BadRequest);
            using (var sink = new HttpLogSink(Endpoint, options: FastOptions(), handler: rejecting))
            {
                sink.Emit(Entry(1));
                sink.Flush();
                Assert.That(sink.DroppedCount, Is.EqualTo(1));
            }
            Assert.That(rejecting.Requests.Count, Is.EqualTo(1), "4xx is not retried");

            var failing = new FakeHandler(Enumerable.Repeat(HttpStatusCode.ServiceUnavailable, 10).ToArray());
            using (var sink = new HttpLogSink(Endpoint, options: FastOptions(), handler: failing))
            {
                sink.Emit(Entry(2));
                sink.Flush();
                Assert.That(sink.DroppedCount, Is.EqualTo(1));
            }
            Assert.That(failing.Requests.Count, Is.EqualTo(3), "first attempt + MaxRetries");
            Assert.That(capture.Messages, Has.Some.Contains("dropped 1 entries"));
        }

        [Test]
        public void ClefPayload_MapsLevelsTemplatesAndProperties()
        {
            var entry = Entry(1, template: "Player {Name} joined");
            entry.Level = LogLevel.Critical;
            entry.Exception = "InvalidOperationException: x";
            entry.Metadata.Add(new MetadataEntry { Key = "Name", Value = "Bob" });

            var line = HttpLogSink.Serialize(new[] { entry }, HttpPayloadFormat.Clef).TrimEnd('\n');
            var clef = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(line,
                new Newtonsoft.Json.JsonSerializerSettings { DateParseHandling = Newtonsoft.Json.DateParseHandling.None });

            Assert.That((string)clef["@l"], Is.EqualTo("Fatal"));
            Assert.That((string)clef["@m"], Is.EqualTo("m1"));
            Assert.That((string)clef["@mt"], Is.EqualTo("Player {Name} joined"));
            Assert.That((string)clef["@x"], Is.EqualTo("InvalidOperationException: x"));
            Assert.That((string)clef["Name"], Is.EqualTo("Bob"));
            Assert.That((string)clef["Category"], Is.EqualTo("Net"));
            Assert.That(clef["MessageTemplate"], Is.Null);
            Assert.That((string)clef["@t"], Does.StartWith("2026-01-01T00:00:01"));
        }

        [Test]
        public void JsonLinesPayload_HasOneObjectPerLine()
        {
            var payload = HttpLogSink.Serialize(new[] { Entry(1), Entry(2) }, HttpPayloadFormat.JsonLines);
            var lines = payload.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Assert.That(lines.Select(l => (string)JObject.Parse(l)["Message"]), Is.EqualTo(new[] { "m1", "m2" }));
            Assert.That(HttpLogSink.ContentType(HttpPayloadFormat.JsonLines), Is.EqualTo("application/x-ndjson"));
        }

        [Test]
        public void RejectsNonHttpEndpoints()
        {
            Assert.Throws<ArgumentException>(() => new HttpLogSink(new Uri("ftp://example.test")));
        }

        // 2 ----------------------------------------------------------------------------------------------

        [TestCase("ftp://example.test/logs")]
        [TestCase("file:///C:/logs")]
        public void HttpSink_InvalidEndpoint_ThrowsWithoutStartingAWorker(string url)
        {
            int before = BatchingLogSink.RunningWorkers;

            Assert.Throws<ArgumentException>(() => new HttpLogSink(new Uri(url)));

            Assert.That(BatchingLogSink.RunningWorkers, Is.EqualTo(before));
        }

        [Test]
        public void HttpSink_Dispose_StopsItsWorker()
        {
            int before = BatchingLogSink.RunningWorkers;
            var sink = new HttpLogSink(new Uri("http://localhost:1/"), options: new BatchingOptions { DrainTimeout = TimeSpan.FromSeconds(1), MaxRetries = 0 });
            Assert.That(BatchingLogSink.RunningWorkers, Is.EqualTo(before + 1));

            sink.Dispose();

            Assert.That(BatchingLogSink.RunningWorkers, Is.EqualTo(before));
        }

        [Test]
        public void Failures_AreRecordedAsLastError_WithTheStatus()
        {
            var handler = new FakeHandler(HttpStatusCode.ServiceUnavailable);
            using var sink = new HttpLogSink(Endpoint, handler: handler, options: FastOptions());

            sink.Emit(new LogEntryDto { Message = "m" });
            sink.Flush();

            Assert.That(sink.SentCount, Is.EqualTo(1), "sent on the retry");
            Assert.That(sink.RetryCount, Is.EqualTo(1));
            Assert.That(sink.LastError, Is.EqualTo("503 Service Unavailable"));
            Assert.That(sink.LastErrorUtc, Is.Not.Null);
            Assert.That(sink.QueuedCount, Is.Zero);
        }

        [Test]
        public void ConfigTestSend_ReportsTheStatus_AndRejectsBadUrls()
        {
            var entry = new LogEntryDto { Message = "test", Timestamp = DateTime.UtcNow };
            var config = new Sinks.Config.HttpSinkConfig { url = Endpoint.ToString() };

            Assert.That(config.TrySendTest(entry, new FakeHandler(HttpStatusCode.OK), out var ok), Is.True);
            Assert.That(ok, Is.EqualTo("200 OK"));

            Assert.That(config.TrySendTest(entry, new FakeHandler(HttpStatusCode.Unauthorized), out var denied), Is.False);
            Assert.That(denied, Is.EqualTo("401 Unauthorized"));

            config.url = "ftp://nope";
            Assert.That(config.TrySendTest(entry, new FakeHandler(), out var invalid), Is.False);
            Assert.That(invalid, Does.StartWith("Invalid URL"));
        }
    }
}
