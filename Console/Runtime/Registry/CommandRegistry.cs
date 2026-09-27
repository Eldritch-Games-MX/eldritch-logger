using EldritchGames.EldritchLogger.Console.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EldritchGames.EldritchLogger.Console.Registry
{
    public class CommandRegistry : ICommandRegistry
    {
        private readonly Dictionary<string, IConsoleCommand> _commands;
        private readonly Dictionary<string, IAdvancedConsoleCommand> _advancedCommands;

        public CommandRegistry()
        {
            _commands = new Dictionary<string, IConsoleCommand>(StringComparer.OrdinalIgnoreCase);
            _advancedCommands = new Dictionary<string, IAdvancedConsoleCommand>(StringComparer.OrdinalIgnoreCase);
        }
        public void Register(IConsoleCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            _commands[command.Name] = command;
        }

        public bool TryGetCommand(string name, out IConsoleCommand command)
            => _commands.TryGetValue(name, out command);

        public IEnumerable<IConsoleCommand> GetAllCommands() => _commands.Values;
        public void Register(IAdvancedConsoleCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            _advancedCommands[command.Name] = command;
        }

        public bool TryGetAdvancedCommand(string name, out IAdvancedConsoleCommand command)
            => _advancedCommands.TryGetValue(name, out command);

        public IEnumerable<IAdvancedConsoleCommand> GetAllAdvancedCommands() => _advancedCommands.Values;

        public IEnumerable<ICommandMetadata> GetAllMetadata()
        {
            return _commands.Values.Cast<ICommandMetadata>()
                .Concat(_advancedCommands.Values.Cast<ICommandMetadata>());
        }
    }
}