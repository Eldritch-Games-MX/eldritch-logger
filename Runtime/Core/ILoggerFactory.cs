namespace EldritchGames.EldritchLogger.Core
{
    /// <summary>
    /// Factory that produces named <see cref="IEldritchLogger"/> instances.
    /// Mirrors the SLF4J <c>ILoggerFactory</c> pattern: the factory is the single
    /// swappable binding point between consumer code and the logging back-end.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Register an implementation once at startup via <see cref="ELoggerFactory.SetFactory"/>.
    /// The default implementation, <see cref="EldritchLoggerFactory"/>, is wired up
    /// automatically by <see cref="Settings.LoggerBootstrap"/>.
    /// </para>
    /// <para>
    /// DI container adapters (Zenject, VContainer, etc.) can bind a custom implementation
    /// to replace the default factory without touching consumer code.
    /// </para>
    /// </remarks>
    public interface ILoggerFactory
    {
        /// <summary>
        /// Returns a logger bound to the given <paramref name="name"/>.
        /// The name is stamped as <c>Logger = name</c> in every entry the logger produces,
        /// making it easy to filter or route logs by caller class.
        /// </summary>
        /// <param name="name">
        /// Identifies the logger — typically the short class name of the caller
        /// (e.g. <c>"PlayerController"</c>).
        /// Must not be null or whitespace.
        /// </param>
        /// <returns>An <see cref="IEldritchLogger"/> that tags all entries with the given name.</returns>
        IEldritchLogger GetLogger(string name);
    }
}
