using TMPro;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Themes
{
    [CreateAssetMenu(fileName = "ConsoleTheme", menuName = "Eldritch Logger/Console Theme")]
    public class ConsoleTheme : ScriptableObject
    {
        [Header("Colors")]
        public Color backgroundColor = Color.black;
        public Color textColor = Color.white;
        public Color promptColor = Color.green;

        [Header("Font")]
        [Tooltip("Optional. Leave empty to keep the prefab's font.")]
        public TMP_FontAsset fontAsset;
        public int fontSize = 14;
    }
}
