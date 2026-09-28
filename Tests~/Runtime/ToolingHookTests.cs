using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Settings;
using EldritchGames.EldritchLogger.Sinks;
using EldritchGames.EldritchLogger.Sinks.Files;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace EldritchGames.EldritchLogger.Tests
{
    public class ToolingHookTests
    {
        [TearDown]
        public void TearDown() => LoggerBootstrap.Shutdown();

        [Test]
        public void Install_RaisesInstalled_AndShutdownRaisesUninstalling()
        {
            ISinkRegistry installedRegistry = null;
            int uninstalling = 0;
            Action<ISinkRegistry> onInstalled = r => installedRegistry = r;
            Action onUninstalling = () => uninstalling++;
            LoggerBootstrap.Installed += onInstalled;
            LoggerBootstrap.Uninstalling += onUninstalling;
            try
            {
                var sink = new RecordingSink();
                LoggerBootstrap.Install(new EldritchLoggerBuilder().AddSink(sink).Build());

                Assert.That(installedRegistry, Is.Not.Null);
                Assert.That(installedRegistry.All, Does.Contain(sink));
                Assert.That(ELoggerFactory.Sinks.All, Does.Contain(sink));

                LoggerBootstrap.Shutdown();
                Assert.That(uninstalling, Is.EqualTo(1));
                Assert.That(ELoggerFactory.Sinks, Is.Null);
            }
            finally
            {
                LoggerBootstrap.Installed -= onInstalled;
                LoggerBootstrap.Uninstalling -= onUninstalling;
            }
        }

        [Test]
        public void ThrowingInstalledHandler_DoesNotBreakInstall()
        {
            using var capture = new SelfLogCapture();
            Action<ISinkRegistry> throwing = _ => throw new InvalidOperationException("handler");
            LoggerBootstrap.Installed += throwing;
            try
            {
                LoggerBootstrap.Install(new EldritchLoggerBuilder().Build());
                Assert.That(ELoggerFactory.Sinks, Is.Not.Null);
                Assert.That(capture.Messages, Has.Some.Contains("Installed handler failed"));
            }
            finally
            {
                LoggerBootstrap.Installed -= throwing;
            }
        }

        [Test]
        public void FileSinks_ExposeDiagnostics()
        {
            var path = Path.Combine(Path.GetTempPath(), $"eldritch_diag_{Guid.NewGuid():N}.jsonl");
            try
            {
                using var sink = new JsonLinesFileSink(path);
                var diagnostics = (ISinkDiagnostics)sink;
                Assert.That(diagnostics.Location, Is.EqualTo(path));
                Assert.That(diagnostics.DroppedCount, Is.EqualTo(0));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void ResolveDirectory_DefaultsToPersistentDataPath()
        {
            Assert.That(LogFileLocator.ResolveDirectory(""), Is.EqualTo(UnityEngine.Application.persistentDataPath));
            Assert.That(LogFileLocator.ResolveDirectory("C:/logs"), Is.EqualTo("C:/logs"));
        }
    }
}
