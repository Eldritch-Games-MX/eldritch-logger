using UnityEngine;
using UnityEngine.UI;

namespace EldritchGames.EldritchLogger.Console.Themes
{
    /// <summary>
    /// Colors a <see cref="Graphic"/> (Image, TextMeshPro text...) from the active console theme.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class ThemeableGraphic : MonoBehaviour, IThemeable
    {
        public enum Role
        {
            Background,
            Text,
            Accent
        }

        [SerializeField] private Role role = Role.Text;
        [SerializeField, Range(0f, 1f)] private float alpha = 1f;

        public void ApplyTheme(ConsoleTheme theme)
        {
            if (theme == null || !TryGetComponent<Graphic>(out var graphic)) return;

            var color = role switch
            {
                Role.Background => theme.backgroundColor,
                Role.Accent => theme.promptColor,
                _ => theme.textColor
            };
            color.a *= alpha;
            graphic.color = color;
        }
    }
}
