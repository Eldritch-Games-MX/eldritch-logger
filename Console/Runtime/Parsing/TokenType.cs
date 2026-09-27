namespace EldritchGames.EldritchLogger.Console.Parsing
{
    /// <summary>
    /// Defines the possible types of tokens produced by the <see cref="Lexer"/>.
    /// </summary>
    /// <remarks>
    /// Token types classify segments of console input into their roles,
    /// such as command names, arguments, flags, or string literals.
    /// </remarks>
    public enum TokenType
    {
        /// <summary>
        /// Represents the command name token.
        /// </summary>
        /// <remarks>
        /// The first token in a parsed input is always marked as <c>Command</c>.
        /// </remarks>
        Command,

        /// <summary>
        /// Represents a flag token, typically prefixed with <c>-</c> or <c>--</c>.
        /// </summary>
        /// <remarks>
        /// Flags are used to modify command behavior, e.g., <c>--verbose</c>.
        /// </remarks>
        Flag,

        /// <summary>
        /// Represents a positional argument token.
        /// </summary>
        /// <remarks>
        /// Arguments are values passed to commands, such as filenames or numbers.
        /// </remarks>
        Argument,

        /// <summary>
        /// Represents a string literal token enclosed in quotes.
        /// </summary>
        /// <remarks>
        /// String literals preserve whitespace and special characters inside quotes.
        /// </remarks>
        StringLiteral
    }
}
