using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Registry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EldritchGames.EldritchLogger.Console.Loader
{
    /// <summary>
    /// Discovers and instantiates <see cref="ICommandGroup"/> implementations at runtime.
    /// </summary>
    /// <remarks>
    /// The <see cref="CommandGroupLoader"/> scans all loaded assemblies for types
    /// implementing <see cref="ICommandGroup"/>. It resolves constructor dependencies
    /// using <see cref="ServiceRegistry"/> and returns instantiated groups ready for registration.
    /// </remarks>
    public static class CommandGroupLoader
    {
        /// <summary>
        /// Finds all <see cref="ICommandGroup"/> implementations in loaded assemblies,
        /// resolves their constructor dependencies, and returns them.
        /// </summary>
        /// <param name="strict">
        /// If true, throws an <see cref="InvalidOperationException"/> when a required dependency
        /// is missing in <see cref="ServiceRegistry"/>. If false, missing dependencies are resolved as null.
        /// </param>
        /// <returns>An enumerable of discovered command groups.</returns>
        public static IEnumerable<ICommandGroup> DiscoverGroups(bool strict = true)
        {
            var groupTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(ICommandGroup).IsAssignableFrom(t) && !t.IsAbstract);

            foreach (var type in groupTypes)
            {
                var ctor = type.GetConstructors().FirstOrDefault();
                if (ctor == null) continue;

                var args = ctor.GetParameters()
                               .Select(p => ServiceRegistry.Resolve(p.ParameterType, strict))
                               .ToArray();

                yield return (ICommandGroup)Activator.CreateInstance(type, args);
            }
        }
    }
}
