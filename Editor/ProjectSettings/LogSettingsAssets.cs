using EldritchGames.EldritchLogger.Settings;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EldritchGames.EldritchLogger.EditorTools.ProjectSettings
{
    /// <summary>Finds and creates the <see cref="LogSettings"/> asset that <see cref="LoggerBootstrap"/> loads.</summary>
    public static class LogSettingsAssets
    {
        public const string DefaultPath = "Assets/Resources/" + LoggerBootstrap.SettingsResourcePath + ".asset";

        /// <summary>
        /// The asset loaded at runtime: a <see cref="LogSettings"/> named <c>LogSettings</c> in a <c>Resources</c> folder.
        /// </summary>
        public static LogSettings FindActive() =>
            FindAllPaths()
                .Where(IsLoadablePath)
                .Select(AssetDatabase.LoadAssetAtPath<LogSettings>)
                .FirstOrDefault(s => s != null);

        /// <summary>Every LogSettings asset in the project, including ones the bootstrap will not load.</summary>
        public static string[] FindAllPaths() =>
            AssetDatabase.FindAssets("t:" + nameof(LogSettings))
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();

        /// <summary>True for <c>.../Resources/LogSettings.asset</c>.</summary>
        public static bool IsLoadablePath(string path)
        {
            path = path.Replace('\\', '/');
            return Path.GetFileNameWithoutExtension(path) == LoggerBootstrap.SettingsResourcePath
                   && path.Contains("/Resources/");
        }

        [MenuItem("Tools/Eldritch Logger/Create Log Settings")]
        public static LogSettings CreateDefault()
        {
            var existing = FindActive();
            if (existing != null)
            {
                Selection.activeObject = existing;
                return existing;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(DefaultPath));
            var settings = ScriptableObject.CreateInstance<LogSettings>();
            AssetDatabase.CreateAsset(settings, DefaultPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = settings;
            return settings;
        }
    }
}
