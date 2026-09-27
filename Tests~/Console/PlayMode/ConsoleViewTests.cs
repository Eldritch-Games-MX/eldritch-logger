using EldritchGames.EldritchLogger.Console.Settings;
using EldritchGames.EldritchLogger.Console.Themes;
using EldritchGames.EldritchLogger.Console.UI;
using NUnit.Framework;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace EldritchGames.EldritchLogger.Console.Tests.PlayMode
{
    public class ConsoleViewTests
    {
        private GameObject root;
        private ConsoleView view;
        private TMP_InputField input;
        private TMP_Text ghost;
        private Canvas canvas;
        private CommandConsoleSettings settings;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Console", typeof(RectTransform));
            canvas = root.AddComponent<Canvas>();

            var content = new GameObject("Content", typeof(RectTransform)).transform;
            content.SetParent(root.transform);

            var prefab = new GameObject("LinePrefab", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            prefab.transform.SetParent(root.transform);

            input = new GameObject("Input", typeof(RectTransform)).AddComponent<TMP_InputField>();
            input.transform.SetParent(root.transform);
            input.textComponent = new GameObject("InputText", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            input.textComponent.transform.SetParent(input.transform);

            ghost = new GameObject("Ghost", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            ghost.transform.SetParent(root.transform);

            settings = ScriptableObject.CreateInstance<CommandConsoleSettings>();
            settings.poolSize = 3;
            settings.maxBufferedLines = 10;

            view = root.AddComponent<ConsoleView>();
            view.Configure(prefab, content, input, ghost, canvas, null, consoleSettings: settings);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(root);
            Object.Destroy(settings);
        }

        [Test]
        public void Pool_HasPoolSizeLines()
        {
            Assert.That(view.Lines.Count, Is.EqualTo(3));
            Assert.That(view.Lines.All(l => !l.gameObject.activeSelf), Is.True);
        }

        [Test]
        public void Refresh_ShowsTheNewestLines()
        {
            for (int i = 0; i < 5; i++) view.AppendLog("line " + i);
            view.Refresh();

            Assert.That(view.Lines.Select(l => l.text), Is.EqualTo(new[] { "line 2", "line 3", "line 4" }));
            Assert.That(view.Lines.All(l => l.gameObject.activeSelf), Is.True);
        }

        [UnityTest]
        public IEnumerator ManyLinesInOneFrame_AreRenderedOnce_AtFrameEnd()
        {
            for (int i = 0; i < 1000; i++) view.AppendLog("burst " + i);
            Assert.That(view.Lines[2].text, Is.Not.EqualTo("burst 999"), "not rendered synchronously");

            yield return null;

            Assert.That(view.Buffer.Count, Is.EqualTo(10));
            Assert.That(view.Lines[2].text, Is.EqualTo("burst 999"));
        }

        [Test]
        public void Clear_HidesEveryLine()
        {
            view.AppendLog("a");
            view.Clear();
            view.Refresh();

            Assert.That(view.Lines.All(l => !l.gameObject.activeSelf), Is.True);
        }

        [Test]
        public void ToggleVisibility_FlipsTheCanvas()
        {
            canvas.enabled = true;
            view.ToggleVisibility();
            Assert.That(view.IsVisible, Is.False);
            view.ToggleVisibility();
            Assert.That(view.IsVisible, Is.True);
        }

        [Test]
        public void GhostSuggestion_CompletesTheCurrentToken_AndCanBeAccepted()
        {
            input.text = "theme da";
            view.ShowGhostSuggestion("Dark");
            Assert.That(ghost.text, Is.EqualTo("theme Dark"));

            view.AcceptGhostSuggestion();
            Assert.That(input.text, Is.EqualTo("theme Dark"));
            Assert.That(ghost.text, Is.Empty);

            input.text = "theme ";
            view.ShowGhostSuggestion("Light");
            Assert.That(ghost.text, Is.EqualTo("theme Light"));

            view.ShowGhostSuggestion(null);
            Assert.That(ghost.text, Is.Empty);
        }

        [Test]
        public void SubmittingInput_RaisesTheEvent()
        {
            string submitted = null;
            view.OnCommandSubmitted += s => submitted = s;

            input.onSubmit.Invoke("help");

            Assert.That(submitted, Is.EqualTo("help"));
            Assert.That(input.text, Is.Empty);
        }

        [Test]
        public void ApplyTheme_StylesLines()
        {
            var theme = ScriptableObject.CreateInstance<ConsoleTheme>();
            theme.textColor = Color.magenta;
            theme.fontSize = 22;

            view.ApplyTheme(theme);

            Assert.That(view.Lines.All(l => l.color == Color.magenta && Mathf.Approximately(l.fontSize, 22)), Is.True);
            Object.Destroy(theme);
        }
    }

    public class ConsoleThemeApplierTests
    {
        [Test]
        public void AppliesToThemeableChildren_AndTheRootPanel()
        {
            var root = new GameObject("Root", typeof(RectTransform));
            try
            {
                root.SetActive(false); // configure before Awake
                var panel = root.AddComponent<Image>();
                var child = new GameObject("Accent", typeof(RectTransform));
                child.transform.SetParent(root.transform);
                var accentImage = child.AddComponent<Image>();
                child.AddComponent<ThemeableGraphic>();

                var applier = root.AddComponent<ConsoleThemeApplier>();
                typeof(ConsoleThemeApplier).GetField("rootPanelImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .SetValue(applier, panel);
                root.SetActive(true);

                var theme = ScriptableObject.CreateInstance<ConsoleTheme>();
                theme.backgroundColor = Color.black;
                theme.textColor = Color.yellow;

                applier.CollectThemeables();
                applier.ApplyTheme(theme);

                Assert.That(applier.CurrentTheme, Is.SameAs(theme));
                Assert.That(panel.color, Is.EqualTo(Color.black));
                Assert.That(accentImage.color, Is.EqualTo(Color.yellow));
                Object.Destroy(theme);
            }
            finally
            {
                Object.Destroy(root);
            }
        }
    }
}
