using EldritchGames.EldritchLogger.Core;
using NUnit.Framework;
using System;

namespace EldritchGames.EldritchLogger.Tests
{
    public class LogCategoryTests
    {
        private enum GameCategory { Loot, Quests }

        [Test]
        public void Equality_IsCaseInsensitive()
        {
            Assert.That(new LogCategory("gameplay"), Is.EqualTo(LogCategory.Gameplay));
            Assert.That(new LogCategory("GAMEPLAY").GetHashCode(), Is.EqualTo(LogCategory.Gameplay.GetHashCode()));
            Assert.That(new LogCategory("Loot") == new LogCategory("loot"), Is.True);
            Assert.That(LogCategory.UI != LogCategory.AI, Is.True);
        }

        [Test]
        public void Default_IsGeneral()
        {
            Assert.That(default(LogCategory), Is.EqualTo(LogCategory.General));
            Assert.That(default(LogCategory).Name, Is.EqualTo("General"));
        }

        [Test]
        public void ImplicitConversion_FromString()
        {
            LogCategory category = "Loot";
            Assert.That(category.Name, Is.EqualTo("Loot"));
        }

        [Test]
        public void From_Enum_UsesValueName()
        {
            Assert.That(LogCategory.From(GameCategory.Quests).Name, Is.EqualTo("Quests"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_RejectsBlankNames(string name)
        {
            Assert.Throws<ArgumentException>(() => new LogCategory(name));
        }

        [Test]
        public void BuiltIn_ContainsTenCategories()
        {
            Assert.That(LogCategory.BuiltIn.Count, Is.EqualTo(10));
            Assert.That(LogCategory.BuiltIn, Does.Contain(LogCategory.Unity));
            Assert.That(LogCategory.BuiltIn, Does.Contain(LogCategory.Input));
        }
    }
}
