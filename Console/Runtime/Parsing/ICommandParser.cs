namespace EldritchGames.EldritchLogger.Console.Parsing
{
    /// <summary>
    /// Defines the contract for parsing raw console input into structured commands.
    /// </summary>
    public interface ICommandParser
    {
        /// <summary>
        /// Parses a list of tokens and raw input into a structured parse result.
        /// </summary>
        /// <param name="tokens">The tokenized representation of the input.</param>
        /// <param name="rawInput">The original raw input string.</param>
        /// <returns>A structured parse result containing the command and arguments.</returns>
        ParseResult Parse(TokenList tokens, string rawInput);
    }

}
