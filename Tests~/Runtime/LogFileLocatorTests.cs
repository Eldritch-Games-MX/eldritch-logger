using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Sinks.Files;
using EldritchGames.EldritchLogger.Sinks.Network;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace EldritchGames.EldritchLogger.Tests
{
    public class LogFileLocatorTests
    {
        private string directory;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "EldritchLocator_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test]
        public void PathFormats()
        {
            var locator = new LogFileLocator(directory, "game", ".txt");
            var session = new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Local).ToUniversalTime();

            Assert.That(Path.GetFileName(locator.SingleFilePath), Is.EqualTo("game.txt"));
            Assert.That(Path.GetFileName(locator.SessionFilePath(session)), Is.EqualTo("game_20260304_050607.txt"));
            Assert.That(Path.GetFileName(new LogFileLocator(directory, " ", ".txt").SingleFilePath), Is.EqualTo("eldritch_logs.txt"));
            Assert.Throws<ArgumentNullException>(() => new LogFileLocator(directory, "x", null));
        }

        [Test]
        public void Locator_KeepsOnlyTheNewestSessions()
        {
            var locator = new LogFileLocator(directory, "game", ".jsonl");
            var start = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            for (int i = 0; i < 6; i++)
                File.WriteAllText(locator.SessionFilePath(start.AddMinutes(i)), "");
            File.WriteAllText(Path.Combine(directory, "other.jsonl"), "");

            locator.DeleteOldSessions(keep: 2);

            var remaining = Directory.GetFiles(directory).Select(Path.GetFileName).OrderBy(n => n).ToArray();
            Assert.That(remaining, Is.EqualTo(new[]
            {
                Path.GetFileName(locator.SessionFilePath(start.AddMinutes(4))),
                Path.GetFileName(locator.SessionFilePath(start.AddMinutes(5))),
                "other.jsonl"
            }));
        }

        [Test]
        public void Locator_IgnoresFilesWhoseExtensionOnlyStartsTheSame()
        {
            var directory = Path.Combine(Path.GetTempPath(), "EldritchExt_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var locator = new LogFileLocator(directory, "game", ".txt");
                File.WriteAllText(Path.Combine(directory, "game_20260101_120000.txt"), "");
                File.WriteAllText(Path.Combine(directory, "game_20260101_120000.txtx"), "");

                locator.DeleteOldSessions(keep: 0);

                Assert.That(Directory.GetFiles(directory).Select(Path.GetFileName), Is.EqualTo(new[] { "game_20260101_120000.txtx" }));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void Retention_LeavesOtherSinksFiles_WhoseNameStartsTheSame()
        {
            var game = new LogFileLocator(directory, "game", ".txt");
            var network = new LogFileLocator(directory, "game_net", ".txt");
            var start = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            for (int i = 0; i < 3; i++)
            {
                File.WriteAllText(game.SessionFilePath(start.AddMinutes(i)), "");
                File.WriteAllText(network.SessionFilePath(start.AddMinutes(i)), "");
            }

            game.DeleteOldSessions(keep: 1);

            var names = Directory.GetFiles(directory).Select(Path.GetFileName).ToArray();
            Assert.That(names.Count(n => n.StartsWith("game_net_")), Is.EqualTo(3), "another sink's sessions are untouched");
            Assert.That(names, Does.Contain(Path.GetFileName(game.SessionFilePath(start.AddMinutes(2)))));
            Assert.That(names.Length, Is.EqualTo(4));
        }
    }
}
