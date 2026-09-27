using EldritchGames.EldritchLogger.Console.Core;
using EldritchGames.EldritchLogger.Console.Parsing;
using System;
using System.Linq;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Commands
{
    [ConsoleCommand]
    public class RepeatCommand : IConsoleCommand
    {
        private readonly ICommandExecutor executor;
        private readonly ICommandParser parser;
        private readonly Lexer lexer;

        public RepeatCommand(ICommandExecutor executor, ICommandParser parser, Lexer lexer)
        {
            this.executor = executor ?? throw new ArgumentNullException(nameof(executor));
            this.parser = parser ?? throw new ArgumentNullException(nameof(parser));
            this.lexer = lexer ?? throw new ArgumentNullException(nameof(lexer));
        }

        public string Name => "repeat";
        public string Description => "Repeats a command a specified number of times. Use --silent to suppress logs, --delay=N to pause between iterations (ms).";

        public ArgSpec[] ExpectedArgs => new[]
        {
            new ArgSpec { Name = "count", Type = "int", Required = true, MustBePositive = true },
            new ArgSpec { Name = "command", Type = "string", Required = true },
            new ArgSpec { Name = "args", Type = "string", Required = false, AllowMultiple = true },
            new ArgSpec { Name = "--silent", Type = "flag", Required = false }
        };


        public void Execute(string[] args)
        {
            // Parse repeat count
            if (!int.TryParse(args[0], out var count) || count <= 0)
            {
                Debug.LogWarning("Repeat count must be a positive integer.");
                return;
            }

            // Safety cap to prevent runaway loops
            const int MaxRepeat = 1000;
            if (count > MaxRepeat)
            {
                Debug.LogWarning($"Repeat count capped at {MaxRepeat} to prevent runaway execution.");
                count = MaxRepeat;
            }

            // Detect --silent flag (suppresses start/finish logs)
            bool silent = args.Any(a => a.Equals("--silent", StringComparison.OrdinalIgnoreCase));

            // Parse --delay flag
            int delayMs = 0;
            var delayArg = args.FirstOrDefault(a => a.StartsWith("--delay=", StringComparison.OrdinalIgnoreCase));
            if (delayArg != null)
            {
                var parts = delayArg.Split('=');
                if (parts.Length == 2 && int.TryParse(parts[1], out var parsedDelay) && parsedDelay >= 0)
                    delayMs = parsedDelay;
                else
                    Debug.LogWarning("Invalid --delay value. Must be a non-negative integer (milliseconds).");
            }

            // Build inner command string excluding flags
            var innerArgs = args
                .Where(a => !a.Equals("--silent", StringComparison.OrdinalIgnoreCase) && !a.StartsWith("--delay=", StringComparison.OrdinalIgnoreCase))
                .Skip(1); // skip count
            var innerRaw = string.Join(" ", innerArgs);

            // Tokenize and parse the inner command using the same pipeline as ConsoleController
            var tokens = lexer.Tokenize(innerRaw);
            var result = parser.Parse(tokens, innerRaw);

            if (!silent)
                Debug.Log($"Repeating command '{innerRaw}' {count} times... (delay={delayMs}ms)");

            // Execute synchronously in a loop
            for (int i = 0; i < count; i++)
            {
                try
                {
                    executor.Execute(result);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Repeat iteration {i + 1} failed: {ex.Message}");
                    break; // stop on error
                }

                if (delayMs > 0 && i < count - 1)
                {
                    // simple blocking delay for tests; in Unity you’d use coroutine/async
                    System.Threading.Thread.Sleep(delayMs);
                }
            }

            if (!silent)
                Debug.Log($"Finished repeating '{innerRaw}' {count} times.");
        }
    }
}
