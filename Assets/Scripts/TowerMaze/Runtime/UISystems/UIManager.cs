// Assets/Scripts/TowerMaze/Runtime/UISystems/UIManager.cs
// UIManager — canvas setup, screen routing, Initialize()
// Visual redesign happens in individual screen files (Tasks 3–9).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace TowerMaze
{
    public class UIManager : MonoBehaviour
    {
        private ThemeDefinition theme;
        private GameConfig gameConfig;
        private ScoreManager scoreManager;
        private Canvas canvas;
        private Image heatOverlay;
        private Image staticMenuBackground;
        private UIHudController hudController;
        private StartScreenController startScreenController;
        private FailScreenController failScreenController;
        private CountdownController countdownController;
        private RushWarningController rushWarningController;
        private ControlFlipController controlFlipController;
        private ShopScreenController shopScreenController;
        private IAPUpsellController iapUpsellController;
        private RewardToastController rewardToastController;
        private PauseScreenController pauseScreenController;
        private Font runtimeFont;
        private EconomyManager economyManager;
        private RewardedAdManager rewardedAdManager;
        private CoinStoreManager coinStoreManager;
        private PlayerController playerController;
        private float cachedBestScore;
        private IReadOnlyList<LeaderboardEntry> cachedLeaderboardEntries = Array.Empty<LeaderboardEntry>();
        private IReadOnlyList<DailyMissionState> cachedDailyMissions = Array.Empty<DailyMissionState>();
        private DailyChestStatus cachedChestStatus;
        private DailyChallengeStatus cachedDailyChallengeStatus;
        private int cachedMissionRerollCost;
        private bool cachedSoundEnabled;
        private bool cachedVibrationEnabled;
        private Action buttonClickSound;
        private bool splashComplete;
        private Action pendingShowStart;
        private static readonly Dictionary<string, Sprite> themedSpriteCache = new();
        public bool IsShopOpen => shopScreenController != null && shopScreenController.gameObject.activeSelf;


        public void Initialize(
            bool splashActive,
            GameConfig config,
            ThemeDefinition definition,
            EconomyManager economy,
            RewardedAdManager rewardedAds,
            CoinStoreManager coinStore,
            PlayerController player,
            Action onPlay,
            Action onPlayDailyChallenge,
            Action onRetry,
            Action onContinue,
            Action onReturnToMenu,
            Action onClaimDoubleReward,
            Action onWatchLifeRefillAd,
            Action onBuyLifeRefillWithCoins,
            Action onToggleSound,
            Action onToggleVibration,
            Action onPause,
            Action onResume,
            Action onButtonClick = null,
            Sprite staticBackground = null,
            ScoreManager scoreMgr = null)
        {
            splashComplete = !splashActive;
            gameConfig = config;
            scoreManager = scoreMgr;
            theme = definition;
            economyManager = economy;
            rewardedAdManager = rewardedAds;
            coinStoreManager = coinStore;
            playerController = player;
            buttonClickSound = onButtonClick;
            runtimeFont = Resources.Load<Font>("TowerMaze/UITheme/Outfit-Bold")
                          ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (coinStoreManager != null)
            {
                coinStoreManager.OffersChanged += HandleCoinStoreOffersChanged;
                coinStoreManager.PurchaseFinished += HandleCoinStorePurchaseFinished;
                coinStoreManager.RestoreFinished += HandleCoinStoreRestoreFinished;
            }
            EnsureCanvas();

            if (staticBackground != null)
            {
                staticMenuBackground = CreateImage("StaticMenuBackground", canvas.transform, Color.white);
                staticMenuBackground.sprite = staticBackground;
                staticMenuBackground.type = Image.Type.Simple;
                staticMenuBackground.preserveAspect = false;
                staticMenuBackground.transform.SetAsFirstSibling(); // Put behind everything
                Stretch(staticMenuBackground.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                staticMenuBackground.gameObject.SetActive(false);
            }

            heatOverlay = CreateImage("HeatOverlay", canvas.transform, new Color(theme.nearLavaOverlay.r, theme.nearLavaOverlay.g, theme.nearLavaOverlay.b, 0f));
            Stretch(heatOverlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            heatOverlay.raycastTarget = false;

            hudController = CreatePanel<UIHudController>("HUD", canvas.transform);
            hudController.Initialize(runtimeFont, theme, onPause, buttonClickSound, gameConfig, scoreManager);

            startScreenController = CreatePanel<StartScreenController>("StartScreen", canvas.transform);
            startScreenController.Initialize(runtimeFont, theme, economyManager, onPlay, onPlayDailyChallenge, ShowShop, HandleChestClaim, onToggleSound, onToggleVibration, HandleMissionReroll, buttonClickSound);

            failScreenController = CreatePanel<FailScreenController>("FailScreen", canvas.transform);
            failScreenController.Initialize(runtimeFont, theme, economyManager, onRetry, onContinue, onReturnToMenu, onClaimDoubleReward, onWatchLifeRefillAd, onBuyLifeRefillWithCoins, buttonClickSound, gameConfig?.failToRetryDelay ?? 0.3f);

            countdownController = CreatePanel<CountdownController>("Countdown", canvas.transform);
            countdownController.Initialize(runtimeFont, theme);

            rushWarningController = CreatePanel<RushWarningController>("RushWarning", canvas.transform);
            rushWarningController.Initialize(runtimeFont, theme);

            controlFlipController = CreatePanel<ControlFlipController>("ControlFlip", canvas.transform);
            controlFlipController.Initialize(runtimeFont, theme);

            shopScreenController = CreatePanel<ShopScreenController>("ShopScreen", canvas.transform);
            shopScreenController.Initialize(runtimeFont, theme, HideShop, HandleShopCoinBoost, HandleCoinStoreRestore, HandleShopAction, buttonClickSound);
            shopScreenController.gameObject.SetActive(false);

            rewardToastController = CreatePanel<RewardToastController>("RewardToast", canvas.transform);
            rewardToastController.Initialize(runtimeFont, theme);
            rewardToastController.gameObject.SetActive(false);

            hudController.SetDependencies(economyManager, rewardToastController);

            iapUpsellController = CreatePanel<IAPUpsellController>("IAPUpsell", canvas.transform);
            iapUpsellController.Initialize(runtimeFont, theme, id => TriggerUpsellPurchase(id), buttonClickSound);
            iapUpsellController.gameObject.SetActive(false);

            pauseScreenController = CreatePanel<PauseScreenController>("PauseScreen", canvas.transform);
            pauseScreenController.Initialize(runtimeFont, theme, onResume, onReturnToMenu, buttonClickSound);
            pauseScreenController.gameObject.SetActive(false);

            if (splashActive)
            {
                startScreenController.gameObject.SetActive(false);
                failScreenController.gameObject.SetActive(false);
                hudController.gameObject.SetActive(false);
                countdownController.gameObject.SetActive(false);
                rushWarningController.gameObject.SetActive(false);
                controlFlipController.gameObject.SetActive(false);
            }
        }


        public void ShowStart(float bestScore, int emberBalance, IReadOnlyList<LeaderboardEntry> leaderboardEntries, IReadOnlyList<DailyMissionState> dailyMissions, DailyChestStatus chestStatus, DailyChallengeStatus challengeStatus, int missionRerollCost, bool soundEnabled, bool vibrationEnabled)
        {
            if (!splashComplete)
            {
                // Intentional overwrite: only the most-recent state matters for the start screen.
                // If ShowStart is called multiple times before splash completes, the last call wins.
                pendingShowStart = () => ShowStart(bestScore, emberBalance, leaderboardEntries, dailyMissions, chestStatus, challengeStatus, missionRerollCost, soundEnabled, vibrationEnabled);
                return;
            }
            cachedBestScore = bestScore;
            cachedLeaderboardEntries = leaderboardEntries ?? Array.Empty<LeaderboardEntry>();
            cachedDailyMissions = dailyMissions ?? Array.Empty<DailyMissionState>();
            cachedChestStatus = chestStatus;
            cachedDailyChallengeStatus = challengeStatus;
            cachedMissionRerollCost = missionRerollCost;
            cachedSoundEnabled = soundEnabled;
            cachedVibrationEnabled = vibrationEnabled;
            startScreenController.gameObject.SetActive(true);
            failScreenController.gameObject.SetActive(false);
            hudController.gameObject.SetActive(false);
            countdownController.gameObject.SetActive(false);
            rushWarningController.gameObject.SetActive(false);
            controlFlipController.gameObject.SetActive(false);
            shopScreenController.gameObject.SetActive(false);
            pauseScreenController?.gameObject.SetActive(false);
            startScreenController.SetState(bestScore, emberBalance, cachedLeaderboardEntries, cachedDailyMissions, cachedChestStatus, cachedDailyChallengeStatus, cachedMissionRerollCost, soundEnabled, vibrationEnabled);
            SetHeat(0f);
            if (staticMenuBackground != null) staticMenuBackground.gameObject.SetActive(true);
        }

        public bool IsSplashComplete => splashComplete;

        internal void OnSplashComplete()
        {
            splashComplete = true;
            Action pending = pendingShowStart;
            pendingShowStart = null; // clear before invoke to prevent re-entrancy issues
            pending?.Invoke();
        }

        public void UpdateCachedLeaderboard(float bestScore, IReadOnlyList<LeaderboardEntry> leaderboardEntries)
        {
            cachedBestScore = bestScore;
            cachedLeaderboardEntries = leaderboardEntries ?? Array.Empty<LeaderboardEntry>();
            if (startScreenController != null && startScreenController.gameObject.activeSelf)
            {
                RefreshStartScreenState();
            }
        }

        public void ShowHud()
        {
            startScreenController.gameObject.SetActive(false);
            failScreenController.gameObject.SetActive(false);
            hudController.gameObject.SetActive(true);
            countdownController.gameObject.SetActive(false);
            rushWarningController.gameObject.SetActive(false);
            shopScreenController.gameObject.SetActive(false);
            pauseScreenController?.gameObject.SetActive(false);
            if (staticMenuBackground != null) staticMenuBackground.gameObject.SetActive(false);
        }

        public void ShowFail(float score, float bestScore, float runTime, IReadOnlyList<LeaderboardEntry> leaderboardEntries, int emberBalance, int rewardValue, int claimedReward, bool canContinue, bool canClaimDoubleReward, string bestDeltaText, string nextTargetText, string modeSummaryText, int remainingLives, bool canWatchLifeRefillAd, bool canBuyLifeRefill, int lifeRefillCoinCost, bool hasContinueOption, int continueCoinCost, bool showUpsell = false)
        {
            failScreenController.gameObject.SetActive(true);
            failScreenController.SetState(score, bestScore, runTime, leaderboardEntries, emberBalance, rewardValue, claimedReward, canContinue, canClaimDoubleReward, bestDeltaText, nextTargetText, modeSummaryText, remainingLives, canWatchLifeRefillAd, canBuyLifeRefill, lifeRefillCoinCost, hasContinueOption, continueCoinCost);
            hudController.gameObject.SetActive(false);
            countdownController.gameObject.SetActive(false);
            rushWarningController.gameObject.SetActive(false);
            controlFlipController.gameObject.SetActive(false);
            shopScreenController.gameObject.SetActive(false);
            pauseScreenController?.gameObject.SetActive(false);
            if (showUpsell) ShowFailIAPUpsell();
            if (staticMenuBackground != null) staticMenuBackground.gameObject.SetActive(false);
        }

        public void ShowCountdown(string label, bool isGo, float score, float bestScore, float runTime, int zoneIndex, float lavaGap, float gapDangerNormalized, bool isNewBest, bool showControlsHint)
        {
            startScreenController.gameObject.SetActive(false);
            failScreenController.gameObject.SetActive(false);
            hudController.gameObject.SetActive(true);
            hudController.SetValues(score, bestScore, runTime, zoneIndex, lavaGap, gapDangerNormalized, isNewBest, showControlsHint);
            countdownController.gameObject.SetActive(true);
            rushWarningController.gameObject.SetActive(false);
            controlFlipController.gameObject.SetActive(false);
            shopScreenController.gameObject.SetActive(false);
            pauseScreenController?.gameObject.SetActive(false);
            countdownController.SetValue(label, isGo);
            if (staticMenuBackground != null) staticMenuBackground.gameObject.SetActive(false);
        }

        public void UpdateHud(float score, float bestScore, float runTime, int zoneIndex, float lavaGap, float gapDangerNormalized, bool isNewBest, bool showControlsHint)
        {
            hudController.SetValues(score, bestScore, runTime, zoneIndex, lavaGap, gapDangerNormalized, isNewBest, showControlsHint);
        }

        public void SetRushState(bool visible, bool rushActive, float pulse, float remainingNormalized)
        {
            if (rushWarningController == null)
            {
                return;
            }

            rushWarningController.gameObject.SetActive(visible);
            if (visible)
            {
                rushWarningController.SetState(rushActive, pulse, remainingNormalized);
            }
        }

        public void SetControlFlipState(bool visible, bool controlsFlipped, float pulse, float remainingNormalized, float totalDurationSeconds)
        {
            if (controlFlipController == null)
            {
                return;
            }

            controlFlipController.gameObject.SetActive(visible);
            if (visible)
            {
                controlFlipController.SetState(controlsFlipped, pulse, remainingNormalized, totalDurationSeconds);
            }
        }

        public void SetHeat(float intensity)
        {
            if (heatOverlay == null)
            {
                return;
            }

            Color color = theme.nearLavaOverlay;
            color.a = Mathf.Lerp(0f, theme.nearLavaOverlay.a, intensity);
            heatOverlay.color = color;
        }

        public void ShowPause()
        {
            if (pauseScreenController == null) return;
            pauseScreenController.gameObject.SetActive(true);
        }

        public void HidePause()
        {
            if (pauseScreenController == null) return;
            pauseScreenController.gameObject.SetActive(false);
        }

        public void ShowNicknamePopup(Action<string> onConfirm)
        {
            var go = new GameObject("NicknamePopup");
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            Stretch(rt);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var popup = go.AddComponent<NicknamePopupController>();
            popup.Initialize(runtimeFont, theme, onConfirm);

        }
        public void QueueRewardToast(string title, string subtitle, Color accentColor)
        {
            rewardToastController?.Enqueue(title, subtitle, accentColor);
        }

        private void ShowShop()
        {
            if (economyManager == null || shopScreenController == null)
            {
                return;
            }

            shopScreenController.gameObject.SetActive(true);
            shopScreenController.SetState(economyManager.EmberBalance, economyManager.Skins, economyManager.TowerSkins, coinStoreManager != null ? coinStoreManager.Offers : Array.Empty<CoinPackOffer>(), economyManager);
        }

        private void HideShop()
        {
            if (shopScreenController != null)
            {
                shopScreenController.gameObject.SetActive(false);
            }
        }

        private void HandleChestClaim()
        {
            if (economyManager == null)
            {
                return;
            }

            DailyChestStatus chestStatus = economyManager.GetDailyChestStatus();
            if (!chestStatus.canClaim)
            {
                RefreshStartScreenState();
                return;
            }

            if (chestStatus.requiresRewardedAd)
            {
                rewardedAdManager?.ShowRewarded(RewardedPlacement.DailyBonusChest, success =>
                {
                    if (!success)
                    {
                        RefreshStartScreenState();
                        return;
                    }

                    int reward = economyManager.ClaimBonusDailyChest();
                    if (reward > 0)
                    {
                        QueueRewardToast("BONUS CHEST", $"+{reward} COIN", new Color(1f, 0.72f, 0.34f, 1f));
                    }
                    RefreshStartScreenState();
                });
                return;
            }

            int freeReward = economyManager.ClaimFreeDailyChest();
            if (freeReward > 0)
            {
                QueueRewardToast("DAILY CHEST", $"+{freeReward} COIN", new Color(1f, 0.68f, 0.28f, 1f));
            }
            RefreshStartScreenState();
        }

        private void HandleShopAction(ShopCatalogType category, string itemId)
        {
            if (economyManager == null)
            {
                return;
            }

            if (category == ShopCatalogType.Ball)
            {
                var skinDef = economyManager.Skins.FirstOrDefault(s => s.id == itemId);
                if (!string.IsNullOrWhiteSpace(skinDef.iapProductId) && !economyManager.IsOwnedSkin(itemId))
                {
                    TriggerIAPSkinPurchase(skinDef.iapProductId);
                    return;
                }
            }
            else if (category == ShopCatalogType.Tower)
            {
                var towerSkinDef = economyManager.TowerSkins.FirstOrDefault(s => s.id == itemId);
                if (!string.IsNullOrWhiteSpace(towerSkinDef.iapProductId) && !economyManager.IsOwnedTowerSkin(itemId))
                {
                    TriggerIAPSkinPurchase(towerSkinDef.iapProductId);
                    return;
                }
            }

            ShopActionResult result = category switch
            {
                ShopCatalogType.Ball => economyManager.PurchaseOrEquipSkin(itemId),
                ShopCatalogType.Tower => economyManager.PurchaseOrEquipTowerSkin(itemId),
                _ => ShopActionResult.None
            };

            if (category == ShopCatalogType.Coin)
            {
                if (coinStoreManager == null)
                {
                    QueueRewardToast("STORE OFFLINE", "COIN PACKS UNAVAILABLE", new Color(1f, 0.64f, 0.3f, 1f));
                    return;
                }

                CoinPackPurchaseResult purchaseResult = coinStoreManager.PurchasePack(itemId);
                switch (purchaseResult.status)
                {
                    case CoinPackPurchaseStatus.Succeeded:
                        QueueRewardToast(
                            purchaseResult.offer.displayName,
                            purchaseResult.message,
                            new Color(1f, 0.82f, 0.28f, 1f));
                        break;

                    case CoinPackPurchaseStatus.Unavailable:
                        QueueRewardToast("PURCHASE UNAVAILABLE", purchaseResult.message, new Color(1f, 0.62f, 0.28f, 1f));
                        break;

                    case CoinPackPurchaseStatus.Pending:
                        QueueRewardToast(purchaseResult.offer.displayName, purchaseResult.message, new Color(1f, 0.82f, 0.28f, 1f));
                        break;

                    default:
                        QueueRewardToast("PURCHASE FAILED", purchaseResult.message, new Color(1f, 0.56f, 0.3f, 1f));
                        break;
                }

                RefreshStartScreenState();
                shopScreenController?.SetState(economyManager.EmberBalance, economyManager.Skins, economyManager.TowerSkins, coinStoreManager.Offers, economyManager);
                return;
            }

            if (category == ShopCatalogType.Ball &&
                (result == ShopActionResult.Purchased || result == ShopActionResult.Equipped) &&
                playerController != null)
            {
                playerController.ApplySkin(economyManager.GetEquippedSkin());
            }

            if (startScreenController != null)
            {
                RefreshStartScreenState();
            }

            shopScreenController?.SetState(economyManager.EmberBalance, economyManager.Skins, economyManager.TowerSkins, coinStoreManager != null ? coinStoreManager.Offers : Array.Empty<CoinPackOffer>(), economyManager);
        }

        private static readonly string[] UpsellCandidateIds = { "welcome_pack", "no_ads_pack", "bundle_neon_rush", "bundle_frost_reign" };

        private void ShowFailIAPUpsell()
        {
            if (iapUpsellController == null || coinStoreManager == null)
            {
                return;
            }

            iapUpsellController.Show(coinStoreManager.Offers, UpsellCandidateIds);
        }

        private void TriggerUpsellPurchase(string offerId)
        {
            if (coinStoreManager == null)
            {
                return;
            }

            CoinPackPurchaseResult purchaseResult = coinStoreManager.PurchasePack(offerId);
            switch (purchaseResult.status)
            {
                case CoinPackPurchaseStatus.Succeeded:
                    QueueRewardToast(purchaseResult.offer.displayName, purchaseResult.message, new Color(1f, 0.82f, 0.28f, 1f));
                    break;
                case CoinPackPurchaseStatus.Pending:
                    QueueRewardToast(purchaseResult.offer.displayName, purchaseResult.message, new Color(1f, 0.82f, 0.28f, 1f));
                    break;
                default:
                    QueueRewardToast("PURCHASE FAILED", purchaseResult.message, new Color(1f, 0.56f, 0.3f, 1f));
                    break;
            }
        }

        private void TriggerIAPSkinPurchase(string iapProductId)
        {
            if (coinStoreManager == null)
            {
                QueueRewardToast("STORE OFFLINE", "PURCHASE UNAVAILABLE", new Color(1f, 0.64f, 0.3f, 1f));
                return;
            }

            CoinPackPurchaseResult purchaseResult = coinStoreManager.PurchasePack(iapProductId);
            switch (purchaseResult.status)
            {
                case CoinPackPurchaseStatus.Succeeded:
                    QueueRewardToast(purchaseResult.offer.displayName, purchaseResult.message, new Color(1f, 0.82f, 0.28f, 1f));
                    break;
                case CoinPackPurchaseStatus.Pending:
                    QueueRewardToast(purchaseResult.offer.displayName, purchaseResult.message, new Color(1f, 0.82f, 0.28f, 1f));
                    break;
                default:
                    QueueRewardToast("PURCHASE FAILED", purchaseResult.message, new Color(1f, 0.56f, 0.3f, 1f));
                    break;
            }

            shopScreenController?.SetState(economyManager.EmberBalance, economyManager.Skins, economyManager.TowerSkins, coinStoreManager != null ? coinStoreManager.Offers : Array.Empty<CoinPackOffer>(), economyManager);
        }

        private void HandleMissionReroll()
        {
            if (economyManager == null)
            {
                return;
            }

            if (!economyManager.TryRerollDailyMissions(out int spentCoins))
            {
                QueueRewardToast("NOT ENOUGH COIN", $"{economyManager.GetMissionRerollCost()} COIN NEEDED", new Color(1f, 0.62f, 0.28f, 1f));
                RefreshStartScreenState();
                return;
            }

            QueueRewardToast("MISSIONS REFRESHED", $"-{spentCoins} COIN", new Color(0.44f, 0.86f, 1f, 1f));
            RefreshStartScreenState();
        }

        private void HandleShopCoinBoost()
        {
            if (economyManager == null)
            {
                return;
            }

            if (rewardedAdManager == null)
            {
                int directReward = economyManager.GetShopCoinBoostReward();
                economyManager.ClaimShopCoinBoost();
                QueueRewardToast("SHOP BOOST", $"+{directReward} COIN", new Color(0.34f, 0.86f, 0.68f, 1f));
                RefreshStartScreenState();
                shopScreenController?.SetState(economyManager.EmberBalance, economyManager.Skins, economyManager.TowerSkins, coinStoreManager != null ? coinStoreManager.Offers : Array.Empty<CoinPackOffer>(), economyManager);
                return;
            }

            rewardedAdManager.ShowRewarded(RewardedPlacement.ShopCoinBoost, success =>
            {
                if (!success)
                {
                    return;
                }

                int reward = economyManager.GetShopCoinBoostReward();
                economyManager.ClaimShopCoinBoost();
                QueueRewardToast("SHOP BOOST", $"+{reward} COIN", new Color(0.34f, 0.86f, 0.68f, 1f));
                RefreshStartScreenState();
                shopScreenController?.SetState(economyManager.EmberBalance, economyManager.Skins, economyManager.TowerSkins, coinStoreManager != null ? coinStoreManager.Offers : Array.Empty<CoinPackOffer>(), economyManager);
            });
        }

        private void HandleCoinStoreRestore()
        {
            if (coinStoreManager == null)
            {
                QueueRewardToast("STORE OFFLINE", "RESTORE UNAVAILABLE", new Color(1f, 0.64f, 0.3f, 1f));
                return;
            }

            coinStoreManager.RestoreTransactions();
        }

        private void RefreshStartScreenState()
        {
            cachedDailyMissions = economyManager != null ? economyManager.DailyMissions : Array.Empty<DailyMissionState>();
            cachedChestStatus = economyManager != null ? economyManager.GetDailyChestStatus() : default;
            cachedDailyChallengeStatus = economyManager != null ? economyManager.DailyChallengeStatus : default;
            cachedMissionRerollCost = economyManager != null ? economyManager.GetMissionRerollCost() : 0;
            startScreenController?.SetState(cachedBestScore, economyManager != null ? economyManager.EmberBalance : 0, cachedLeaderboardEntries, cachedDailyMissions, cachedChestStatus, cachedDailyChallengeStatus, cachedMissionRerollCost, cachedSoundEnabled, cachedVibrationEnabled);
        }

        private void HandleCoinStoreOffersChanged()
        {
            if (economyManager == null || shopScreenController == null || !shopScreenController.gameObject.activeSelf)
            {
                return;
            }

            shopScreenController.SetState(economyManager.EmberBalance, economyManager.Skins, economyManager.TowerSkins, coinStoreManager != null ? coinStoreManager.Offers : Array.Empty<CoinPackOffer>(), economyManager);
        }

        private void HandleCoinStorePurchaseFinished(CoinPackPurchaseResult result)
        {
            if (result.status == CoinPackPurchaseStatus.Pending || result.status == CoinPackPurchaseStatus.None)
            {
                return;
            }

            switch (result.status)
            {
                case CoinPackPurchaseStatus.Succeeded:
                    QueueRewardToast(result.offer.displayName, result.message, new Color(1f, 0.82f, 0.28f, 1f));
                    break;

                case CoinPackPurchaseStatus.Unavailable:
                    QueueRewardToast("PURCHASE DEFERRED", result.message, new Color(1f, 0.72f, 0.34f, 1f));
                    break;

                default:
                    QueueRewardToast("PURCHASE FAILED", result.message, new Color(1f, 0.56f, 0.3f, 1f));
                    break;
            }

            RefreshStartScreenState();
            if (shopScreenController != null && shopScreenController.gameObject.activeSelf && economyManager != null)
            {
                shopScreenController.SetState(economyManager.EmberBalance, economyManager.Skins, economyManager.TowerSkins, coinStoreManager != null ? coinStoreManager.Offers : Array.Empty<CoinPackOffer>(), economyManager);
            }
        }

        private void HandleCoinStoreRestoreFinished(bool success, string message)
        {
            QueueRewardToast(success ? "RESTORE COMPLETE" : "RESTORE", message, success ? new Color(0.42f, 0.86f, 1f, 1f) : new Color(1f, 0.68f, 0.3f, 1f));
            if (shopScreenController != null && shopScreenController.gameObject.activeSelf && economyManager != null)
            {
                shopScreenController.SetState(economyManager.EmberBalance, economyManager.Skins, economyManager.TowerSkins, coinStoreManager != null ? coinStoreManager.Offers : Array.Empty<CoinPackOffer>(), economyManager);
            }
        }

        private void EnsureCanvas()
        {
            canvas = GetComponentInChildren<Canvas>(true);
            if (canvas == null)
            {
                GameObject canvasObject = new("UICanvas");
                canvasObject.transform.SetParent(transform, false);
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 50;
            }

            canvas.pixelPerfect = true;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
               scaler = canvas.gameObject.AddComponent<CanvasScaler>();
               scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
               scaler.referenceResolution = new Vector2(1080f, 1920f);
               scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
               scaler.matchWidthOrHeight = 0f; // Match Width for Portrait
               scaler.dynamicPixelsPerUnit = 2f;
            }
            else
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.matchWidthOrHeight = 0.5f; // Keep original if scaler already exists
                scaler.referencePixelsPerUnit = 100f;
                scaler.dynamicPixelsPerUnit = 2f;
            }


            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            EventSystem eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new("EventSystem");
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
            }

#if ENABLE_INPUT_SYSTEM
            InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
            {
                legacyModule.enabled = false;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (eventSystem.GetComponent<StandaloneInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }
#endif
        }

        private T CreatePanel<T>(string panelName, Transform parent) where T : Component
        {
            GameObject panel = new(panelName);
            panel.transform.SetParent(parent, false);
            RectTransform rectTransform = panel.AddComponent<RectTransform>();
            Stretch(rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return panel.AddComponent<T>();
        }

        internal static Text CreateText(string name, Transform parent, Font font, int fontSize, TextAnchor alignment, Color color)
        {
            GameObject textObject = new(name);
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.alignByGeometry = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.resizeTextForBestFit = false;
            text.supportRichText = false;
            text.raycastTarget = false;
            text.lineSpacing = 0.92f;
            return text;
        }

        internal static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject imageObject = new(name);
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        internal static Button CreateButton(string name, Transform parent, Font font, string label, Color baseColor, Color textColor)
        {
            Image image = CreateImage(name, parent, Color.white);
            ApplyButtonSurface(image, baseColor);
            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
            colors.selectedColor = new Color(0.98f, 0.98f, 0.98f, 1f);
            colors.pressedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
            colors.disabledColor = new Color(0.72f, 0.72f, 0.72f, 0.92f);
            button.colors = colors;

            Shadow shadow = image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.18f);
            shadow.effectDistance = new Vector2(0f, -8f);

            Outline outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.88f, 0.94f, 1f, 0.76f);
            outline.effectDistance = new Vector2(2f, -2f);

            Text text = CreateText($"{name}_Label", image.transform, font, 44, TextAnchor.MiddleCenter, textColor);
            text.text = label;
            Stretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            StyleToyText(text, new Color(0.12f, 0.2f, 0.42f, 0.72f), new Vector2(1f, -1f), new Color(1f, 1f, 1f, 0.18f), new Vector2(0f, 1f));
            return button;
        }

        internal static void BindButton(Button btn, Action action, Action soundCallback = null)
        {
            btn.onClick.AddListener(() =>
            {
                soundCallback?.Invoke();
                action?.Invoke();
            });
        }

        internal static Image CreateCard(string name, Transform parent, Color fillColor, Color outlineColor)
        {
            Image card = CreateImage(name, parent, Color.white);
            ApplyCardSurface(card, fillColor);
            Shadow shadow = card.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.16f);
            shadow.effectDistance = new Vector2(0f, -10f);

            Outline outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(3f, -3f);
            return card;
        }

        internal static void ApplyButtonSurface(Image image, Color targetColor)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = GetThemeButtonSprite(targetColor);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        internal static void ApplyCardSurface(Image image, Color targetColor)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = GetThemePanelSprite(targetColor);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        private static Sprite GetThemeButtonSprite(Color targetColor)
        {
            string key = "btn_flat_" + ColorUtility.ToHtmlStringRGB(targetColor);
            return CreateFlatSprite(key, 64, 32, targetColor);
        }

        private static Sprite GetThemePanelSprite(Color targetColor)
        {
            string key = "panel_flat_" + ColorUtility.ToHtmlStringRGB(targetColor);
            return CreateFlatSprite(key, 64, 18, targetColor);
        }

        private static string ResolveButtonThemeResource(Color color)
        {
            Color.RGBToHSV(color, out float hue, out float saturation, out float value);
            if (saturation < 0.14f)
            {
                return "button_grey";
            }

            if (hue >= 0.2f && hue <= 0.48f)
            {
                return "button_green";
            }

            if (hue >= 0.08f && hue <= 0.18f)
            {
                return "button_yellow";
            }

            return "button_blue";
        }

        private static string ResolvePanelThemeResource(Color color)
        {
            Color.RGBToHSV(color, out float hue, out float saturation, out float value);
            if (hue >= 0.03f && hue <= 0.17f && saturation > 0.18f)
            {
                return "panel_yellow";
            }

            if (hue >= 0.68f && hue <= 0.9f)
            {
                return "panel_purple";
            }

            if (hue >= 0.18f && hue <= 0.7f)
            {
                return "panel_blue";
            }

            return "panel_blue";
        }

        private static bool IsLightPanelColor(Color color)
        {
            Color.RGBToHSV(color, out _, out float saturation, out float value);
            return saturation < 0.12f && value > 0.9f;
        }

        private static bool IsDarkPanelColor(Color color)
        {
            Color.RGBToHSV(color, out _, out float saturation, out float value);
            return value < 0.22f || (value < 0.3f && saturation < 0.28f);
        }

        private static bool IsYellowThemeColor(Color color)
        {
            Color.RGBToHSV(color, out float hue, out float saturation, out float value);
            return hue >= 0.08f && hue <= 0.18f && saturation > 0.18f && value > 0.55f;
        }

        private static Sprite LoadGeneratedThemeSprite(string cacheKey, Func<Sprite> createSprite)
        {
            if (themedSpriteCache.TryGetValue(cacheKey, out Sprite cachedSprite))
            {
                return cachedSprite;
            }

            Sprite sprite = createSprite();
            themedSpriteCache[cacheKey] = sprite;
            return sprite;
        }

        private static Sprite LoadThemeSprite(string resourcePath, Vector4 border, string fallbackName, float fallbackRadius)
        {
            if (themedSpriteCache.TryGetValue(resourcePath, out Sprite cachedSprite))
            {
                return cachedSprite;
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                Sprite fallback = CreateRoundedSprite(fallbackName, 64, fallbackRadius);
                themedSpriteCache[resourcePath] = fallback;
                return fallback;
            }

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                border);
            sprite.name = resourcePath;
            themedSpriteCache[resourcePath] = sprite;
            return sprite;
        }

        private static Sprite CreateRoundedSprite(string name, int size, float radius)
        {
            Texture2D texture = new(size, size, TextureFormat.ARGB32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color32[] pixels = new Color32[size * size];
            float maxCoord = size - 1f;
            float clampedRadius = Mathf.Clamp(radius, 2f, size * 0.5f);
            float minCenter = clampedRadius - 0.5f;
            float maxCenter = maxCoord - clampedRadius + 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nearestX = Mathf.Clamp(x, minCenter, maxCenter);
                    float nearestY = Mathf.Clamp(y, minCenter, maxCenter);
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(nearestX, nearestY));
                    float alpha = Mathf.Clamp01(clampedRadius + 0.5f - distance);
                    pixels[(y * size) + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(clampedRadius, clampedRadius, clampedRadius, clampedRadius));
            sprite.name = name;
            return sprite;
        }

        private static Sprite CreateFlatSprite(string name, int size, int radius, Color color)
        {
            if (themedSpriteCache.TryGetValue(name, out var cached)) return cached;

            int r = Mathf.Min(radius, size / 2);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[size * size];
            // Uses sub-pixel SDF (same approach as CreateRoundedSprite) for anti-aliased corners.
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inCornerRegion = (x < r || x >= size - r) && (y < r || y >= size - r);
                    float alpha;
                    if (inCornerRegion)
                    {
                        float cx = (x < r) ? r : size - r - 1;
                        float cy = (y < r) ? r : size - r - 1;
                        float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                        alpha = Mathf.Clamp01(r + 0.5f - dist); // sub-pixel anti-alias
                    }
                    else
                    {
                        alpha = 1f;
                    }
                    pixels[y * size + x] = new Color32(
                        (byte)(color.r * 255),
                        (byte)(color.g * 255),
                        (byte)(color.b * 255),
                        (byte)(color.a * alpha * 255)
                    );
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            int b = size / 4;
            var sprite = Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f, 0,
                SpriteMeshType.FullRect,
                new Vector4(b, b, b, b)
            );
            sprite.name = name;
            themedSpriteCache[name] = sprite;
            return sprite;
        }

        // RETIRED: replaced by CreateFlatSprite. Kept for rollback. Do not add new callers.
        private static Sprite CreateGlossyPanelSprite(
            string name,
            int size,
            float radius,
            Color topFill,
            Color bottomFill,
            Color borderColor,
            Color glossColor,
            Vector4 border)
        {
            Texture2D texture = new(size, size, TextureFormat.ARGB32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color32[] pixels = new Color32[size * size];
            float maxCoord = size - 1f;
            float clampedRadius = Mathf.Clamp(radius, 2f, size * 0.5f);
            float minCenter = clampedRadius - 0.5f;
            float maxCenter = maxCoord - clampedRadius + 0.5f;

            for (int y = 0; y < size; y++)
            {
                float verticalT = y / maxCoord;
                for (int x = 0; x < size; x++)
                {
                    float nearestX = Mathf.Clamp(x, minCenter, maxCenter);
                    float nearestY = Mathf.Clamp(y, minCenter, maxCenter);
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(nearestX, nearestY));
                    float alpha = Mathf.Clamp01(clampedRadius + 0.5f - distance);
                    if (alpha <= 0f)
                    {
                        pixels[(y * size) + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    float edgeDistance = Mathf.Min(Mathf.Min(x, maxCoord - x), Mathf.Min(y, maxCoord - y));
                    float edgeMask = 1f - Mathf.SmoothStep(4f, 16f, edgeDistance);
                    Color baseColor = Color.Lerp(bottomFill, topFill, verticalT);
                    Color panelColor = Color.Lerp(baseColor, borderColor, edgeMask * 0.85f);

                    float gloss = Mathf.SmoothStep(0.56f, 0.96f, verticalT) * (1f - (edgeMask * 0.35f));
                    panelColor = Color.Lerp(panelColor, glossColor, gloss * 0.4f);

                    float shadow = Mathf.SmoothStep(0f, 0.18f, verticalT) * 0.08f;
                    panelColor = Color.Lerp(panelColor, new Color(0.72f, 0.8f, 0.92f, 1f), shadow);

                    pixels[(y * size) + x] = new Color(
                        panelColor.r,
                        panelColor.g,
                        panelColor.b,
                        alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                border);
            sprite.name = name;
            return sprite;
        }

        internal static void StyleToyText(Text text, Color outlineColor, Vector2 outlineDistance, Color shadowColor, Vector2 shadowDistance)
        {
            if (text == null)
            {
                return;
            }
        }

        internal static void StyleButtonLabel(Button button, int fontSize, TextAnchor alignment, Vector2 offsetMin, Vector2 offsetMax)
        {
            Text text = button.GetComponentInChildren<Text>();
            if (text == null)
            {
                return;
            }

            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.raycastTarget = false;
            Stretch(text.rectTransform, Vector2.zero, Vector2.one, offsetMin, offsetMax);
            StyleToyText(text, new Color(0.12f, 0.2f, 0.42f, 0.72f), new Vector2(1f, -1f), new Color(1f, 1f, 1f, 0.18f), new Vector2(0f, 1f));
        }

        internal static void Stretch(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }

        // ── Required static helpers (Tasks 3–9) ─────────────────────────────────

        public static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>(); img.color = color; return img;
        }

        public static Text CreateText(Transform parent, string name, string text,
            int size, FontStyle style, Color color, Font font,
            TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>(); t.text = text; t.font = font;
            t.fontSize = size; t.fontStyle = style; t.color = color; t.alignment = anchor; return t;
        }

        // Stretch with default params covers both Stretch(rt) and Stretch(rt, l, r, t, b) call sites.
        public static void Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom); rt.offsetMax = new Vector2(-right, -top);
        }

        public static void BindButton(Button btn, System.Action onClick)
        {
            btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(() => onClick?.Invoke());
        }

        /// <summary>Creates a 26x26px icon pill button (used for settings and medals in top bars).</summary>
        public static GameObject CreateIconButton(Transform parent, string name, string icon, Font font, Color bgColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>(); img.color = bgColor; img.raycastTarget = true;
            var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(26, 26);
            var lbl = CreateText(go.transform, "Icon", icon,
                13, FontStyle.Normal, Color.white, font, TextAnchor.MiddleCenter);
            Stretch(lbl.rectTransform);
            return go;
        }

        /// <summary>Creates a gradient action button (START/CONTINUE/CLAIM style).</summary>
        public static GameObject CreateActionButton(Transform parent, string name, string label,
            Font font, Color colorA, Color colorB, int height, int padH)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = colorA;
            img.raycastTarget = true;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, height);
            var lbl = CreateText(go.transform, "Label", label,
                15, FontStyle.Bold, Color.white, font, TextAnchor.MiddleCenter);
            Stretch(lbl.rectTransform);
            BindButton(btn, () => { });
            return go;
        }

        /// <summary>Secondary button: semi-transparent dark bg, dim text.</summary>
        public static GameObject CreateSecondaryButton(Transform parent, string name, string label, Font font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.10f);
            img.raycastTarget = true;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            go.AddComponent<LayoutElement>().flexibleWidth = 1;
            var lbl = CreateText(go.transform, "Label", label,
                10, FontStyle.Bold, new Color(1, 1, 1, 0.70f), font, TextAnchor.MiddleCenter);
            Stretch(lbl.rectTransform);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 44);
            return go;
        }
    }

    // ─── Legacy color tokens (pre-redesign) ─────────────────────────────────────
    // These were originally defined in UISystems.cs (lines 13-49).
    // All references will be replaced with UIStyle.* during Tasks 4-11.
    public static class UIColors
    {
        public static readonly Color Primary     = new Color(0.29f, 0.52f, 0.91f);
        public static readonly Color PrimaryLight = new Color(0.49f, 0.69f, 1.00f);
        public static readonly Color PrimaryBg   = new Color(0.29f, 0.52f, 0.91f, 0.12f);
        public static readonly Color Surface     = new Color(1f, 1f, 1f, 0.06f);
        public static readonly Color Card        = new Color(1f, 1f, 1f, 0.10f);
        public static readonly Color Divider     = new Color(1f, 1f, 1f, 0.08f);
        public static readonly Color TextDark    = new Color(1f, 1f, 1f, 0.95f);
        public static readonly Color TextMid     = new Color(1f, 1f, 1f, 0.65f);
        public static readonly Color TextDim     = new Color(1f, 1f, 1f, 0.40f);
        public static readonly Color Danger      = new Color(0.94f, 0.27f, 0.27f);
        public static readonly Color Success     = new Color(0.06f, 0.72f, 0.51f);
        public static readonly Color SuccessBg   = new Color(0.06f, 0.72f, 0.51f, 0.15f);
        public static readonly Color SuccessText = new Color(0.06f, 0.72f, 0.51f);
        public static readonly Color Warning     = new Color(1.00f, 0.62f, 0.04f);
        public static readonly Color HudBg       = new Color(0.06f, 0.04f, 0.12f, 0.85f);
        public static readonly Color HudCard     = new Color(1f, 1f, 1f, 0.08f);
        public static readonly Color HudBorder   = new Color(1f, 1f, 1f, 0.05f);
        public static readonly Color HudTextDim  = new Color(1f, 1f, 1f, 0.50f);
    }

    // ─── Shop catalog type (pre-redesign) ───────────────────────────────────────
    public enum ShopCatalogType { Coin, Ball, Tower }
}
