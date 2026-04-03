using UnityEngine;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Setttings
{
    [CreateAssetMenu(fileName = "LogSettings", menuName = "Eldritch Logger/Log Settings", order = 0)]
    public class LogSettings : ScriptableObject
    {
        public LogLevel logLevel = LogLevel.Debug;
        public List<LogCategory> enabledCategories = new List<LogCategory>
    {
        LogCategory.General,
        LogCategory.Network,
        LogCategory.UI,
        LogCategory.Audio,
        LogCategory.Physics,
        LogCategory.AI,
        LogCategory.Animation,
        LogCategory.Input,
    };

        public bool IsCategoryEnabled(LogCategory category) => enabledCategories.Contains(category);
    }
}