using EldritchGames.EldritchLogger.Console.Arguments;
using EldritchGames.EldritchLogger.Console.Execution;
using EldritchGames.EldritchLogger.Console.Output;
using EldritchGames.EldritchLogger.Console.Parsing;
using System;
using System.Collections;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Commands.BuiltIn
{
    /// <summary>
    /// Runs another command several times. Its own flags must come before the repeated command:
    /// <c>repeat 3 --delay=500 history --limit 2</c>.
    /// </summary>
    public sealed class RepeatCommand : IAsyncConsoleCommand
    {
        private readonly ICommandExecutor executor;
        private readonly int maxRepeat;

        public CommandDescriptor Descriptor { get; } = new(
            "repeat",
            "Repeats a command a number of times.",
            parameters: new[]
            {
                ParameterSpec.Required("count", ArgumentTypes.PositiveInt, "How many times to run the command."),
                ParameterSpec.Remainder("command", description: "The command line to repeat.")
            },
            flags: new[]
            {
                FlagSpec.Switch("silent", "Do not print start/finish messages."),
                FlagSpec.WithValue("delay", ArgumentTypes.NonNegativeInt, description: "Milliseconds to wait between runs.")
            });

        public RepeatCommand(ICommandExecutor executor, int maxRepeat = 1000)
        {
            this.executor = executor ?? throw new ArgumentNullException(nameof(executor));
            this.maxRepeat = Math.Max(1, maxRepeat);
        }

        public IEnumerator Execute(CommandContext context)
        {
            var args = context.Arguments;
            int count = args.Get<int>("count");
            bool silent = args.HasFlag("silent");
            int delayMs = args.GetFlag("delay", 0);

            if (count > maxRepeat)
            {
                context.Output.Warn($"Repeat count capped at {maxRepeat}.");
                count = maxRepeat;
            }

            var inner = ParsedCommand.FromTokens(args.GetRemainder("command"));
            if (string.Equals(inner.Name, Descriptor.Name, StringComparison.OrdinalIgnoreCase))
            {
                context.Output.Warn("'repeat' cannot repeat itself.");
                yield break;
            }

            if (!silent)
                context.Output.Info($"Repeating '{inner.RawText}' {count} times (delay {delayMs} ms)...");

            for (int i = 0; i < count; i++)
            {
                if (executor.Execute(inner) == ExecutionStatus.Failed)
                {
                    context.Output.Warn($"Stopped after {i} of {count} runs.");
                    yield break;
                }

                if (delayMs > 0 && i < count - 1)
                    yield return new WaitForSecondsRealtime(delayMs / 1000f);
            }

            if (!silent)
                context.Output.Info($"Finished repeating '{inner.RawText}'.");
        }
    }
}
