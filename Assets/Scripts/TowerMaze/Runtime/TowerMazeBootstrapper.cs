using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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

        private static bool IsMobileRuntimePlatform =>
            Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer;

        private static bool IsLowEndMobileDevice
        {
            get
            {
                if (!IsMobileRuntimePlatform)
                {
                    return false;
                }

                int memoryMb = SystemInfo.systemMemorySize;
                int cpuCores = SystemInfo.processorCount;
                return (memoryMb > 0 && memoryMb <= 4096) || (cpuCores > 0 && cpuCores <= 6);
            }
        }

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
            ConfigurePerformanceDefaults();
            AnalyticsManager.Initialize();
            LogVerbose("[Bootstrapper] Awake started");
            if (initialized)
            {
                LogVerbose("[Bootstrapper] Already initialized, skipping");
                return;
            }

            try {
                if (gameConfig == null) {
                    LogVerbose("[Bootstrapper] Loading GameConfig from Resources...");
                    gameConfig = Resources.Load<GameConfig>("TowerMaze/GameConfig");
                }
                if (difficultyProfile == null)
                {
                    difficultyProfile = Resources.Load<DifficultyProfile>("TowerMaze/DifficultyProfile")
                        ?? Resources.Load<DifficultyProfile>("TowerMaze/StandardDifficultyProfile");
                }
                if (themeDefinition == null) themeDefinition = LoadThemeDefinition();
                if (difficultyProfile == null) difficultyProfile = CreateFallbackDifficultyProfile();

                if (gameConfig == null) Debug.LogError("[Bootstrapper] CRITICAL: GameConfig asset not found in Resources/TowerMaze/GameConfig!");
                else LogVerbose($"[Bootstrapper] GameConfig loaded: {gameConfig.name}");

                LogVerbose("[Bootstrapper] Ensuring roots...");
                EnsureRoots();
                ConfigureEnvironment();

                LogVerbose("[Bootstrapper] Creating Managers...");
                ScoreManager scoreManager = EnsureComponent<ScoreManager>(EnsureChild(managersRoot, "ScoreManager"));
                LogVerbose("[Bootstrapper] Created ScoreManager");
                EconomyManager economyManager = EnsureComponent<EconomyManager>(EnsureChild(managersRoot, "EconomyManager"));
                LogVerbose("[Bootstrapper] Created EconomyManager");
                RewardedAdManager rewardedAdManager = EnsureComponent<RewardedAdManager>(EnsureChild(managersRoot, "RewardedAdManager"));
                InterstitialAdManager interstitialAdManager = EnsureComponent<InterstitialAdManager>(EnsureChild(managersRoot, "InterstitialAdManager"));
                InAppReviewManager inAppReviewManager = EnsureComponent<InAppReviewManager>(EnsureChild(managersRoot, "InAppReviewManager"));
                CoinStoreManager coinStoreManager = EnsureComponent<CoinStoreManager>(EnsureChild(managersRoot, "CoinStoreManager"));
                AudioManager audioManager = EnsureComponent<AudioManager>(EnsureChild(managersRoot, "AudioManager"));
                BannerAdManager bannerAdManager = EnsureComponent<BannerAdManager>(EnsureChild(managersRoot, "BannerAdManager"));
                FirebaseCloudManager firebaseCloudManager = EnsureComponent<FirebaseCloudManager>(EnsureChild(managersRoot, "FirebaseCloudManager"));
                RunManager runManager = EnsureComponent<RunManager>(EnsureChild(managersRoot, "RunManager"));
                ChapterManager chapterManager = EnsureComponent<ChapterManager>(EnsureChild(managersRoot, "ChapterManager"));
                LogVerbose("[Bootstrapper] All Managers created");
                
                LogVerbose("[Bootstrapper] Creating Tower System...");
                Transform towerMotionRoot = EnsureChild(towerSystemRoot, "TowerMotionRoot");
                TowerSinkController sinkController = EnsureComponent<TowerSinkController>(towerMotionRoot);
                Transform towerRoot = EnsureChild(towerMotionRoot, "TowerRoot");
                TowerRotationController rotationController = EnsureComponent<TowerRotationController>(towerRoot);
                TowerGenerator towerGenerator = EnsureComponent<TowerGenerator>(towerRoot);
                
                LavaController lavaController = EnsureComponent<LavaController>(EnsureChild(towerSystemRoot, "Lava"));
                LogVerbose("[Bootstrapper] Tower System created");

                LogVerbose("[Bootstrapper] Creating Player...");
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

                LogVerbose("[Bootstrapper] Initializing systems...");
                towerGenerator.Initialize(gameConfig, difficultyProfile, themeDefinition);
                lavaController.Initialize(gameConfig, themeDefinition);
                playerController.Initialize(gameConfig, towerGenerator, towerRoot, themeDefinition, audioManager, cameraFollow);
                scoreManager.Initialize(gameConfig);
                economyManager.Initialize();
                chapterManager.Initialize(gameConfig != null ? gameConfig.seed : 1347);
                coinStoreManager.Initialize(economyManager);
                
                System.Action applyTowerVisuals = () => {
                    if (economyManager != null && towerGenerator != null)
                        towerGenerator.ApplyTowerSkin(economyManager.GetEquippedTowerSkin(), economyManager.EmberBalance);
                };
                applyTowerVisuals();
                economyManager.EmberBalanceChanged += _ => applyTowerVisuals();
                economyManager.EquippedTowerSkinChanged += _ => applyTowerVisuals();
                rewardedAdManager.Initialize(gameConfig);
                interstitialAdManager.Initialize(gameConfig);
                bannerAdManager.Initialize(gameConfig);
                inAppReviewManager.Initialize();
                playerController.ApplySkin(economyManager.GetEquippedSkin());
                
                cameraFollow.Initialize(cameraTarget);
                backdropController.Initialize(themeDefinition, mainCamera, cameraTarget);

                System.Action<string> claimMissionAction = (missionId) =>
                {
                    DailyMissionRewardResult result = economyManager.ClaimMissionReward(missionId);
                    if (result.rewardEmber > 0)
                    {
                        uiManager.QueueRewardToast("MISSION CLAIMED", $"+{result.rewardEmber} COIN", new UnityEngine.Color(1f, 0.82f, 0.32f, 1f));
                        audioManager.PlayMissionComplete();
                        uiManager.RefreshDailyMissions(economyManager.DailyMissions);
                    }
                };

                uiManager.Initialize(splashActive: true, gameConfig, themeDefinition, economyManager, rewardedAdManager, coinStoreManager, playerController, runManager.StartRun, runManager.StartDailyChallenge, runManager.RetryRun, runManager.ContinueRun, runManager.ReturnToMainMenu, runManager.ClaimDoubleReward, runManager.WatchAdForLifeRefill, runManager.BuyLifeRefillWithCoins, runManager.ToggleSound, runManager.ToggleVibration, runManager.PauseRun, runManager.ResumeRun, audioManager.PlayButtonClick, null, scoreManager, bannerAdManager, claimMissionAction,
                    onPlayChapter: () => runManager.StartChapterRun(chapterManager.UnlockedUpTo),
                    onPlayEndless: runManager.StartRun,
                    onShowChapters: () => uiManager.ShowChapterSelect(chapterManager, (idx) => runManager.StartChapterRun(idx)),
                    chapterManager: chapterManager);
                runManager.Initialize(gameConfig, difficultyProfile, themeDefinition, towerGenerator, playerController, lavaController, scoreManager, economyManager, coinStoreManager, rewardedAdManager, audioManager, uiManager, backdropController, cameraFollow, inAppReviewManager, interstitialAdManager, chapterManager);

                firebaseCloudManager.Initialize(gameConfig, economyManager, scoreManager, coinStoreManager, uiManager);
                firebaseCloudManager.NicknameRequired += () =>
                {
                    uiManager.ShowNicknamePopup((name, onComplete) =>
                    {
                        firebaseCloudManager.TrySetNickname(name, onComplete);
                    });
                };
                
                initialized = true;
                LogVerbose($"[Bootstrapper] Awake completed successfully. Camera CullingMask: {mainCamera.cullingMask}, Layer: {mainCamera.gameObject.layer}");
                LogVerbose($"[Bootstrapper] PlayerRoot Active: {playerRoot.gameObject.activeInHierarchy}, TowerRoot Active: {towerRoot.gameObject.activeInHierarchy}");
            } catch (System.Exception e) {
                Debug.LogError($"[Bootstrapper] CRITICAL FAILURE in Awake: {e.Message}\n{e.StackTrace}");
            }
        }

        private void EnsureRoots()
        {
            if (managersRoot == null) managersRoot = EnsureChild(transform, "Managers");
            if (towerSystemRoot == null) towerSystemRoot = EnsureChild(transform, "TowerSystem");
            if (playerRoot == null) playerRoot = EnsureChild(transform, "Player");
            if (cameraRigRoot == null) cameraRigRoot = EnsureChild(transform, "CameraRig");
            if (vfxRoot == null) vfxRoot = EnsureChild(transform, "VFX");
            if (uiRoot == null) uiRoot = EnsureChild(transform, "UI");
        }

        private static ThemeDefinition LoadThemeDefinition()
        {
            Debug.LogWarning("[Bootstrapper] Serialized themeDefinition is missing. Trying the default theme asset from Resources.");
            ThemeDefinition loadedTheme = Resources.Load<ThemeDefinition>("TowerMaze/StandardTheme")
                ?? Resources.Load<ThemeDefinition>("TowerMaze/VolcanicTheme");
            if (loadedTheme != null)
            {
                LogVerbose($"[Bootstrapper] Loaded fallback theme asset: {loadedTheme.name}");
                return loadedTheme;
            }

            Debug.LogWarning("[Bootstrapper] ThemeDefinition not found at Resources/TowerMaze/StandardTheme. Using runtime fallback theme.");
            return CreateFallbackThemeDefinition();
        }

        private static DifficultyProfile CreateFallbackDifficultyProfile()
        {
            DifficultyProfile fallback = ScriptableObject.CreateInstance<DifficultyProfile>();
            fallback.name = "RuntimeFallbackDifficultyProfile";
            Debug.LogWarning("[Bootstrapper] DifficultyProfile was missing. Using in-memory defaults.");
            return fallback;
        }

        private static void ConfigurePerformanceDefaults()
        {
            DeviceQualityProfile.EnsureInitialized();
            DeviceTier tier = DeviceQualityProfile.Tier;

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = DeviceQualityProfile.TargetFps;

            ApplyUrpAssetTierOverrides();

            if (!IsMobileRuntimePlatform)
            {
                return;
            }

            QualitySettings.SetQualityLevel((int)tier, true);
            QualitySettings.antiAliasing = DeviceQualityProfile.MsaaEnabled ? 2 : 0;
            QualitySettings.pixelLightCount = DeviceQualityProfile.PixelLightCount;
            QualitySettings.anisotropicFiltering = DeviceQualityProfile.AnisotropicFilteringLevel > 0
                ? AnisotropicFiltering.Enable
                : AnisotropicFiltering.Disable;
            QualitySettings.shadows = DeviceQualityProfile.ShadowsEnabled
                ? UnityEngine.ShadowQuality.All
                : UnityEngine.ShadowQuality.Disable;
            QualitySettings.shadowDistance = DeviceQualityProfile.ShadowsEnabled ? 30f : 20f;
            QualitySettings.realtimeReflectionProbes = false;
            QualitySettings.lodBias = tier == DeviceTier.Low ? 1.35f : 1.5f;
            QualitySettings.maximumLODLevel = tier == DeviceTier.Low ? 1 : 0;
            QualitySettings.resolutionScalingFixedDPIFactor = DeviceQualityProfile.RenderScale;

            Debug.Log($"[Bootstrapper] {DeviceQualityProfile.DescribeTier()}");
        }

        private static void ApplyUrpAssetTierOverrides()
        {
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                urp.renderScale = DeviceQualityProfile.RenderScale;
                urp.msaaSampleCount = DeviceQualityProfile.MsaaEnabled ? 2 : 1;
                urp.shadowDistance = DeviceQualityProfile.ShadowsEnabled ? 30f : 0f;
                ToggleSsaoFeature(urp, DeviceQualityProfile.SsaoEnabled);

                if (urp.volumeProfile != null)
                {
                    ApplyVolumeProfileTier(urp.volumeProfile);
                }
            }
        }

        private static void ApplyVolumeProfileTier(VolumeProfile profile)
        {
            if (profile.TryGet(out UnityEngine.Rendering.Universal.Bloom bloom))
            {
                bloom.intensity.Override(DeviceQualityProfile.BloomIntensity);
                bloom.maxIterations.Override(DeviceQualityProfile.BloomMaxIterations);
                bloom.highQualityFiltering.Override(DeviceQualityProfile.BloomHighQualityFiltering);
            }
            if (profile.TryGet(out UnityEngine.Rendering.Universal.Vignette vignette))
            {
                vignette.intensity.Override(DeviceQualityProfile.VignetteIntensity);
            }
        }

        private static void ToggleSsaoFeature(UniversalRenderPipelineAsset urp, bool enabled)
        {
            var rendererDataListField = typeof(UniversalRenderPipelineAsset)
                .GetField("m_RendererDataList", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (rendererDataListField == null) return;
            if (rendererDataListField.GetValue(urp) is not UnityEngine.Rendering.Universal.ScriptableRendererData[] rendererList) return;

            foreach (var rendererData in rendererList)
            {
                if (rendererData == null || rendererData.rendererFeatures == null) continue;
                foreach (var feature in rendererData.rendererFeatures)
                {
                    if (feature == null) continue;
                    if (feature.GetType().Name.Contains("ScreenSpaceAmbientOcclusion"))
                    {
                        feature.SetActive(enabled);
                    }
                }
            }
        }

        private static void LogVerbose(string message)
        {
            if (Application.isEditor || Debug.isDebugBuild)
            {
                Debug.Log(message);
            }
        }

        private static ThemeDefinition CreateFallbackThemeDefinition()
        {
            ThemeDefinition fallback = ScriptableObject.CreateInstance<ThemeDefinition>();
            fallback.name = "RuntimeFallbackTheme";
            fallback.themeId = "runtime_fallback";

            fallback.skyColor = new Color(0.38f, 0.2f, 0.12f, 1f);
            fallback.fogColor = new Color(0.35f, 0.19f, 0.15f, 1f);
            fallback.towerPathColor = Color.white;
            fallback.towerMainPathColor = Color.white;
            fallback.towerWallColor = new Color(0.19f, 0.16f, 0.15f, 1f);
            fallback.lavaColor = new Color(1f, 0.39f, 0.08f, 1f);
            fallback.lavaEmissionColor = new Color(1f, 0.42f, 0.08f, 1f);
            fallback.accentColor = new Color(1f, 0.69f, 0.19f, 1f);
            fallback.nearLavaOverlay = new Color(1f, 0.46f, 0.14f, 0.18f);

            fallback.towerWallBaseMap = LoadOptionalTexture("TowerMaze/BallSkins/Metal043A/Metal043A_2K-JPG_Color");
            fallback.towerWallNormalMap = LoadOptionalTexture("TowerMaze/BallSkins/Metal043A/Metal043A_2K-JPG_NormalGL");
            fallback.towerWallTextureScale = new Vector2(2.35f, 0.8f);
            fallback.towerPathBaseMap = LoadOptionalTexture("TowerMaze/BallSkins/Metal044A/Metal044A_2K-JPG_Color");
            fallback.towerPathNormalMap = LoadOptionalTexture("TowerMaze/BallSkins/Metal044A/Metal044A_2K-JPG_NormalGL");
            fallback.towerPathTextureScale = new Vector2(1.95f, 0.95f);
            fallback.towerMainPathBaseMap = LoadOptionalTexture("TowerMaze/BallSkins/Lava004/Lava004_2K-JPG_Color");
            fallback.towerMainPathNormalMap = LoadOptionalTexture("TowerMaze/BallSkins/Lava004/Lava004_2K-JPG_NormalGL");
            fallback.towerMainPathTextureScale = new Vector2(2.05f, 1f);

            fallback.heroPrimary = new Color(0.12f, 0.12f, 0.14f, 1f);
            fallback.heroSecondary = new Color(0.93f, 0.41f, 0.19f, 1f);
            fallback.heroAccent = new Color(0.96f, 0.83f, 0.58f, 1f);
            return fallback;
        }

        private static Texture2D LoadOptionalTexture(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return null;
            }

            return Resources.Load<Texture2D>(resourcePath);
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
                directionalLight.transform.rotation = Quaternion.Euler(55f, -28f, 0f);
            }

            Color warmAccent = Color.Lerp(Color.white, themeDefinition != null ? themeDefinition.lavaColor : new Color(1f, 0.5f, 0.2f), 0.18f);
            directionalLight.color = warmAccent;
            directionalLight.intensity = DeviceQualityProfile.DirectionalLightIntensity;
            directionalLight.bounceIntensity = DeviceQualityProfile.DirectionalLightBounce;
            directionalLight.shadows = DeviceQualityProfile.DirectionalShadowMode;
            directionalLight.shadowStrength = DeviceQualityProfile.ShadowsEnabled ? 0.62f : 0f;
            directionalLight.shadowBias = 0.05f;
            directionalLight.shadowNormalBias = 0.35f;

            EnsureReflectionProbe(backdropSky);
        }

        private static void EnsureReflectionProbe(Color backdropSky)
        {
            ReflectionProbe probe = FindAnyObjectByType<ReflectionProbe>();
            if (probe == null)
            {
                GameObject probeObject = new("GlobalReflectionProbe");
                probe = probeObject.AddComponent<ReflectionProbe>();
            }

            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = IsMobileRuntimePlatform
                ? UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.NoTimeSlicing
                : UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.IndividualFaces;
            probe.resolution = DeviceQualityProfile.ReflectionProbeResolution;
            probe.size = new Vector3(60f, 120f, 60f);
            probe.center = new Vector3(0f, 40f, 0f);
            probe.transform.position = Vector3.zero;
            probe.boxProjection = DeviceQualityProfile.ReflectionBoxProjection;
            probe.nearClipPlane = 0.3f;
            probe.farClipPlane = 150f;
            probe.backgroundColor = backdropSky;
            probe.clearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags.SolidColor;
            probe.intensity = DeviceQualityProfile.ReflectionProbeIntensity;
        }

        private Camera EnsureCamera()
        {
            bool allowHdr = DeviceQualityProfile.HdrEnabled;
            bool allowMsaa = !IsMobileRuntimePlatform || DeviceQualityProfile.MsaaEnabled;
            Camera existing = Camera.main;
            if (existing != null)
            {
                existing.clearFlags = CameraClearFlags.Skybox;
                existing.backgroundColor = EnvironmentBackdropController.GetSkyColor(themeDefinition);
                existing.fieldOfView = 54f;
                existing.allowHDR = allowHdr;
                existing.allowMSAA = allowMsaa;
                ConfigureUniversalCamera(existing);
                return existing;
            }

            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = EnvironmentBackdropController.GetSkyColor(themeDefinition);
            camera.fieldOfView = 54f;
            camera.allowHDR = allowHdr;
            camera.allowMSAA = allowMsaa;
            ConfigureUniversalCamera(camera);
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static void ConfigureUniversalCamera(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            UniversalAdditionalCameraData additionalCameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            if (additionalCameraData == null)
            {
                return;
            }

            if (IsMobileRuntimePlatform)
            {
                additionalCameraData.renderPostProcessing = false;
                additionalCameraData.renderShadows = false;
            }
            else
            {
                additionalCameraData.renderPostProcessing = true;
                additionalCameraData.renderShadows = true;
            }
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
