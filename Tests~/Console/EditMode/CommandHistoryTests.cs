using EldritchGames.EldritchLogger.Console.Commands;
using NUnit.Framework;
using System;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode
{
    public class CommandHistoryTests
    {
        [Test]
        public void KeepsTheNewestEntries_OldestFirst()
        {
            var history = new CommandHistory(3);
            foreach (var c in new[] { "a", "b", "c", "d" }) history.Add(c);

            Assert.That(history.GetAll(), Is.EqualTo(new[] { "b", "c", "d" }));
            Assert.That(history.GetLast(2), Is.EqualTo(new[] { "c", "d" }));
            Assert.That(history.GetLast(10), Is.EqualTo(new[] { "b", "c", "d" }));
            Assert.That(history.GetLast(-1), Is.Empty);
        }

        [Test]
        public void ConsecutiveDuplicatesAndBlanks_CanBeIgnored()
        {
            var history = new CommandHistory(10, ignoreConsecutiveDuplicates: true);
            foreach (var c in new[] { "a", "a", " ", "b", "a" }) history.Add(c);

            Assert.That(history.GetAll(), Is.EqualTo(new[] { "a", "b", "a" }));
        }

        [Test]
        public void RejectsNonPositiveSize()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CommandHistory(0));
        }
    }
}
