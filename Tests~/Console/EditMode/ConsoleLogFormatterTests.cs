using EldritchGames.EldritchLogger.Console.UI;
using NUnit.Framework;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode
{
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
