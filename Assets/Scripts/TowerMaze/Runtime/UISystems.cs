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
    public enum ShopCatalogType
    {
        Coin,
        Ball,
        Tower,
    }

    static class UIColors
    {
        // Primary
        public static readonly Color Primary      = new Color(0.486f, 0.227f, 0.929f, 1f); // #7C3AED
        public static readonly Color PrimaryLight  = new Color(0.655f, 0.545f, 0.980f, 1f); // #A78BFA
        public static readonly Color PrimaryBg     = new Color(0.929f, 0.914f, 0.996f, 1f); // #EDE9FE
        // Surface
        public static readonly Color Surface       = new Color(0.973f, 0.969f, 1.000f, 1f); // #F8F7FF
        public static readonly Color Card          = Color.white;                             // #FFFFFF
        public static readonly Color Divider       = new Color(0.941f, 0.933f, 1.000f, 1f); // #F0EEFF
        // Text
        public static readonly Color TextDark      = new Color(0.067f, 0.067f, 0.067f, 1f); // #111111
        public static readonly Color TextMid       = new Color(0.333f, 0.333f, 0.333f, 1f); // #555555
        public static readonly Color TextDim       = new Color(0.667f, 0.667f, 0.667f, 1f); // #AAAAAA
        // Semantic
        public static readonly Color Success       = new Color(0.063f, 0.725f, 0.506f, 1f); // #10B981
        public static readonly Color SuccessBg     = new Color(0.820f, 0.980f, 0.898f, 1f); // #D1FAE5
        public static readonly Color SuccessText   = new Color(0.020f, 0.588f, 0.412f, 1f); // #059669
        public static readonly Color Danger        = new Color(0.937f, 0.267f, 0.267f, 1f); // #EF4444
        public static readonly Color DangerBg      = new Color(0.996f, 0.886f, 0.886f, 1f); // #FEE2E2
        public static readonly Color DangerText    = new Color(0.863f, 0.149f, 0.149f, 1f); // #DC2626
        public static readonly Color Warning       = new Color(0.961f, 0.620f, 0.043f, 1f); // #F59E0B
        public static readonly Color WarningBg     = new Color(0.996f, 0.953f, 0.784f, 1f); // #FEF3C7
        public static readonly Color WarningText   = new Color(0.851f, 0.467f, 0.024f, 1f); // #D97706
        // HUD
        public static readonly Color HudBg         = new Color(0.059f, 0.039f, 0.118f, 1f); // #0F0A1E
        public static readonly Color HudCard        = new Color(0.110f, 0.078f, 0.200f, 1f); // #1C1433
        public static readonly Color HudBorder      = new Color(0.176f, 0.125f, 0.314f, 1f); // #2D2050
        public static readonly Color HudTextDim     = new Color(0.486f, 0.435f, 0.627f, 1f); // #7C6FA0
    }

    public sealed class UIManager : MonoBehaviour
    {
        private ThemeDefinition theme;
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
            Sprite staticBackground = null)
        {
            splashComplete = !splashActive;
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
            hudController.Initialize(runtimeFont, theme, onPause, buttonClickSound);

            startScreenController = CreatePanel<StartScreenController>("StartScreen", canvas.transform);
            startScreenController.Initialize(runtimeFont, theme, economyManager, onPlay, onPlayDailyChallenge, ShowShop, HandleChestClaim, onToggleSound, onToggleVibration, HandleMissionReroll, buttonClickSound);

            failScreenController = CreatePanel<FailScreenController>("FailScreen", canvas.transform);
            failScreenController.Initialize(runtimeFont, theme, economyManager, onRetry, onContinue, onReturnToMenu, onClaimDoubleReward, onWatchLifeRefillAd, onBuyLifeRefillWithCoins, buttonClickSound);

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
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
            scaler.dynamicPixelsPerUnit = 2f;

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject eventSystem = new("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<InputSystemUIInputModule>();
            }
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
    }

    public sealed class CountdownController : MonoBehaviour
    {
        private Text countdownText;
        private Color numberColor;
        private Color goColor;

        public void Initialize(Font font, ThemeDefinition theme)
        {
            countdownText = UIManager.CreateText("CountdownText", transform, font, 180, TextAnchor.MiddleCenter, UIColors.Primary);
            countdownText.fontStyle = FontStyle.Bold;
            numberColor = UIColors.Primary;
            goColor = UIColors.PrimaryLight;
            UIManager.Stretch(countdownText.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(-220f, -180f), new Vector2(220f, 180f));
        }

        public void SetValue(string label, bool isGo)
        {
            countdownText.text = label;
            countdownText.fontSize = isGo ? 150 : 180;
            countdownText.color = isGo ? goColor : numberColor;
            countdownText.transform.localScale = isGo ? Vector3.one * 1.08f : Vector3.one;
        }
    }

    public sealed class RewardToastController : MonoBehaviour
    {
        private readonly Queue<RewardToastData> pendingToasts = new();
        private Image backgroundImage;
        private Text titleText;
        private Text subtitleText;
        private float displayRemaining;
        private const float DisplayDuration = 1.85f;

        private readonly struct RewardToastData
        {
            public readonly string Title;
            public readonly string Subtitle;
            public readonly Color AccentColor;

            public RewardToastData(string title, string subtitle, Color accentColor)
            {
                Title = title;
                Subtitle = subtitle;
                AccentColor = accentColor;
            }
        }

        public void Initialize(Font font, ThemeDefinition theme)
        {
            GameObject container = new("RewardToastContainer");
            container.transform.SetParent(transform, false);
            RectTransform containerRect = container.AddComponent<RectTransform>();
            UIManager.Stretch(containerRect, new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f), new Vector2(-260f, -64f), new Vector2(260f, 64f));

            backgroundImage = container.AddComponent<Image>();
            backgroundImage.color = new Color(UIColors.Card.r, UIColors.Card.g, UIColors.Card.b, 0f);

            titleText = UIManager.CreateText("RewardToastTitle", container.transform, font, 34, TextAnchor.UpperCenter, UIColors.TextDark);
            titleText.fontStyle = FontStyle.Bold;
            UIManager.Stretch(titleText.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(18f, -10f), new Vector2(-18f, -4f));

            subtitleText = UIManager.CreateText("RewardToastSubtitle", container.transform, font, 28, TextAnchor.LowerCenter, UIColors.TextDim);
            UIManager.Stretch(subtitleText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.55f), new Vector2(18f, 8f), new Vector2(-18f, -8f));
        }

        public void Enqueue(string title, string subtitle, Color accentColor)
        {
            pendingToasts.Enqueue(new RewardToastData(title, subtitle, accentColor));
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                ShowNextToast();
            }
        }

        private void Update()
        {
            if (displayRemaining <= 0f)
            {
                return;
            }

            displayRemaining = Mathf.Max(0f, displayRemaining - Time.unscaledDeltaTime);
            float normalized = 1f - (displayRemaining / DisplayDuration);
            float fadeIn = Mathf.Clamp01(normalized / 0.18f);
            float fadeOut = Mathf.Clamp01(displayRemaining / 0.28f);
            float alpha = Mathf.Min(fadeIn, fadeOut);

            Color background = backgroundImage.color;
            background.a = 0.84f * alpha;
            backgroundImage.color = background;

            Color title = titleText.color;
            title.a = alpha;
            titleText.color = title;

            Color subtitle = subtitleText.color;
            subtitle.a = alpha;
            subtitleText.color = subtitle;

            float pulse = 0.5f + (0.5f * Mathf.Sin(Time.unscaledTime * 7f));
            transform.GetChild(0).localScale = Vector3.one * Mathf.Lerp(0.96f, 1.02f, pulse * alpha);

            if (displayRemaining <= 0f)
            {
                ShowNextToast();
            }
        }

        private void ShowNextToast()
        {
            if (pendingToasts.Count == 0)
            {
                gameObject.SetActive(false);
                return;
            }

            RewardToastData toast = pendingToasts.Dequeue();
            titleText.text = toast.Title;
            subtitleText.text = toast.Subtitle;
            titleText.color = UIColors.TextDark;
            subtitleText.color = UIColors.Warning;
            backgroundImage.color = new Color(UIColors.Card.r, UIColors.Card.g, UIColors.Card.b, 0f);
            transform.GetChild(0).localScale = Vector3.one * 0.96f;
            displayRemaining = DisplayDuration;
        }
    }

    public sealed class UIHudController : MonoBehaviour
    {
        private Image gapCard;
        private Image newBestCard;
        private Image controlsHintCard;
        private Text currentScoreText;
        private Text bestScoreText;
        private Text zoneText;
        private Text timerText;
        private Text gapText;
        private Text newBestText;
        private Text controlsHintText;
        private Button pauseButton;

        public void Initialize(Font font, ThemeDefinition theme, Action onPause, Action soundCallback = null)
        {
            Color subtleHighlight = new Color(1f, 1f, 1f, 0.18f);
            Color subtleShadow = new Color(0.06f, 0.08f, 0.12f, 0.16f);

            Image currentScorePanel = UIManager.CreateCard("CurrentScoreCard", transform, UIColors.HudCard, UIColors.HudBorder);
            UIManager.Stretch(currentScorePanel.rectTransform, new Vector2(0.03f, 0.92f), new Vector2(0.26f, 0.975f), Vector2.zero, Vector2.zero);
            currentScoreText = UIManager.CreateText("CurrentScore", currentScorePanel.transform, font, 25, TextAnchor.MiddleCenter, Color.white);
            currentScoreText.fontStyle = FontStyle.Bold;
            UIManager.Stretch(currentScoreText.rectTransform, Vector2.zero, Vector2.one, new Vector2(16f, 6f), new Vector2(-16f, -6f));
            UIManager.StyleToyText(currentScoreText, subtleHighlight, new Vector2(0f, 1f), subtleShadow, new Vector2(0f, -1f));

            Image bestScorePanel = UIManager.CreateCard("BestScoreCard", transform, UIColors.HudCard, UIColors.HudBorder);
            UIManager.Stretch(bestScorePanel.rectTransform, new Vector2(0.74f, 0.92f), new Vector2(0.97f, 0.975f), Vector2.zero, Vector2.zero);
            bestScoreText = UIManager.CreateText("BestScore", bestScorePanel.transform, font, 25, TextAnchor.MiddleCenter, UIColors.PrimaryLight);
            bestScoreText.fontStyle = FontStyle.Bold;
            UIManager.Stretch(bestScoreText.rectTransform, Vector2.zero, Vector2.one, new Vector2(16f, 6f), new Vector2(-16f, -6f));
            UIManager.StyleToyText(bestScoreText, subtleHighlight, new Vector2(0f, 1f), subtleShadow, new Vector2(0f, -1f));

            Image zonePanel = UIManager.CreateCard("ZoneCard", transform, UIColors.HudCard, UIColors.HudBorder);
            UIManager.Stretch(zonePanel.rectTransform, new Vector2(0.39f, 0.92f), new Vector2(0.61f, 0.975f), Vector2.zero, Vector2.zero);
            zoneText = UIManager.CreateText("Zone", zonePanel.transform, font, 24, TextAnchor.MiddleCenter, UIColors.PrimaryLight);
            zoneText.fontStyle = FontStyle.Bold;
            UIManager.Stretch(zoneText.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 0f), new Vector2(-12f, 0f));
            UIManager.StyleToyText(zoneText, new Color(1f, 1f, 1f, 0.16f), new Vector2(0f, 1f), new Color(0.08f, 0.1f, 0.12f, 0.18f), new Vector2(0f, -1f));

            Image timerPanel = UIManager.CreateCard("TimerCard", transform, UIColors.HudCard, UIColors.HudBorder);
            UIManager.Stretch(timerPanel.rectTransform, new Vector2(0.35f, 0.86f), new Vector2(0.65f, 0.91f), Vector2.zero, Vector2.zero);
            timerText = UIManager.CreateText("Timer", timerPanel.transform, font, 23, TextAnchor.MiddleCenter, UIColors.HudTextDim);
            timerText.fontStyle = FontStyle.Bold;
            UIManager.Stretch(timerText.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 0f), new Vector2(-12f, 0f));
            UIManager.StyleToyText(timerText, subtleHighlight, new Vector2(0f, 1f), subtleShadow, new Vector2(0f, -1f));

            gapCard = UIManager.CreateCard("GapCard", transform, UIColors.HudCard, UIColors.HudBorder);
            UIManager.Stretch(gapCard.rectTransform, new Vector2(0.03f, 0.86f), new Vector2(0.26f, 0.91f), Vector2.zero, Vector2.zero);
            gapText = UIManager.CreateText("Gap", gapCard.transform, font, 22, TextAnchor.MiddleCenter, UIColors.HudTextDim);
            gapText.fontStyle = FontStyle.Bold;
            UIManager.Stretch(gapText.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 0f), new Vector2(-12f, 0f));
            UIManager.StyleToyText(gapText, subtleHighlight, new Vector2(0f, 1f), subtleShadow, new Vector2(0f, -1f));

            newBestCard = UIManager.CreateCard("NewBestCard", transform, UIColors.SuccessBg, UIColors.HudBorder);
            UIManager.Stretch(newBestCard.rectTransform, new Vector2(0.74f, 0.86f), new Vector2(0.97f, 0.91f), Vector2.zero, Vector2.zero);
            newBestText = UIManager.CreateText("NewBest", newBestCard.transform, font, 21, TextAnchor.MiddleCenter, UIColors.SuccessText);
            newBestText.fontStyle = FontStyle.Bold;
            newBestText.text = "NEW BEST";
            UIManager.Stretch(newBestText.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));
            UIManager.StyleToyText(newBestText, new Color(0.12f, 0.32f, 0.12f, 0.72f), new Vector2(1f, -1f), new Color(1f, 1f, 1f, 0.16f), new Vector2(0f, 1f));
            newBestCard.gameObject.SetActive(false);

            controlsHintCard = UIManager.CreateCard("ControlsHintCard", transform, UIColors.HudCard, UIColors.HudBorder);
            UIManager.Stretch(controlsHintCard.rectTransform, new Vector2(0.24f, 0.03f), new Vector2(0.76f, 0.072f), Vector2.zero, Vector2.zero);
            controlsHintText = UIManager.CreateText("ControlsHint", controlsHintCard.transform, font, 18, TextAnchor.MiddleCenter, UIColors.HudTextDim);
            controlsHintText.fontStyle = FontStyle.Bold;
            controlsHintText.text = "HOLD UP  |  SWIPE  |  DROP";
            UIManager.Stretch(controlsHintText.rectTransform, Vector2.zero, Vector2.one, new Vector2(14f, 0f), new Vector2(-14f, 0f));
            UIManager.StyleToyText(controlsHintText, subtleHighlight, new Vector2(0f, 1f), subtleShadow, new Vector2(0f, -1f));

            // Pause button — bottom-left, same row as controls hint
            Image pauseCard = UIManager.CreateCard("PauseBtn", transform, UIColors.HudCard, UIColors.HudBorder);
            UIManager.Stretch(pauseCard.rectTransform,
                new Vector2(0.03f, 0.03f), new Vector2(0.13f, 0.072f),
                Vector2.zero, Vector2.zero);
            Text pauseLabel = UIManager.CreateText("PauseBtnLabel", pauseCard.transform, font, 22, TextAnchor.MiddleCenter, UIColors.HudTextDim);
            pauseLabel.text = "II";
            pauseLabel.fontStyle = FontStyle.Bold;
            UIManager.Stretch(pauseLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            pauseButton = pauseCard.gameObject.AddComponent<Button>();
            UIManager.BindButton(pauseButton, onPause, soundCallback);
        }

        public void SetValues(float score, float bestScore, float runTime, int zoneIndex, float lavaGap, float gapDangerNormalized, bool isNewBest, bool showControlsHint)
        {
            currentScoreText.text = $"HEIGHT {score:0.0}m";
            bestScoreText.text = $"BEST {bestScore:0.0}m";
            zoneText.text = $"ZONE {zoneIndex + 1}";
            timerText.text = UiTextFormatter.FormatTime(runTime);
            gapText.text = $"LAVA {lavaGap:0.0}m";

            float dangerValue = Mathf.Clamp01(gapDangerNormalized);
            float gapPulseValue = 0.5f + (0.5f * Mathf.Sin(Time.unscaledTime * 9f));
            UIManager.ApplyCardSurface(gapCard, Color.Lerp(UIColors.HudCard, UIColors.Danger, dangerValue));
            gapText.color = Color.Lerp(UIColors.HudTextDim, Color.white, dangerValue);
            gapCard.transform.localScale = Vector3.one * (dangerValue > 0.6f
                ? Mathf.Lerp(1f, 1.12f, ((dangerValue - 0.6f) / 0.4f) * gapPulseValue)
                : 1f);

            newBestCard.gameObject.SetActive(isNewBest);
            if (isNewBest)
            {
                float pulse = 0.5f + (0.5f * Mathf.Sin(Time.unscaledTime * 8f));
                UIManager.ApplyCardSurface(newBestCard, Color.Lerp(UIColors.SuccessBg, UIColors.SuccessText, pulse));
                newBestCard.transform.localScale = Vector3.one * Mathf.Lerp(0.98f, 1.05f, pulse);
            }

            controlsHintCard.gameObject.SetActive(showControlsHint);
            return;
        }
    }

    internal static class UiTextFormatter
    {
        public static string FormatTime(float seconds)
        {
            float clampedSeconds = Mathf.Max(0f, seconds);
            int minutes = Mathf.FloorToInt(clampedSeconds / 60f);
            float remainingSeconds = clampedSeconds - (minutes * 60f);
            return $"{minutes:00}:{remainingSeconds:00.0}";
        }

        public static string FormatCountdown(TimeSpan time)
        {
            TimeSpan clamped = time < TimeSpan.Zero ? TimeSpan.Zero : time;
            int totalHours = Mathf.Max(0, (int)clamped.TotalHours);
            return $"{totalHours:00}:{clamped.Minutes:00}:{clamped.Seconds:00}";
        }

        public static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, Mathf.Max(1, maxLength - 3)) + "...";
        }
    }

    public sealed class PauseScreenController : MonoBehaviour
    {
        private Action buttonClickSound;

        public void Initialize(Font font, ThemeDefinition theme, Action onResume, Action onReturnToMenu, Action onButtonClick = null)
        {
            buttonClickSound = onButtonClick;

            // Full-screen dim overlay
            Image dim = UIManager.CreateImage("PauseDim", transform, new Color(0f, 0f, 0f, 0.72f));
            UIManager.Stretch(dim.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            dim.raycastTarget = true;

            // Center panel
            Image panel = UIManager.CreateCard("PausePanel", transform, UIColors.HudBg, UIColors.HudBorder);
            UIManager.Stretch(panel.rectTransform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-200f, -180f), new Vector2(200f, 180f));

            // Title
            Text title = UIManager.CreateText("PauseTitle", panel.transform, font, 36, TextAnchor.MiddleCenter, UIColors.PrimaryLight);
            title.text = "PAUSED";
            title.fontStyle = FontStyle.Bold;
            UIManager.Stretch(title.rectTransform,
                new Vector2(0f, 0.72f), new Vector2(1f, 1f),
                new Vector2(24f, 0f), new Vector2(-24f, 0f));

            // Resume button
            Button resumeBtn = UIManager.CreateButton("ResumeBtn", panel.transform, font, "DEVAM ET", UIColors.Primary, Color.white);
            UIManager.Stretch(resumeBtn.GetComponent<RectTransform>(),
                new Vector2(0.1f, 0.42f), new Vector2(0.9f, 0.68f),
                Vector2.zero, Vector2.zero);
            UIManager.BindButton(resumeBtn, onResume, buttonClickSound);

            // Main menu button
            Button menuBtn = UIManager.CreateButton("MainMenuBtn", panel.transform, font, "ANA MENÜ", UIColors.HudCard, UIColors.PrimaryLight);
            UIManager.Stretch(menuBtn.GetComponent<RectTransform>(),
                new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.36f),
                Vector2.zero, Vector2.zero);
            UIManager.BindButton(menuBtn, onReturnToMenu, buttonClickSound);
        }
    }

    public sealed class LeaderboardPanelController : MonoBehaviour
    {
        private Text titleText;
        private Text entriesText;

        public void Initialize(Font font, ThemeDefinition theme, string title)
        {
            Image backdropCard = UIManager.CreateCard("LeaderboardBackdrop", transform, UIColors.Card, UIColors.Divider);
            UIManager.Stretch(backdropCard.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image titleBadge = UIManager.CreateCard("LeaderboardTitlePill", transform, UIColors.PrimaryBg, UIColors.Divider);
            UIManager.Stretch(titleBadge.rectTransform, new Vector2(0.12f, 0.72f), new Vector2(0.88f, 0.94f), Vector2.zero, Vector2.zero);

            titleText = UIManager.CreateText("LeaderboardTitle", titleBadge.transform, font, 18, TextAnchor.MiddleCenter, UIColors.TextDark);
            titleText.fontStyle = FontStyle.Bold;
            titleText.text = title;
            UIManager.Stretch(titleText.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 0f), new Vector2(-12f, 0f));
            UIManager.StyleToyText(titleText, new Color(1f, 1f, 1f, 0.18f), new Vector2(0f, 1f), new Color(0.06f, 0.08f, 0.12f, 0.18f), new Vector2(0f, -1f));

            entriesText = UIManager.CreateText("LeaderboardEntries", transform, font, 18, TextAnchor.UpperCenter, UIColors.Primary);
            UIManager.Stretch(entriesText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.72f), new Vector2(18f, 14f), new Vector2(-18f, -8f));
            UIManager.StyleToyText(entriesText, new Color(1f, 1f, 1f, 0.12f), new Vector2(0f, 1f), new Color(0.06f, 0.08f, 0.12f, 0.16f), new Vector2(0f, -1f));
            return;
        }

        public void SetTitle(string title)
        {
            if (titleText != null) titleText.text = title;
        }

        public void SetEntries(IReadOnlyList<LeaderboardEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                entriesText.text = "No runs yet";
                return;
            }

            StringBuilder builder = new();
            int count = Mathf.Min(3, entries.Count);
            for (int index = 0; index < count; index++)
            {
                LeaderboardEntry entry = entries[index];
                builder.Append(index + 1)
                    .Append("  ");

                if (!string.IsNullOrWhiteSpace(entry.label))
                {
                    builder.Append(entry.label)
                        .Append("  ")
                        .Append(entry.height.ToString("0.0"))
                        .Append("m");
                }
                else
                {
                    builder.Append(entry.height.ToString("0.0"))
                        .Append("m   ")
                        .Append(UiTextFormatter.FormatTime(entry.timeSeconds).Replace(".0", string.Empty));
                }

                if (index < count - 1)
                {
                    builder.AppendLine();
                }
            }

            entriesText.text = builder.ToString();
        }
    }

    public sealed class ControlFlipController : MonoBehaviour
    {
        private Image pulseImage;
        private Image timerTrack;
        private Image timerFill;
        private Text warningText;
        private Color warningColor;
        private Color activeColor;

        public void Initialize(Font font, ThemeDefinition theme)
        {
            warningColor = new Color(UIColors.PrimaryBg.r, UIColors.PrimaryBg.g, UIColors.PrimaryBg.b, 0f);
            activeColor = new Color(UIColors.PrimaryBg.r, UIColors.PrimaryBg.g, UIColors.PrimaryBg.b, 0f);

            pulseImage = UIManager.CreateImage("ControlFlipPulse", transform, warningColor);
            UIManager.Stretch(pulseImage.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            pulseImage.raycastTarget = false;

            timerTrack = UIManager.CreateImage("ControlFlipTrack", transform, new Color(0f, 0f, 0f, 0.3f));
            UIManager.Stretch(timerTrack.rectTransform, new Vector2(0.16f, 0.2f), new Vector2(0.84f, 0.2f), new Vector2(0f, -18f), new Vector2(0f, 6f));
            timerTrack.raycastTarget = false;

            timerFill = UIManager.CreateImage("ControlFlipFill", timerTrack.transform, new Color(1f, 0.76f, 0.28f, 1f));
            timerFill.type = Image.Type.Filled;
            timerFill.fillMethod = Image.FillMethod.Horizontal;
            timerFill.fillOrigin = 0;
            timerFill.fillAmount = 1f;
            UIManager.Stretch(timerFill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            timerFill.raycastTarget = false;

            warningText = UIManager.CreateText("ControlFlipText", transform, font, 72, TextAnchor.MiddleCenter, Color.white);
            warningText.fontStyle = FontStyle.Bold;
            warningText.text = "CONTROL FLIP";
            UIManager.Stretch(warningText.rectTransform, new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f), new Vector2(-340f, -70f), new Vector2(340f, 70f));
        }

        public void SetState(bool controlsFlipped, float pulse, float remainingNormalized, float totalDurationSeconds)
        {
            float clampedPulse = Mathf.Clamp01(pulse);
            Color overlayColor = controlsFlipped ? activeColor : warningColor;
            overlayColor.a = controlsFlipped ? Mathf.Lerp(0.08f, 0.18f, clampedPulse) : Mathf.Lerp(0.05f, 0.13f, clampedPulse);
            pulseImage.color = overlayColor;
            timerFill.color = controlsFlipped ? new Color(1f, 0.28f, 0.28f, 1f) : new Color(1f, 0.72f, 0.28f, 1f);
            timerFill.fillAmount = Mathf.Clamp01(remainingNormalized);
            warningText.color = controlsFlipped
                ? Color.Lerp(new Color(1f, 0.82f, 0.82f, 1f), Color.white, clampedPulse)
                : Color.Lerp(new Color(1f, 0.86f, 0.62f, 0.96f), Color.white, clampedPulse);
            warningText.text = $"CONTROL FLIP {Mathf.Max(1, Mathf.CeilToInt(totalDurationSeconds))}s";
            warningText.transform.localScale = Vector3.one * (controlsFlipped
                ? Mathf.Lerp(1f, 1.06f, clampedPulse)
                : Mathf.Lerp(0.98f, 1.03f, clampedPulse));
        }
    }

    public sealed class RushWarningController : MonoBehaviour
    {
        private RectTransform badgeRoot;
        private Image badgeBg;
        private Image iconImage;
        private Image timerFill;
        private Text hurryText;

        public void Initialize(Font font, ThemeDefinition theme)
        {
            // Floating center-top badge
            GameObject rootObj = new GameObject("RushBadge");
            badgeRoot = rootObj.AddComponent<RectTransform>();
            badgeRoot.SetParent(transform, false);
            badgeRoot.anchorMin = new Vector2(0.5f, 1f);
            badgeRoot.anchorMax = new Vector2(0.5f, 1f);
            badgeRoot.pivot = new Vector2(0.5f, 1f);
            badgeRoot.anchoredPosition = new Vector2(0f, -40f);
            badgeRoot.sizeDelta = new Vector2(320f, 90f);

            // HQ Dark Panel Background
            badgeBg = UIManager.CreateImage("BadgeBg", badgeRoot, UIColors.Card);
            badgeBg.sprite = Resources.Load<Sprite>("TowerMaze/UITheme/panel_dark_hq");
            badgeBg.type = Image.Type.Sliced;
            UIManager.Stretch(badgeBg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            badgeBg.raycastTarget = false;

            // Premium Ember Icon
            iconImage = UIManager.CreateImage("WarningIcon", badgeRoot, Color.white);
            iconImage.sprite = Resources.Load<Sprite>("TowerMaze/UITheme/ember_icon_hq");
            iconImage.preserveAspect = true;
            iconImage.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            iconImage.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            iconImage.rectTransform.pivot = new Vector2(0f, 0.5f);
            iconImage.rectTransform.anchoredPosition = new Vector2(15f, 0f);
            iconImage.rectTransform.sizeDelta = new Vector2(60f, 60f);

            // Rush Title
            hurryText = UIManager.CreateText("HurryText", badgeRoot, font, 24, TextAnchor.MiddleLeft, UIColors.Warning);
            hurryText.fontStyle = FontStyle.Bold;
            hurryText.text = "HURRY UP!";
            hurryText.rectTransform.anchorMin = new Vector2(0.24f, 0.4f);
            hurryText.rectTransform.anchorMax = new Vector2(0.95f, 0.9f);
            hurryText.rectTransform.offsetMin = Vector2.zero;
            hurryText.rectTransform.offsetMax = Vector2.zero;

            // Compact Timer bar at the bottom of badge
            Image timerTrack = UIManager.CreateImage("TimerTrack", badgeRoot, new Color(0f, 0f, 0f, 0.5f));
            timerTrack.rectTransform.anchorMin = new Vector2(0.24f, 0.15f);
            timerTrack.rectTransform.anchorMax = new Vector2(0.9f, 0.25f);
            timerTrack.rectTransform.offsetMin = Vector2.zero;
            timerTrack.rectTransform.offsetMax = Vector2.zero;
            timerTrack.raycastTarget = false;

            timerFill = UIManager.CreateImage("TimerFill", timerTrack.transform, UIColors.Warning);
            timerFill.type = Image.Type.Filled;
            timerFill.fillMethod = Image.FillMethod.Horizontal;
            timerFill.fillOrigin = 0;
            timerFill.fillAmount = 1f;
            UIManager.Stretch(timerFill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            timerFill.raycastTarget = false;
        }

        public void SetState(bool rushActive, float pulse, float remainingNormalized)
        {
            float p = Mathf.Clamp01(pulse);
            
            // Badge visual state
            Color accent = rushActive ? new Color(1f, 0.9f, 0.6f, 1f) : new Color(1f, 0.65f, 0.2f, 1f);
            badgeBg.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.9f, 1f, p));
            badgeRoot.localScale = Vector3.one * (1f + p * 0.05f);
            
            timerFill.color = accent;
            timerFill.fillAmount = Mathf.Clamp01(remainingNormalized);

            hurryText.text = rushActive ? "RUSH ACTIVE!" : "HURRY UP!";
            hurryText.color = Color.Lerp(Color.white, accent, p);

            // Icon rotation pulse
            iconImage.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 8f) * 10f * p);
            iconImage.color = Color.Lerp(new Color(1f, 1f, 1f, 0.8f), Color.white, p);
        }
    }

    public sealed class ShopScreenController : MonoBehaviour
    {
        private Font runtimeFont;
        private ThemeDefinition theme;
        private Action<ShopCatalogType, string> onItemSelected;
        private Action buttonClickSound;
        private Text emberText;
        private Text adRewardButtonLabel;
        private Button coinsTabButton;
        private Button ballsTabButton;
        private Button towersTabButton;
        private Transform listRoot;
        private Text emptyStateText;
        private EconomyManager economyManager;
        private IReadOnlyList<BallSkinDefinition> ballSkins = Array.Empty<BallSkinDefinition>();
        private IReadOnlyList<TowerSkinDefinition> towerSkins = Array.Empty<TowerSkinDefinition>();
        private IReadOnlyList<CoinPackOffer> coinPackOffers = Array.Empty<CoinPackOffer>();
        private ShopCatalogType currentCategory = ShopCatalogType.Coin;
        private readonly List<Button> itemButtons = new();
        private readonly List<LayoutElement> itemLayouts = new();
        private readonly List<Text> itemPrimaryLabels = new();
        private readonly List<Text> itemSecondaryLabels = new();
        private readonly List<Image> itemBackgrounds = new();
        private readonly List<Image> itemPreviewFrames = new();
        private readonly List<Image> itemPreviewBackplates = new();
        private readonly List<RawImage> itemPreviews = new();
        private readonly List<Text> itemPreviewTexts = new();
        private readonly List<Image> itemPricePills = new();
        private readonly List<Text> itemPriceTexts = new();
        private readonly List<Image> itemBadgePills = new();
        private readonly List<Text> itemBadgeTexts = new();
        private readonly List<string> itemIds = new();
        private static Texture2D coinPackSingleTexture;
        private static Texture2D coinPackStackTexture;
        private static Texture2D coinPackBagTexture;
        private static Texture2D coinPackChestTexture;
        private static Texture2D noAdsOfferTexture;

        public void Initialize(Font font, ThemeDefinition themeDefinition, Action onClose, Action onClaimCoinBoost, Action onRestorePurchases, Action<ShopCatalogType, string> onSelectItem, Action onButtonClick = null)
        {
            runtimeFont = font;
            theme = themeDefinition;
            onItemSelected = onSelectItem;
            buttonClickSound = onButtonClick;

            Image shopOverlay = UIManager.CreateImage("ShopOverlay", transform, new Color(0f, 0f, 0f, 0.7f));
            UIManager.Stretch(shopOverlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image shopPanel = UIManager.CreateCard("ShopPanel", transform, new Color(0.06f, 0.07f, 0.09f, 0.98f), new Color(0.64f, 0.88f, 0.92f, 0.72f));
            UIManager.Stretch(shopPanel.rectTransform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);

            Image topHeaderCard = UIManager.CreateCard("HeaderCard", shopPanel.transform, UIColors.Card, UIColors.Divider);
            UIManager.Stretch(topHeaderCard.rectTransform, new Vector2(0.03f, 0.87f), new Vector2(0.97f, 0.96f), Vector2.zero, Vector2.zero);

            Text shopTitle = UIManager.CreateText("ShopTitle", topHeaderCard.transform, font, 38, TextAnchor.MiddleLeft, Color.black);
            shopTitle.fontStyle = FontStyle.Bold;
            shopTitle.text = "SHOP";
            UIManager.Stretch(shopTitle.rectTransform, new Vector2(0f, 0.22f), new Vector2(0.28f, 0.95f), new Vector2(28f, 0f), new Vector2(-16f, 0f));
            UIManager.StyleToyText(shopTitle, new Color(0.22f, 0.08f, 0.36f, 0.82f), new Vector2(1f, -1f), new Color(1f, 1f, 1f, 0.18f), new Vector2(0f, 1f));

            Image emberPill = UIManager.CreateCard("ShopEmberPill", topHeaderCard.transform, UIColors.Card, UIColors.Divider);
            UIManager.Stretch(emberPill.rectTransform, new Vector2(0.3f, 0.18f), new Vector2(0.66f, 0.82f), Vector2.zero, Vector2.zero);
            emberText = UIManager.CreateText("ShopEmber", emberPill.transform, font, 24, TextAnchor.MiddleCenter, new Color(0.18f, 0.24f, 0.18f, 1f));
            emberText.fontStyle = FontStyle.Bold;
            UIManager.Stretch(emberText.rectTransform, Vector2.zero, Vector2.one, new Vector2(16f, 0f), new Vector2(-16f, 0f));
            UIManager.StyleToyText(emberText, new Color(1f, 1f, 1f, 0.18f), new Vector2(0f, 1f), new Color(0.14f, 0.22f, 0.16f, 0.16f), new Vector2(0f, -1f));

            Button adRewardButton = UIManager.CreateButton("AdRewardButton", topHeaderCard.transform, font, "AD +COIN", UIColors.Success, Color.white);
            UIManager.Stretch((RectTransform)adRewardButton.transform, new Vector2(0.62f, 0.18f), new Vector2(0.79f, 0.82f), Vector2.zero, Vector2.zero);
            UIManager.StyleButtonLabel(adRewardButton, 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            UIManager.BindButton(adRewardButton, () => onClaimCoinBoost?.Invoke(), buttonClickSound);
            adRewardButtonLabel = adRewardButton.GetComponentInChildren<Text>();

            Button restoreButton = UIManager.CreateButton("RestoreShop", topHeaderCard.transform, font, "RESTORE", UIColors.Card, UIColors.Primary);
            UIManager.Stretch((RectTransform)restoreButton.transform, new Vector2(0.8f, 0.18f), new Vector2(0.91f, 0.82f), Vector2.zero, Vector2.zero);
            UIManager.StyleButtonLabel(restoreButton, 16, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            UIManager.BindButton(restoreButton, () => onRestorePurchases?.Invoke(), buttonClickSound);

            Button exitButton = UIManager.CreateButton("CloseShop", topHeaderCard.transform, font, "CLOSE", UIColors.Card, UIColors.Primary);
            UIManager.Stretch((RectTransform)exitButton.transform, new Vector2(0.92f, 0.18f), new Vector2(0.98f, 0.82f), Vector2.zero, Vector2.zero);
            UIManager.StyleButtonLabel(exitButton, 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            UIManager.BindButton(exitButton, () => onClose(), buttonClickSound);

            Image categoryCard = UIManager.CreateCard("TabCard", shopPanel.transform, new Color(0.24f, 0.08f, 0.08f, 0.98f), new Color(0.95f, 0.35f, 0.35f, 0.94f));
            UIManager.Stretch(categoryCard.rectTransform, new Vector2(0.03f, 0.78f), new Vector2(0.97f, 0.85f), Vector2.zero, Vector2.zero);

            coinsTabButton = UIManager.CreateButton("CoinsTab", categoryCard.transform, font, "COINS", UIColors.Primary, Color.white);
            UIManager.Stretch((RectTransform)coinsTabButton.transform, new Vector2(0.02f, 0.12f), new Vector2(0.32f, 0.88f), Vector2.zero, Vector2.zero);
            UIManager.StyleButtonLabel(coinsTabButton, 22, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            UIManager.BindButton(coinsTabButton, () => SwitchCategory(ShopCatalogType.Coin), buttonClickSound);

            ballsTabButton = UIManager.CreateButton("BallsTab", categoryCard.transform, font, "BALLS", Color.clear, UIColors.TextDim);
            UIManager.Stretch((RectTransform)ballsTabButton.transform, new Vector2(0.35f, 0.12f), new Vector2(0.65f, 0.88f), Vector2.zero, Vector2.zero);
            UIManager.StyleButtonLabel(ballsTabButton, 24, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            UIManager.BindButton(ballsTabButton, () => SwitchCategory(ShopCatalogType.Ball), buttonClickSound);

            towersTabButton = UIManager.CreateButton("TowersTab", categoryCard.transform, font, "TOWERS", Color.clear, UIColors.TextDim);
            UIManager.Stretch((RectTransform)towersTabButton.transform, new Vector2(0.68f, 0.12f), new Vector2(0.98f, 0.88f), Vector2.zero, Vector2.zero);
            UIManager.StyleButtonLabel(towersTabButton, 24, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            UIManager.BindButton(towersTabButton, () => SwitchCategory(ShopCatalogType.Tower), buttonClickSound);

            Image itemListCard = UIManager.CreateCard("ListCard", shopPanel.transform, new Color(0.12f, 0.08f, 0.18f, 0.98f), new Color(0.65f, 0.45f, 0.95f, 0.92f));
            UIManager.Stretch(itemListCard.rectTransform, new Vector2(0.03f, 0.08f), new Vector2(0.97f, 0.76f), Vector2.zero, Vector2.zero);

            GameObject shopViewportObject = new("Viewport");
            shopViewportObject.transform.SetParent(itemListCard.transform, false);
            RectTransform shopViewportRect = shopViewportObject.AddComponent<RectTransform>();
            UIManager.Stretch(shopViewportRect, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.96f), Vector2.zero, Vector2.zero);
            Image shopViewportImage = shopViewportObject.AddComponent<Image>();
            shopViewportImage.color = new Color(0f, 0f, 0f, 0.22f);
            Mask shopViewportMask = shopViewportObject.AddComponent<Mask>();
            shopViewportMask.showMaskGraphic = false;

            GameObject shopScrollObject = new("ScrollView");
            shopScrollObject.transform.SetParent(shopViewportObject.transform, false);
            RectTransform shopScrollRectTransform = shopScrollObject.AddComponent<RectTransform>();
            UIManager.Stretch(shopScrollRectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ScrollRect shopScrollRect = shopScrollObject.AddComponent<ScrollRect>();
            shopScrollRect.horizontal = false;
            shopScrollRect.vertical = true;
            shopScrollRect.movementType = ScrollRect.MovementType.Clamped;
            shopScrollRect.scrollSensitivity = 32f;
            shopScrollRect.viewport = shopViewportRect;

            GameObject shopListObject = new("ListRoot");
            shopListObject.transform.SetParent(shopScrollObject.transform, false);
            RectTransform shopListRect = shopListObject.AddComponent<RectTransform>();
            UIManager.Stretch(shopListRect, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            shopListRect.pivot = new Vector2(0.5f, 1f);
            VerticalLayoutGroup shopLayoutGroup = shopListObject.AddComponent<VerticalLayoutGroup>();
            shopLayoutGroup.spacing = 12f;
            shopLayoutGroup.childAlignment = TextAnchor.UpperCenter;
            shopLayoutGroup.childControlWidth = true;
            shopLayoutGroup.childControlHeight = false;
            shopLayoutGroup.childForceExpandWidth = true;
            shopLayoutGroup.childForceExpandHeight = false;
            shopLayoutGroup.padding = new RectOffset(0, 0, 0, 0);
            ContentSizeFitter shopFitter = shopListObject.AddComponent<ContentSizeFitter>();
            shopFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            shopFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            shopScrollRect.content = shopListRect;
            listRoot = shopListObject.transform;

            emptyStateText = UIManager.CreateText("EmptyState", shopViewportObject.transform, font, 24, TextAnchor.MiddleCenter, new Color(0.2f, 0.18f, 0.42f, 0.92f));
            emptyStateText.text = "No skins available.";
            UIManager.Stretch(emptyStateText.rectTransform, new Vector2(0.15f, 0.35f), new Vector2(0.85f, 0.65f), Vector2.zero, Vector2.zero);
            UIManager.StyleToyText(emptyStateText, new Color(1f, 1f, 1f, 0.18f), new Vector2(0f, 1f), new Color(0.14f, 0.2f, 0.38f, 0.16f), new Vector2(0f, -1f));
            emptyStateText.gameObject.SetActive(false);
        }

        public void SetState(int emberBalance, IReadOnlyList<BallSkinDefinition> skins, IReadOnlyList<TowerSkinDefinition> towerSkinList, IReadOnlyList<CoinPackOffer> coinOffers, EconomyManager activeEconomyManager)
        {
            economyManager = activeEconomyManager;
            ballSkins = skins ?? Array.Empty<BallSkinDefinition>();
            towerSkins = towerSkinList ?? Array.Empty<TowerSkinDefinition>();
            coinPackOffers = coinOffers ?? Array.Empty<CoinPackOffer>();
            emberText.text = $"COIN  {emberBalance}";
            if (adRewardButtonLabel != null && economyManager != null)
            {
                adRewardButtonLabel.text = $"AD +{economyManager.GetShopCoinBoostReward()} COIN";
            }
            RefreshVisibleItems();
        }

        private void SwitchCategory(ShopCatalogType category)
        {
            currentCategory = category;
            RefreshVisibleItems();
        }

        private void RefreshVisibleItems()
        {
            UpdateTabState();
            if (currentCategory != ShopCatalogType.Coin && economyManager == null)
            {
                return;
            }

            int visibleCount = currentCategory switch
            {
                ShopCatalogType.Coin => coinPackOffers.Count,
                ShopCatalogType.Ball => ballSkins.Count,
                _ => towerSkins.Count
            };

            emptyStateText.text = currentCategory == ShopCatalogType.Coin
                ? "Coin packs are unavailable."
                : "No skins available.";
            emptyStateText.gameObject.SetActive(visibleCount == 0);
            EnsureItemButtons(visibleCount);

            if (currentCategory == ShopCatalogType.Ball)
            {
                var sortedSkins = ballSkins.OrderBy(s =>
                {
                    bool owned = economyManager.IsOwnedSkin(s.id);
                    bool isIap = !string.IsNullOrEmpty(s.iapProductId);
                    if (owned) return 0;
                    return isIap ? 2 : 1;
                }).ThenBy(s => s.priceEmber).ToList();

                for (int index = 0; index < visibleCount; index++)
                {
                    BallSkinDefinition skin = sortedSkins[index];
                    bool owned = economyManager.IsOwnedSkin(skin.id);
                    bool equipped = economyManager.EquippedSkinId == skin.id;
                    ConfigureCatalogItem(
                        index,
                        skin.id,
                        owned ? new Color(1.0f, 0.98f, 0.9f, 0.98f) : new Color(0.65f, 0.45f, 0.95f, 0.98f),
                        skin.baseColor,
                        BallSkinTextureLibrary.LoadTexture(skin.baseMapResourcePath),
                        skin.textureScale,
                        BuildCatalogTitle(skin.displayName),
                        BuildCatalogDetail(skin.priceEmber, owned, equipped, skin.iapProductId));
                    itemButtons[index].interactable = owned || (!string.IsNullOrWhiteSpace(skin.iapProductId) ? true : economyManager.EmberBalance >= skin.priceEmber);
                    itemButtons[index].gameObject.SetActive(true);
                }
            }
            else if (currentCategory == ShopCatalogType.Tower)
            {
                var sortedSkins = towerSkins.OrderBy(s =>
                {
                    bool owned = economyManager.IsOwnedTowerSkin(s.id);
                    bool isIap = !string.IsNullOrEmpty(s.iapProductId);
                    if (owned) return 0;
                    return isIap ? 2 : 1;
                }).ThenBy(s => s.priceEmber).ToList();

                for (int index = 0; index < visibleCount; index++)
                {
                    TowerSkinDefinition towerSkin = sortedSkins[index];
                    bool owned = economyManager.IsOwnedTowerSkin(towerSkin.id);
                    bool equipped = economyManager.EquippedTowerSkinId == towerSkin.id;
                    ConfigureCatalogItem(
                        index,
                        towerSkin.id,
                        owned ? new Color(1.0f, 0.98f, 0.9f, 0.98f) : new Color(0.65f, 0.45f, 0.95f, 0.98f),
                        towerSkin.mainPathTint,
                        ResolveTowerPreviewTexture(towerSkin),
                        towerSkin.wallTextureScale,
                        BuildCatalogTitle(towerSkin.displayName),
                        BuildCatalogDetail(towerSkin.priceEmber, owned, equipped, towerSkin.iapProductId));
                    itemButtons[index].interactable = owned || !string.IsNullOrWhiteSpace(towerSkin.iapProductId) || economyManager.EmberBalance >= towerSkin.priceEmber;
                    itemButtons[index].gameObject.SetActive(true);
                }
            }
            else // Coin packs
            {
                for (int index = 0; index < visibleCount; index++)
                {
                    ConfigureCoinPackItem(index, coinPackOffers[index]);
                    itemButtons[index].interactable = coinPackOffers[index].productType == ProductType.Consumable || !coinPackOffers[index].owned;
                    itemButtons[index].gameObject.SetActive(true);
                }
            }

            for (int index = visibleCount; index < itemButtons.Count; index++)
            {
                itemButtons[index].gameObject.SetActive(false);
            }
        }

        private void ConfigureCatalogItem(int index, string itemId, Color backgroundColor, Color accentColor, Texture previewTexture, Vector2 previewScale, string title, string detail)
        {
            itemIds[index] = itemId;
            SetItemHeight(index, 116f);
            UIManager.ApplyCardSurface(itemBackgrounds[index], backgroundColor);
            // ApplyCardSurface routes purple hues (0.68-0.9) to panel_purple sprite.
            // For cream/low-saturation colors it uses panel_light (cool white) — apply a warm tint.
            Color.RGBToHSV(backgroundColor, out _, out float bgSat, out _);
            itemBackgrounds[index].color = bgSat < 0.15f
                ? new Color(1f, 0.93f, 0.78f, 1f)   // warm cream tint over panel_light
                : Color.white;                        // purple: panel_purple sprite handles color
            UIManager.ApplyCardSurface(itemPreviewFrames[index], new Color(accentColor.r, accentColor.g, accentColor.b, 0.94f));
            ApplyDefaultPreviewLayout(index);
            itemPreviews[index].texture = previewTexture != null ? previewTexture : Texture2D.whiteTexture;
            itemPreviews[index].color = previewTexture != null ? Color.white : accentColor;
            itemPreviews[index].uvRect = new Rect(0f, 0f, Mathf.Max(1f, previewScale.x), Mathf.Max(1f, previewScale.y));
            itemPreviewTexts[index].text = previewTexture == null ? "SKIN" : string.Empty;
            itemPreviewTexts[index].gameObject.SetActive(previewTexture == null);
            itemPrimaryLabels[index].text = title;
            itemPrimaryLabels[index].fontSize = 20;
            itemPrimaryLabels[index].color = bgSat < 0.15f
                ? new Color(0.22f, 0.12f, 0.04f, 1f)    // cream bg → dark warm brown
                : new Color(0.96f, 0.92f, 1f, 1f);       // purple bg → near-white lavender
            itemSecondaryLabels[index].text = detail;
            itemSecondaryLabels[index].fontSize = 16;
            itemSecondaryLabels[index].color = bgSat < 0.15f
                ? new Color(0.38f, 0.24f, 0.08f, 1f)    // cream bg → medium warm brown
                : new Color(0.82f, 0.76f, 1f, 1f);       // purple bg → soft lavender
            itemSecondaryLabels[index].gameObject.SetActive(true);
            itemPreviewBackplates[index].gameObject.SetActive(false);
            itemPricePills[index].gameObject.SetActive(false);
            itemBadgePills[index].gameObject.SetActive(false);
        }

        private void ConfigureCoinPackItem(int index, CoinPackOffer offer)
        {
            itemIds[index] = offer.id;
            SetItemHeight(index, GetCoinOfferHeight(offer));
            Texture2D previewTexture = GetCoinPackPreviewTexture(offer);

            Color cardColor = GetCoinOfferCardColor(offer);
            UIManager.ApplyButtonSurface(itemBackgrounds[index], cardColor);
            UIManager.ApplyCardSurface(itemPreviewFrames[index], GetCoinOfferPreviewFrameColor(offer));
            bool showNoAdsBackplate = offer.kind == StoreOfferKind.NoAds;
            itemPreviewBackplates[index].gameObject.SetActive(showNoAdsBackplate);
            if (showNoAdsBackplate)
            {
                UIManager.ApplyCardSurface(itemPreviewBackplates[index], new Color(0.56f, 0.98f, 0.62f, 0.98f));
            }
            ApplyPreviewTextureLayout(index, previewTexture);
            itemPreviews[index].texture = previewTexture != null ? previewTexture : Texture2D.whiteTexture;
            itemPreviews[index].color = previewTexture != null ? GetCoinOfferPreviewTint(offer) : new Color(0.18f, 0.22f, 0.32f, 1f);
            itemPreviews[index].uvRect = new Rect(0f, 0f, 1f, 1f);
            itemPreviewTexts[index].text = GetCoinOfferPreviewFallbackText(offer);
            itemPreviewTexts[index].gameObject.SetActive(previewTexture == null);

            itemPrimaryLabels[index].text = GetCoinOfferPrimaryText(offer);
            itemPrimaryLabels[index].fontSize = GetCoinOfferPrimaryFontSize(offer);
            itemPrimaryLabels[index].color = Color.black;

            string detail = GetCoinOfferDetail(offer);
            itemSecondaryLabels[index].text = detail;
            itemSecondaryLabels[index].fontSize = offer.kind == StoreOfferKind.WelcomePack ? 14 : 15;
            itemSecondaryLabels[index].color = new Color(0.28f, 0.24f, 0.12f, 1f);
            itemSecondaryLabels[index].gameObject.SetActive(true);

            UIManager.ApplyButtonSurface(itemPricePills[index], offer.owned ? UIColors.Card : UIColors.Primary);
            itemPriceTexts[index].text = offer.owned ? "OWNED" : offer.priceLabel;
            itemPriceTexts[index].fontSize = offer.featured ? 24 : 22;
            itemPriceTexts[index].color = offer.owned ? UIColors.TextDim : Color.white;
            itemPricePills[index].gameObject.SetActive(true);

            bool showBadge = !string.IsNullOrWhiteSpace(offer.badgeLabel);
            itemBadgePills[index].gameObject.SetActive(showBadge);
            if (showBadge)
            {
                UIManager.ApplyButtonSurface(
                    itemBadgePills[index],
                    UIColors.SuccessBg);
                itemBadgeTexts[index].text = offer.badgeLabel;
                itemBadgeTexts[index].color = UIColors.SuccessText;
                itemBadgeTexts[index].fontSize = offer.kind == StoreOfferKind.NoAds ? 14 : 13;
            }
        }

        private Texture ResolveTowerPreviewTexture(TowerSkinDefinition towerSkin)
        {
            Texture preview = BallSkinTextureLibrary.LoadTexture(towerSkin.wallBaseMapResourcePath);
            return preview != null ? preview : theme.towerWallBaseMap;
        }


        private void UpdateTabState()
        {
            bool coinActive = currentCategory == ShopCatalogType.Coin;
            bool ballActive = currentCategory == ShopCatalogType.Ball;
            bool towerActive = currentCategory == ShopCatalogType.Tower;

            UIManager.ApplyButtonSurface(coinsTabButton.GetComponent<Image>(), coinActive ? UIColors.Primary : Color.clear);
            UIManager.ApplyButtonSurface(ballsTabButton.GetComponent<Image>(), ballActive ? UIColors.Primary : Color.clear);
            UIManager.ApplyButtonSurface(towersTabButton.GetComponent<Image>(), towerActive ? UIColors.Primary : Color.clear);

            coinsTabButton.GetComponentInChildren<Text>().color = coinActive ? Color.white : UIColors.TextDim;
            ballsTabButton.GetComponentInChildren<Text>().color = ballActive ? Color.white : UIColors.TextDim;
            towersTabButton.GetComponentInChildren<Text>().color = towerActive ? Color.white : UIColors.TextDim;
        }

        private void EnsureItemButtons(int count)
        {
            while (itemButtons.Count < count)
            {
                int buttonIndex = itemButtons.Count;
                Button button = UIManager.CreateButton($"ShopItem_{buttonIndex}", listRoot, runtimeFont, "ITEM", UIColors.Card, UIColors.TextDark);
                RectTransform buttonRect = (RectTransform)button.transform;
                buttonRect.anchorMin = new Vector2(0f, 1f);
                buttonRect.anchorMax = new Vector2(1f, 1f);
                buttonRect.pivot = new Vector2(0.5f, 1f);
                buttonRect.sizeDelta = new Vector2(0f, 122f);
                LayoutElement layoutElement = button.gameObject.AddComponent<LayoutElement>();
                layoutElement.minHeight = 122f;
                layoutElement.preferredHeight = 122f;

                Text primaryLabel = button.GetComponentInChildren<Text>();
                primaryLabel.fontSize = 20;
                primaryLabel.fontStyle = FontStyle.Bold;
                primaryLabel.alignment = TextAnchor.MiddleLeft;
                UIManager.Stretch(primaryLabel.rectTransform, new Vector2(0f, 0.46f), new Vector2(0.7f, 0.9f), new Vector2(136f, 0f), new Vector2(-18f, -4f));

                Image previewFrame = UIManager.CreateCard("PreviewFrame", button.transform, new Color(0.25f, 0.84f, 0.78f, 0.98f), new Color(1f, 0.95f, 0.86f, 0.74f));
                previewFrame.raycastTarget = false;
                UIManager.Stretch(previewFrame.rectTransform, new Vector2(0f, 0.12f), new Vector2(0f, 0.88f), new Vector2(16f, 0f), new Vector2(108f, 0f));

                Image previewBackplate = UIManager.CreateCard("PreviewBackplate", previewFrame.transform, new Color(0.56f, 0.98f, 0.62f, 0.98f), new Color(0.86f, 1f, 0.88f, 0.92f));
                previewBackplate.raycastTarget = false;
                UIManager.Stretch(previewBackplate.rectTransform, Vector2.zero, Vector2.one, new Vector2(16f, 16f), new Vector2(-16f, -16f));
                previewBackplate.gameObject.SetActive(false);

                GameObject previewObject = new("Preview");
                previewObject.transform.SetParent(previewFrame.transform, false);
                RawImage preview = previewObject.AddComponent<RawImage>();
                preview.raycastTarget = false;
                UIManager.Stretch((RectTransform)preview.transform, Vector2.zero, Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -10f));

                Text previewText = UIManager.CreateText("PreviewText", previewFrame.transform, runtimeFont, 18, TextAnchor.MiddleCenter, Color.black);
                previewText.fontStyle = FontStyle.Bold;
                UIManager.Stretch(previewText.rectTransform, Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-6f, -6f));
                UIManager.StyleToyText(previewText, new Color(1f, 1f, 1f, 0.25f), new Vector2(0f, 1f), new Color(0f, 0f, 0f, 0.12f), new Vector2(0f, -1f));
                previewText.gameObject.SetActive(false);

                Text secondaryLabel = UIManager.CreateText("SecondaryLabel", button.transform, runtimeFont, 15, TextAnchor.UpperLeft, UIColors.TextDim);
                UIManager.Stretch(secondaryLabel.rectTransform, new Vector2(0f, 0.08f), new Vector2(0.7f, 0.48f), new Vector2(136f, 6f), new Vector2(-18f, -8f));
                UIManager.StyleToyText(secondaryLabel, new Color(1f, 1f, 1f, 0.16f), new Vector2(0f, 1f), new Color(0f, 0f, 0f, 0.08f), new Vector2(0f, -1f));

                Image pricePill = UIManager.CreateImage("PricePill", button.transform, Color.white);
                UIManager.ApplyButtonSurface(pricePill, UIColors.Primary);
                pricePill.raycastTarget = false;
                UIManager.Stretch(pricePill.rectTransform, new Vector2(0.72f, 0.2f), new Vector2(0.98f, 0.8f), Vector2.zero, Vector2.zero);
                Text priceText = UIManager.CreateText("PriceText", pricePill.transform, runtimeFont, 22, TextAnchor.MiddleCenter, Color.white);
                priceText.fontStyle = FontStyle.Bold;
                UIManager.Stretch(priceText.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));
                UIManager.StyleToyText(priceText, new Color(0f, 0f, 0f, 0.14f), new Vector2(0f, -1f), new Color(1f, 1f, 1f, 0.16f), new Vector2(0f, 1f));
                pricePill.gameObject.SetActive(false);

                Image badgePill = UIManager.CreateImage("BadgePill", button.transform, Color.white);
                UIManager.ApplyButtonSurface(badgePill, UIColors.SuccessBg);
                badgePill.raycastTarget = false;
                UIManager.Stretch(badgePill.rectTransform, new Vector2(0f, 0.78f), new Vector2(0.24f, 0.98f), new Vector2(12f, 0f), new Vector2(0f, 0f));
                Text badgeText = UIManager.CreateText("BadgeText", badgePill.transform, runtimeFont, 13, TextAnchor.MiddleCenter, UIColors.SuccessText);
                badgeText.fontStyle = FontStyle.Bold;
                UIManager.Stretch(badgeText.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));
                UIManager.StyleToyText(badgeText, new Color(1f, 1f, 1f, 0.14f), new Vector2(0f, 1f), new Color(0f, 0f, 0f, 0.08f), new Vector2(0f, -1f));
                badgePill.gameObject.SetActive(false);

                int capturedIndex = buttonIndex;
                UIManager.BindButton(button, () => onItemSelected?.Invoke(currentCategory, itemIds[capturedIndex]), buttonClickSound);
                itemButtons.Add(button);
                itemLayouts.Add(layoutElement);
                itemPrimaryLabels.Add(primaryLabel);
                itemSecondaryLabels.Add(secondaryLabel);
                itemBackgrounds.Add(button.GetComponent<Image>());
                itemPreviewFrames.Add(previewFrame);
                itemPreviewBackplates.Add(previewBackplate);
                itemPreviews.Add(preview);
                itemPreviewTexts.Add(previewText);
                itemPricePills.Add(pricePill);
                itemPriceTexts.Add(priceText);
                itemBadgePills.Add(badgePill);
                itemBadgeTexts.Add(badgeText);
                itemIds.Add(string.Empty);
            }
        }

        private void SetItemHeight(int index, float height)
        {
            if (index < 0 || index >= itemLayouts.Count)
            {
                return;
            }

            itemLayouts[index].minHeight = height;
            itemLayouts[index].preferredHeight = height;
            ((RectTransform)itemButtons[index].transform).sizeDelta = new Vector2(0f, height);
        }

        private static string BuildCatalogTitle(string displayName)
        {
            return UiTextFormatter.Truncate(displayName.ToUpperInvariant(), 20);
        }

        private static string BuildCatalogDetail(int priceCoin, bool owned, bool equipped, string iapProductId = "")
        {
            if (equipped)
            {
                return "SELECTED";
            }

            if (owned)
            {
                return "TAP TO EQUIP";
            }

            if (!string.IsNullOrWhiteSpace(iapProductId))
            {
                return "TRY 2.500";
            }

            return $"{priceCoin} COIN";
        }

        private static string FormatCoinAmount(int amount)
        {
            string digits = Mathf.Max(amount, 0).ToString();
            if (digits.Length <= 3)
            {
                return digits;
            }

            StringBuilder builder = new();
            int digitsInGroup = 0;
            for (int index = digits.Length - 1; index >= 0; index--)
            {
                if (digitsInGroup == 3)
                {
                    builder.Insert(0, ' ');
                    digitsInGroup = 0;
                }

                builder.Insert(0, digits[index]);
                digitsInGroup++;
            }

            return builder.ToString();
        }

        internal static Texture2D GetCoinPackPreviewTexture(CoinPackOffer offer)
        {
            if (offer.kind == StoreOfferKind.NoAds)
            {
                return LoadCoinPackTexture(ref noAdsOfferTexture, "TowerMaze/ShopIcons/no_ads_icon");
            }

            if (offer.kind == StoreOfferKind.WelcomePack)
            {
                return LoadCoinPackTexture(ref coinPackChestTexture, "TowerMaze/CoinArt/HQ/coin_hq_chest_gold");
            }

            if (offer.kind == StoreOfferKind.ExclusiveBundle)
            {
                return LoadCoinPackTexture(ref coinPackBagTexture, "TowerMaze/CoinArt/HQ/coin_hq_bag");
            }

            if (offer.featured || offer.coinAmount >= 100000)
            {
                return LoadCoinPackTexture(ref coinPackChestTexture, "TowerMaze/CoinArt/HQ/coin_hq_chest_gold");
            }

            if (offer.coinAmount >= 25000)
            {
                return LoadCoinPackTexture(ref coinPackBagTexture, "TowerMaze/CoinArt/HQ/coin_hq_bag");
            }

            if (offer.coinAmount >= 5000)
            {
                return LoadCoinPackTexture(ref coinPackStackTexture, "TowerMaze/CoinArt/HQ/coin_hq_stack");
            }

            return LoadCoinPackTexture(ref coinPackSingleTexture, "TowerMaze/CoinArt/HQ/coin_hq_single");
        }

        private static float GetCoinOfferHeight(CoinPackOffer offer)
        {
            return offer.kind switch
            {
                StoreOfferKind.WelcomePack => 156f,
                StoreOfferKind.NoAds => 106f,
                StoreOfferKind.ExclusiveBundle => 132f,
                _ => offer.featured ? 156f : 122f,
            };
        }

        internal static Color GetCoinOfferCardColor(CoinPackOffer offer)
        {
            return offer.kind switch
            {
                StoreOfferKind.WelcomePack => new Color(0.99f, 0.92f, 0.7f, 0.99f),
                StoreOfferKind.NoAds => new Color(0.92f, 0.98f, 0.92f, 0.99f),
                StoreOfferKind.ExclusiveBundle => new Color(0.95f, 0.9f, 0.98f, 0.99f),
                _ => offer.featured
                    ? new Color(0.98f, 0.93f, 0.7f, 0.98f)
                    : new Color(0.97f, 0.95f, 0.84f, 0.98f),
            };
        }

        internal static Color GetCoinOfferPreviewFrameColor(CoinPackOffer offer)
        {
            return offer.kind switch
            {
                StoreOfferKind.NoAds => new Color(1f, 0.78f, 0.78f, 0.98f),
                StoreOfferKind.ExclusiveBundle => new Color(0.78f, 0.5f, 1f, 0.98f),
                _ => UIColors.Warning,
            };
        }

        internal static Color GetCoinOfferPreviewTint(CoinPackOffer offer)
        {
            return offer.kind switch
            {
                StoreOfferKind.NoAds => new Color(0.92f, 0.18f, 0.18f, 1f),
                _ => Color.white,
            };
        }

        private static string GetCoinOfferPreviewFallbackText(CoinPackOffer offer)
        {
            return offer.kind switch
            {
                StoreOfferKind.NoAds => "NO\nADS",
                StoreOfferKind.WelcomePack => "VIP",
                StoreOfferKind.ExclusiveBundle => "BUNDLE",
                _ => offer.featured ? "COIN\nMEGA" : "COIN",
            };
        }

        private static string GetCoinOfferPrimaryText(CoinPackOffer offer)
        {
            return offer.kind switch
            {
                StoreOfferKind.WelcomePack => "WELCOME PACK",
                StoreOfferKind.NoAds => "NO ADS",
                StoreOfferKind.ExclusiveBundle => offer.displayName,
                _ => $"{FormatCoinAmount(offer.coinAmount)} COIN",
            };
        }

        private static int GetCoinOfferPrimaryFontSize(CoinPackOffer offer)
        {
            return offer.kind switch
            {
                StoreOfferKind.WelcomePack => 26,
                StoreOfferKind.NoAds => 24,
                StoreOfferKind.ExclusiveBundle => 21,
                _ => offer.featured ? 28 : 24,
            };
        }

        private static string GetCoinOfferDetail(CoinPackOffer offer)
        {
            return offer.kind switch
            {
                StoreOfferKind.WelcomePack => $"+{offer.coinAmount} COIN\n{offer.bonusLabel}",
                StoreOfferKind.NoAds => "REMOVE POPUP ADS FOREVER\nKEEP OPTIONAL REWARD ADS",
                StoreOfferKind.ExclusiveBundle => $"+{offer.coinAmount} COIN\n{offer.bonusLabel}",
                _ => string.IsNullOrWhiteSpace(offer.bonusLabel)
                    ? offer.displayName
                    : $"{offer.displayName}\n{offer.bonusLabel}",
            };
        }

        private static Texture2D LoadCoinPackTexture(ref Texture2D cachedTexture, string resourcePath)
        {
            if (cachedTexture == null)
            {
                cachedTexture = Resources.Load<Texture2D>(resourcePath);
            }

            return cachedTexture;
        }

        private void ApplyPreviewTextureLayout(int index, Texture previewTexture)
        {
            RectTransform previewRect = (RectTransform)itemPreviews[index].transform;
            if (previewTexture == null)
            {
                ApplyDefaultPreviewLayout(index);
                return;
            }

            float aspect = Mathf.Max(0.01f, previewTexture.width / (float)Mathf.Max(1, previewTexture.height));
            const float inset = 8f;
            const float baseFit = 72f;
            if (aspect >= 1f)
            {
                float scaledHeight = baseFit / aspect;
                float extraPadding = Mathf.Clamp((baseFit - scaledHeight) * 0.5f, 0f, 22f);
                UIManager.Stretch(previewRect, Vector2.zero, Vector2.one, new Vector2(inset, inset + extraPadding), new Vector2(-inset, -inset - extraPadding));
                return;
            }

            float scaledWidth = baseFit * aspect;
            float extraSidePadding = Mathf.Clamp((baseFit - scaledWidth) * 0.5f, 0f, 18f);
            UIManager.Stretch(previewRect, Vector2.zero, Vector2.one, new Vector2(inset + extraSidePadding, inset), new Vector2(-inset - extraSidePadding, -inset));
        }

        private void ApplyDefaultPreviewLayout(int index)
        {
            RectTransform previewRect = (RectTransform)itemPreviews[index].transform;
            UIManager.Stretch(previewRect, Vector2.zero, Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -10f));
        }
    }

    public sealed class StartScreenController : MonoBehaviour
    {
        private EconomyManager economyManager;
        private Action buttonClickSound;
        private Text bestScoreText;
        private Text emberText;
        private Text lifeIconText;
        private Text lifeInfoText;
        private Text chestInfoText;
        private Text missionText;
        private ScrollRect missionScroll;
        private Text challengeInfoText;
        private GameObject settingsPanel;
        private Text settingsSoundLabel;
        private Image settingsSoundBg;
        private Text settingsVibLabel;
        private Image settingsVibBg;
        private Button settingsLangTRBtn;
        private Button settingsLangENBtn;
        private Button settingsLangESBtn;
        private Text settingsTitleText;
        private Text soundRowLabelText;
        private Text vibeRowLabelText;
        private Text langRowLabelText;
        private Text closeBtnLabelText;
        private Text playBtnLabel;
        private Text missionTitleLabel;
        private Text shopButtonLabel;
        private Text challengeTitleLabel;
        private Text chestButtonLabel;
        private Text challengeButtonLabel;
        private Text rerollButtonLabel;
        private Button chestButton;
        private Button challengeButton;
        private Button rerollButton;
        private LeaderboardPanelController leaderboardPanel;
        private float nextLifeRefreshTime;

        public void Initialize(Font font, ThemeDefinition theme, EconomyManager economy, Action onPlay, Action onPlayDailyChallenge, Action onOpenShop, Action onClaimChest, Action onToggleSound, Action onToggleVibration, Action onRerollMissions, Action onButtonClick = null)
        {
            economyManager = economy;
            buttonClickSound = onButtonClick;
            Color lightCard = UIColors.Card;
            Color lightOutline = UIColors.Divider;
            Color lightText = UIColors.Primary;
            Color yellowCard = UIColors.Card;
            Color yellowOutline = UIColors.Divider;
            Color yellowText = UIColors.TextDark;
            Color subtleHighlight = new Color(1f, 1f, 1f, 0.16f);
            Color subtleShadow = new Color(0.05f, 0.07f, 0.12f, 0.2f);

            Image background = UIManager.CreateImage("StartBackdrop", transform, UIColors.Surface);
            UIManager.Stretch(background.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Button shopButton = UIManager.CreateButton("ShopButton", transform, font, "SHOP", UIColors.Card, UIColors.Primary);
            UIManager.Stretch((RectTransform)shopButton.transform, new Vector2(0.35f, 0.495f), new Vector2(0.65f, 0.555f), Vector2.zero, Vector2.zero);
            UIManager.StyleButtonLabel(shopButton, 24, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            UIManager.BindButton(shopButton, () => onOpenShop(), buttonClickSound);
            shopButtonLabel = shopButton.GetComponentInChildren<Text>();

            Image emberPill = UIManager.CreateCard("EmberPill", transform, lightCard, lightOutline);
            UIManager.Stretch(emberPill.rectTransform, new Vector2(0.25f, 0.92f), new Vector2(0.51f, 0.975f), Vector2.zero, Vector2.zero);
            emberText = UIManager.CreateText("EmberText", emberPill.transform, font, 24, TextAnchor.MiddleCenter, lightText);
            emberText.fontStyle = FontStyle.Bold;
            UIManager.Stretch(emberText.rectTransform, Vector2.zero, Vector2.one, new Vector2(20f, 0f), new Vector2(-20f, 0f));
            UIManager.StyleToyText(emberText, subtleHighlight, new Vector2(0f, 1f), subtleShadow, new Vector2(0f, -1f));

            Image bestPill = UIManager.CreateCard("BestPill", transform, yellowCard, yellowOutline);
            UIManager.Stretch(bestPill.rectTransform, new Vector2(0.53f, 0.92f), new Vector2(0.79f, 0.975f), Vector2.zero, Vector2.zero);
            bestScoreText = UIManager.CreateText("BestScore", bestPill.transform, font, 24, TextAnchor.MiddleCenter, yellowText);
            bestScoreText.fontStyle = FontStyle.Bold;
            UIManager.Stretch(bestScoreText.rectTransform, Vector2.zero, Vector2.one, new Vector2(20f, 0f), new Vector2(-20f, 0f));
            UIManager.StyleToyText(bestScoreText, subtleHighlight, new Vector2(0f, 1f), subtleShadow, new Vector2(0f, -1f));

            Color activeToggle = UIColors.Success;
            Color inactiveToggle = new Color(0.3f, 0.34f, 0.42f, 0.9f);

            // Settings icon button (premium card style)
            GameObject gearBtnObj = new("GearButton");
            gearBtnObj.transform.SetParent(transform, false);
            RectTransform gearBtnRect = gearBtnObj.AddComponent<RectTransform>();
            UIManager.Stretch(gearBtnRect, new Vector2(0.85f, 0.925f), new Vector2(0.96f, 0.97f), Vector2.zero, Vector2.zero);
            
            Image gearBtnBg = UIManager.CreateCard("GearBg", gearBtnObj.transform, new Color(0.12f, 0.12f, 0.12f, 0.95f), lightOutline);
            UIManager.Stretch(gearBtnBg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Button gearButton = gearBtnObj.AddComponent<Button>();
            ColorBlock gearCb = gearButton.colors;
            gearCb.normalColor = Color.white;
            gearCb.highlightedColor = new Color(0.85f, 0.92f, 1f, 1f);
            gearCb.pressedColor = new Color(0.7f, 0.8f, 1f, 1f);
            gearButton.colors = gearCb;
            gearButton.targetGraphic = gearBtnBg;

            GameObject gearIconObj = new("GearIcon");
            gearIconObj.transform.SetParent(gearBtnObj.transform, false);
            RectTransform gearIconRect = gearIconObj.AddComponent<RectTransform>();
            UIManager.Stretch(gearIconRect, Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-6f, -6f));
            Image gearIconImg = gearIconObj.AddComponent<Image>();
            
            const string gearPath = "TowerMaze/UITheme/settings_icon_premium";
            Sprite gearSprite = Resources.Load<Sprite>(gearPath);
            if (gearSprite == null)
            {
                // Fallback: Texture2D load (robust against import settings)
                Texture2D tex = Resources.Load<Texture2D>(gearPath);
                if (tex != null)
                {
                    gearSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }

            if (gearSprite != null)
            {
                gearIconImg.sprite = gearSprite;
                gearIconImg.preserveAspect = true;
                gearIconImg.color = Color.white;
            }
            else
            {
                gearIconImg.color = Color.clear;
            }
            gearIconImg.raycastTarget = false;

            Image missionRibbon = UIManager.CreateCard("MissionRibbon", transform, lightCard, lightOutline);
            UIManager.Stretch(missionRibbon.rectTransform, new Vector2(0.17f, 0.04f), new Vector2(0.83f, 0.14f), Vector2.zero, Vector2.zero);
            missionTitleLabel = UIManager.CreateText("MissionTitle", missionRibbon.transform, font, 18, TextAnchor.MiddleCenter, lightText);
            missionTitleLabel.text = "MISSIONS";
            missionTitleLabel.fontStyle = FontStyle.Bold;
            UIManager.Stretch(missionTitleLabel.rectTransform, new Vector2(0f, 0.58f), new Vector2(0.42f, 1f), new Vector2(12f, 0f), new Vector2(-12f, 0f));
            UIManager.StyleToyText(missionTitleLabel, subtleHighlight, new Vector2(0f, 1f), subtleShadow, new Vector2(0f, -1f));

            // Viewport — clips the scrollable content to the ribbon body
            GameObject missionViewportObj = new("MissionViewport");
            missionViewportObj.transform.SetParent(missionRibbon.transform, false);
            RectTransform missionViewportRect = missionViewportObj.AddComponent<RectTransform>();
            UIManager.Stretch(missionViewportRect, new Vector2(0f, 0f), new Vector2(0.72f, 0.64f), new Vector2(18f, 4f), new Vector2(-10f, -4f)); // Note: offsetMin.y is 4f (was 8f on the old Text) — intentional, gives scroll area slightly more vertical room
            Image missionViewportImage = missionViewportObj.AddComponent<Image>();
            missionViewportImage.color = Color.clear;
            Mask missionMask = missionViewportObj.AddComponent<Mask>();
            missionMask.showMaskGraphic = false;

            // ScrollRect — handles vertical scrolling
            GameObject missionScrollObj = new("MissionScroll");
            missionScrollObj.transform.SetParent(missionViewportObj.transform, false);
            RectTransform missionScrollRectTransform = missionScrollObj.AddComponent<RectTransform>();
            UIManager.Stretch(missionScrollRectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            missionScroll = missionScrollObj.AddComponent<ScrollRect>();
            missionScroll.horizontal = false;
            missionScroll.vertical = true;
            missionScroll.movementType = ScrollRect.MovementType.Clamped;
            missionScroll.scrollSensitivity = 32f;
            missionScroll.viewport = missionViewportRect;

            // Content Text — grows vertically with content; ScrollRect scrolls it
            missionText = UIManager.CreateText("MissionText", missionScrollObj.transform, font, 16, TextAnchor.UpperCenter, lightText);
            missionText.rectTransform.anchorMin = new Vector2(0f, 1f);
            missionText.rectTransform.anchorMax = new Vector2(1f, 1f);
            missionText.rectTransform.pivot = new Vector2(0.5f, 1f);
            missionText.rectTransform.offsetMin = Vector2.zero;
            missionText.rectTransform.offsetMax = Vector2.zero;
            missionText.verticalOverflow = VerticalWrapMode.Overflow;
            ContentSizeFitter missionFitter = missionText.gameObject.AddComponent<ContentSizeFitter>();
            missionFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            missionFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            UIManager.StyleToyText(missionText, subtleHighlight, new Vector2(0f, 1f), subtleShadow, new Vector2(0f, -1f));
            missionScroll.content = missionText.rectTransform;

            rerollButton = UIManager.CreateButton("RerollButton", missionRibbon.transform, font, "REROLL", UIColors.Warning, UIColors.TextDark);
            UIManager.Stretch((RectTransform)rerollButton.transform, new Vector2(0.74f, 0.18f), new Vector2(0.98f, 0.82f), Vector2.zero, Vector2.zero);
            UIManager.StyleButtonLabel(rerollButton, 17, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            UIManager.BindButton(rerollButton, () => onRerollMissions?.Invoke(), buttonClickSound);
            rerollButtonLabel = rerollButton.GetComponentInChildren<Text>();

            chestButton = UIManager.CreateButton("ChestButton", transform, font, "CHEST", UIColors.Success, Color.white);
            UIManager.Stretch((RectTransform)chestButton.transform, new Vector2(0.04f, 0.92f), new Vector2(0.22f, 0.975f), Vector2.zero, Vector2.zero);
            UIManager.StyleButtonLabel(chestButton, 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            UIManager.BindButton(chestButton, () => onClaimChest(), buttonClickSound);
            chestButtonLabel = chestButton.GetComponentInChildren<Text>();

            chestInfoText = UIManager.CreateText("ChestInfoText", transform, font, 13, TextAnchor.MiddleCenter, new Color(0.88f, 0.92f, 0.98f, 1f));
            UIManager.Stretch(chestInfoText.rectTransform, new Vector2(0.04f, 0.875f), new Vector2(0.22f, 0.915f), Vector2.zero, Vector2.zero);
            UIManager.StyleToyText(chestInfoText, new Color(1f, 1f, 1f, 0.08f), new Vector2(0f, 1f), new Color(0f, 0f, 0f, 0.32f), new Vector2(0f, -1f));

            Image leaderboardCard = UIManager.CreateCard("LeaderboardCard", transform, lightCard, lightOutline);
            UIManager.Stretch(leaderboardCard.rectTransform, new Vector2(0.18f, 0.215f), new Vector2(0.82f, 0.365f), Vector2.zero, Vector2.zero);
            leaderboardPanel = leaderboardCard.gameObject.AddComponent<LeaderboardPanelController>();
            leaderboardPanel.Initialize(font, theme, "TOP RUNS");

            Image titlePlaque = UIManager.CreateCard("TitlePlaque", transform, yellowCard, yellowOutline);
            UIManager.Stretch(titlePlaque.rectTransform, new Vector2(0.2f, 0.79f), new Vector2(0.78f, 0.885f), Vector2.zero, Vector2.zero);
            Text title = UIManager.CreateText("Title", titlePlaque.transform, font, 32, TextAnchor.MiddleCenter, yellowText);
            title.fontStyle = FontStyle.Bold;
            title.text = "TOWER MAZE";
            UIManager.Stretch(title.rectTransform, new Vector2(0f, 0.52f), new Vector2(1f, 0.94f), new Vector2(24f, 0f), new Vector2(-24f, 0f));
            UIManager.StyleToyText(title, subtleHighlight, new Vector2(0f, 1f), subtleShadow, new Vector2(0f, -1f));

            Text subtitle = UIManager.CreateText("Subtitle", titlePlaque.transform, font, 16, TextAnchor.MiddleCenter, yellowText);
            subtitle.text = "CLIMB FAST";
            UIManager.Stretch(subtitle.rectTransform, new Vector2(0f, 0.08f), new Vector2(1f, 0.4f), new Vector2(24f, 0f), new Vector2(-24f, 0f));
            UIManager.StyleToyText(subtitle, subtleHighlight, new Vector2(0f, 1f), subtleShadow, new Vector2(0f, -1f));

            Image lifePill = UIManager.CreateCard("LifePill", transform, lightCard, lightOutline);
            UIManager.Stretch(lifePill.rectTransform, new Vector2(0.04f, 0.775f), new Vector2(0.22f, 0.84f), Vector2.zero, Vector2.zero);
            lifeIconText = UIManager.CreateText("LifeIconText", lifePill.transform, font, 20, TextAnchor.MiddleCenter, new Color(1f, 0.28f, 0.36f, 1f));
            lifeIconText.fontStyle = FontStyle.Bold;
            lifeIconText.text = "❤";
            UIManager.Stretch(lifeIconText.rectTransform, new Vector2(0f, 0f), new Vector2(0.24f, 1f), new Vector2(8f, 0f), new Vector2(-2f, 0f));
            UIManager.StyleToyText(lifeIconText, new Color(1f, 1f, 1f, 0.12f), new Vector2(0f, 1f), new Color(0.36f, 0f, 0.02f, 0.18f), new Vector2(0f, -1f));

            lifeInfoText = UIManager.CreateText("LifeInfoText", lifePill.transform, font, 14, TextAnchor.MiddleLeft, lightText);
            lifeInfoText.fontStyle = FontStyle.Bold;
            UIManager.Stretch(lifeInfoText.rectTransform, new Vector2(0.24f, 0f), new Vector2(1f, 1f), new Vector2(6f, 0f), new Vector2(-10f, 0f));
            UIManager.StyleToyText(lifeInfoText, subtleHighlight, new Vector2(0f, 1f), subtleShadow, new Vector2(0f, -1f));

            UIManager.Stretch(lifePill.rectTransform, new Vector2(0.035f, 0.79f), new Vector2(0.19f, 0.85f), Vector2.zero, Vector2.zero);
            lifeIconText.fontSize = 22;
            lifeIconText.text = "\u2665";
            UIManager.Stretch(lifeIconText.rectTransform, new Vector2(0.04f, 0f), new Vector2(0.3f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));
            lifeInfoText.resizeTextForBestFit = true;
            lifeInfoText.resizeTextMinSize = 10;
            lifeInfoText.resizeTextMaxSize = 18;
            lifeInfoText.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIManager.Stretch(lifeInfoText.rectTransform, new Vector2(0.3f, 0f), new Vector2(1f, 1f), new Vector2(4f, 0f), new Vector2(-6f, 0f));

            Image challengeCard = UIManager.CreateCard("ChallengeCard", transform, lightCard, lightOutline);
            UIManager.Stretch(challengeCard.rectTransform, new Vector2(0.28f, 0.635f), new Vector2(0.72f, 0.78f), Vector2.zero, Vector2.zero);
            challengeTitleLabel = UIManager.CreateText("ChallengeTitle", challengeCard.transform, font, 18, TextAnchor.MiddleCenter, lightText);
            challengeTitleLabel.text = "DAILY CHALLENGE";
            challengeTitleLabel.fontStyle = FontStyle.Bold;
            UIManager.Stretch(challengeTitleLabel.rectTransform, new Vector2(0f, 0.64f), new Vector2(1f, 1f), new Vector2(12f, 0f), new Vector2(-12f, 0f));
            UIManager.StyleToyText(challengeTitleLabel, subtleHighlight, new Vector2(0f, 1f), subtleShadow, new Vector2(0f, -1f));

            challengeInfoText = UIManager.CreateText("ChallengeInfo", challengeCard.transform, font, 15, TextAnchor.UpperCenter, lightText);
            UIManager.Stretch(challengeInfoText.rectTransform, new Vector2(0f, 0.22f), new Vector2(1f, 0.70f), new Vector2(14f, 4f), new Vector2(-14f, -4f));
            UIManager.StyleToyText(challengeInfoText, subtleHighlight, new Vector2(0f, 1f), subtleShadow, new Vector2(0f, -1f));

            // Warning strip at the bottom of the challenge card
            Image warningStrip = UIManager.CreateImage("WarningStrip", challengeCard.transform, UIColors.Warning);
            UIManager.Stretch(warningStrip.rectTransform, Vector2.zero, new Vector2(1f, 0.22f), Vector2.zero, Vector2.zero);
            warningStrip.raycastTarget = false;
            Text warningText = UIManager.CreateText("WarningText", warningStrip.transform, font, 11, TextAnchor.MiddleCenter, Color.white);
            warningText.text = "! Daily run doesn't affect leaderboard results";
            warningText.fontStyle = FontStyle.Bold;
            warningText.resizeTextForBestFit = true;
            warningText.resizeTextMinSize = 8;
            warningText.resizeTextMaxSize = 11;
            UIManager.Stretch(warningText.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));
            warningText.raycastTarget = false;

            // DAILY RUN button sits just below the challenge card (card bottom = 0.635)
            challengeButton = UIManager.CreateButton("ChallengeButton", transform, font, "DAILY RUN", UIColors.Warning, UIColors.TextDark);
            UIManager.Stretch((RectTransform)challengeButton.transform, new Vector2(0.32f, 0.585f), new Vector2(0.68f, 0.63f), Vector2.zero, Vector2.zero);
            UIManager.StyleButtonLabel(challengeButton, 20, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            UIManager.BindButton(challengeButton, () => onPlayDailyChallenge?.Invoke(), buttonClickSound);
            challengeButtonLabel = challengeButton.GetComponentInChildren<Text>();

            Button playButton = UIManager.CreateButton("PlayButton", transform, font, "BASLA", UIColors.Primary, Color.white);
            UIManager.Stretch((RectTransform)playButton.transform, new Vector2(0.34f, 0.41f), new Vector2(0.66f, 0.475f), Vector2.zero, Vector2.zero);
            UIManager.StyleButtonLabel(playButton, 28, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            UIManager.BindButton(playButton, () => onPlay(), buttonClickSound);
            playBtnLabel = playButton.GetComponentInChildren<Text>();

            // Settings panel — created LAST so it renders on top of all other start screen UI
            settingsPanel = new GameObject("SettingsPanel");
            settingsPanel.transform.SetParent(transform, false);
            RectTransform settingsRoot = settingsPanel.AddComponent<RectTransform>();
            UIManager.Stretch(settingsRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image dimOverlay = settingsPanel.AddComponent<Image>();
            dimOverlay.color = new Color(0f, 0f, 0f, 0.65f);

            Image settingsCard = UIManager.CreateCard("SettingsCard", settingsPanel.transform,
                new Color(0.07f, 0.09f, 0.16f, 0.98f), new Color(0.25f, 0.35f, 0.62f, 0.55f));
            UIManager.Stretch(settingsCard.rectTransform, new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.78f), Vector2.zero, Vector2.zero);

            settingsTitleText = UIManager.CreateText("SettingsTitle", settingsCard.transform, font, 28, TextAnchor.MiddleCenter, Color.white);
            settingsTitleText.text = "SETTINGS";
            settingsTitleText.fontStyle = FontStyle.Bold;
            UIManager.Stretch(settingsTitleText.rectTransform, new Vector2(0f, 0.82f), new Vector2(1f, 1f), new Vector2(16f, 0f), new Vector2(-16f, 0f));
            UIManager.StyleToyText(settingsTitleText, subtleHighlight, new Vector2(0f, 1f), subtleShadow, new Vector2(0f, -1f));

            Image titleDivider = UIManager.CreateImage("Divider", settingsCard.transform, new Color(0.3f, 0.4f, 0.65f, 0.45f));
            UIManager.Stretch(titleDivider.rectTransform, new Vector2(0.05f, 0.79f), new Vector2(0.95f, 0.79f), Vector2.zero, new Vector2(0f, 2f));
            titleDivider.raycastTarget = false;

            // Sound row
            soundRowLabelText = UIManager.CreateText("SoundRowLabel", settingsCard.transform, font, 20, TextAnchor.MiddleLeft, Color.white);
            soundRowLabelText.text = "SOUND";
            UIManager.Stretch(soundRowLabelText.rectTransform, new Vector2(0.06f, 0.6f), new Vector2(0.56f, 0.74f), Vector2.zero, Vector2.zero);

            Button soundToggleBtn = UIManager.CreateButton("SoundToggle", settingsCard.transform, font, "ON", activeToggle, Color.white);
            UIManager.Stretch((RectTransform)soundToggleBtn.transform, new Vector2(0.58f, 0.61f), new Vector2(0.94f, 0.74f), Vector2.zero, Vector2.zero);
            UIManager.StyleButtonLabel(soundToggleBtn, 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            UIManager.BindButton(soundToggleBtn, () => onToggleSound(), buttonClickSound);
            settingsSoundLabel = soundToggleBtn.GetComponentInChildren<Text>();
            settingsSoundBg = soundToggleBtn.GetComponent<Image>();

            // Vibration row
            vibeRowLabelText = UIManager.CreateText("VibRowLabel", settingsCard.transform, font, 20, TextAnchor.MiddleLeft, Color.white);
            vibeRowLabelText.text = "VIBRATION";
            UIManager.Stretch(vibeRowLabelText.rectTransform, new Vector2(0.06f, 0.44f), new Vector2(0.56f, 0.58f), Vector2.zero, Vector2.zero);

            Button vibeToggleBtn = UIManager.CreateButton("VibToggle", settingsCard.transform, font, "ON", activeToggle, Color.white);
            UIManager.Stretch((RectTransform)vibeToggleBtn.transform, new Vector2(0.58f, 0.45f), new Vector2(0.94f, 0.58f), Vector2.zero, Vector2.zero);
            UIManager.StyleButtonLabel(vibeToggleBtn, 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            UIManager.BindButton(vibeToggleBtn, () => onToggleVibration(), buttonClickSound);
            settingsVibLabel = vibeToggleBtn.GetComponentInChildren<Text>();
            settingsVibBg = vibeToggleBtn.GetComponent<Image>();

            // Language row (3 buttons: TR / EN / ES)
            langRowLabelText = UIManager.CreateText("LangRowLabel", settingsCard.transform, font, 20, TextAnchor.MiddleLeft, Color.white);
            langRowLabelText.text = "LANGUAGE";
            UIManager.Stretch(langRowLabelText.rectTransform, new Vector2(0.06f, 0.27f), new Vector2(0.5f, 0.41f), Vector2.zero, Vector2.zero);

            settingsLangTRBtn = UIManager.CreateButton("LangTR", settingsCard.transform, font, "TR", activeToggle, Color.white);
            UIManager.Stretch((RectTransform)settingsLangTRBtn.transform, new Vector2(0.53f, 0.28f), new Vector2(0.66f, 0.41f), Vector2.zero, Vector2.zero);
            UIManager.StyleButtonLabel(settingsLangTRBtn, 16, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            UIManager.BindButton(settingsLangTRBtn, () =>
            {
                PlayerPrefs.SetString("TowerMaze.Language", "tr");
                PlayerPrefs.Save();
                UpdateLangButtonColors(activeToggle, inactiveToggle);
                ApplyLanguage("tr");
            }, buttonClickSound);

            settingsLangENBtn = UIManager.CreateButton("LangEN", settingsCard.transform, font, "EN", inactiveToggle, Color.white);
            UIManager.Stretch((RectTransform)settingsLangENBtn.transform, new Vector2(0.68f, 0.28f), new Vector2(0.80f, 0.41f), Vector2.zero, Vector2.zero);
            UIManager.StyleButtonLabel(settingsLangENBtn, 16, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            UIManager.BindButton(settingsLangENBtn, () =>
            {
                PlayerPrefs.SetString("TowerMaze.Language", "en");
                PlayerPrefs.Save();
                UpdateLangButtonColors(activeToggle, inactiveToggle);
                ApplyLanguage("en");
            }, buttonClickSound);

            settingsLangESBtn = UIManager.CreateButton("LangES", settingsCard.transform, font, "ES", inactiveToggle, Color.white);
            UIManager.Stretch((RectTransform)settingsLangESBtn.transform, new Vector2(0.82f, 0.28f), new Vector2(0.94f, 0.41f), Vector2.zero, Vector2.zero);
            UIManager.StyleButtonLabel(settingsLangESBtn, 16, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            UIManager.BindButton(settingsLangESBtn, () =>
            {
                PlayerPrefs.SetString("TowerMaze.Language", "es");
                PlayerPrefs.Save();
                UpdateLangButtonColors(activeToggle, inactiveToggle);
                ApplyLanguage("es");
            }, buttonClickSound);

            // Close button — white
            Button closePanelBtn = UIManager.CreateButton("CloseSettings", settingsCard.transform, font, "CLOSE",
                Color.white, new Color(0.1f, 0.12f, 0.2f, 1f));
            UIManager.Stretch((RectTransform)closePanelBtn.transform, new Vector2(0.25f, 0.05f), new Vector2(0.75f, 0.18f), Vector2.zero, Vector2.zero);
            UIManager.StyleButtonLabel(closePanelBtn, 20, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            closeBtnLabelText = closePanelBtn.GetComponentInChildren<Text>();
            UIManager.BindButton(closePanelBtn, () => settingsPanel.SetActive(false), buttonClickSound);

            settingsPanel.SetActive(false);
            UIManager.BindButton(gearButton, () => settingsPanel.SetActive(true), buttonClickSound);

            // Apply saved language on startup
            ApplyLanguage(PlayerPrefs.GetString("TowerMaze.Language", "tr"));
        }

        public void SetState(float bestScore, int emberBalance, IReadOnlyList<LeaderboardEntry> leaderboardEntries, IReadOnlyList<DailyMissionState> dailyMissions, DailyChestStatus chestStatus, DailyChallengeStatus challengeStatus, int missionRerollCost, bool soundEnabled, bool vibrationEnabled)
        {
            string lang = PlayerPrefs.GetString("TowerMaze.Language", "tr");
            switch (lang)
            {
                case "en":
                    bestScoreText.text = $"BEST {bestScore:0.0}M";
                    emberText.text = $"COIN {emberBalance}";
                    chestButtonLabel.text = chestStatus.canClaim ? (chestStatus.requiresRewardedAd ? "BONUS CHEST" : "DAILY CHEST") : "CHEST TMRW";
                    chestInfoText.text = chestStatus.canClaim ? (chestStatus.requiresRewardedAd ? $"WATCH AD +{chestStatus.rewardPreview}" : $"+{chestStatus.rewardPreview} COIN") : "ALL CLAIMED";
                    challengeButtonLabel.text = challengeStatus.rewardClaimed ? "DAILY REPLAY" : $"DAILY RUN +{challengeStatus.firstClearReward}";
                    rerollButtonLabel.text = $"REROLL {missionRerollCost}";
                    break;
                case "es":
                    bestScoreText.text = $"MEJOR {bestScore:0.0}M";
                    emberText.text = $"COIN {emberBalance}";
                    chestButtonLabel.text = chestStatus.canClaim ? (chestStatus.requiresRewardedAd ? "COFRE EXTRA" : "COFRE DIA") : "MANANA";
                    chestInfoText.text = chestStatus.canClaim ? (chestStatus.requiresRewardedAd ? $"VER AD +{chestStatus.rewardPreview}" : $"+{chestStatus.rewardPreview} COIN") : "YA ABIERTO";
                    challengeButtonLabel.text = challengeStatus.rewardClaimed ? "REJUGAR" : $"RETO DIA +{challengeStatus.firstClearReward}";
                    rerollButtonLabel.text = $"ROTAR {missionRerollCost}";
                    break;
                default: // "tr"
                    bestScoreText.text = $"EN IYI {bestScore:0.0}M";
                    emberText.text = $"COIN {emberBalance}";
                    chestButtonLabel.text = chestStatus.canClaim ? (chestStatus.requiresRewardedAd ? "BONUS SANDIK" : "GUNLUK SANDIK") : "YARIN SANDIK";
                    chestInfoText.text = chestStatus.canClaim ? (chestStatus.requiresRewardedAd ? $"REKLAM +{chestStatus.rewardPreview}" : $"+{chestStatus.rewardPreview} COIN") : "HEPSI ALINDI";
                    challengeButtonLabel.text = challengeStatus.rewardClaimed ? "TEKRAR OYN" : $"GUNLUK KOS +{challengeStatus.firstClearReward}";
                    rerollButtonLabel.text = $"YENILE {missionRerollCost}";
                    break;
            }
            UpdateLifeInfo();
            chestButton.interactable = chestStatus.canClaim;
            missionText.text = BuildMissionText(dailyMissions);
            LayoutRebuilder.ForceRebuildLayoutImmediate(missionText.rectTransform);
            if (missionScroll != null) missionScroll.verticalNormalizedPosition = 1f;
            challengeInfoText.text = BuildChallengeText(challengeStatus);
            Color activeToggle = UIColors.Success;
            Color inactiveToggle = new Color(0.3f, 0.34f, 0.42f, 0.9f);
            if (settingsSoundLabel != null) settingsSoundLabel.text = soundEnabled ? "ON" : "OFF";
            if (settingsSoundBg != null) settingsSoundBg.color = soundEnabled ? activeToggle : inactiveToggle;
            if (settingsVibLabel != null) settingsVibLabel.text = vibrationEnabled ? "ON" : "OFF";
            if (settingsVibBg != null) settingsVibBg.color = vibrationEnabled ? activeToggle : inactiveToggle;
            UpdateLangButtonColors(activeToggle, inactiveToggle);
            leaderboardPanel.SetEntries(leaderboardEntries);
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy || economyManager == null || Time.unscaledTime < nextLifeRefreshTime)
            {
                return;
            }

            nextLifeRefreshTime = Time.unscaledTime + 1f;
            UpdateLifeInfo();
        }

        private void UpdateLifeInfo()
        {
            if (lifeInfoText == null || economyManager == null)
            {
                return;
            }

            int remainingLives = economyManager.RemainingLives;
            lifeInfoText.text = remainingLives >= EconomyManager.MaxLifeCount
                ? $"{remainingLives}/{EconomyManager.MaxLifeCount} FULL"
                : $"{remainingLives}/{EconomyManager.MaxLifeCount} {UiTextFormatter.FormatCountdown(economyManager.GetTimeUntilNextLife())}";
        }

        private static string BuildMissionText(IReadOnlyList<DailyMissionState> dailyMissions)
        {
            if (dailyMissions == null || dailyMissions.Count == 0)
            {
                return "No missions available.";
            }

            StringBuilder builder = new();
            int visibleCount = dailyMissions.Count;
            for (int index = 0; index < visibleCount; index++)
            {
                DailyMissionState mission = dailyMissions[index];
                builder.Append(index + 1)
                    .Append("  ")
                    .Append(UiTextFormatter.Truncate(mission.description, 24))
                    .Append("  ")
                    .Append(mission.claimed ? "DONE" : $"{Mathf.Min(mission.progressValue, mission.targetValue)}/{mission.targetValue}")
                    .Append("  +")
                    .Append(mission.rewardEmber);

                if (index < visibleCount - 1)
                {
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private void UpdateLangButtonColors(Color active, Color inactive)
        {
            string lang = PlayerPrefs.GetString("TowerMaze.Language", "tr");
            if (settingsLangTRBtn != null) settingsLangTRBtn.GetComponent<Image>().color = lang == "tr" ? active : inactive;
            if (settingsLangENBtn != null) settingsLangENBtn.GetComponent<Image>().color = lang == "en" ? active : inactive;
            if (settingsLangESBtn != null) settingsLangESBtn.GetComponent<Image>().color = lang == "es" ? active : inactive;
        }

        private void ApplyLanguage(string lang)
        {
            switch (lang)
            {
                case "en":
                    if (playBtnLabel != null) playBtnLabel.text = "START";
                    if (shopButtonLabel != null) shopButtonLabel.text = "SHOP";
                    if (missionTitleLabel != null) missionTitleLabel.text = "MISSIONS";
                    if (challengeTitleLabel != null) challengeTitleLabel.text = "DAILY CHALLENGE";
                    if (rerollButtonLabel != null) rerollButtonLabel.text = "REROLL";
                    leaderboardPanel?.SetTitle("TOP RUNS");
                    if (settingsTitleText != null) settingsTitleText.text = "SETTINGS";
                    if (soundRowLabelText != null) soundRowLabelText.text = "SOUND";
                    if (vibeRowLabelText != null) vibeRowLabelText.text = "VIBRATION";
                    if (langRowLabelText != null) langRowLabelText.text = "LANGUAGE";
                    if (closeBtnLabelText != null) closeBtnLabelText.text = "CLOSE";
                    break;
                case "es":
                    if (playBtnLabel != null) playBtnLabel.text = "JUGAR";
                    if (shopButtonLabel != null) shopButtonLabel.text = "TIENDA";
                    if (missionTitleLabel != null) missionTitleLabel.text = "MISIONES";
                    if (challengeTitleLabel != null) challengeTitleLabel.text = "RETO DIARIO";
                    if (rerollButtonLabel != null) rerollButtonLabel.text = "ROTAR";
                    leaderboardPanel?.SetTitle("TOP");
                    if (settingsTitleText != null) settingsTitleText.text = "AJUSTES";
                    if (soundRowLabelText != null) soundRowLabelText.text = "SONIDO";
                    if (vibeRowLabelText != null) vibeRowLabelText.text = "VIBRACION";
                    if (langRowLabelText != null) langRowLabelText.text = "IDIOMA";
                    if (closeBtnLabelText != null) closeBtnLabelText.text = "CERRAR";
                    break;
                default: // "tr"
                    if (playBtnLabel != null) playBtnLabel.text = "BASLA";
                    if (shopButtonLabel != null) shopButtonLabel.text = "MAGAZA";
                    if (missionTitleLabel != null) missionTitleLabel.text = "GOREVLER";
                    if (challengeTitleLabel != null) challengeTitleLabel.text = "GUNLUK GOREV";
                    if (rerollButtonLabel != null) rerollButtonLabel.text = "YENILE";
                    leaderboardPanel?.SetTitle("EN IYI");
                    if (settingsTitleText != null) settingsTitleText.text = "AYARLAR";
                    if (soundRowLabelText != null) soundRowLabelText.text = "SES";
                    if (vibeRowLabelText != null) vibeRowLabelText.text = "TITRESIM";
                    if (langRowLabelText != null) langRowLabelText.text = "DIL";
                    if (closeBtnLabelText != null) closeBtnLabelText.text = "KAPAT";
                    break;
            }
        }

        private static string BuildChallengeText(DailyChallengeStatus challengeStatus)
        {
            if (challengeStatus.seed == 0)
            {
                return "Loading challenge...";
            }

            string rewardState = challengeStatus.rewardClaimed ? "reward claimed" : $"+{challengeStatus.firstClearReward} coin";
            return $"Target {challengeStatus.targetHeight}m  |  Best {challengeStatus.bestHeight:0.0}m  |  {rewardState}\n{EconomyManager.GetModifierDisplayName(challengeStatus.primaryModifier)} + {EconomyManager.GetModifierDisplayName(challengeStatus.secondaryModifier)}";
        }

    }

    public sealed class FailScreenController : MonoBehaviour
    {
        private EconomyManager economyManager;
        private Action buttonClickSound;
        private Text scoreText;
        private Text timeText;
        private Text bestScoreText;
        private Text emberText;
        private Text rewardText;
        private Text nextTargetText;
        private Button retryButton;
        private Text retryButtonLabel;
        private Button continueButton;
        private Text continueButtonLabel;
        private Button rewardButton;
        private Text rewardButtonLabel;
        private Action retryRunAction;
        private Action continueRunAction;
        private Action watchLifeRefillAdAction;
        private Action buyLifeRefillWithCoinsAction;
        private bool retryUsesLifeRefillAd;
        private bool continueUsesCoinLifeRefill;
        private bool showingLifeTimer;
        private int cachedLifeRefillCoinCost;
        private float nextLifeRefreshTime;

        public void Initialize(Font font, ThemeDefinition theme, EconomyManager economy, Action onRetry, Action onContinue, Action onReturnToMenu, Action onClaimDoubleReward, Action onWatchLifeRefillAd, Action onBuyLifeRefillWithCoins, Action onButtonClick = null)
        {
            economyManager = economy;
            buttonClickSound = onButtonClick;
            retryRunAction = onRetry;
            continueRunAction = onContinue;
            watchLifeRefillAdAction = onWatchLifeRefillAd;
            buyLifeRefillWithCoinsAction = onBuyLifeRefillWithCoins;

            Color darkSurface = UIColors.Surface;
            Color darkOutline = UIColors.Divider;
            Color lightSurface = UIColors.Card;
            Color lightOutline = UIColors.Divider;
            Color yellowSurface = UIColors.Card;
            Color yellowOutline = UIColors.Divider;
            Color darkText = UIColors.TextDark;
            Color coolText = UIColors.TextMid;

            Image failOverlay = UIManager.CreateImage("FailBackdrop", transform, new Color(0f, 0f, 0f, 0.4f));
            UIManager.Stretch(failOverlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image failPanel = UIManager.CreateCard("FailPanel", transform, darkSurface, darkOutline);
            UIManager.Stretch(failPanel.rectTransform, new Vector2(0.08f, 0.1f), new Vector2(0.92f, 0.9f), Vector2.zero, Vector2.zero);

            Image failTitleCard = UIManager.CreateCard("FailTitleCard", failPanel.transform, yellowSurface, yellowOutline);
            UIManager.Stretch(failTitleCard.rectTransform, new Vector2(0.03f, 0.86f), new Vector2(0.97f, 0.98f), Vector2.zero, Vector2.zero);

            Text failTitle = UIManager.CreateText("FailTitle", failTitleCard.transform, font, 56, TextAnchor.MiddleCenter, Color.black);
            failTitle.text = "TRY AGAIN";
            failTitle.fontStyle = FontStyle.Bold;
            UIManager.Stretch(failTitle.rectTransform, Vector2.zero, Vector2.one, new Vector2(16f, 0f), new Vector2(-16f, 0f));
            UIManager.StyleToyText(failTitle, new Color(1f, 1f, 1f, 0.18f), new Vector2(0f, 1f), new Color(0.08f, 0.1f, 0.12f, 0.16f), new Vector2(0f, -1f));

            Image summaryCard = UIManager.CreateCard("StatsCard", failPanel.transform, lightSurface, lightOutline);
            UIManager.Stretch(summaryCard.rectTransform, new Vector2(0.04f, 0.67f), new Vector2(0.96f, 0.82f), Vector2.zero, Vector2.zero);

            scoreText = UIManager.CreateText("ScoreText", summaryCard.transform, font, 28, TextAnchor.MiddleLeft, coolText);
            scoreText.fontStyle = FontStyle.Bold;
            UIManager.Stretch(scoreText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 1f), new Vector2(20f, 0f), new Vector2(-10f, -4f));

            timeText = UIManager.CreateText("TimeText", summaryCard.transform, font, 26, TextAnchor.MiddleRight, coolText);
            timeText.fontStyle = FontStyle.Bold;
            UIManager.Stretch(timeText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1f, 1f), new Vector2(10f, 0f), new Vector2(-20f, -4f));

            bestScoreText = UIManager.CreateText("BestScoreText", summaryCard.transform, font, 26, TextAnchor.MiddleLeft, coolText);
            bestScoreText.fontStyle = FontStyle.Bold;
            UIManager.Stretch(bestScoreText.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(20f, 4f), new Vector2(-10f, 0f));

            emberText = UIManager.CreateText("EmberText", summaryCard.transform, font, 26, TextAnchor.MiddleRight, UIColors.TextDark);
            emberText.fontStyle = FontStyle.Bold;
            UIManager.Stretch(emberText.rectTransform, new Vector2(0.5f, 0f), new Vector2(1f, 0.5f), new Vector2(10f, 4f), new Vector2(-20f, 0f));

            Image rewardSummaryCard = UIManager.CreateCard("RewardCard", failPanel.transform, lightSurface, lightOutline);
            UIManager.Stretch(rewardSummaryCard.rectTransform, new Vector2(0.04f, 0.49f), new Vector2(0.96f, 0.645f), Vector2.zero, Vector2.zero);
            rewardText = UIManager.CreateText("RewardText", rewardSummaryCard.transform, font, 20, TextAnchor.MiddleCenter, coolText);
            rewardText.fontStyle = FontStyle.Bold;
            UIManager.Stretch(rewardText.rectTransform, new Vector2(0f, 0.45f), new Vector2(1f, 1f), new Vector2(16f, 0f), new Vector2(-16f, 0f));

            nextTargetText = UIManager.CreateText("NextTargetText", rewardSummaryCard.transform, font, 16, TextAnchor.MiddleCenter, coolText);
            nextTargetText.fontStyle = FontStyle.Bold;
            UIManager.Stretch(nextTargetText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.45f), new Vector2(16f, 0f), new Vector2(-16f, 0f));

            retryButton = UIManager.CreateButton("RetryButton", failPanel.transform, font, "RETRY", UIColors.Danger, Color.white);
            UIManager.Stretch((RectTransform)retryButton.transform, new Vector2(0.04f, 0.315f), new Vector2(0.96f, 0.455f), Vector2.zero, Vector2.zero);
            UIManager.StyleButtonLabel(retryButton, 28, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            UIManager.BindButton(retryButton, HandleRetryPressed, buttonClickSound);
            retryButtonLabel = retryButton.GetComponentInChildren<Text>();

            Button menuActionButton = UIManager.CreateButton("MenuButton", failPanel.transform, font, "MAIN MENU", Color.clear, UIColors.TextDim);
            UIManager.Stretch((RectTransform)menuActionButton.transform, new Vector2(0.04f, 0.09f), new Vector2(0.48f, 0.155f), Vector2.zero, Vector2.zero);
            UIManager.StyleButtonLabel(menuActionButton, 20, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            UIManager.BindButton(menuActionButton, () => onReturnToMenu(), buttonClickSound);

            continueButton = UIManager.CreateButton("ContinueButton", failPanel.transform, font, "CONTINUE", UIColors.Success, Color.white);
            UIManager.Stretch((RectTransform)continueButton.transform, new Vector2(0.04f, 0.180f), new Vector2(0.96f, 0.285f), Vector2.zero, Vector2.zero);
            UIManager.StyleButtonLabel(continueButton, 24, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            UIManager.BindButton(continueButton, HandleContinuePressed, buttonClickSound);
            continueButtonLabel = continueButton.GetComponentInChildren<Text>();

            rewardButton = UIManager.CreateButton("RewardButton", failPanel.transform, font, "x2 COIN", UIColors.Warning, UIColors.TextDark);
            UIManager.Stretch((RectTransform)rewardButton.transform, new Vector2(0.52f, 0.09f), new Vector2(0.96f, 0.155f), Vector2.zero, Vector2.zero);
            UIManager.StyleButtonLabel(rewardButton, 20, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            UIManager.BindButton(rewardButton, () => onClaimDoubleReward(), buttonClickSound);
            rewardButtonLabel = rewardButton.GetComponentInChildren<Text>();
        }

        private void HandleRetryPressed()
        {
            if (retryUsesLifeRefillAd)
            {
                watchLifeRefillAdAction?.Invoke();
                return;
            }

            retryRunAction?.Invoke();
        }

        private void HandleContinuePressed()
        {
            if (continueUsesCoinLifeRefill)
            {
                buyLifeRefillWithCoinsAction?.Invoke();
                return;
            }

            continueRunAction?.Invoke();
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy || !showingLifeTimer || economyManager == null || Time.unscaledTime < nextLifeRefreshTime)
            {
                return;
            }

            nextLifeRefreshTime = Time.unscaledTime + 1f;
            nextTargetText.text = BuildLifeRefillText();
        }

        public void SetState(float score, float bestScore, float runTime, IReadOnlyList<LeaderboardEntry> leaderboardEntries, int emberBalance, int rewardValue, int claimedReward, bool canContinue, bool canClaimDoubleReward, string bestDeltaText, string nextTarget, string modeSummaryText, int remainingLives, bool canWatchLifeRefillAd, bool canBuyLifeRefill, int lifeRefillCoinCost, bool hasContinueOption, int continueCoinCost)
        {
            bool outOfLives = remainingLives <= 0;
            cachedLifeRefillCoinCost = lifeRefillCoinCost;
            showingLifeTimer = outOfLives && economyManager != null && remainingLives < EconomyManager.MaxLifeCount;
            scoreText.text = $"HEIGHT  {score:0.0}m";
            timeText.text = $"TIME  {UiTextFormatter.FormatTime(runTime)}";
            bestScoreText.text = $"BEST  {bestScore:0.0}m  |  {bestDeltaText}";
            emberText.text = $"COIN  {emberBalance}";
            if (claimedReward > 0)
            {
                rewardText.text = outOfLives
                    ? $"CLAIMED  +{claimedReward}\nOUT OF LIVES"
                    : $"CLAIMED  +{claimedReward} COIN\n{modeSummaryText}";
            }
            else if (rewardValue > 0)
            {
                rewardText.text = outOfLives
                    ? $"REWARD  +{rewardValue}\nOUT OF LIVES"
                    : $"RUN REWARD  +{rewardValue} COIN\n{modeSummaryText}";
            }
            else
            {
                rewardText.text = outOfLives
                    ? "OUT OF LIVES"
                    : modeSummaryText;
            }

            nextTargetText.text = outOfLives ? BuildLifeRefillText() : nextTarget;

            retryUsesLifeRefillAd = outOfLives;
            retryButton.interactable = outOfLives ? canWatchLifeRefillAd : true;
            retryButtonLabel.text = outOfLives
                ? $"WATCH AD +{EconomyManager.LifeRefillAmount} LIFE"
                : $"RETRY  {remainingLives}/{EconomyManager.MaxLifeCount}";

            continueUsesCoinLifeRefill = !canContinue && outOfLives;
            continueButton.interactable = canContinue || (outOfLives && canBuyLifeRefill);
            continueButtonLabel.text = canContinue
                ? $"CONTINUE {continueCoinCost} COIN"
                : outOfLives
                    ? (canBuyLifeRefill ? $"{lifeRefillCoinCost} COIN +{EconomyManager.LifeRefillAmount} LIFE" : $"NEED {lifeRefillCoinCost} COIN")
                    : hasContinueOption
                        ? $"NEED {continueCoinCost} COIN"
                        : "CONTINUE USED";

            rewardButton.interactable = canClaimDoubleReward && claimedReward <= 0;
            rewardButtonLabel.text = claimedReward > 0
                ? "x2 CLAIMED"
                : canClaimDoubleReward
                    ? $"WATCH AD x2  +{rewardValue * 2}"
                    : "x2 UNAVAILABLE";
        }

        private string BuildLifeRefillText()
        {
            if (economyManager == null)
            {
                return $"AD OR {cachedLifeRefillCoinCost} COIN  →  +{EconomyManager.LifeRefillAmount} LIFE";
            }

            return $"AD OR {cachedLifeRefillCoinCost} COIN  →  +{EconomyManager.LifeRefillAmount} LIFE\nFREE LIFE IN  {UiTextFormatter.FormatCountdown(economyManager.GetTimeUntilNextLife())}";
        }
    }

    public sealed class IAPUpsellController : MonoBehaviour
    {
        private Font runtimeFont;
        private Action<string> onPurchase;
        private Action buttonClickSound;
        private Image card;
        private Image previewFrame;
        private RawImage previewImage;
        private Text titleText;
        private Text descText;
        private Text priceText;
        private Button buyButton;
        private string currentOfferId;

        public void Initialize(Font font, ThemeDefinition themeDefinition, Action<string> purchaseCallback, Action onButtonClick = null)
        {
            buttonClickSound = onButtonClick;
            runtimeFont = font;
            onPurchase = purchaseCallback;
            BuildUI();
        }

        private void BuildUI()
        {
            RectTransform rt = (RectTransform)transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Semi-transparent backdrop
            Image backdrop = gameObject.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.7f);
            backdrop.raycastTarget = true;

            // Card
            GameObject cardObj = new("UpsellCard");
            cardObj.transform.SetParent(transform, false);
            card = cardObj.AddComponent<Image>();
            UIManager.ApplyCardSurface(card, UIColors.Card);
            RectTransform cardRt = (RectTransform)cardObj.transform;
            cardRt.anchorMin = new Vector2(0.08f, 0.22f);
            cardRt.anchorMax = new Vector2(0.92f, 0.78f);
            cardRt.offsetMin = Vector2.zero;
            cardRt.offsetMax = Vector2.zero;

            // X (close) button — top-right corner of card
            Button closeBtn = UIManager.CreateButton("UpsellClose", cardObj.transform, runtimeFont, "✕", UIColors.Card, UIColors.TextDark);
            Text closeBtnLabel = closeBtn.GetComponentInChildren<Text>();
            closeBtnLabel.fontSize = 18;
            closeBtnLabel.fontStyle = FontStyle.Bold;
            RectTransform closeBtnRt = (RectTransform)closeBtn.transform;
            closeBtnRt.anchorMin = new Vector2(1f, 1f);
            closeBtnRt.anchorMax = new Vector2(1f, 1f);
            closeBtnRt.pivot = new Vector2(0.5f, 0.5f);
            closeBtnRt.anchoredPosition = new Vector2(-20f, -20f);
            closeBtnRt.sizeDelta = new Vector2(36f, 36f);
            UIManager.BindButton(closeBtn, OnCloseClicked, buttonClickSound);

            // Image frame (top 28% of card)
            previewFrame = UIManager.CreateCard("UpsellPreviewFrame", cardObj.transform, UIColors.PrimaryBg, UIColors.Divider);
            previewFrame.raycastTarget = false;
            UIManager.Stretch(previewFrame.rectTransform, new Vector2(0.30f, 0.63f), new Vector2(0.70f, 0.93f), Vector2.zero, Vector2.zero);

            // Product image inside frame — AspectRatioFitter prevents distortion
            GameObject previewObj = new("UpsellPreview");
            previewObj.transform.SetParent(previewFrame.transform, false);
            previewImage = previewObj.AddComponent<RawImage>();
            previewImage.raycastTarget = false;
            AspectRatioFitter arf = previewObj.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            arf.aspectRatio = 1f;
            RectTransform previewRt = (RectTransform)previewObj.transform;
            previewRt.anchorMin = new Vector2(0.1f, 0.1f);
            previewRt.anchorMax = new Vector2(0.9f, 0.9f);
            previewRt.offsetMin = Vector2.zero;
            previewRt.offsetMax = Vector2.zero;

            // Title
            titleText = UIManager.CreateText("UpsellTitle", cardObj.transform, runtimeFont, 21, TextAnchor.MiddleCenter, UIColors.TextDark);
            titleText.fontStyle = FontStyle.Bold;
            UIManager.Stretch(titleText.rectTransform, new Vector2(0.04f, 0.46f), new Vector2(0.96f, 0.62f), Vector2.zero, Vector2.zero);

            // Description
            descText = UIManager.CreateText("UpsellDesc", cardObj.transform, runtimeFont, 15, TextAnchor.MiddleCenter, UIColors.TextMid);
            descText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIManager.Stretch(descText.rectTransform, new Vector2(0.04f, 0.32f), new Vector2(0.96f, 0.47f), Vector2.zero, Vector2.zero);

            // Price
            priceText = UIManager.CreateText("UpsellPrice", cardObj.transform, runtimeFont, 17, TextAnchor.MiddleCenter, Color.white);
            priceText.fontStyle = FontStyle.Bold;
            UIManager.Stretch(priceText.rectTransform, new Vector2(0.04f, 0.18f), new Vector2(0.96f, 0.32f), Vector2.zero, Vector2.zero);

            // Buy button
            buyButton = UIManager.CreateButton("UpsellBuy", cardObj.transform, runtimeFont, "SATIN AL", UIColors.Primary, Color.white);
            Text buyLabel = buyButton.GetComponentInChildren<Text>();
            buyLabel.fontStyle = FontStyle.Bold;
            buyLabel.fontSize = 18;
            UIManager.Stretch(((RectTransform)buyButton.transform), new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.17f), Vector2.zero, Vector2.zero);
            UIManager.BindButton(buyButton, OnBuyClicked, buttonClickSound);
        }

        public void Show(IReadOnlyList<CoinPackOffer> allOffers, string[] candidateIds)
        {
            CoinPackOffer? picked = PickOffer(allOffers, candidateIds);
            if (picked == null)
            {
                gameObject.SetActive(false);
                return;
            }

            CoinPackOffer offer = picked.Value;
            currentOfferId = offer.id;
            titleText.text = offer.displayName;
            descText.text = !string.IsNullOrWhiteSpace(offer.bonusLabel) ? offer.bonusLabel : offer.badgeLabel;
            priceText.text = offer.priceLabel;

            // Apply offer-specific colors
            UIManager.ApplyCardSurface(card, ShopScreenController.GetCoinOfferCardColor(offer));
            UIManager.ApplyCardSurface(previewFrame, ShopScreenController.GetCoinOfferPreviewFrameColor(offer));

            // Load product image
            Texture2D tex = ShopScreenController.GetCoinPackPreviewTexture(offer);
            previewImage.texture = tex != null ? tex : Texture2D.whiteTexture;
            previewImage.color = tex != null ? ShopScreenController.GetCoinOfferPreviewTint(offer) : new Color(0.18f, 0.22f, 0.32f, 1f);

            gameObject.SetActive(true);
        }

        private static CoinPackOffer? PickOffer(IReadOnlyList<CoinPackOffer> allOffers, string[] candidateIds)
        {
            var candidates = new List<CoinPackOffer>();
            foreach (string id in candidateIds)
            {
                foreach (CoinPackOffer offer in allOffers)
                {
                    if (offer.id == id && !offer.owned)
                    {
                        candidates.Add(offer);
                        break;
                    }
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            int seed = (int)(DateTime.UtcNow.Ticks >> 16);
            return candidates[new System.Random(seed).Next(0, candidates.Count)];
        }

        private void OnBuyClicked()
        {
            gameObject.SetActive(false);
            if (!string.IsNullOrEmpty(currentOfferId))
            {
                onPurchase?.Invoke(currentOfferId);
            }
        }

        private void OnCloseClicked()
        {
            gameObject.SetActive(false);
        }
    }

    public sealed class SplashScreenController : MonoBehaviour
    {
        private bool isVisible;
        public bool IsVisible => isVisible;

        public void Initialize(Font font, Texture2D backgroundTexture, Action onComplete)
        {
            if (isVisible) return;
            isVisible = true;

            // Create standalone canvas that renders above all other UI
            Canvas splashCanvas = gameObject.AddComponent<Canvas>();
            splashCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            splashCanvas.sortingOrder = 100;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
            scaler.dynamicPixelsPerUnit = 2f;

            // Root CanvasGroup for fade in/out.
            // alpha starts at 1 (fully opaque) so the splash covers the game world
            // on the very first frame — before the coroutine gets its first tick.
            CanvasGroup rootGroup = gameObject.AddComponent<CanvasGroup>();
            rootGroup.alpha = 1f;
            rootGroup.blocksRaycasts = false;

            // Full-screen background — centered with AspectRatioFitter (EnvelopeParent)
            // so the image covers the screen without stretching on non-9:16 devices.
            // Falls back to solid black if backgroundTexture is null.
            GameObject bgObj = new("SplashBg");
            bgObj.transform.SetParent(transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = Vector2.zero;
            RawImage bg = bgObj.AddComponent<RawImage>();
            bg.color = backgroundTexture != null ? Color.white : UIColors.HudBg;
            bg.texture = backgroundTexture;
            bg.raycastTarget = false;
            if (backgroundTexture != null)
            {
                AspectRatioFitter fitter = bgObj.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = (float)backgroundTexture.width / backgroundTexture.height;
            }

            // Spinner — always visible regardless of whether SpinnerRing.png exists.
            // Prefer the sprite-based filled ring; fall back to a plain white rotating bar.
            GameObject spinnerObj = new("Spinner");
            spinnerObj.transform.SetParent(transform, false);
            RectTransform spinRect = spinnerObj.AddComponent<RectTransform>();
            spinRect.anchorMin = new Vector2(0.5f, 0f);
            spinRect.anchorMax = new Vector2(0.5f, 0f);
            spinRect.pivot = new Vector2(0.5f, 0.5f);
            spinRect.anchoredPosition = new Vector2(0f, 120f);
            Sprite spinnerSprite = Resources.Load<Sprite>("TowerMaze/UITheme/SpinnerRing");
            if (spinnerSprite != null)
            {
                spinRect.sizeDelta = new Vector2(80f, 80f);
                Image spinnerImage = spinnerObj.AddComponent<Image>();
                spinnerImage.sprite = spinnerSprite;
                spinnerImage.type = Image.Type.Filled;
                spinnerImage.fillMethod = Image.FillMethod.Radial360;
                spinnerImage.fillAmount = 0.75f;
                spinnerImage.color = UIColors.PrimaryLight;
                spinnerImage.raycastTarget = false;
            }
            else
            {
                // Fallback: thin white bar that rotates to indicate loading
                spinRect.sizeDelta = new Vector2(6f, 32f);
                Image barImage = spinnerObj.AddComponent<Image>();
                barImage.color = Color.white;
                barImage.raycastTarget = false;
            }

            StartCoroutine(SplashRoutine(rootGroup, spinnerObj, onComplete));
        }

        private System.Collections.IEnumerator SplashRoutine(
            CanvasGroup rootGroup,
            GameObject spinnerObj,
            Action onComplete)
        {
            float startTime = Time.realtimeSinceStartup;
            const float minDisplayTime = 6f;
            // alpha is already 1 from Initialize() — no fade-in needed.
            // Wait for minimum display time while spinning
            float spinnerAngle = 0f;
            while (Time.realtimeSinceStartup - startTime < minDisplayTime)
            {
                spinnerAngle -= 360f * Time.unscaledDeltaTime;
                spinnerObj.transform.localRotation = Quaternion.Euler(0f, 0f, spinnerAngle);
                yield return null;
                if (this == null) yield break;
            }

            isVisible = false;
            onComplete?.Invoke();
            Destroy(gameObject);
        }
    }
}
