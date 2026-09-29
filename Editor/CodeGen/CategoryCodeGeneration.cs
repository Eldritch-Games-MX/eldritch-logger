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
            if (!IsValidOutputPath(path))
            {
                Debug.LogError($"[EldritchLogger] Category output path must be a .cs file under Assets/: '{path}'.");
                return false;
            }

            var code = CategoryCodeGenerator.Generate(settings.categories.Select(c => c.name),
                                                      options.categoriesNamespace, options.categoriesClassName);
            if (!WriteIfChanged(path, code)) return false;

            AssetDatabase.ImportAsset(path);
            return true;
        }

        /// <summary>Regenerates when auto-generation is enabled in the project settings.</summary>
        public static void GenerateIfEnabled(LogSettings settings)
        {
            if (EldritchLoggerProjectSettings.instance.autoGenerateCategories)
                Generate(settings);
        }

        /// <summary>The output must be a <c>.cs</c> file inside the project's <c>Assets</c> folder.</summary>
        internal static bool IsValidOutputPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            path = path.Replace('\\', '/');
            return path.StartsWith("Assets/") && path.EndsWith(".cs") && !path.Contains("/../");
        }

        /// <summary>
        /// Writes <paramref name="code"/> unless the file already has exactly that content,
        /// so unchanged categories do not trigger a recompile. Returns true when the file was written.
        /// </summary>
        internal static bool WriteIfChanged(string path, string code)
        {
            if (File.Exists(path) && File.ReadAllText(path) == code) return false;

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(path, code);
            return true;
        }
    }
}
