using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Registry;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Parsing
{
    public class CommandExecutor : ICommandExecutor
    {
        private readonly ICommandRegistry _registry;
        private readonly CommandHistory _history;

        public CommandExecutor(ICommandRegistry registry, CommandHistory history)
        {
            _registry = registry;
            _history = history;
        }

        public void Execute(ParseResult result)
        {
            if (!result.Success)
            {
                Debug.LogWarning($"Parse error: {result.ErrorMessage}");
                return;
            }

            var parsed = result.Command;

            // Record the raw input into history
            _history.Add(parsed.RawText);

            // First check advanced commands
            if (_registry.TryGetAdvancedCommand(parsed.Name, out var advancedCommand))
            {
                if (!ArgValidator.Validate(advancedCommand, parsed.Arguments.ToArray(), out var error))
                {
                    Debug.LogWarning(error);
                    return;
                }

                advancedCommand.Execute(parsed.Arguments, parsed.Flags);
                return;
            }

            // Fallback to simple commands
            if (_registry.TryGetCommand(parsed.Name, out var command))
            {
                if (!ArgValidator.Validate(command, parsed.Arguments.ToArray(), out var error))
                {
                    Debug.LogWarning(error);
                    return;
                }

                command.Execute(parsed.Arguments.ToArray());
                return;
            }

            Debug.LogWarning($"Unknown command: {parsed.Name}");
        }
    }
}
