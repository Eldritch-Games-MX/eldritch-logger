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

    public class ConsoleLogFormatterTests
    {
        [Test]
        public void StripsRichTextOnlyWhenEnabled()
        {
            const string text = "<color=#FF0000>red</color> <b>bold</b>";

            Assert.That(new ConsoleLogFormatter(true).Format(text), Is.EqualTo("red bold"));
            Assert.That(new ConsoleLogFormatter(false).Format(text), Is.EqualTo(text));
            Assert.That(new ConsoleLogFormatter(true).Format(null), Is.EqualTo(string.Empty));
        }
    }
}
