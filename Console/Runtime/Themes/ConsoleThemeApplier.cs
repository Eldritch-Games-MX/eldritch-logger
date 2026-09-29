using EldritchGames.EldritchLogger.Console.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EldritchGames.EldritchLogger.Console.Themes
{
    /// <summary>
    /// Applies a <see cref="ConsoleTheme"/> to the console: the root panel background, the
    /// <see cref="ConsoleView"/>, and every <see cref="IThemeable"/> component below this object.
    /// </summary>
    [ExecuteAlways]
    public class ConsoleThemeApplier : MonoBehaviour, IConsoleThemeApplier
    {
        [SerializeField] private ConsoleTheme theme;
        [SerializeField] private Image rootPanelImage;
        [SerializeField] private ConsoleView consoleView;

        private readonly List<IThemeable> themeables = new();

        public ConsoleTheme CurrentTheme => theme;

        private void Awake()
        {
            CollectThemeables();
            ApplyTheme();
        }

        private void OnValidate()
        {
            CollectThemeables();
            ApplyTheme();
        }

        /// <summary>Re-scans the hierarchy for <see cref="IThemeable"/> components (after adding UI at runtime).</summary>
        public void CollectThemeables()
        {
            themeables.Clear();
            var seen = new HashSet<IThemeable>();

            if (consoleView != null && seen.Add(consoleView))
                themeables.Add(consoleView);

            foreach (var themeable in GetComponentsInChildren<IThemeable>(true))
                if (seen.Add(themeable))
                    themeables.Add(themeable);
        }

        public void ApplyTheme()
        {
            if (theme == null) return;

            if (rootPanelImage != null)
                rootPanelImage.color = theme.backgroundColor;

            foreach (var themeable in themeables)
            {
                if (themeable is Object unityObject && unityObject == null) continue; // destroyed
                themeable.ApplyTheme(theme);
            }
        }

        public void ApplyTheme(ConsoleTheme newTheme)
        {
            if (newTheme == null) return;
            theme = newTheme;
            ApplyTheme();
        }
    }
}
