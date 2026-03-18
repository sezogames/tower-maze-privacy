using UnityEngine;

namespace TowerMaze
{
    [DefaultExecutionOrder(-100)]
    public sealed class TowerMazeBootstrapper : MonoBehaviour
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private DifficultyProfile difficultyProfile;
        [SerializeField] private ThemeDefinition themeDefinition;

        [Header("Root References")]
        [SerializeField] private Transform managersRoot;
        [SerializeField] private Transform towerSystemRoot;
        [SerializeField] private Transform playerRoot;
        [SerializeField] private Transform cameraRigRoot;
        [SerializeField] private Transform vfxRoot;
        [SerializeField] private Transform uiRoot;

        private bool initialized;

        public void AssignRoots(Transform managers, Transform towerSystem, Transform player, Transform cameraRig, Transform vfx, Transform ui)
        {
            managersRoot = managers;
            towerSystemRoot = towerSystem;
            playerRoot = player;
            cameraRigRoot = cameraRig;
            vfxRoot = vfx;
            uiRoot = ui;
        }

        public void SetConfigAssets(GameConfig config, DifficultyProfile difficulty, ThemeDefinition theme)
        {
            gameConfig = config;
            difficultyProfile = difficulty;
            themeDefinition = theme;
        }


        private void Awake()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            EnsureRoots();
            ConfigureEnvironment();

            ScoreManager scoreManager = EnsureComponent<ScoreManager>(EnsureChild(managersRoot, "ScoreManager"));
            EconomyManager economyManager = EnsureComponent<EconomyManager>(EnsureChild(managersRoot, "EconomyManager"));
            RewardedAdManager rewardedAdManager = EnsureComponent<RewardedAdManager>(EnsureChild(managersRoot, "RewardedAdManager"));
            InAppReviewManager inAppReviewManager = EnsureComponent<InAppReviewManager>(EnsureChild(managersRoot, "InAppReviewManager"));
            CoinStoreManager coinStoreManager = EnsureComponent<CoinStoreManager>(EnsureChild(managersRoot, "CoinStoreManager"));
            AudioManager audioManager = EnsureComponent<AudioManager>(EnsureChild(managersRoot, "AudioManager"));
            PlayFabCloudManager playFabCloudManager = EnsureComponent<PlayFabCloudManager>(EnsureChild(managersRoot, "PlayFabCloudManager"));
            RunManager runManager = EnsureComponent<RunManager>(EnsureChild(managersRoot, "RunManager"));

            Transform towerMotionRoot = EnsureChild(towerSystemRoot, "TowerMotionRoot");
            TowerSinkController sinkController = EnsureComponent<TowerSinkController>(towerMotionRoot);
            Transform towerRoot = EnsureChild(towerMotionRoot, "TowerRoot");
            TowerRotationController rotationController = EnsureComponent<TowerRotationController>(towerRoot);
            TowerGenerator towerGenerator = EnsureComponent<TowerGenerator>(towerRoot);
            LavaController lavaController = EnsureComponent<LavaController>(EnsureChild(towerSystemRoot, "Lava"));
            _ = sinkController;
            _ = rotationController;

            Transform playerVisualRoot = EnsureChild(playerRoot, "Visual");
            Transform cameraTarget = EnsureChild(playerRoot, "CameraTarget");
            cameraTarget.localPosition = new Vector3(0f, 0.34f, 0f);
            EnsureComponent<PlayerInputHandler>(playerRoot);
            PlayerController playerController = EnsureComponent<PlayerController>(playerRoot);
            EnsureComponent<HeroVisualController>(playerVisualRoot);

            Camera mainCamera = EnsureCamera();
            mainCamera.transform.SetParent(cameraRigRoot, false);
            CameraFollowController cameraFollow = EnsureComponent<CameraFollowController>(mainCamera.transform);
            EnvironmentBackdropController backdropController = EnsureComponent<EnvironmentBackdropController>(EnsureChild(vfxRoot, "Backdrop"));

            UIManager uiManager = EnsureComponent<UIManager>(uiRoot);

            Texture2D splashTex = Resources.Load<Texture2D>("TowerMaze/UITheme/SplashBackground");
            Font splashFont = Resources.Load<Font>("TowerMaze/UITheme/Outfit-Bold");

            Texture2D staticBgTex = Resources.Load<Texture2D>("TowerMaze/UITheme/MainMenuStaticBackground");
            Sprite staticBgSprite = null;
            if (staticBgTex != null)
            {
                staticBgSprite = Sprite.Create(staticBgTex, new Rect(0, 0, staticBgTex.width, staticBgTex.height), new Vector2(0.5f, 0.5f));
            }

