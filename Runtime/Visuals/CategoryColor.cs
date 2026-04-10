using EldritchGames.EldritchLogger.Core;
using System;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Visuals
{
    [Serializable]
    public class CategoryColor
    {
        public LogCategory category;
        public Color color;

        public CategoryColor(LogCategory category, Color color)
        {
            this.category = category;
            this.color = color;
        }
    }
}