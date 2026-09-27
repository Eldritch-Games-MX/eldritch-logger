using EldritchGames.EldritchLogger.Console.Core;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Console.Registry
{
    /// <summary>
    /// Provides registration and lookup for console commands.
    /// </summary>
    public interface ICommandRegistry
    {
        /// <summary>
        /// Registers a basic console command.
        /// </summary>
        void Register(IConsoleCommand command);

        /// <summary>
        /// Attempts to retrieve a registered basic command by name.
        /// </summary>
        bool TryGetCommand(string name, out IConsoleCommand command);

        /// <summary>
        /// Gets all registered basic commands.
        /// </summary>
        IEnumerable<IConsoleCommand> GetAllCommands();

        /// <summary>
        /// Registers an advanced console command.
        /// </summary>
        void Register(IAdvancedConsoleCommand command);

        /// <summary>
        /// Attempts to retrieve a registered advanced command by name.
        /// </summary>
        bool TryGetAdvancedCommand(string name, out IAdvancedConsoleCommand command);

        /// <summary>
        /// Gets all registered advanced commands.
        /// </summary>
        IEnumerable<IAdvancedConsoleCommand> GetAllAdvancedCommands();

        /// <summary>
        /// Gets metadata for all registered commands (basic and advanced).
        /// </summary>
        IEnumerable<ICommandMetadata> GetAllMetadata();
    }

}
