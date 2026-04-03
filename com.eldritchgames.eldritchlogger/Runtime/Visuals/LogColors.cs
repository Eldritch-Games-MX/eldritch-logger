using UnityEngine;

namespace EldritchGames.EldritchLogger.Visuals
{
    public static class LogColors
    {
        public static string GetColor(LogCategory category)
        {
            return category switch
            {
                LogCategory.General => "#FFFFFF",
                LogCategory.Gameplay => "green",
                LogCategory.UI => "blue",
                LogCategory.Audio => "yellow",
                LogCategory.Network => "magenta",
                LogCategory.AI => "cyan",
                LogCategory.Physics => "orange",
                LogCategory.Animation => "purple",
                LogCategory.Input => "#008000", // Dark green
                _ => "#FFFFFF"
            };
        }
    }
}
