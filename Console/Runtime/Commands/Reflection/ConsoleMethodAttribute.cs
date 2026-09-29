using System;

namespace EldritchGames.EldritchLogger.Console.Commands.Reflection
{
    /// <summary>
    /// Turns a method into a console command. The method's parameters become the command's arguments.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Static methods, or instance methods of a <c>MonoBehaviour</c> (run on every active instance).</item>
    /// <item>Supported parameter types: <c>int</c>, <c>float</c>, <c>double</c>, <c>bool</c>, <c>string</c> and enums.
    /// Optional parameters become optional arguments; a trailing <c>params</c> array accepts any number of values.</item>
    /// <item>Parameters of type <see cref="CommandContext"/> or <c>IConsoleOutput</c> are injected, not typed by the user.</item>
    /// <item>A non-void return value is printed. Returning <c>IEnumerator</c> runs the method as a coroutine.</item>
    /// </list>
    /// <code>
    /// [ConsoleMethod("give", "Adds an item to the player.")]
    /// static void Give(string item, int amount = 1) { ... }
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ConsoleMethodAttribute : UnityEngine.Scripting.PreserveAttribute // Preserve: keeps the method in IL2CPP builds with code stripping
    {
        /// <summary>Command name. Defaults to the method name in lower case.</summary>
        public string Name { get; }

        public string Description { get; }

        public string[] Aliases { get; set; }

        /// <summary>Only runs when the console's cheat policy allows cheats.</summary>
        public bool IsCheat { get; set; }

        public ConsoleMethodAttribute(string name = null, string description = null)
        {
            Name = name;
            Description = description;
        }
    }

    /// <summary>
    /// Exposes a static field or property as a console variable: <c>name</c> prints the value,
    /// <c>name value</c> sets it. <c>cvars</c> lists every variable.
    /// </summary>
    /// <remarks>
    /// Supported types: <c>int</c>, <c>float</c>, <c>double</c>, <c>bool</c>, <c>string</c> and enums.
    /// Properties without a setter, <c>readonly</c> fields and <see cref="ReadOnly"/> variables can only be read.
    /// <code>
    /// [ConsoleVariable("timescale", "Game speed.", Min = 0, Max = 10)]
    /// static float TimeScale { get => Time.timeScale; set => Time.timeScale = value; }
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
    public sealed class ConsoleVariableAttribute : UnityEngine.Scripting.PreserveAttribute // Preserve: keeps the member when stripping
    {
        /// <summary>Variable name. Defaults to the member name in lower case.</summary>
        public string Name { get; }

        public string Description { get; }

        /// <summary>Lowest value accepted for numeric variables (NaN = no limit).</summary>
        public double Min { get; set; } = double.NaN;

        /// <summary>Highest value accepted for numeric variables (NaN = no limit).</summary>
        public double Max { get; set; } = double.NaN;

        public bool ReadOnly { get; set; }

        /// <summary>Changing the value requires cheats (reading is always allowed).</summary>
        public bool IsCheat { get; set; }

        public ConsoleVariableAttribute(string name = null, string description = null)
        {
            Name = name;
            Description = description;
        }
    }
}
