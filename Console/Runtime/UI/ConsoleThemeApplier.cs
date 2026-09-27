using EldritchGames.EldritchLogger.Console.Settings;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EldritchGames.EldritchLogger.Console.UI;

namespace EldritchGames.EldritchLogger.Console.Loader
{
    [ExecuteAlways]
    public class ConsoleThemeApplier : MonoBehaviour, IConsoleThemeApplier
    {
        [SerializeField] private ConsoleTheme theme;
        [SerializeField] private Image rootPanelImage;
        [SerializeField] private ConsoleView consoleView;

        private void Awake()
        {
            InitializeTheme();
        }

        private void OnValidate()
        {
            InitializeTheme();
        }

        public void ApplyTheme()
        {
            var colorBlock = BuildColorBlock();

            // Main background
            if (rootPanelImage != null)
                rootPanelImage.color = theme.backgroundColor;

            ApplyToLogEntries();
            ApplyToInput(colorBlock);
            ApplyToSubmitButton();
        }

        public void ApplyTheme(ConsoleTheme newTheme)
        {
            theme = newTheme;
            ApplyTheme();
        }

        private void InitializeTheme()
        {
            if (theme == null || consoleView == null)
            {
                Debug.LogWarning("ConsoleThemeApplier: Theme or ConsoleView reference is missing.");
                return;
            }
            ApplyTheme();
        }

        private void ApplyToLogEntries()
        {
            TMP_Text prefab = consoleView.LogLinePrefab;
            if (prefab != null)
            {
                prefab.color = theme.textColor;
                prefab.fontSize = theme.fontSize;
            }

            // Update existing entries
            foreach (var logLine in consoleView.GetComponentsInChildren<TMP_Text>(true))
            {
                logLine.color = theme.textColor;
                logLine.fontSize = theme.fontSize;
            }
        }

        private void ApplyToInput(ColorBlock colorBlock)
        {
            if (consoleView.CommandInput.TryGetComponent<TMP_InputField>(out var inputField))
            {
                // Text and placeholder
                inputField.textComponent.color = theme.textColor;
                if (inputField.placeholder is TMP_Text placeholder)
                    placeholder.color = new Color(theme.textColor.r, theme.textColor.g, theme.textColor.b, 0.5f);

                // Interaction colors
                if (inputField.TryGetComponent<Selectable>(out var selectable))
                {
                    selectable.colors = colorBlock;
                }

                // Scrollbars
                foreach (var scrollbar in consoleView.GetComponentsInChildren<Scrollbar>(true))
                {
                    scrollbar.colors = BuildColorBlock();

                    if (scrollbar.handleRect != null)
                    {
                        if (scrollbar.handleRect.TryGetComponent<Image>(out var handleImage))
                            handleImage.color = theme.promptColor;
                    }
                }

                // Ghost text
                if (consoleView.GhostText != null)
                    consoleView.GhostText.color = theme.promptColor;
            }
        }

        private void ApplyToSubmitButton()
        {
            if (consoleView.SendButton != null)
            {
                if (consoleView.SendButton.TryGetComponent<Button>(out var button))
                    button.colors = BuildColorBlock();

                if (consoleView.SendButton.TryGetComponent<Image>(out var image))
                    image.color = theme.promptColor;

                TMP_Text buttonText = consoleView.SendButton.GetComponentInChildren<TMP_Text>();
                if (buttonText != null)
                {
                    buttonText.color = theme.textColor;
                }
            }
        }

        private ColorBlock BuildColorBlock()
        {
            ColorBlock colors = new()
            {
                normalColor = theme.textColor,
                highlightedColor = theme.promptColor,
                pressedColor = theme.promptColor * 0.9f,
                selectedColor = theme.promptColor,
                disabledColor = new Color(theme.textColor.r, theme.textColor.g, theme.textColor.b, 0.3f),
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };
            return colors;
        }
    }
}
