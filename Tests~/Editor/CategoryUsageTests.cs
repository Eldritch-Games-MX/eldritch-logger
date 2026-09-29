using EldritchGames.EldritchLogger.EditorTools.CodeGen;
using NUnit.Framework;

namespace EldritchGames.EldritchLogger.Tests.Editor
{
    public class CategoryUsageTests
    {
        [Test]
        public void FindUnreferenced_LooksForStringsAndGeneratedIdentifiers()
        {
            var sources = new[]
            {
                "logger.Log(LogLevel.Info, \"economy\", \"bought\");",        // string literal, any case
                "logger.AtInfo(LogCategories.LootDrops).Log(\"x\");",           // generated field
                "enum GameCategory { Quests }"                                    // enum member
            };

            var unused = CategoryUsage.FindUnreferenced(new[] { "Economy", "Loot Drops", "Quests", "Crafting", "Craft" }, sources);

            Assert.That(unused, Is.EquivalentTo(new[] { "Crafting", "Craft" }));
        }

        [Test]
        public void FindUnreferenced_MatchesWholeNamesOnly()
        {
            var unused = CategoryUsage.FindUnreferenced(new[] { "AI", "Quest" }, new[] { "var questLog = MAIN_MENU; // \"Quests\"" });
            Assert.That(unused, Is.EquivalentTo(new[] { "AI", "Quest" }));
        }
    }
}
