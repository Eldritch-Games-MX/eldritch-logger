using EldritchGames.EldritchLogger.Console.Commands;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode.Command
{
    [TestFixture]
    public class HistoryCommandTests
    {
        private CommandHistory history;
        private HistoryCommand historyCommand;
        private List<string> logMessages;

        [SetUp]
        public void SetUp()
        {
            history = new CommandHistory(10);
            historyCommand = new HistoryCommand(history);

            logMessages = new List<string>();
            Application.logMessageReceived += CaptureLog;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= CaptureLog;
        }

        private void CaptureLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Log)
                logMessages.Add(condition);
        }

        [Test]
        public void Name_ReturnsHistory()
        {
            Assert.AreEqual("history", historyCommand.Name);
        }

        [Test]
        public void Description_ReturnsExpectedText()
        {
            Assert.AreEqual(
                "Displays previously entered commands. Use --limit N to show only the last N entries.",
                historyCommand.Description);
        }

        [TestCase(3, new[] { "cmd1", "cmd2", "cmd3" }, null, new[] { "1: cmd1", "2: cmd2", "3: cmd3" })]
        [TestCase(5, new[] { "cmdA", "cmdB", "cmdC", "cmdD", "cmdE" }, "2", new[] { "1: cmdD", "2: cmdE" })]
        [TestCase(2, new[] { "x", "y" }, "notANumber", new[] { "1: x", "2: y" })]
        [TestCase(0, new string[0], null, new string[0])]
        public void Execute_ParameterizedLimitCases(
            int count,
            string[] historyEntries,
            string limitFlag,
            string[] expectedLogs)
        {
            foreach (var entry in historyEntries)
                history.Add(entry);

            var flags = new Dictionary<string, string>();
            if (limitFlag != null)
                flags["limit"] = limitFlag;

            historyCommand.Execute(new List<string>(), flags);

            CollectionAssert.AreEquivalent(expectedLogs, logMessages);
        }
    }
}
