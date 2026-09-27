using EldritchGames.EldritchLogger.Console.Arguments;
using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Output;
using EldritchGames.EldritchLogger.Console.Parsing;
using EldritchGames.EldritchLogger.Console.Registry;
using System;

namespace EldritchGames.EldritchLogger.Console.Execution
{
    public enum ExecutionStatus
    {
        /// <summary>A synchronous command finished without throwing.</summary>
        Completed,
        /// <summary>An asynchronous command was started.</summary>
        Started,
        /// <summary>Parsing, binding or execution failed; the reason was written to the output.</summary>
        Failed
    }

    public interface ICommandExecutor
    {
        /// <summary>Lexes, parses, binds and runs a command line, recording it in the history.</summary>
        ExecutionStatus Execute(string input);

        /// <summary>Binds and runs an already-parsed command (no history recorded).</summary>
        ExecutionStatus Execute(ParsedCommand command);
    }

    /// <summary>
    /// Input line → tokens → <see cref="ParsedCommand"/> → bound arguments → command.
    /// Every failure is reported to <see cref="IConsoleOutput"/>; nothing propagates to the caller.
    /// </summary>
    public sealed class CommandExecutor : ICommandExecutor
    {
        private readonly ICommandRegistry registry;
        private readonly Lexer lexer;
        private readonly ICommandParser parser;
        private readonly IArgumentBinder binder;
        private readonly CommandHistory history;
        private readonly IConsoleOutput output;
        private readonly ICommandRunner runner;

        public CommandExecutor(ICommandRegistry registry,
                               Lexer lexer,
                               ICommandParser parser,
                               IArgumentBinder binder,
                               CommandHistory history,
                               IConsoleOutput output,
                               ICommandRunner runner)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
            this.lexer = lexer ?? throw new ArgumentNullException(nameof(lexer));
            this.parser = parser ?? throw new ArgumentNullException(nameof(parser));
            this.binder = binder ?? throw new ArgumentNullException(nameof(binder));
            this.history = history;
            this.output = output ?? throw new ArgumentNullException(nameof(output));
            this.runner = runner ?? throw new ArgumentNullException(nameof(runner));
        }

        public ExecutionStatus Execute(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return ExecutionStatus.Failed;

            var result = parser.Parse(lexer.Tokenize(input), input);
            if (!result.Success)
            {
                output.Warn($"Parse error: {result.ErrorMessage}");
                return ExecutionStatus.Failed;
            }

            history?.Add(input.Trim());
            return Execute(result.Command);
        }

        public ExecutionStatus Execute(ParsedCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            if (!registry.TryGet(command.Name, out var target))
            {
                output.Warn($"Unknown command: {command.Name}. Type 'help' to list commands.");
                return ExecutionStatus.Failed;
            }

            var bound = binder.Bind(target.Descriptor, command);
            if (!bound.Success)
            {
                output.Warn(bound.Error);
                return ExecutionStatus.Failed;
            }

            var context = new CommandContext(bound.Arguments, output, command.RawText);
            try
            {
                switch (target)
                {
                    case IConsoleCommand sync:
                        sync.Execute(context);
                        return ExecutionStatus.Completed;

                    case IAsyncConsoleCommand async:
                        runner.Run(async.Execute(context), ex => ReportFailure(command.Name, ex));
                        return ExecutionStatus.Started;

                    default:
                        output.Error($"'{command.Name}' implements neither IConsoleCommand nor IAsyncConsoleCommand.");
                        return ExecutionStatus.Failed;
                }
            }
            catch (Exception ex)
            {
                ReportFailure(command.Name, ex);
                return ExecutionStatus.Failed;
            }
        }

        private void ReportFailure(string name, Exception ex) =>
            output.Error($"'{name}' failed: {ex.GetType().Name}: {ex.Message}");
    }
}
