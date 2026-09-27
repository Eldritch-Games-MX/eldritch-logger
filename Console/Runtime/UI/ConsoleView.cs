using EldritchGames.EldritchLogger.Console.Settings;
using EldritchGames.EldritchLogger.Console.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EldritchGames.EldritchLogger.Console.UI
{
    /// <summary>
    /// uGUI/TextMeshPro console view. Lines are stored in a <see cref="ConsoleLogBuffer"/> and a
    /// fixed pool of text objects shows the newest ones; the pool is redrawn at most once per
    /// frame, however many lines arrive.
    /// </summary>
    public class ConsoleView : MonoBehaviour, IConsoleView, IThemeable
    {
        private const int DefaultPoolSize = 20;
        private const int DefaultBufferSize = 500;

        [Header("Prefabs")]
        [SerializeField] private TMP_Text logLinePrefab;
        [Header("References")]
        [SerializeField] private TMP_InputField commandInput;
        [SerializeField] private Button sendButton;
        [SerializeField] private Transform logContent;
        [SerializeField] private TMP_Text ghostText;
        [SerializeField] private Canvas canvas;
        [SerializeField] private CommandConsoleSettings settings;

        private readonly List<TMP_Text> lines = new();
        private ConsoleLogBuffer buffer;
        private IConsoleLogFormatter formatter;
        private TMP_InputField wiredInput;
        private Button wiredButton;
        private Scrollbar[] scrollbars;
        private ConsoleTheme theme;
        private int renderedVersion = -1;

        public event Action<string> OnCommandSubmitted;
        public event Action<string> OnInputChanged;

        public TMP_Text LogLinePrefab => logLinePrefab;
        public TMP_InputField CommandInput => commandInput;
        public TMP_Text GhostText => ghostText;
        public Button SendButton => sendButton;
        public ConsoleLogBuffer Buffer => buffer;

        /// <summary>The pooled line objects, oldest first.</summary>
        public IReadOnlyList<TMP_Text> Lines => lines;

        public bool IsVisible => canvas != null && canvas.enabled;

        private void Awake()
        {
            if (Application.isPlaying) Initialize();
        }

        private void Start()
        {
            if (Application.isPlaying && canvas != null)
                canvas.enabled = false; // the console starts hidden
        }

        private void LateUpdate()
        {
            if (buffer != null && buffer.Version != renderedVersion)
                Refresh();
        }

        private void OnDestroy() => UnwireInput();

        /// <summary>Sets references from code (tests, procedurally built UI) and initializes.</summary>
        public void Configure(TMP_Text prefab, Transform content, TMP_InputField input, TMP_Text ghost,
                              Canvas targetCanvas, Button button, IConsoleLogFormatter logFormatter = null,
                              CommandConsoleSettings consoleSettings = null)
        {
            logLinePrefab = prefab;
            logContent = content;
            commandInput = input;
            ghostText = ghost;
            canvas = targetCanvas;
            sendButton = button;
            settings = consoleSettings;
            formatter = logFormatter;
            buffer = null;
            Initialize();
        }

        public void SetConsoleLogFormatter(IConsoleLogFormatter logFormatter) => formatter = logFormatter;

        /// <summary>Creates the buffer and line pool and hooks up input. Safe to call more than once.</summary>
        public void Initialize()
        {
            formatter ??= new ConsoleLogFormatter(settings != null && settings.stripRichTextTags);

            if (buffer == null)
            {
                buffer = new ConsoleLogBuffer(settings != null ? Math.Max(1, settings.maxBufferedLines) : DefaultBufferSize);
                renderedVersion = -1;
                BuildPool(settings != null ? Math.Max(1, settings.poolSize) : DefaultPoolSize);
            }

            WireInput();
        }

        private void BuildPool(int size)
        {
            foreach (var line in lines)
                if (line != null) Destroy(line.gameObject);
            lines.Clear();

            if (logLinePrefab == null || logContent == null)
            {
                Debug.LogWarning("ConsoleView: logLinePrefab or logContent not set; lines will not be displayed.");
                return;
            }

            for (int i = 0; i < size; i++)
            {
                var line = Instantiate(logLinePrefab, logContent);
                line.gameObject.SetActive(false);
                lines.Add(line);
                if (theme != null) StyleLine(line, theme);
            }

            scrollbars = null;
        }

        private void WireInput()
        {
            if (commandInput != wiredInput)
            {
                UnwireInput();
                if (commandInput != null)
                {
                    commandInput.onSubmit.AddListener(HandleSubmit);
                    commandInput.onValueChanged.AddListener(HandleValueChanged);
                }
                wiredInput = commandInput;
            }

            if (sendButton != wiredButton)
            {
                if (wiredButton != null) wiredButton.onClick.RemoveListener(HandleButtonClick);
                if (sendButton != null) sendButton.onClick.AddListener(HandleButtonClick);
                wiredButton = sendButton;
            }
        }

        private void UnwireInput()
        {
            if (wiredInput != null)
            {
                wiredInput.onSubmit.RemoveListener(HandleSubmit);
                wiredInput.onValueChanged.RemoveListener(HandleValueChanged);
                wiredInput = null;
            }
            if (wiredButton != null)
            {
                wiredButton.onClick.RemoveListener(HandleButtonClick);
                wiredButton = null;
            }
        }

        private void HandleSubmit(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return;

            OnCommandSubmitted?.Invoke(input);
            commandInput.text = string.Empty;
            commandInput.ActivateInputField();
        }

        private void HandleValueChanged(string input) => OnInputChanged?.Invoke(input);

        private void HandleButtonClick()
        {
            if (commandInput != null) HandleSubmit(commandInput.text);
        }

        public void AppendLog(string message)
        {
            if (buffer == null) Initialize();
            buffer.Add(formatter.Format(message));
        }

        public void Clear()
        {
            if (buffer == null) Initialize();
            buffer.Clear();
        }

        /// <summary>Redraws the pool from the buffer immediately (normally done once per frame).</summary>
        public void Refresh()
        {
            if (buffer == null) return;

            int visible = Math.Min(buffer.Count, lines.Count);
            int first = buffer.Count - visible;

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line == null) continue;

                bool active = i < visible;
                if (active) line.text = buffer[first + i];
                if (line.gameObject.activeSelf != active) line.gameObject.SetActive(active);
            }

            renderedVersion = buffer.Version;
        }

        public void ToggleVisibility()
        {
            if (canvas == null) return;
            canvas.enabled = !canvas.enabled;
            if (canvas.enabled && commandInput != null) commandInput.ActivateInputField();
        }

        public void ShowGhostSuggestion(string suggestion)
        {
            if (ghostText == null || commandInput == null) return;

            var typed = commandInput.text;
            if (string.IsNullOrEmpty(typed) || string.IsNullOrEmpty(suggestion))
            {
                ghostText.text = string.Empty;
                return;
            }

            // Replace the token being typed with the suggestion, keeping earlier tokens as typed.
            bool endsWithSpace = char.IsWhiteSpace(typed[typed.Length - 1]);
            var tokens = typed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var keep = endsWithSpace ? tokens : tokens.Take(tokens.Length - 1);
            var prefix = string.Join(" ", keep);

            ghostText.text = string.IsNullOrEmpty(prefix) ? suggestion : prefix + " " + suggestion;
        }

        public void AcceptGhostSuggestion()
        {
            if (ghostText == null || commandInput == null || string.IsNullOrEmpty(ghostText.text)) return;

            commandInput.text = ghostText.text;
            commandInput.caretPosition = commandInput.text.Length;
            ghostText.text = string.Empty;
        }

        public void ApplyTheme(ConsoleTheme newTheme)
        {
            if (newTheme == null) return;
            theme = newTheme;

            foreach (var line in lines)
                if (line != null) StyleLine(line, newTheme);

            var colors = BuildColorBlock(newTheme);

            if (commandInput != null)
            {
                if (commandInput.textComponent != null) StyleText(commandInput.textComponent, newTheme, newTheme.textColor);
                if (commandInput.placeholder is TMP_Text placeholder)
                    StyleText(placeholder, newTheme, WithAlpha(newTheme.textColor, 0.5f));
                commandInput.colors = colors;
            }

            if (ghostText != null)
                StyleText(ghostText, newTheme, WithAlpha(newTheme.promptColor, 0.5f));

            if (sendButton != null)
            {
                sendButton.colors = colors;
                if (sendButton.targetGraphic != null) sendButton.targetGraphic.color = newTheme.promptColor;
                var label = sendButton.GetComponentInChildren<TMP_Text>(true);
                if (label != null) StyleText(label, newTheme, newTheme.textColor);
            }

            scrollbars ??= GetComponentsInChildren<Scrollbar>(true);
            foreach (var scrollbar in scrollbars)
            {
                if (scrollbar == null) continue;
                scrollbar.colors = colors;
                if (scrollbar.handleRect != null && scrollbar.handleRect.TryGetComponent<Image>(out var handle))
                    handle.color = newTheme.promptColor;
            }
        }

        private static void StyleLine(TMP_Text line, ConsoleTheme theme) => StyleText(line, theme, theme.textColor);

        private static void StyleText(TMP_Text text, ConsoleTheme theme, Color color)
        {
            text.color = color;
            text.fontSize = theme.fontSize;
            if (theme.fontAsset != null) text.font = theme.fontAsset;
        }

        private static Color WithAlpha(Color color, float alpha) => new(color.r, color.g, color.b, alpha);

        private static ColorBlock BuildColorBlock(ConsoleTheme theme) => new()
        {
            normalColor = theme.textColor,
            highlightedColor = theme.promptColor,
            pressedColor = theme.promptColor * 0.9f,
            selectedColor = theme.promptColor,
            disabledColor = WithAlpha(theme.textColor, 0.3f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };
    }
}
