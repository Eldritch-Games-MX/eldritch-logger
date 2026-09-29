namespace EldritchGames.EldritchLogger.Console.Commands.BuiltIn
{
    public sealed class ClearCommand : IConsoleCommand
    {
        public CommandDescriptor Descriptor { get; } =
            new("clear", "Clears the console output.", aliases: new[] { "cls" });

        public void Execute(CommandContext context) => context.Output.Clear();
    }
}
