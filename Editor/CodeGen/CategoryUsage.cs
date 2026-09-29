using EldritchGames.EldritchLogger.EditorTools.ProjectSettings;
using EldritchGames.EldritchLogger.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace EldritchGames.EldritchLogger.EditorTools.CodeGen
{
    /// <summary>
    /// Finds custom categories that no script mentions. A category counts as used when its name appears as a
    /// string literal (<c>"Economy"</c>, any case) or its generated identifier appears in code
    /// (<c>LogCategories.Economy</c>, or an enum member of that name). It is a text search: a name built at
    /// runtime is not found, so the result is a hint, not proof.
    /// </summary>
    public static class CategoryUsage
    {
        /// <summary>The categories from <paramref name="categories"/> not referenced in any of <paramref name="sources"/>.</summary>
        public static List<string> FindUnreferenced(IEnumerable<string> categories, IEnumerable<string> sources)
        {
            var remaining = categories.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (remaining.Count == 0) return remaining;

            var patterns = remaining.ToDictionary(c => c, Pattern, StringComparer.OrdinalIgnoreCase);
            foreach (var source in sources)
            {
                if (string.IsNullOrEmpty(source)) continue;
                remaining.RemoveAll(c => patterns[c].IsMatch(source));
                if (remaining.Count == 0) break;
            }
            return remaining;
        }

        /// <summary>
        /// Custom (non-built-in) categories of <paramref name="settings"/> not referenced by any script under
        /// <c>Assets/</c>. The generated categories class itself is ignored, since it lists every category.
        /// </summary>
        public static List<string> FindUnreferencedInProject(LogSettings settings)
        {
            var custom = settings.categories.Where(c => !c.IsBuiltIn).Select(c => c.name).ToList();
            if (custom.Count == 0) return custom;

            var generated = Path.GetFullPath(EldritchLoggerProjectSettings.instance.categoriesOutputPath ?? string.Empty);
            var scripts = Directory.EnumerateFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories)
                .Where(path => !string.Equals(Path.GetFullPath(path), generated, StringComparison.OrdinalIgnoreCase));
            return FindUnreferenced(custom, scripts.Select(ReadOrEmpty));
        }

        private static Regex Pattern(string category)
        {
            var literal = "\"" + Regex.Escape(category) + "\"";
            var identifier = CategoryCodeGenerator.ToIdentifier(category);
            var pattern = literal + "|" + @"(?<![\w""])" + Regex.Escape(identifier) + @"(?![\w""])";
            return new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        }

        private static string ReadOrEmpty(string path)
        {
            try { return File.ReadAllText(path); }
            catch (IOException) { return string.Empty; }
            catch (UnauthorizedAccessException) { return string.Empty; }
        }
    }
}
