using System;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Console.Registry
{
    /// <summary>
    /// Provides a simple service locator for console dependencies.
    /// </summary>
    /// <remarks>
    /// Services are registered once at bootstrap and resolved when commands or groups
    /// request them via constructor injection. Strict mode ensures missing services
    /// throw clear errors at startup.
    /// </remarks>
    public static class ServiceRegistry
    {
        private static readonly Dictionary<Type, object> services = new();

        /// <summary>
        /// Registers a service instance for the given type.
        /// </summary>
        /// <typeparam name="T">The type of the service.</typeparam>
        /// <param name="instance">The service instance to register.</param>
        public static void Register<T>(T instance) => services[typeof(T)] = instance;

        /// <summary>
        /// Resolves a service instance for the given type.
        /// </summary>
        /// <param name="type">The type of the service to resolve.</param>
        /// <param name="strict">
        /// If true, throws an <see cref="InvalidOperationException"/> when the service is not found.
        /// If false, returns null instead.
        /// </param>
        /// <returns>The resolved service instance, or null if not found and strict mode is disabled.</returns>
        public static object Resolve(Type type, bool strict = true)
        {
            if (services.TryGetValue(type, out var service))
                return service;

            if (strict)
                throw new InvalidOperationException(
                    $"No service registered for type {type.FullName}. " +
                    $"Make sure to call ServiceRegistry.Register<{type.Name}> before discovery.");

            return null;
        }

        /// <summary>
        /// Clears all registered services from the registry.
        /// </summary>
        public static void Clear() => services.Clear();
    }
}