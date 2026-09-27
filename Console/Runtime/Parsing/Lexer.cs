using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Console.Parsing
{
    /// <summary>
    /// Converts raw console input text into a sequence of tokens.
    /// </summary>
    /// <remarks>
    /// The <see cref="Lexer"/> reads characters from a <see cref="CharacterStream"/>,
    /// builds tokens using a <see cref="TokenBuilder"/>, and produces a <see cref="TokenList"/>.
    /// The first token is always marked as a <see cref="TokenType.Command"/>.
    /// </remarks>
    public class Lexer
    {
        private readonly TokenBuilder _builder = new TokenBuilder();

        /// <summary>
        /// Tokenizes the specified input string into a list of tokens.
        /// </summary>
        /// <param name="input">The raw console input string.</param>
        /// <returns>
        /// A <see cref="TokenList"/> containing the parsed tokens.
        /// The first token is always treated as the command name.
        /// </returns>
        /// <remarks>
        /// Whitespace is skipped between tokens. A safeguard counter prevents
        /// infinite loops if input length is exceeded.
        /// </remarks>
        public TokenList Tokenize(string input)
        {
            input ??= string.Empty;
            var tokens = new List<Token>();
            var stream = new CharacterStream(input);

            int safetyCounter = 0;
            stream.SkipWhitespace();

            while (stream.HasNext)
            {
                if (++safetyCounter > input.Length * 2) break; // safeguard

                var token = _builder.BuildToken(stream);
                tokens.Add(token);
                stream.SkipWhitespace();
            }

            if (tokens.Count > 0)
                tokens[0] = new Token(TokenType.Command, tokens[0].Value);

            return new TokenList(tokens);
        }
    }
}
