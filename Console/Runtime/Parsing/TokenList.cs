using System.Collections.Generic;
using System.Linq;

namespace EldritchGames.EldritchLogger.Console.Parsing
{
    public class TokenList
    {
        private readonly List<Token> _tokens;
        private int _position;

        public TokenList(List<Token> tokens)
        {
            _tokens = tokens;
            _position = 0;
        }

        public bool HasNext => _position < _tokens.Count;
        public int Count => _tokens.Count;

        public Token Peek() => _tokens[_position];
        public Token Next() => _tokens[_position++];

        public void Reset() => _position = 0;
        public IEnumerable<Token> AllTokens => _tokens;

        public override string ToString()
        {
            return string.Join(" ", _tokens.Select(t => $"{t.Type}:{t.Value}"));
        }
    }
}
