namespace EldritchGames.EldritchLogger.Console.Parsing
{
    /// <summary>
    /// Represents a single lexical token produced by the <see cref="Lexer"/>.
    /// </summary>
    /// <remarks>
    /// A <see cref="Token"/> contains both its type (such as command, argument, or flag)
    /// and its string value. Tokens are used by the parser to construct a <see cref="Domain.ParsedCommand"/>.
    /// </remarks>
    public class Token
    {
        /// <summary>
        /// Gets the type of the token.
        /// </summary>
        /// <remarks>
        /// The <see cref="TokenType"/> indicates the role of the token in the command input,
        /// such as <c>Command</c>, <c>Argument</c>, or <c>Flag</c>.
        /// </remarks>
        public TokenType Type { get; }

        /// <summary>
        /// Gets the raw string value of the token.
        /// </summary>
        /// <remarks>
        /// This is the exact substring extracted from the input, such as the command name,
        /// argument text, or flag identifier.
        /// </remarks>
        public string Value { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Token"/> class.
        /// </summary>
        /// <param name="type">The type of the token.</param>
        /// <param name="value">The raw string value of the token.</param>
        public Token(TokenType type, string value)
        {
            Type = type;
            Value = value;
        }
    }
}
