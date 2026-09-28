using EldritchGames.EldritchLogger.EditorTools.ProjectSettings;
using EldritchGames.EldritchLogger.Settings;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EldritchGames.EldritchLogger.EditorTools.CodeGen
{
    /// <summary>Writes the generated categories class using the project settings.</summary>
    public static class CategoryCodeGeneration
    {
        /// <summary>Generates the class for <paramref name="settings"/>. Returns false when the file was already up to date.</summary>
        public static bool Generate(LogSettings settings)
        {
            if (settings == null) return false;

            var options = EldritchLoggerProjectSettings.instance;
            var path = options.categoriesOutputPath;
            if (string.IsNullOrWhiteSpace(path) || !path.Replace('\\', '/').StartsWith("Assets/") || !path.EndsWith(".cs"))
            {
                Debug.LogError($"[EldritchLogger] Category output path must be a .cs file under Assets/: '{path}'.");
                return false;
            }

            var code = CategoryCodeGenerator.Generate(settings.categories.Select(c => c.name),
                                                      options.categoriesNamespace, options.categoriesClassName);

            // Only touch the file when the content changes, to avoid needless recompiles.
            if (File.Exists(path) && File.ReadAllText(path) == code) return false;

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, code);
            AssetDatabase.ImportAsset(path);
            return true;
        }

        /// <summary>Regenerates when auto-generation is enabled in the project settings.</summary>
        public static void GenerateIfEnabled(LogSettings settings)
        {
            if (EldritchLoggerProjectSettings.instance.autoGenerateCategories)
                Generate(settings);
        }
    }
}
