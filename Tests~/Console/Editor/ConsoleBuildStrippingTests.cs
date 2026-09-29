using EldritchGames.EldritchLogger.Console.EditorTools;
using EldritchGames.EldritchLogger.Console.Settings;
using EldritchGames.EldritchLogger.Console.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EldritchGames.EldritchLogger.Console.Tests.Editor
{
    public class ConsoleBuildStrippingTests
    {
        private Scene scene;
        private CommandConsoleSettings settings;

        [SetUp]
        public void SetUp()
        {
            // A fresh, empty scene (additive scenes are refused while the runner's untitled scene is unsaved).
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            settings = ScriptableObject.CreateInstance<CommandConsoleSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var root in scene.GetRootGameObjects())
                Object.DestroyImmediate(root);
            Object.DestroyImmediate(settings);
        }

        /// <summary>Creates Root/Bootstrap with the bootstrap's consoleRoot pointing at Root.</summary>
        private GameObject CreateConsole(ConsoleAvailability availability)
        {
            settings.availability = availability;

            var root = new GameObject("Console Root");
            SceneManager.MoveGameObjectToScene(root, scene);
            var child = new GameObject("Bootstrap");
            child.transform.SetParent(root.transform);
            var bootstrap = child.AddComponent<ConsoleBootstrap>();

            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("settings").objectReferenceValue = settings;
            serialized.FindProperty("consoleRoot").objectReferenceValue = root;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        [Test]
        public void ReleaseBuilds_RemoveDevelopmentOnlyConsoles_WithTheirRoot()
        {
            var root = CreateConsole(ConsoleAvailability.DevelopmentBuilds);

            int removed = ConsoleBuildProcessor.StripUnavailableConsoles(scene, development: false, disabledByDefine: false);

            Assert.That(removed, Is.EqualTo(1));
            Assert.That(root == null, Is.True, "the whole console root is destroyed, not just the bootstrap object");
        }

        [Test]
        public void DevelopmentBuilds_KeepDevelopmentConsoles()
        {
            var root = CreateConsole(ConsoleAvailability.DevelopmentBuilds);

            Assert.That(ConsoleBuildProcessor.StripUnavailableConsoles(scene, development: true, disabledByDefine: false), Is.EqualTo(0));
            Assert.That(root != null, Is.True);
        }

        [Test]
        public void TheDisableDefine_RemovesEvenAlwaysAvailableConsoles()
        {
            var root = CreateConsole(ConsoleAvailability.Always);

            ConsoleBuildProcessor.StripUnavailableConsoles(scene, development: true, disabledByDefine: true);

            Assert.That(root == null, Is.True);
        }

        [Test]
        public void AlwaysAvailableConsoles_ShipInReleaseBuilds_WithAWarning()
        {
            var root = CreateConsole(ConsoleAvailability.Always);

            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("ships the in-game console in a release build"));
            Assert.That(ConsoleBuildProcessor.StripUnavailableConsoles(scene, development: false, disabledByDefine: false), Is.EqualTo(0));
            Assert.That(root != null, Is.True);
        }

        [Test]
        public void EditorOnlyConsoles_AreRemovedFromEveryBuild()
        {
            var root = CreateConsole(ConsoleAvailability.EditorOnly);

            ConsoleBuildProcessor.StripUnavailableConsoles(scene, development: true, disabledByDefine: false);

            Assert.That(root == null, Is.True);
        }
    }
}
