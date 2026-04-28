#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TowerMaze.EditorTools
{
    public static class PreValidateChaptersTool
    {
        private const string AssetPath = "Assets/Resources/TowerMaze/ChapterSeedTable.asset";

        [MenuItem("Tools/TowerMaze/Pre-Validate Chapters")]
        public static void Run()
        {
            var config = Resources.Load<GameConfig>("TowerMaze/GameConfig");
            if (config == null)
            {
                Debug.LogError("[PreValidateChaptersTool] GameConfig not found at Resources/TowerMaze/GameConfig");
                return;
            }

            var theme = Resources.Load<ThemeDefinition>("TowerMaze/StandardTheme")
                ?? Resources.Load<ThemeDefinition>("TowerMaze/VolcanicTheme");
            if (theme == null)
            {
                Debug.LogError("[PreValidateChaptersTool] No ThemeDefinition found in Resources/TowerMaze");
                return;
            }

            var difficultyProfile = Resources.Load<DifficultyProfile>("TowerMaze/DifficultyProfile")
                ?? Resources.Load<DifficultyProfile>("TowerMaze/StandardDifficultyProfile");
            if (difficultyProfile == null)
            {
                Debug.LogError("[PreValidateChaptersTool] No DifficultyProfile found in Resources/TowerMaze");
                return;
            }

            float ballPlayerSpeed = config.climbSpeed;
            int baseSeed = config.seed;

            var validator = new ChapterValidator(config, difficultyProfile, theme);
            var table = AssetDatabase.LoadAssetAtPath<ChapterSeedTable>(AssetPath)
                ?? CreateAssetAt<ChapterSeedTable>(AssetPath);
            table.EnsureSize();

            const int MaxAttempts = 16;
            const float LavaHeadStart = 8f;
            int unreachableCount = 0;

            try
            {
                for (int n = 1; n <= ChapterManager.TotalChapters; n++)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "Pre-Validating Chapters",
                            $"Chapter {n}/{ChapterManager.TotalChapters}",
                            n / (float)ChapterManager.TotalChapters))
                    {
                        Debug.LogWarning("[PreValidateChaptersTool] Cancelled by user");
                        break;
                    }

                    float c = ChapterManager.ComputeComplexity(n);
                    float h = ChapterManager.ComputeTargetHeight(n);
                    float sm = ChapterManager.ComputeSafetyMargin(c);
                    MazeSettings ms = ChapterManager.ComputeMazeSettings(n);

                    // Pick the first reachable seed (re-roll if A* says unreachable),
                    // then derive sinkSpeed from its actual optimal time. This grounds
                    // the lava budget in the real maze instead of formula heuristics.
                    int chosenAttempt = 0;
                    float chosenOptimalTime = float.PositiveInfinity;
                    for (int attempt = 0; attempt < MaxAttempts; attempt++)
                    {
                        int seed = (baseSeed * 31) ^ (n * 7919) ^ (attempt * 12911);
                        float optimalTime = validator.MeasureOptimalTime(seed, ms, h, ballPlayerSpeed);
                        if (!float.IsPositiveInfinity(optimalTime))
                        {
                            chosenAttempt = attempt;
                            chosenOptimalTime = optimalTime;
                            break;
                        }
                    }

                    if (float.IsPositiveInfinity(chosenOptimalTime))
                    {
                        unreachableCount++;
                        Debug.LogError($"[PreValidateChaptersTool] Chapter {n} unreachable on all {MaxAttempts} attempts");
                        // Fall back to formula sinkSpeed so the runtime still has a value.
                        table.SetAttempt(n, 0);
                        table.SetSinkSpeed(n, ChapterManager.ComputeSinkSpeed(n, ballPlayerSpeed));
                        continue;
                    }

                    float derivedSinkSpeed = (h + LavaHeadStart) / Mathf.Max(0.01f, chosenOptimalTime * sm);
                    table.SetAttempt(n, chosenAttempt);
                    table.SetSinkSpeed(n, derivedSinkSpeed);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[PreValidateChaptersTool] Done. Asset: {AssetPath}. Unreachable chapters: {unreachableCount}");
        }

        private static T CreateAssetAt<T>(string path) where T : ScriptableObject
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
#endif
