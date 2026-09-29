using System;

namespace EldritchGames.EldritchLogger.Console.Commands
{
    /// <summary>
    /// A command defined inline: a descriptor plus a callback.
    /// <code>
    /// registry.Register(new DelegateCommand(new CommandDescriptor("ping", "Replies pong."), ctx => ctx.Output.Info("pong")));
    /// </code>
    /// </summary>
    public sealed class DelegateCommand : IConsoleCommand
    {
        private readonly Action<CommandContext> execute;

        public CommandDescriptor Descriptor { get; }

        public DelegateCommand(CommandDescriptor descriptor, Action<CommandContext> execute)
        {
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public void Execute(CommandContext context) => execute(context);
    }
}
