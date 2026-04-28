// Assets/Scripts/TowerMaze/Runtime/UISystems/UIManager.cs
// UIManager — canvas setup, screen routing, Initialize()
// Visual redesign happens in individual screen files (Tasks 3–9).
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace TowerMaze
{
    [Flags]
    public enum UIFontRole
    {
        Default = 0,
        Button = 1,
        Popup = 2
    }

    internal sealed class ResponsiveTextSize : MonoBehaviour
    {
        private Text text;
        private int baseFontSize;
        private int bestFitMinSize = -1;
        private int bestFitMaxSize = -1;
        private UIFontRole role;

        internal void SetFontSize(int size, UIFontRole textRole = UIFontRole.Default)
        {
            baseFontSize = Mathf.Max(1, size);
            role = textRole;
            Apply();
        }

        internal void SetBestFit(int minSize, int maxSize, UIFontRole textRole = UIFontRole.Default)
        {
            bestFitMinSize = Mathf.Max(1, minSize);
            bestFitMaxSize = Mathf.Max(bestFitMinSize, maxSize);
            role = textRole;
            Apply();
        }

        internal void SetRole(UIFontRole textRole)
        {
            role = textRole;
            Apply();
        }

        private void Awake()
        {
            text = GetComponent<Text>();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void OnRectTransformDimensionsChange()
        {
            Apply();
        }

        internal void Apply()
        {
            if (text == null)
            {
                text = GetComponent<Text>();
            }

            if (text == null)
            {
                return;
            }

            if (baseFontSize <= 0)
            {
                baseFontSize = Mathf.Max(1, text.fontSize);
            }

            if (bestFitMinSize < 0 && text.resizeTextForBestFit)
            {
                bestFitMinSize = Mathf.Max(1, text.resizeTextMinSize);
            }

            if (bestFitMaxSize < 0 && text.resizeTextForBestFit)
            {
                bestFitMaxSize = Mathf.Max(1, text.resizeTextMaxSize);
            }

            if (baseFontSize > 0)
            {
                text.fontSize = UIManager.ScaleFontSize(text.transform, baseFontSize, role);
            }

            if (bestFitMinSize > 0)
            {
                text.resizeTextMinSize = UIManager.ScaleFontSize(text.transform, bestFitMinSize, role);
            }

            if (bestFitMaxSize > 0)
            {
                text.resizeTextMaxSize = UIManager.ScaleFontSize(text.transform, bestFitMaxSize, role);
            }
        }
    }

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
        private ShopUIController shopUIController;
        private IAPUpsellController iapUpsellController;
        private RewardToastController rewardToastController;
        private PauseScreenController pauseScreenController;
        private ReviewPopupController reviewPopupController;
        private ChapterCompleteScreenController chapterCompleteController;
        private TierCelebrationScreenController tierCelebrationController;
        private ChapterSelectScreenController chapterSelectController;
        private bool chapterSelectBuilt;
        private Font runtimeFont;
        private EconomyManager economyManager;
        private RewardedAdManager rewardedAdManager;
        private CoinStoreManager coinStoreManager;
        private PlayerController playerController;
        private BannerAdManager bannerAdManager;
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
        private const float GlobalFontMultiplier = 1f;
        private const float DefaultTextMultiplier = 1f;
        private const float ButtonTextMultiplier = 1.16f;
        private const float PopupTextMultiplier = 1.10f;
        private const float TinyTextBoost = 1.60f;
        private const float SmallTextBoost = 1.45f;
        private const float RegularTextBoost = 1.28f;
        private const float MediumTextBoost = 1.14f;
        private const float LargeTextBoost = 1.06f;
        private const float HugeTextBoost = 1.02f;
        private const int ButtonBaseFontSizeOffset = 3;
        private const string FailUpsellDeathCountKey = "FailUpsellDeathCount";
        private static readonly Dictionary<string, Sprite> themedSpriteCache = new();
        public bool IsShopOpen => shopUIController != null && shopUIController.gameObject.activeSelf;


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
            ScoreManager scoreMgr = null,
            BannerAdManager bannerAdsManager = null,
            Action<string> onClaimMission = null,
            Action onPlayChapter = null,
            Action onPlayEndless = null,
            Action onShowChapters = null,
            ChapterManager chapterManager = null)
        {
            splashComplete = !splashActive;
            gameConfig = config;
            scoreManager = scoreMgr;
            theme = definition;
            economyManager = economy;
            rewardedAdManager = rewardedAds;
            bannerAdManager = bannerAdsManager;
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
            hudController.Initialize(runtimeFont, theme, onPause, playerController, buttonClickSound, gameConfig, scoreManager);

            startScreenController = CreatePanel<StartScreenController>("StartScreen", canvas.transform);
            startScreenController.Initialize(runtimeFont, theme, economyManager, onPlay, onPlayDailyChallenge, ShowShop, HandleChestClaim, onToggleSound, onToggleVibration, HandleMissionReroll, buttonClickSound, onClaimMission, onPlayChapter, onPlayEndless, onShowChapters, chapterManager);

            failScreenController = CreatePanel<FailScreenController>("FailScreen", canvas.transform);
            failScreenController.Initialize(runtimeFont, theme, economyManager, onRetry, onContinue, onReturnToMenu, onClaimDoubleReward, onWatchLifeRefillAd, onBuyLifeRefillWithCoins, buttonClickSound, gameConfig?.failToRetryDelay ?? 0.3f, gameConfig?.androidStoreUrl, gameConfig?.iosStoreUrl);

            countdownController = CreatePanel<CountdownController>("Countdown", canvas.transform);
            countdownController.Initialize(runtimeFont, theme);

            rushWarningController = CreatePanel<RushWarningController>("RushWarning", canvas.transform);
            rushWarningController.Initialize(runtimeFont, theme);

            controlFlipController = CreatePanel<ControlFlipController>("ControlFlip", canvas.transform);
            controlFlipController.Initialize(runtimeFont, theme);

            shopUIController = CreatePanel<ShopUIController>("ShopScreen", canvas.transform);
            shopUIController.Initialize(runtimeFont, theme, HideShop, HandleShopCoinBoost, HandleCoinStoreRestore, HandleShopAction, buttonClickSound);
            shopUIController.gameObject.SetActive(false);

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

            reviewPopupController = CreatePanel<ReviewPopupController>("ReviewPopup", canvas.transform);
            reviewPopupController.gameObject.SetActive(false);

            chapterCompleteController = CreatePanel<ChapterCompleteScreenController>("ChapterCompleteScreen", canvas.transform);
            chapterCompleteController.Initialize(runtimeFont, theme);
            chapterCompleteController.gameObject.SetActive(false);

            tierCelebrationController = CreatePanel<TierCelebrationScreenController>("TierCelebrationScreen", canvas.transform);
            tierCelebrationController.Initialize(runtimeFont, theme);
            tierCelebrationController.gameObject.SetActive(false);

            chapterSelectController = CreatePanel<ChapterSelectScreenController>("ChapterSelectScreen", canvas.transform);
            chapterSelectController.gameObject.SetActive(false);

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
            AnalyticsManager.LogEvent("screen_viewed", new System.Collections.Generic.Dictionary<string, object> { { "screen", "start" } });
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
            HideFailUpsell();
            hudController.gameObject.SetActive(false);
            countdownController.gameObject.SetActive(false);
            rushWarningController.gameObject.SetActive(false);
            controlFlipController.gameObject.SetActive(false);
            shopUIController.gameObject.SetActive(false);
            pauseScreenController?.gameObject.SetActive(false);
            chapterCompleteController?.gameObject.SetActive(false);
            tierCelebrationController?.gameObject.SetActive(false);
            chapterSelectController?.gameObject.SetActive(false);
            startScreenController.SetState(bestScore, emberBalance, cachedLeaderboardEntries, cachedDailyMissions, cachedChestStatus, cachedDailyChallengeStatus, cachedMissionRerollCost, soundEnabled, vibrationEnabled);
            SetHeat(0f);
            if (staticMenuBackground != null) staticMenuBackground.gameObject.SetActive(true);

            if (bannerAdManager != null)
            {
                if (coinStoreManager != null && coinStoreManager.HasNoAds)
                {
                    bannerAdManager.HideBanner();
                }
                else
                {
                    bannerAdManager.ShowBanner();
                }
            }

            if (economyManager != null && economyManager.ShouldShowReviewPrompt())
            {
                ShowReviewPopup();
            }
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
                // Start screen is visible — push fresh data directly to the popup
                // so the leaderboard refreshes even if it's already open.
                startScreenController.UpdateLeaderboardData(cachedBestScore, cachedLeaderboardEntries);
                RefreshStartScreenState();
            }
            else if (pendingShowStart != null)
            {
                // Splash is still showing. Replace the pending ShowStart so that when
                // the splash ends the start screen will already have the Firebase data.
                var freshLeaderboard = cachedLeaderboardEntries;
                var freshBest = cachedBestScore;
                var prev = pendingShowStart;
                pendingShowStart = () =>
                {
                    cachedBestScore = freshBest;
                    cachedLeaderboardEntries = freshLeaderboard;
                    prev?.Invoke();
                };
            }
        }

        public void ShowHud()
        {
            startScreenController.gameObject.SetActive(false);
            failScreenController.gameObject.SetActive(false);
            HideFailUpsell();
            hudController.gameObject.SetActive(true);
            countdownController.gameObject.SetActive(false);
            rushWarningController.gameObject.SetActive(false);
            shopUIController.gameObject.SetActive(false);
            pauseScreenController?.gameObject.SetActive(false);
            if (staticMenuBackground != null) staticMenuBackground.gameObject.SetActive(false);
            bannerAdManager?.HideBanner();
        }

        public void ShowFail(float score, float bestScore, float runTime, IReadOnlyList<LeaderboardEntry> leaderboardEntries, int emberBalance, int rewardValue, int claimedReward, bool canContinue, bool canClaimDoubleReward, string bestDeltaText, string nextTargetText, string modeSummaryText, int remainingLives, bool canWatchLifeRefillAd, bool canBuyLifeRefill, int lifeRefillCoinCost, bool hasContinueOption, int continueCoinCost, bool showUpsell = false, bool isDailyChallenge = false)
        {
            AnalyticsManager.LogEvent("screen_viewed", new System.Collections.Generic.Dictionary<string, object> { { "screen", "fail" } });
            bool shouldShowUpsell = ShouldShowFailUpsell(showUpsell);
            failScreenController.gameObject.SetActive(true);
            HideFailUpsell();
            failScreenController.SetState(score, bestScore, runTime, leaderboardEntries, emberBalance, rewardValue, claimedReward, canContinue, canClaimDoubleReward, bestDeltaText, nextTargetText, modeSummaryText, remainingLives, canWatchLifeRefillAd, canBuyLifeRefill, lifeRefillCoinCost, hasContinueOption, continueCoinCost, isDailyChallenge);
            hudController.gameObject.SetActive(false);
            countdownController.gameObject.SetActive(false);
            rushWarningController.gameObject.SetActive(false);
            controlFlipController.gameObject.SetActive(false);
            shopUIController.gameObject.SetActive(false);
            pauseScreenController?.gameObject.SetActive(false);
            if (shouldShowUpsell) ShowFailIAPUpsell();
            if (staticMenuBackground != null) staticMenuBackground.gameObject.SetActive(false);
            bannerAdManager?.HideBanner();
        }

        public void ShowTierCelebration(int tierIndex, int bonusEmber, bool isLastChapter, System.Action onContinue)
        {
            startScreenController.gameObject.SetActive(false);
            failScreenController.gameObject.SetActive(false);
            HideFailUpsell();
            hudController.gameObject.SetActive(false);
            countdownController.gameObject.SetActive(false);
            chapterSelectController?.gameObject.SetActive(false);
            chapterCompleteController?.gameObject.SetActive(false);
            tierCelebrationController?.gameObject.SetActive(false);
            tierCelebrationController.gameObject.SetActive(true);
            tierCelebrationController.SetState(tierIndex, bonusEmber, isLastChapter, onContinue);
            SetHeat(0f);
            if (staticMenuBackground != null) staticMenuBackground.gameObject.SetActive(true);
            if (bannerAdManager != null)
            {
                if (coinStoreManager != null && coinStoreManager.HasNoAds) bannerAdManager.HideBanner();
                else bannerAdManager.ShowBanner();
            }
        }

        public void ShowChapterComplete(int chapterIndex, float reachedHeight, float targetHeight,
            int coinsRewarded, bool nextUnlocked, bool isLastChapter,
            System.Action onMenu, System.Action onNextChapter, System.Action onChapterSelect)
        {
            startScreenController.gameObject.SetActive(false);
            failScreenController.gameObject.SetActive(false);
            hudController.gameObject.SetActive(false);
            countdownController.gameObject.SetActive(false);
            chapterSelectController?.gameObject.SetActive(false);
            chapterCompleteController.gameObject.SetActive(true);
            chapterCompleteController.SetState(chapterIndex, reachedHeight, targetHeight, coinsRewarded, nextUnlocked, isLastChapter, onMenu, onNextChapter, onChapterSelect);
            SetHeat(0f);
            if (staticMenuBackground != null) staticMenuBackground.gameObject.SetActive(true);
            if (bannerAdManager != null)
            {
                if (coinStoreManager != null && coinStoreManager.HasNoAds) bannerAdManager.HideBanner();
                else bannerAdManager.ShowBanner();
            }
        }

        public void ShowChapterFail(int chapterIndex, float reachedHeight, float targetHeight, int coinsRewarded, System.Action onReturn)
        {
            startScreenController.gameObject.SetActive(false);
            failScreenController.gameObject.SetActive(false);
            hudController.gameObject.SetActive(false);
            countdownController.gameObject.SetActive(false);
            chapterSelectController?.gameObject.SetActive(false);
            chapterCompleteController.gameObject.SetActive(true);
            chapterCompleteController.SetFailState(chapterIndex, reachedHeight, targetHeight, coinsRewarded, onReturn);
            SetHeat(0f);
            if (staticMenuBackground != null) staticMenuBackground.gameObject.SetActive(true);
            if (bannerAdManager != null)
            {
                if (coinStoreManager != null && coinStoreManager.HasNoAds) bannerAdManager.HideBanner();
                else bannerAdManager.ShowBanner();
            }
        }

        public void ShowChapterSelect(ChapterManager chapterManager, System.Action<int> onChapterSelected)
        {
            startScreenController.gameObject.SetActive(false);
            failScreenController.gameObject.SetActive(false);
            hudController.gameObject.SetActive(false);
            chapterCompleteController?.gameObject.SetActive(false);
            tierCelebrationController?.gameObject.SetActive(false);
            chapterSelectController.gameObject.SetActive(true);
            if (!chapterSelectBuilt)
            {
                chapterSelectController.Initialize(runtimeFont, theme, chapterManager, onChapterSelected,
                    () => ShowStart(cachedBestScore, economyManager?.EmberBalance ?? 0, cachedLeaderboardEntries,
                        cachedDailyMissions, cachedChestStatus, cachedDailyChallengeStatus,
                        cachedMissionRerollCost, cachedSoundEnabled, cachedVibrationEnabled));
                chapterSelectBuilt = true;
            }
            else
            {
                chapterSelectController.Refresh(chapterManager);
            }
            SetHeat(0f);
            if (staticMenuBackground != null) staticMenuBackground.gameObject.SetActive(true);
        }

        public void ShowCountdown(string label, bool isGo, float score, float bestScore, float runTime, int zoneIndex, float lavaGap, float gapDangerNormalized, bool isNewBest, bool showControlsHint, string bestLabel = null)
        {
            startScreenController.gameObject.SetActive(false);
            failScreenController.gameObject.SetActive(false);
            HideFailUpsell();
            hudController.gameObject.SetActive(true);
            hudController.SetValues(score, bestScore, runTime, zoneIndex, lavaGap, gapDangerNormalized, isNewBest, showControlsHint, bestLabel);
            countdownController.gameObject.SetActive(true);
            rushWarningController.gameObject.SetActive(false);
            controlFlipController.gameObject.SetActive(false);
            shopUIController.gameObject.SetActive(false);
            pauseScreenController?.gameObject.SetActive(false);
            countdownController.SetValue(label, isGo);
            if (staticMenuBackground != null) staticMenuBackground.gameObject.SetActive(false);
            bannerAdManager?.HideBanner();
        }

        public void UpdateHud(float score, float bestScore, float runTime, int zoneIndex, float lavaGap, float gapDangerNormalized, bool isNewBest, bool showControlsHint, string bestLabel = null)
        {
            hudController.SetValues(score, bestScore, runTime, zoneIndex, lavaGap, gapDangerNormalized, isNewBest, showControlsHint, bestLabel);
        }

        public void SpawnCoinFloat(int amount)
        {
            if (hudController != null)
            {
                hudController.SpawnCoinFloat(amount, this);
            }
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
            float overlayIntensity = Mathf.Pow(Mathf.Clamp01(intensity), 1.65f);
            color.a = Mathf.Lerp(0f, theme.nearLavaOverlay.a, overlayIntensity);
            heatOverlay.color = color;
        }

        public void ShowPause()
        {
            if (pauseScreenController == null) return;
            pauseScreenController.gameObject.SetActive(true);
            bannerAdManager?.HideBanner();
        }

        public void HidePause()
        {
            if (pauseScreenController == null) return;
            pauseScreenController.gameObject.SetActive(false);
            if (startScreenController != null && startScreenController.gameObject.activeSelf)
            {
                if (coinStoreManager == null || !coinStoreManager.HasNoAds)
                    bannerAdManager?.ShowBanner();
            }
        }

        public void ShowNicknamePopup(Action<string, Action<bool, string>> onConfirm)
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

        public void ShowReviewPopup()
        {
            if (reviewPopupController == null) return;
            
            reviewPopupController.gameObject.SetActive(true);
            reviewPopupController.transform.SetAsLastSibling();
            reviewPopupController.Initialize(runtimeFont, theme, 
                HandleReviewRate, 
                HandleReviewLater, 
                HandleReviewNever);
        }

        private void HandleReviewRate()
        {
            buttonClickSound?.Invoke();
            reviewPopupController.gameObject.SetActive(false);
            economyManager.SetReviewState(ReviewPromptState.Rated);
            
            string url = string.Empty;
#if UNITY_ANDROID
            url = gameConfig.androidStoreUrl;
#elif UNITY_IOS
            url = gameConfig.iosStoreUrl;
#endif
            if (!string.IsNullOrEmpty(url))
            {
                Application.OpenURL(url);
            }
        }

        private void HandleReviewLater()
        {
            buttonClickSound?.Invoke();
            reviewPopupController.gameObject.SetActive(false);
            economyManager.SetReviewState(ReviewPromptState.Dismissed);
        }

        private void HandleReviewNever()
        {
            buttonClickSound?.Invoke();
            reviewPopupController.gameObject.SetActive(false);
            economyManager.SetReviewState(ReviewPromptState.Never);
        }

        public void QueueRewardToast(string title, string subtitle, Color accentColor)
        {
            rewardToastController?.Enqueue(title, subtitle, accentColor);
        }

        private void ShowShop()
        {
            if (economyManager == null || shopUIController == null)
            {
                return;
            }

            shopUIController.gameObject.SetActive(true);
            shopUIController.transform.SetAsLastSibling();
            shopUIController.SetState(economyManager.EmberBalance, economyManager.Skins, economyManager.TowerSkins, coinStoreManager != null ? coinStoreManager.Offers : Array.Empty<CoinPackOffer>(), economyManager);
            bannerAdManager?.HideBanner();
        }

        private void HideShop()
        {
            if (shopUIController != null)
            {
                shopUIController.gameObject.SetActive(false);
            }

            if (startScreenController != null && startScreenController.gameObject.activeSelf)
            {
                if (coinStoreManager == null || !coinStoreManager.HasNoAds)
                    bannerAdManager?.ShowBanner();
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
                        QueueRewardToast(GetBonusChestTitle(), FormatCoinReward(reward), new Color(1f, 0.72f, 0.34f, 1f));
                    }
                    RefreshStartScreenState();
                });
                return;
            }

            int freeReward = economyManager.ClaimFreeDailyChest();
            if (freeReward > 0)
            {
                QueueRewardToast(GetDailyChestTitle(), FormatCoinReward(freeReward), new Color(1f, 0.68f, 0.28f, 1f));
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
                    QueueRewardToast(GetStoreOfflineTitle(), GetCoinPacksUnavailableMessage(), new Color(1f, 0.64f, 0.3f, 1f));
                    return;
                }

                CoinPackPurchaseResult purchaseResult = coinStoreManager.PurchasePack(itemId);
                switch (purchaseResult.status)
                {
                    case CoinPackPurchaseStatus.Succeeded:
                        QueueRewardToast(
                            GetLocalizedOfferToastTitle(purchaseResult.offer),
                            purchaseResult.message,
                            new Color(1f, 0.82f, 0.28f, 1f));
                        break;

                    case CoinPackPurchaseStatus.Unavailable:
                        QueueRewardToast(GetPurchaseUnavailableTitle(), purchaseResult.message, new Color(1f, 0.62f, 0.28f, 1f));
                        break;

                    case CoinPackPurchaseStatus.Pending:
                        QueueRewardToast(GetLocalizedOfferToastTitle(purchaseResult.offer), purchaseResult.message, new Color(1f, 0.82f, 0.28f, 1f));
                        break;

                    default:
                        QueueRewardToast(GetPurchaseFailedTitle(), purchaseResult.message, new Color(1f, 0.56f, 0.3f, 1f));
                        break;
                }

                RefreshStartScreenState();
                shopUIController?.SetState(economyManager.EmberBalance, economyManager.Skins, economyManager.TowerSkins, coinStoreManager.Offers, economyManager);
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

            shopUIController?.SetState(economyManager.EmberBalance, economyManager.Skins, economyManager.TowerSkins, coinStoreManager != null ? coinStoreManager.Offers : Array.Empty<CoinPackOffer>(), economyManager);
        }

        private static readonly string[] UpsellCandidateIds = { "welcome_pack", "no_ads_pack", "bundle_neon_rush", "bundle_frost_reign" };

        private bool ShouldShowFailUpsell(bool requested)
        {
            if (!requested)
            {
                return false;
            }

            int deathCount = Mathf.Max(0, PlayerPrefs.GetInt(FailUpsellDeathCountKey, 0)) + 1;
            PlayerPrefs.SetInt(FailUpsellDeathCountKey, deathCount);
            PlayerPrefs.Save();
            return deathCount % 2 == 0;
        }

        private void HideFailUpsell()
        {
            if (iapUpsellController != null)
            {
                iapUpsellController.gameObject.SetActive(false);
            }
        }

        private void ShowFailIAPUpsell()
        {
            if (iapUpsellController == null || coinStoreManager == null)
            {
                return;
            }

            string[] eligibleCandidateIds = GetEligibleFailUpsellCandidateIds();
            if (eligibleCandidateIds.Length == 0)
            {
                HideFailUpsell();
                return;
            }

            iapUpsellController.Show(coinStoreManager.Offers, eligibleCandidateIds);
        }

        private string[] GetEligibleFailUpsellCandidateIds()
        {
            var eligibleIds = new List<string>(UpsellCandidateIds.Length);
            IReadOnlyList<CoinPackOffer> offers = coinStoreManager != null ? coinStoreManager.Offers : Array.Empty<CoinPackOffer>();

            for (int candidateIndex = 0; candidateIndex < UpsellCandidateIds.Length; candidateIndex++)
            {
                string candidateId = UpsellCandidateIds[candidateIndex];
                for (int offerIndex = 0; offerIndex < offers.Count; offerIndex++)
                {
                    CoinPackOffer offer = offers[offerIndex];
                    if (!string.Equals(offer.id, candidateId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (IsOfferEligibleForUpsell(offer))
                    {
                        eligibleIds.Add(candidateId);
                    }

                    break;
                }
            }

            return eligibleIds.ToArray();
        }

        private bool IsOfferEligibleForUpsell(CoinPackOffer offer)
        {
            if (string.IsNullOrWhiteSpace(offer.id))
            {
                return false;
            }

            if (offer.productType != ProductType.Consumable && offer.owned)
            {
                return false;
            }

            if (offer.kind == StoreOfferKind.NoAds && coinStoreManager != null && coinStoreManager.HasNoAds)
            {
                return false;
            }

            if (economyManager == null)
            {
                return true;
            }

            bool hasLinkedRewards = false;
            bool allLinkedRewardsOwned = true;

            if (!string.IsNullOrWhiteSpace(offer.ballSkinId))
            {
                hasLinkedRewards = true;
                allLinkedRewardsOwned &= economyManager.IsOwnedSkin(offer.ballSkinId);
            }

            if (!string.IsNullOrWhiteSpace(offer.towerSkinId))
            {
                hasLinkedRewards = true;
                allLinkedRewardsOwned &= economyManager.IsOwnedTowerSkin(offer.towerSkinId);
            }

            return !hasLinkedRewards || !allLinkedRewardsOwned;
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
                    QueueRewardToast(GetLocalizedOfferToastTitle(purchaseResult.offer), purchaseResult.message, new Color(1f, 0.82f, 0.28f, 1f));
                    break;
                case CoinPackPurchaseStatus.Pending:
                    QueueRewardToast(GetLocalizedOfferToastTitle(purchaseResult.offer), purchaseResult.message, new Color(1f, 0.82f, 0.28f, 1f));
                    break;
                default:
                    QueueRewardToast(GetPurchaseFailedTitle(), purchaseResult.message, new Color(1f, 0.56f, 0.3f, 1f));
                    break;
            }
        }

        private void TriggerIAPSkinPurchase(string iapProductId)
        {
            if (coinStoreManager == null)
            {
                QueueRewardToast(GetStoreOfflineTitle(), GetPurchaseUnavailableMessage(), new Color(1f, 0.64f, 0.3f, 1f));
                return;
            }

            CoinPackPurchaseResult purchaseResult = coinStoreManager.PurchasePack(iapProductId);
            switch (purchaseResult.status)
            {
                case CoinPackPurchaseStatus.Succeeded:
                    QueueRewardToast(GetLocalizedOfferToastTitle(purchaseResult.offer), purchaseResult.message, new Color(1f, 0.82f, 0.28f, 1f));
                    break;
                case CoinPackPurchaseStatus.Pending:
                    QueueRewardToast(GetLocalizedOfferToastTitle(purchaseResult.offer), purchaseResult.message, new Color(1f, 0.82f, 0.28f, 1f));
                    break;
                default:
                    QueueRewardToast(GetPurchaseFailedTitle(), purchaseResult.message, new Color(1f, 0.56f, 0.3f, 1f));
                    break;
            }

            shopUIController?.SetState(economyManager.EmberBalance, economyManager.Skins, economyManager.TowerSkins, coinStoreManager != null ? coinStoreManager.Offers : Array.Empty<CoinPackOffer>(), economyManager);
        }

        private void HandleMissionReroll()
        {
            if (economyManager == null)
            {
                return;
            }

            if (!economyManager.TryRerollDailyMissions(out int spentCoins))
            {
                QueueRewardToast(GetNotEnoughCoinTitle(), FormatCoinNeeded(economyManager.GetMissionRerollCost()), new Color(1f, 0.62f, 0.28f, 1f));
                RefreshStartScreenState();
                return;
            }

            QueueRewardToast(GetMissionsRefreshedTitle(), FormatCoinSpend(spentCoins), new Color(0.44f, 0.86f, 1f, 1f));
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
                QueueRewardToast(GetShopBoostTitle(), FormatCoinReward(directReward), new Color(0.34f, 0.86f, 0.68f, 1f));
                RefreshStartScreenState();
                shopUIController?.SetState(economyManager.EmberBalance, economyManager.Skins, economyManager.TowerSkins, coinStoreManager != null ? coinStoreManager.Offers : Array.Empty<CoinPackOffer>(), economyManager);
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
                QueueRewardToast(GetShopBoostTitle(), FormatCoinReward(reward), new Color(0.34f, 0.86f, 0.68f, 1f));
                RefreshStartScreenState();
                shopUIController?.SetState(economyManager.EmberBalance, economyManager.Skins, economyManager.TowerSkins, coinStoreManager != null ? coinStoreManager.Offers : Array.Empty<CoinPackOffer>(), economyManager);
            });
        }

        private void HandleCoinStoreRestore()
        {
            if (coinStoreManager == null)
            {
                QueueRewardToast(GetStoreOfflineTitle(), GetRestoreUnavailableMessage(), new Color(1f, 0.64f, 0.3f, 1f));
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

        public void RefreshDailyMissions(IReadOnlyList<DailyMissionState> missions)
        {
            cachedDailyMissions = missions ?? Array.Empty<DailyMissionState>();
            startScreenController?.SetState(cachedBestScore, economyManager != null ? economyManager.EmberBalance : 0, cachedLeaderboardEntries, cachedDailyMissions, cachedChestStatus, cachedDailyChallengeStatus, cachedMissionRerollCost, cachedSoundEnabled, cachedVibrationEnabled);
        }

        private void HandleCoinStoreOffersChanged()
        {
            if (economyManager == null || shopUIController == null || !shopUIController.gameObject.activeSelf)
            {
                return;
            }

            shopUIController.SetState(economyManager.EmberBalance, economyManager.Skins, economyManager.TowerSkins, coinStoreManager != null ? coinStoreManager.Offers : Array.Empty<CoinPackOffer>(), economyManager);
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
                    QueueRewardToast(GetLocalizedOfferToastTitle(result.offer), result.message, new Color(1f, 0.82f, 0.28f, 1f));
                    break;

                case CoinPackPurchaseStatus.Unavailable:
                    QueueRewardToast(GetPurchaseDeferredTitle(), result.message, new Color(1f, 0.72f, 0.34f, 1f));
                    break;

                default:
                    QueueRewardToast(GetPurchaseFailedTitle(), result.message, new Color(1f, 0.56f, 0.3f, 1f));
                    break;
            }

            RefreshStartScreenState();
            if (shopUIController != null && shopUIController.gameObject.activeSelf && economyManager != null)
            {
                shopUIController.SetState(economyManager.EmberBalance, economyManager.Skins, economyManager.TowerSkins, coinStoreManager != null ? coinStoreManager.Offers : Array.Empty<CoinPackOffer>(), economyManager);
            }
        }

        private void HandleCoinStoreRestoreFinished(bool success, string message)
        {
            QueueRewardToast(GetRestoreTitle(success), message, success ? new Color(0.42f, 0.86f, 1f, 1f) : new Color(1f, 0.68f, 0.3f, 1f));
            if (shopUIController != null && shopUIController.gameObject.activeSelf && economyManager != null)
            {
                shopUIController.SetState(economyManager.EmberBalance, economyManager.Skins, economyManager.TowerSkins, coinStoreManager != null ? coinStoreManager.Offers : Array.Empty<CoinPackOffer>(), economyManager);
            }
        }

        private static string GetLocalizedOfferToastTitle(CoinPackOffer offer)
        {
            if (string.IsNullOrWhiteSpace(offer.id) && string.IsNullOrWhiteSpace(offer.productId))
            {
                return GetPurchaseUnavailableTitle();
            }

            return ShopUIController.GetLocalizedOfferTitle(offer);
        }

        private static string GetCoinWord()
        {
            return UILanguage.Translate("COIN", "COIN", "MONEDA");
        }

        private static string FormatCoinReward(int amount)
        {
            return $"+{Mathf.Max(0, amount)} {GetCoinWord()}";
        }

        private static string FormatCoinSpend(int amount)
        {
            return $"-{Mathf.Max(0, amount)} {GetCoinWord()}";
        }

        private static string FormatCoinNeeded(int amount)
        {
            return $"{Mathf.Max(0, amount)} {GetCoinWord()} {UILanguage.Translate("GEREKLI", "NEEDED", "NECESARIAS")}";
        }

        private static string GetBonusChestTitle()
        {
            return UILanguage.Translate("BONUS SANDIK", "BONUS CHEST", "COFRE BONUS");
        }

        private static string GetDailyChestTitle()
        {
            return UILanguage.Translate("GUNLUK SANDIK", "DAILY CHEST", "COFRE DIARIO");
        }

        private static string GetStoreOfflineTitle()
        {
            return UILanguage.Translate("MAGAZA KAPALI", "STORE OFFLINE", "TIENDA DESCONECTADA");
        }

        private static string GetCoinPacksUnavailableMessage()
        {
            return UILanguage.Translate("COIN paketleri kullanilamiyor", "COIN PACKS UNAVAILABLE", "LOS PAQUETES DE MONEDAS NO ESTAN DISPONIBLES");
        }

        private static string GetPurchaseUnavailableTitle()
        {
            return UILanguage.Translate("SATIN ALMA KULLANILAMIYOR", "PURCHASE UNAVAILABLE", "COMPRA NO DISPONIBLE");
        }

        private static string GetPurchaseUnavailableMessage()
        {
            return UILanguage.Translate("SATIN ALMA KULLANILAMIYOR", "PURCHASE UNAVAILABLE", "COMPRA NO DISPONIBLE");
        }

        private static string GetPurchaseFailedTitle()
        {
            return UILanguage.Translate("SATIN ALMA BASARISIZ", "PURCHASE FAILED", "COMPRA FALLIDA");
        }

        private static string GetPurchaseDeferredTitle()
        {
            return UILanguage.Translate("SATIN ALMA BEKLEMEDE", "PURCHASE DEFERRED", "COMPRA APLAZADA");
        }

        private static string GetNotEnoughCoinTitle()
        {
            return UILanguage.Translate("YETERSIZ COIN", "NOT ENOUGH COIN", "NO HAY SUFICIENTES MONEDAS");
        }

        private static string GetMissionsRefreshedTitle()
        {
            return UILanguage.Translate("GOREVLER YENILENDI", "MISSIONS REFRESHED", "MISIONES ACTUALIZADAS");
        }

        private static string GetShopBoostTitle()
        {
            return UILanguage.Translate("MAGAZA BONUSU", "SHOP BOOST", "BONO DE TIENDA");
        }

        private static string GetRestoreUnavailableMessage()
        {
            return UILanguage.Translate("GERI YUKLEME KULLANILAMIYOR", "RESTORE UNAVAILABLE", "RESTAURACION NO DISPONIBLE");
        }

        private static string GetRestoreTitle(bool success)
        {
            return success
                ? UILanguage.Translate("GERI YUKLEME TAMAM", "RESTORE COMPLETE", "RESTAURACION COMPLETA")
                : UILanguage.Translate("GERI YUKLE", "RESTORE", "RESTAURAR");
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
                canvas.sortingOrder = -1;
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
                scaler.matchWidthOrHeight = 0f; // Match Width for Portrait
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

        internal static Text CreateText(string name, Transform parent, Font font, int fontSize, TextAnchor alignment, Color color, UIFontRole role = UIFontRole.Default)
        {
            GameObject textObject = new(name);
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.alignment = alignment;
            text.color = color;
            text.alignByGeometry = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.resizeTextForBestFit = false;
            text.supportRichText = false;
            text.raycastTarget = false;
            text.lineSpacing = 0.92f;
            SetScaledFontSize(text, fontSize, role);
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

            Text text = CreateText($"{name}_Label", image.transform, font, 44, TextAnchor.MiddleCenter, textColor, UIFontRole.Button);
            text.text = label;
            Stretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            StyleToyText(text, new Color(0.12f, 0.2f, 0.42f, 0.72f), new Vector2(1f, -1f), new Color(1f, 1f, 1f, 0.18f), new Vector2(0f, 1f));
            return button;
        }

        internal static Button CreateCandyCloseButton(string name, Transform parent, Font font, int fontSize = 18)
        {
            Button button = CreateButton(name, parent, font, "X", Color.white, Color.white);
            button.transition = Selectable.Transition.ColorTint;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            Image background = button.GetComponent<Image>();
            if (background != null)
            {
                UICandySkin.ApplyCandyButton(background, "out_btn_purple", new Vector4(150f, 160f, 150f, 160f), 350f);
                background.color = Color.white;
                button.targetGraphic = background;
            }

            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = "X";
                label.fontStyle = FontStyle.Bold;
                label.color = Color.white;
                label.resizeTextForBestFit = true;
                SetScaledBestFit(label, Mathf.Max(14, fontSize - 4), fontSize, UIFontRole.Button);
                Stretch(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            return button;
        }

        internal static void BindButton(Button btn, Action action, Action soundCallback = null)
        {
            var guard = btn.gameObject.GetComponent<ClickGuard>() ?? btn.gameObject.AddComponent<ClickGuard>();
            btn.onClick.AddListener(() =>
            {
                if (!guard.IsValidClick()) return;
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
            string key = "btn_flat_" + ColorUtility.ToHtmlStringRGBA(targetColor);
            return CreateFlatSprite(key, 64, 32, targetColor);
        }

        private static Sprite GetThemePanelSprite(Color targetColor)
        {
            string key = "panel_flat_" + ColorUtility.ToHtmlStringRGBA(targetColor);
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

            SetScaledFontSize(text, fontSize, UIFontRole.Button);
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.raycastTarget = false;
            Stretch(text.rectTransform, Vector2.zero, Vector2.one, offsetMin, offsetMax);
            StyleToyText(text, new Color(0.12f, 0.2f, 0.42f, 0.72f), new Vector2(1f, -1f), new Color(1f, 1f, 1f, 0.18f), new Vector2(0f, 1f));
        }

        internal static int ScaleFontSize(Transform context, int baseFontSize, UIFontRole role = UIFontRole.Default)
        {
            float canvasScale = Mathf.Max(0.01f, GetCanvasScaleFactor(context));
            int effectiveBaseFontSize = baseFontSize;
            if ((role & UIFontRole.Button) != 0)
            {
                effectiveBaseFontSize = Mathf.Max(1, effectiveBaseFontSize + ButtonBaseFontSizeOffset);
            }

            float boost = GetResponsiveBoost(effectiveBaseFontSize);
            float roleMultiplier = GetRoleMultiplier(role);
            float mobileBoost = GetMobilePortraitReadingBoost();
            float scaledSize = effectiveBaseFontSize * boost * GlobalFontMultiplier * roleMultiplier * mobileBoost;
            return Mathf.Max(1, Mathf.RoundToInt(scaledSize / canvasScale));
        }

        internal static void SetScaledFontSize(Text text, int baseFontSize, UIFontRole role = UIFontRole.Default)
        {
            if (text == null)
            {
                return;
            }

            EnsureResponsiveTextSize(text).SetFontSize(baseFontSize, role);
        }

        internal static void SetScaledBestFit(Text text, int minSize, int maxSize, UIFontRole role = UIFontRole.Default)
        {
            if (text == null)
            {
                return;
            }

            EnsureResponsiveTextSize(text).SetBestFit(minSize, maxSize, role);
        }

        internal static void SetTextRole(Text text, UIFontRole role)
        {
            if (text == null)
            {
                return;
            }

            EnsureResponsiveTextSize(text).SetRole(role);
        }

        internal static void ApplyPopupTextRoles(Transform root)
        {
            if (root == null)
            {
                return;
            }

            Text[] texts = root.GetComponentsInChildren<Text>(true);
            for (int index = 0; index < texts.Length; index++)
            {
                UIFontRole role = UIFontRole.Popup;
                if (texts[index].GetComponentInParent<Button>() != null)
                {
                    role |= UIFontRole.Button;
                }

                SetTextRole(texts[index], role);
            }
        }

        private static ResponsiveTextSize EnsureResponsiveTextSize(Text text)
        {
            ResponsiveTextSize scaler = text.GetComponent<ResponsiveTextSize>();
            if (scaler == null)
            {
                scaler = text.gameObject.AddComponent<ResponsiveTextSize>();
            }

            return scaler;
        }

        private static float GetResponsiveBoost(int baseFontSize)
        {
            if (baseFontSize <= 10)
            {
                return TinyTextBoost;
            }

            if (baseFontSize <= 12)
            {
                return SmallTextBoost;
            }

            if (baseFontSize <= 16)
            {
                return RegularTextBoost;
            }

            if (baseFontSize <= 20)
            {
                return MediumTextBoost;
            }

            if (baseFontSize <= 32)
            {
                return LargeTextBoost;
            }

            return HugeTextBoost;
        }

        private static float GetRoleMultiplier(UIFontRole role)
        {
            float multiplier = DefaultTextMultiplier;

            if ((role & UIFontRole.Button) != 0)
            {
                multiplier *= ButtonTextMultiplier;
            }

            if ((role & UIFontRole.Popup) != 0)
            {
                multiplier *= PopupTextMultiplier;
            }

            return multiplier;
        }

        private static float GetMobilePortraitReadingBoost()
        {
            if (Screen.height <= Screen.width)
            {
                return 1f;
            }

            int shortEdge = Mathf.Max(1, Mathf.Min(Screen.width, Screen.height));
            if (shortEdge <= 1080)
            {
                return 1.18f;
            }

            if (shortEdge <= 1284)
            {
                return 1.10f;
            }

            if (shortEdge <= 1440)
            {
                return 1.05f;
            }

            return 1f;
        }

        private static float GetCanvasScaleFactor(Transform context)
        {
            CanvasScaler scaler = context != null ? context.GetComponentInParent<CanvasScaler>() : null;
            if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                return 1f;
            }

            float widthScale = Screen.width / Mathf.Max(1f, scaler.referenceResolution.x);
            float heightScale = Screen.height / Mathf.Max(1f, scaler.referenceResolution.y);

            return scaler.screenMatchMode switch
            {
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight => Mathf.Pow(
                    2f,
                    Mathf.Lerp(
                        Mathf.Log(Mathf.Max(0.01f, widthScale), 2f),
                        Mathf.Log(Mathf.Max(0.01f, heightScale), 2f),
                        scaler.matchWidthOrHeight)),
                CanvasScaler.ScreenMatchMode.Expand => Mathf.Min(widthScale, heightScale),
                CanvasScaler.ScreenMatchMode.Shrink => Mathf.Max(widthScale, heightScale),
                _ => 1f,
            };
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
            TextAnchor anchor = TextAnchor.MiddleCenter,
            UIFontRole role = UIFontRole.Default)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>(); t.text = text; t.font = font;
            SetScaledFontSize(t, size, role);
            t.fontStyle = style; t.color = color; t.alignment = anchor; return t;
        }

        // Stretch with default params covers both Stretch(rt) and Stretch(rt, l, r, t, b) call sites.
        public static void Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom); rt.offsetMax = new Vector2(-right, -top);
        }

        public static void BindButton(Button btn, System.Action onClick)
        {
            var guard = btn.gameObject.GetComponent<ClickGuard>() ?? btn.gameObject.AddComponent<ClickGuard>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => { if (guard.IsValidClick()) onClick?.Invoke(); });
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
                15, FontStyle.Bold, Color.white, font, TextAnchor.MiddleCenter, UIFontRole.Button);
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
                14, FontStyle.Bold, new Color(1, 1, 1, 0.70f), font, TextAnchor.MiddleCenter, UIFontRole.Button);
            lbl.resizeTextForBestFit = true;
            SetScaledBestFit(lbl, 12, 14, UIFontRole.Button);
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

    /// <summary>
    /// Prevents accidental button clicks caused by Unity Editor Game View scale slider
    /// dragging leaking pointer events into the game canvas.
    /// Tracks pointer-down position and rejects clicks where the pointer moved too far.
    /// </summary>
    public sealed class ClickGuard : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private const float MaxDragPixels = 20f;
        private Vector2 pointerDownPos;
        private Vector2 pointerUpPos;
        private bool pointerWasDown;
        private float pointerDownTime;

        public void OnPointerDown(PointerEventData eventData)
        {
            pointerDownPos = eventData.position;
            pointerDownTime = Time.unscaledTime;
            pointerWasDown = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pointerUpPos = eventData.position;
        }

        public bool IsValidClick()
        {
            if (!pointerWasDown)
                return false;

            float elapsed = Time.unscaledTime - pointerDownTime;
            float distance = Vector2.Distance(pointerDownPos, pointerUpPos);
            pointerWasDown = false;

            // Reject if pointer was held too long (drag) or moved too far
            return elapsed < 1f && distance < MaxDragPixels;
        }
    }

    // ─── Shop catalog type (pre-redesign) ───────────────────────────────────────
    public enum ShopCatalogType { Coin, Ball, Tower }
}
