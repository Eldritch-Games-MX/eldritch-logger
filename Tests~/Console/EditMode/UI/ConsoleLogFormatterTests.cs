using EldritchGames.EldritchLogger.Console.Settings;
using EldritchGames.EldritchLogger.Console.Loader;
using NUnit.Framework;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode.UI
{
    [TestFixture]
    public class ConsoleLogFormatterTests
    {
        private ConsoleLogFormatter formatter;
        private CommandConsoleSettings settings;

        [SetUp]
        public void SetUp()
        {
            formatter = new ConsoleLogFormatter();
            settings = ScriptableObject.CreateInstance<CommandConsoleSettings>();
        }

        [Test]
        public void Format_StripsRichTextTags_WhenEnabled()
        {
            settings.stripRichTextTags = true;
            string input = "<color=red>Error:</color> Something went wrong";

            string output = formatter.Format(settings, input);

            Assert.AreEqual("Error: Something went wrong", output);
        }

        [Test]
        public void Format_KeepsRichTextTags_WhenDisabled()
        {
            settings.stripRichTextTags = false;
            string input = "<color=red>Error:</color> Something went wrong";

            string output = formatter.Format(settings, input);

            StringAssert.Contains("<color=red>", output);
        }
    }
}