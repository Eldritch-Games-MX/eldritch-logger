using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Pipeline;
using EldritchGames.EldritchLogger.Settings;
using NUnit.Framework;
using System;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Tests
{
    public class SettingsLogFilterTests
    {
        [Test]
        public void SettingsFilter_RejectsUnknownCategories_AndNullSettings()
        {
            var settings = ScriptableObject.CreateInstance<LogSettings>();
            try
            {
                var filter = new SettingsLogFilter(settings);
                Assert.That(filter.IsEnabled(LogLevel.Critical, "NeverRegistered"), Is.False);
                Assert.Throws<ArgumentNullException>(() => new SettingsLogFilter(null));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void CategoryOverrides_TurnOnAndOffCleanly()
        {
            var settings = ScriptableObject.CreateInstance<Settings.LogSettings>();
            try
            {
                var filter = new SettingsLogFilter(settings);
                Assert.That(filter.IsCategoryEnabled(LogCategory.AI), Is.True, "no overrides: settings decide");

                filter.SetCategoryOverride(LogCategory.AI, false);
                Assert.That(filter.IsCategoryEnabled(LogCategory.AI), Is.False);

                filter.SetCategoryOverride(LogCategory.AI, null); // last override removed
                Assert.That(filter.IsCategoryEnabled(LogCategory.AI), Is.True);

                filter.SetCategoryOverride(LogCategory.UI, false);
                filter.ClearOverrides();
                Assert.That(filter.IsCategoryEnabled(LogCategory.UI), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }
    }
}
