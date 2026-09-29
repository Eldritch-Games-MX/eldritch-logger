using EldritchGames.EldritchLogger.EditorTools.LogViewer;
using NUnit.Framework;
using System.Linq;

namespace EldritchGames.EldritchLogger.Tests.Editor
{
    public class StackTraceLinksTests
    {
        [Test]
        public void Parse_FindsUnityAndDotNetLocations()
        {
            const string trace =
                "Player:Die () (at Assets/Scripts/Player.cs:42)\n" +
                "UnityEngine.Debug:LogError (object)\n" +
                "  at Enemy.Attack () [0x00001] in C:\\Game\\Assets\\Scripts\\Enemy.cs:17\r\n" +
                "  at Boss.Run () in /home/dev/Game/Assets/Boss.cs:line 8\n" +
                "\n";

            var frames = StackTraceLinks.Parse(trace);

            Assert.That(frames.Count, Is.EqualTo(4), "blank lines are skipped");
            Assert.That((frames[0].FilePath, frames[0].Line), Is.EqualTo(("Assets/Scripts/Player.cs", 42)));
            Assert.That(frames[1].FilePath, Is.Null);
            Assert.That((frames[2].FilePath, frames[2].Line), Is.EqualTo((@"C:\Game\Assets\Scripts\Enemy.cs", 17)));
            Assert.That((frames[3].FilePath, frames[3].Line), Is.EqualTo(("/home/dev/Game/Assets/Boss.cs", 8)));
            Assert.That(frames.All(f => !f.Text.EndsWith("\r")), Is.True);
        }

        [Test]
        public void Parse_HandlesEmptyInput()
        {
            Assert.That(StackTraceLinks.Parse(null), Is.Empty);
            Assert.That(StackTraceLinks.Parse(""), Is.Empty);
        }

        [TestCase("Assets/Scripts/Player.cs", "C:/Game", "Assets/Scripts/Player.cs")]
        [TestCase(@"C:\Game\Assets\Scripts\Player.cs", "C:/Game", "Assets/Scripts/Player.cs")]
        [TestCase("Packages/com.x/Runtime/A.cs", "C:/Game", "Packages/com.x/Runtime/A.cs")]
        [TestCase("/build-machine/work/Assets/Scripts/Player.cs", "C:/Game", "Assets/Scripts/Player.cs")]
        public void ToProjectPath(string path, string root, string expected)
        {
            Assert.That(StackTraceLinks.ToProjectPath(path, root), Is.EqualTo(expected));
        }
    }
}
