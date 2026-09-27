using EldritchGames.EldritchLogger.Console.Domain;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Console.Parsing
{
    public class CommandParser : ICommandParser
    {
        public ParseResult Parse(TokenList tokens, string rawInput)
        {
            if (!tokens.HasNext)
                return ParseResult.Fail("No command entered.");

            var commandToken = tokens.Next();
            if (commandToken.Type != TokenType.Command)
                return ParseResult.Fail("Expected a command at the start.");

            var name = commandToken.Value;
            var args = new List<string>();
            var flags = new Dictionary<string, string>();

            while (tokens.HasNext)
            {
                var token = tokens.Next();
                switch (token.Type)
                {
                    case TokenType.Argument:
                    case TokenType.StringLiteral:
                        args.Add(token.Value);
                        break;

                    case TokenType.Flag:
                        var parts = token.Value.Split('=', 2);
                        var key = parts[0];
                        var value = parts.Length > 1 ? parts[1] : "true";
                        flags[key] = value;
                        break;

                    default:
                        return ParseResult.Fail($"Unexpected token: {token.Value}");
                }
            }

            var parsedCommand = new ParsedCommand(name, args, flags, rawInput);
            return ParseResult.Ok(parsedCommand, rawInput);
        }

    }

}