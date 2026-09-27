using EldritchGames.EldritchLogger.Console.Domain;

namespace EldritchGames.EldritchLogger.Console.Parsing
{
    /// <summary>
    /// Represents the outcome of parsing a console command input.
    /// </summary>
    /// <remarks>
    /// A <see cref="ParseResult"/> indicates whether parsing succeeded,
    /// provides any error messages, and contains the parsed command if successful.
    /// </remarks>
    public class ParseResult
    {
        /// <summary>
        /// Gets a value indicating whether the parsing operation succeeded.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets the error message if parsing failed; otherwise <c>null</c>.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Gets the parsed command if parsing succeeded; otherwise <c>null</c>.
        /// </summary>
        public ParsedCommand Command { get; }

        /// <summary>
        /// Gets the raw text input that was parsed.
        /// </summary>
        public string RawText { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParseResult"/> class.
        /// </summary>
        /// <param name="success">Indicates whether parsing succeeded.</param>
        /// <param name="errorMessage">The error message if parsing failed.</param>
        /// <param name="command">The parsed command if parsing succeeded.</param>
        /// <param name="rawText">The raw text input that was parsed.</param>
        private ParseResult(bool success, string errorMessage, ParsedCommand command, string rawText)
        {
            Success = success;
            ErrorMessage = errorMessage;
            Command = command;
            RawText = rawText;
        }

        /// <summary>
        /// Creates a successful <see cref="ParseResult"/> containing the parsed command.
        /// </summary>
        /// <param name="command">The parsed command.</param>
        /// <param name="rawText">The raw text input that was parsed.</param>
        /// <returns>A successful parse result.</returns>
        public static ParseResult Ok(ParsedCommand command, string rawText)
            => new(true, null, command, rawText);

        /// <summary>
        /// Creates a failed <see cref="ParseResult"/> with the specified error message.
        /// </summary>
        /// <param name="errorMessage">The error message describing why parsing failed.</param>
        /// <returns>A failed parse result.</returns>
        public static ParseResult Fail(string errorMessage)
            => new(false, errorMessage, null, null);
    }
}
