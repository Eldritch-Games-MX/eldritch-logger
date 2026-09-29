using EldritchGames.EldritchLogger.Console.Commands;
using System;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Console.Registry
{
    public interface ICommandRegistry
    {
        /// <summary>Registers a command under its name and aliases. Replaces an existing command with the same name.</summary>
        void Register(ICommand command);

        /// <summary>Removes a command (by name or alias) and all its aliases.</summary>
        bool Unregister(string nameOrAlias);

        bool TryGet(string nameOrAlias, out ICommand command);

        /// <summary>Distinct registered commands, in registration order.</summary>
        IReadOnlyList<ICommand> All { get; }
    }

    /// <summary>Case-insensitive command registry with alias support.</summary>
    public sealed class CommandRegistry : ICommandRegistry
    {
        private readonly Dictionary<string, ICommand> byName = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<ICommand> commands = new();

        public IReadOnlyList<ICommand> All => commands;

        public void Register(ICommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            var descriptor = command.Descriptor ?? throw new ArgumentException("Command has no descriptor.", nameof(command));

            Unregister(descriptor.Name);
            foreach (var alias in descriptor.Aliases)
                if (byName.TryGetValue(alias, out var existing) && !ReferenceEquals(existing, command))
                    throw new InvalidOperationException(
                        $"Alias '{alias}' of '{descriptor.Name}' is already used by '{existing.Descriptor.Name}'.");

            commands.Add(command);
            byName[descriptor.Name] = command;
            foreach (var alias in descriptor.Aliases)
                byName[alias] = command;
        }

        public bool Unregister(string nameOrAlias)
        {
            if (nameOrAlias == null || !byName.TryGetValue(nameOrAlias, out var command)) return false;

            commands.Remove(command);
            byName.Remove(command.Descriptor.Name);
            foreach (var alias in command.Descriptor.Aliases)
                if (byName.TryGetValue(alias, out var owner) && ReferenceEquals(owner, command))
                    byName.Remove(alias);
            return true;
        }

        public bool TryGet(string nameOrAlias, out ICommand command)
        {
            command = null;
            return nameOrAlias != null && byName.TryGetValue(nameOrAlias, out command);
        }
    }
}
