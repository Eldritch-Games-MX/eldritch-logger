using System.Text.RegularExpressions;

namespace EldritchGames.EldritchLogger.Console.UI
{
    /// <summary>Transforms a line before it is displayed in the console.</summary>
    public interface IConsoleLogFormatter
    {
        string Format(string message);
    }

    /// <summary>Optionally strips rich-text tags (<c>&lt;color=...&gt;</c>, <c>&lt;b&gt;</c>...).</summary>
    public sealed class ConsoleLogFormatter : IConsoleLogFormatter
    {
        private static readonly Regex RichTextRegex = new("<.*?>", RegexOptions.Compiled);

        private readonly bool stripRichTextTags;

        public ConsoleLogFormatter(bool stripRichTextTags = false)
        {
            this.stripRichTextTags = stripRichTextTags;
        }

        public string Format(string message)
        {
            if (string.IsNullOrEmpty(message) || !stripRichTextTags) return message ?? string.Empty;
            return RichTextRegex.Replace(message, string.Empty);
        }
    }
}
