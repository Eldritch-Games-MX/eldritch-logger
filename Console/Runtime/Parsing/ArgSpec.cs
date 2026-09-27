namespace EldritchGames.EldritchLogger.Console.Parsing
{

    /// <summary>
    /// Defines the specification for a command argument, including its name, type, and constraints.
    /// </summary>
    public class ArgSpec
    {
        /// <summary>
        /// Gets or sets the name of the argument.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the argument.
        /// Supported values include "int", "string", and "flag".
        /// </summary>
        public string Type { get; set; } // "int", "string", "flag"

        /// <summary>
        /// Gets or sets a value indicating whether this argument is required.
        /// If true, the command cannot execute without this argument.
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether numeric arguments must be positive.
        /// Only relevant when <see cref="Type"/> is "int".
        /// </summary>
        public bool MustBePositive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether multiple values are allowed for this argument.
        /// If true, the argument can be repeated multiple times in the command input.
        /// </summary>
        public bool AllowMultiple { get; set; }
    }
}