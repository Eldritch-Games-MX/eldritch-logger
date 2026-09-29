using System.Text;

namespace EldritchGames.EldritchLogger.Console.Parsing
{
    /// <summary>
    /// Builds tokens from a <see cref="CharacterStream"/> during lexical analysis.
    /// </summary>
    /// <remarks>
    /// The <see cref="TokenBuilder"/> reads characters sequentially, handling quoted strings,
    /// whitespace separation, and classification into arguments or flags.
    /// </remarks>
    public class TokenBuilder
    {
        /// <summary>
        /// Constructs a single token from the provided character stream.
        /// </summary>
        /// <param name="stream">The character stream to read from.</param>
        /// <returns>
        /// A <see cref="Token"/> representing the next unit of input.
        /// Quoted values are returned as <see cref="TokenType.StringLiteral"/>,
        /// flags are classified by prefix (<c>-</c> or <c>--</c>),
        /// and other values are treated as arguments.
        /// </returns>
        public Token BuildToken(CharacterStream stream)
        {
            var sb = new StringBuilder();
            bool inQuotes = false;
            bool wasQuoted = false;
            int safetyCounter = 0;

            while (stream.HasNext)
            {
                if (++safetyCounter > 10000) break; // safeguard

                var c = stream.Peek();

                if (stream.IsQuote(c))
                {
                    inQuotes = !inQuotes;
                    wasQuoted = true;
                    stream.Next(); // consume quote
                    continue;
                }

                if (char.IsWhiteSpace(c) && !inQuotes)
                    break;

                sb.Append(stream.Next());
            }

            var value = sb.ToString();

            if (wasQuoted)
                return new Token(TokenType.StringLiteral, value);

            if (string.IsNullOrEmpty(value))
                return new Token(TokenType.Argument, string.Empty);

            return Classify(value);
        }

        /// <summary>
        /// Classifies a raw string value into a specific token type.
        /// </summary>
        /// <param name="value">The raw string value to classify.</param>
        /// <returns>
        /// A <see cref="Token"/> with type <see cref="TokenType.Flag"/> if the value
        /// starts with <c>-</c> or <c>--</c>, otherwise an <see cref="TokenType.Argument"/>.
        /// </returns>
        private Token Classify(string value)
        {
            if (value.Length > 2 && value.StartsWith("--")) return new Token(TokenType.Flag, value.Substring(2));

            // "-v" is a flag, but "-5" / "-0.5" are negative numbers and "-" is a plain argument.
            if (value.Length > 1 && value[0] == '-' && !char.IsDigit(value[1]) && value[1] != '.')
                return new Token(TokenType.Flag, value.Substring(1));

            return new Token(TokenType.Argument, value);
        }
    }
}
