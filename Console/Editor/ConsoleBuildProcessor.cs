using EldritchGames.EldritchLogger.Console.Settings;
using EldritchGames.EldritchLogger.Console.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EldritchGames.EldritchLogger.Console.EditorTools
{
    /// <summary>
    /// Removes the in-game console from scenes in builds where it is not available
    /// (see <see cref="CommandConsoleSettings.availability"/> and <see cref="ConsoleAvailabilityPolicy.DisableDefine"/>),
    /// so it is not even present in the player data. Warns when a release build ships the console.
    /// </summary>
    public sealed class ConsoleBuildProcessor : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (report == null) return; // entering Play Mode, not building

            bool development = (report.summary.options & BuildOptions.Development) != 0;
            bool disabledByDefine = ConsoleDefines.IsDisabledFor(report.summary.platformGroup);

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var bootstrap in root.GetComponentsInChildren<ConsoleBootstrap>(true))
                {
                    if (bootstrap == null) continue; // already removed with a previous root

                    var availability = bootstrap.Settings != null ? bootstrap.Settings.availability : ConsoleAvailability.DevelopmentBuilds;
                    bool keep = !disabledByDefine && ConsoleAvailabilityPolicy.IsAvailable(availability, isEditor: false, development);

                    if (keep)
                    {
                        if (!development)
                            Debug.LogWarning($"[Console] '{scene.name}' ships the in-game console in a release build " +
                                             $"(availability: {availability}). Commands can be run by players.", bootstrap);
                        continue;
                    }

                    Object.DestroyImmediate(bootstrap.ConsoleRoot);
                }
            }
        }
    }

    /// <summary>Reads and writes <see cref="ConsoleAvailabilityPolicy.DisableDefine"/> in Player Settings.</summary>
    public static class ConsoleDefines
    {
        public static bool IsDisabledFor(BuildTargetGroup group) =>
            HasDefine(NamedBuildTarget.FromBuildTargetGroup(group));

        public static bool HasDefine(NamedBuildTarget target)
        {
            PlayerSettings.GetScriptingDefineSymbols(target, out var defines);
            return System.Array.IndexOf(defines, ConsoleAvailabilityPolicy.DisableDefine) >= 0;
        }

        public static void SetDefine(NamedBuildTarget target, bool enabled)
        {
            PlayerSettings.GetScriptingDefineSymbols(target, out var defines);
            var list = new System.Collections.Generic.List<string>(defines);
            list.Remove(ConsoleAvailabilityPolicy.DisableDefine);
            if (enabled) list.Add(ConsoleAvailabilityPolicy.DisableDefine);
            PlayerSettings.SetScriptingDefineSymbols(target, list.ToArray());
        }

        public static NamedBuildTarget CurrentTarget =>
            NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
    }
}
