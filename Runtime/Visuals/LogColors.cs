using UnityEngine;
using EldritchGames.EldritchLogger.Settings;
using EldritchGames.EldritchLogger.Core;

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
    }
}
