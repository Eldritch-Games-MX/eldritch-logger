namespace EldritchGames.EldritchLogger.Console.Parsing
{
    /// <summary>
    /// A lexical token. For <see cref="TokenType.Flag"/> the value excludes the leading dashes
    /// and may contain an inline value (<c>limit=5</c>).
    /// </summary>
    public class Token
    {
        public TokenType Type { get; }

        public string Value { get; }

        public Token(TokenType type, string value)
        {
            Type = type;
            Value = value ?? string.Empty;
        }

        /// <summary>Renders the token back to command-line text (quotes and dashes restored).</summary>
        public string ToSourceText() => Type switch
        {
            TokenType.Flag => "--" + Value,
            TokenType.StringLiteral => "\"" + Value + "\"",
            _ => Value
        };

        public override string ToString() => $"{Type}:{Value}";
    }
}
