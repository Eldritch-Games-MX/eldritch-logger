using EldritchGames.EldritchLogger.Console.Settings;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EldritchGames.EldritchLogger.Console.Loader;
using System.Reflection;
using EldritchGames.EldritchLogger.Console.UI;

namespace EldritchGames.EldritchLogger.Console.Tests.PlayMode.UI
{
    [TestFixture]
    public class ConsoleThemeApplierTests
    {
        private ConsoleThemeApplier applier;
        private ConsoleTheme theme;
        private ConsoleView consoleView;
        private Image rootPanelImage;
        private TMP_InputField inputField;
        private Button sendButton;
        private TMP_Text ghostText;

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("ThemeApplier");
            applier = go.AddComponent<ConsoleThemeApplier>();

            theme = ScriptableObject.CreateInstance<ConsoleTheme>();
            theme.backgroundColor = Color.black;
            theme.textColor = Color.green;
            theme.promptColor = Color.cyan;
            theme.fontSize = 18;

            rootPanelImage = new GameObject("RootPanel").AddComponent<Image>();

            var consoleGO = new GameObject("ConsoleView");
            consoleView = consoleGO.AddComponent<ConsoleView>();

            var prefabGO = new GameObject("LogLinePrefab");
            var prefabText = prefabGO.AddComponent<TextMeshProUGUI>();
            typeof(ConsoleView).GetField("logLinePrefab", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(consoleView, prefabText);

            var logContentGO = new GameObject("LogContent");
            typeof(ConsoleView).GetField("logContent", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(consoleView, logContentGO.transform);

            inputField = new GameObject("Input").AddComponent<TMP_InputField>();
            var textGO = new GameObject("InputText");
            inputField.textComponent = textGO.AddComponent<TextMeshProUGUI>();
            typeof(ConsoleView).GetField("commandInput", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(consoleView, inputField);

            var sendGO = new GameObject("SendButton");
            sendGO.AddComponent<Image>();
            sendButton = sendGO.AddComponent<Button>();
            var buttonTextGO = new GameObject("ButtonText");
            buttonTextGO.transform.SetParent(sendGO.transform);
            buttonTextGO.AddComponent<TextMeshProUGUI>();
            typeof(ConsoleView).GetField("sendButton", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(consoleView, sendButton);

            ghostText = new GameObject("GhostText").AddComponent<TextMeshProUGUI>();
            typeof(ConsoleView).GetField("ghostText", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(consoleView, ghostText);

            typeof(ConsoleThemeApplier).GetField("theme", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(applier, theme);
            typeof(ConsoleThemeApplier).GetField("rootPanelImage", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(applier, rootPanelImage);
            typeof(ConsoleThemeApplier).GetField("consoleView", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(applier, consoleView);

            applier.SendMessage("Awake");
        }

        [Test]
        public void ApplyTheme_SetsRootPanelColor()
        {
            Assert.AreEqual(theme.backgroundColor, rootPanelImage.color);
        }

        [Test]
        public void ApplyTheme_UpdatesLogLinePrefab()
        {
            Assert.AreEqual(theme.textColor, consoleView.LogLinePrefab.color);
            Assert.AreEqual(theme.fontSize, consoleView.LogLinePrefab.fontSize);
        }

        [Test]
        public void ApplyTheme_UpdatesInputFieldColors()
        {
            Assert.AreEqual(theme.textColor, inputField.textComponent.color);
            if (inputField.placeholder is TMP_Text placeholder)
            {
                Assert.AreEqual(0.5f, placeholder.color.a, 0.01f);
            }
        }

        [Test]
        public void ApplyTheme_UpdatesGhostTextColor()
        {
            Assert.AreEqual(theme.promptColor, ghostText.color);
        }

        [Test]
        public void ApplyTheme_UpdatesSendButtonColors()
        {
            var buttonColors = sendButton.colors;
            Assert.AreEqual(theme.textColor, buttonColors.normalColor);
            Assert.AreEqual(theme.promptColor, buttonColors.highlightedColor);

            var image = sendButton.GetComponent<Image>();
            Assert.AreEqual(theme.promptColor, image.color);

            var buttonText = sendButton.GetComponentInChildren<TMP_Text>();
            Assert.AreEqual(theme.textColor, buttonText.color);
        }
    }
}
