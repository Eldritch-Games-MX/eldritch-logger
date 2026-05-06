using UnityEngine;
using EldritchGames.EldritchLogger.Settings;
using EldritchGames.EldritchLogger.Core;
using System;

namespace EldritchGames.EldritchLogger.Visuals
{
    public static class LogColors
    {
        public static Color GetColor(LogCategory category, LogSettings settings)
        {
            return settings != null ? settings.GetCategoryColor(category) : Color.white;
        }

        public static string GetColorString(LogCategory category, LogSettings settings)
        {
            Color color = GetColor(category, settings);
            return $"#{ColorUtility.ToHtmlStringRGB(color)}";
        }

        public static Color GetColor(string categoryName, LogSettings settings)
        {
            if (settings == null) return Color.white;
            if (Enum.TryParse<LogCategory>(categoryName, out var cat))
                return GetColor(cat, settings);
            return settings.GetCustomCategoryColor(categoryName);
        }

        public static string GetColorString(string categoryName, LogSettings settings)
        {
            Color color = GetColor(categoryName, settings);
            return $"#{ColorUtility.ToHtmlStringRGB(color)}";
        }
    }
}
