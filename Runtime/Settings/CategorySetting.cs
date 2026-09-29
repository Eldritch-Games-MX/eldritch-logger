using EldritchGames.EldritchLogger.Core;
using System;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Settings
{
    /// <summary>
    /// Per-category configuration. Built-in and custom categories use the same shape.
    /// </summary>
    [Serializable]
    public class CategorySetting
    {
        public string name;
        public Color color = Color.white;
        public bool enabled = true;

        public CategorySetting() { }

        public CategorySetting(string name, Color color, bool enabled = true)
        {
            this.name = name;
            this.color = color;
            this.enabled = enabled;
        }

        public LogCategory Category => new(name);

        public bool IsBuiltIn
        {
            get
            {
                foreach (var builtIn in LogCategory.BuiltIn)
                    if (string.Equals(builtIn.Name, name, StringComparison.OrdinalIgnoreCase))
                        return true;
                return false;
            }
        }
    }
}
