using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
#if GOOGLE_MOBILE_ADS
using GoogleMobileAds.Api;
#endif

namespace TowerMaze
{
    internal sealed class AdCallbackDispatcher : MonoBehaviour
    {
        private static readonly Queue<Action> PendingActions = new Queue<Action>();
        private static readonly object PendingActionsLock = new object();

        private static AdCallbackDispatcher instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetStatics()
        {
            lock (PendingActionsLock)
            {
                PendingActions.Clear();
            }

            instance = null;
        }

        public static void EnsureInstance()
        {
            if (instance != null)
            {
                return;
            }

            GameObject dispatcherObject = new GameObject("AdCallbackDispatcher");
            DontDestroyOnLoad(dispatcherObject);
            instance = dispatcherObject.AddComponent<AdCallbackDispatcher>();
        }

        public static void Enqueue(Action action)
        {
            if (action == null)
            {
                return;
            }

            lock (PendingActionsLock)
            {
                PendingActions.Enqueue(action);
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            List<Action> actionsToRun = null;
            lock (PendingActionsLock)
            {
                if (PendingActions.Count > 0)
                {
                    actionsToRun = new List<Action>(PendingActions);
                    PendingActions.Clear();
                }
            }

            if (actionsToRun != null)
            {
                foreach (var action in actionsToRun)
                {
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }
            }
        }
    }

    public enum RewardedPlacement
    {
        DoubleRunReward = 0,
        DailyBonusChest = 1,
        ShopCoinBoost = 2,
        LifeRefill = 3,
    }

    public enum DailyMissionType
    {
        ReachHeightInRun = 0,
        ReachZoneInRun = 1,
        CompleteRuns = 2,
        SurviveRushEvents = 3,
        FinishWithoutContinue = 4,
        StayNearLavaSeconds = 5,
        SetNewBest = 6,
        PlayDailyChallenge = 7,
        ReachHeightInDailyChallenge = 8,
        ReachHeightUnderTime = 9,
        CompleteRunsWithModifier = 10,
    }

    [Serializable]
    public struct DailyMissionState
    {
        public string id;
        public DailyMissionType type;
        public string description;
        public int targetValue;
        public int progressValue;
        public int secondaryTargetValue;
        public int rewardEmber;
        public string contextValue;
        public bool claimed;

        public bool IsCompleted => progressValue >= targetValue;
    }

    [Serializable]
    public struct DailyChestStatus
    {
        public bool canClaim;
        public bool requiresRewardedAd;
        public string buttonLabel;
        public string statusLabel;
        public int rewardPreview;

        public DailyChestStatus(bool canClaim, bool requiresRewardedAd, string buttonLabel, string statusLabel, int rewardPreview)
        {
            this.canClaim = canClaim;
            this.requiresRewardedAd = requiresRewardedAd;
            this.buttonLabel = buttonLabel;
            this.statusLabel = statusLabel;
            this.rewardPreview = rewardPreview;
        }
    }

    [Serializable]
    public struct DailyMissionRewardResult
    {
        public int rewardEmber;
        public int completedMissionCount;

        public DailyMissionRewardResult(int rewardEmber, int completedMissionCount)
        {
            this.rewardEmber = rewardEmber;
            this.completedMissionCount = completedMissionCount;
        }
    }

    [Serializable]
    internal sealed class DailyMissionSaveData
    {
        public string dateKey;
        public List<DailyMissionState> missions = new();
        public string lastFreeChestClaimDateKey;
        public string lastBonusChestClaimDateKey;
        public int rerollCount;
        public int rewardPatternIndex;
    }

    public sealed class RewardedAdManager : MonoBehaviour
    {
        private const string AndroidTestRewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";
        private const string IosTestRewardedAdUnitId = "ca-app-pub-3940256099942544/1712485313";

        [SerializeField] private bool usingSimulatedProvider = true;
#if GOOGLE_MOBILE_ADS
        [SerializeField] private bool adLoading;
#endif
        [SerializeField] private bool adReady;
        [SerializeField] private bool adShowing;

        private GameConfig config;
        private Action<bool> completionCallback;

#if GOOGLE_MOBILE_ADS
        private RewardedAd rewardedAd;
        private bool rewardEarned;
        internal static bool sdkInitialized;
        internal static bool sdkInitializing;
        internal static readonly List<Action> onSdkInitialized = new();
#endif

        public bool IsSimulatedProvider => usingSimulatedProvider;
        public bool CanShowRewarded => usingSimulatedProvider || adReady;

        public void Initialize(GameConfig gameConfig)
        {
            config = gameConfig;
            // Removed force simulation for editor to allow testing actual Google Test Ads
            usingSimulatedProvider = (config != null && config.useSimulatedRewardedAds);
            adShowing = false;
            adReady = false;

            if (usingSimulatedProvider)
            {
                if (Debug.isDebugBuild) Debug.Log("[RewardedAdManager] Using SIMULATED provider.");
                adReady = true;
                return;
            }

            string adUnitId = ResolveRewardedAdUnitId();
            if (string.IsNullOrWhiteSpace(adUnitId))
            {
                Debug.LogWarning("[RewardedAdManager] Rewarded ads disabled: no ad unit ID resolved.");
                return;
            }

            if (Debug.isDebugBuild) Debug.Log($"[RewardedAdManager] Initializing with Ad Unit: {adUnitId}");

#if GOOGLE_MOBILE_ADS
            AdCallbackDispatcher.EnsureInstance();
            adLoading = false;
            
            if (sdkInitialized)
            {
                if (Debug.isDebugBuild) Debug.Log("[RewardedAdManager] SDK already initialized. Loading first ad...");
                LoadRewardedAd();
                return;
            }

            if (sdkInitializing)
            {
                if (Debug.isDebugBuild) Debug.Log("[RewardedAdManager] SDK initialization already in progress...");
                return;
            }

            sdkInitializing = true;
            if (Debug.isDebugBuild) Debug.Log("[RewardedAdManager] Initializing Google Mobile Ads SDK...");
            MobileAds.Initialize(status => {
                AdCallbackDispatcher.Enqueue(() =>
                {
                    sdkInitialized = true;
                    sdkInitializing = false;
                    if (Debug.isDebugBuild) Debug.Log("[RewardedAdManager] SDK Initialized.");
                    foreach (var action in onSdkInitialized) action?.Invoke();
                    onSdkInitialized.Clear();
                    LoadRewardedAd();
                });
            });
#else
            Debug.LogWarning("[RewardedAdManager] Rewarded ads disabled: Google Mobile Ads SDK is not installed or GOOGLE_MOBILE_ADS define is missing.");
#endif
        }

        public void ShowRewarded(RewardedPlacement placement, Action<bool> onCompleted)
        {
            if (adShowing)
            {
                onCompleted?.Invoke(false);
                return;
            }

            completionCallback = onCompleted;
            AnalyticsManager.LogEvent("rewarded_ad_requested", new Dictionary<string, object> { { "placement", placement.ToString() } });

            if (usingSimulatedProvider)
            {
                StartCoroutine(SimulateRewardedFlow());
                return;
            }

#if GOOGLE_MOBILE_ADS
            if (rewardedAd == null || !rewardedAd.CanShowAd())
            {
                Debug.LogWarning("[RewardedAdManager] Ad NOT ready! Firing failure callback and reloading.");
                completionCallback?.Invoke(false);
                completionCallback = null;
                LoadRewardedAd();
                return;
            }

            if (Debug.isDebugBuild) Debug.Log("[RewardedAdManager] Showing REAL Rewarded Ad...");
            adShowing = true;
            adReady = false;
            rewardEarned = false;
            rewardedAd.Show(_ => {
                AdCallbackDispatcher.Enqueue(() =>
                {
                    if (Debug.isDebugBuild) Debug.Log("[RewardedAdManager] Reward Earned callback received.");
                    rewardEarned = true;
                });
            });
#else
            completionCallback?.Invoke(false);
            completionCallback = null;
#endif
        }

