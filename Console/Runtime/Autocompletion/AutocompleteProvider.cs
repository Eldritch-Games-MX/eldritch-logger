using System;
using System.Collections.Generic;
using System.Linq;
using EldritchGames.EldritchLogger.Console.Registry;

namespace EldritchGames.EldritchLogger.Console.Autocompletion
{
    public class AutocompleteProvider : IAutocompleteProvider
    {
        private readonly ICommandRegistry registry;

        public AutocompleteProvider(ICommandRegistry registry)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public IEnumerable<string> Suggest(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Enumerable.Empty<string>();

            var tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var current = tokens.Last();

            if (tokens.Length == 1)
            {
                return registry.GetAllMetadata()
                    .Select(c => c.Name)
                    .Where(n => n.StartsWith(current, StringComparison.OrdinalIgnoreCase));
            }

            var command = registry.GetAllMetadata()
                .FirstOrDefault(c => c.Name.Equals(tokens[0], StringComparison.OrdinalIgnoreCase));

            if (command != null)
            {
                return command.ExpectedArgs
                    .Select(spec =>
                        spec.Type == "flag" ? spec.Name :
                        spec.AllowMultiple ? $"<{spec.Name}:{spec.Type}...>" :
                        $"<{spec.Name}:{spec.Type}>")
                    .Where(s => s.StartsWith(current, StringComparison.OrdinalIgnoreCase));
            }

            return Enumerable.Empty<string>();
        }
    }
}
