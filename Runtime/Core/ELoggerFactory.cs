using System;

namespace EldritchGames.EldritchLogger.Core
{
    /// <summary>
    /// Static entry point for obtaining named <see cref="IEldritchLogger"/> instances.
    /// Mirrors the SLF4J <c>LoggerFactory</c> pattern.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Typical usage for MonoBehaviours — initialize in <c>Awake</c>, not as a field initializer.
    /// Unity can run MonoBehaviour constructors during edit-mode deserialization, before
    /// <see cref="Settings.LoggerBootstrap"/> has registered the factory, which would cause
    /// the field to silently capture <see cref="NullLogger.Instance"/>:
    /// <code>
    /// private IEldritchLogger _logger;
    /// void Awake() { _logger = ELoggerFactory.GetLogger&lt;PlayerController&gt;(); }
    /// </code>
    /// If the factory has not yet been set, <see cref="GetLogger{T}"/> returns
    /// <see cref="NullLogger.Instance"/> instead of throwing.
    /// </para>
    /// <para>
    /// For pure C# classes, prefer constructor injection to keep dependencies explicit:
    /// <code>
    /// public GameService(IEldritchLogger logger) { _logger = logger; }
    /// </code>
    /// </para>
    /// <para>
    /// DI container adapters can replace the factory at startup:
    /// <code>
    /// ELoggerFactory.SetFactory(container.Resolve&lt;ILoggerFactory&gt;());
    /// </code>
    /// </para>
    /// </remarks>
    public static class ELoggerFactory
    {
        private static ILoggerFactory _factory;

        /// <summary>
        /// Registers the active <see cref="ILoggerFactory"/> implementation.
        /// Called once by <see cref="Settings.LoggerBootstrap"/> or a DI container adapter.
        /// </summary>
        /// <param name="factory">The factory to register. Must not be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="factory"/> is null.</exception>
        public static void SetFactory(ILoggerFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// Clears the registered factory.
        /// After this call, <see cref="GetLogger{T}"/> returns <see cref="NullLogger.Instance"/>.
        /// Called automatically on application quit by <see cref="Settings.LoggerBootstrap"/>.
        /// </summary>
        public static void ClearFactory()
        {
            _factory = null;
        }

        /// <summary>
        /// Returns a logger named after <typeparamref name="T"/>.
        /// Every entry produced by the returned logger carries <c>Logger = typeof(T).Name</c>
        /// in its metadata.
        /// </summary>
        /// <typeparam name="T">The class requesting the logger — used as the logger name.</typeparam>
        /// <returns>
        /// A named <see cref="IEldritchLogger"/>, or <see cref="NullLogger.Instance"/> if no
        /// factory has been registered.
        /// </returns>
        public static IEldritchLogger GetLogger<T>() => GetLogger(typeof(T).Name);

        /// <summary>
        /// Returns a logger bound to the given <paramref name="name"/>.
        /// Every entry produced by the returned logger carries <c>Logger = name</c> in its metadata.
        /// </summary>
        /// <param name="name">The logger name — typically the short class name of the caller.</param>
        /// <returns>
        /// A named <see cref="IEldritchLogger"/>, or <see cref="NullLogger.Instance"/> if no
        /// factory has been registered.
        /// </returns>
        public static IEldritchLogger GetLogger(string name)
        {
            return _factory?.GetLogger(name) ?? NullLogger.Instance;
        }
    }
}
