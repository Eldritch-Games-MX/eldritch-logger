using EldritchGames.EldritchLogger.Console.Arguments;
using EldritchGames.EldritchLogger.Console.Output;
using System;
using System.Collections;

namespace EldritchGames.EldritchLogger.Console.Commands
{
    /// <summary>Anything that can be registered and invoked from the console.</summary>
    public interface ICommand
    {
        CommandDescriptor Descriptor { get; }
    }

    /// <summary>A command that completes synchronously.</summary>
    public interface IConsoleCommand : ICommand
    {
        void Execute(CommandContext context);
    }

    /// <summary>
    /// A command that runs over several frames as a coroutine
    /// (yield <c>WaitForSeconds</c>, <c>null</c>, nested enumerators...).
    /// </summary>
    public interface IAsyncConsoleCommand : ICommand
    {
        IEnumerator Execute(CommandContext context);
    }

    /// <summary>Everything a command receives when it runs.</summary>
    public sealed class CommandContext
    {
        public CommandArguments Arguments { get; }

        /// <summary>Where the command writes its output. Do not use <c>Debug.Log</c> for command output.</summary>
        public IConsoleOutput Output { get; }

        /// <summary>The original input line.</summary>
        public string RawInput { get; }

        public CommandContext(CommandArguments arguments, IConsoleOutput output, string rawInput)
        {
            Arguments = arguments ?? CommandArguments.Empty;
            Output = output ?? throw new ArgumentNullException(nameof(output));
            RawInput = rawInput ?? string.Empty;
        }
    }

    /// <summary>
    /// Registers a set of related commands. Implementations are discovered automatically;
    /// constructor parameters are resolved from the console's service provider.
    /// </summary>
    public interface ICommandGroup
    {
        string Name { get; }

        void Register(Registry.ICommandRegistry registry);
    }

    /// <summary>
    /// Marks a command class for automatic registration. Constructor parameters are resolved
    /// from the console's service provider. Commands registered by an <see cref="ICommandGroup"/>
    /// should not carry this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ConsoleCommandAttribute : Attribute
    {
    }
}
