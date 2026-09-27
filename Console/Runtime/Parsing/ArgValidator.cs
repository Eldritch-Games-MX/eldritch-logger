using EldritchGames.EldritchLogger.Console.Core;
using System.Linq;

namespace EldritchGames.EldritchLogger.Console.Parsing
{
    /// <summary>
    /// Provides validation logic for command arguments and flags.
    /// </summary>
    /// <remarks>
    /// The <see cref="ArgValidator"/> ensures that user-supplied arguments match
    /// the expected specifications defined in <see cref="ArgSpec"/> metadata.
    /// It checks required arguments, type constraints, multiplicity, and flags.
    /// </remarks>
    public static class ArgValidator
    {
        /// <summary>
        /// Validates the arguments for a given command against its metadata.
        /// </summary>
        /// <param name="command">The command metadata containing expected arguments and flags.</param>
        /// <param name="args">The arguments provided by the user.</param>
        /// <param name="error">
        /// When validation fails, contains a descriptive error message; otherwise <c>null</c>.
        /// </param>
        /// <returns><c>true</c> if validation succeeds; otherwise <c>false</c>.</returns>
        public static bool Validate(ICommandMetadata command, string[] args, out string error)
        {
            return ValidateInternal(command.Name, command.ExpectedArgs, args, out error);
        }

        /// <summary>
        /// Performs internal validation of arguments against specifications.
        /// </summary>
        /// <param name="commandName">The name of the command being validated.</param>
        /// <param name="specs">The expected argument specifications.</param>
        /// <param name="args">The arguments provided by the user.</param>
        /// <param name="error">
        /// When validation fails, contains a descriptive error message; otherwise <c>null</c>.
        /// </param>
        /// <returns><c>true</c> if validation succeeds; otherwise <c>false</c>.</returns>
        private static bool ValidateInternal(string commandName, ArgSpec[] specs, string[] args, out string error)
        {
            specs ??= new ArgSpec[0];

            var positionalSpecs = specs.Where(s => s.Type != "flag").ToArray();
            var flagSpecs = specs.Where(s => s.Type == "flag").ToArray();

            int requiredCount = positionalSpecs.Count(s => s.Required);

            // Check minimum required arguments
            if (args.Count(a => !a.StartsWith("--")) < requiredCount)
            {
                error = $"Command '{commandName}' expects at least {requiredCount} arguments.";
                return false;
            }

            // Check maximum allowed arguments (if multiple not allowed)
            if (!positionalSpecs.Any(s => s.AllowMultiple))
            {
                var maxArgs = positionalSpecs.Length;
                if (args.Count(a => !a.StartsWith("--")) > maxArgs)
                {
                    error = $"Command '{commandName}' expects at most {maxArgs} arguments.";
                    return false;
                }
            }

            // Validate argument types and constraints
            var positionalArgs = args.Where(a => !a.StartsWith("--")).ToArray();
            for (int i = 0; i < positionalSpecs.Length && i < positionalArgs.Length; i++)
            {
                var spec = positionalSpecs[i];
                var arg = positionalArgs[i];

                if (spec.Type == "int" && !int.TryParse(arg, out _))
                {
                    error = $"Argument '{spec.Name}' must be an integer.";
                    return false;
                }

                if (spec.MustBePositive && int.TryParse(arg, out var parsed) && parsed <= 0)
                {
                    error = $"Argument '{spec.Name}' must be positive.";
                    return false;
                }
            }

            // Validate required flags
            foreach (var flag in flagSpecs)
            {
                bool present = args.Contains(flag.Name);
                if (flag.Required && !present)
                {
                    error = $"Command '{commandName}' requires flag '{flag.Name}'.";
                    return false;
                }
            }

            error = null;
            return true;
        }
    }
}
