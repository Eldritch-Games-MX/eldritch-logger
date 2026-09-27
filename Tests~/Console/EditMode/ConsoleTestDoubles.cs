using EldritchGames.EldritchLogger.Console.Arguments;
using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Execution;
using EldritchGames.EldritchLogger.Console.Output;
using EldritchGames.EldritchLogger.Console.Parsing;
using EldritchGames.EldritchLogger.Console.Registry;
using EldritchGames.EldritchLogger.Console.UI;
using System;
using System.Collections;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode
{
    /// <summary>In-memory console view.</summary>
    internal sealed class FakeView : IConsoleView
    {
        public readonly List<string> Lines = new();
        public string Ghost;
        public bool Accepted;
        public bool IsVisible { get; set; } = true;
        public int Clears;

        public event Action<string> OnCommandSubmitted;
        public event Action<string> OnInputChanged;

        public void AppendLog(string message) => Lines.Add(message);
        public void Clear() { Lines.Clear(); Clears++; }
        public void ShowGhostSuggestion(string suggestion) => Ghost = suggestion;
        public void AcceptGhostSuggestion() => Accepted = true;
        public void ToggleVisibility() => IsVisible = !IsVisible;

        public void Submit(string text) => OnCommandSubmitted?.Invoke(text);
        public void Type(string text) => OnInputChanged?.Invoke(text);
    }

    internal sealed class RecordingOutput : IConsoleOutput
    {
        public readonly List<(string message, ConsoleMessageType type)> Messages = new();
        public int Clears;

        public void Write(string message, ConsoleMessageType type = ConsoleMessageType.Info) => Messages.Add((message, type));
        public void Clear() => Clears++;

        public IEnumerable<string> Texts { get { foreach (var m in Messages) yield return m.message; } }
    }

    /// <summary>A command that records its invocations.</summary>
    internal sealed class SpyCommand : IConsoleCommand
    {
        public readonly List<CommandArguments> Calls = new();
        public Action<CommandContext> OnExecute;

        public SpyCommand(CommandDescriptor descriptor) => Descriptor = descriptor;
        public SpyCommand(string name = "echo") : this(new CommandDescriptor(name, "test",
            parameters: new[] { ParameterSpec.Variadic("words", ArgumentTypes.String) },
            flags: new[] { FlagSpec.Switch("loud") })) { }

        public CommandDescriptor Descriptor { get; }

        public void Execute(CommandContext context)
        {
            Calls.Add(context.Arguments);
            OnExecute?.Invoke(context);
        }
    }

    internal sealed class SpyAsyncCommand : IAsyncConsoleCommand
    {
        public int Steps;
        public CommandDescriptor Descriptor { get; } = new("wait", "async test");

        public IEnumerator Execute(CommandContext context)
        {
            Steps++;
            yield return null;
            Steps++;
        }
    }

    internal static class ConsoleTestHelpers
    {
        public static ParsedCommand Parse(string input)
        {
            var result = new CommandParser().Parse(new Lexer().Tokenize(input), input);
            if (!result.Success) throw new InvalidOperationException(result.ErrorMessage);
            return result.Command;
        }

        public static CommandExecutor CreateExecutor(ICommandRegistry registry, IConsoleOutput output,
                                                     CommandHistory history = null, ICommandRunner runner = null) =>
            new(registry, new Lexer(), new CommandParser(), new ArgumentBinder(), history, output,
                runner ?? new ImmediateCommandRunner());
    }
}
