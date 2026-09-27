using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Registry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EldritchGames.EldritchLogger.Console.Services
{
    /// <summary>
    /// Finds <see cref="ICommandGroup"/> implementations and <see cref="ConsoleCommandAttribute"/>
    /// commands, creates them through a <see cref="ConsoleServiceProvider"/> and registers them.
    /// Only assemblies that reference the console assembly are scanned, and the scan is cached.
    /// </summary>
    public sealed class CommandDiscovery
    {
        private static readonly object CacheLock = new();
        private static Type[] cachedGroupTypes;
        private static Type[] cachedCommandTypes;

        private readonly ConsoleServiceProvider services;
        private readonly Action<string> warn;

        /// <param name="warn">Receives a message for every group or command that could not be created.</param>
        public CommandDiscovery(ConsoleServiceProvider services, Action<string> warn = null)
        {
            this.services = services ?? throw new ArgumentNullException(nameof(services));
            this.warn = warn ?? (_ => { });
        }

        /// <summary>Registers every discoverable group and attributed command. Failures are reported and skipped.</summary>
        /// <returns>The number of groups and commands registered.</returns>
        public int RegisterAll(ICommandRegistry registry) =>
            RegisterAll(registry, GroupTypes, CommandTypes);

        /// <summary>Registers the given types (used by <see cref="RegisterAll(ICommandRegistry)"/> and tests).</summary>
        public int RegisterAll(ICommandRegistry registry, IEnumerable<Type> groupTypes, IEnumerable<Type> commandTypes)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            int registered = 0;

            foreach (var type in groupTypes)
            {
                if (!TryCreate(type, "command group", out var instance)) continue;
                try
                {
                    ((ICommandGroup)instance).Register(registry);
                    registered++;
                }
                catch (Exception ex)
                {
                    warn($"Command group '{type.Name}' failed to register: {ex.Message}");
                }
            }

            foreach (var type in commandTypes)
            {
                if (!TryCreate(type, "command", out var instance)) continue;
                try
                {
                    registry.Register((ICommand)instance);
                    registered++;
                }
                catch (Exception ex)
                {
                    warn($"Command '{type.Name}' failed to register: {ex.Message}");
                }
            }

            return registered;
        }

        private bool TryCreate(Type type, string kind, out object instance)
        {
            try
            {
                if (services.TryCreate(type, out instance, out var missing)) return true;
                warn($"Skipped {kind} '{type.Name}': missing {missing}.");
            }
            catch (Exception ex)
            {
                instance = null;
                var inner = ex is TargetInvocationException { InnerException: { } e } ? e : ex;
                warn($"Skipped {kind} '{type.Name}': constructor threw {inner.GetType().Name}: {inner.Message}");
            }
            return false;
        }

        public static IReadOnlyList<Type> GroupTypes
        {
            get
            {
                EnsureScanned();
                return cachedGroupTypes;
            }
        }

        public static IReadOnlyList<Type> CommandTypes
        {
            get
            {
                EnsureScanned();
                return cachedCommandTypes;
            }
        }

        private static void EnsureScanned()
        {
            if (cachedGroupTypes != null) return;
            lock (CacheLock)
            {
                if (cachedGroupTypes != null) return;

                var consoleAssembly = typeof(ICommand).Assembly;
                var consoleName = consoleAssembly.GetName().Name;
                var types = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => a == consoleAssembly || a.GetReferencedAssemblies().Any(r => r.Name == consoleName))
                    .SelectMany(SafeGetTypes)
                    .Where(t => t is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false })
                    .ToArray();

                cachedCommandTypes = types
                    .Where(t => typeof(ICommand).IsAssignableFrom(t) && t.IsDefined(typeof(ConsoleCommandAttribute), false))
                    .ToArray();
                cachedGroupTypes = types
                    .Where(t => typeof(ICommandGroup).IsAssignableFrom(t))
                    .ToArray();
            }
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
            }
        }
    }
}
