using System;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Core
{
    /// <summary>
    /// Identifies the subsystem a log entry belongs to.
    /// Built-in categories are exposed as static fields; any other name can be used as a
    /// custom category (register it in <see cref="Settings.LogSettings"/> to enable it).
    /// </summary>
    /// <remarks>
    /// Names compare case-insensitively. <c>default(LogCategory)</c> is <see cref="General"/>.
    /// </remarks>
    public readonly struct LogCategory : IEquatable<LogCategory>
    {
        private const string DefaultName = "General";

        public static readonly LogCategory General = new("General");
        public static readonly LogCategory Gameplay = new("Gameplay");
        public static readonly LogCategory UI = new("UI");
        public static readonly LogCategory Audio = new("Audio");
        public static readonly LogCategory Network = new("Network");
        public static readonly LogCategory AI = new("AI");
        public static readonly LogCategory Physics = new("Physics");
        public static readonly LogCategory Animation = new("Animation");
        public static readonly LogCategory Input = new("Input");

        /// <summary>Messages forwarded from Unity's own log (Debug.Log, errors, exceptions).</summary>
        public static readonly LogCategory Unity = new("Unity");

        /// <summary>The categories that ship with the logger.</summary>
        public static IReadOnlyList<LogCategory> BuiltIn { get; } = new[]
        {
            General, Gameplay, UI, Audio, Network, AI, Physics, Animation, Input, Unity
        };

        private readonly string name;

        /// <summary>The category name. Never null.</summary>
        public string Name => name ?? DefaultName;

        public LogCategory(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Category name must not be null or whitespace.", nameof(name));
            this.name = name.Trim();
        }

        /// <summary>Creates a category from any enum value, using the value's name.</summary>
        public static LogCategory From(Enum value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return new LogCategory(value.ToString());
        }

        public static implicit operator LogCategory(string name) => new(name);

        public bool Equals(LogCategory other) =>
            string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object obj) => obj is LogCategory other && Equals(other);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Name);

        public static bool operator ==(LogCategory left, LogCategory right) => left.Equals(right);

        public static bool operator !=(LogCategory left, LogCategory right) => !left.Equals(right);

        public override string ToString() => Name;
    }
}
