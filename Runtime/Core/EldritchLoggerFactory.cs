using System;

namespace EldritchGames.EldritchLogger.Core
{
    /// <summary>
    /// Default <see cref="ILoggerFactory"/> implementation.
    /// Wraps a shared root <see cref="IEldritchLogger"/> and hands out
    /// <see cref="NamedLogger"/> decorators that stamp the caller's class name
    /// onto every log entry they produce.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Settings.LoggerBootstrap"/> creates this factory at startup and registers it
    /// via <see cref="ELoggerFactory.SetFactory"/>. You do not normally instantiate this class
    /// directly unless you are writing a DI container adapter or a custom bootstrap.
    /// </para>
    /// <para>
    /// Disposing this factory disposes the underlying root logger and its sinks.
    /// After disposal, <see cref="ELoggerFactory.GetLogger{T}"/> will return
    /// <see cref="NullLogger.Instance"/> until a new factory is registered.
    /// </para>
    /// </remarks>
    public sealed class EldritchLoggerFactory : ILoggerFactory, IDisposable
    {
        private readonly IEldritchLogger _root;

        /// <summary>
        /// Initializes the factory with the given root logger.
        /// All named loggers produced by this factory share the same root back-end.
        /// </summary>
        /// <param name="root">The root logger that processes every entry. Must not be null.</param>
        public EldritchLoggerFactory(IEldritchLogger root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or whitespace.</exception>
        public IEldritchLogger GetLogger(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Logger name must not be null or whitespace.", nameof(name));
            return new NamedLogger(_root, name);
        }

        /// <summary>
        /// Disposes the root logger and all its sinks.
        /// Call this once on application quit; <see cref="Settings.LoggerBootstrap"/> does this automatically.
        /// </summary>
        public void Dispose()
        {
            if (_root is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
