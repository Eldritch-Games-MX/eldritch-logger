using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Registry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EldritchGames.EldritchLogger.Console.Services
{
    /// <summary>What <see cref="CommandDiscovery"/> did: registrations, skipped types and name conflicts.</summary>
    public sealed class DiscoveryReport
    {
        public readonly struct Registration
        {
            /// <summary>The command group or attributed command type that produced the command.</summary>
            public Type Source { get; }
            public ICommand Command { get; }

            public Registration(Type source, ICommand command)
            {
                Source = source;
                Command = command;
            }
        }

        public readonly struct Skipped
        {
            public Type Type { get; }
            public string Reason { get; }

            public Skipped(Type type, string reason)
            {
                Type = type;
                Reason = reason;
            }
        }

        private readonly List<Registration> registrations = new();
        private readonly List<Skipped> skipped = new();
        private readonly List<string> conflicts = new();

        public IReadOnlyList<Registration> Registrations => registrations;
        public IReadOnlyList<Skipped> SkippedTypes => skipped;

        /// <summary>Commands that replaced another command with the same name, and rejected aliases.</summary>
        public IReadOnlyList<string> Conflicts => conflicts;

        internal void AddRegistration(Type source, ICommand command) => registrations.Add(new Registration(source, command));
        internal void AddSkipped(Type type, string reason) => skipped.Add(new Skipped(type, reason));
        internal void AddConflict(string message) => conflicts.Add(message);

        /// <summary>The source type that registered <paramref name="command"/>, or null.</summary>
        public Type SourceOf(ICommand command)
        {
            foreach (var r in registrations)
                if (ReferenceEquals(r.Command, command)) return r.Source;
            return null;
        }
    }

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

        /// <param name="warn">Receives a message for every skipped type and every conflict.</param>
        public CommandDiscovery(ConsoleServiceProvider services, Action<string> warn = null)
        {
            this.services = services ?? throw new ArgumentNullException(nameof(services));
            this.warn = warn ?? (_ => { });
        }

        /// <summary>Registers every discoverable group and attributed command. Failures are reported and skipped.</summary>
        public DiscoveryReport RegisterAll(ICommandRegistry registry) =>
            RegisterAll(registry, GroupTypes, CommandTypes);

        /// <summary>Registers the given types (used by <see cref="RegisterAll(ICommandRegistry)"/> and tests).</summary>
        public DiscoveryReport RegisterAll(ICommandRegistry registry, IEnumerable<Type> groupTypes, IEnumerable<Type> commandTypes)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            var report = new DiscoveryReport();
            var tracking = new TrackingRegistry(registry, report, warn);

            foreach (var type in groupTypes)
            {
                if (!TryCreate(type, "command group", report, out var instance)) continue;
                tracking.Source = type;
                try
                {
                    ((ICommandGroup)instance).Register(tracking);
                }
                catch (Exception ex)
                {
                    Skip(report, type, $"Command group '{type.Name}' failed to register: {ex.Message}");
                }
            }

            foreach (var type in commandTypes)
            {
                if (!TryCreate(type, "command", report, out var instance)) continue;
                tracking.Source = type;
                tracking.Register((ICommand)instance);
            }

            tracking.Source = null;
            return report;
        }

        private bool TryCreate(Type type, string kind, DiscoveryReport report, out object instance)
        {
            try
            {
                if (services.TryCreate(type, out instance, out var missing)) return true;
                Skip(report, type, $"Skipped {kind} '{type.Name}': missing {missing}.");
            }
            catch (Exception ex)
            {
                instance = null;
                var inner = ex is TargetInvocationException { InnerException: { } e } ? e : ex;
                Skip(report, type, $"Skipped {kind} '{type.Name}': constructor threw {inner.GetType().Name}: {inner.Message}");
            }
            return false;
        }

        private void Skip(DiscoveryReport report, Type type, string message)
        {
            report.AddSkipped(type, message);
            warn(message);
        }

        /// <summary>Records who registered what, and reports replaced names and rejected aliases.</summary>
        private sealed class TrackingRegistry : ICommandRegistry
        {
            private readonly ICommandRegistry inner;
            private readonly DiscoveryReport report;
            private readonly Action<string> warn;

            public Type Source { get; set; }

            public TrackingRegistry(ICommandRegistry inner, DiscoveryReport report, Action<string> warn)
            {
                this.inner = inner;
                this.report = report;
                this.warn = warn;
            }

            public IReadOnlyList<ICommand> All => inner.All;
            public bool TryGet(string nameOrAlias, out ICommand command) => inner.TryGet(nameOrAlias, out command);
            public bool Unregister(string nameOrAlias) => inner.Unregister(nameOrAlias);

            public void Register(ICommand command)
            {
                if (Source == null)
                {
                    // Registered after discovery (e.g. a group that kept the registry): pass through.
                    inner.Register(command);
                    return;
                }

                var name = command.Descriptor.Name;
                if (inner.TryGet(name, out var existing) && !ReferenceEquals(existing, command))
                {
                    var previous = report.SourceOf(existing)?.Name ?? "unknown";
                    Conflict($"'{name}' from {Source.Name} replaces the command registered by {previous}.");
                }

                try
                {
                    inner.Register(command);
                    report.AddRegistration(Source, command);
                }
                catch (Exception ex)
                {
                    Conflict($"'{name}' from {Source.Name} was not registered: {ex.Message}");
                }
            }

            private void Conflict(string message)
            {
                report.AddConflict(message);
                warn(message);
            }
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
                    .Where(a => a == consoleAssembly || References(a, consoleName))
                    .Where(a => !References(a, "nunit.framework")) // test assemblies define throwaway commands
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

        private static bool References(Assembly assembly, string name) =>
            assembly.GetReferencedAssemblies().Any(r => r.Name == name);

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
