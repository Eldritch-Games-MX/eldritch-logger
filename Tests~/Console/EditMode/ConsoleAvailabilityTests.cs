using EldritchGames.EldritchLogger.Console.Settings;
using NUnit.Framework;
using System;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode
{
    public class ConsoleAvailabilityTests
    {
        [TestCase(ConsoleAvailability.Always, false, false, true)]
        [TestCase(ConsoleAvailability.DevelopmentBuilds, false, false, false)]
        [TestCase(ConsoleAvailability.DevelopmentBuilds, false, true, true)]
        [TestCase(ConsoleAvailability.DevelopmentBuilds, true, false, true)]
        [TestCase(ConsoleAvailability.EditorOnly, false, true, false)]
        [TestCase(ConsoleAvailability.EditorOnly, true, false, true)]
        [TestCase(ConsoleAvailability.Never, true, true, false)]
        public void IsAvailable(ConsoleAvailability availability, bool editor, bool development, bool expected)
        {
            Assert.That(ConsoleAvailabilityPolicy.IsAvailable(availability, editor, development), Is.EqualTo(expected));
        }

        [Test]
        public void DefaultSettings_AreDevelopmentBuildsWithCheats()
        {
            var settings = ScriptableObject.CreateInstance<CommandConsoleSettings>();
            Assert.That(settings.availability, Is.EqualTo(ConsoleAvailability.DevelopmentBuilds));
            Assert.That(settings.allowCheats, Is.True);
            Assert.That(ConsoleAvailabilityPolicy.IsAvailableHere(settings), Is.True, "the editor is always a development environment");
            UnityEngine.Object.DestroyImmediate(settings);
        }
    }
}
