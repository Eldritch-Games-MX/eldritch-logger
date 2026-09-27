using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Console.Core
{
    /// <summary>
    /// Represents a console command with advanced execution semantics,
    /// typically supporting both positional arguments and named flags.
    /// </summary>
    public interface IAdvancedConsoleCommand : ICommandMetadata
    {
        /// <summary>
        /// Executes the advanced command with the provided arguments and flags.
        /// </summary>
        /// <param name="args">A list of positional arguments.</param>
        /// <param name="flags">A dictionary of flag names and their values.</param>
        void Execute(List<string> args, Dictionary<string, string> flags);

    }
}