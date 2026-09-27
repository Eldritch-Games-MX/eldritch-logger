using EldritchGames.EldritchLogger.Console.Parsing;
using System;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Console.Arguments
{
    /// <summary>
    /// Typed, validated arguments for one command invocation, produced by <see cref="ArgumentBinder"/>.
    /// </summary>
    public sealed class CommandArguments
    {
        private readonly Dictionary<string, object> values = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, object> flags = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, IReadOnlyList<Token>> remainders = new(StringComparer.OrdinalIgnoreCase);

        public static readonly CommandArguments Empty = new();

        internal void SetValue(string name, object value) => values[name] = value;
        internal void SetFlag(string name, object value) => flags[name] = value;
        internal void SetRemainder(string name, IReadOnlyList<Token> tokens) => remainders[name] = tokens;

        public bool Has(string name) => values.ContainsKey(name) || remainders.ContainsKey(name);

        /// <summary>Returns a bound parameter value. Throws if it was not supplied.</summary>
        public T Get<T>(string name)
        {
            if (!values.TryGetValue(name, out var value))
                throw new KeyNotFoundException($"Argument '{name}' was not supplied.");
            return (T)value;
        }

        public T GetOrDefault<T>(string name, T defaultValue = default) =>
            values.TryGetValue(name, out var value) ? (T)value : defaultValue;

        /// <summary>Values of a variadic parameter (empty if none were supplied).</summary>
        public IReadOnlyList<T> GetAll<T>(string name)
        {
            if (!values.TryGetValue(name, out var value)) return Array.Empty<T>();
            var items = (IReadOnlyList<object>)value;
            var result = new T[items.Count];
            for (int i = 0; i < items.Count; i++) result[i] = (T)items[i];
            return result;
        }

        /// <summary>Tokens captured by a remainder parameter (empty if none were supplied).</summary>
        public IReadOnlyList<Token> GetRemainder(string name) =>
            remainders.TryGetValue(name, out var tokens) ? tokens : Array.Empty<Token>();

        public bool HasFlag(string name) => flags.ContainsKey(name.TrimStart('-'));

        public T GetFlag<T>(string name, T defaultValue = default) =>
            flags.TryGetValue(name.TrimStart('-'), out var value) && value is T typed ? typed : defaultValue;
    }
}
