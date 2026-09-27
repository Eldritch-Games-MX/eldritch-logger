using EldritchGames.EldritchLogger.Console.Settings;
using EldritchGames.EldritchLogger.Console.Loader;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EldritchGames.EldritchLogger.Console.UI;

namespace EldritchGames.EldritchLogger.Console.Tests.PlayMode.UI
{
    // -------------------------------
    // Unit-style tests (logic only)
    // -------------------------------
    [TestFixture]
    public class ConsoleViewUnitTests
    {
        private ConsoleView consoleView;
        private TMP_InputField inputField;
        private TMP_Text ghostText;
        private Canvas canvas;

        [SetUp]
        public void SetUp()
        {
            // Dependencies
            inputField = new GameObject("Input").AddComponent<TMP_InputField>();
            var textGO = new GameObject("InputText");
            inputField.textComponent = textGO.AddComponent<TextMeshProUGUI>();

            ghostText = new GameObject("GhostText").AddComponent<TextMeshProUGUI>();
            canvas = new GameObject("Canvas").AddComponent<Canvas>();

            // ConsoleView with minimal config
            consoleView = new GameObject("ConsoleView").AddComponent<ConsoleView>();
            consoleView.Configure(prefab: null, content: null, input: inputField,
                                  ghost: ghostText, c: canvas, button: null, formatter: null);
            // No Initialize() here — unit tests focus on pure logic
        }

        [Test]
        public void ShowGhostSuggestion_SetsGhostText()
        {
            inputField.text = "hel";
            consoleView.ShowGhostSuggestion("hello");
            Assert.AreEqual("hello", ghostText.text);
        }

        [Test]
        public void AcceptGhostSuggestion_ReplacesInputText()
        {
            ghostText.text = "help";
            inputField.text = "";
            consoleView.AcceptGhostSuggestion();
            Assert.AreEqual("help", inputField.text);
            Assert.IsEmpty(ghostText.text);
        }

        [Test]
        public void SetVisibility_TogglesCanvas()
        {
            bool initial = canvas.enabled;
            consoleView.SetVisibility();
            Assert.AreNotEqual(initial, canvas.enabled);
        }
    }

    // -------------------------------
    // Integration-style tests (Awake + pool + events)
    // -------------------------------
    [TestFixture]
    public class ConsoleViewIntegrationTests
    {
        private ConsoleView consoleView;
        private TMP_InputField inputField;
        private TMP_Text ghostText;
        private Canvas canvas;
        private Button sendButton;
        private TMP_Text prefabText;
        private Transform logContent;
        private CommandConsoleSettings settings;

        [SetUp]
        public void SetUp()
        {
            prefabText = new GameObject("LogLinePrefab").AddComponent<TextMeshProUGUI>();
            logContent = new GameObject("LogContent").transform;

            inputField = new GameObject("Input").AddComponent<TMP_InputField>();
            inputField.textComponent = new GameObject("InputText").AddComponent<TextMeshProUGUI>();

            ghostText = new GameObject("GhostText").AddComponent<TextMeshProUGUI>();
            canvas = new GameObject("Canvas").AddComponent<Canvas>();
            sendButton = new GameObject("SendButton").AddComponent<Button>();

            settings = ScriptableObject.CreateInstance<CommandConsoleSettings>();
            settings.poolSize = 3;

            consoleView = new GameObject("ConsoleView").AddComponent<ConsoleView>();
            consoleView.Configure(prefabText, logContent, inputField, ghostText, canvas, sendButton,
                                  formatter: new ConsoleLogFormatter());

            typeof(ConsoleView).GetField("settings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(consoleView, settings);

            consoleView.Initialize();
        }

        [Test]
        public void AppendLog_AddsMessageToLog()
        {
            consoleView.AppendLog("Hello World");
            var child = logContent.GetComponentInChildren<TMP_Text>(true);
            Assert.AreEqual("Hello World", child.text);
        }

        [Test]
        public void AppendLog_StripsRichTextTags_WhenEnabled()
        {
            settings.stripRichTextTags = true;
            consoleView.AppendLog("<color=red>Error:</color> Something went wrong");

            var child = logContent.GetComponentInChildren<TMP_Text>(true);
            Assert.AreEqual("Error: Something went wrong", child.text);
        }

        [Test]
        public void AppendLog_KeepsRichTextTags_WhenDisabled()
        {
            settings.stripRichTextTags = false;
            consoleView.AppendLog("<color=red>Error:</color> Something went wrong");

            var child = logContent.GetComponentInChildren<TMP_Text>(true);
            StringAssert.Contains("<color=red>", child.text);
        }
    }
}
