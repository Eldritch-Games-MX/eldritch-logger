using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace EldritchGames.EldritchLogger.Core
{
    /// <summary>
    /// Structured message templates: <c>"Player {Name} took {Damage:0.0} damage"</c>.
    /// Holes are filled from arguments in order, and each hole becomes a property of the entry,
    /// so entries can be searched and grouped by value instead of by parsing text.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><c>{{</c> and <c>}}</c> are literal braces.</item>
    /// <item><c>{Name:format}</c> applies a .NET format string (invariant culture) to the rendered text;
    /// the property keeps the original value.</item>
    /// <item>Missing arguments leave the hole as written; extra arguments are ignored.</item>
    /// <item>Rendering never throws: an invalid format is ignored, and a value whose <c>ToString()</c> throws
    /// is shown as its type name.</item>
    /// </list>
    /// </remarks>
    public sealed class MessageTemplate
    {
        private const int MaxCachedTemplates = 1000;
        private static readonly ConcurrentDictionary<string, MessageTemplate> Cache = new();
        private static int cacheCount;

        private readonly Token[] tokens;

        public string Text { get; }

        /// <summary>Hole names, in order of appearance.</summary>
        public IReadOnlyList<string> PropertyNames { get; }

        private readonly struct Token
        {
            public readonly string Literal;   // non-null for text
            public readonly string Name;      // non-null for holes
            public readonly string Format;
            public readonly string Raw;       // the hole as written, e.g. "{Name:0.0}"

            public Token(string literal) : this() => Literal = literal;

            public Token(string name, string format, string raw)
            {
                Literal = null;
                Name = name;
                Format = format;
                Raw = raw;
            }
        }

        private MessageTemplate(string text, Token[] tokens)
        {
            Text = text;
            this.tokens = tokens;
            var names = new List<string>();
            foreach (var t in tokens)
                if (t.Name != null) names.Add(t.Name);
            PropertyNames = names;
        }

        /// <summary>Templates currently cached (never more than the cache limit).</summary>
        internal static int CacheCount => Volatile.Read(ref cacheCount);

        /// <summary>
        /// Parses (and caches) <paramref name="template"/>. When the cache is full it is emptied and refilled
        /// by the templates in use, so one-off templates cannot crowd out the frequent ones for good.
        /// </summary>
        public static MessageTemplate Parse(string template)
        {
            template ??= string.Empty;
            if (Cache.TryGetValue(template, out var cached)) return cached;

            var parsed = new MessageTemplate(template, Tokenize(template));
            if (Cache.TryAdd(template, parsed) && Interlocked.Increment(ref cacheCount) > MaxCachedTemplates)
            {
                lock (Cache)
                {
                    if (Volatile.Read(ref cacheCount) > MaxCachedTemplates)
                    {
                        Cache.Clear();
                        Volatile.Write(ref cacheCount, 0);
                    }
                }
            }
            return parsed;
        }

        /// <summary>Renders the message and adds one property per filled hole to <paramref name="properties"/>.</summary>
        public string Render(object[] args, IDictionary<string, object> properties)
        {
            args ??= Array.Empty<object>();
            var sb = new StringBuilder(Text.Length + 16 * args.Length);
            int argIndex = 0;

            foreach (var token in tokens)
            {
                if (token.Literal != null)
                {
                    sb.Append(token.Literal);
                    continue;
                }

                if (argIndex >= args.Length)
                {
                    sb.Append(token.Raw);
                    continue;
                }

                var value = args[argIndex++];
                if (properties != null) properties[token.Name] = value;
                sb.Append(FormatValue(value, token.Format));
            }

            return sb.ToString();
        }

        private static string FormatValue(object value, string format) =>
            value == null ? "null" : LogValues.Format(value, format);

        private static Token[] Tokenize(string text)
        {
            var tokens = new List<Token>();
            var literal = new StringBuilder();
            var usedNames = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '{' && i + 1 < text.Length && text[i + 1] == '{') { literal.Append('{'); i++; continue; }
                if (c == '}' && i + 1 < text.Length && text[i + 1] == '}') { literal.Append('}'); i++; continue; }

                if (c == '{')
                {
                    int close = text.IndexOf('}', i + 1);
                    if (close < 0) { literal.Append(text, i, text.Length - i); break; }

                    var inner = text.Substring(i + 1, close - i - 1);
                    int colon = inner.IndexOf(':');
                    var name = (colon >= 0 ? inner.Substring(0, colon) : inner).Trim().TrimStart('@', '$');
                    var format = colon >= 0 ? inner.Substring(colon + 1) : null;

                    if (!IsValidName(name))
                    {
                        literal.Append(text, i, close - i + 1);
                    }
                    else
                    {
                        if (literal.Length > 0) { tokens.Add(new Token(literal.ToString())); literal.Clear(); }

                        // Repeated names get a suffix so both values are kept.
                        var unique = name;
                        for (int n = 2; !usedNames.Add(unique); n++) unique = name + "_" + n;
                        tokens.Add(new Token(unique, format, text.Substring(i, close - i + 1)));
                    }
                    i = close;
                    continue;
                }

                literal.Append(c);
            }

            if (literal.Length > 0) tokens.Add(new Token(literal.ToString()));
            return tokens.ToArray();
        }

        private static bool IsValidName(string name)
        {
            if (name.Length == 0) return false;
            foreach (var ch in name)
                if (!char.IsLetterOrDigit(ch) && ch != '_') return false;
            return true;
        }
    }
}
