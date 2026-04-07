using EldritchGames.EldritchLogger.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Tests
{
    public class LogSettingsTests
    {
        [Test]
        public void CategoryEnableDisable_WorksCorrectly()
        {
            var settings = ScriptableObject.CreateInstance<LogSettings>();
            settings.enabledCategories.Clear(); // Reset defaults

            Assert.IsFalse(settings.IsCategoryEnabled(LogCategory.UI));

            settings.enabledCategories.Add(LogCategory.UI);
            Assert.IsTrue(settings.IsCategoryEnabled(LogCategory.UI));

            settings.enabledCategories.Remove(LogCategory.UI);
            Assert.IsFalse(settings.IsCategoryEnabled(LogCategory.UI));
        }
    }
}