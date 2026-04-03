using UnityEditor;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Editor
{
    [CustomEditor(typeof(LogSettings))]
    public class LogSettingsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            LogSettings settings = (LogSettings)target;

            settings.logLevel = (LogLevel)EditorGUILayout.EnumPopup("Minimum Log Level", settings.logLevel);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Enabled Categories", EditorStyles.boldLabel);

            foreach (LogCategory category in System.Enum.GetValues(typeof(LogCategory)))
            {
                bool enabled = settings.enabledCategories.Contains(category);
                bool newEnabled = EditorGUILayout.Toggle(category.ToString(), enabled);

                if (newEnabled && !enabled)
                {
                    settings.enabledCategories.Add(category);
                }
                else if (!newEnabled && enabled)
                {
                    settings.enabledCategories.Remove(category);
                }
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(settings);
            }
        }
    }
}
