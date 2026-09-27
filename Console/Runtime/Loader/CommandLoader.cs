using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Core;
using EldritchGames.EldritchLogger.Console.Parsing;
using EldritchGames.EldritchLogger.Console.Registry;
using EldritchGames.EldritchLogger.Console.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Loader
{
    /// <summary>
    /// Discovers and registers console commands marked with <see cref="ConsoleCommandAttribute"/> at runtime.
    /// </summary>
    public static class CommandLoader
    {
        public static void RegisterAttributedCommands(
            ICommandRegistry registry,
            IConsoleView view,
            CommandHistory history,
            CommandConsoleSettings settings,
            ICommandParser parser,
            ICommandExecutor executor,
            Lexer lexer,
            IConsoleThemeApplier themeApplier,
            IThemeLoader themeLoader)
        {
            // Build a dictionary of known services
            var services = new Dictionary<Type, object>
        {
            { typeof(ICommandRegistry), registry },
            { typeof(IConsoleView), view },
            { typeof(CommandHistory), history },
            { typeof(CommandConsoleSettings), settings },
            { typeof(ICommandParser), parser },
            { typeof(ICommandExecutor), executor },
            { typeof(Lexer), lexer },
            { typeof(IConsoleThemeApplier), themeApplier },
            { typeof(IThemeLoader), themeLoader }
        };

            var commandTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttribute<ConsoleCommandAttribute>() != null
                            && !t.IsAbstract
                            && !t.IsInterface);

            foreach (var type in commandTypes)
            {
                try
                {
                    var ctor = type.GetConstructors().FirstOrDefault();
                    if (ctor == null)
                    {
                        Debug.LogWarning($"{type.Name} has no public constructor.");
                        continue;
                    }

                    // Resolve constructor parameters from dictionary
                    var args = ctor.GetParameters()
                                   .Select(p =>
                                       services.TryGetValue(p.ParameterType, out var service)
                                           ? service
                                           : null)
                                   .ToArray();

                    var instance = Activator.CreateInstance(type, args);

                    switch (instance)
                    {
                        case IAdvancedConsoleCommand advCmd:
                            registry.Register(advCmd);
                            Debug.Log($"Registered advanced command: {advCmd.Name}");
                            break;
                        case IConsoleCommand cmd:
                            registry.Register(cmd);
                            Debug.Log($"Registered command: {cmd.Name}");
                            break;
                        default:
                            Debug.LogWarning($"{type.Name} is marked [ConsoleCommand] but does not implement a valid interface.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to register command {type.Name}: {ex.Message}");
                }
            }
        }
    }
}