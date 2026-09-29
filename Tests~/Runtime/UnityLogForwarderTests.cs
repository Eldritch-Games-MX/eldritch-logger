using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Formatting;
using EldritchGames.EldritchLogger.Pipeline;
using EldritchGames.EldritchLogger.Sinks;
using EldritchGames.EldritchLogger.Sinks.Files;
using EldritchGames.EldritchLogger.Sinks.Network;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;

namespace EldritchGames.EldritchLogger.Tests
{
    public class UnityLogForwarderTests
    {
        [TestCase(LogType.Log, UnityLogCapture.ErrorsAndExceptions, null)]
        [TestCase(LogType.Warning, UnityLogCapture.ErrorsAndExceptions, null)]
        [TestCase(LogType.Error, UnityLogCapture.ErrorsAndExceptions, LogLevel.Error)]
        [TestCase(LogType.Exception, UnityLogCapture.ErrorsAndExceptions, LogLevel.Error)]
        [TestCase(LogType.Assert, UnityLogCapture.ErrorsAndExceptions, LogLevel.Error)]
        [TestCase(LogType.Warning, UnityLogCapture.WarningsAndAbove, LogLevel.Warning)]
        [TestCase(LogType.Log, UnityLogCapture.All, LogLevel.Info)]
        [TestCase(LogType.Error, UnityLogCapture.Off, null)]
        public void ToLevel(LogType type, UnityLogCapture capture, LogLevel? expected)
        {
            Assert.That(UnityLogForwarder.ToLevel(type, capture), Is.EqualTo(expected));
        }

        [Test]
        public void ForwardsUnityErrors_WithSourceAndStackTrace()
        {
            var sink = new RecordingSink();
            using var logger = new EldritchLoggerBuilder().AddSink(sink).CaptureUnityLogs(UnityLogCapture.ErrorsAndExceptions).Build();

            LogAssert.Expect(LogType.Error, "engine failure");
            Debug.LogError("engine failure");
            Debug.Log("plain log is below the capture level");

            var entry = sink.Entries.Single();
            Assert.That(entry.Level, Is.EqualTo(LogLevel.Error));
            Assert.That(entry.Category, Is.EqualTo("Unity"));
            Assert.That(entry.GetMetadata(LogPropertyKeys.Source), Is.EqualTo(LogPropertyKeys.UnitySource));
            Assert.That(entry.GetMetadata(LogPropertyKeys.StackTrace), Does.Contain(nameof(ForwardsUnityErrors_WithSourceAndStackTrace)));
        }

        [Test]
        public void DoesNotLoop_AndDoesNotEchoBackToTheUnityConsole()
        {
            var sink = new RecordingSink();
            int unityMessages = 0;
            Application.LogCallback count = (_, _, _) => unityMessages++;
            Application.logMessageReceived += count;
            try
            {
                using var logger = new EldritchLoggerBuilder()
                    .AddSink(sink)
                    .AddSink(new UnityConsoleSink(new TextLogFormatter(null, richText: false)))
                    .CaptureUnityLogs(UnityLogCapture.All)
                    .Build();

                logger.AtInfo().Log("from the logger");  // UnityConsoleSink writes it; the forwarder must ignore that echo
                Debug.Log("from Unity");                 // forwarded; UnityConsoleSink must not print it again

                Assert.That(sink.Entries.Select(e => e.Message), Is.EqualTo(new[] { "from the logger", "from Unity" }));
                Assert.That(unityMessages, Is.EqualTo(2));
            }
            finally
            {
                Application.logMessageReceived -= count;
            }
        }

        [Test]
        public void StopsForwardingWhenTheLoggerIsDisposed()
        {
            var sink = new RecordingSink();
            var logger = new EldritchLoggerBuilder().AddSink(sink).CaptureUnityLogs(UnityLogCapture.All).Build();

            logger.Dispose();
            Debug.Log("after dispose");

            Assert.That(sink.Entries, Is.Empty);
        }

        // 5 ----------------------------------------------------------------------------------------------

        [Test]
        public void Forwarder_IgnoresSelfLogReports_FromAnyThread()
        {
            var sink = new RecordingSink();
            using var logger = new EldritchLoggerBuilder().AddSink(sink).CaptureUnityLogs(UnityLogCapture.All).Build();
            SelfLog.Reset();
            try
            {
                // SelfLog writes to the Unity Console (Debug.LogWarning) from a sink-like background thread.
                Task.Run(() => SelfLog.Report("sink trouble")).Wait();
                Debug.LogWarning("a real game warning");

                Assert.That(sink.Entries.Select(e => e.Message), Is.EqualTo(new[] { "a real game warning" }));
            }
            finally
            {
                SelfLog.Reset();
            }
        }

        [Test]
        public void IsFromUnity_RecognisesForwardedEntries()
        {
            var forwarded = new LogEntryDto();
            forwarded.Metadata.Add(new MetadataEntry { Key = LogPropertyKeys.Source, Value = LogPropertyKeys.UnitySource });

            Assert.That(UnityLogForwarder.IsFromUnity(forwarded), Is.True);
            Assert.That(UnityLogForwarder.IsFromUnity(new LogEntryDto()), Is.False);
            Assert.That(UnityLogForwarder.IsFromUnity(null), Is.False);
        }

