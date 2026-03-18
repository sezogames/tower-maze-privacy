using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
#if GOOGLE_MOBILE_ADS
using GoogleMobileAds.Api;
#endif

namespace TowerMaze
{
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
    }

    public sealed class RewardedAdManager : MonoBehaviour
    {
        [SerializeField] private bool usingSimulatedProvider = true;
        [SerializeField] private bool adLoading;
        [SerializeField] private bool adReady;
        [SerializeField] private bool adShowing;

        private GameConfig config;
        private Action<bool> completionCallback;

#if GOOGLE_MOBILE_ADS
        private RewardedAd rewardedAd;
        private bool rewardEarned;
#endif

        public bool IsSimulatedProvider => usingSimulatedProvider;
        public bool CanShowRewarded => usingSimulatedProvider || adReady;

        public void Initialize(GameConfig gameConfig)
        {
            config = gameConfig;
            usingSimulatedProvider = config == null || config.useSimulatedRewardedAds;

#if GOOGLE_MOBILE_ADS
            if (!usingSimulatedProvider)
            {
                MobileAds.Initialize(_ => LoadRewardedAd());
                return;
            }
#endif

            adReady = true;
        }

        public void ShowRewarded(RewardedPlacement placement, Action<bool> onCompleted)
        {
            if (adShowing)
            {
                onCompleted?.Invoke(false);
                return;
            }

            completionCallback = onCompleted;

            if (usingSimulatedProvider)
            {
                StartCoroutine(SimulateRewardedFlow());
                return;
            }

#if GOOGLE_MOBILE_ADS
            if (rewardedAd == null || !rewardedAd.CanShowAd())
            {
                completionCallback?.Invoke(false);
                completionCallback = null;
                LoadRewardedAd();
                return;
            }

            adShowing = true;
            adReady = false;
            rewardEarned = false;
            rewardedAd.Show(_ => rewardEarned = true);
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

            adLoading = true;
            adReady = false;
            string adUnitId = ResolveRewardedAdUnitId();
            AdRequest request = new();
            RewardedAd.Load(adUnitId, request, (RewardedAd ad, LoadAdError error) =>
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
        }

        private void RegisterRewardedCallbacks(RewardedAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                adShowing = false;
                completionCallback?.Invoke(rewardEarned);
                completionCallback = null;
                LoadRewardedAd();
            };

            ad.OnAdFullScreenContentFailed += _ =>
            {
                adShowing = false;
                completionCallback?.Invoke(false);
                completionCallback = null;
                LoadRewardedAd();
            };
        }

        private string ResolveRewardedAdUnitId()
        {
#if UNITY_ANDROID
            return string.IsNullOrWhiteSpace(config.androidRewardedAdUnitId)
                ? "ca-app-pub-3940256099942544/5224354917"
                : config.androidRewardedAdUnitId;
#elif UNITY_IOS
            return string.IsNullOrWhiteSpace(config.iosRewardedAdUnitId)
                ? "ca-app-pub-3940256099942544/1712485313"
                : config.iosRewardedAdUnitId;
#else
            return "unused-editor-rewarded";
#endif
        }
#endif
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
                return new CoinPackPurchaseResult(CoinPackPurchaseStatus.Failed, default, "PACK NOT FOUND");
            }

            if (offer.productType != ProductType.Consumable && offer.owned)
            {
                return new CoinPackPurchaseResult(CoinPackPurchaseStatus.Failed, offer, "ALREADY OWNED");
            }

            if (economyManager == null)
            {
                return new CoinPackPurchaseResult(CoinPackPurchaseStatus.Failed, offer, $"{GetStoreDisplayName()} NOT READY");
            }

            if (ShouldSimulatePurchase())
            {
                bool granted = ApplyOfferRewards(offer, out string rewardMessage);
                CoinPackOffer updatedOffer = GetOffer(packId);
                return new CoinPackPurchaseResult(
                    granted ? CoinPackPurchaseStatus.Succeeded : CoinPackPurchaseStatus.Failed,
                    updatedOffer,
                    granted ? rewardMessage : "TEST PURCHASE FAILED");
            }

            if (purchaseInProgress)
            {
                return new CoinPackPurchaseResult(CoinPackPurchaseStatus.Failed, offer, "PURCHASE IN PROGRESS");
            }

            if (storeController == null)
            {
                InitializeStoreController();
                return new CoinPackPurchaseResult(CoinPackPurchaseStatus.Pending, offer, "STORE CONNECTING");
            }

            if (!storeConnected)
            {
                InitializeStoreController();
                return new CoinPackPurchaseResult(CoinPackPurchaseStatus.Pending, offer, "STORE CONNECTING");
            }

            if (!productsFetched)
            {
                FetchProducts();
                return new CoinPackPurchaseResult(CoinPackPurchaseStatus.Pending, offer, $"LOADING {GetStoreDisplayName()} PRICES");
            }

            Product product = storeController.GetProductById(offer.productId);
            if (product == null)
            {
                FetchProducts();
                return new CoinPackPurchaseResult(CoinPackPurchaseStatus.Unavailable, offer, $"{GetStoreDisplayName()} PRODUCT NOT READY");
            }

            purchaseInProgress = true;
            pendingPackId = offer.id;
            pendingProductId = offer.productId;
            storeController.PurchaseProduct(product);
            return new CoinPackPurchaseResult(CoinPackPurchaseStatus.Pending, offer, $"OPENING {GetStoreDisplayName()}");
        }

        public void RestoreTransactions()
        {
            if (ShouldSimulatePurchase())
            {
                RestoreFinished?.Invoke(false, "TEST STORE HAS NOTHING TO RESTORE");
                return;
            }

            if (purchaseInProgress)
            {
                RestoreFinished?.Invoke(false, "WAIT FOR CURRENT PURCHASE");
                return;
            }

            if (storeController == null || !storeConnected)
            {
                restoreRequested = true;
                InitializeStoreController();
                RestoreFinished?.Invoke(false, $"CONNECTING TO {GetStoreDisplayName()}");
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
                if (offers[i].id == packId)
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
                if (product?.metadata == null || string.IsNullOrWhiteSpace(product.metadata.localizedPriceString))
                {
                    continue;
                }

                int offerIndex = GetOfferIndexByProductId(product.definition.id);
                if (offerIndex < 0)
                {
                    continue;
                }

                CoinPackOffer offer = offers[offerIndex];
                if (offer.priceLabel == product.metadata.localizedPriceString)
                {
                    continue;
                }

                offer.priceLabel = product.metadata.localizedPriceString;
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
                PurchaseFinished?.Invoke(new CoinPackPurchaseResult(CoinPackPurchaseStatus.Failed, default, "UNKNOWN STORE PRODUCT"));
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
                    successMessage = "PURCHASE RESTORED";
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
                    successMessage = "PURCHASE RESTORED";
                }
            }

            storeController?.ConfirmPurchase(order);
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
            PurchaseFinished?.Invoke(new CoinPackPurchaseResult(CoinPackPurchaseStatus.Unavailable, offer, $"{GetStoreDisplayName()} NEEDS APPROVAL"));
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
                RestoreFinished?.Invoke(false, $"{GetStoreDisplayName()} RESTORE FAILED");
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
                PurchaseFailureReason.PurchasingUnavailable => $"{storeName} BILLING UNAVAILABLE",
                PurchaseFailureReason.ExistingPurchasePending => "ANOTHER PURCHASE IS ALREADY OPEN",
                PurchaseFailureReason.ProductUnavailable => $"ITEM NOT AVAILABLE IN {storeName}",
                PurchaseFailureReason.SignatureInvalid => "STORE RECEIPT INVALID",
                PurchaseFailureReason.UserCancelled => "PURCHASE CANCELLED",
                PurchaseFailureReason.PaymentDeclined => "PAYMENT DECLINED",
                PurchaseFailureReason.DuplicateTransaction => "PURCHASE ALREADY PROCESSED",
                PurchaseFailureReason.ValidationFailure => "PURCHASE VALIDATION FAILED",
                PurchaseFailureReason.StoreNotConnected => $"{storeName} CONNECTION LOST",
                PurchaseFailureReason.PurchaseMissing => "STORE DID NOT RETURN THE ORDER",
                _ => string.IsNullOrWhiteSpace(details) ? $"{storeName} PURCHASE FAILED" : details.ToUpperInvariant()
            };
        }

        private string FormatRestoreMessage(bool success, string message)
        {
            if (success)
            {
                return string.IsNullOrWhiteSpace(message)
                    ? $"{GetStoreDisplayName()} RESTORE CHECK COMPLETE"
                    : message.ToUpperInvariant();
            }

            return string.IsNullOrWhiteSpace(message)
                ? $"{GetStoreDisplayName()} RESTORE FAILED"
                : message.ToUpperInvariant();
        }

        private bool ApplyOfferRewards(CoinPackOffer offer, out string message)
        {
            if (economyManager == null)
            {
                message = $"{GetStoreDisplayName()} NOT READY";
                return false;
            }

            switch (offer.kind)
            {
                case StoreOfferKind.NoAds:
                    SetOfferOwned(offer.id, true);
                    message = "ADS REMOVED";
                    return true;

                case StoreOfferKind.WelcomePack:
                case StoreOfferKind.ExclusiveBundle:
                    if (offer.coinAmount > 0)
                    {
                        economyManager.GrantEmber(offer.coinAmount);
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
                        ? $"BUNDLE UNLOCKED  +{offer.coinAmount} COIN"
                        : "BUNDLE UNLOCKED";
                    return true;

                default:
                    if (offer.coinAmount > 0)
                    {
                        economyManager.GrantEmber(offer.coinAmount);
                    }

                    if (offer.productType != ProductType.Consumable)
                    {
                        SetOfferOwned(offer.id, true);
                    }

                    message = offer.coinAmount > 0 ? $"+{offer.coinAmount} COIN" : "PURCHASE COMPLETE";
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
            return "STORE";
#endif
        }

        private void BuildCatalog()
        {
            if (offers.Count > 0)
            {
                return;
            }

            offers.Add(new CoinPackOffer(
                "welcome_pack",
                "towermaze.bundle.welcome",
                "WELCOME PACK",
                2000,
                "TRY 149.99",
                "LIMITED",
                "HAZARD NEON  |  OBSIDIAN GATE",
                false,
                StoreOfferKind.WelcomePack,
                ProductType.NonConsumable,
                "hazard_neon",
                "obsidian_gate"));
            offers.Add(new CoinPackOffer(
                "no_ads_pack",
                "towermaze.bundle.noads",
                "NO ADS",
                0,
                "TRY 199.99",
                "FOREVER",
                "REMOVE POPUP ADS FOREVER",
                false,
                StoreOfferKind.NoAds,
                ProductType.NonConsumable));
            offers.Add(new CoinPackOffer(
                "bundle_neon_rush",
                "towermaze.bundle.neonrush",
                "NEON RUSH BUNDLE",
                2500,
                "TRY 299.99",
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
                5000,
                "TRY 499.99",
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
                "TRY 2.500,00",
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
                "TRY 2.500,00",
                "PREMIUM",
                "EXCLUSIVE BALL SKIN",
                false,
                StoreOfferKind.ExclusiveBundle,
                ProductType.NonConsumable,
                "dark_sovereign",
                ""));
            offers.Add(new CoinPackOffer(
                "skin_gilded_sanctum",
                "towermaze.skin.gilded_sanctum",
                "GILDED SANCTUM",
                0,
                "TRY 2.500,00",
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
                "TRY 2.500,00",
                "PREMIUM",
                "EXCLUSIVE TOWER SKIN",
                false,
                StoreOfferKind.ExclusiveBundle,
                ProductType.NonConsumable,
                "",
                "shadow_citadel"));
            offers.Add(new CoinPackOffer(
                "champion_pack",
                "towermaze.coinpack.champion",
                "CHAMPION PACK",
                65000,
                "TRY 4,999.99",
                "BEST VALUE",
                "+13 SAVE  |  +13 BOOST  |  +13 SKIP",
                true,
                StoreOfferKind.CoinPack,
                ProductType.Consumable));
            offers.Add(new CoinPackOffer("coin_pack_1000", "towermaze.coinpack.1000", "STARTER PACK", 1000, "TRY 99.99"));
            offers.Add(new CoinPackOffer("coin_pack_5000", "towermaze.coinpack.5000", "SUPPORT PACK", 5000, "TRY 399.99"));
            offers.Add(new CoinPackOffer("coin_pack_10000", "towermaze.coinpack.10000", "BOOST PACK", 10000, "TRY 799.99"));
            offers.Add(new CoinPackOffer("coin_pack_25000", "towermaze.coinpack.25000", "MEGA PACK", 25000, "TRY 1,499.99"));
            offers.Add(new CoinPackOffer("coin_pack_50000", "towermaze.coinpack.50000", "ULTRA PACK", 50000, "TRY 2,999.99"));
            offers.Add(new CoinPackOffer("coin_pack_100000", "towermaze.coinpack.100000", "LEGEND PACK", 100000, "TRY 4,999.99"));
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
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var factory = new AndroidJavaClass("com.google.android.play.core.review.ReviewManagerFactory");
                reviewManager = factory.CallStatic<AndroidJavaObject>("create", activity);

                var requestTask = reviewManager.Call<AndroidJavaObject>("requestReviewFlow");
                yield return WaitForTask(requestTask);

                if (!requestTask.Call<bool>("isSuccessful"))
                {
                    UnityEngine.Debug.LogWarning("[InAppReviewManager] requestReviewFlow failed");
                    yield break;
                }

                reviewInfo = requestTask.Call<AndroidJavaObject>("getResult");

                using var unityPlayer2 = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity2 = unityPlayer2.GetStatic<AndroidJavaObject>("currentActivity");
                var launchTask = reviewManager.Call<AndroidJavaObject>("launchReviewFlow", activity2, reviewInfo);
                yield return WaitForTask(launchTask);
                // Başarı durumunda log yok — Google API dialog gösterip göstermemeyi kendisi karar verir.
            }
            finally
            {
                reviewInfo?.Dispose();
                reviewManager?.Dispose();
            }
        }

        private static System.Collections.IEnumerator WaitForTask(AndroidJavaObject task)
        {
            while (!task.Call<bool>("isComplete"))
                yield return null;
        }
#endif
    }
}
