using System;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Visuals
{
    [Serializable]
    public class CustomCategoryEntry
    {
        public string name;
        public Color color;
        public bool enabled;

        public CustomCategoryEntry(string name, Color color)
        {
            this.name = name;
            this.color = color;
            this.enabled = true;
        }
    }
}
