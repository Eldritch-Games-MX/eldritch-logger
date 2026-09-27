
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Console.Autocompletion
{
    /// <summary>
    /// Provides autocompletion suggestions for console input.
    /// </summary>
    public interface IAutocompleteProvider
    {
        /// <summary>
        /// Suggests autocompletions based on the current input.
        /// </summary>
        /// <param name="input">The current input string.</param>
        /// <returns>A collection of suggested autocompletions.</returns>
        IEnumerable<string> Suggest(string input);
    }
}