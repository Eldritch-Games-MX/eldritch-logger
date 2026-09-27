using EldritchGames.EldritchLogger.Console.Loader;
using EldritchGames.EldritchLogger.Console.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EldritchGames.EldritchLogger.Console.UI
{
    public class ConsoleView : MonoBehaviour, IConsoleView
    {
        [Header("Prefabs")]
        [SerializeField] private TMP_Text logLinePrefab;
        [Header("References")]
        [SerializeField] private TMP_InputField commandInput;
        [SerializeField] private Button sendButton;
        [SerializeField] private Transform logContent;
        [SerializeField] private TMP_Text ghostText;
        [SerializeField] private Canvas canvas;
        [SerializeField] private CommandConsoleSettings settings;

        private readonly int defaultPoolSize = 20;
        private readonly Queue<TMP_Text> logEntryPool = new();
        private IConsoleLogFormatter formatter;

        public event Action<string> OnCommandSubmitted;
        public event Action<string> OnInputChanged;

        public TMP_Text LogLinePrefab => logLinePrefab;
        public TMP_InputField CommandInput => commandInput;
        public TMP_Text GhostText => ghostText;
        public Button SendButton => sendButton;

        private void Awake()
        {
            Initialize();
        }

        private void Start()
        {
            SetVisibility();
        }

        public void Configure(TMP_Text prefab, Transform content, TMP_InputField input,
                              TMP_Text ghost, Canvas c, Button button, IConsoleLogFormatter formatter)
        {
            logLinePrefab = prefab;
            logContent = content;
            commandInput = input;
            ghostText = ghost;
            canvas = c;
            sendButton = button;
            this.formatter = formatter;
        }

        public void SetConsoleLogFormatter(IConsoleLogFormatter formatter)
        {
            this.formatter = formatter;
        }

        public void Initialize()
        {
            // Initialize default formatter if not set via inspector or Configure
            formatter ??= new ConsoleLogFormatter();

            if (logLinePrefab == null || logContent == null)
            {
                Debug.LogWarning("ConsoleView: logLinePrefab or logContent not set, skipping pool init.");
            } else if(settings == null)
            {
                Debug.LogWarning("ConsoleView: settings not set, setting default of 20.");
                InstantiatePool(defaultPoolSize);
            } else if(settings.poolSize <= 0)
            {
                Debug.LogWarning("ConsoleView: poolSize in settings is not positive, setting default of 20.");
                InstantiatePool(defaultPoolSize);
            }
            else
            {
                InstantiatePool(settings.poolSize);
            }

            if (commandInput != null)
            {
                commandInput.onSubmit.AddListener(HandleCommandSubmit);
                commandInput.onValueChanged.AddListener(input => OnInputChanged?.Invoke(input));
            }

            if (sendButton != null)
            {
                sendButton.onClick.AddListener(HandleButtonClick);
            }
        }

        private void HandleCommandSubmit(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return;

            OnCommandSubmitted?.Invoke(input);
            commandInput.text = string.Empty;
        }

        private void HandleButtonClick()
        {
            var input = commandInput.text;
            HandleCommandSubmit(input);
        }

        private void InstantiatePool(int poolSize)
        {
            for (int i = 0; i < poolSize; i++)
            {
                var entry = Instantiate(logLinePrefab, logContent);
                entry.gameObject.SetActive(false);
                logEntryPool.Enqueue(entry);
            }
        }

        public void AppendLog(string message)
        {
            if (formatter != null)
            {
                message = formatter.Format(settings, message);
            }

            TMP_Text line;
            if (logEntryPool.Count > 0)
            {
                line = logEntryPool.Dequeue();
                line.gameObject.SetActive(true);
            }
            else
            {
                // recycle oldest
                line = logContent.GetChild(0).GetComponent<TMP_Text>();
                line.transform.SetAsLastSibling();
            }

            line.text = message;
            logEntryPool.Enqueue(line);
        }

        public void Clear()
        {
            foreach (Transform child in logContent)
            {
                var text = child.GetComponent<TMP_Text>();
                if (text != null)
                {
                    text.text = string.Empty;
                    text.gameObject.SetActive(false);
                }
            }

            logEntryPool.Clear();
            foreach (Transform child in logContent)
            {
                if (child.TryGetComponent<TMP_Text>(out var text))
                {
                    logEntryPool.Enqueue(text);
                }
            }
        }

        public void SetVisibility()
        {
            if (canvas != null)
                canvas.enabled = !canvas.enabled;
        }

        public void ShowGhostSuggestion(string suggestion)
        {
            if (ghostText == null || commandInput == null)
                return;

            var typed = commandInput.text;

            if (string.IsNullOrEmpty(typed) || string.IsNullOrEmpty(suggestion))
            {
                ghostText.text = string.Empty;
                return;
            }

            var tokens = typed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                ghostText.text = suggestion;
                return;
            }

            var current = tokens.Last();

            if (suggestion.StartsWith(current, StringComparison.OrdinalIgnoreCase))
            {
                var prefix = string.Join(" ", tokens.Take(tokens.Length - 1));
                ghostText.text = string.IsNullOrEmpty(prefix)
                    ? suggestion
                    : prefix + " " + suggestion;
            }
            else
            {
                ghostText.text = typed + " " + suggestion;
            }

            ghostText.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);
        }

        public void AcceptGhostSuggestion()
        {
            if (!string.IsNullOrEmpty(ghostText?.text))
            {
                commandInput.text = ghostText.text;
                commandInput.caretPosition = commandInput.text.Length;
                ghostText.text = string.Empty;
            }
        }
    }
}
