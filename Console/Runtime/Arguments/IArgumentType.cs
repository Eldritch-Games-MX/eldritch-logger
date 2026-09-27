using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EldritchGames.EldritchLogger.Console.Arguments
{
    /// <summary>
    /// Parses, describes and suggests values for one kind of command argument.
    /// Implement it to add new argument types without touching the binder, help or autocomplete.
    /// </summary>
    public interface IArgumentType
    {
        /// <summary>Short name shown in usage strings, e.g. <c>int</c>.</summary>
        string Name { get; }

        bool TryParse(string raw, out object value, out string error);

        /// <summary>Candidate values starting with <paramref name="prefix"/> (may be empty).</summary>
        IEnumerable<string> Suggest(string prefix);
    }

    /// <summary>Built-in argument types.</summary>
    public static class ArgumentTypes
    {
        public static readonly IArgumentType String = new StringArgumentType();
        public static readonly IArgumentType Int = new IntArgumentType("int", int.MinValue);
        public static readonly IArgumentType PositiveInt = new IntArgumentType("int+", 1);
        public static readonly IArgumentType NonNegativeInt = new IntArgumentType("int", 0);
        public static readonly IArgumentType Float = new FloatArgumentType();
        public static readonly IArgumentType Bool = new ChoiceArgumentType("bool", () => new[] { "true", "false" });

        /// <summary>One of a dynamic set of values (e.g. theme names). Comparison is case-insensitive.</summary>
        public static IArgumentType Choice(string name, Func<IEnumerable<string>> values) => new ChoiceArgumentType(name, values);

        /// <summary>A value of enum <typeparamref name="T"/> by name (case-insensitive).</summary>
        public static IArgumentType Enum<T>() where T : struct, System.Enum => new EnumArgumentType<T>();
    }

    internal sealed class StringArgumentType : IArgumentType
    {
        public string Name => "string";

        public bool TryParse(string raw, out object value, out string error)
        {
            value = raw ?? string.Empty;
            error = null;
            return true;
        }

        public IEnumerable<string> Suggest(string prefix) => Enumerable.Empty<string>();
    }

    internal sealed class IntArgumentType : IArgumentType
    {
        private readonly int minimum;

        public string Name { get; }

        public IntArgumentType(string name, int minimum)
        {
            Name = name;
            this.minimum = minimum;
        }

        public bool TryParse(string raw, out object value, out string error)
        {
            value = null;
            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                error = $"'{raw}' is not a whole number.";
                return false;
            }
            if (parsed < minimum)
            {
                error = minimum == 1 ? $"'{raw}' must be positive." : $"'{raw}' must be at least {minimum}.";
                return false;
            }

            value = parsed;
            error = null;
            return true;
        }

        public IEnumerable<string> Suggest(string prefix) => Enumerable.Empty<string>();
    }

    internal sealed class FloatArgumentType : IArgumentType
    {
        public string Name => "float";

        public bool TryParse(string raw, out object value, out string error)
        {
            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                value = parsed;
                error = null;
                return true;
            }
            value = null;
            error = $"'{raw}' is not a number.";
            return false;
        }

        public IEnumerable<string> Suggest(string prefix) => Enumerable.Empty<string>();
    }

    internal sealed class ChoiceArgumentType : IArgumentType
    {
        private readonly Func<IEnumerable<string>> values;

        public string Name { get; }

        public ChoiceArgumentType(string name, Func<IEnumerable<string>> values)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            this.values = values ?? throw new ArgumentNullException(nameof(values));
        }

        public bool TryParse(string raw, out object value, out string error)
        {
            var match = values().FirstOrDefault(v => string.Equals(v, raw, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                value = match;
                error = null;
                return true;
            }
            value = null;
            error = $"'{raw}' is not a valid {Name}. Expected one of: {string.Join(", ", values())}.";
            return false;
        }

        public IEnumerable<string> Suggest(string prefix) =>
            values().Where(v => v.StartsWith(prefix ?? string.Empty, StringComparison.OrdinalIgnoreCase));
    }

    internal sealed class EnumArgumentType<T> : IArgumentType where T : struct, Enum
    {
        public string Name => typeof(T).Name;

        public bool TryParse(string raw, out object value, out string error)
        {
            if (System.Enum.TryParse<T>(raw, true, out var parsed) && System.Enum.IsDefined(typeof(T), parsed))
            {
                value = parsed;
                error = null;
                return true;
            }
            value = null;
            error = $"'{raw}' is not a valid {Name}. Expected one of: {string.Join(", ", System.Enum.GetNames(typeof(T)))}.";
            return false;
        }

        public IEnumerable<string> Suggest(string prefix) =>
            System.Enum.GetNames(typeof(T)).Where(n => n.StartsWith(prefix ?? string.Empty, StringComparison.OrdinalIgnoreCase));
    }
}