        private IEnumerator SimulateRewardedFlow()
        {
            adShowing = true;
            adReady = false;
            yield return new WaitForSecondsRealtime(Mathf.Max(0.15f, config != null ? config.simulatedRewardedDuration : 0.8f));
            adShowing = false;
            adReady = true;
            completionCallback?.Invoke(true);
            completionCallback = null;
        }

#if GOOGLE_MOBILE_ADS
        private void LoadRewardedAd()
        {
            if (usingSimulatedProvider || adLoading)
            {
                return;
            }

            string adUnitId = ResolveRewardedAdUnitId();
            if (string.IsNullOrWhiteSpace(adUnitId))
            {
                adReady = false;
                return;
            }

            adLoading = true;
            adReady = false;
            AdRequest request = new();
            RewardedAd.Load(adUnitId, request, (RewardedAd ad, LoadAdError error) =>
            {
                AdCallbackDispatcher.Enqueue(() =>
                {
                    adLoading = false;
                    if (error != null || ad == null)
                    {
                        adReady = false;
                        return;
                    }

                    rewardedAd = ad;
                    RegisterRewardedCallbacks(rewardedAd);
                    adReady = true;
                });
            });
        }

        private void RegisterRewardedCallbacks(RewardedAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                AdCallbackDispatcher.Enqueue(() =>
                {
                    adShowing = false;
                    completionCallback?.Invoke(rewardEarned);
                    completionCallback = null;
                    LoadRewardedAd();
                });
            };

            ad.OnAdFullScreenContentFailed += _ =>
            {
                AdCallbackDispatcher.Enqueue(() =>
                {
                    adShowing = false;
                    completionCallback?.Invoke(false);
                    completionCallback = null;
                    LoadRewardedAd();
                });
            };
        }

#endif

        private string ResolveRewardedAdUnitId()
        {
            if (config == null)
            {
                return string.Empty;
            }

#if UNITY_ANDROID
            return NormalizeRewardedAdUnitId(config.androidRewardedAdUnitId, AndroidTestRewardedAdUnitId);
#elif UNITY_IOS
            return NormalizeRewardedAdUnitId(config.iosRewardedAdUnitId, IosTestRewardedAdUnitId);
#else
            return string.Empty;
#endif
        }

        private static string NormalizeRewardedAdUnitId(string configuredId, string testId)
        {
            string normalizedId = string.IsNullOrWhiteSpace(configuredId)
                ? string.Empty
                : configuredId.Trim();

            if (Application.isEditor)
            {
                return string.IsNullOrWhiteSpace(normalizedId) ? testId : normalizedId;
            }

            if (string.IsNullOrWhiteSpace(normalizedId))
            {
                return string.Empty;
            }

            if (string.Equals(normalizedId, testId, StringComparison.Ordinal))
            {
                Debug.LogWarning("[RewardedAdManager] Rewarded ads disabled: Google test ad unit IDs are editor-only in device builds.");
                return string.Empty;
            }

            return normalizedId;
        }
    }

    public sealed class InterstitialAdManager : MonoBehaviour
    {
        private const string AndroidTestInterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";
        private const string IosTestInterstitialAdUnitId = "ca-app-pub-3940256099942544/4411468910";

        [SerializeField] private bool usingSimulatedProvider = true;
#if GOOGLE_MOBILE_ADS
        [SerializeField] private bool adLoading;
#endif
        [SerializeField] private bool adReady;
        [SerializeField] private bool adShowing;

        private GameConfig config;
        private CoinStoreManager coinStoreManager;
        private Action<bool> completionCallback;

#if GOOGLE_MOBILE_ADS
        private InterstitialAd interstitialAd;
#endif

        public bool CanShowInterstitial => !IsInterstitialSuppressed && (usingSimulatedProvider || adReady);

        private bool IsInterstitialSuppressed => coinStoreManager != null && coinStoreManager.HasNoAds;

        public void Initialize(GameConfig gameConfig)
        {
            config = gameConfig;
            coinStoreManager ??= FindAnyObjectByType<CoinStoreManager>();
            // Removed force simulation for editor to allow testing actual Google Test Ads
            usingSimulatedProvider = (config != null && config.useSimulatedRewardedAds);
            adShowing = false;
            adReady = false;

            if (usingSimulatedProvider)
            {
                if (Debug.isDebugBuild) Debug.Log("[InterstitialAdManager] Using SIMULATED provider.");
                adReady = true;
                return;
            }

            string adUnitId = ResolveInterstitialAdUnitId();
            if (string.IsNullOrWhiteSpace(adUnitId))
            {
                Debug.LogWarning("[InterstitialAdManager] Interstitial ads disabled: no production ad unit ID configured.");
                return;
            }

#if GOOGLE_MOBILE_ADS
            AdCallbackDispatcher.EnsureInstance();
            adLoading = false;
            if (RewardedAdManager.sdkInitialized)
            {
                LoadInterstitialAd();
            }
            else
            {
                RewardedAdManager.onSdkInitialized.Add(LoadInterstitialAd);
                if (!RewardedAdManager.sdkInitializing)
                {
                    RewardedAdManager.sdkInitializing = true;
                    MobileAds.Initialize(_ => AdCallbackDispatcher.Enqueue(() => {
                        RewardedAdManager.sdkInitialized = true;
                        RewardedAdManager.sdkInitializing = false;
                        foreach (var action in RewardedAdManager.onSdkInitialized) action?.Invoke();
                        RewardedAdManager.onSdkInitialized.Clear();
                    }));
                }
            }
#else
            Debug.LogWarning("[InterstitialAdManager] Interstitial ads disabled: Google Mobile Ads SDK is not installed.");
#endif
        }

        public void ShowInterstitial(Action<bool> onCompleted)
        {
            if (IsInterstitialSuppressed)
            {
                onCompleted?.Invoke(false);
                return;
            }

            if (adShowing)
            {
                onCompleted?.Invoke(false);
                return;
            }

            completionCallback = onCompleted;

            if (usingSimulatedProvider)
            {
                StartCoroutine(SimulateInterstitialFlow());
                return;
            }

#if GOOGLE_MOBILE_ADS
            if (interstitialAd == null || !interstitialAd.CanShowAd())
            {
                completionCallback?.Invoke(false);
                completionCallback = null;
                LoadInterstitialAd();
                return;
            }

            adShowing = true;
            adReady = false;
            interstitialAd.Show();
#else
            completionCallback?.Invoke(false);
            completionCallback = null;
#endif
        }

        private IEnumerator SimulateInterstitialFlow()
        {
            adShowing = true;
            adReady = false;
            yield return new WaitForSecondsRealtime(Mathf.Max(0.15f, config != null ? config.simulatedRewardedDuration : 0.8f));
            adShowing = false;
            adReady = true;
            completionCallback?.Invoke(true);
            completionCallback = null;
        }

#if GOOGLE_MOBILE_ADS
        private void LoadInterstitialAd()
        {
            if (usingSimulatedProvider || adLoading)
            {
                return;
            }

            string adUnitId = ResolveInterstitialAdUnitId();
            if (string.IsNullOrWhiteSpace(adUnitId))
            {
                adReady = false;
                return;
            }

            adLoading = true;
            adReady = false;
            AdRequest request = new();
            InterstitialAd.Load(adUnitId, request, (InterstitialAd ad, LoadAdError error) =>
            {
                AdCallbackDispatcher.Enqueue(() =>
                {
                    adLoading = false;
                    if (error != null || ad == null)
                    {
                        adReady = false;
                        return;
                    }

                    interstitialAd = ad;
                    RegisterInterstitialCallbacks(interstitialAd);
                    adReady = true;
                });
            });
        }

