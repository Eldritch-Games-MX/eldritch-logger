using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.EditorTools.CodeGen;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace EldritchGames.EldritchLogger.Tests.Editor
{
    public class CategoryCodeGeneratorTests
    {
        [TestCase("Loot", "Loot")]
        [TestCase("loot drops", "LootDrops")]
        [TestCase("AI-Nav/Path", "AINavPath")]
        [TestCase("3D", "_3D")]
        [TestCase("class", "Class")]
        [TestCase("!!!", "Category")]
        public void ToIdentifier(string name, string expected)
        {
            Assert.That(CategoryCodeGenerator.ToIdentifier(name), Is.EqualTo(expected));
        }

        [Test]
        public void Generate_WritesOneFieldPerCategory_InANamespace()
        {
            var code = CategoryCodeGenerator.Generate(new[] { "General", "loot drops", "Loot-Drops", "LOOT DROPS", "All", "Say \"hi\"" },
                                                      "Game.Logging", "GameCategories");

            StringAssert.Contains("namespace Game.Logging", code);
            StringAssert.Contains("public static class GameCategories", code);
            StringAssert.Contains("public static readonly LogCategory General = new LogCategory(\"General\");", code);
            StringAssert.Contains("public static readonly LogCategory LootDrops = new LogCategory(\"loot drops\");", code);
            StringAssert.Contains("public static readonly LogCategory LootDrops2 = new LogCategory(\"Loot-Drops\");", code);
            StringAssert.DoesNotContain("\"LOOT DROPS\"", code, "names differing only by case are the same category");
            StringAssert.Contains("public static readonly LogCategory All2 = new LogCategory(\"All\");", code);
            StringAssert.Contains("new LogCategory(\"Say \\\"hi\\\"\")", code);
            StringAssert.Contains("public static readonly LogCategory[] All = { General, LootDrops, LootDrops2, All2, SayHi };", code);
        }

        [TestCase("Assets/Scripts/LogCategories.cs", true)]
        [TestCase("Assets\\Scripts\\LogCategories.cs", true)]
        [TestCase("Packages/foo/LogCategories.cs", false)]
        [TestCase("Assets/Scripts/LogCategories.txt", false)]
        [TestCase("Assets/../Outside.cs", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void OutputPath_MustBeACsFileUnderAssets(string path, bool expected)
        {
            Assert.That(CategoryCodeGeneration.IsValidOutputPath(path), Is.EqualTo(expected));
        }

        [Test]
        public void WriteIfChanged_WritesOnlyWhenTheContentDiffers()
        {
            var directory = Path.Combine(Path.GetTempPath(), "EldritchCodegen_" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "Nested", "LogCategories.cs");
            try
            {
                Assert.That(CategoryCodeGeneration.WriteIfChanged(path, "v1"), Is.True, "creates the directory and file");
                Assert.That(CategoryCodeGeneration.WriteIfChanged(path, "v1"), Is.False, "unchanged: no write, no recompile");
                Assert.That(CategoryCodeGeneration.WriteIfChanged(path, "v2"), Is.True);
                Assert.That(File.ReadAllText(path), Is.EqualTo("v2"));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Test]
        public void Generate_WithoutNamespace_UsesTheGlobalNamespace()
        {
            var code = CategoryCodeGenerator.Generate(Array.Empty<string>(), "", "");

            StringAssert.DoesNotContain("namespace", code);
            StringAssert.Contains("public static class LogCategories", code);
            StringAssert.Contains("public static readonly LogCategory[] All = { };", code);
        }
    }
}
