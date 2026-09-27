using EldritchGames.EldritchLogger.Console.Commands;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode.Command
{
    [TestFixture]
    public class CommandHistoryTests
    {
        [Test]
        public void Constructor_InitialCountIsZero()
        {
            var history = new CommandHistory(5);
            Assert.AreEqual(0, history.Count);
        }

        [Test]
        public void Add_IncreasesCountUntilMaxSize()
        {
            var history = new CommandHistory(3);
            history.Add("cmd1");
            history.Add("cmd2");
            history.Add("cmd3");
            history.Add("cmd4"); // overwrites oldest

            Assert.AreEqual(3, history.Count);
            CollectionAssert.AreEqual(new[] { "cmd2", "cmd3", "cmd4" }, history.GetAll().ToArray());
        }

        [Test]
        public void Add_DuplicateFilteringEnabled_SkipsConsecutiveDuplicates()
        {
            var history = new CommandHistory(5, ignoreConsecutiveDuplicates: true);
            history.Add("cmd1");
            history.Add("cmd1"); // skipped
            history.Add("cmd2");

            Assert.AreEqual(2, history.Count);
            CollectionAssert.AreEqual(new[] { "cmd1", "cmd2" }, history.GetAll().ToArray());
        }

        [Test]
        public void EnableDuplicateFiltering_TogglesBehavior()
        {
            var history = new CommandHistory(5);
            history.Add("cmd1");
            history.Add("cmd1"); // allowed initially

            history.EnableDuplicateFiltering();
            history.Add("cmd1"); // skipped now

            Assert.AreEqual(2, history.Count);
            CollectionAssert.AreEqual(new[] { "cmd1", "cmd1" }, history.GetAll().ToArray());
        }

        [TestCase(5, 5, new[] { "a", "b", "c", "d", "e" }, new[] { "a", "b", "c", "d", "e" })]
        [TestCase(5, 3, new[] { "a", "b", "c", "d", "e" }, new[] { "c", "d", "e" })]
        [TestCase(5, 10, new[] { "a", "b", "c", "d", "e" }, new[] { "a", "b", "c", "d", "e" })]
        [TestCase(5, 2, new[] { "x", "y" }, new[] { "x", "y" })]
        [TestCase(5, 0, new[] { "x", "y" }, new string[0])]
        public void GetLast_ReturnsExpectedEntries(
            int bufferSize,
            int limit,
            string[] commands,
            string[] expected)
        {
            var history = new CommandHistory(bufferSize);
            foreach (var cmd in commands)
                history.Add(cmd);

            var result = history.GetLast(limit).ToArray();
            CollectionAssert.AreEqual(expected, result);
        }

        [Test]
        public void GetAll_ReturnsAllEntriesInOrder()
        {
            var history = new CommandHistory(5);
            history.Add("cmd1");
            history.Add("cmd2");
            history.Add("cmd3");

            var result = history.GetAll().ToArray();
            CollectionAssert.AreEqual(new[] { "cmd1", "cmd2", "cmd3" }, result);
        }

        [Test]
        public void GetAll_WhenBufferWraps_ReturnsCorrectOrder()
        {
            var history = new CommandHistory(3);
            history.Add("cmd1");
            history.Add("cmd2");
            history.Add("cmd3");
            history.Add("cmd4"); // overwrites cmd1

            var result = history.GetAll().ToArray();
            CollectionAssert.AreEqual(new[] { "cmd2", "cmd3", "cmd4" }, result);
        }
    }
}
