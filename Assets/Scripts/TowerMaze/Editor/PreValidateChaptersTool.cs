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
                    float s = ChapterManager.ComputeSinkSpeed(n, ballPlayerSpeed);
                    float sm = ChapterManager.ComputeSafetyMargin(c);
                    MazeSettings ms = ChapterManager.ComputeMazeSettings(n);
                    validator.TryValidateChapter(n, baseSeed, h, ms, s, sm, ballPlayerSpeed, out int attempt);
                    table.SetAttempt(n, attempt);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PreValidateChaptersTool] Done. Asset: " + AssetPath);
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