        private static void RaiseUnhandled(UnityLogForwarder forwarder, Exception exception) =>
            typeof(UnityLogForwarder).GetMethod("OnUnhandledException", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(forwarder, new object[] { null, new UnhandledExceptionEventArgs(exception, false) });

        private static void RaiseUnityLog(UnityLogForwarder forwarder, string condition, LogType type) =>
            typeof(UnityLogForwarder).GetMethod("OnLogMessage", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(forwarder, new object[] { condition, "at Somewhere()", type });

        private sealed class FlushCountingSink : ILogSink, IFlushableSink
        {
            public readonly List<LogEntryDto> Entries = new();
            public int Flushes;
            public string Name => "FlushCounting";
            public LogLevel MinimumLevel => LogLevel.Debug;
            public void Emit(LogEntryDto entry) => Entries.Add(entry);
            public void Flush() => Flushes++;
        }

        [Test]
        public void UncaughtException_IsForwardedOnce_WhicheverSourceComesFirst()
        {
            var sink = new FlushCountingSink();
            using var logger = new EldritchLoggerBuilder().AddSink(sink).Build();
            using var forwarder = new UnityLogForwarder(logger, UnityLogCapture.ErrorsAndExceptions);

            // .NET event first, then Unity's log line.
            RaiseUnhandled(forwarder, new InvalidOperationException("first"));
            RaiseUnityLog(forwarder, "InvalidOperationException: first", LogType.Exception);

            // Unity's log line first, then the .NET event.
            RaiseUnityLog(forwarder, "NullReferenceException: second", LogType.Exception);
            RaiseUnhandled(forwarder, new NullReferenceException("second"));

            Assert.That(sink.Entries.Count, Is.EqualTo(2));
            Assert.That(sink.Flushes, Is.EqualTo(2), "every unhandled exception flushes the sinks");
        }

        [Test]
        public void RepeatedExceptionsFromOneSource_AreAllForwarded()
        {
            var sink = new FlushCountingSink();
            using var logger = new EldritchLoggerBuilder().AddSink(sink).Build();
            using var forwarder = new UnityLogForwarder(logger, UnityLogCapture.ErrorsAndExceptions);

            for (int i = 0; i < 3; i++)
                RaiseUnityLog(forwarder, "IndexOutOfRangeException: every frame", LogType.Exception);

            Assert.That(sink.Entries.Count, Is.EqualTo(3));
        }

        [Test]
        public void SuppressCapture_KeepsWritesOutOfTheLogger_OnlyInsideTheScope()
        {
            var sink = new RecordingSink();
            using var logger = new EldritchLoggerBuilder().AddSink(sink).CaptureUnityLogs(UnityLogCapture.All).Build();

            using (UnityLogForwarder.SuppressCapture())
            {
                Debug.Log("written by a custom sink");
            }
            Debug.Log("game log");

            var scope = UnityLogForwarder.SuppressCapture();
            scope.Dispose();
            scope.Dispose(); // double dispose must not unbalance the counter
            Debug.Log("after double dispose");

            Assert.That(sink.Entries.Select(e => e.Message), Is.EqualTo(new[] { "game log", "after double dispose" }));
        }

        private sealed class SlowFlushSink : ILogSink, IFlushableSink
        {
            public string Name => "SlowFlush";
            public LogLevel MinimumLevel => LogLevel.Debug;
            public void Emit(LogEntryDto entry) { }
            public void Flush() => Thread.Sleep(5000);
        }

        [Test]
        public void UnhandledException_FlushIsBounded()
        {
            using var logger = new EldritchLoggerBuilder().AddSink(new SlowFlushSink()).Build();
            using var forwarder = new UnityLogForwarder(logger, UnityLogCapture.ErrorsAndExceptions);
            var handler = typeof(UnityLogForwarder).GetMethod("OnUnhandledException", BindingFlags.NonPublic | BindingFlags.Instance);

            var started = DateTime.UtcNow;
            handler.Invoke(forwarder, new object[] { null, new UnhandledExceptionEventArgs(new Exception("crash"), true) });

            Assert.That(DateTime.UtcNow - started, Is.LessThan(UnityLogForwarder.CrashFlushTimeout + TimeSpan.FromSeconds(1)));
        }

        [Test]
        public void AUserPropertyCalledSource_IsNotMistakenForUnityOutput()
        {
            var entry = new LogEntryDto { Level = LogLevel.Warning, Category = "General", Message = "user entry", Timestamp = DateTime.UtcNow };
            entry.Metadata.Add(new MetadataEntry { Key = "Source", Value = "Unity" });

            Assert.That(UnityLogForwarder.IsFromUnity(entry), Is.False);

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("user entry"));
            new UnityConsoleSink(new TextLogFormatter(null, richText: false)).Emit(entry);
        }
    }
}
