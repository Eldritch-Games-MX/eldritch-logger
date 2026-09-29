using EldritchGames.EldritchLogger.Console.Themes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace EldritchGames.EldritchLogger.Console.Tests.PlayMode
{
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
