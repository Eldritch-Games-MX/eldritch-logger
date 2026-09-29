using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Console.Parsing
{
    /// <summary>Turns lexer output into a <see cref="ParsedCommand"/>.</summary>
    public interface ICommandParser
    {
        ParseResult Parse(TokenList tokens, string rawInput);
    }

    public sealed class CommandParser : ICommandParser
    {
        public ParseResult Parse(TokenList tokens, string rawInput)
        {
            if (tokens == null || !tokens.HasNext)
                return ParseResult.Fail("No command entered.");

            var commandToken = tokens.Next();
            if (commandToken.Type != TokenType.Command || string.IsNullOrWhiteSpace(commandToken.Value))
                return ParseResult.Fail("Expected a command at the start.");

            var arguments = new List<Token>();
            while (tokens.HasNext)
                arguments.Add(tokens.Next());

            return ParseResult.Ok(new ParsedCommand(commandToken.Value, arguments, rawInput));
        }
    }
}
