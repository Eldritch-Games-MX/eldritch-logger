using EldritchGames.EldritchLogger.Console.Arguments;
using EldritchGames.EldritchLogger.Console.Output;
using EldritchGames.EldritchLogger.Console.Registry;
using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Sinks;
using EldritchGames.EldritchLogger.Sinks.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EldritchGames.EldritchLogger.Console.Commands.BuiltIn
{
    /// <summary>
    /// <c>log.*</c> commands to inspect and adjust the running EldritchLogger:
    /// level and category overrides, sinks, flushing, the log folder and test entries.
    /// Overrides are in-memory only; the LogSettings asset is never modified.
    /// </summary>
    public sealed class LoggerCommandGroup : ICommandGroup
    {
        private static readonly string[] LevelNames = Enum.GetNames(typeof(LogLevel));

        private readonly Func<ILogControl> control;
        private readonly Func<ISinkRegistry> sinks;
        private readonly Func<string, IEldritchLogger> loggers;
        private readonly Action<string> openFolder;

        public string Name => "Logger";

        /// <summary>Uses the logger installed in <see cref="ELoggerFactory"/> (looked up on every command).</summary>
        public LoggerCommandGroup()
            : this(() => ELoggerFactory.Control, () => ELoggerFactory.Sinks, ELoggerFactory.GetLogger,
                   path => UnityEngine.Application.OpenURL("file:///" + path.Replace('\\', '/'))) { }

        internal LoggerCommandGroup(Func<ILogControl> control, Func<ISinkRegistry> sinks,
                                    Func<string, IEldritchLogger> loggers, Action<string> openFolder)
        {
            this.control = control ?? throw new ArgumentNullException(nameof(control));
            this.sinks = sinks ?? throw new ArgumentNullException(nameof(sinks));
            this.loggers = loggers ?? throw new ArgumentNullException(nameof(loggers));
            this.openFolder = openFolder ?? throw new ArgumentNullException(nameof(openFolder));
        }

        public void Register(ICommandRegistry registry)
        {
            registry.Register(new DelegateCommand(new CommandDescriptor(
                "log.level",
                "Shows the minimum log level, sets a runtime override, or resets it.",
                parameters: new[] { ParameterSpec.Optional("level", ArgumentTypes.Choice("level", () => LevelNames.Append("reset"))) }),
                Level));

            registry.Register(new DelegateCommand(new CommandDescriptor(
                "log.category",
                "Shows a category's state, turns it on or off at runtime, or resets it.",
                parameters: new[]
                {
                    ParameterSpec.Required("name", ArgumentTypes.Choice("category", CategoryNames)),
                    ParameterSpec.Optional("state", ArgumentTypes.Choice("state", () => new[] { "on", "off", "reset" }))
                }),
                Category));

            registry.Register(new DelegateCommand(new CommandDescriptor(
                "log.categories", "Lists categories and whether they are logged."), Categories));

            registry.Register(new DelegateCommand(new CommandDescriptor(
                "log.sinks", "Lists the running sinks."), Sinks));

            registry.Register(new DelegateCommand(new CommandDescriptor(
                "log.flush", "Writes out everything buffered by the sinks."), Flush));

            registry.Register(new DelegateCommand(new CommandDescriptor(
                "log.open", "Opens the folder that file sinks write to."), Open));

            registry.Register(new DelegateCommand(new CommandDescriptor(
                "log.reset", "Removes every runtime level and category override."), Reset));

            registry.Register(new DelegateCommand(new CommandDescriptor(
                "log.test",
                "Logs a test entry through the logger.",
                // The level is a flag so the message can start with any word: log.test Something broke --level=Error
                parameters: new[] { ParameterSpec.Variadic("message", ArgumentTypes.String) },
                flags: new[] { FlagSpec.WithValue("level", ArgumentTypes.Enum<LogLevel>(), description: "Level of the test entry (default Info).") }),
                Test));
        }

        private IEnumerable<string> CategoryNames() =>
            control()?.Categories.Select(c => c.Category.Name) ?? Enumerable.Empty<string>();

        private bool TryGetControl(CommandContext context, out ILogControl logControl)
        {
            logControl = control();
            if (logControl != null) return true;
            context.Output.Warn("The logger is not initialized, or it does not support runtime control.");
            return false;
        }

        private void Level(CommandContext context)
        {
            if (!TryGetControl(context, out var logControl)) return;

            var requested = context.Arguments.GetOrDefault<string>("level");
            if (requested == "reset")
                logControl.MinimumLevelOverride = null;
            else if (requested != null)
                logControl.MinimumLevelOverride = (LogLevel)Enum.Parse(typeof(LogLevel), requested, ignoreCase: true);

            var note = logControl.MinimumLevelOverride.HasValue ? " (runtime override)" : " (from settings)";
            context.Output.Info($"Minimum level: {logControl.MinimumLevel}{note}");

            // The logger also drops entries that no sink accepts, whatever the filter says.
            if (sinks().LevelHiddenBySinks(logControl.MinimumLevel) is { } lowestSink)
                context.Output.Warn($"No sink accepts entries below {lowestSink}, so {logControl.MinimumLevel} entries are still discarded. " +
                                    "Lower a sink's minimum level in the LogSettings asset.");
        }

        private void Category(CommandContext context)
        {
            if (!TryGetControl(context, out var logControl)) return;

            var category = new LogCategory(context.Arguments.Get<string>("name"));
            switch (context.Arguments.GetOrDefault<string>("state"))
            {
                case "on": logControl.SetCategoryOverride(category, true); break;
                case "off": logControl.SetCategoryOverride(category, false); break;
                case "reset": logControl.SetCategoryOverride(category, null); break;
            }

            var state = logControl.Categories.FirstOrDefault(c => c.Category == category);
            context.Output.Info($"{category.Name}: {Describe(state)}");
        }

        private void Categories(CommandContext context)
        {
            if (!TryGetControl(context, out var logControl)) return;

            foreach (var state in logControl.Categories.OrderBy(c => c.Category.Name, StringComparer.OrdinalIgnoreCase))
                context.Output.Info($"{state.Category.Name}: {Describe(state)}");
        }

        private static string Describe(CategoryState state) =>
            (state.Enabled ? "on" : "off") + (state.Overridden ? " (runtime override)" : "");

        private void Sinks(CommandContext context)
        {
            var registry = sinks();
            if (registry == null)
            {
                context.Output.Warn("The logger is not initialized.");
                return;
            }

            foreach (var sink in registry.All)
            {
                var line = $"{sink.Name}  ≥{sink.MinimumLevel}";
                if (sink is ISinkDiagnostics diagnostics)
                {
                    if (diagnostics.DroppedCount > 0) line += $"  dropped: {diagnostics.DroppedCount}";
                    if (!string.IsNullOrEmpty(diagnostics.Location)) line += $"  → {diagnostics.Location}";
                }
                context.Output.Info(line);
            }
        }

        private void Flush(CommandContext context)
        {
            if (!TryGetControl(context, out var logControl)) return;
            logControl.Flush();
            context.Output.Info("Sinks flushed.");
        }

        private void Open(CommandContext context)
        {
            // Folders of the running file sinks, or the default log folder.
            var folders = (sinks()?.All ?? Array.Empty<ILogSink>())
                .OfType<ISinkDiagnostics>()
                .Select(d => d.Location)
                .Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                .Select(Path.GetDirectoryName)
                .Distinct()
                .ToList();
            if (folders.Count == 0) folders.Add(LogFileLocator.ResolveDirectory(null));

            foreach (var folder in folders) context.Output.Info(folder);
            openFolder(folders[0]);
        }

        private void Reset(CommandContext context)
        {
            if (!TryGetControl(context, out var logControl)) return;
            logControl.ClearOverrides();
            context.Output.Info($"Overrides cleared. Minimum level: {logControl.MinimumLevel}");
        }

        private void Test(CommandContext context)
        {
            var level = context.Arguments.GetFlag("level", LogLevel.Info);
            var words = context.Arguments.GetAll<string>("message");
            var message = words.Count > 0 ? string.Join(" ", words) : "Test entry from the console";

            var logger = loggers("Console");
            if (!logger.IsEnabled(level, LogCategory.General))
            {
                context.Output.Warn($"{level} entries in 'General' are currently filtered out (see log.level and log.category).");
                return;
            }

            logger.At(level).Log(message);
        }
    }
}
