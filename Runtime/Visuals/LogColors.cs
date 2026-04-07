using UnityEngine;

namespace EldritchGames.EldritchLogger.Visuals
{
    public static class LogColors
    {
        public static Color GetColor(LogCategory category)
        {
            return category switch
            {
                LogCategory.General => Color.white,
                LogCategory.Gameplay => Color.green,
                LogCategory.UI => Color.blue,
                LogCategory.Audio => Color.yellow,
                LogCategory.Network => Color.magenta,
                LogCategory.AI => Color.cyan,
                LogCategory.Physics => new Color(1f, 0.5f, 0f),   // orange
                LogCategory.Animation => new Color(0.5f, 0f, 0.5f), // purple
                LogCategory.Input => new Color(0f, 0.5f, 0f),   // dark green
                _ => Color.white
            };
        }

        public static string GetColorString(LogCategory category)
        {
            Color color = GetColor(category);
            return $"#{ColorUtility.ToHtmlStringRGB(color)}";
        }
    }
}