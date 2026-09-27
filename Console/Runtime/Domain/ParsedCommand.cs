using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Console.Domain
{
    /// <summary>
    /// Represents the result of parsing a console command string.
    /// </summary>
    /// <remarks>
    /// A <see cref="ParsedCommand"/> contains the command name, its arguments,
    /// any flags provided, and the original raw text input. It is used by the
    /// parser and executor to handle user input consistently.
    /// </remarks>
    public class ParsedCommand
    {
        /// <summary>
        /// Gets the name of the command (e.g., "help", "theme").
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the list of positional arguments supplied to the command.
        /// </summary>
        public List<string> Arguments { get; }

        /// <summary>
        /// Gets the dictionary of flags supplied to the command.
        /// </summary>
        /// <remarks>
        /// Flags are typically key-value pairs such as <c>--color=red</c>.
        /// </remarks>
        public Dictionary<string, string> Flags { get; }

        /// <summary>
        /// Gets the raw text input that was parsed into this command.
        /// </summary>
        public string RawText { get; }

        /// <summary>
        /// Provides an empty <see cref="ParsedCommand"/> instance with no name,
        /// arguments, flags, or raw text.
        /// </summary>
        public static readonly ParsedCommand Empty =
            new(string.Empty, new List<string>(), new Dictionary<string, string>(), string.Empty);

        /// <summary>
        /// Initializes a new instance of the <see cref="ParsedCommand"/> class.
        /// </summary>
        /// <param name="name">The name of the command.</param>
        /// <param name="arguments">The list of positional arguments.</param>
        /// <param name="flags">The dictionary of flags.</param>
        /// <param name="rawText">The original raw text input.</param>
        public ParsedCommand(string name, List<string> arguments, Dictionary<string, string> flags, string rawText)
        {
            Name = name;
            Arguments = arguments;
            Flags = flags;
            RawText = rawText;
        }
    }
}
