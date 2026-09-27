namespace EldritchGames.EldritchLogger.Console.Core
{
    /// <summary>
    /// Represents a basic console command that accepts only positional arguments.
    /// </summary>
    public interface IConsoleCommand : ICommandMetadata
    {
        /// <summary>
        /// Executes the command with the provided arguments.
        /// </summary>
        /// <param name="args">An array of arguments passed to the command.</param>
        void Execute(string[] args);
    }
}

