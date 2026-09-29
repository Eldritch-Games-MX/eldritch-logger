using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Registry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EldritchGames.EldritchLogger.Console.Autocompletion
{
    public interface IAutocompleteProvider
    {
        /// <summary>
        /// Completions for the token being typed at the end of <paramref name="input"/>
        /// (command names, flag names, or argument values), best match first.
        /// </summary>
        IEnumerable<string> Suggest(string input);
    }

    /// <summary>
    /// Suggests command names and aliases for the first token, then flags (<c>--x</c>) and
    /// argument values using each parameter's <c>IArgumentType.Suggest</c>.
    /// </summary>
    public sealed class AutocompleteProvider : IAutocompleteProvider
    {
        private readonly ICommandRegistry registry;

        public AutocompleteProvider(ICommandRegistry registry)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public IEnumerable<string> Suggest(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return Enumerable.Empty<string>();

            bool endsWithSpace = char.IsWhiteSpace(input[input.Length - 1]);
            var tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string current = endsWithSpace ? string.Empty : tokens[tokens.Length - 1];
            int completedTokens = endsWithSpace ? tokens.Length : tokens.Length - 1;

            if (completedTokens == 0)
                return SuggestCommands(current);

            if (!registry.TryGet(tokens[0], out var command))
                return Enumerable.Empty<string>();

            var descriptor = command.Descriptor;

            if (current.StartsWith("-"))
            {
                var flagPrefix = current.TrimStart('-');
                return descriptor.Flags
                    .Where(f => f.Name.StartsWith(flagPrefix, StringComparison.OrdinalIgnoreCase))
                    .Select(f => f.IsSwitch ? $"--{f.Name}" : $"--{f.Name}=");
            }

            // Position among positional arguments typed so far (flags and their values excluded).
            int position = 0;
            for (int i = 1; i < completedTokens; i++)
            {
                var token = tokens[i];
                if (token.StartsWith("--") || (token.StartsWith("-") && token.Length > 1 && !char.IsDigit(token[1])))
                {
                    var flag = descriptor.FindFlag(token.TrimStart('-').Split('=')[0]);
                    if (flag != null && !flag.IsSwitch && !token.Contains("=")) i++; // skip its value
                    continue;
                }
                position++;
            }

            if (descriptor.Parameters.Count == 0) return Enumerable.Empty<string>();

            var parameter = descriptor.Parameters[Math.Min(position, descriptor.Parameters.Count - 1)];
            if (position >= descriptor.Parameters.Count && parameter.Kind == ParameterKind.Single)
                return Enumerable.Empty<string>();

            if (parameter.Kind == ParameterKind.Remainder)
                return position == descriptor.Parameters.Count - 1 ? SuggestCommands(current) : Enumerable.Empty<string>();

            return parameter.Type.Suggest(current);
        }

        private IEnumerable<string> SuggestCommands(string prefix) =>
            registry.All
                .SelectMany(c => new[] { c.Descriptor.Name }.Concat(c.Descriptor.Aliases))
                .Where(n => n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(n => n.Length)
                .ThenBy(n => n, StringComparer.OrdinalIgnoreCase);
    }
}
