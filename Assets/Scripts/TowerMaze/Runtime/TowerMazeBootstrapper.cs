using System.Collections;
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
        private bool bootstrapRoutineStarted;

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
            Application.runInBackground = true;
            ConfigurePerformanceDefaults();
            LogVerbose("[Bootstrapper] Awake started");
            
            if (initialized)
            {
                LogVerbose("[Bootstrapper] Already initialized, skipping");
                return;
            }
        }

        private void Start()
        {
            TryStartBootstrap("Start");
        }

        private void TryStartBootstrap(string trigger)
        {
            if (initialized || bootstrapRoutineStarted)
            {
                return;
            }

            bootstrapRoutineStarted = true;
            StartCoroutine(BootstrapRoutine());
        }

        private IEnumerator BootstrapRoutine()
        {
            LogVerbose("[Bootstrapper] Starting BootstrapRoutine...");
            
            // Step 1: Config Loading
            try {
                if (gameConfig == null) {
                    LogVerbose("[Bootstrapper] Loading GameConfig from Resources...");
                    gameConfig = Resources.Load<GameConfig>("TowerMaze/GameConfig");
                }
            } catch (System.Exception e) { Debug.LogError($"[Bootstrapper] Config Load Error: {e.Message}"); }
            yield return null;

            try {
                if (difficultyProfile == null)
                {
                    difficultyProfile = Resources.Load<DifficultyProfile>("TowerMaze/DifficultyProfile")
                        ?? Resources.Load<DifficultyProfile>("TowerMaze/StandardDifficultyProfile");
                }
            } catch (System.Exception e) { Debug.LogError($"[Bootstrapper] Difficulty Load Error: {e.Message}"); }
            yield return null;

            try {
                if (themeDefinition == null) themeDefinition = LoadThemeDefinition();
                if (difficultyProfile == null) difficultyProfile = CreateFallbackDifficultyProfile();
            } catch (System.Exception e) { Debug.LogError($"[Bootstrapper] Theme/Fallback Error: {e.Message}"); }
            yield return null;

            if (gameConfig == null) Debug.LogError("[Bootstrapper] CRITICAL: GameConfig asset not found in Resources/TowerMaze/GameConfig!");
            else LogVerbose($"[Bootstrapper] GameConfig loaded: {gameConfig.name}");

            // Step 2: Roots and Environment
            try {
                LogVerbose("[Bootstrapper] Ensuring roots...");
                EnsureRoots();
                ConfigureEnvironment();
            } catch (System.Exception e) { Debug.LogError($"[Bootstrapper] Roots/Env Error: {e.Message}"); }
            yield return null;

            // Step 3: Manager Creation
            ScoreManager scoreManager = null;
            EconomyManager economyManager = null;
            RewardedAdManager rewardedAdManager = null;
            InterstitialAdManager interstitialAdManager = null;
            InAppReviewManager inAppReviewManager = null;
            CoinStoreManager coinStoreManager = null;
            AudioManager audioManager = null;
            BannerAdManager bannerAdManager = null;
            FirebaseCloudManager firebaseCloudManager = null;
            RunManager runManager = null;
            ChapterManager chapterManager = null;

            try {
                LogVerbose("[Bootstrapper] Creating Managers...");
                scoreManager = EnsureComponent<ScoreManager>(EnsureChild(managersRoot, "ScoreManager"));
                economyManager = EnsureComponent<EconomyManager>(EnsureChild(managersRoot, "EconomyManager"));
                rewardedAdManager = EnsureComponent<RewardedAdManager>(EnsureChild(managersRoot, "RewardedAdManager"));
                interstitialAdManager = EnsureComponent<InterstitialAdManager>(EnsureChild(managersRoot, "InterstitialAdManager"));
                inAppReviewManager = EnsureComponent<InAppReviewManager>(EnsureChild(managersRoot, "InAppReviewManager"));
                coinStoreManager = EnsureComponent<CoinStoreManager>(EnsureChild(managersRoot, "CoinStoreManager"));
                audioManager = EnsureComponent<AudioManager>(EnsureChild(managersRoot, "AudioManager"));
                bannerAdManager = EnsureComponent<BannerAdManager>(EnsureChild(managersRoot, "BannerAdManager"));
                firebaseCloudManager = EnsureComponent<FirebaseCloudManager>(EnsureChild(managersRoot, "FirebaseCloudManager"));
                runManager = EnsureComponent<RunManager>(EnsureChild(managersRoot, "RunManager"));
                chapterManager = EnsureComponent<ChapterManager>(EnsureChild(managersRoot, "ChapterManager"));
                LogVerbose("[Bootstrapper] All Managers created");
            } catch (System.Exception e) { Debug.LogError($"[Bootstrapper] Manager Creation Error: {e.Message}"); }
            yield return null;
            
            // Step 4: Tower and Player Setup
            TowerGenerator towerGenerator = null;
            Transform towerRoot = null;
            LavaController lavaController = null;
            PlayerController playerController = null;
            Camera mainCamera = null;
            CameraFollowController cameraFollow = null;
            EnvironmentBackdropController backdropController = null;
            UIManager uiManager = null;
            Transform cameraTarget = null;

            try {
                LogVerbose("[Bootstrapper] Creating Tower System...");
                Transform towerMotionRoot = EnsureChild(towerSystemRoot, "TowerMotionRoot");
                EnsureComponent<TowerSinkController>(towerMotionRoot);
                towerRoot = EnsureChild(towerMotionRoot, "TowerRoot");
                EnsureComponent<TowerRotationController>(towerRoot);
                towerGenerator = EnsureComponent<TowerGenerator>(towerRoot);
                lavaController = EnsureComponent<LavaController>(EnsureChild(towerSystemRoot, "Lava"));
                LogVerbose("[Bootstrapper] Tower System created");
            } catch (System.Exception e) { Debug.LogError($"[Bootstrapper] Tower Setup Error: {e.Message}"); }
            yield return null;

            try {
                LogVerbose("[Bootstrapper] Creating Player...");
                Transform playerVisualRoot = EnsureChild(playerRoot, "Visual");
                cameraTarget = EnsureChild(playerRoot, "CameraTarget");
                cameraTarget.localPosition = new Vector3(0f, 0.34f, 0f);
                EnsureComponent<PlayerInputHandler>(playerRoot);
                playerController = EnsureComponent<PlayerController>(playerRoot);
                EnsureComponent<HeroVisualController>(playerVisualRoot);
                LogVerbose("[Bootstrapper] Player created");
            } catch (System.Exception e) { Debug.LogError($"[Bootstrapper] Player Setup Error: {e.Message}"); }
            yield return null;

            try {
                mainCamera = EnsureCamera();
                mainCamera.transform.SetParent(cameraRigRoot, false);
                cameraFollow = EnsureComponent<CameraFollowController>(mainCamera.transform);
                backdropController = EnsureComponent<EnvironmentBackdropController>(EnsureChild(vfxRoot, "Backdrop"));
                uiManager = EnsureComponent<UIManager>(uiRoot);
            } catch (System.Exception e) { Debug.LogError($"[Bootstrapper] Camera/UI Setup Error: {e.Message}"); }
            yield return null;

            // Step 5: Splash Screen
            try {
                Texture2D splashTex = Resources.Load<Texture2D>("TowerMaze/UITheme/SplashBackground");
                Font splashFont = Resources.Load<Font>("TowerMaze/UITheme/Outfit-Bold");
                SplashScreenController splashController = new GameObject("SplashScreen").AddComponent<SplashScreenController>();
                splashController.Initialize(splashFont, splashTex, onComplete: () => uiManager.OnSplashComplete());
            } catch (System.Exception e) { Debug.LogError($"[Bootstrapper] Splash Error: {e.Message}"); }
            yield return null;

            // Step 6: System Initialization
            try {
                LogVerbose("[Bootstrapper] Initializing systems...");
                towerGenerator.Initialize(gameConfig, difficultyProfile, themeDefinition);
                lavaController.Initialize(gameConfig, themeDefinition);
                playerController.Initialize(gameConfig, towerGenerator, towerRoot, themeDefinition, audioManager, cameraFollow);
                scoreManager.Initialize(gameConfig);
                economyManager.Initialize();
            } catch (System.Exception e) { Debug.LogError($"[Bootstrapper] System Init Error: {e.Message}"); }
            yield return null;

            try {
                int chapterBaseSeed = gameConfig != null ? gameConfig.seed : 1347;
                float chapterBallSpeed = gameConfig != null ? gameConfig.climbSpeed : 2.65f;
                ChapterSeedTable preBakedSeeds = Resources.Load<ChapterSeedTable>("TowerMaze/ChapterSeedTable");
                chapterManager.Initialize(chapterBaseSeed, chapterBallSpeed, preBakedSeeds);
                coinStoreManager.Initialize(economyManager);
            } catch (System.Exception e) { Debug.LogError($"[Bootstrapper] Chapter/Store Error: {e.Message}"); }
            yield return null;
            
            try {
                System.Action applyTowerVisuals = () => {
                    if (economyManager != null && towerGenerator != null)
                        towerGenerator.ApplyTowerSkin(economyManager.GetEquippedTowerSkin(), economyManager.EmberBalance);
                };
                applyTowerVisuals();
                economyManager.EmberBalanceChanged += _ => applyTowerVisuals();
                economyManager.EquippedTowerSkinChanged += _ => applyTowerVisuals();

                inAppReviewManager.Initialize();
                playerController.ApplySkin(economyManager.GetEquippedSkin());
                cameraFollow.Initialize(cameraTarget);
                backdropController.Initialize(themeDefinition, mainCamera, cameraTarget);
            } catch (System.Exception e) { Debug.LogError($"[Bootstrapper] Visuals/Review Error: {e.Message}"); }
            yield return null;

            try {
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
            } catch (System.Exception e) { Debug.LogError($"[Bootstrapper] UI/RunManager Error: {e.Message}"); }
            yield return null;

            // Step 7: Third-party systems
            StartCoroutine(InitThirdPartySystemsRoutine(gameConfig, rewardedAdManager, interstitialAdManager, bannerAdManager, firebaseCloudManager, economyManager, scoreManager, coinStoreManager, uiManager));
            
            initialized = true;
            LogVerbose("[Bootstrapper] BootstrapRoutine completed successfully.");
        }

        private IEnumerator InitThirdPartySystemsRoutine(GameConfig config, RewardedAdManager rewarded, InterstitialAdManager interstitial, BannerAdManager banner, FirebaseCloudManager firebase, EconomyManager economy, ScoreManager score, CoinStoreManager coinStore, UIManager ui)
        {
            // Initial delay to allow UI and game world to be fully visible
            yield return new WaitForSeconds(0.2f);

            LogVerbose("[Bootstrapper] Initializing Analytics (Deferred)...");
            try 
            {
                AnalyticsManager.Initialize();
            } 
            catch (System.Exception ex) 
            {
                Debug.LogWarning($"[Bootstrapper] Analytics initialization failed: {ex.Message}");
            }

            yield return null;

            LogVerbose("[Bootstrapper] Initializing Firebase Cloud (Deferred)...");
            try 
            {
                if (firebase != null)
                {
                    firebase.Initialize(config, economy, score, coinStore, ui, chapterManager);
                    firebase.NicknameRequired += () =>
                    {
                        if (ui != null)
                        {
                            ui.ShowNicknamePopup((name, onComplete) =>
                            {
                                firebase.TrySetNickname(name, onComplete);
                            });
                        }
                    };
                    // First-launch onboarding: if no nickname is cached locally, fire
                    // the popup proactively now (instead of waiting for the player to
                    // post a score). Idempotent within the session via the existing
                    // nicknamePromptRequestedThisSession flag.
                    if (string.IsNullOrEmpty(PlayerPrefs.GetString("TowerMaze.Firebase.Nickname", string.Empty)))
                    {
                        firebase.RequestNicknameNow();
                    }
                }
            } 
            catch (System.Exception ex) 
            {
                Debug.LogWarning($"[Bootstrapper] Firebase initialization failed: {ex.Message}");
            }

            yield return new WaitForSeconds(0.3f); // Further delay before Ads

            LogVerbose("[Bootstrapper] Initializing Ad Systems (Deferred)...");
            // Small delay to allow the engine and other managers to settle
            yield return new WaitForSeconds(0.5f);
            
            try 
            {
                if (rewarded != null)
                {
                    rewarded.Initialize(config);
                    LogVerbose("[Bootstrapper] RewardedAdManager initialization requested");
                }
            } 
            catch (System.Exception ex) 
            {
                Debug.LogWarning($"[Bootstrapper] RewardedAdManager failed: {ex.Message}");
            }

            yield return null; // Yield between each to spread load

            try 
            {
                if (interstitial != null)
                {
                    interstitial.Initialize(config);
                    LogVerbose("[Bootstrapper] InterstitialAdManager initialization requested");
                }
            } 
            catch (System.Exception ex) 
            {
                Debug.LogWarning($"[Bootstrapper] InterstitialAdManager failed: {ex.Message}");
            }

            yield return null;

            try 
            {
                if (banner != null)
                {
                    banner.Initialize(config);
                    LogVerbose("[Bootstrapper] BannerAdManager initialization requested");
                }
            } 
            catch (System.Exception ex) 
            {
                Debug.LogWarning($"[Bootstrapper] BannerAdManager failed: {ex.Message}");
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
                if (existing.GetComponent<AudioListener>() == null)
                {
                    existing.gameObject.AddComponent<AudioListener>();
                }
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
