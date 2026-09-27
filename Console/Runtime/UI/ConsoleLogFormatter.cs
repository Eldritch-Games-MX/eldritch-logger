using EldritchGames.EldritchLogger.Console.Settings;
using System.Text.RegularExpressions;

namespace EldritchGames.EldritchLogger.Console.Loader
{
    public class ConsoleLogFormatter : IConsoleLogFormatter
    {
        private static readonly Regex RichTextRegex = new("<.*?>", RegexOptions.Compiled);

        public string Format(CommandConsoleSettings logSettings, string logMessage)
        {
            if (logSettings != null && logSettings.stripRichTextTags)
            {
                return RichTextRegex.Replace(logMessage, string.Empty);
            }
            return logMessage;
        }
    }
}
