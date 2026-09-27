using EldritchGames.EldritchLogger.Console.Parsing;

namespace EldritchGames.EldritchLogger.Console.Core
{
    /// <summary>
    /// Provides metadata common to all console commands, including name, description, and expected arguments.
    /// </summary>
    public interface ICommandMetadata
    {
        /// <summary>
        /// Gets the unique name of the command.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the human-readable description of the command.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the specification of expected arguments for the command.
        /// </summary>
        ArgSpec[] ExpectedArgs { get; }
    }
}