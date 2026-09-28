using UnityEditor;
using UnityEngine;

namespace EldritchGames.EldritchLogger.EditorTools.ProjectSettings
{
    /// <summary>
    /// Editor-only options stored in <c>ProjectSettings/EldritchLoggerSettings.asset</c> (commit it to source control).
    /// </summary>
    [FilePath("ProjectSettings/EldritchLoggerSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class EldritchLoggerProjectSettings : ScriptableSingleton<EldritchLoggerProjectSettings>
    {
        [Tooltip("Where the generated categories class is written.")]
        public string categoriesOutputPath = "Assets/Scripts/Generated/LogCategories.cs";

        [Tooltip("Namespace of the generated class. Empty for the global namespace.")]
        public string categoriesNamespace = "";

        public string categoriesClassName = "LogCategories";

        [Tooltip("Regenerate the class whenever categories are added or removed in LogSettings.")]
        public bool autoGenerateCategories;

        public void SaveSettings() => Save(true);
    }
}
