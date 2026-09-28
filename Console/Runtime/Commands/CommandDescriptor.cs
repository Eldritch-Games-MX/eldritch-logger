using EldritchGames.EldritchLogger.Console.Arguments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EldritchGames.EldritchLogger.Console.Commands
{
    /// <summary>
    /// Describes a command: its name, aliases, help text and the arguments and flags it accepts.
    /// The binder, <c>help</c> and autocomplete all work from this description.
    /// </summary>
    public sealed class CommandDescriptor
    {
        public string Name { get; }
        public string Description { get; }
        public IReadOnlyList<string> Aliases { get; }
        public IReadOnlyList<ParameterSpec> Parameters { get; }
        public IReadOnlyList<FlagSpec> Flags { get; }

        /// <summary>Cheat commands only run when the console's <c>ICheatPolicy</c> allows cheats.</summary>
        public bool IsCheat { get; }

        public CommandDescriptor(string name,
                                 string description,
                                 IEnumerable<ParameterSpec> parameters = null,
                                 IEnumerable<FlagSpec> flags = null,
                                 IEnumerable<string> aliases = null,
                                 bool isCheat = false)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Any(char.IsWhiteSpace))
                throw new ArgumentException("Command name must be a single non-empty word.", nameof(name));

            Name = name;
            Description = description ?? string.Empty;
            Parameters = parameters?.ToArray() ?? Array.Empty<ParameterSpec>();
            Flags = flags?.ToArray() ?? Array.Empty<FlagSpec>();
            Aliases = aliases?.ToArray() ?? Array.Empty<string>();
            IsCheat = isCheat;

            for (int i = 0; i < Parameters.Count - 1; i++)
                if (Parameters[i].Kind != ParameterKind.Single)
                    throw new ArgumentException($"Only the last parameter of '{name}' may be variadic or a remainder.");

            bool seenOptional = false;
            foreach (var p in Parameters)
            {
                if (!p.IsRequired) seenOptional = true;
                else if (seenOptional)
                    throw new ArgumentException($"Required parameter '{p.Name}' of '{name}' follows an optional one.");
            }
        }

        public FlagSpec FindFlag(string name) =>
            Flags.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));

        /// <summary>Usage line, e.g. <c>repeat &lt;count:int+&gt; &lt;command...&gt; [--silent] [--delay=&lt;int&gt;]</c>.</summary>
        public string Usage
        {
            get
            {
                var sb = new StringBuilder(Name);
                foreach (var p in Parameters)
                    sb.Append(' ').Append(p.Usage);
                foreach (var f in Flags)
                    sb.Append(' ').Append(f.Usage);
                return sb.ToString();
            }
        }
    }

    public enum ParameterKind
    {
        /// <summary>Exactly one token.</summary>
        Single,
        /// <summary>All remaining positional tokens, each parsed with the parameter type.</summary>
        Variadic,
        /// <summary>
        /// All remaining tokens verbatim, flags included (e.g. a nested command line).
        /// Flags after this parameter belong to it, not to the command.
        /// </summary>
        Remainder
    }

    public sealed class ParameterSpec
    {
        public string Name { get; }
        public IArgumentType Type { get; }
        public bool IsRequired { get; }
        public ParameterKind Kind { get; }
        public string Description { get; }

        private ParameterSpec(string name, IArgumentType type, bool required, ParameterKind kind, string description)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Parameter name is required.", nameof(name));
            Name = name;
            Type = type ?? ArgumentTypes.String;
            IsRequired = required;
            Kind = kind;
            Description = description ?? string.Empty;
        }

        public static ParameterSpec Required(string name, IArgumentType type, string description = null) =>
            new(name, type, true, ParameterKind.Single, description);

        public static ParameterSpec Optional(string name, IArgumentType type, string description = null) =>
            new(name, type, false, ParameterKind.Single, description);

        public static ParameterSpec Variadic(string name, IArgumentType type, bool required = false, string description = null) =>
            new(name, type, required, ParameterKind.Variadic, description);

        public static ParameterSpec Remainder(string name, bool required = true, string description = null) =>
            new(name, ArgumentTypes.String, required, ParameterKind.Remainder, description);

        public string Usage
        {
            get
            {
                string token = Kind switch
                {
                    ParameterKind.Single => $"<{Name}:{Type.Name}>",
                    ParameterKind.Variadic => $"<{Name}:{Type.Name}...>",
                    _ => $"<{Name}...>"
                };
                return IsRequired ? token : $"[{token}]";
            }
        }
    }

    public sealed class FlagSpec
    {
        public string Name { get; }

        /// <summary>Type of the flag's value, or null for a boolean switch.</summary>
        public IArgumentType ValueType { get; }

        public bool IsRequired { get; }
        public string Description { get; }

        public bool IsSwitch => ValueType == null;

        private FlagSpec(string name, IArgumentType valueType, bool required, string description)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Flag name is required.", nameof(name));
            Name = name.TrimStart('-');
            ValueType = valueType;
            IsRequired = required;
            Description = description ?? string.Empty;
        }

        /// <summary>A boolean flag: <c>--silent</c>.</summary>
        public static FlagSpec Switch(string name, string description = null) => new(name, null, false, description);

        /// <summary>A flag with a value: <c>--limit=5</c> or <c>--limit 5</c>.</summary>
        public static FlagSpec WithValue(string name, IArgumentType type, bool required = false, string description = null) =>
            new(name, type ?? ArgumentTypes.String, required, description);

        public string Usage
        {
            get
            {
                string token = IsSwitch ? $"--{Name}" : $"--{Name}=<{ValueType.Name}>";
                return IsRequired ? token : $"[{token}]";
            }
        }
    }
}
