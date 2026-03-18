#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerMaze.Editor
{
    public static class TowerMazePrototypeSetup
    {
        private const string ConfigFolder = "Assets/TowerMaze/Config";
        private const string ScenePath = "Assets/Scenes/TowerMazePrototype.unity";

        [MenuItem("TowerMaze/Setup Prototype Scene")]
        public static void SetupPrototypeScene()
        {
            EnsureFolders();

            GameConfig gameConfig = EnsureAsset<GameConfig>($"{ConfigFolder}/GameConfig.asset");
            DifficultyProfile difficultyProfile = EnsureAsset<DifficultyProfile>($"{ConfigFolder}/DifficultyProfile.asset");
            ThemeDefinition themeDefinition = EnsureAsset<ThemeDefinition>($"{ConfigFolder}/VolcanicTheme.asset");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject gameRoot = new("GameRoot");
            TowerMazeBootstrapper bootstrapper = gameRoot.AddComponent<TowerMazeBootstrapper>();

            Transform managers = CreateChild(gameRoot.transform, "Managers");
            CreateChild(managers, "GameManager");
            CreateChild(managers, "RunManager");
            CreateChild(managers, "ScoreManager");
            CreateChild(managers, "AudioManager");

            Transform towerSystem = CreateChild(gameRoot.transform, "TowerSystem");
            Transform player = CreateChild(gameRoot.transform, "Player");
            Transform cameraRig = CreateChild(gameRoot.transform, "CameraRig");
            Transform vfx = CreateChild(gameRoot.transform, "VFX");
            Transform ui = CreateChild(gameRoot.transform, "UI");

            bootstrapper.AssignRoots(managers, towerSystem, player, cameraRig, vfx, ui);
            bootstrapper.SetConfigAssets(gameConfig, difficultyProfile, themeDefinition);

            ConfigurePlayerSettings();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        [MenuItem("TowerMaze/Runtime/Start Run")]
        public static void StartRun()
        {
            RunManager runManager = Object.FindAnyObjectByType<RunManager>();
            if (runManager != null && Application.isPlaying)
            {
                runManager.StartRun();
            }
        }

        [MenuItem("TowerMaze/Runtime/Retry")]
        public static void RetryRun()
        {
            RunManager runManager = Object.FindAnyObjectByType<RunManager>();
            if (runManager != null && Application.isPlaying)
            {
                runManager.RetryRun();
            }
        }

        [MenuItem("TowerMaze/Runtime/Continue")]
        public static void ContinueRun()
        {
            RunManager runManager = Object.FindAnyObjectByType<RunManager>();
            if (runManager != null && Application.isPlaying)
            {
                runManager.ContinueRun();
            }
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/TowerMaze"))
            {
                AssetDatabase.CreateFolder("Assets", "TowerMaze");
            }

            if (!AssetDatabase.IsValidFolder(ConfigFolder))
            {
                AssetDatabase.CreateFolder("Assets/TowerMaze", "Config");
            }
        }

        private static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing;
            }

            GameObject gameObject = new(name);
            gameObject.transform.SetParent(parent, false);
            return gameObject.transform;
        }
    }
}
#endif
