using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Domain;
using EldritchGames.EldritchLogger.Pipeline;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Tests
{
    public class BuiltInEnricherTests
    {
        [Test]
        public void BuiltInEnrichers_AddValues_WithoutOverwriting()
        {
            using var scene = new SceneEnricher();
            var version = new BuildVersionEnricher();
            var entry = new LogEntry(LogLevel.Info, default, "m");

            var fresh = new Dictionary<string, object>();
            scene.Enrich(entry, fresh);
            version.Enrich(entry, fresh);
            Assert.That(fresh[LogPropertyKeys.Scene], Is.EqualTo(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name));
            Assert.That(fresh[LogPropertyKeys.BuildVersion], Is.EqualTo(Application.version));

            var preset = new Dictionary<string, object> { [LogPropertyKeys.Scene] = "Mine", [LogPropertyKeys.BuildVersion] = "0.0.1" };
            scene.Enrich(entry, preset);
            version.Enrich(entry, preset);
            Assert.That(preset[LogPropertyKeys.Scene], Is.EqualTo("Mine"));
            Assert.That(preset[LogPropertyKeys.BuildVersion], Is.EqualTo("0.0.1"));
        }
    }
}
