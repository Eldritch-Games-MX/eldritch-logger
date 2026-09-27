using EldritchGames.EldritchLogger.Console.Registry;
namespace EldritchGames.EldritchLogger.Console.Commands
{
    /// <summary>
    /// Defines a group of console commands that can be registered together.
    /// </summary>
    /// <remarks>
    /// Implement this interface in plugin or module classes to organize related commands.
    /// Groups are discovered at runtime and their commands are automatically registered.
    /// </remarks>
    public interface ICommandGroup
    {
        /// <summary>
        /// Registers all commands in this group with the provided registry.
        /// </summary>
        /// <param name="registry">
        /// The command registry where commands should be added.
        /// </param>
        void Register(ICommandRegistry registry);
        string Name { get; }
    }
}
