using EldritchGames.EldritchLogger.Console.EditorTools;
using NUnit.Framework;
using UnityEditor;

namespace EldritchGames.EldritchLogger.Console.Tests.Editor
{
    public class ConsoleDefinesTests
    {
        [Test]
        public void SetDefine_AddsAndRemovesTheDisableDefine_WithoutTouchingOthers()
        {
            var target = UnityEditor.Build.NamedBuildTarget.Standalone;
            PlayerSettings.GetScriptingDefineSymbols(target, out var original);
            try
            {
                PlayerSettings.SetScriptingDefineSymbols(target, new[] { "SOME_OTHER_DEFINE" });

                ConsoleDefines.SetDefine(target, true);
                Assert.That(ConsoleDefines.HasDefine(target), Is.True);

                ConsoleDefines.SetDefine(target, false);
                Assert.That(ConsoleDefines.HasDefine(target), Is.False);

                PlayerSettings.GetScriptingDefineSymbols(target, out var after);
                Assert.That(after, Is.EqualTo(new[] { "SOME_OTHER_DEFINE" }));
            }
            finally
            {
                PlayerSettings.SetScriptingDefineSymbols(target, original);
            }
        }
    }
}
