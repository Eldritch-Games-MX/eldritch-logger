namespace EldritchGames.EldritchLogger.Console.Parsing
{
    /// <summary>
    /// Defines the contract for executing parsed console commands.
    /// </summary>
    public interface ICommandExecutor
    {
        /// <summary>
        /// Executes the command represented by the given parse result.
        /// </summary>
        /// <param name="result">The result of parsing user input.</param>
        void Execute(ParseResult result);
    }
}
