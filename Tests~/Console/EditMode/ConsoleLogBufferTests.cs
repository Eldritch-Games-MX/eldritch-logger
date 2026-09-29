using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.UI;
using NUnit.Framework;
using System;
using System.Linq;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode
{
    public class ConsoleLogBufferTests
    {
        [Test]
        public void Wraps_KeepingTheNewestLines()
        {
            var buffer = new ConsoleLogBuffer(3);
            for (int i = 0; i < 5; i++) buffer.Add("l" + i);

            Assert.That(buffer.Count, Is.EqualTo(3));
            Assert.That(Enumerable.Range(0, 3).Select(i => buffer[i]), Is.EqualTo(new[] { "l2", "l3", "l4" }));
        }

        [Test]
        public void Version_ChangesOnEveryMutation()
        {
            var buffer = new ConsoleLogBuffer(2);
            int v0 = buffer.Version;
            buffer.Add("a");
            int v1 = buffer.Version;
            buffer.Clear();

            Assert.That(v1, Is.GreaterThan(v0));
            Assert.That(buffer.Version, Is.GreaterThan(v1));
            Assert.That(buffer.Count, Is.EqualTo(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = buffer[0]);
        }
    }
}
