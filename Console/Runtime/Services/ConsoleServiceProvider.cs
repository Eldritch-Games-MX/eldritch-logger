using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EldritchGames.EldritchLogger.Console.Services
{
    /// <summary>
    /// Minimal service container owned by one console instance. Services are registered under an
    /// explicit type (usually an interface) and injected into command groups and commands.
    /// </summary>
    public sealed class ConsoleServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> services = new();

        /// <summary>Registers <paramref name="instance"/> as <typeparamref name="TService"/>. Null instances are ignored.</summary>
        public ConsoleServiceProvider Register<TService>(TService instance) where TService : class
        {
            if (instance is UnityEngine.Object unityObject ? unityObject != null : instance != null)
                services[typeof(TService)] = instance;
            return this;
        }

        public object GetService(Type serviceType) =>
            services.TryGetValue(serviceType, out var service) ? service : null;

        public T Get<T>() where T : class => GetService(typeof(T)) as T;

        public bool IsRegistered(Type serviceType) => services.ContainsKey(serviceType);

        public void Clear() => services.Clear();

        /// <summary>
        /// Creates <paramref name="type"/> using the public constructor with the most parameters
        /// that can all be satisfied (registered services, or parameters with default values).
        /// </summary>
        /// <param name="missing">When creation fails, the unresolved dependency types.</param>
        public bool TryCreate(Type type, out object instance, out string missing)
        {
            instance = null;
            missing = null;
            var constructors = type.GetConstructors().OrderByDescending(c => c.GetParameters().Length).ToArray();
            if (constructors.Length == 0)
            {
                missing = "a public constructor";
                return false;
            }

            string firstMissing = null;
            foreach (var ctor in constructors)
            {
                if (TryResolveArguments(ctor, out var args, out var unresolved))
                {
                    instance = ctor.Invoke(args);
                    return true;
                }
                firstMissing ??= unresolved;
            }

            missing = firstMissing;
            return false;
        }

        private bool TryResolveArguments(ConstructorInfo ctor, out object[] args, out string unresolved)
        {
            var parameters = ctor.GetParameters();
            args = new object[parameters.Length];
            unresolved = null;
            var missingTypes = new List<string>();

            for (int i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                if (services.TryGetValue(p.ParameterType, out var service))
                    args[i] = service;
                else if (p.HasDefaultValue)
                    args[i] = p.DefaultValue;
                else
                    missingTypes.Add(p.ParameterType.Name);
            }

            if (missingTypes.Count == 0) return true;
            unresolved = string.Join(", ", missingTypes);
            return false;
        }
    }
}
