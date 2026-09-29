namespace EldritchGames.EldritchLogger.Console.Parsing
{
    /// <summary>
    /// Provides a simple character stream abstraction for parsing console input.
    /// </summary>
    /// <remarks>
    /// The <see cref="CharacterStream"/> allows sequential access to characters
    /// in an input string, with support for peeking, consuming, and skipping whitespace.
    /// It is typically used by the lexer and parser to tokenize user input.
    /// </remarks>
    public class CharacterStream
    {
        private readonly string _input;
        private int _position;

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterStream"/> class.
        /// </summary>
        /// <param name="input">The input string to parse. If null, an empty string is used.</param>
        public CharacterStream(string input)
        {
            _input = input ?? string.Empty;
            _position = 0;
        }

        /// <summary>
        /// Gets a value indicating whether there are more characters available in the stream.
        /// </summary>
        public bool HasNext => _position < _input.Length;

        /// <summary>
        /// Returns the current character without advancing the stream position.
        /// </summary>
        /// <returns>
        /// The current character if available; otherwise the null character (<c>'\0'</c>).
        /// </returns>
        public char Peek() => HasNext ? _input[_position] : '\0';

        /// <summary>
        /// Returns the current character and advances the stream position.
        /// </summary>
        /// <returns>
        /// The current character if available; otherwise the null character (<c>'\0'</c>).
        /// </returns>
        public char Next() => HasNext ? _input[_position++] : '\0';

        /// <summary>
        /// Advances the stream position past any whitespace characters.
        /// </summary>
        /// <remarks>
        /// Includes a safeguard counter to prevent infinite loops if input length is exceeded.
        /// </remarks>
        public void SkipWhitespace()
        {
            int safetyCounter = 0;
            while (HasNext && char.IsWhiteSpace(Peek()))
            {
                _position++;
                if (++safetyCounter > _input.Length) break; // safeguard
            }
        }

        /// <summary>
        /// Determines whether the specified character is a quote character.
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns><c>true</c> if the character is a double quote (<c>"</c>); otherwise <c>false</c>.</returns>
        public bool IsQuote(char c) => c == '"';
    }
}
