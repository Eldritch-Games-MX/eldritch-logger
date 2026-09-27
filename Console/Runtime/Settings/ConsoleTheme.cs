using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Settings
{
    [CreateAssetMenu(fileName = "ConsoleTheme", menuName = "Eldritch Logger/Console Theme")]
    public class ConsoleTheme : ScriptableObject
    {
        [Header("Colors")]
        public Color backgroundColor = Color.black;
        public Color textColor = Color.white;
        public Color promptColor = Color.green;

        [Header("Font")]
        public Font font;
        public int fontSize = 14;
    }
}