            SplashScreenController splashController = new GameObject("SplashScreen")
                .AddComponent<SplashScreenController>();
            splashController.Initialize(splashFont, splashTex, onComplete: () => uiManager.OnSplashComplete());

            towerGenerator.Initialize(gameConfig, difficultyProfile, themeDefinition);
            lavaController.Initialize(gameConfig, themeDefinition);
            playerController.Initialize(gameConfig, towerGenerator, towerRoot, themeDefinition, audioManager, cameraFollow);
            scoreManager.Initialize();
            economyManager.Initialize();
            coinStoreManager.Initialize(economyManager);
            System.Action applyTowerVisuals = () => towerGenerator.ApplyTowerSkin(economyManager.GetEquippedTowerSkin(), economyManager.EmberBalance);
            applyTowerVisuals();
            economyManager.EmberBalanceChanged += _ => applyTowerVisuals();
            economyManager.EquippedTowerSkinChanged += _ => applyTowerVisuals();
            rewardedAdManager.Initialize(gameConfig);
            inAppReviewManager.Initialize();
            playerController.ApplySkin(economyManager.GetEquippedSkin());
            uiManager.Initialize(splashActive: true, themeDefinition, economyManager, rewardedAdManager, coinStoreManager, playerController, runManager.StartRun, runManager.StartDailyChallenge, runManager.RetryRun, runManager.ContinueRun, runManager.ReturnToMainMenu, runManager.ClaimDoubleReward, runManager.WatchAdForLifeRefill, runManager.BuyLifeRefillWithCoins, runManager.ToggleSound, runManager.ToggleVibration, runManager.PauseRun, runManager.ResumeRun, audioManager.PlayButtonClick, staticBgSprite);
            cameraFollow.Initialize(cameraTarget);
            backdropController.Initialize(themeDefinition, mainCamera, cameraTarget);

            runManager.Initialize(gameConfig, difficultyProfile, themeDefinition, towerGenerator, playerController, lavaController, scoreManager, economyManager, rewardedAdManager, audioManager, uiManager, backdropController, cameraFollow, inAppReviewManager);
            playFabCloudManager.Initialize(gameConfig, economyManager, scoreManager, coinStoreManager, uiManager);
        }


        private void EnsureRoots()
        {
            managersRoot ??= EnsureChild(transform, "Managers");
            towerSystemRoot ??= EnsureChild(transform, "TowerSystem");
            playerRoot ??= EnsureChild(transform, "Player");
            cameraRigRoot ??= EnsureChild(transform, "CameraRig");
            vfxRoot ??= EnsureChild(transform, "VFX");
            uiRoot ??= EnsureChild(transform, "UI");
        }

        private void ConfigureEnvironment()
        {
            Color backdropSky = EnvironmentBackdropController.GetSkyColor(themeDefinition);
            Color backdropFog = EnvironmentBackdropController.GetFogColor(themeDefinition);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(backdropFog, Color.white, 0.22f) * 0.88f;
            RenderSettings.fog = true;
            RenderSettings.fogColor = backdropFog;
            RenderSettings.fogDensity = 0.0085f;

            Light directionalLight = FindAnyObjectByType<Light>();
            if (directionalLight == null)
            {
                GameObject lightObject = new("Directional Light");
                directionalLight = lightObject.AddComponent<Light>();
                directionalLight.type = LightType.Directional;
                directionalLight.transform.rotation = Quaternion.Euler(50f, -28f, 0f);
            }

            directionalLight.color = new Color(1f, 0.96f, 0.9f, 1f);
            directionalLight.intensity = 1.34f;
        }

        private Camera EnsureCamera()
        {
            Camera existing = Camera.main;
            if (existing != null)
            {
                existing.clearFlags = CameraClearFlags.Skybox;
                existing.backgroundColor = EnvironmentBackdropController.GetSkyColor(themeDefinition);
                existing.fieldOfView = 54f;
                return existing;
            }

            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = EnvironmentBackdropController.GetSkyColor(themeDefinition);
            camera.fieldOfView = 54f;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static T EnsureComponent<T>(Transform target) where T : Component
        {
            T component = target.GetComponent<T>();
            if (component == null)
            {
                component = target.gameObject.AddComponent<T>();
            }

            return component;
        }

        private static Transform EnsureChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                return child;
            }

            GameObject childObject = new(childName);
            childObject.transform.SetParent(parent, false);
            return childObject.transform;
        }
    }
}
