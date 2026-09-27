using System;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Console.Parsing
{
    /// <summary>
    /// A tokenized command line: the command name and the raw argument tokens that follow it.
    /// Tokens are bound to typed arguments later by <c>ArgumentBinder</c>.
    /// </summary>
    public sealed class ParsedCommand
    {
        public string Name { get; }

        /// <summary>Tokens after the command name, in input order (arguments, string literals, flags).</summary>
        public IReadOnlyList<Token> ArgumentTokens { get; }

        /// <summary>The original input line.</summary>
        public string RawText { get; }

        public ParsedCommand(string name, IReadOnlyList<Token> argumentTokens, string rawText)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Command name is required.", nameof(name));
            Name = name;
            ArgumentTokens = argumentTokens ?? Array.Empty<Token>();
            RawText = rawText ?? name;
        }

        /// <summary>
        /// Builds a command from a token sequence whose first token is the command name
        /// (used to re-execute a nested command without re-lexing it).
        /// </summary>
        public static ParsedCommand FromTokens(IReadOnlyList<Token> tokens)
        {
            if (tokens == null || tokens.Count == 0)
                throw new ArgumentException("At least one token (the command name) is required.", nameof(tokens));

            var rest = new Token[tokens.Count - 1];
            for (int i = 1; i < tokens.Count; i++) rest[i - 1] = tokens[i];

            var raw = new List<string>(tokens.Count);
            foreach (var token in tokens) raw.Add(token.ToSourceText());

            return new ParsedCommand(tokens[0].Value, rest, string.Join(" ", raw));
        }
    }

    public sealed class ParseResult
    {
        public bool Success { get; }
        public string ErrorMessage { get; }
        public ParsedCommand Command { get; }

        private ParseResult(bool success, string errorMessage, ParsedCommand command)
        {
            Success = success;
            ErrorMessage = errorMessage;
            Command = command;
        }

        public static ParseResult Ok(ParsedCommand command) =>
            new(true, null, command ?? throw new ArgumentNullException(nameof(command)));

        public static ParseResult Fail(string errorMessage) => new(false, errorMessage, null);
    }
}