        private void RegisterInterstitialCallbacks(InterstitialAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                AdCallbackDispatcher.Enqueue(() =>
                {
                    adShowing = false;
                    completionCallback?.Invoke(true);
                    completionCallback = null;
                    LoadInterstitialAd();
                });
            };

            ad.OnAdFullScreenContentFailed += _ =>
            {
                AdCallbackDispatcher.Enqueue(() =>
                {
                    adShowing = false;
                    completionCallback?.Invoke(false);
                    completionCallback = null;
                    LoadInterstitialAd();
                });
            };
        }
#endif

        private string ResolveInterstitialAdUnitId()
        {
            if (config == null)
            {
                return string.Empty;
            }

#if UNITY_ANDROID
            return NormalizeAdUnitId(config.androidInterstitialAdUnitId, AndroidTestInterstitialAdUnitId);
#elif UNITY_IOS
            return NormalizeAdUnitId(config.iosInterstitialAdUnitId, IosTestInterstitialAdUnitId);
#else
            return string.Empty;
#endif
        }

        private static string NormalizeAdUnitId(string configuredId, string testId)
        {
            string normalizedId = string.IsNullOrWhiteSpace(configuredId)
                ? string.Empty
                : configuredId.Trim();

            if (Application.isEditor)
            {
                return string.IsNullOrWhiteSpace(normalizedId) ? testId : normalizedId;
            }

            if (string.IsNullOrWhiteSpace(normalizedId))
            {
                return string.Empty;
            }

            if (string.Equals(normalizedId, testId, StringComparison.Ordinal))
            {
                Debug.LogWarning("[InterstitialAdManager] Interstitial ads disabled: Google test ad unit IDs are editor-only in device builds.");
                return string.Empty;
            }

            return normalizedId;
        }
    }

    public sealed class BannerAdManager : MonoBehaviour
    {
        private const string AndroidTestBannerAdUnitId = "ca-app-pub-3940256099942544/6300978111";
        private const string IosTestBannerAdUnitId = "ca-app-pub-3940256099942544/2934735716";
        private const float RetryBaseDelay = 30f;
        private const float RetryMaxDelay = 300f;

        private GameConfig config;
#if GOOGLE_MOBILE_ADS
        private BannerView bannerView;
#endif
        private bool isInitialized;
        private bool isVisible;
        private bool shouldBeVisible;
        private bool isLoading;
        private int retryCount;

        public void Initialize(GameConfig gameConfig)
        {
            config = gameConfig;
            isInitialized = false;
            isVisible = false;
            shouldBeVisible = false;

            string adUnitId = ResolveBannerAdUnitId();
            if (string.IsNullOrWhiteSpace(adUnitId))
            {
                return;
            }

#if GOOGLE_MOBILE_ADS
            AdCallbackDispatcher.EnsureInstance();
            if (RewardedAdManager.sdkInitialized)
            {
                isInitialized = true;
                LoadBannerAd();
                if (shouldBeVisible) ShowBanner();
            }
            else
            {
                RewardedAdManager.onSdkInitialized.Add(() => {
                    isInitialized = true;
                    LoadBannerAd();
                    if (shouldBeVisible) ShowBanner();
                });
                if (!RewardedAdManager.sdkInitializing)
                {
                    RewardedAdManager.sdkInitializing = true;
                    MobileAds.Initialize(_ => AdCallbackDispatcher.Enqueue(() => {
                        RewardedAdManager.sdkInitialized = true;
                        RewardedAdManager.sdkInitializing = false;
                        foreach (var action in RewardedAdManager.onSdkInitialized) action?.Invoke();
                        RewardedAdManager.onSdkInitialized.Clear();
                    }));
                }
            }
#else
            Debug.LogWarning("[BannerAdManager] Banner ads disabled: Google Mobile Ads SDK is not installed.");
#endif
        }

        public void ShowBanner()
        {
            shouldBeVisible = true;
            if (isVisible) return;
            
            if (!isInitialized)
            {
                return;
            }

#if GOOGLE_MOBILE_ADS
            if (bannerView != null)
            {
                bannerView.Show();
                isVisible = true;
            }
            else if (isInitialized && !isLoading)
            {
                LoadBannerAd();
            }
#endif
        }

        public void HideBanner()
        {
            shouldBeVisible = false;
            if (!isVisible) return;

#if GOOGLE_MOBILE_ADS
            if (bannerView != null)
            {
                bannerView.Hide();
            }
#endif
            isVisible = false;
        }

#if GOOGLE_MOBILE_ADS
        private void LoadBannerAd()
        {
            if (isLoading) return;
            isLoading = true;

            if (bannerView != null)
            {
                bannerView.Destroy();
            }

            string adUnitId = ResolveBannerAdUnitId();
            bannerView = new BannerView(adUnitId, AdSize.Banner, AdPosition.Bottom);

            bannerView.OnBannerAdLoaded += () => {
                AdCallbackDispatcher.Enqueue(() =>
                {
                    isLoading = false;
                    retryCount = 0;
                    if (shouldBeVisible)
                    {
                        bannerView.Show();
                        isVisible = true;
                    }
                });
            };
            bannerView.OnBannerAdLoadFailed += (error) => {
                AdCallbackDispatcher.Enqueue(() =>
                {
                    isLoading = false;
                    float delay = Mathf.Min(RetryBaseDelay * Mathf.Pow(2, retryCount), RetryMaxDelay);
                    retryCount++;
                    Debug.LogWarning($"[BannerAdManager] Banner ad no fill, retrying in {delay:F0}s.");
                    StartCoroutine(RetryLoadRoutine(delay));
                });
            };

            AdRequest request = new AdRequest();
            bannerView.LoadAd(request);

            // Start hidden until explicitly shown
            bannerView.Hide();
        }

        private IEnumerator RetryLoadRoutine(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (isInitialized && !isLoading)
            {
                LoadBannerAd();
            }
        }
#endif

        private void OnDestroy()
        {
#if GOOGLE_MOBILE_ADS
            if (bannerView != null)
            {
                bannerView.Destroy();
            }
#endif
        }

        private string ResolveBannerAdUnitId()
        {
            if (config == null) return string.Empty;

#if UNITY_ANDROID
            return NormalizeAdUnitId(config.androidBannerAdUnitId, AndroidTestBannerAdUnitId);
#elif UNITY_IOS
            return NormalizeAdUnitId(config.iosBannerAdUnitId, IosTestBannerAdUnitId);
#else
            return string.Empty;
#endif
        }

        private static string NormalizeAdUnitId(string configuredId, string testId)
        {
            string normalizedId = string.IsNullOrWhiteSpace(configuredId)
                ? string.Empty
                : configuredId.Trim();

            if (Application.isEditor)
            {
                return string.IsNullOrWhiteSpace(normalizedId) ? testId : normalizedId;
            }

            if (string.IsNullOrWhiteSpace(normalizedId))
            {
                return string.Empty;
            }

            if (string.Equals(normalizedId, testId, StringComparison.Ordinal))
            {
                Debug.LogWarning("[BannerAdManager] Banner ads disabled: Google test ad unit IDs are editor-only in device builds.");
                return string.Empty;
            }

            return normalizedId;
        }
    }

    [Serializable]
    public enum StoreOfferKind
    {
        CoinPack = 0,
        WelcomePack = 1,
        NoAds = 2,
        ExclusiveBundle = 3,
    }

    [Serializable]
    public struct CoinPackOffer
    {
        public string id;
        public string productId;
        public string displayName;
        public int coinAmount;
        public string priceLabel;
        public string badgeLabel;
        public string bonusLabel;
        public bool featured;
        public bool owned;
        public StoreOfferKind kind;
        public ProductType productType;
        public string ballSkinId;
        public string towerSkinId;

        public CoinPackOffer(
            string id,
            string productId,
            string displayName,
            int coinAmount,
            string priceLabel,
            string badgeLabel = "",
            string bonusLabel = "",
            bool featured = false,
            StoreOfferKind kind = StoreOfferKind.CoinPack,
            ProductType productType = ProductType.Consumable,
            string ballSkinId = "",
            string towerSkinId = "",
            bool owned = false)
        {
            this.id = id;
            this.productId = productId;
            this.displayName = displayName;
            this.coinAmount = coinAmount;
            this.priceLabel = priceLabel;
            this.badgeLabel = badgeLabel;
            this.bonusLabel = bonusLabel;
            this.featured = featured;
            this.kind = kind;
            this.productType = productType;
            this.ballSkinId = ballSkinId;
            this.towerSkinId = towerSkinId;
            this.owned = owned;
        }
    }

    public enum CoinPackPurchaseStatus
    {
        None = 0,
        Succeeded = 1,
        Failed = 2,
        Unavailable = 3,
        Pending = 4,
    }

    public readonly struct CoinPackPurchaseResult
    {
        public readonly CoinPackPurchaseStatus status;
        public readonly CoinPackOffer offer;
        public readonly string message;

        public CoinPackPurchaseResult(CoinPackPurchaseStatus status, CoinPackOffer offer, string message)
        {
            this.status = status;
            this.offer = offer;
            this.message = message;
        }
    }

    public sealed class CoinStoreManager : MonoBehaviour
    {
        private const string PurchasedOfferIdsKey = "TowerMaze.Store.OwnedOffers";
        [SerializeField] private bool simulatePurchasesInEditor = true;
        [SerializeField] private bool simulatePurchasesInDevelopmentBuilds;
        [SerializeField] private bool connectToStoreOnInitialize = true;

        private readonly List<CoinPackOffer> offers = new();
        private readonly HashSet<string> grantedTransactionIds = new();
        private readonly HashSet<string> ownedOfferIds = new();
        private EconomyManager economyManager;
        private StoreController storeController;
        private bool storeConnected;
        private bool productsFetched;
        private bool initializationRequested;
        private bool purchaseInProgress;
        private bool restoreRequested;
        private string pendingPackId = string.Empty;
        private string pendingProductId = string.Empty;

        public IReadOnlyList<CoinPackOffer> Offers => offers;
        public bool IsStoreReady => storeConnected && productsFetched;
        public bool HasNoAds => ownedOfferIds.Contains("no_ads_pack");
        public event Action OffersChanged;
        public event Action<CoinPackPurchaseResult> PurchaseFinished;
        public event Action<bool, string> RestoreFinished;
        public event Action StateChanged;

        public void Initialize(EconomyManager economy)
        {
            economyManager = economy;
            BuildCatalog();
            LoadOwnedOffers();
            ApplyOwnedFlags();
            if (!ShouldSimulatePurchase() && connectToStoreOnInitialize)
            {
                InitializeStoreController();
            }
        }

        public CoinPackPurchaseResult PurchasePack(string packId)
        {
            CoinPackOffer offer = GetOffer(packId);
            if (string.IsNullOrWhiteSpace(offer.id))
            {
                return new CoinPackPurchaseResult(CoinPackPurchaseStatus.Failed, default, UILanguage.Translate("PAKET BULUNAMADI", "PACK NOT FOUND", "PAQUETE NO ENCONTRADO"));
            }

            if (offer.productType != ProductType.Consumable && offer.owned)
            {
                return new CoinPackPurchaseResult(CoinPackPurchaseStatus.Failed, offer, UILanguage.Translate("ZATEN SAHIP", "ALREADY OWNED", "YA OBTENIDO"));
            }

            if (economyManager == null)
            {
                return new CoinPackPurchaseResult(CoinPackPurchaseStatus.Failed, offer, $"{GetStoreDisplayName()} {UILanguage.Translate("HAZIR DEGIL", "NOT READY", "NO LISTO")}");
            }

            if (ShouldSimulatePurchase())
            {
                bool granted = ApplyOfferRewards(offer, out string rewardMessage);
                CoinPackOffer updatedOffer = GetOffer(packId);
                return new CoinPackPurchaseResult(
                    granted ? CoinPackPurchaseStatus.Succeeded : CoinPackPurchaseStatus.Failed,
                    updatedOffer,
                    granted ? rewardMessage : UILanguage.Translate("TEST SATIN ALMA BASARISIZ", "TEST PURCHASE FAILED", "COMPRA DE PRUEBA FALLIDA"));
            }

            if (purchaseInProgress)
            {
                return new CoinPackPurchaseResult(CoinPackPurchaseStatus.Failed, offer, UILanguage.Translate("SATIN ALMA SURUYOR", "PURCHASE IN PROGRESS", "COMPRA EN CURSO"));
            }

            if (storeController == null)
            {
                InitializeStoreController();
                return new CoinPackPurchaseResult(CoinPackPurchaseStatus.Pending, offer, UILanguage.Translate("MAGAZA BAGLANIYOR", "STORE CONNECTING", "CONECTANDO LA TIENDA"));
            }

            if (!storeConnected)
            {
                InitializeStoreController();
                return new CoinPackPurchaseResult(CoinPackPurchaseStatus.Pending, offer, UILanguage.Translate("MAGAZA BAGLANIYOR", "STORE CONNECTING", "CONECTANDO LA TIENDA"));
            }

            if (!productsFetched)
            {
                FetchProducts();
                return new CoinPackPurchaseResult(CoinPackPurchaseStatus.Pending, offer, $"{UILanguage.Translate("YUKLENIYOR", "LOADING", "CARGANDO")} {GetStoreDisplayName()} {UILanguage.Translate("FIYATLARI", "PRICES", "PRECIOS")}");
            }

            Product product = storeController.GetProductById(offer.productId);
            if (product == null)
            {
                FetchProducts();
                return new CoinPackPurchaseResult(CoinPackPurchaseStatus.Unavailable, offer, $"{GetStoreDisplayName()} {UILanguage.Translate("URUN HAZIR DEGIL", "PRODUCT NOT READY", "PRODUCTO NO LISTO")}");
            }

            purchaseInProgress = true;
            pendingPackId = offer.id;
            pendingProductId = offer.productId;
            AnalyticsManager.LogEvent("iap_initiated", new Dictionary<string, object> { { "product_id", offer.productId }, { "pack_id", offer.id } });
            storeController.PurchaseProduct(product);
            return new CoinPackPurchaseResult(CoinPackPurchaseStatus.Pending, offer, $"{UILanguage.Translate("ACILIYOR", "OPENING", "ABRIENDO")} {GetStoreDisplayName()}");
        }

        public void RestoreTransactions()
        {
            if (ShouldSimulatePurchase())
            {
                RestoreFinished?.Invoke(false, UILanguage.Translate("TEST MAGAZASINDA GERI YUKLENECEK OGESI YOK", "TEST STORE HAS NOTHING TO RESTORE", "LA TIENDA DE PRUEBA NO TIENE NADA QUE RESTAURAR"));
                return;
            }

            if (purchaseInProgress)
            {
                RestoreFinished?.Invoke(false, UILanguage.Translate("MEVCUT SATIN ALMAYI BEKLE", "WAIT FOR CURRENT PURCHASE", "ESPERA A LA COMPRA ACTUAL"));
                return;
            }

            if (storeController == null || !storeConnected)
            {
                restoreRequested = true;
                InitializeStoreController();
                RestoreFinished?.Invoke(false, $"{GetStoreDisplayName()} {UILanguage.Translate("BAGLANTISI KURULUYOR", "CONNECTING", "CONECTANDO")}");
                return;
            }

            ExecuteRestoreTransactions();
        }

        internal CoinStoreCloudSaveData ExportCloudData()
        {
            return new CoinStoreCloudSaveData
            {
                ownedOfferIds = new List<string>(ownedOfferIds)
            };
        }

        internal void ImportCloudData(CoinStoreCloudSaveData saveData)
        {
            ownedOfferIds.Clear();
            if (saveData?.ownedOfferIds != null)
            {
                for (int index = 0; index < saveData.ownedOfferIds.Count; index++)
                {
                    string offerId = saveData.ownedOfferIds[index];
                    if (!string.IsNullOrWhiteSpace(offerId))
                    {
                        ownedOfferIds.Add(offerId);
                    }
                }
            }

            SaveOwnedOffers();
            ApplyOwnedFlags();
            OffersChanged?.Invoke();
            StateChanged?.Invoke();
        }

        private CoinPackOffer GetOffer(string packId)
        {
            for (int i = 0; i < offers.Count; i++)
            {
                if (offers[i].id == packId || offers[i].productId == packId)
                {
                    return offers[i];
                }
            }

            return default;
        }

        private bool ShouldSimulatePurchase()
        {
            if (Application.isEditor)
            {
                return simulatePurchasesInEditor;
            }

            return Debug.isDebugBuild && simulatePurchasesInDevelopmentBuilds;
        }

        private async void InitializeStoreController()
        {
            if (initializationRequested || ShouldSimulatePurchase())
            {
                return;
            }

            initializationRequested = true;
            try
            {
                storeController = UnityIAPServices.StoreController();
                storeController.OnStoreDisconnected += OnStoreDisconnected;
                storeController.OnProductsFetched += OnProductsFetched;
                storeController.OnProductsFetchFailed += OnProductsFetchFailed;
                storeController.OnPurchasePending += OnPurchasePending;
                storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
                storeController.OnPurchaseFailed += OnPurchaseFailed;
                storeController.OnPurchaseDeferred += OnPurchaseDeferred;
                await storeController.Connect();
                storeConnected = true;
                FetchProducts();
                if (restoreRequested)
                {
                    ExecuteRestoreTransactions();
                }
            }
            catch (Exception exception)
            {
                storeConnected = false;
                initializationRequested = false;
                restoreRequested = false;
                Debug.LogWarning($"Coin store connection failed: {exception.Message}");
            }
        }

        private void FetchProducts()
        {
            if (storeController == null || !storeConnected)
            {
                return;
            }

            List<ProductDefinition> productDefinitions = new(offers.Count);
            for (int index = 0; index < offers.Count; index++)
            {
                productDefinitions.Add(new ProductDefinition(offers[index].productId, offers[index].productType));
            }

            storeController.FetchProducts(productDefinitions);
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription description)
        {
            storeConnected = false;
            productsFetched = false;
            initializationRequested = false;
            restoreRequested = false;
            Debug.LogWarning($"Coin store disconnected: {description.message}");
        }

        private void OnProductsFetched(List<Product> products)
        {
            productsFetched = true;
            initializationRequested = true;
            bool changed = false;
            for (int productIndex = 0; productIndex < products.Count; productIndex++)
            {
                Product product = products[productIndex];
                string resolvedUsdPriceLabel = ResolveUsdPriceLabel(product);
                if (string.IsNullOrWhiteSpace(resolvedUsdPriceLabel))
                {
                    continue;
                }

                int offerIndex = GetOfferIndexByProductId(product.definition.id);
                if (offerIndex < 0)
                {
                    continue;
                }

                CoinPackOffer offer = offers[offerIndex];
                if (offer.priceLabel == resolvedUsdPriceLabel)
                {
                    continue;
                }

                offer.priceLabel = resolvedUsdPriceLabel;
                offers[offerIndex] = offer;
                changed = true;
            }

            if (changed)
            {
                OffersChanged?.Invoke();
            }
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            productsFetched = false;
            Debug.LogWarning($"Coin store product fetch failed: {failure.FailureReason}");
        }

        private void OnPurchasePending(PendingOrder order)
        {
            Product product = GetFirstProductInOrder(order);
            CoinPackOffer offer = GetOfferByProductId(product != null ? product.definition.id : pendingProductId);
            if (string.IsNullOrWhiteSpace(offer.id))
            {
                purchaseInProgress = false;
                pendingPackId = string.Empty;
                pendingProductId = string.Empty;
                PurchaseFinished?.Invoke(new CoinPackPurchaseResult(CoinPackPurchaseStatus.Failed, default, UILanguage.Translate("BILINMEYEN MAGAZA URUNU", "UNKNOWN STORE PRODUCT", "PRODUCTO DE TIENDA DESCONOCIDO")));
                return;
            }

            string transactionId = order.Info.TransactionID ?? string.Empty;
            bool alreadyGranted;
            string successMessage;
            if (offer.productType == ProductType.Consumable)
            {
                alreadyGranted = !string.IsNullOrWhiteSpace(transactionId) && !grantedTransactionIds.Add(transactionId);
                if (!alreadyGranted)
                {
                    ApplyOfferRewards(offer, out successMessage);
                }
                else
                {
                    successMessage = UILanguage.Translate("SATIN ALMA GERI YUKLENDI", "PURCHASE RESTORED", "COMPRA RESTAURADA");
                }
            }
            else
            {
                alreadyGranted = IsOfferOwned(offer.id);
                if (!alreadyGranted)
                {
                    ApplyOfferRewards(offer, out successMessage);
                }
                else
                {
                    SyncOwnedOffer(offer.id);
                    successMessage = UILanguage.Translate("SATIN ALMA GERI YUKLENDI", "PURCHASE RESTORED", "COMPRA RESTAURADA");
                }
            }

            storeController?.ConfirmPurchase(order);
            AnalyticsManager.LogEvent("iap_completed", new Dictionary<string, object> { { "product_id", offer.productId }, { "pack_id", offer.id } });
            PurchaseFinished?.Invoke(new CoinPackPurchaseResult(CoinPackPurchaseStatus.Succeeded, GetOffer(offer.id), successMessage));
        }

        private void OnPurchaseConfirmed(Order order)
        {
            purchaseInProgress = false;
            pendingPackId = string.Empty;
            pendingProductId = string.Empty;
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            purchaseInProgress = false;
            CoinPackOffer offer = GetOfferByProductId(GetFirstProductInOrder(order)?.definition.id ?? pendingProductId);
            AnalyticsManager.LogEvent("iap_failed", new Dictionary<string, object> { { "product_id", offer.productId ?? pendingProductId }, { "reason", order.FailureReason.ToString() } });
            pendingPackId = string.Empty;
            pendingProductId = string.Empty;
            PurchaseFinished?.Invoke(new CoinPackPurchaseResult(CoinPackPurchaseStatus.Failed, offer, FormatPurchaseFailureMessage(order.FailureReason, order.Details)));
        }

        private void OnPurchaseDeferred(DeferredOrder order)
        {
            purchaseInProgress = false;
            CoinPackOffer offer = GetOfferByProductId(GetFirstProductInOrder(order)?.definition.id ?? pendingProductId);
            pendingPackId = string.Empty;
            pendingProductId = string.Empty;
            PurchaseFinished?.Invoke(new CoinPackPurchaseResult(CoinPackPurchaseStatus.Unavailable, offer, $"{GetStoreDisplayName()} {UILanguage.Translate("ONAY BEKLIYOR", "NEEDS APPROVAL", "NECESITA APROBACION")}"));
        }

        private Product GetFirstProductInOrder(Order order)
        {
            return order?.CartOrdered?.Items().FirstOrDefault()?.Product;
        }

        private CoinPackOffer GetOfferByProductId(string productId)
        {
            int offerIndex = GetOfferIndexByProductId(productId);
            return offerIndex >= 0 ? offers[offerIndex] : GetOffer(pendingPackId);
        }

        private int GetOfferIndexByProductId(string productId)
        {
            for (int index = 0; index < offers.Count; index++)
            {
                if (offers[index].productId == productId)
                {
                    return index;
                }
            }

            return -1;
        }

        private void ExecuteRestoreTransactions()
        {
            if (storeController == null)
            {
                restoreRequested = false;
                RestoreFinished?.Invoke(false, $"{GetStoreDisplayName()} {UILanguage.Translate("GERI YUKLEME BASARISIZ", "RESTORE FAILED", "RESTAURACION FALLIDA")}");
                return;
            }

            restoreRequested = true;
            storeController.RestoreTransactions((success, message) =>
            {
                restoreRequested = false;
                string resolvedMessage = FormatRestoreMessage(success, message);
                RestoreFinished?.Invoke(success, resolvedMessage);
            });
        }

        private string FormatPurchaseFailureMessage(PurchaseFailureReason failureReason, string details)
        {
            string storeName = GetStoreDisplayName();
            return failureReason switch
            {
                PurchaseFailureReason.PurchasingUnavailable => $"{storeName} {UILanguage.Translate("ODEME KULLANILAMIYOR", "BILLING UNAVAILABLE", "FACTURACION NO DISPONIBLE")}",
                PurchaseFailureReason.ExistingPurchasePending => UILanguage.Translate("BASKA BIR SATIN ALMA HALA ACIK", "ANOTHER PURCHASE IS ALREADY OPEN", "OTRA COMPRA YA ESTA ABIERTA"),
                PurchaseFailureReason.ProductUnavailable => $"{UILanguage.Translate("URUN KULLANILAMIYOR", "ITEM NOT AVAILABLE IN", "OBJETO NO DISPONIBLE EN")} {storeName}",
                PurchaseFailureReason.SignatureInvalid => UILanguage.Translate("MAGAZA MAKBUZU GECERSIZ", "STORE RECEIPT INVALID", "RECIBO DE TIENDA INVALIDO"),
                PurchaseFailureReason.UserCancelled => UILanguage.Translate("SATIN ALMA IPTAL EDILDI", "PURCHASE CANCELLED", "COMPRA CANCELADA"),
                PurchaseFailureReason.PaymentDeclined => UILanguage.Translate("ODEME REDDEDILDI", "PAYMENT DECLINED", "PAGO RECHAZADO"),
                PurchaseFailureReason.DuplicateTransaction => UILanguage.Translate("SATIN ALMA ZATEN ISLENDI", "PURCHASE ALREADY PROCESSED", "LA COMPRA YA FUE PROCESADA"),
                PurchaseFailureReason.ValidationFailure => UILanguage.Translate("SATIN ALMA DOGRULAMASI BASARISIZ", "PURCHASE VALIDATION FAILED", "LA VALIDACION DE LA COMPRA FALLO"),
                PurchaseFailureReason.StoreNotConnected => $"{storeName} {UILanguage.Translate("BAGLANTISI KESILDI", "CONNECTION LOST", "CONEXION PERDIDA")}",
                PurchaseFailureReason.PurchaseMissing => UILanguage.Translate("MAGAZA SIPARISI DONDURMEDI", "STORE DID NOT RETURN THE ORDER", "LA TIENDA NO DEVOLVIO EL PEDIDO"),
                _ => string.IsNullOrWhiteSpace(details) ? $"{storeName} {UILanguage.Translate("SATIN ALMA BASARISIZ", "PURCHASE FAILED", "COMPRA FALLIDA")}" : details.ToUpperInvariant()
            };
        }

        private string FormatRestoreMessage(bool success, string message)
        {
            if (success)
            {
                return string.IsNullOrWhiteSpace(message)
                    ? $"{GetStoreDisplayName()} {UILanguage.Translate("GERI YUKLEME KONTROLU TAMAM", "RESTORE CHECK COMPLETE", "COMPROBACION DE RESTAURACION COMPLETA")}"
                    : message.ToUpperInvariant();
            }

            return string.IsNullOrWhiteSpace(message)
                ? $"{GetStoreDisplayName()} {UILanguage.Translate("GERI YUKLEME BASARISIZ", "RESTORE FAILED", "RESTAURACION FALLIDA")}"
                : message.ToUpperInvariant();
        }

        private bool ApplyOfferRewards(CoinPackOffer offer, out string message)
        {
            if (economyManager == null)
            {
                message = $"{GetStoreDisplayName()} {UILanguage.Translate("HAZIR DEGIL", "NOT READY", "NO LISTO")}";
                return false;
            }

            switch (offer.kind)
            {
                case StoreOfferKind.NoAds:
                    SetOfferOwned(offer.id, true);
                    message = UILanguage.Translate("REKLAMLAR KALDIRILDI", "ADS REMOVED", "ANUNCIOS ELIMINADOS");
                    return true;

                case StoreOfferKind.WelcomePack:
                case StoreOfferKind.ExclusiveBundle:
                    if (offer.coinAmount > 0)
                    {
                        economyManager.GrantEmber(offer.coinAmount, source: "iap_bundle");
                    }

                    if (!string.IsNullOrWhiteSpace(offer.ballSkinId))
                    {
                        economyManager.GrantSkinOwnership(offer.ballSkinId, true);
                    }

                    if (!string.IsNullOrWhiteSpace(offer.towerSkinId))
                    {
                        economyManager.GrantTowerSkinOwnership(offer.towerSkinId, true);
                    }

                    if (offer.productType != ProductType.Consumable)
                    {
                        SetOfferOwned(offer.id, true);
                    }

                    message = offer.coinAmount > 0
                        ? $"{UILanguage.Translate("PAKET ACILDI", "BUNDLE UNLOCKED", "PAQUETE DESBLOQUEADO")}  +{offer.coinAmount} {UILanguage.Translate("COIN", "COIN", "MONEDA")}"
                        : UILanguage.Translate("PAKET ACILDI", "BUNDLE UNLOCKED", "PAQUETE DESBLOQUEADO");
                    return true;

                default:
                    if (offer.coinAmount > 0)
                    {
                        economyManager.GrantEmber(offer.coinAmount, source: "iap_coinpack");
                    }

                    if (offer.productType != ProductType.Consumable)
                    {
                        SetOfferOwned(offer.id, true);
                    }

                    message = offer.coinAmount > 0
                        ? $"+{offer.coinAmount} {UILanguage.Translate("COIN", "COIN", "MONEDA")}"
                        : UILanguage.Translate("SATIN ALMA TAMAMLANDI", "PURCHASE COMPLETE", "COMPRA COMPLETA");
                    return true;
            }
        }

        private void LoadOwnedOffers()
        {
            ownedOfferIds.Clear();
            string rawValue = PlayerPrefs.GetString(PurchasedOfferIdsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return;
            }

            string[] tokens = rawValue.Split('|');
            for (int index = 0; index < tokens.Length; index++)
            {
                if (!string.IsNullOrWhiteSpace(tokens[index]))
                {
                    ownedOfferIds.Add(tokens[index]);
                }
            }
        }

        private void SaveOwnedOffers()
        {
            PlayerPrefs.SetString(PurchasedOfferIdsKey, string.Join("|", ownedOfferIds));
            PlayerPrefs.Save();
        }

        private bool IsOfferOwned(string offerId)
        {
            return !string.IsNullOrWhiteSpace(offerId) && ownedOfferIds.Contains(offerId);
        }

        private void ApplyOwnedFlags()
        {
            for (int index = 0; index < offers.Count; index++)
            {
                CoinPackOffer offer = offers[index];
                offer.owned = offer.productType != ProductType.Consumable && IsOfferOwned(offer.id);
                offers[index] = offer;
            }
        }

        private void SetOfferOwned(string offerId, bool owned)
        {
            if (string.IsNullOrWhiteSpace(offerId))
            {
                return;
            }

            bool changed = owned ? ownedOfferIds.Add(offerId) : ownedOfferIds.Remove(offerId);
            if (!changed)
            {
                SyncOwnedOffer(offerId);
                return;
            }

            SaveOwnedOffers();
            SyncOwnedOffer(offerId);
            OffersChanged?.Invoke();
            StateChanged?.Invoke();
        }

        private void SyncOwnedOffer(string offerId)
        {
            int offerIndex = GetOfferIndex(offerId);
            if (offerIndex < 0)
            {
                return;
            }

            CoinPackOffer offer = offers[offerIndex];
            bool owned = offer.productType != ProductType.Consumable && IsOfferOwned(offer.id);
            if (offer.owned == owned)
            {
                return;
            }

            offer.owned = owned;
            offers[offerIndex] = offer;
        }

        private int GetOfferIndex(string offerId)
        {
            for (int index = 0; index < offers.Count; index++)
            {
                if (offers[index].id == offerId)
                {
                    return index;
                }
            }

            return -1;
        }

        private static string GetStoreDisplayName()
        {
#if UNITY_ANDROID
            return "GOOGLE PLAY";
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            return "APP STORE";
#else
            return UILanguage.Translate("MAGAZA", "STORE", "TIENDA");
#endif
        }

        private static string ResolveUsdPriceLabel(Product product)
        {
            if (product?.metadata == null)
            {
                return null;
            }

            if (string.Equals(product.metadata.isoCurrencyCode, "USD", StringComparison.OrdinalIgnoreCase))
            {
                return FormatUsdPrice(product.metadata.localizedPrice);
            }

            string localized = product.metadata.localizedPriceString;
            if (!string.IsNullOrWhiteSpace(localized))
            {
                return localized;
            }

            if (product.metadata.localizedPrice > 0m)
            {
                string symbol = product.metadata.isoCurrencyCode;
                return string.IsNullOrWhiteSpace(symbol)
                    ? product.metadata.localizedPrice.ToString("0.00", CultureInfo.InvariantCulture)
                    : $"{product.metadata.localizedPrice.ToString("0.00", CultureInfo.InvariantCulture)} {symbol}";
            }

            return null;
        }

        private static string FormatUsdPrice(decimal amount)
        {
            return "$" + amount.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private void BuildCatalog()
        {
            if (offers.Count > 0)
            {
                return;
            }

            // Reprice 2026-05: aligned with casual mobile standard ($0.99-$99 ladder).
            // Welcome dropped to micro-tier with bumped coin to function as a true hook.
            offers.Add(new CoinPackOffer(
                "welcome_pack",
                "towermaze.bundle.welcome",
                "WELCOME PACK",
                5000,
                FormatUsdPrice(2.99m),
                "LIMITED",
                "HAZARD NEON  |  OBSIDIAN GATE",
                false,
                StoreOfferKind.WelcomePack,
                ProductType.NonConsumable,
                "hazard_neon",
                "obsidian_gate"));
            // No-Ads slashed from $19.99 to industry-standard $2.99 — conversion
            // expected to climb from <1% to 3-5% range, lifetime revenue up.
            offers.Add(new CoinPackOffer(
                "no_ads_pack",
                "towermaze.bundle.noads",
                "NO ADS",
                0,
                FormatUsdPrice(2.99m),
                "FOREVER",
                "REMOVE POPUP ADS FOREVER",
                false,
                StoreOfferKind.NoAds,
                ProductType.NonConsumable));
            offers.Add(new CoinPackOffer(
                "bundle_neon_rush",
                "towermaze.bundle.neonrush",
                "NEON RUSH BUNDLE",
                5000,
                FormatUsdPrice(4.99m),
                "BUNDLE",
                "HAZARD NEON  |  OBSIDIAN GATE",
                false,
                StoreOfferKind.ExclusiveBundle,
                ProductType.NonConsumable,
                "hazard_neon",
                "obsidian_gate"));
            offers.Add(new CoinPackOffer(
                "bundle_frost_reign",
                "towermaze.bundle.frostreign",
                "FROST REIGN BUNDLE",
                12000,
                FormatUsdPrice(9.99m),
                "BUNDLE",
                "VOID ICE  |  FROST KEEP",
                false,
                StoreOfferKind.ExclusiveBundle,
                ProductType.NonConsumable,
                "void_ice",
                "frost_keep"));
            // ─── PREMIUM SKIN IAP (gerçek para, coin yok) ────────────────
            offers.Add(new CoinPackOffer(
                "skin_solar_crown",
                "towermaze.skin.solar_crown",
                "SOLAR CROWN",
                0,
                FormatUsdPrice(4.99m),
                "PREMIUM",
                "EXCLUSIVE BALL SKIN",
                false,
                StoreOfferKind.ExclusiveBundle,
                ProductType.NonConsumable,
                "solar_crown",
                ""));
            offers.Add(new CoinPackOffer(
                "skin_dark_sovereign",
                "towermaze.skin.dark_sovereign",
                "DARK SOVEREIGN",
                0,
                FormatUsdPrice(4.99m),
                "PREMIUM",
                "EXCLUSIVE BALL SKIN",
                false,
                StoreOfferKind.ExclusiveBundle,
                ProductType.NonConsumable,
                "dark_sovereign",
                ""));
            offers.Add(new CoinPackOffer(
                "skin_silver_mirror",
                "towermaze.skin.silver",
                "SILVER MIRROR",
                0,
                FormatUsdPrice(9.99m),
                "PREMIUM",
                "EXCLUSIVE BALL SKIN",
                false,
                StoreOfferKind.ExclusiveBundle,
                ProductType.NonConsumable,
                "silver_mirror",
                ""));
            offers.Add(new CoinPackOffer(
                "skin_golden_glory",
                "towermaze.skin.gold",
                "GOLDEN GLORY",
                0,
                FormatUsdPrice(9.99m),
                "PREMIUM",
                "EXCLUSIVE BALL SKIN",
                false,
                StoreOfferKind.ExclusiveBundle,
                ProductType.NonConsumable,
                "golden_glory",
                ""));
            offers.Add(new CoinPackOffer(
                "skin_gilded_sanctum",
                "towermaze.skin.gilded_sanctum",
                "GILDED SANCTUM",
                0,
                FormatUsdPrice(4.99m),
                "PREMIUM",
                "EXCLUSIVE TOWER SKIN",
                false,
                StoreOfferKind.ExclusiveBundle,
                ProductType.NonConsumable,
                "",
                "gilded_sanctum"));
            offers.Add(new CoinPackOffer(
                "skin_shadow_citadel",
                "towermaze.skin.shadow_citadel",
                "SHADOW CITADEL",
                0,
                FormatUsdPrice(4.99m),
                "PREMIUM",
                "EXCLUSIVE TOWER SKIN",
                false,
                StoreOfferKind.ExclusiveBundle,
                ProductType.NonConsumable,
                "",
                "shadow_citadel"));
            offers.Add(new CoinPackOffer(
                "skin_silver_spire",
                "towermaze.skin.silver_tower",
                "SILVER SPIRE",
                0,
                FormatUsdPrice(9.99m),
                "PREMIUM",
                "EXCLUSIVE TOWER SKIN",
                false,
                StoreOfferKind.ExclusiveBundle,
                ProductType.NonConsumable,
                "",
                "silver_spire"));
            offers.Add(new CoinPackOffer(
                "skin_golden_bastion",
                "towermaze.skin.gold_tower",
                "GOLDEN BASTION",
                0,
                FormatUsdPrice(9.99m),
                "PREMIUM",
                "EXCLUSIVE TOWER SKIN",
                false,
                StoreOfferKind.ExclusiveBundle,
                ProductType.NonConsumable,
                "",
                "golden_bastion"));
            // --- NEON PRO BUNDLE ---
            // Neon Pro now at $14.99 with bonus coins so the bundle reads as
            // "two skins + free coins" rather than the old whale-only $99.99.
            offers.Add(new CoinPackOffer(
                "bundle_neon_pro",
                "towermaze.bundle.neon_pro",
                "NEON PRO BUNDLE",
                8000,
                FormatUsdPrice(14.99m),
                "BUNDLE",
                "NEON BALL  |  NEON TOWER",
                false,
                StoreOfferKind.ExclusiveBundle,
                ProductType.NonConsumable,
                "neon_ball",
                "neon_tower"));
            // Champion pack stays in whale tier (kept Consumable so it stacks)
            // with the best coin/$ rate (~2,500 coins per $) to anchor the top.
            offers.Add(new CoinPackOffer(
                "champion_pack",
                "towermaze.coinpack.champion",
                "CHAMPION PACK",
                250000,
                FormatUsdPrice(99.99m),
                "BEST VALUE",
                "+13 SAVE  |  +13 BOOST  |  +13 SKIP",
                true,
                StoreOfferKind.CoinPack,
                ProductType.Consumable));
            // Coin ladder: micro-hook ($0.99) up to mega ($49.99). Rate climbs
            // monotonically with tier so each step up feels like a better deal.
            offers.Add(new CoinPackOffer("coin_pack_1000", "towermaze.coinpack.1000", "STARTER PACK", 800, FormatUsdPrice(0.99m)));
            offers.Add(new CoinPackOffer("coin_pack_5000", "towermaze.coinpack.5000", "SUPPORT PACK", 3500, FormatUsdPrice(2.99m)));
            offers.Add(new CoinPackOffer("coin_pack_10000", "towermaze.coinpack.10000", "BOOST PACK", 6500, FormatUsdPrice(4.99m)));
            offers.Add(new CoinPackOffer("coin_pack_25000", "towermaze.coinpack.25000", "MEGA PACK", 15000, FormatUsdPrice(9.99m)));
            offers.Add(new CoinPackOffer("coin_pack_50000", "towermaze.coinpack.50000", "ULTRA PACK", 35000, FormatUsdPrice(19.99m)));
            offers.Add(new CoinPackOffer("coin_pack_100000", "towermaze.coinpack.100000", "LEGEND PACK", 100000, FormatUsdPrice(49.99m)));
            OffersChanged?.Invoke();
        }
    }

    public sealed class InAppReviewManager : MonoBehaviour
    {
        public void Initialize() { }

        /// <summary>
        /// iOS: native RequestStoreReview.
        /// Android: Google Play In-App Review API via AndroidJavaObject (native dialog, oyundan çıkılmaz).
        /// Editor: sadece log.
        /// Google kotayı aştığında diyalogu kendisi bastırır — hata fırlatmaz.
        /// </summary>
        public void RequestReview()
        {
#if UNITY_IOS
            UnityEngine.iOS.Device.RequestStoreReview();
#elif UNITY_ANDROID
            StartCoroutine(RequestReviewCoroutine());
#else
            UnityEngine.Debug.Log("[InAppReviewManager] RequestReview called (editor — no-op)");
#endif
        }

#if UNITY_ANDROID
        private System.Collections.IEnumerator RequestReviewCoroutine()
        {
            AndroidJavaObject reviewManager = null;
            AndroidJavaObject reviewInfo = null;
            AndroidJavaObject reviewContext = null;
            AndroidJavaObject reviewActivity = null;
            AndroidJavaObject launchTask = null;
            try
            {
                reviewActivity = TryGetUnityJavaObject("currentActivity");
                reviewContext = reviewActivity ?? TryGetUnityJavaObject("currentContext");
                if (reviewContext == null)
                {
                    UnityEngine.Debug.LogWarning("[InAppReviewManager] Review skipped: Unity activity/context unavailable.");
                    yield break;
                }
                using var factory = new AndroidJavaClass("com.google.android.play.core.review.ReviewManagerFactory");
                reviewManager = factory.CallStatic<AndroidJavaObject>("create", reviewContext);

                var requestTask = reviewManager.Call<AndroidJavaObject>("requestReviewFlow");
                yield return WaitForTask(requestTask);

                if (!requestTask.Call<bool>("isSuccessful"))
                {
                    UnityEngine.Debug.LogWarning("[InAppReviewManager] requestReviewFlow failed");
                    yield break;
                }

                reviewInfo = requestTask.Call<AndroidJavaObject>("getResult");

                if (reviewActivity == null)
                {
                    UnityEngine.Debug.LogWarning("[InAppReviewManager] Review launch skipped: currentActivity unavailable.");
                    yield break;
                }

                try
                {
                    launchTask = reviewManager.Call<AndroidJavaObject>("launchReviewFlow", reviewActivity, reviewInfo);
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogWarning($"[InAppReviewManager] launchReviewFlow failed: {exception.Message}");
                    yield break;
                }

                yield return WaitForTask(launchTask);
                // Başarı durumunda log yok — Google API dialog gösterip göstermemeyi kendisi karar verir.
            }
            finally
            {
                launchTask?.Dispose();
                reviewInfo?.Dispose();
                reviewManager?.Dispose();
                reviewActivity?.Dispose();
                if (!ReferenceEquals(reviewContext, reviewActivity))
                {
                    reviewContext?.Dispose();
                }
            }
        }

        private static System.Collections.IEnumerator WaitForTask(AndroidJavaObject task)
        {
            if (task == null)
            {
                yield break;
            }

            while (!task.Call<bool>("isComplete"))
                yield return null;
        }

        private static AndroidJavaObject TryGetUnityJavaObject(string fieldName)
        {
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                return unityPlayer.GetStatic<AndroidJavaObject>(fieldName);
            }
            catch
            {
                return null;
            }
        }
#endif
    }
}
