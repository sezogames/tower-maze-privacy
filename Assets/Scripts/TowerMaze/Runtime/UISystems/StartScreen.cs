using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TowerMaze
{
    public sealed class StartScreenController : MonoBehaviour
    {
        private EconomyManager economyManager;
        private PlayerProfileManager profileManager;
        private Action buttonClickSound;
        private Font runtimeFont;
        private Text bestScoreText;
        private Text captionText;
        private Text startButtonLabelText;
        private Text shopButtonText;
        private Text missionsButtonText;
        private Text settingsTitleText;
        private Text settingsAudioHeaderText;
        private Text languageLabelText;
        private Text missionsTitleText;
        private Text missionsSubtitleText;
        private Text leaderboardTitleText;
        private Text leaderboardCloseButtonText;
        private RectTransform startButtonRt;
        private RectTransform bestScoreChipRt;
        private RectTransform startLifeBarRt;
        private RectTransform profileAvatarRt;
        private RectTransform dailyChallengeButtonRt;
        private RectTransform endlessButtonRt;
        private RectTransform leaderboardButtonRt;
        private RectTransform settingsButtonRt;
        private RectTransform logoRt;
        private readonly List<Text> logoTextLayers = new();
        private RectTransform secondaryRowRt;
        private Button dailyChallengeButton;
        private Image dailyChallengeButtonImage;
        private Text dailyChallengeTitleText;
        #pragma warning disable CS0414
        private Text dailyChallengeSubtitleText;
        #pragma warning restore CS0414
        private LifeBarUI startLifeBarUI;
        private GameObject settingsPanel;
        private RectTransform settingsPanelRt;
        private CanvasGroup settingsPanelCanvasGroup;
        private Coroutine settingsPanelRoutine;
        private Text settingsSoundLabelText;
        private Text settingsVibrationLabelText;
        private Text settingsSoundOnText;
        private Text settingsSoundOffText;
        private Text settingsVibOnText;
        private Text settingsVibOffText;
        private Image settingsSoundToggleBg;
        private Image settingsVibToggleBg;
        private Image settingsSoundIcon;
        private Image settingsVibIcon;
        private Text settingsSoundToggleText;
        private Text settingsVibToggleText;
        private Text privacyButtonText;
        private Text graphicsQualityButtonText;
        private Image profileAvatarImage;
        private Image profileAvatarFrameImage;
        private Button profileAvatarButton;
        private GameObject privacyOverlay;
        private RectTransform privacySheet;
        private Text privacyTitleText;
        private Text privacySubtitleText;
        private Text privacyBodyText;
        private RectTransform privacyBodyRect;
        private ScrollRect privacyScrollRect;
        private bool cachedSoundEnabled;
        private bool cachedVibrationEnabled;
        private Coroutine pulseCoroutine;
        private GameObject leaderboardOverlay;
        private RectTransform leaderboardSheet;
        private RectTransform leaderboardContentRect;
        private ScrollRect leaderboardScrollRect;
        private Text leaderboardSubtitleText;
        private GameObject missionsOverlay;
        private RectTransform missionsSheet;
        private Text missionCountdownText;
        private Coroutine missionCountdownRoutine;
        private System.Action<string> _onClaimMission;
        private GameObject[] missionClaimBtns;
        private string[] missionIds;
        private Text[] missionTitleTexts;
        private Text[] missionProgressTexts;
        private Image[] missionProgressFills;
        private Text[] missionRewardTexts;
        private Image[] missionRewardBackgrounds;
        private Button[] missionRewardButtons;
        private Text[] missionStatusTexts;
        private Text[] missionClaimButtonTexts;
        private GameObject dailyChallengePopupOverlay;
        private RectTransform dailyChallengePopupSheet;
        private Text dailyChallengePopupPlayLabel;
        private Button dailyChallengePopupPlayButton;
        private Image dailyChallengePopupPlayImage;
        private Text dailyChallengePopupTargetText;
        private Text dailyChallengePopupRewardText;
        private Text dailyChallengePopupModifierText;
        private Text dailyChallengePopupStatusText;
        private Text dailyChallengePopupDisclaimerText;
        private Action storedOnPlayDailyChallenge;
        private Action storedOnPlayChapter;
        private Action storedOnPlayEndless;
        private Action storedOnShowChapters;
        private Action storedOnOpenProfile;
        private ChapterManager cachedChapterManager;
        private readonly List<Outline> leaderboardRowOutlines = new();
        private readonly List<Image> leaderboardRowBgs = new();
        private readonly List<Text> leaderboardRankTexts = new();
        private readonly List<Text> leaderboardNameTexts = new();
        private readonly List<Text> leaderboardScoreTexts = new();
        private readonly List<Image> leaderboardAvatarIcons = new();
        private readonly List<Image> leaderboardAvatarFrames = new();
        private readonly List<Image> leaderboardAvatarBackers = new();
        private List<Sprite> profileAvatarSprites;
        private readonly string[] supportedLanguageCodes = UILanguage.SupportedCodes;
        private Image[] languageChipBackgrounds;
        private Text[] languageChipTexts;
        private float cachedBestScoreValue;
        private string cachedOwnRankText = "--";
        private DailyChallengeStatus cachedDailyChallengeStatus;
        private Coroutine ownRowPulseRoutine;
        private IReadOnlyList<LeaderboardEntry> cachedLeaderboardEntries = Array.Empty<LeaderboardEntry>();
        // Cached chapter-mode leaderboard rows (height field carries chapter number).
        private IReadOnlyList<LeaderboardEntry> cachedChapterLeaderboardEntries = Array.Empty<LeaderboardEntry>();
        private enum LeaderboardMode { Endless, Chapter }
        private LeaderboardMode currentLeaderboardMode = LeaderboardMode.Endless;
        private Image leaderboardTabEndlessBg;
        private Image leaderboardTabChapterBg;
        private Text leaderboardTabEndlessLabel;
        private Text leaderboardTabChapterLabel;
        private IReadOnlyList<DailyMissionState> cachedDailyMissions = Array.Empty<DailyMissionState>();
        private static readonly Color LogoCandyChipActive = new Color(0.06f, 0.73f, 0.51f, 1f);   // Jelly Green (UIStyle.Owned)
        private static readonly Color LogoCandyChipInactive = new Color(0.94f, 0.27f, 0.27f, 1f); // Jelly Red (UIStyle.Danger)
        private static readonly Color LogoCandyChipInactiveText = new Color(1f, 1f, 1f, 0.85f);
        private static readonly Color LogoCandyAqua = new(0.28f, 0.64f, 0.88f, 1f);
        private static readonly Color LogoCandyMint = new(0.40f, 0.79f, 0.57f, 1f);
        private static readonly Color LogoCandyOwnRowTint = new(0.64f, 0.42f, 0.92f, 1f);
        private static readonly Color LogoCandyRewardTint = new(1f, 0.94f, 0.80f, 1f);
        private static readonly Color SheetOverlayTint = new(UIStyle.MenuBg.r, UIStyle.MenuBg.g, UIStyle.MenuBg.b, 0.58f);
        private static readonly Color SheetPanelTint = new(0.055f, 0.022f, 0.130f, 0.985f);
        private static readonly Color SheetSectionTint = new(0.125f, 0.048f, 0.235f, 0.94f);
        private static readonly Color SheetWarmTint = new(1.000f, 0.420f, 0.070f, 0.96f);
        private static readonly Color SheetTitleRibbonTint = new(0.235f, 0.074f, 0.390f, 0.96f);
        private static readonly Color SheetTitleTextTint = new(1f, 1f, 1f, 1f);
        private static readonly Color SheetTitleShadowTint = new(1f, 0.84f, 0.42f, 0.26f);
        private static readonly Color SheetTitleOutlineTint = new(0.24f, 0.10f, 0.44f, 0.84f);
        private static readonly Color SheetChromeOutlineTint = new(0.36f, 0.22f, 0.58f, 0.30f);
        private static readonly Color SheetChromeShadowTint = new(0.08f, 0.04f, 0.15f, 0.24f);
        private static readonly Color LeaderboardRowTint = new(0.92f, 0.84f, 0.95f, 0.99f);
        private static readonly Color LeaderboardPremiumOverlayTint = new(0.006f, 0.002f, 0.030f, 0.76f);
        private static readonly Color LeaderboardPremiumPanelFallbackTint = new(0.028f, 0.018f, 0.084f, 0.985f);
        private static readonly Color LeaderboardPremiumGold = new(1f, 0.80f, 0.28f, 1f);
        private static readonly Color LeaderboardPremiumCyan = new(1.00f, 0.64f, 0.18f, 1f);
        private static readonly Color LeaderboardPremiumText = new(1f, 0.94f, 0.82f, 1f);
        private static readonly Color LeaderboardPremiumMutedText = new(0.78f, 0.67f, 0.86f, 0.86f);
        private static readonly Color LeaderboardPremiumRowTint = new(0.135f, 0.045f, 0.245f, 0.92f);
        private static readonly Color LeaderboardPremiumEmptyRowTint = new(0.070f, 0.026f, 0.135f, 0.70f);
        private static readonly Color LeaderboardPremiumOwnRowTint = new(0.280f, 0.065f, 0.330f, 0.96f);
        private static readonly Color LeaderboardPremiumActiveTabTint = new(1.000f, 0.365f, 0.080f, 0.96f);
        private static readonly Color LeaderboardPremiumInactiveTabTint = new(0.145f, 0.055f, 0.255f, 0.90f);
        private const float LeaderboardRowHeight = 54f;
        private const float LeaderboardRowSpacing = 5f;
        private const int LeaderboardContentBottomPadding = 8;
        private static Sprite leaderboardAvatarMaskSprite;
        private static Sprite leaderboardPremiumModalSprite;

        public void Initialize(Font font, ThemeDefinition theme, EconomyManager economy,
            Action onPlay, Action onPlayDailyChallenge, Action onOpenShop, Action onClaimChest,
            Action onToggleSound, Action onToggleVibration, Action onRerollMissions,
            Action onButtonClick = null, Action<string> onClaimMission = null,
            Action onPlayChapter = null, Action onPlayEndless = null, Action onShowChapters = null,
            Action onOpenProfile = null, ChapterManager chapterManager = null)
        {
            storedOnPlayChapter = onPlayChapter ?? onPlay;
            storedOnPlayEndless = onPlayEndless ?? onPlay;
            storedOnShowChapters = onShowChapters;
            storedOnOpenProfile = onOpenProfile;
            cachedChapterManager = chapterManager;
            economyManager = economy;
            buttonClickSound = onButtonClick;
            runtimeFont = font;
            _onClaimMission = onClaimMission;
            UILanguage.EnsureDefaultLanguage();

            // ── Full-screen background ─────────────────────────────────────────────
            Image bg = UIManager.CreateImage("StartBg", transform, UIStyle.MenuBg);
            UIManager.Stretch(bg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject profileAvatarGo = CreateProfileAvatarEntry(transform);
            profileAvatarRt = profileAvatarGo.GetComponent<RectTransform>();

            GameObject lifeBarCardGo = new GameObject("StartLifeBarCard");
            lifeBarCardGo.transform.SetParent(transform, false);
            Image startLifeBarCard = lifeBarCardGo.AddComponent<Image>();
            ApplyMainPremiumSurface(startLifeBarCard, "main_premium_button", "out_btn_purple", preserveAspect: false);
            
            startLifeBarCard.rectTransform.anchorMin = new Vector2(0.04f, 0.90f);
            startLifeBarCard.rectTransform.anchorMax = new Vector2(0.36f, 0.99f);
            startLifeBarCard.rectTransform.offsetMin = startLifeBarCard.rectTransform.offsetMax = Vector2.zero;
            startLifeBarRt = startLifeBarCard.rectTransform;
            startLifeBarUI = startLifeBarCard.gameObject.AddComponent<LifeBarUI>();
            startLifeBarUI.Initialize(font);
            startLifeBarUI.SetCompactHeaderLayout(true);
            startLifeBarUI.SetEconomyManager(economyManager);
            WireProfileAvatarEvents();
            RefreshProfileAvatarEntry();
            storedOnPlayDailyChallenge = onPlayDailyChallenge;
            GameObject dailyChallengeButtonGo = CreateDailyChallengeButton(transform, font,
                ShowDailyChallengePopup, buttonClickSound);
            dailyChallengeButtonRt = dailyChallengeButtonGo.GetComponent<RectTransform>();

            // ── Top bar ───────────────────────────────────────────────────────────
            // Leaderboard icon button (🏅)
            Texture2D flagTex = Resources.Load<Texture2D>("TowerMaze/UITheme/out_icon_flag");
            Sprite flagSprite = flagTex != null ? Sprite.Create(flagTex, new Rect(0, 0, flagTex.width, flagTex.height), new Vector2(0.5f, 0.5f), 100f) : null;
            var lbBtnGo = CreateCandyIconCircle("LeaderboardBtn", transform, flagSprite,
                ShowLeaderboardSheet, buttonClickSound);
            var lbRt = lbBtnGo.GetComponent<RectTransform>();
            lbRt.anchorMin = new Vector2(0.70f, 0.89f);
            lbRt.anchorMax = new Vector2(0.84f, 0.99f);
            lbRt.offsetMin = lbRt.offsetMax = Vector2.zero;
            leaderboardButtonRt = lbRt;

            // Settings icon button (⚙)
            Texture2D gearTex = Resources.Load<Texture2D>("TowerMaze/UITheme/out_icon_gear");
            Sprite gearSprite = gearTex != null ? Sprite.Create(gearTex, new Rect(0, 0, gearTex.width, gearTex.height), new Vector2(0.5f, 0.5f), 100f) : null;
            var settingsBtnGo = CreateCandyIconCircle("SettingsBtn", transform, gearSprite,
                ShowSettingsPanel, buttonClickSound);
            var settingsRt = settingsBtnGo.GetComponent<RectTransform>();
            settingsRt.anchorMin = new Vector2(0.86f, 0.89f);
            settingsRt.anchorMax = new Vector2(1.00f, 0.99f);
            settingsRt.offsetMin = settingsRt.offsetMax = Vector2.zero;
            settingsButtonRt = settingsRt;
            lbBtnGo.transform.SetAsLastSibling();
            settingsBtnGo.transform.SetAsLastSibling();

            // ── Logo ──────────────────────────────────────────────────────────────
            GameObject logoGroup = new GameObject("Logo");
            logoGroup.transform.SetParent(transform, false);
            logoRt = logoGroup.AddComponent<RectTransform>();
            logoRt.anchorMin = new Vector2(-0.05f, 0.46f);
            logoRt.anchorMax = new Vector2(1.05f, 0.96f);
            logoRt.offsetMin = logoRt.offsetMax = Vector2.zero;

            Image logoImage = logoGroup.AddComponent<Image>();
            Texture2D logoTex = Resources.Load<Texture2D>("TowerMaze/UITheme/CandyTowerMazeLogo");
            if (logoTex != null)
            {
                logoImage.sprite = Sprite.Create(logoTex, new Rect(0, 0, logoTex.width, logoTex.height), new Vector2(0.5f, 0.5f), 100f);
                logoImage.preserveAspect = true;
            }

            // ── START button (Candy Orange) ───────────
            GameObject startBtnGo = new GameObject("StartButton");
            startBtnGo.transform.SetParent(transform, false);
            Button startBtn = startBtnGo.AddComponent<Button>();
            Image startImg = startBtnGo.AddComponent<Image>();
            ApplyMainPremiumSurface(startImg, "main_premium_cta", "out_btn_orange", preserveAspect: false);
            startBtn.targetGraphic = startImg;
            startButtonRt = startBtnGo.GetComponent<RectTransform>();
            startButtonRt.anchorMin = new Vector2(0.12f, 0.44f);
            startButtonRt.anchorMax = new Vector2(0.88f, 0.58f);
            startButtonRt.offsetMin = startButtonRt.offsetMax = Vector2.zero;
            Text startLbl = UIManager.CreateText("Label", startBtnGo.transform, font, 24, TextAnchor.MiddleCenter, Color.white, UIFontRole.Button);
            startLbl.fontStyle = FontStyle.Bold;
            startLbl.color = Color.white;
            startLbl.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(startLbl, 18, 24, UIFontRole.Button);
            startLbl.text = GetChapterButtonLabel();
            UIManager.Stretch(startLbl.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Shadow startLabelShadow = startLbl.gameObject.AddComponent<Shadow>();
            startLabelShadow.effectColor = new Color(0.18f, 0.06f, 0.28f, 0.34f);
            startLabelShadow.effectDistance = new Vector2(0f, -2f);
            Outline startLabelOutline = startLbl.gameObject.AddComponent<Outline>();
            startLabelOutline.effectColor = new Color(1f, 0.95f, 0.82f, 0.18f);
            startLabelOutline.effectDistance = new Vector2(1f, -1f);
            UIManager.BindButton(startBtn,
                () => { StartCoroutine(UIStyle.ButtonPress(startButtonRt)); storedOnPlayChapter?.Invoke(); }, null);
            startButtonLabelText = startBtn.GetComponentInChildren<Text>();


            // ── Secondary row: [SHOP] [MISSIONS] ─────────────
            var secondaryRow = new GameObject("SecondaryRow");
            secondaryRow.transform.SetParent(transform, false);
            var rowCg = secondaryRow.AddComponent<CanvasGroup>();
            rowCg.alpha = 1.00f; // opaque to maintain glossy candy colors
            var rowRt = secondaryRow.AddComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0.15f, 0.30f);
            rowRt.anchorMax = new Vector2(0.85f, 0.38f);
            rowRt.offsetMin = rowRt.offsetMax = Vector2.zero;
            secondaryRowRt = rowRt;

            var shopBtnGo = CreateSecondaryButton("ShopBtn", secondaryRow.transform, font, "SHOP",
                () => onOpenShop?.Invoke(), buttonClickSound);
            var missionsBtnGo = CreateSecondaryButton("MissionsBtn", secondaryRow.transform, font,
                "MISSIONS", ShowMissionsSheet, buttonClickSound);
            shopButtonText = shopBtnGo.GetComponentInChildren<Text>();
            missionsButtonText = missionsBtnGo.GetComponentInChildren<Text>();
            LayoutTwoChildren(rowRt, shopBtnGo.GetComponent<RectTransform>(),
                missionsBtnGo.GetComponent<RectTransform>(), 8f);

            // ── ENDLESS button (below Daily Challenge, same candy style) ──────
            GameObject endlessButtonGo = CreateEndlessButton(transform, font,
                () => storedOnPlayEndless?.Invoke(), buttonClickSound);
            endlessButtonRt = endlessButtonGo.GetComponent<RectTransform>();

            // ── Settings panel (built once, starts offscreen) ─────────────────
            BuildSettingsPanel(font, onToggleSound, onToggleVibration);

            // ── Bottom sheets (leaderboard + missions) ────────────────────────
            BuildLeaderboardSheet(font);
            BuildMissionsSheet(font);
            BuildPrivacyPolicySheet(font);
            BuildDailyChallengePopup(font);
            ApplyPortraitLayout();
            ApplyLocalizedTexts();
            UpdateMissionCountdown(GetTimeUntilNextLocalDay());
            EnsureMissionCountdownTicker();
        }

        private void OnEnable()
        {
            if (startButtonRt == null) return;
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            pulseCoroutine = StartCoroutine(UIStyle.Pulse(startButtonRt, 1f, 1.05f, 1.4f));
            WireProfileAvatarEvents();
            RefreshProfileAvatarEntry();
            ApplyPortraitLayout();
            ApplyLocalizedTexts();
            UpdateMissionCountdown(GetTimeUntilNextLocalDay());
            EnsureMissionCountdownTicker();
        }

        private void OnDisable()
        {
            if (pulseCoroutine != null) { StopCoroutine(pulseCoroutine); pulseCoroutine = null; }
            if (settingsPanelRoutine != null) { StopCoroutine(settingsPanelRoutine); settingsPanelRoutine = null; }
            if (missionCountdownRoutine != null) { StopCoroutine(missionCountdownRoutine); missionCountdownRoutine = null; }
        }

        private void OnDestroy()
        {
            if (profileManager != null)
            {
                profileManager.ProfileChanged -= RefreshProfileAvatarEntry;
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            ApplyPortraitLayout();
        }

        public void SetState(float bestScore, int emberBalance,
            IReadOnlyList<LeaderboardEntry> leaderboardEntries,
            IReadOnlyList<DailyMissionState> dailyMissions,
            DailyChestStatus chestStatus, DailyChallengeStatus challengeStatus,
            int missionRerollCost, bool soundEnabled, bool vibrationEnabled)
        {
            cachedBestScoreValue = bestScore;
            cachedLeaderboardEntries = leaderboardEntries ?? Array.Empty<LeaderboardEntry>();
            cachedDailyMissions = dailyMissions ?? Array.Empty<DailyMissionState>();
            cachedDailyChallengeStatus = challengeStatus;
            if (bestScoreText != null)
                bestScoreText.text = $"\U0001f3c6 {bestScore:0}m";
            if (captionText != null)
                captionText.text = FormatBestCaption(bestScore);

            cachedSoundEnabled = soundEnabled;
            cachedVibrationEnabled = vibrationEnabled;
            startLifeBarUI?.SetEconomyManager(economyManager);
            RefreshLanguageTabs();
            ApplyLocalizedTexts();
            RefreshLeaderboardRows(resetPosition: false);

            RefreshMissionCards();

            UpdateMissionCountdown(GetTimeUntilNextLocalDay());
            EnsureMissionCountdownTicker();
            if (startButtonLabelText != null)
                startButtonLabelText.text = GetChapterButtonLabel();
        }

        public void SetChapterManager(ChapterManager chapterManager)
        {
            cachedChapterManager = chapterManager;
            if (startButtonLabelText != null)
                startButtonLabelText.text = GetChapterButtonLabel();
        }

        private string GetChapterButtonLabel()
        {
            if (cachedChapterManager != null)
            {
                int n = cachedChapterManager.UnlockedUpTo;
                return string.Format(UILanguage.Translate("BÖLÜM {0}", "LEVEL {0}", "NIVEL {0}"), n);
            }
            return UILanguage.Translate("BAŞLA", "START", "INICIO");
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private GameObject CreateIconCircle(string name, Transform parent, Font font,
            string icon, Action onClick, Action sound)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            SetupCandyOrb(img);
            var btn = go.AddComponent<Button>();
            UIManager.BindButton(btn, () => {
                StartCoroutine(UIStyle.ButtonPress(go.GetComponent<RectTransform>())); onClick?.Invoke();
            }, sound);
            var lbl = UIManager.CreateText("Icon", go.transform, font, 16,
                TextAnchor.MiddleCenter, UIStyle.TextPrimary);
            lbl.text = icon ?? string.Empty;
            UIManager.Stretch(lbl.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return go;
        }

        private GameObject CreateCandyIconCircle(string name, Transform parent,
            Sprite iconSprite, Action onClick, Action sound)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            ApplyMainPremiumSurface(img, "main_premium_icon_frame", "logo_jelly_orb", preserveAspect: true);
            var btn = go.AddComponent<Button>();
            UIManager.BindButton(btn, () => {
                StartCoroutine(UIStyle.ButtonPress(go.GetComponent<RectTransform>())); onClick?.Invoke();
            }, sound);
            if (iconSprite != null)
            {
                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(go.transform, false);
                var iconImg = iconGo.AddComponent<Image>();
                iconImg.sprite = iconSprite;
                iconImg.preserveAspect = true;
                UIManager.Stretch(iconGo.GetComponent<RectTransform>(), new Vector2(0.24f, 0.24f), new Vector2(0.76f, 0.76f), Vector2.zero, Vector2.zero);
            }
            return go;
        }

        private GameObject CreateProfileAvatarEntry(Transform parent)
        {
            GameObject go = new GameObject("ProfileAvatarEntry");
            go.transform.SetParent(parent, false);
            profileAvatarRt = go.AddComponent<RectTransform>();

            Image hitArea = go.AddComponent<Image>();
            hitArea.color = new Color(1f, 1f, 1f, 0.001f);
            hitArea.raycastTarget = true;

            profileAvatarButton = go.AddComponent<Button>();
            profileAvatarButton.transition = Selectable.Transition.None;
            profileAvatarButton.targetGraphic = hitArea;
            UIManager.BindButton(profileAvatarButton, () =>
            {
                StartCoroutine(UIStyle.ButtonPress(profileAvatarRt));
                storedOnOpenProfile?.Invoke();
            }, buttonClickSound);

            GameObject frameGo = new GameObject("PremiumFrame");
            frameGo.transform.SetParent(go.transform, false);
            profileAvatarFrameImage = frameGo.AddComponent<Image>();
            ApplyMainPremiumSurface(profileAvatarFrameImage, "main_premium_avatar_frame", "frame_gold_premium", preserveAspect: true);
            profileAvatarFrameImage.raycastTarget = false;
            UIManager.Stretch(profileAvatarFrameImage.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject avatarGo = new GameObject("SelectedAvatar");
            avatarGo.transform.SetParent(go.transform, false);
            profileAvatarImage = avatarGo.AddComponent<Image>();
            profileAvatarImage.preserveAspect = true;
            profileAvatarImage.raycastTarget = false;
            UIManager.Stretch(profileAvatarImage.rectTransform, new Vector2(0.145f, 0.130f), new Vector2(0.855f, 0.835f), Vector2.zero, Vector2.zero);
            avatarGo.transform.SetAsLastSibling();
            return go;
        }

        private void WireProfileAvatarEvents()
        {
            profileManager = profileManager != null ? profileManager : PlayerProfileManager.Instance ?? FindAnyObjectByType<PlayerProfileManager>();
            if (profileManager != null)
            {
                profileManager.ProfileChanged -= RefreshProfileAvatarEntry;
                profileManager.ProfileChanged += RefreshProfileAvatarEntry;
            }
        }

        private void RefreshProfileAvatarEntry()
        {
            if (profileAvatarImage == null)
            {
                return;
            }

            profileManager = profileManager != null ? profileManager : PlayerProfileManager.Instance ?? FindAnyObjectByType<PlayerProfileManager>();
            EnsureProfileAvatarSprites();
            Sprite selectedSprite = null;
            if (profileManager != null && profileAvatarSprites != null && profileAvatarSprites.Count > 0)
            {
                int selectedIndex = Mathf.Clamp(profileManager.SelectedAvatarIndex, 0, profileAvatarSprites.Count - 1);
                selectedSprite = profileAvatarSprites[selectedIndex];
            }

            selectedSprite ??= Resources.Load<Sprite>("TowerMaze/UITheme/icon_avatar_default");
            profileAvatarImage.sprite = selectedSprite;
            profileAvatarImage.color = selectedSprite != null ? Color.white : new Color(1f, 1f, 1f, 0.22f);
            profileAvatarImage.enabled = selectedSprite != null;
        }



        private GameObject CreateSecondaryButton(string name, Transform parent, Font font,
            string label, Action onClick, Action sound)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            ApplyMainPremiumSurface(img, "main_premium_button", "out_btn_purple", preserveAspect: false);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            UIManager.BindButton(btn, () => {
                StartCoroutine(UIStyle.ButtonPress(go.GetComponent<RectTransform>())); onClick?.Invoke();
            }, sound);
            var lbl = UIManager.CreateText("Label", go.transform, font, 17,
                TextAnchor.MiddleCenter, Color.white, UIFontRole.Button);
            lbl.text = label;
            lbl.fontStyle = FontStyle.Bold;
            lbl.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(lbl, 13, 18, UIFontRole.Button);
            UIManager.Stretch(lbl.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(14f, 0f), new Vector2(-14f, 0f));
            Shadow labelShadow = lbl.gameObject.AddComponent<Shadow>();
            labelShadow.effectColor = new Color(0.14f, 0.04f, 0.20f, 0.48f);
            labelShadow.effectDistance = new Vector2(0f, -2f);
            Outline labelOutline = lbl.gameObject.AddComponent<Outline>();
            labelOutline.effectColor = new Color(1f, 0.94f, 0.70f, 0.22f);
            labelOutline.effectDistance = new Vector2(1f, -1f);

            return go;
        }

        private GameObject CreateDailyChallengeButton(Transform parent, Font font, Action onClick, Action sound)
        {
            GameObject go = new GameObject("DailyChallengeButton");
            go.transform.SetParent(parent, false);

            dailyChallengeButtonImage = go.AddComponent<Image>();
            ApplyMainPremiumSurface(dailyChallengeButtonImage, "main_premium_button", "out_btn_purple", preserveAspect: false);

            dailyChallengeButton = go.AddComponent<Button>();
            dailyChallengeButton.targetGraphic = dailyChallengeButtonImage;
            UIManager.BindButton(dailyChallengeButton, () =>
            {
                StartCoroutine(UIStyle.ButtonPress(go.GetComponent<RectTransform>()));
                onClick?.Invoke();
            }, sound);

            dailyChallengeTitleText = UIManager.CreateText("Title", go.transform, font, 17, TextAnchor.MiddleCenter, Color.white, UIFontRole.Button);
            dailyChallengeTitleText.fontStyle = FontStyle.Bold;
            dailyChallengeTitleText.resizeTextForBestFit = true;
            dailyChallengeTitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            dailyChallengeTitleText.verticalOverflow = VerticalWrapMode.Overflow;
            UIManager.SetScaledBestFit(dailyChallengeTitleText, 12, 17, UIFontRole.Button);
            UIManager.Stretch(dailyChallengeTitleText.rectTransform, new Vector2(0.08f, 0.15f), new Vector2(0.92f, 0.85f), Vector2.zero, Vector2.zero);
            Shadow titleShadow = dailyChallengeTitleText.gameObject.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0.14f, 0.05f, 0.20f, 0.44f);
            titleShadow.effectDistance = new Vector2(0f, -3f);

            dailyChallengeSubtitleText = null;

            RefreshDailyChallengeButton();
            return go;
        }

        private GameObject CreateEndlessButton(Transform parent, Font font, Action onClick, Action sound)
        {
            GameObject go = new GameObject("EndlessButton");
            go.transform.SetParent(parent, false);

            Image img = go.AddComponent<Image>();
            ApplyMainPremiumSurface(img, "main_premium_button", "out_btn_purple", preserveAspect: false);

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            UIManager.BindButton(btn, () =>
            {
                StartCoroutine(UIStyle.ButtonPress(go.GetComponent<RectTransform>()));
                onClick?.Invoke();
            }, sound);

            Text lbl = UIManager.CreateText("Title", go.transform, font, 17, TextAnchor.MiddleCenter, Color.white, UIFontRole.Button);
            lbl.fontStyle = FontStyle.Bold;
            lbl.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(lbl, 12, 17, UIFontRole.Button);
            UIManager.Stretch(lbl.rectTransform, new Vector2(0.08f, 0.15f), new Vector2(0.92f, 0.85f), Vector2.zero, Vector2.zero);
            lbl.text = UILanguage.Translate("Endless Mode", "Endless Mode", "Endless Mode");
            Shadow shadow = lbl.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.14f, 0.05f, 0.20f, 0.44f);
            shadow.effectDistance = new Vector2(0f, -3f);

            return go;
        }

        private static void LayoutTwoChildren(RectTransform parent, RectTransform a,
            RectTransform b, float gapPx)
        {
            float gapFrac = gapPx / 360f;
            a.anchorMin = new Vector2(0f, 0f);
            a.anchorMax = new Vector2(0.5f - gapFrac / 2f, 1f);
            a.offsetMin = a.offsetMax = Vector2.zero;
            b.anchorMin = new Vector2(0.5f + gapFrac / 2f, 0f);
            b.anchorMax = new Vector2(1f, 1f);
            b.offsetMin = b.offsetMax = Vector2.zero;
        }

        private RectTransform CreateLogoWordContainer(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject wordObject = new GameObject(name);
            wordObject.transform.SetParent(parent, false);
            RectTransform wordRect = wordObject.AddComponent<RectTransform>();
            wordRect.anchorMin = anchorMin;
            wordRect.anchorMax = anchorMax;
            wordRect.offsetMin = wordRect.offsetMax = Vector2.zero;
            return wordRect;
        }

        private void CreateLogoWordLayers(string namePrefix, RectTransform parent, Font font, string textValue,
            Color faceColor, Color depthColor, Color highlightColor, TextAnchor alignment)
        {
            CreateLogoLayer(namePrefix + "Depth", parent, font, textValue, depthColor, new Vector2(6.5f, -6.5f), alignment);
            CreateLogoLayer(namePrefix + "Highlight", parent, font, textValue, highlightColor, new Vector2(-2f, 2f), alignment);

            Text face = CreateLogoLayer(namePrefix + "Face", parent, font, textValue, faceColor, Vector2.zero, alignment);
            Shadow faceShadow = face.gameObject.AddComponent<Shadow>();
            faceShadow.effectColor = new Color(0.10f, 0.04f, 0.17f, 0.56f);
            faceShadow.effectDistance = new Vector2(0f, -6f);

            Outline faceOutline = face.gameObject.AddComponent<Outline>();
            faceOutline.effectColor = new Color(1f, 0.95f, 0.78f, 0.30f);
            faceOutline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private Text CreateLogoLayer(string name, RectTransform parent, Font font, string textValue, Color color, Vector2 offset, TextAnchor alignment)
        {
            Text label = UIManager.CreateText(name, parent, font, 56, alignment, color);
            label.text = textValue;
            label.fontStyle = FontStyle.Bold;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.resizeTextForBestFit = false;
            label.lineSpacing = 0.86f;
            Vector2 leftInset = alignment == TextAnchor.MiddleLeft ? new Vector2(4f, 0f) : Vector2.zero;
            UIManager.Stretch(label.rectTransform, Vector2.zero, Vector2.one, leftInset, Vector2.zero);
            label.rectTransform.anchoredPosition = offset;
            logoTextLayers.Add(label);
            return label;
        }

        private void SetupCandyButton(Image img, string textureName, Vector4 slice, float ppu = 100f)
        {
            UICandySkin.ApplyCandyButton(img, textureName, slice, ppu);
        }

        private void ApplyMainPremiumSurface(Image image, string premiumSpriteName, string fallbackTextureName, bool preserveAspect)
        {
            if (image == null)
            {
                return;
            }

            Sprite premiumSprite = UICandySkin.GetSprite(premiumSpriteName, 100f);
            if (premiumSprite != null)
            {
                image.sprite = premiumSprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = preserveAspect;
                image.color = Color.white;
                return;
            }

            Sprite fallbackSprite = UICandySkin.GetSprite(fallbackTextureName, 100f);
            if (fallbackSprite != null)
            {
                image.sprite = fallbackSprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = preserveAspect;
                image.color = Color.white;
            }
        }

        private void SetupCandyPanel(Image img)
        {
            SetupCandyButton(img, "sheet_modal_panel", new Vector4(220f, 220f, 220f, 220f), 220f);
        }

        private void SetupCandyRow(Image img)
        {
            SetupCandyButton(img, "sheet_modal_row", new Vector4(160f, 160f, 160f, 160f), 220f);
        }

        private void SetupCandyOrb(Image img)
        {
            UICandySkin.ApplyCandyOrb(img);
        }

        private static void ConfigureCandyChrome(Image image, Color shadowColor, Color outlineColor, Vector2 shadowDistance, Vector2 outlineDistance)
        {
            if (image == null)
            {
                return;
            }

            Shadow shadow = image.GetComponent<Shadow>() ?? image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = shadowColor;
            shadow.effectDistance = shadowDistance;

            Outline outline = image.GetComponent<Outline>() ?? image.gameObject.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = outlineDistance;
        }

        private static GradientImage GetOrCreateGradient(RectTransform parent, string name)
        {
            Transform existing = parent.Find(name);
            GradientImage gradient = existing != null ? existing.GetComponent<GradientImage>() : null;
            if (gradient != null)
            {
                return gradient;
            }

            GameObject gradientObject = new(name);
            gradientObject.transform.SetParent(parent, false);
            return gradientObject.AddComponent<GradientImage>();
        }

        private void ApplyCandySheetPanel(Image image)
        {
            ApplyMainPremiumSurface(image, "main_premium_panel", "sheet_modal_panel", preserveAspect: false);
            image.color = Color.white;
            ConfigureCandyChrome(image, new Color(0f, 0f, 0f, 0.36f), new Color(0.82f, 0.24f, 1f, 0.16f), new Vector2(0f, -12f), new Vector2(1f, -1f));
        }

        private void ApplyCandySectionRow(Image image, Color tint)
        {
            Sprite pillSprite = GetFlatPillSprite();
            if (pillSprite != null)
            {
                image.sprite = pillSprite;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1f;
            }
            else
            {
                image.sprite = null;
                image.type = Image.Type.Simple;
            }
            image.color = tint;
            ConfigureCandyChrome(image, new Color(0f, 0f, 0f, 0.22f), new Color(0.86f, 0.33f, 1f, 0.12f), new Vector2(0f, -3f), new Vector2(1f, -1f));
        }

        private bool TryApplyLeaderboardPremiumModalSprite(Image target)
        {
            if (target == null)
            {
                return false;
            }

            if (leaderboardPremiumModalSprite == null)
            {
                Texture2D texture = Resources.Load<Texture2D>("TowerMaze/UITheme/leaderboard_modal_premium_clean");
                if (texture == null)
                {
                    return false;
                }

                leaderboardPremiumModalSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                leaderboardPremiumModalSprite.name = "TowerMaze_LeaderboardPremiumModal";
            }

            target.sprite = leaderboardPremiumModalSprite;
            target.type = Image.Type.Simple;
            target.preserveAspect = false;
            target.color = Color.white;
            return true;
        }

        private void ApplyLeaderboardPremiumPanel(Image image)
        {
            ApplyMainPremiumSurface(image, "main_premium_panel", "sheet_modal_panel", preserveAspect: false);
            image.color = Color.white;
            ConfigureCandyChrome(image, new Color(0f, 0f, 0f, 0.48f), new Color(0.86f, 0.33f, 1f, 0.18f), new Vector2(0f, -16f), new Vector2(1f, -1f));
        }

        private void ApplyLeaderboardPremiumPlate(Image image, Color tint, Color outlineTint, Vector2 outlineDistance)
        {
            if (image == null)
            {
                return;
            }

            Sprite pillSprite = GetFlatPillSprite();
            if (pillSprite != null)
            {
                image.sprite = pillSprite;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1f;
            }
            else
            {
                image.sprite = null;
                image.type = Image.Type.Simple;
            }
            image.color = tint;
            image.raycastTarget = true;
            ConfigureCandyChrome(image, new Color(0f, 0f, 0f, 0.26f), new Color(0.86f, 0.33f, 1f, 0.12f), new Vector2(0f, -4f), new Vector2(1f, -1f));
            AddAmbientGloss(image.rectTransform, "PremiumPlateGloss", new Vector2(0.035f, 0.64f), new Vector2(0.965f, 0.94f), new Color(1f, 1f, 1f, 0.07f), new Color(1f, 1f, 1f, 0f));
        }

        private static void AddLeaderboardTextChrome(Text text, Color shadowColor, Color outlineColor, Vector2 shadowDistance, Vector2 outlineDistance)
        {
            if (text == null)
            {
                return;
            }

            Shadow shadow = text.GetComponent<Shadow>() ?? text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = shadowColor;
            shadow.effectDistance = shadowDistance;

            Outline outline = text.GetComponent<Outline>() ?? text.gameObject.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = outlineDistance;
        }

        private void AddAmbientGloss(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color topColor, Color bottomColor)
        {
            if (parent == null)
            {
                return;
            }

            GradientImage ambient = GetOrCreateGradient(parent, name);
            ambient.colorTop = topColor;
            ambient.colorBottom = bottomColor;
            UIManager.Stretch(ambient.rectTransform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            ambient.rectTransform.SetAsFirstSibling();
            ambient.raycastTarget = false;
        }

        private void ApplyCandyChipPolish(Image image, bool warm)
        {
            if (image == null)
            {
                return;
            }

            ConfigureCandyChrome(
                image,
                new Color(0.10f, 0.05f, 0.18f, 0.16f),
                new Color(1f, 1f, 1f, warm ? 0.12f : 0.07f),
                new Vector2(0f, -2f),
                new Vector2(1f, -1f));

            AddAmbientGloss(
                image.rectTransform,
                "Gloss",
                new Vector2(0.04f, 0.54f),
                new Vector2(0.96f, 0.96f),
                new Color(1f, 1f, 1f, warm ? 0.24f : 0.18f),
                new Color(1f, 1f, 1f, 0f));

            AddAmbientGloss(
                image.rectTransform,
                "Rim",
                new Vector2(0.05f, 0.72f),
                new Vector2(0.95f, 0.98f),
                warm ? new Color(1f, 0.84f, 0.44f, 0.18f) : new Color(1f, 1f, 1f, 0.08f),
                new Color(1f, 1f, 1f, 0f));
        }

        private Text CreateCandyTitleRibbon(Transform parent, Font font, string name, string fallbackText, Vector2 anchorMin, Vector2 anchorMax, int fontSize)
        {
            Image ribbon = UIManager.CreateImage(name + "Ribbon", parent, Color.white);
            ApplyCandySectionRow(ribbon, SheetTitleRibbonTint);
            UIManager.Stretch(ribbon.rectTransform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);

            GradientImage gloss = GetOrCreateGradient(ribbon.rectTransform, "Gloss");
            gloss.colorTop = new Color(1f, 1f, 1f, 0.14f);
            gloss.colorBottom = new Color(1f, 1f, 1f, 0f);
            UIManager.Stretch(gloss.rectTransform, new Vector2(0.03f, 0.54f), new Vector2(0.97f, 0.96f), Vector2.zero, Vector2.zero);
            gloss.rectTransform.SetAsFirstSibling();
            gloss.raycastTarget = false;

            Text title = UIManager.CreateText(name + "Text", ribbon.transform, font, fontSize, TextAnchor.MiddleCenter, SheetTitleTextTint);
            title.text = fallbackText;
            title.fontStyle = FontStyle.Bold;
            title.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(title, Mathf.Max(14, fontSize - 5), fontSize, UIFontRole.Popup);
            UIManager.Stretch(title.rectTransform, new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.94f), Vector2.zero, Vector2.zero);

            Shadow titleShadow = title.GetComponent<Shadow>() ?? title.gameObject.AddComponent<Shadow>();
            titleShadow.effectColor = SheetTitleShadowTint;
            titleShadow.effectDistance = new Vector2(0f, -4f);

            Outline titleOutline = title.GetComponent<Outline>() ?? title.gameObject.AddComponent<Outline>();
            titleOutline.effectColor = SheetTitleOutlineTint;
            titleOutline.effectDistance = new Vector2(1.5f, -1.5f);

            return title;
        }

        private Text CreateCandySectionBadge(Transform parent, Font font, string name, string textValue, Vector2 anchorMin, Vector2 anchorMax, Color tint)
        {
            Image badge = UIManager.CreateImage(name, parent, Color.white);
            ApplyCandySectionRow(badge, tint);
            UIManager.Stretch(badge.rectTransform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            AddAmbientGloss(badge.rectTransform, "BadgeGloss", new Vector2(0.04f, 0.50f), new Vector2(0.96f, 0.94f), new Color(1f, 1f, 1f, 0.22f), new Color(1f, 1f, 1f, 0f));

            Text label = UIManager.CreateText(name + "Text", badge.transform, font, 14, TextAnchor.MiddleCenter, Color.white, UIFontRole.Popup);
            label.text = textValue;
            label.fontStyle = FontStyle.Bold;
            label.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(label, 12, 14, UIFontRole.Popup);
            UIManager.Stretch(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));
            return label;
        }

        private void ApplyTitleOrbSprite(Transform parent, string titleName, string spriteName)
        {
            if (parent == null)
            {
                return;
            }

            Transform ribbon = parent.Find(titleName + "Ribbon");
            Transform orbTransform = ribbon != null ? ribbon.Find(titleName + "Orb") : null;
            Transform titleTransform = ribbon != null ? ribbon.Find(titleName + "Text") : null;
            if (orbTransform == null)
            {
                return;
            }

            Image orbImage = orbTransform.GetComponent<Image>();
            Sprite orbSprite = UICandySkin.GetSprite(spriteName, 100f);
            if (orbImage == null || orbSprite == null)
            {
                return;
            }

            orbImage.sprite = orbSprite;
            orbImage.type = Image.Type.Simple;
            orbImage.preserveAspect = true;
            orbImage.color = Color.white;

            RectTransform orbRect = orbTransform as RectTransform;
            if (orbRect != null)
            {
                UIManager.Stretch(orbRect, new Vector2(0.015f, 0.08f), new Vector2(0.205f, 0.92f), Vector2.zero, Vector2.zero);
            }

            RectTransform titleRect = titleTransform as RectTransform;
            if (titleRect != null)
            {
                UIManager.Stretch(titleRect, new Vector2(0.21f, 0.06f), new Vector2(0.90f, 0.94f), new Vector2(4f, 0f), new Vector2(-8f, 0f));
            }
        }

        private void ApplyTitleOrbSprite(Transform parent, string titleName, Sprite sprite)
        {
            if (parent == null || sprite == null)
            {
                return;
            }

            Transform ribbon = parent.Find(titleName + "Ribbon");
            Transform orbTransform = ribbon != null ? ribbon.Find(titleName + "Orb") : null;
            Transform titleTransform = ribbon != null ? ribbon.Find(titleName + "Text") : null;
            if (orbTransform == null)
            {
                return;
            }

            Image orbImage = orbTransform.GetComponent<Image>();
            if (orbImage == null)
            {
                return;
            }

            orbImage.sprite = sprite;
            orbImage.type = Image.Type.Simple;
            orbImage.preserveAspect = true;
            orbImage.color = Color.white;

            RectTransform orbRect = orbTransform as RectTransform;
            if (orbRect != null)
            {
                UIManager.Stretch(orbRect, new Vector2(0.03f, 0.14f), new Vector2(0.17f, 0.86f), new Vector2(2f, 0f), new Vector2(-2f, 0f));
            }

            RectTransform titleRect = titleTransform as RectTransform;
            if (titleRect != null)
            {
                UIManager.Stretch(titleRect, new Vector2(0.18f, 0.06f), new Vector2(0.90f, 0.94f), new Vector2(4f, 0f), new Vector2(-8f, 0f));
            }
        }

        private void BuildSettingsPanel(Font font, Action onToggleSound, Action onToggleVibration)
        {
            // Dimmed overlay backer
            GameObject overlay = new GameObject("SettingsOverlay");
            overlay.transform.SetParent(transform, false);
            var overlayRt = overlay.AddComponent<RectTransform>();
            UIManager.Stretch(overlayRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.AddComponent<Image>().color = SheetOverlayTint;
            settingsPanel = overlay;
            settingsPanelCanvasGroup = overlay.AddComponent<CanvasGroup>();
            settingsPanelCanvasGroup.alpha = 0f;

            // Centered Modal Card
            GameObject card = new GameObject("SettingsCard");
            card.transform.SetParent(overlay.transform, false);
            settingsPanelRt = card.AddComponent<RectTransform>();
            settingsPanelRt.anchorMin = new Vector2(0.05f, 0.20f);
            settingsPanelRt.anchorMax = new Vector2(0.95f, 0.88f);
            settingsPanelRt.offsetMin = settingsPanelRt.offsetMax = Vector2.zero;
            Image cardImage = card.AddComponent<Image>();
            ApplyCandySheetPanel(cardImage);
            AddAmbientGloss(settingsPanelRt, "SettingsTopGloss", new Vector2(0.04f, 0.58f), new Vector2(0.96f, 0.98f), new Color(1f, 1f, 1f, 0.18f), new Color(1f, 1f, 1f, 0f));
            AddAmbientGloss(settingsPanelRt, "SettingsLowerWash", new Vector2(0.07f, 0.04f), new Vector2(0.93f, 0.48f), new Color(1f, 0.86f, 0.56f, 0.09f), new Color(0.96f, 0.84f, 1f, 0f));

            settingsTitleText = CreateCandyTitleRibbon(card.transform, font, "SettingsTitle", "SETTINGS", new Vector2(0.06f, 0.84f), new Vector2(0.90f, 0.97f), 22);
            Texture2D settingsOrbTexture = Resources.Load<Texture2D>("TowerMaze/UITheme/out_icon_gear");
            if (settingsOrbTexture != null)
            {
                Sprite settingsOrbSprite = Sprite.Create(settingsOrbTexture, new Rect(0, 0, settingsOrbTexture.width, settingsOrbTexture.height), new Vector2(0.5f, 0.5f), 100f);
                ApplyTitleOrbSprite(card.transform, "SettingsTitle", settingsOrbSprite);
            }

            Button closeButton = UIManager.CreateCandyCloseButton("CloseBtn", card.transform, font, 16);
            ApplyMainPremiumSurface(closeButton.targetGraphic as Image, "main_premium_icon_frame", "out_btn_purple", preserveAspect: true);
            UIManager.BindButton(closeButton, HideSettingsPanel, buttonClickSound);
            var closeRt = (RectTransform)closeButton.transform;
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-22f, -18f);
            closeRt.sizeDelta = new Vector2(54f, 54f);

            Image controlsBacker = UIManager.CreateImage("ControlsBacker", card.transform, Color.white);
            ApplyCandySectionRow(controlsBacker, new Color(SheetSectionTint.r, SheetSectionTint.g, SheetSectionTint.b, 0.82f));
            controlsBacker.rectTransform.anchorMin = new Vector2(0.08f, 0.46f);
            controlsBacker.rectTransform.anchorMax = new Vector2(0.92f, 0.76f);
            controlsBacker.rectTransform.offsetMin = controlsBacker.rectTransform.offsetMax = Vector2.zero;
            controlsBacker.raycastTarget = false;
            AddAmbientGloss(controlsBacker.rectTransform, "ControlsGloss", new Vector2(0.03f, 0.56f), new Vector2(0.97f, 0.95f), new Color(1f, 1f, 1f, 0.12f), new Color(1f, 1f, 1f, 0f));
            controlsBacker.transform.SetAsFirstSibling();

            settingsAudioHeaderText = CreateCandySectionBadge(
                controlsBacker.transform,
                font,
                "ControlsHeaderBadge",
                "AUDIO",
                new Vector2(0.05f, 0.82f),
                new Vector2(0.35f, 0.97f),
                UIStyle.Action); // Main screen orange
            settingsAudioHeaderText.alignment = TextAnchor.MiddleCenter;

            BuildSettingsToggleRow("Sound", font, controlsBacker.transform,
                ref settingsSoundLabelText, ref settingsSoundIcon, ref settingsSoundToggleBg, ref settingsSoundToggleText, 0.60f,
                () => { onToggleSound?.Invoke(); });

            BuildSettingsToggleRow("Vibration", font, controlsBacker.transform,
                ref settingsVibrationLabelText, ref settingsVibIcon, ref settingsVibToggleBg, ref settingsVibToggleText, 0.24f,
                () => { onToggleVibration?.Invoke(); });

            Image languageBacker = UIManager.CreateImage("LanguageBacker", card.transform, Color.white);
            ApplyCandySectionRow(languageBacker, new Color(SheetSectionTint.r, SheetSectionTint.g, SheetSectionTint.b, 0.80f));
            languageBacker.rectTransform.anchorMin = new Vector2(0.08f, 0.18f);
            languageBacker.rectTransform.anchorMax = new Vector2(0.92f, 0.40f);
            languageBacker.rectTransform.offsetMin = languageBacker.rectTransform.offsetMax = Vector2.zero;
            languageBacker.raycastTarget = false;
            AddAmbientGloss(languageBacker.rectTransform, "LanguageBackerGlow", new Vector2(0.04f, 0.34f), new Vector2(0.96f, 0.88f), new Color(1f, 1f, 1f, 0.09f), new Color(1f, 1f, 1f, 0f));
            languageBacker.transform.SetAsFirstSibling();

            var langLabel = CreateCandySectionBadge(
                languageBacker.transform,
                font,
                "LangLabelBadge",
                "LANGUAGE",
                new Vector2(0.05f, 0.76f),
                new Vector2(0.40f, 0.96f),
                UIStyle.Action); // Main screen orange
            languageLabelText = langLabel;

            Image langCard = UIManager.CreateCard("LanguageCard", languageBacker.transform, Color.white, new Color(0f, 0f, 0f, 0f));
            ApplyCandySectionRow(langCard, new Color(0.080f, 0.026f, 0.160f, 0.92f));
            langCard.rectTransform.anchorMin = new Vector2(0.05f, 0.12f);
            langCard.rectTransform.anchorMax = new Vector2(0.95f, 0.64f);
            langCard.rectTransform.offsetMin = langCard.rectTransform.offsetMax = Vector2.zero;
            AddAmbientGloss(langCard.rectTransform, "LanguageCardGloss", new Vector2(0.04f, 0.52f), new Vector2(0.96f, 0.95f), new Color(1f, 1f, 1f, 0.16f), new Color(1f, 1f, 1f, 0f));

            languageChipBackgrounds = new Image[supportedLanguageCodes.Length];
            languageChipTexts = new Text[supportedLanguageCodes.Length];
            for (int index = 0; index < supportedLanguageCodes.Length; index++)
            {
                SpawnLangButton(langCard.transform, supportedLanguageCodes[index], font, index);
            }

            RefreshLanguageTabs();

            Button privacyBtn = UIManager.CreateButton("PrivacyBtn", card.transform, font, "PRIVACY", Color.white, Color.white);
            ApplyMainPremiumSurface(privacyBtn.targetGraphic as Image, "main_premium_button", "out_btn_purple", preserveAspect: false);
            RectTransform privacyRt = (RectTransform)privacyBtn.transform;
            privacyRt.anchorMin = new Vector2(0.08f, 0.05f);
            privacyRt.anchorMax = new Vector2(0.48f, 0.13f);
            privacyRt.offsetMin = privacyRt.offsetMax = Vector2.zero;
            AddAmbientGloss(privacyRt, "PrivacyButtonGloss", new Vector2(0.03f, 0.58f), new Vector2(0.97f, 0.94f), new Color(1f, 1f, 1f, 0.10f), new Color(1f, 1f, 1f, 0f));
            UIManager.StyleButtonLabel(privacyBtn, 14, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            privacyButtonText = privacyBtn.GetComponentInChildren<Text>();
            if (privacyButtonText != null)
            {
                privacyButtonText.fontStyle = FontStyle.Bold;
                privacyButtonText.resizeTextForBestFit = true;
                UIManager.SetScaledBestFit(privacyButtonText, 11, 14, UIFontRole.Button);
            }

            UIManager.BindButton(privacyBtn, ShowPrivacyPolicySheet, buttonClickSound);

            Button qualityBtn = UIManager.CreateButton("QualityBtn", card.transform, font, "GRAPHICS", Color.white, Color.white);
            ApplyMainPremiumSurface(qualityBtn.targetGraphic as Image, "main_premium_button", "out_btn_purple", preserveAspect: false);
            RectTransform qualityRt = (RectTransform)qualityBtn.transform;
            qualityRt.anchorMin = new Vector2(0.52f, 0.05f);
            qualityRt.anchorMax = new Vector2(0.92f, 0.13f);
            qualityRt.offsetMin = qualityRt.offsetMax = Vector2.zero;
            AddAmbientGloss(qualityRt, "QualityButtonGloss", new Vector2(0.03f, 0.58f), new Vector2(0.97f, 0.94f), new Color(1f, 1f, 1f, 0.10f), new Color(1f, 1f, 1f, 0f));
            UIManager.StyleButtonLabel(qualityBtn, 14, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            graphicsQualityButtonText = qualityBtn.GetComponentInChildren<Text>();
            if (graphicsQualityButtonText != null)
            {
                graphicsQualityButtonText.fontStyle = FontStyle.Bold;
                graphicsQualityButtonText.resizeTextForBestFit = true;
                UIManager.SetScaledBestFit(graphicsQualityButtonText, 11, 14, UIFontRole.Button);
            }
            RefreshGraphicsQualityLabel();
            UIManager.BindButton(qualityBtn, CycleGraphicsQuality, buttonClickSound);

            settingsPanelRt.anchoredPosition = new Vector2(1200f, 0f);
            settingsPanel.SetActive(false);
            UIManager.ApplyPopupTextRoles(overlay.transform);
        }

        private static Sprite cachedFlatPillSprite;
        private static Sprite GetFlatPillSprite()
        {
            if (cachedFlatPillSprite != null) return cachedFlatPillSprite;

            const int height = 48;
            const int width = 96;
            float radius = height * 0.5f;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float cx = x + 0.5f;
                    float cy = y + 0.5f;
                    // Center of left/right capsule arcs:
                    float leftCx = radius;
                    float rightCx = width - radius;
                    float dx = 0f;
                    if (cx < leftCx) dx = leftCx - cx;
                    else if (cx > rightCx) dx = cx - rightCx;
                    float dy = Mathf.Abs(cy - radius);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    // Anti-aliased coverage at edge.
                    float alpha = Mathf.Clamp01(radius - dist);
                    pixels[(y * width) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply(false, true);

            // 9-slice border = radius so corners stay perfectly round when stretched.
            Vector4 border = new(radius, radius, radius, radius);
            cachedFlatPillSprite = Sprite.Create(tex,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, border);
            cachedFlatPillSprite.name = "TowerMaze_FlatPill";
            return cachedFlatPillSprite;
        }

        private static Sprite LoadTextureAsSprite(string resourcePath)
        {
            Texture2D tex = Resources.Load<Texture2D>(resourcePath);
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private void BuildSettingsToggleRow(string labelStr, Font font, Transform parent,
            ref Text rowLabelText, ref Image iconImg, ref Image chipBg, ref Text chipText, float anchorYMid, Action onToggle)
        {
            float rowHalfHeight = 0.15f;
            Image rowShell = UIManager.CreateImage(labelStr + "Row", parent, Color.white);
            ApplyCandySectionRow(rowShell, new Color(0.105f, 0.035f, 0.205f, 0.94f));
            rowShell.rectTransform.anchorMin = new Vector2(0.05f, anchorYMid - rowHalfHeight);
            rowShell.rectTransform.anchorMax = new Vector2(0.95f, anchorYMid + rowHalfHeight);
            rowShell.rectTransform.offsetMin = rowShell.rectTransform.offsetMax = Vector2.zero;
            rowShell.raycastTarget = false;
            AddAmbientGloss(rowShell.rectTransform, "RowGloss", new Vector2(0.03f, 0.54f), new Vector2(0.97f, 0.94f), new Color(1f, 1f, 1f, 0.20f), new Color(1f, 1f, 1f, 0f));

            bool soundRow = string.Equals(labelStr, "Sound", StringComparison.OrdinalIgnoreCase);

            iconImg = UIManager.CreateImage(labelStr + "Icon", rowShell.transform, Color.white);
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            UIManager.Stretch(iconImg.rectTransform, new Vector2(0.06f, 0.16f), new Vector2(0.24f, 0.84f), Vector2.zero, Vector2.zero);
            string iconPath = soundRow ? "TowerMaze/UITheme/jelly_sound_icon_hq" : "TowerMaze/UITheme/jelly_vib_icon_hq";
            iconImg.sprite = Resources.Load<Sprite>(iconPath) ?? LoadTextureAsSprite(iconPath);
            iconImg.rectTransform.localScale = new Vector3(1.65f, 1.65f, 1f);

            var lbl = UIManager.CreateText("Lbl_" + labelStr, rowShell.transform, font, 18,
                TextAnchor.MiddleLeft, LeaderboardPremiumText);
            lbl.text = labelStr.ToUpperInvariant();
            lbl.fontStyle = FontStyle.Bold;
            lbl.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(lbl, 14, 18, UIFontRole.Popup);
            UIManager.Stretch(lbl.rectTransform, new Vector2(0.24f, 0.20f), new Vector2(0.54f, 0.80f), Vector2.zero, Vector2.zero);
            rowLabelText = lbl;

            var go = new GameObject("Toggle_" + labelStr);
            go.transform.SetParent(rowShell.transform, false);
            chipBg = go.AddComponent<Image>();
            chipBg.preserveAspect = true;
            chipBg.color = Color.white;
            ApplyMainPremiumSurface(chipBg, "main_premium_button", "out_btn_purple", preserveAspect: true);
            ConfigureCandyChrome(
                chipBg,
                new Color(0.10f, 0.06f, 0.16f, 0.16f),
                new Color(1f, 1f, 1f, 0.08f),
                new Vector2(0f, -2f),
                new Vector2(1f, -1f));
            AddAmbientGloss(
                chipBg.rectTransform,
                "ToggleGloss",
                new Vector2(0.04f, 0.54f),
                new Vector2(0.96f, 0.94f),
                new Color(1f, 1f, 1f, 0.14f),
                new Color(1f, 1f, 1f, 0f));

            var rt = chipBg.rectTransform;
            UIManager.Stretch(rt, new Vector2(0.69f, 0.18f), new Vector2(0.94f, 0.82f), Vector2.zero, Vector2.zero);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = chipBg;
            UIManager.BindButton(btn, () => {
                StartCoroutine(UIStyle.ButtonPress(rt));
                onToggle?.Invoke();
            }, buttonClickSound);

            chipText = UIManager.CreateText("StatusTxt", go.transform, font, 15,
                TextAnchor.MiddleCenter, Color.white);
            chipText.fontStyle = FontStyle.Bold;
            chipText.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(chipText, 12, 15, UIFontRole.Button);
            UIManager.Stretch(chipText.rectTransform, new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.85f), Vector2.zero, Vector2.zero);
            Shadow chipShadow = chipText.gameObject.AddComponent<Shadow>();
            chipShadow.effectColor = new Color(0.16f, 0.08f, 0.20f, 0.28f);
            chipShadow.effectDistance = new Vector2(0f, -1f);
        }

        private void SpawnLangButton(Transform parent, string code, Font font, int index)
        {
            var go = new GameObject($"Lang_{code}");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            ApplyMainPremiumSurface(img, "main_premium_button", "out_btn_purple", preserveAspect: true);
            img.color = Color.white;
            
            var rt = go.GetComponent<RectTransform>();
            float slotWidth = 1f / Mathf.Max(1, supportedLanguageCodes.Length);
            rt.anchorMin = new Vector2(slotWidth * index + 0.03f, 0.18f);
            rt.anchorMax = new Vector2(slotWidth * (index + 1) - 0.03f, 0.82f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            ConfigureCandyChrome(
                img,
                new Color(0.10f, 0.06f, 0.16f, 0.14f),
                new Color(1f, 1f, 1f, 0.08f),
                new Vector2(0f, -2f),
                new Vector2(1f, -1f));
            AddAmbientGloss(
                img.rectTransform,
                "LangGloss",
                new Vector2(0.04f, 0.54f),
                new Vector2(0.96f, 0.94f),
                new Color(1f, 1f, 1f, 0.14f),
                new Color(1f, 1f, 1f, 0f));

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var txt = UIManager.CreateText(go.transform, "T", code,
                16, FontStyle.Bold, Color.white, font, TextAnchor.MiddleCenter);
            txt.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(txt, 14, 17, UIFontRole.Button);
            UIManager.Stretch(txt.rectTransform);
            UIManager.BindButton(btn, () => {
                UILanguage.SetLanguageCode(code);
                RefreshLanguageTabs();
                ApplyLocalizedTexts();
            });

            if (languageChipBackgrounds != null && index < languageChipBackgrounds.Length)
            {
                languageChipBackgrounds[index] = img;
            }

            if (languageChipTexts != null && index < languageChipTexts.Length)
            {
                languageChipTexts[index] = txt;
            }
        }

        private void RefreshLanguageTabs()
        {
            if (languageChipBackgrounds == null || languageChipTexts == null)
            {
                return;
            }

            string currentCode = UILanguage.GetLanguageCode();
            for (int index = 0; index < supportedLanguageCodes.Length; index++)
            {
                string code = supportedLanguageCodes[index].ToUpperInvariant();
                bool active = string.Equals(currentCode, code, StringComparison.OrdinalIgnoreCase);
                if (languageChipBackgrounds[index] != null)
                {
                    // Premium Tinting Logic: Distinct colors per language
                    Color targetCol = new Color(0.4f, 0.4f, 0.4f, 0.6f); // Inactive glass grey
                    if (active)
                    {
                        targetCol = code switch
                        {
                            "TR" => new Color(1.0f, 0.30f, 0.55f), // Turkish Pink (Swapped)
                            "EN" => new Color(0.55f, 0.35f, 1.0f), // English Purple
                            "ES" => new Color(1.0f, 0.44f, 0.12f), // Spanish Orange (Swapped)
                            _ => Color.white
                        };
                    }
                    
                    languageChipBackgrounds[index].color = targetCol;
                    languageChipBackgrounds[index].rectTransform.localScale = active ? new Vector3(1.04f, 1.04f, 1f) : Vector3.one;
                }

                if (languageChipTexts[index] != null)
                {
                    languageChipTexts[index].color = active ? Color.white : new Color(1f, 1f, 1f, 0.6f);
                    languageChipTexts[index].fontStyle = FontStyle.Bold;
                }
            }
        }

        private void ShowSettingsPanel()
        {
            if (settingsPanel == null || settingsPanelRt == null)
            {
                return;
            }

            if (settingsPanelRoutine != null)
            {
                StopCoroutine(settingsPanelRoutine);
            }

            settingsPanel.SetActive(true);
            settingsPanelRoutine = StartCoroutine(AnimateSettingsPanel(open: true));
        }

        private void HideSettingsPanel()
        {
            if (settingsPanel == null || settingsPanelRt == null)
            {
                return;
            }

            if (settingsPanelRoutine != null)
            {
                StopCoroutine(settingsPanelRoutine);
            }

            settingsPanelRoutine = StartCoroutine(AnimateSettingsPanel(open: false));
        }

        private IEnumerator AnimateSettingsPanel(bool open)
        {
            if (settingsPanelCanvasGroup == null)
            {
                settingsPanelCanvasGroup = settingsPanel.GetComponent<CanvasGroup>();
                if (settingsPanelCanvasGroup == null)
                {
                    settingsPanelCanvasGroup = settingsPanel.AddComponent<CanvasGroup>();
                }
            }

            Vector2 from = settingsPanelRt.anchoredPosition;
            Vector2 to = open ? Vector2.zero : new Vector2(1200f, 0f);
            float fromAlpha = settingsPanelCanvasGroup.alpha;
            float toAlpha = open ? 1f : 0f;
            float duration = 0.2f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                settingsPanelRt.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
                settingsPanelCanvasGroup.alpha = Mathf.LerpUnclamped(fromAlpha, toAlpha, eased);
                yield return null;
            }

            settingsPanelRt.anchoredPosition = to;
            settingsPanelCanvasGroup.alpha = toAlpha;
            if (!open)
            {
                settingsPanel.SetActive(false);
            }

            settingsPanelRoutine = null;
        }

        private void ShowLeaderboardSheet()
        {
            if (leaderboardOverlay == null || leaderboardSheet == null) return;
            RefreshLeaderboardRows(resetPosition: true);
            leaderboardOverlay.SetActive(true);
            leaderboardSheet.gameObject.SetActive(true);
            float h = leaderboardSheet.rect.height > 1f ? leaderboardSheet.rect.height : 800f;
            StartCoroutine(UIStyle.SlideUp(leaderboardSheet, h, 0.25f));
        }

        /// <summary>
        /// Called by UIManager when fresh Firebase leaderboard data arrives.
        /// Updates the cached entries and immediately refreshes the popup rows
        /// whether the popup is open or not, so data is always up-to-date.
        /// </summary>
        public void UpdateLeaderboardData(float bestScore, IReadOnlyList<LeaderboardEntry> entries)
        {
            cachedBestScoreValue = bestScore;
            cachedLeaderboardEntries = entries ?? Array.Empty<LeaderboardEntry>();
            // Always refresh rows — if the popup is closed this is fast (no-op on hidden objects).
            // If the popup is open, the user will see live Firebase data appear.
            RefreshLeaderboardRows(resetPosition: false);
        }

        // Chapter-mode leaderboard cache. Refresh rows immediately if the chapter tab
        // is currently visible so live Firestore updates appear without re-opening.
        public void UpdateChapterLeaderboardData(IReadOnlyList<LeaderboardEntry> entries)
        {
            cachedChapterLeaderboardEntries = entries ?? Array.Empty<LeaderboardEntry>();
            if (currentLeaderboardMode == LeaderboardMode.Chapter)
            {
                RefreshLeaderboardRows(resetPosition: false);
            }
        }

        private void ShowMissionsSheet()
        {
            if (missionsOverlay == null || missionsSheet == null) return;
            UpdateMissionCountdown(GetTimeUntilNextLocalDay());
            missionsOverlay.SetActive(true);
            missionsSheet.gameObject.SetActive(true);
            float h = missionsSheet.rect.height > 1f ? missionsSheet.rect.height : 900f;
            StartCoroutine(UIStyle.SlideUp(missionsSheet, h, 0.25f));
        }

        private void RefreshGraphicsQualityLabel()
        {
            if (graphicsQualityButtonText == null) return;
            QualityOverrideMode mode = DeviceQualityProfile.CurrentOverrideMode;
            string modeWord = mode switch
            {
                QualityOverrideMode.Low => TranslateText("DUSUK", "LOW", "BAJO"),
                QualityOverrideMode.Mid => TranslateText("ORTA", "MID", "MEDIO"),
                QualityOverrideMode.High => TranslateText("YUKSEK", "HIGH", "ALTO"),
                _ => TranslateText("OTO", "AUTO", "AUTO"),
            };
            string prefix = TranslateText("GFX", "GFX", "GFX");
            graphicsQualityButtonText.text = $"{prefix}: {modeWord}";
        }

        private void CycleGraphicsQuality()
        {
            QualityOverrideMode current = DeviceQualityProfile.CurrentOverrideMode;
            QualityOverrideMode next = current switch
            {
                QualityOverrideMode.Auto => QualityOverrideMode.Low,
                QualityOverrideMode.Low => QualityOverrideMode.Mid,
                QualityOverrideMode.Mid => QualityOverrideMode.High,
                _ => QualityOverrideMode.Auto,
            };
            DeviceQualityProfile.SetOverrideMode(next);
            RefreshGraphicsQualityLabel();
        }

        private void ShowPrivacyPolicySheet()
        {
            if (privacyOverlay == null || privacySheet == null)
            {
                return;
            }

            RefreshPrivacyScrollLayout(resetPosition: true);
            privacyOverlay.SetActive(true);
            privacyOverlay.transform.SetAsLastSibling();
            privacySheet.gameObject.SetActive(true);
            float h = privacySheet.rect.height > 1f ? privacySheet.rect.height : 960f;
            StartCoroutine(UIStyle.SlideUp(privacySheet, h, 0.25f));
        }

        private IEnumerator CloseSheet(RectTransform sheet, GameObject overlayRoot = null)
        {
            float h = sheet.rect.height > 1f ? sheet.rect.height : 800f;
            yield return StartCoroutine(UIStyle.SlideDown(sheet, h, 0.20f));
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
                yield break;
            }

            sheet.gameObject.SetActive(false);
        }

        private void BuildLeaderboardSheet(Font font)
        {
            leaderboardOverlay = new GameObject("LeaderboardOverlay");
            leaderboardOverlay.transform.SetParent(transform, false);
            RectTransform overlayRt = leaderboardOverlay.AddComponent<RectTransform>();
            UIManager.Stretch(overlayRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            leaderboardOverlay.AddComponent<Image>().color = LeaderboardPremiumOverlayTint;
            leaderboardOverlay.AddComponent<CanvasGroup>().alpha = 1f;

            GameObject cardGo = new GameObject("LeaderboardCard");
            cardGo.transform.SetParent(leaderboardOverlay.transform, false);
            leaderboardSheet = cardGo.AddComponent<RectTransform>();
            leaderboardSheet.anchorMin = new Vector2(0.08f, 0.13f);
            leaderboardSheet.anchorMax = new Vector2(0.92f, 0.90f);
            leaderboardSheet.offsetMin = leaderboardSheet.offsetMax = Vector2.zero;
            Image cardImage = cardGo.AddComponent<Image>();
            ApplyLeaderboardPremiumPanel(cardImage);
            cardGo.AddComponent<CanvasGroup>().alpha = 0f;

            Image handle = UIManager.CreateImage("Handle", cardGo.transform, Color.clear);
            handle.rectTransform.anchorMin = new Vector2(0.35f, 0.95f);
            handle.rectTransform.anchorMax = new Vector2(0.65f, 0.97f);
            handle.rectTransform.offsetMin = handle.rectTransform.offsetMax = Vector2.zero;
            handle.raycastTarget = false;

            leaderboardTitleText = UIManager.CreateText("LeaderboardTitle", cardGo.transform, font, 24, TextAnchor.MiddleCenter, LeaderboardPremiumGold, UIFontRole.Popup);
            leaderboardTitleText.text = "LEADERBOARD";
            leaderboardTitleText.fontStyle = FontStyle.Bold;
            leaderboardTitleText.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(leaderboardTitleText, 18, 24, UIFontRole.Popup);
            UIManager.Stretch(leaderboardTitleText.rectTransform, new Vector2(0.14f, 0.842f), new Vector2(0.86f, 0.908f), Vector2.zero, Vector2.zero);
            AddLeaderboardTextChrome(leaderboardTitleText, new Color(0f, 0f, 0f, 0.52f), new Color(1f, 0.58f, 0.16f, 0.24f), new Vector2(0f, -2.2f), new Vector2(1.2f, -1.2f));

            Image subtitleChip = UIManager.CreateImage("SubtitleChip", cardGo.transform, Color.white);
            ApplyLeaderboardPremiumPlate(subtitleChip, new Color(0.070f, 0.025f, 0.085f, 0.92f), new Color(1f, 0.66f, 0.18f, 0.34f), new Vector2(1f, -1f));
            subtitleChip.raycastTarget = false;
            subtitleChip.rectTransform.anchorMin = new Vector2(0.145f, 0.690f);
            subtitleChip.rectTransform.anchorMax = new Vector2(0.855f, 0.730f);
            subtitleChip.rectTransform.offsetMin = subtitleChip.rectTransform.offsetMax = Vector2.zero;

            BuildLeaderboardTabStrip(cardGo.transform, font);

            leaderboardSubtitleText = UIManager.CreateText("Subtitle", cardGo.transform, font, 13, TextAnchor.MiddleLeft, LeaderboardPremiumMutedText);
            leaderboardSubtitleText.text = "Sen: 0m - #--. sira";
            leaderboardSubtitleText.fontStyle = FontStyle.Bold;
            leaderboardSubtitleText.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(leaderboardSubtitleText, 11, 13, UIFontRole.Popup);
            leaderboardSubtitleText.rectTransform.SetParent(subtitleChip.transform, false);
            UIManager.Stretch(leaderboardSubtitleText.rectTransform, Vector2.zero, Vector2.one, new Vector2(20f, 0f), new Vector2(-20f, 0f));
            AddLeaderboardTextChrome(leaderboardSubtitleText, new Color(0f, 0f, 0f, 0.30f), new Color(0f, 0f, 0f, 0.16f), new Vector2(0f, -1.2f), new Vector2(0.6f, -0.6f));

            GameObject viewportObject = new GameObject("LeaderboardViewport");
            viewportObject.transform.SetParent(cardGo.transform, false);
            RectTransform viewportRect = viewportObject.AddComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0.120f, 0.205f);
            viewportRect.anchorMax = new Vector2(0.880f, 0.600f);
            viewportRect.offsetMin = viewportRect.offsetMax = Vector2.zero;
            Image viewportImage = viewportObject.AddComponent<Image>();
            viewportImage.color = new Color(0.025f, 0.010f, 0.038f, 0.30f);
            viewportImage.raycastTarget = true;
            Mask viewportMask = viewportObject.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            GameObject scrollObject = new GameObject("LeaderboardScrollView");
            scrollObject.transform.SetParent(viewportObject.transform, false);
            RectTransform scrollRect = scrollObject.AddComponent<RectTransform>();
            UIManager.Stretch(scrollRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image scrollHitArea = scrollObject.AddComponent<Image>();
            scrollHitArea.color = new Color(1f, 1f, 1f, 0.001f);
            scrollHitArea.raycastTarget = true;

            leaderboardScrollRect = scrollObject.AddComponent<ScrollRect>();
            leaderboardScrollRect.horizontal = false;
            leaderboardScrollRect.vertical = true;
            leaderboardScrollRect.movementType = ScrollRect.MovementType.Clamped;
            leaderboardScrollRect.scrollSensitivity = 38f;
            leaderboardScrollRect.viewport = viewportRect;

            GameObject contentObject = new GameObject("LeaderboardContent");
            contentObject.transform.SetParent(scrollObject.transform, false);
            leaderboardContentRect = contentObject.AddComponent<RectTransform>();
            UIManager.Stretch(leaderboardContentRect, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            leaderboardContentRect.pivot = new Vector2(0.5f, 1f);
            VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = LeaderboardRowSpacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(0, 0, 0, LeaderboardContentBottomPadding);
            ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            leaderboardScrollRect.content = leaderboardContentRect;

            Button closeBtn = UIManager.CreateButton("CloseBtn", cardGo.transform, font, "KAPAT", Color.white, Color.white);
            ApplyMainPremiumSurface(closeBtn.targetGraphic as Image, "main_premium_cta", "out_btn_orange", preserveAspect: false);
            RectTransform closeRt = (RectTransform)closeBtn.transform;
            closeRt.anchorMin = new Vector2(0.20f, 0.085f);
            closeRt.anchorMax = new Vector2(0.80f, 0.145f);
            closeRt.offsetMin = closeRt.offsetMax = Vector2.zero;
            UIManager.StyleButtonLabel(closeBtn, 16, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            leaderboardCloseButtonText = closeBtn.GetComponentInChildren<Text>();
            if (leaderboardCloseButtonText != null)
            {
                leaderboardCloseButtonText.color = Color.white;
                leaderboardCloseButtonText.fontStyle = FontStyle.Bold;
                leaderboardCloseButtonText.resizeTextForBestFit = true;
                UIManager.SetScaledBestFit(leaderboardCloseButtonText, 14, 16, UIFontRole.Button);
                UIManager.Stretch(leaderboardCloseButtonText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 1.5f), new Vector2(0f, -0.5f));
                AddLeaderboardTextChrome(leaderboardCloseButtonText, new Color(0f, 0f, 0f, 0.46f), new Color(1f, 0.88f, 0.34f, 0.24f), new Vector2(0f, -2f), new Vector2(0.8f, -0.8f));
            }
            UIManager.BindButton(closeBtn, () => StartCoroutine(CloseSheet(leaderboardSheet, leaderboardOverlay)), buttonClickSound);
            RefreshLeaderboardRows(resetPosition: false);

            leaderboardOverlay.SetActive(false);
            UIManager.ApplyPopupTextRoles(leaderboardOverlay.transform);
        }

        private void BuildPrivacyPolicySheet(Font font)
        {
            privacyOverlay = new GameObject("PrivacyOverlay");
            privacyOverlay.transform.SetParent(transform, false);
            RectTransform overlayRt = privacyOverlay.AddComponent<RectTransform>();
            UIManager.Stretch(overlayRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            privacyOverlay.AddComponent<Image>().color = SheetOverlayTint;
            privacyOverlay.AddComponent<CanvasGroup>().alpha = 1f;

            GameObject card = new GameObject("PrivacyCard");
            card.transform.SetParent(privacyOverlay.transform, false);
            privacySheet = card.AddComponent<RectTransform>();
            privacySheet.anchorMin = new Vector2(0.04f, 0.05f);
            privacySheet.anchorMax = new Vector2(0.96f, 0.95f);
            privacySheet.offsetMin = privacySheet.offsetMax = Vector2.zero;
            Image cardImage = card.AddComponent<Image>();
            ApplyCandySheetPanel(cardImage);
            card.AddComponent<CanvasGroup>().alpha = 0f;

            Image handle = UIManager.CreateImage("Handle", card.transform, Color.clear);
            handle.rectTransform.anchorMin = new Vector2(0.40f, 0.965f);
            handle.rectTransform.anchorMax = new Vector2(0.60f, 0.985f);
            handle.rectTransform.offsetMin = handle.rectTransform.offsetMax = Vector2.zero;
            handle.raycastTarget = false;

            privacyTitleText = UIManager.CreateText("Title", card.transform, font, 22, TextAnchor.MiddleLeft, UIStyle.PopupText);
            privacyTitleText.fontStyle = FontStyle.Bold;
            privacyTitleText.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(privacyTitleText, 18, 22, UIFontRole.Popup);
            privacyTitleText.rectTransform.anchorMin = new Vector2(0.12f, 0.87f);
            privacyTitleText.rectTransform.anchorMax = new Vector2(0.88f, 0.95f);
            privacyTitleText.rectTransform.offsetMin = privacyTitleText.rectTransform.offsetMax = Vector2.zero;

            privacySubtitleText = UIManager.CreateText("Subtitle", card.transform, font, 14, TextAnchor.MiddleLeft, UIStyle.PopupTextDim);
            privacySubtitleText.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(privacySubtitleText, 12, 14, UIFontRole.Popup);
            privacySubtitleText.rectTransform.anchorMin = new Vector2(0.12f, 0.82f);
            privacySubtitleText.rectTransform.anchorMax = new Vector2(0.88f, 0.87f);
            privacySubtitleText.rectTransform.offsetMin = privacySubtitleText.rectTransform.offsetMax = Vector2.zero;

            Button closeButton = UIManager.CreateCandyCloseButton("CloseBtn", card.transform, font, 16);
            UIManager.BindButton(closeButton, () => StartCoroutine(CloseSheet(privacySheet, privacyOverlay)), buttonClickSound);
            RectTransform closeRt = (RectTransform)closeButton.transform;
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-22f, -18f);
            closeRt.sizeDelta = new Vector2(54f, 54f);

            GameObject viewportObject = new GameObject("Viewport");
            viewportObject.transform.SetParent(card.transform, false);
            RectTransform viewportRect = viewportObject.AddComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0.12f, 0.08f);
            viewportRect.anchorMax = new Vector2(0.88f, 0.78f);
            viewportRect.offsetMin = viewportRect.offsetMax = Vector2.zero;
            Image viewportImage = viewportObject.AddComponent<Image>();
            viewportImage.color = new Color(0.08f, 0.10f, 0.16f, 0.18f);
            viewportImage.raycastTarget = true;
            Mask viewportMask = viewportObject.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            GameObject scrollObject = new GameObject("ScrollView");
            scrollObject.transform.SetParent(viewportObject.transform, false);
            RectTransform scrollRect = scrollObject.AddComponent<RectTransform>();
            UIManager.Stretch(scrollRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image scrollHitArea = scrollObject.AddComponent<Image>();
            scrollHitArea.color = new Color(1f, 1f, 1f, 0.001f);
            scrollHitArea.raycastTarget = true;

            ScrollRect scroll = scrollObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            scroll.viewport = viewportRect;
            privacyScrollRect = scroll;

            privacyBodyText = UIManager.CreateText("Body", scrollObject.transform, font, 14, TextAnchor.UpperLeft, Color.white);
            privacyBodyText.fontStyle = FontStyle.Normal;
            privacyBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            privacyBodyText.verticalOverflow = VerticalWrapMode.Overflow;
            privacyBodyText.resizeTextForBestFit = false;
            privacyBodyText.supportRichText = false;
            privacyBodyText.lineSpacing = 1.1f;
            privacyBodyText.raycastTarget = false;

            RectTransform bodyRect = privacyBodyText.rectTransform;
            privacyBodyRect = bodyRect;
            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.offsetMin = new Vector2(12f, 0f);
            bodyRect.offsetMax = new Vector2(-12f, 0f);
            bodyRect.anchoredPosition = Vector2.zero;

            ContentSizeFitter fitter = privacyBodyText.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = bodyRect;
            RefreshPrivacyScrollLayout(resetPosition: false);

            privacyOverlay.SetActive(false);
            UIManager.ApplyPopupTextRoles(privacyOverlay.transform);
        }

        private void BuildMissionsSheet(Font font)
        {
            missionsOverlay = new GameObject("MissionsOverlay");
            missionsOverlay.transform.SetParent(transform, false);
            RectTransform overlayRt = missionsOverlay.AddComponent<RectTransform>();
            UIManager.Stretch(overlayRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            missionsOverlay.AddComponent<Image>().color = SheetOverlayTint;
            missionsOverlay.AddComponent<CanvasGroup>().alpha = 1f;

            GameObject card = new GameObject("MissionsCard");
            card.transform.SetParent(missionsOverlay.transform, false);
            missionsSheet = card.AddComponent<RectTransform>();
            missionsSheet.anchorMin = new Vector2(0.04f, 0.07f);
            missionsSheet.anchorMax = new Vector2(0.96f, 0.91f);
            missionsSheet.offsetMin = missionsSheet.offsetMax = Vector2.zero;
            Image cardImage = card.AddComponent<Image>();
            ApplyCandySheetPanel(cardImage);
            card.AddComponent<CanvasGroup>().alpha = 0f;

            Image missionHandle = UIManager.CreateImage("Handle", card.transform, Color.clear);
            missionHandle.rectTransform.anchorMin = new Vector2(0.35f, 0.95f);
            missionHandle.rectTransform.anchorMax = new Vector2(0.65f, 0.97f);
            missionHandle.rectTransform.offsetMin = missionHandle.rectTransform.offsetMax = Vector2.zero;
            missionHandle.raycastTarget = false;

            missionsTitleText = CreateCandyTitleRibbon(card.transform, font, "MissionsTitle", "DAILY MISSIONS", new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.95f), 22);
            ApplyTitleOrbSprite(card.transform, "MissionsTitle", "missions_icon_jelly_premium");

            Image subtitleChip = UIManager.CreateImage("SubtitleChip", card.transform, Color.white);
            ApplyCandySectionRow(subtitleChip, SheetSectionTint);
            subtitleChip.rectTransform.anchorMin = new Vector2(0.12f, 0.72f);
            subtitleChip.rectTransform.anchorMax = new Vector2(0.56f, 0.79f);
            subtitleChip.rectTransform.offsetMin = subtitleChip.rectTransform.offsetMax = Vector2.zero;

            Text subtitle = UIManager.CreateText("Subtitle", subtitleChip.transform, font, 16, TextAnchor.MiddleCenter, UIStyle.PopupTextDim);
            subtitle.text = "Her gün sıfırlanır";
            subtitle.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(subtitle, 13, 16, UIFontRole.Popup);
            UIManager.Stretch(subtitle.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));
            missionsSubtitleText = subtitle;

            Image countdownChip = UIManager.CreateImage("CountdownChip", card.transform, Color.white);
            ApplyCandySectionRow(countdownChip, UIStyle.MenuBg);
            countdownChip.rectTransform.anchorMin = new Vector2(0.60f, 0.72f);
            countdownChip.rectTransform.anchorMax = new Vector2(0.82f, 0.79f);
            countdownChip.rectTransform.offsetMin = countdownChip.rectTransform.offsetMax = Vector2.zero;

            missionCountdownText = UIManager.CreateText("Countdown", countdownChip.transform, font, 17, TextAnchor.MiddleCenter, Color.white);
            missionCountdownText.text = "23:59:59";
            missionCountdownText.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(missionCountdownText, 14, 17, UIFontRole.Popup);
            UIManager.Stretch(missionCountdownText.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));

            Button closeButton = UIManager.CreateCandyCloseButton("CloseBtn", card.transform, font, 16);
            ApplyMainPremiumSurface(closeButton.targetGraphic as Image, "main_premium_icon_frame", "out_btn_purple", preserveAspect: true);
            UIManager.BindButton(closeButton, () => StartCoroutine(CloseSheet(missionsSheet, missionsOverlay)), buttonClickSound);
            RectTransform closeRt = (RectTransform)closeButton.transform;
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-22f, -18f);
            closeRt.sizeDelta = new Vector2(54f, 54f);
            closeButton.transform.SetAsLastSibling();

            GameObject listGo = new GameObject("List");
            listGo.transform.SetParent(card.transform, false);
            RectTransform listRt = listGo.AddComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0.05f, 0.06f);
            listRt.anchorMax = new Vector2(0.95f, 0.66f);
            listRt.offsetMin = listRt.offsetMax = Vector2.zero;

            const int missionCount = 2;
            missionClaimBtns = new GameObject[missionCount];
            missionIds = new string[missionCount];
            missionTitleTexts = new Text[missionCount];
            missionProgressTexts = new Text[missionCount];
            missionProgressFills = new Image[missionCount];
            missionRewardTexts = new Text[missionCount];
            missionRewardBackgrounds = new Image[missionCount];
            missionRewardButtons = new Button[missionCount];
            missionStatusTexts = new Text[missionCount];
            missionClaimButtonTexts = new Text[missionCount];

            for (int i = 0; i < missionCount; i++)
            {
                int idx = i;
                float yTop = 1f - i * 0.48f;
                Image missionCard = UIManager.CreateCard($"Mission{i}", listRt, Color.white, new Color(0f, 0f, 0f, 0f));
                ApplyCandySectionRow(missionCard, SheetSectionTint);
                missionCard.rectTransform.anchorMin = new Vector2(0f, yTop - 0.40f);
                missionCard.rectTransform.anchorMax = new Vector2(1f, yTop);
                missionCard.rectTransform.offsetMin = missionCard.rectTransform.offsetMax = Vector2.zero;

                Text mTitle = UIManager.CreateText("Title", missionCard.transform, font, 18, TextAnchor.UpperLeft, LeaderboardPremiumText);
                mTitle.text = "Görev";
                mTitle.fontStyle = FontStyle.Bold;
                mTitle.resizeTextForBestFit = true;
                UIManager.SetScaledBestFit(mTitle, 14, 18, UIFontRole.Popup);
                mTitle.rectTransform.anchorMin = new Vector2(0.04f, 0.62f);
                mTitle.rectTransform.anchorMax = new Vector2(0.70f, 0.90f);
                mTitle.rectTransform.offsetMin = mTitle.rectTransform.offsetMax = Vector2.zero;
                missionTitleTexts[i] = mTitle;

                Text progress = UIManager.CreateText("Progress", missionCard.transform, font, 15, TextAnchor.UpperRight, LeaderboardPremiumMutedText);
                progress.text = "0/0 tamamlandı";
                progress.resizeTextForBestFit = true;
                UIManager.SetScaledBestFit(progress, 12, 15, UIFontRole.Popup);
                progress.rectTransform.anchorMin = new Vector2(0.70f, 0.64f);
                progress.rectTransform.anchorMax = new Vector2(0.96f, 0.88f);
                progress.rectTransform.offsetMin = progress.rectTransform.offsetMax = Vector2.zero;
                missionProgressTexts[i] = progress;

                Sprite pillSprite = GetFlatPillSprite();

                Image barTrack = UIManager.CreateImage("BarTrack", missionCard.transform, Color.white);
                if (pillSprite != null)
                {
                    barTrack.sprite = pillSprite;
                    barTrack.type = Image.Type.Sliced;
                    barTrack.pixelsPerUnitMultiplier = 1f;
                }
                barTrack.color = new Color(0.055f, 0.020f, 0.100f, 0.95f);
                barTrack.rectTransform.anchorMin = new Vector2(0.04f, 0.39f);
                barTrack.rectTransform.anchorMax = new Vector2(0.96f, 0.51f);
                barTrack.rectTransform.offsetMin = barTrack.rectTransform.offsetMax = Vector2.zero;
                barTrack.raycastTarget = false;

                Image barFill = UIManager.CreateImage("BarFill", barTrack.transform, Color.white);
                if (pillSprite != null)
                {
                    barFill.sprite = pillSprite;
                    barFill.type = Image.Type.Sliced;
                    barFill.pixelsPerUnitMultiplier = 1f;
                }
                barFill.color = new Color(1f, 0.42f, 0.08f, 0.96f);
                barFill.rectTransform.anchorMin = new Vector2(0f, 0f);
                barFill.rectTransform.anchorMax = new Vector2(0f, 1f);
                barFill.rectTransform.offsetMin = barFill.rectTransform.offsetMax = Vector2.zero;
                barFill.raycastTarget = false;
                missionProgressFills[i] = barFill;

                Image rewardBg = UIManager.CreateImage("RewardBg", missionCard.transform, new Color(0f, 0f, 0f, 0.4f));
                ApplyMainPremiumSurface(rewardBg, "main_premium_cta", "out_btn_orange", preserveAspect: false);
                rewardBg.color = Color.white;
                rewardBg.rectTransform.anchorMin = new Vector2(0.65f, 0.12f);
                rewardBg.rectTransform.anchorMax = new Vector2(0.96f, 0.34f);
                rewardBg.rectTransform.offsetMin = rewardBg.rectTransform.offsetMax = Vector2.zero;
                rewardBg.raycastTarget = true;
                missionRewardBackgrounds[i] = rewardBg;

                Button rewardBtn = rewardBg.gameObject.AddComponent<Button>();
                rewardBtn.targetGraphic = rewardBg;
                rewardBtn.transition = Selectable.Transition.ColorTint;
                rewardBtn.interactable = false;
                missionRewardButtons[i] = rewardBtn;
                int rewardIdx = i;
                UIManager.BindButton(rewardBtn, () => _onClaimMission?.Invoke(missionIds[rewardIdx]), buttonClickSound);

                Text rewardText = UIManager.CreateText("Reward", rewardBg.transform, font, 15, TextAnchor.MiddleCenter, Color.white);
                rewardText.text = "+0 COIN";
                rewardText.fontStyle = FontStyle.Bold;
                rewardText.resizeTextForBestFit = true;
                UIManager.SetScaledBestFit(rewardText, 12, 15, UIFontRole.Popup);
                UIManager.Stretch(rewardText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                rewardText.raycastTarget = false;
                missionRewardTexts[i] = rewardText;

                Text statusText = UIManager.CreateText("Status", missionCard.transform, font, 16, TextAnchor.MiddleLeft, LeaderboardPremiumMutedText);
                statusText.text = "DEVAM";
                statusText.resizeTextForBestFit = true;
                UIManager.SetScaledBestFit(statusText, 13, 16, UIFontRole.Popup);
                statusText.rectTransform.anchorMin = new Vector2(0.04f, 0.10f);
                statusText.rectTransform.anchorMax = new Vector2(0.62f, 0.30f);
                statusText.rectTransform.offsetMin = statusText.rectTransform.offsetMax = Vector2.zero;
                missionStatusTexts[i] = statusText;

                Button claimBtn = UIManager.CreateButton("ClaimBtn", missionCard.transform, font, "CLAIM \u2713", Color.white, Color.white);
                ApplyMainPremiumSurface(claimBtn.targetGraphic as Image, "main_premium_cta", "out_btn_orange", preserveAspect: false);
                RectTransform claimBtnRt = (RectTransform)claimBtn.transform;
                claimBtnRt.anchorMin = new Vector2(0.04f, 0.10f);
                claimBtnRt.anchorMax = new Vector2(0.64f, 0.32f);
                claimBtnRt.offsetMin = claimBtnRt.offsetMax = Vector2.zero;
                UIManager.StyleButtonLabel(claimBtn, 16, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
                UIManager.BindButton(claimBtn, () => _onClaimMission?.Invoke(missionIds[idx]), buttonClickSound);
                claimBtn.gameObject.SetActive(false);
                missionClaimBtns[i] = claimBtn.gameObject;
                missionClaimButtonTexts[i] = claimBtn.GetComponentInChildren<Text>();
                if (missionClaimButtonTexts[i] != null)
                {
                    missionClaimButtonTexts[i].resizeTextForBestFit = true;
                    UIManager.SetScaledBestFit(missionClaimButtonTexts[i], 13, 16, UIFontRole.Button);
                }
            }

            missionsOverlay.SetActive(false);
            UIManager.ApplyPopupTextRoles(missionsOverlay.transform);
        }

        private string TranslateText(string tr, string en, string es)
        {
            return UILanguage.Translate(tr, en, es);
        }

        private string FormatBestCaption(float bestScore)
        {
            return $"{TranslateText("En iyi", "Best", "Mejor")}: {bestScore:0}m";
        }

        private string GetDailyChallengeTitle()
        {
            return TranslateText("Daily Challenge", "Daily Challenge", "Daily Challenge");
        }

        private string FormatDailyChallengeSubtitle()
        {
            if (cachedDailyChallengeStatus.seed == 0 || cachedDailyChallengeStatus.targetHeight <= 0)
            {
                return TranslateText("Hazirlaniyor...", "Preparing...", "Preparando...");
            }

            if (cachedDailyChallengeStatus.rewardClaimed)
            {
                float bestHeight = Mathf.Max(0f, cachedDailyChallengeStatus.bestHeight);
                return TranslateText(
                    $"En iyi {bestHeight:0.0}m\nOdul alindi",
                    $"Best {bestHeight:0.0}m\nReward claimed",
                    $"Mejor {bestHeight:0.0}m\nPremio cobrado");
            }

            return TranslateText(
                $"Hedef {cachedDailyChallengeStatus.targetHeight}m\n+{cachedDailyChallengeStatus.firstClearReward} {GetCoinWord()}",
                $"Target {cachedDailyChallengeStatus.targetHeight}m\n+{cachedDailyChallengeStatus.firstClearReward} {GetCoinWord()}",
                $"Meta {cachedDailyChallengeStatus.targetHeight}m\n+{cachedDailyChallengeStatus.firstClearReward} {GetCoinWord()}");
        }

        private void RefreshDailyChallengeButton()
        {
            if (dailyChallengeButton == null || dailyChallengeButtonImage == null)
            {
                return;
            }

            bool isReady = cachedDailyChallengeStatus.seed != 0 && cachedDailyChallengeStatus.targetHeight > 0;
            dailyChallengeButton.interactable = isReady;
            dailyChallengeButtonImage.color = isReady
                ? Color.white
                : new Color(0.78f, 0.78f, 0.88f, 0.92f);

            if (dailyChallengeTitleText != null)
            {
                dailyChallengeTitleText.text = GetDailyChallengeTitle();
                dailyChallengeTitleText.color = isReady
                    ? Color.white
                    : new Color(1f, 1f, 1f, 0.70f);
            }

            RefreshDailyChallengePopup();
        }

        private void BuildDailyChallengePopup(Font font)
        {
            dailyChallengePopupOverlay = new GameObject("DailyChallengePopupOverlay");
            dailyChallengePopupOverlay.transform.SetParent(transform, false);
            RectTransform overlayRt = dailyChallengePopupOverlay.AddComponent<RectTransform>();
            UIManager.Stretch(overlayRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            dailyChallengePopupOverlay.AddComponent<Image>().color = SheetOverlayTint;
            dailyChallengePopupOverlay.AddComponent<CanvasGroup>().alpha = 1f;

            GameObject card = new GameObject("DailyChallengeCard");
            card.transform.SetParent(dailyChallengePopupOverlay.transform, false);
            dailyChallengePopupSheet = card.AddComponent<RectTransform>();
            dailyChallengePopupSheet.anchorMin = new Vector2(0.06f, 0.22f);
            dailyChallengePopupSheet.anchorMax = new Vector2(0.94f, 0.78f);
            dailyChallengePopupSheet.offsetMin = dailyChallengePopupSheet.offsetMax = Vector2.zero;
            Image cardImage = card.AddComponent<Image>();
            ApplyCandySheetPanel(cardImage);
            card.AddComponent<CanvasGroup>().alpha = 0f;

            CreateCandyTitleRibbon(card.transform, font, "DailyChallengeTitle",
                "Daily Challenge", new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.97f), 22);

            Button closeButton = UIManager.CreateCandyCloseButton("CloseBtn", card.transform, font, 16);
            UIManager.BindButton(closeButton, () => StartCoroutine(CloseSheet(dailyChallengePopupSheet, dailyChallengePopupOverlay)), buttonClickSound);
            RectTransform closeRt = (RectTransform)closeButton.transform;
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-22f, -18f);
            closeRt.sizeDelta = new Vector2(54f, 54f);
            closeButton.transform.SetAsLastSibling();

            // Target row
            Image targetRow = UIManager.CreateImage("TargetRow", card.transform, Color.white);
            ApplyCandySectionRow(targetRow, SheetSectionTint);
            targetRow.rectTransform.anchorMin = new Vector2(0.08f, 0.71f);
            targetRow.rectTransform.anchorMax = new Vector2(0.92f, 0.83f);
            targetRow.rectTransform.offsetMin = targetRow.rectTransform.offsetMax = Vector2.zero;

            dailyChallengePopupTargetText = UIManager.CreateText("TargetText", targetRow.transform, font, 17, TextAnchor.MiddleCenter, UIStyle.PopupText);
            dailyChallengePopupTargetText.fontStyle = FontStyle.Bold;
            dailyChallengePopupTargetText.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(dailyChallengePopupTargetText, 14, 17, UIFontRole.Popup);
            UIManager.Stretch(dailyChallengePopupTargetText.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 0f), new Vector2(-12f, 0f));

            // Reward row
            Image rewardRow = UIManager.CreateImage("RewardRow", card.transform, Color.white);
            ApplyCandySectionRow(rewardRow, UIStyle.MenuBg);
            rewardRow.rectTransform.anchorMin = new Vector2(0.08f, 0.57f);
            rewardRow.rectTransform.anchorMax = new Vector2(0.92f, 0.69f);
            rewardRow.rectTransform.offsetMin = rewardRow.rectTransform.offsetMax = Vector2.zero;

            dailyChallengePopupRewardText = UIManager.CreateText("RewardText", rewardRow.transform, font, 17, TextAnchor.MiddleCenter, Color.white);
            dailyChallengePopupRewardText.fontStyle = FontStyle.Bold;
            dailyChallengePopupRewardText.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(dailyChallengePopupRewardText, 14, 17, UIFontRole.Popup);
            UIManager.Stretch(dailyChallengePopupRewardText.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 0f), new Vector2(-12f, 0f));

            // Modifier row
            Image modifierRow = UIManager.CreateImage("ModifierRow", card.transform, Color.white);
            ApplyCandySectionRow(modifierRow, SheetSectionTint);
            modifierRow.rectTransform.anchorMin = new Vector2(0.08f, 0.43f);
            modifierRow.rectTransform.anchorMax = new Vector2(0.92f, 0.55f);
            modifierRow.rectTransform.offsetMin = modifierRow.rectTransform.offsetMax = Vector2.zero;

            dailyChallengePopupModifierText = UIManager.CreateText("ModifierText", modifierRow.transform, font, 15, TextAnchor.MiddleCenter, UIStyle.PopupText);
            dailyChallengePopupModifierText.fontStyle = FontStyle.Bold;
            dailyChallengePopupModifierText.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(dailyChallengePopupModifierText, 12, 15, UIFontRole.Popup);
            UIManager.Stretch(dailyChallengePopupModifierText.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 0f), new Vector2(-12f, 0f));

            // Status text
            dailyChallengePopupStatusText = UIManager.CreateText("StatusText", card.transform, font, 14, TextAnchor.MiddleCenter, UIStyle.PopupTextDim);
            dailyChallengePopupStatusText.fontStyle = FontStyle.Bold;
            dailyChallengePopupStatusText.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(dailyChallengePopupStatusText, 11, 14, UIFontRole.Popup);
            Shadow statusShadow = dailyChallengePopupStatusText.gameObject.AddComponent<Shadow>();
            statusShadow.effectColor = new Color(0, 0, 0, 0.35f);
            statusShadow.effectDistance = new Vector2(0, -1.5f);
            Outline statusOutline = dailyChallengePopupStatusText.gameObject.AddComponent<Outline>();
            statusOutline.effectColor = new Color(0, 0, 0, 0.25f);
            statusOutline.effectDistance = new Vector2(0.5f, -0.5f);
            dailyChallengePopupStatusText.rectTransform.anchorMin = new Vector2(0.08f, 0.30f);
            dailyChallengePopupStatusText.rectTransform.anchorMax = new Vector2(0.92f, 0.42f);
            dailyChallengePopupStatusText.rectTransform.offsetMin = dailyChallengePopupStatusText.rectTransform.offsetMax = Vector2.zero;

            // Disclaimer text (does not affect best score / leaderboard)
            dailyChallengePopupDisclaimerText = UIManager.CreateText("DisclaimerText", card.transform, font, 12, TextAnchor.MiddleCenter, UIStyle.PopupTextDim);
            dailyChallengePopupDisclaimerText.fontStyle = FontStyle.Italic;
            dailyChallengePopupDisclaimerText.resizeTextForBestFit = true;
            dailyChallengePopupDisclaimerText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIManager.SetScaledBestFit(dailyChallengePopupDisclaimerText, 10, 12, UIFontRole.Popup);
            dailyChallengePopupDisclaimerText.rectTransform.anchorMin = new Vector2(0.06f, 0.19f);
            dailyChallengePopupDisclaimerText.rectTransform.anchorMax = new Vector2(0.94f, 0.28f);
            dailyChallengePopupDisclaimerText.rectTransform.offsetMin = dailyChallengePopupDisclaimerText.rectTransform.offsetMax = Vector2.zero;

            // Play button
            GameObject playBtnGo = new GameObject("PlayBtn");
            playBtnGo.transform.SetParent(card.transform, false);
            RectTransform playRt = playBtnGo.AddComponent<RectTransform>();
            playRt.anchorMin = new Vector2(0.15f, 0.06f);
            playRt.anchorMax = new Vector2(0.85f, 0.18f);
            playRt.offsetMin = playRt.offsetMax = Vector2.zero;
            dailyChallengePopupPlayImage = playBtnGo.AddComponent<Image>();
            SetupCandyButton(dailyChallengePopupPlayImage, "out_btn_orange", new Vector4(150f, 130f, 150f, 130f), 350f);
            dailyChallengePopupPlayButton = playBtnGo.AddComponent<Button>();
            dailyChallengePopupPlayButton.targetGraphic = dailyChallengePopupPlayImage;
            UIManager.BindButton(dailyChallengePopupPlayButton, () =>
            {
                StartCoroutine(UIStyle.ButtonPress(playRt));
                StartCoroutine(CloseSheet(dailyChallengePopupSheet, dailyChallengePopupOverlay));
                storedOnPlayDailyChallenge?.Invoke();
            }, buttonClickSound);

            dailyChallengePopupPlayLabel = UIManager.CreateText("PlayLabel", playBtnGo.transform, font, 20, TextAnchor.MiddleCenter, Color.white);
            dailyChallengePopupPlayLabel.fontStyle = FontStyle.Bold;
            dailyChallengePopupPlayLabel.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(dailyChallengePopupPlayLabel, 16, 20, UIFontRole.Button);
            UIManager.Stretch(dailyChallengePopupPlayLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            dailyChallengePopupOverlay.SetActive(false);
            UIManager.ApplyPopupTextRoles(dailyChallengePopupOverlay.transform);
        }

        private void ShowDailyChallengePopup()
        {
            if (dailyChallengePopupOverlay == null || dailyChallengePopupSheet == null) return;
            RefreshDailyChallengePopup();
            dailyChallengePopupOverlay.SetActive(true);
            dailyChallengePopupSheet.gameObject.SetActive(true);
            float h = dailyChallengePopupSheet.rect.height > 1f ? dailyChallengePopupSheet.rect.height : 800f;
            StartCoroutine(UIStyle.SlideUp(dailyChallengePopupSheet, h, 0.25f));
        }

        private void RefreshDailyChallengePopup()
        {
            bool played = economyManager != null && economyManager.HasPlayedDailyChallengeToday;
            DailyChallengeStatus status = cachedDailyChallengeStatus;

            if (dailyChallengePopupTargetText != null)
            {
                dailyChallengePopupTargetText.text = TranslateText(
                    $"Hedef: {status.targetHeight}m",
                    $"Target: {status.targetHeight}m",
                    $"Meta: {status.targetHeight}m");
            }

            if (dailyChallengePopupRewardText != null)
            {
                dailyChallengePopupRewardText.text = $"+{status.firstClearReward} {GetCoinWord()}";
            }

            if (dailyChallengePopupModifierText != null)
            {
                string primary = FormatModifierName(status.primaryModifier);
                string secondary = FormatModifierName(status.secondaryModifier);
                if (!string.IsNullOrEmpty(primary) && !string.IsNullOrEmpty(secondary))
                    dailyChallengePopupModifierText.text = $"{primary} + {secondary}";
                else if (!string.IsNullOrEmpty(primary))
                    dailyChallengePopupModifierText.text = primary;
                else
                    dailyChallengePopupModifierText.text = TranslateText("Standart", "Standard", "Estandar");
            }

            if (dailyChallengePopupStatusText != null)
            {
                dailyChallengePopupStatusText.gameObject.SetActive(false);
            }

            if (dailyChallengePopupPlayButton != null)
            {
                dailyChallengePopupPlayButton.interactable = !played;
            }

            if (dailyChallengePopupPlayImage != null)
            {
                dailyChallengePopupPlayImage.color = played ? new Color(0.6f, 0.6f, 0.6f, 0.8f) : Color.white;
            }

            if (dailyChallengePopupPlayLabel != null)
            {
                dailyChallengePopupPlayLabel.text = TranslateText("OYNA", "PLAY", "JUGAR");
                dailyChallengePopupPlayLabel.color = played ? new Color(1f, 1f, 1f, 0.5f) : Color.white;
            }

            if (dailyChallengePopupDisclaimerText != null)
            {
                dailyChallengePopupDisclaimerText.text = GetDailyChallengeDisclaimer();
            }
        }

        private static string FormatModifierName(RunModifierType modifier)
        {
            switch (modifier)
            {
                case RunModifierType.Slipstream: return "Slipstream";
                case RunModifierType.HighStakes: return "High Stakes";
                default: return string.Empty;
            }
        }

        private string GetDailyChallengeDisclaimer()
        {
            return TranslateText(
                "En iyi skoru ve liderlik tablosunu etkilemez",
                "Does not affect best score or leaderboard",
                "No afecta al mejor puntaje ni al ranking");
        }

        private string FormatLeaderboardSubtitle(float bestScore, string rankText)
        {
            string safeRank = string.IsNullOrWhiteSpace(rankText) ? "--" : rankText;
            return $"{TranslateText("Sen", "You", "Tu")}: {bestScore:0}m - #{safeRank}";
        }

        private IEnumerator RowAlphaPulse(Image bg, Color normalColor, float minA, float maxA, float period)
        {
            float half = period / 2f;
            while (bg != null)
            {
                float t = 0f;
                while (t < half)
                {
                    if (bg == null) yield break;
                    Color c = normalColor;
                    c.a = Mathf.Lerp(minA, maxA, Mathf.SmoothStep(0, 1, t / half));
                    bg.color = c;
                    t += Time.deltaTime;
                    yield return null;
                }
                t = 0f;
                while (t < half)
                {
                    if (bg == null) yield break;
                    Color c = normalColor;
                    c.a = Mathf.Lerp(maxA, minA, Mathf.SmoothStep(0, 1, t / half));
                    bg.color = c;
                    t += Time.deltaTime;
                    yield return null;
                }
            }
        }

        private void RefreshLeaderboardRows(bool resetPosition)
        {
            if (leaderboardContentRect == null)
            {
                return;
            }

            // Active list depends on the selected tab. Endless uses the cached cloud
            // (or fallback local) entries; Chapter uses the dedicated cloud chapter list.
            bool chapterMode = currentLeaderboardMode == LeaderboardMode.Chapter;
            IReadOnlyList<LeaderboardEntry> activeEntries = chapterMode
                ? cachedChapterLeaderboardEntries
                : cachedLeaderboardEntries;

            int targetRowCount = Mathf.Max(activeEntries?.Count ?? 0, 4);
            EnsureLeaderboardRowCount(targetRowCount);

            profileManager = profileManager != null ? profileManager : PlayerProfileManager.Instance ?? FindAnyObjectByType<PlayerProfileManager>();
            string localProfileName = profileManager != null ? PlayerProfileManager.ToLeaderboardNickname(profileManager.PlayerName) : string.Empty;
            string localPlayerName = PlayerPrefs.GetString("TowerMaze.Firebase.Nickname", PlayerPrefs.GetString("PlayerName", ""));
            string localUid = PlayerPrefs.GetString("TowerMaze.Firebase.Uid", string.Empty);
            string ownRankText = "--";

            if (ownRowPulseRoutine != null)
            {
                StopCoroutine(ownRowPulseRoutine);
                ownRowPulseRoutine = null;
            }

            for (int index = 0; index < leaderboardRowBgs.Count; index++)
            {
                bool visible = index < targetRowCount;
                leaderboardRowBgs[index].gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                bool hasEntry = activeEntries != null && index < activeEntries.Count;
                LeaderboardEntry entry = hasEntry ? activeEntries[index] : default;
                bool isOwnEntry = hasEntry && IsOwnLeaderboardEntry(entry, localUid, localProfileName, localPlayerName);
                if (isOwnEntry)
                {
                    ownRankText = (index + 1).ToString();
                }

                if (isOwnEntry)
                {
                    ownRowPulseRoutine = StartCoroutine(RowAlphaPulse(leaderboardRowBgs[index], LeaderboardPremiumOwnRowTint, 0.58f, 0.92f, 1.5f));
                }
                else
                {
                    leaderboardRowBgs[index].color = hasEntry ? LeaderboardPremiumRowTint : LeaderboardPremiumEmptyRowTint;
                }
                leaderboardRankTexts[index].text = (index + 1).ToString();
                leaderboardRankTexts[index].color = isOwnEntry
                    ? Color.white
                    : !hasEntry
                        ? new Color(0.48f, 0.60f, 0.74f, 0.62f)
                    : index switch
                    {
                        0 => LeaderboardPremiumGold,
                        1 => new Color(0.78f, 0.88f, 1f, 0.95f),
                        2 => new Color(1f, 0.55f, 0.24f, 0.95f),
                        _ => LeaderboardPremiumMutedText
                    };

                if (index < leaderboardAvatarBackers.Count && leaderboardAvatarBackers[index] != null)
                {
                    leaderboardAvatarBackers[index].gameObject.SetActive(hasEntry);
                }

                Image avatarIcon = leaderboardAvatarIcons[index];
                avatarIcon.enabled = hasEntry;
                if (hasEntry)
                {
                    Sprite avatarSprite = ResolveLeaderboardAvatar(entry, isOwnEntry);
                    if (avatarSprite != null)
                    {
                        avatarIcon.sprite = avatarSprite;
                        avatarIcon.color = Color.white;
                    }
                    else
                    {
                        avatarIcon.color = new Color(0.8f, 0.8f, 0.8f, 0.5f);
                    }
                }

                Image avatarFrame = leaderboardAvatarFrames[index];
                if (hasEntry && !string.IsNullOrEmpty(entry.avatarFrameId) && entry.avatarFrameId != "none")
                {
                    avatarFrame.enabled = true;
                    AvatarFrameDefinition frameDef = economyManager != null ? economyManager.GetAvatarFrame(entry.avatarFrameId) : default;
                    
                    Sprite frameSprite = Resources.Load<Sprite>($"TowerMaze/UITheme/frame_{entry.avatarFrameId}");
                    if (frameSprite != null)
                    {
                        avatarFrame.sprite = frameSprite;
                        avatarFrame.color = Color.white;
                    }
                    else
                    {
                        avatarFrame.color = frameDef.frameColor;
                        Sprite defaultFrame = Resources.Load<Sprite>("TowerMaze/UITheme/frame_default");
                        if (defaultFrame != null) avatarFrame.sprite = defaultFrame;
                    }
                }
                else
                {
                    avatarFrame.enabled = false;
                }

                leaderboardNameTexts[index].text = hasEntry
                    ? GetLeaderboardDisplayName(entry, index + 1, isOwnEntry)
                    : "---";
                leaderboardNameTexts[index].color = hasEntry ? (isOwnEntry ? Color.white : LeaderboardPremiumText) : LeaderboardPremiumMutedText;
                leaderboardNameTexts[index].rectTransform.anchorMin = hasEntry ? new Vector2(0.300f, 0f) : new Vector2(0.220f, 0f);
                leaderboardNameTexts[index].rectTransform.anchorMax = new Vector2(0.675f, 1f);
                // In chapter mode the height field carries the chapter number — the
                // formatter renders "CH 247" instead of the endless "247m".
                leaderboardScoreTexts[index].text = hasEntry
                    ? (chapterMode ? $"CH {entry.height:0}" : $"{entry.height:0}m")
                    : (chapterMode ? "CH --" : "0m");
                leaderboardScoreTexts[index].color = hasEntry
                    ? (isOwnEntry ? LeaderboardPremiumGold : LeaderboardPremiumCyan)
                    : new Color(0.84f, 0.62f, 0.92f, 0.72f);
                leaderboardRowOutlines[index].effectColor = isOwnEntry
                    ? new Color(1f, 0.45f, 0.10f, 0.46f)
                    : (hasEntry ? new Color(0.86f, 0.33f, 1f, 0.18f) : new Color(0.86f, 0.33f, 1f, 0.08f));
            }

            RefreshLeaderboardContentHeight(targetRowCount, resetPosition);

            cachedOwnRankText = ownRankText;
            if (leaderboardSubtitleText != null)
            {
                leaderboardSubtitleText.text = FormatLeaderboardSubtitle(cachedBestScoreValue, ownRankText);
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(leaderboardContentRect);
            if (resetPosition && leaderboardScrollRect != null)
            {
                leaderboardScrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void RefreshLeaderboardContentHeight(int rowCount, bool resetPosition)
        {
            if (leaderboardContentRect == null || leaderboardScrollRect == null)
            {
                return;
            }

            float rowsHeight = rowCount > 0 ? rowCount * LeaderboardRowHeight : 0f;
            float spacingHeight = rowCount > 1 ? (rowCount - 1) * LeaderboardRowSpacing : 0f;
            float preferredHeight = rowsHeight + spacingHeight + LeaderboardContentBottomPadding;
            float viewportHeight = leaderboardScrollRect.viewport != null ? leaderboardScrollRect.viewport.rect.height : 0f;
            float contentHeight = Mathf.Max(preferredHeight, viewportHeight + 1f);
            leaderboardContentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
            if (resetPosition)
            {
                leaderboardContentRect.anchoredPosition = Vector2.zero;
            }
        }

        private void BuildLeaderboardTabStrip(Transform parent, Font font)
        {
            GameObject stripGo = new GameObject("LeaderboardTabStrip");
            stripGo.transform.SetParent(parent, false);
            RectTransform stripRt = stripGo.AddComponent<RectTransform>();
            stripRt.anchorMin = new Vector2(0.120f, 0.632f);
            stripRt.anchorMax = new Vector2(0.880f, 0.678f);
            stripRt.offsetMin = stripRt.offsetMax = Vector2.zero;

            leaderboardTabEndlessBg = UIManager.CreateImage("TabEndless", stripGo.transform, Color.white);
            ApplyLeaderboardPremiumPlate(leaderboardTabEndlessBg, LeaderboardPremiumActiveTabTint, new Color(1f, 0.74f, 0.22f, 0.62f), new Vector2(2f, -2f));
            leaderboardTabEndlessBg.rectTransform.anchorMin = new Vector2(0f, 0f);
            leaderboardTabEndlessBg.rectTransform.anchorMax = new Vector2(0.48f, 1f);
            leaderboardTabEndlessBg.rectTransform.offsetMin = leaderboardTabEndlessBg.rectTransform.offsetMax = Vector2.zero;
            leaderboardTabEndlessLabel = UIManager.CreateText("Label", leaderboardTabEndlessBg.transform, font, 15, TextAnchor.MiddleCenter, Color.white, UIFontRole.Popup);
            leaderboardTabEndlessLabel.fontStyle = FontStyle.Bold;
            UIManager.Stretch(leaderboardTabEndlessLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 2f), new Vector2(0f, -1f));
            UIManager.SetScaledBestFit(leaderboardTabEndlessLabel, 12, 15, UIFontRole.Popup);
            AddLeaderboardTextChrome(leaderboardTabEndlessLabel, new Color(0f, 0f, 0f, 0.35f), new Color(1f, 0.80f, 0.28f, 0.18f), new Vector2(0f, -1.5f), new Vector2(0.7f, -0.7f));
            Button endlessBtn = leaderboardTabEndlessBg.gameObject.AddComponent<Button>();
            endlessBtn.targetGraphic = leaderboardTabEndlessBg;
            UIManager.BindButton(endlessBtn, () => SetLeaderboardMode(LeaderboardMode.Endless), null);

            leaderboardTabChapterBg = UIManager.CreateImage("TabChapter", stripGo.transform, Color.white);
            ApplyLeaderboardPremiumPlate(leaderboardTabChapterBg, LeaderboardPremiumInactiveTabTint, new Color(0.28f, 0.94f, 0.46f, 0.34f), new Vector2(2f, -2f));
            leaderboardTabChapterBg.rectTransform.anchorMin = new Vector2(0.52f, 0f);
            leaderboardTabChapterBg.rectTransform.anchorMax = new Vector2(1f, 1f);
            leaderboardTabChapterBg.rectTransform.offsetMin = leaderboardTabChapterBg.rectTransform.offsetMax = Vector2.zero;
            leaderboardTabChapterLabel = UIManager.CreateText("Label", leaderboardTabChapterBg.transform, font, 15, TextAnchor.MiddleCenter, Color.white, UIFontRole.Popup);
            leaderboardTabChapterLabel.fontStyle = FontStyle.Bold;
            UIManager.Stretch(leaderboardTabChapterLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 2f), new Vector2(0f, -1f));
            UIManager.SetScaledBestFit(leaderboardTabChapterLabel, 12, 15, UIFontRole.Popup);
            AddLeaderboardTextChrome(leaderboardTabChapterLabel, new Color(0f, 0f, 0f, 0.35f), new Color(1f, 0.56f, 0.18f, 0.14f), new Vector2(0f, -1.5f), new Vector2(0.7f, -0.7f));
            Button chapterBtn = leaderboardTabChapterBg.gameObject.AddComponent<Button>();
            chapterBtn.targetGraphic = leaderboardTabChapterBg;
            UIManager.BindButton(chapterBtn, () => SetLeaderboardMode(LeaderboardMode.Chapter), null);

            ApplyLeaderboardTabVisuals();
        }

        private void SetLeaderboardMode(LeaderboardMode mode)
        {
            if (currentLeaderboardMode == mode) return;
            currentLeaderboardMode = mode;
            ApplyLeaderboardTabVisuals();
            RefreshLeaderboardRows(resetPosition: true);
        }

        private void ApplyLeaderboardTabVisuals()
        {
            bool endless = currentLeaderboardMode == LeaderboardMode.Endless;
            if (leaderboardTabEndlessLabel != null)
            {
                leaderboardTabEndlessLabel.text = UILanguage.Translate("ENDLESS", "ENDLESS", "INFINITO");
                leaderboardTabEndlessLabel.color = endless ? Color.white : new Color(0.88f, 0.76f, 0.92f, 0.82f);
            }
            if (leaderboardTabChapterLabel != null)
            {
                leaderboardTabChapterLabel.text = UILanguage.Translate("BOLUM", "CHAPTER", "NIVEL");
                leaderboardTabChapterLabel.color = endless ? new Color(0.88f, 0.76f, 0.92f, 0.82f) : Color.white;
            }
            if (leaderboardTabEndlessBg != null)
            {
                leaderboardTabEndlessBg.color = endless ? LeaderboardPremiumActiveTabTint : LeaderboardPremiumInactiveTabTint;
            }
            if (leaderboardTabChapterBg != null)
            {
                leaderboardTabChapterBg.color = endless ? LeaderboardPremiumInactiveTabTint : LeaderboardPremiumActiveTabTint;
            }
        }

        private void EnsureLeaderboardRowCount(int targetRowCount)
        {
            if (leaderboardContentRect == null || runtimeFont == null)
            {
                return;
            }

            while (leaderboardRowBgs.Count < targetRowCount)
            {
                int index = leaderboardRowBgs.Count;
                Image row = UIManager.CreateImage($"Row{index}", leaderboardContentRect, LeaderboardPremiumRowTint);
                row.raycastTarget = false;
                Shadow rowShadow = row.gameObject.AddComponent<Shadow>();
                rowShadow.effectColor = new Color(0f, 0f, 0f, 0.36f);
                rowShadow.effectDistance = new Vector2(0f, -3f);
                LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
                rowLayout.preferredHeight = LeaderboardRowHeight;

                Outline ownOutline = row.gameObject.AddComponent<Outline>();
                ownOutline.effectColor = new Color(1f, 0.64f, 0.18f, 0.24f);
                ownOutline.effectDistance = new Vector2(2f, -2f);

                Text rank = UIManager.CreateText("Rank", row.transform, runtimeFont, 15, TextAnchor.MiddleCenter, LeaderboardPremiumMutedText);
                rank.fontStyle = FontStyle.Bold;
                rank.rectTransform.anchorMin = new Vector2(0.04f, 0f);
                rank.rectTransform.anchorMax = new Vector2(0.14f, 1f);
                rank.rectTransform.offsetMin = rank.rectTransform.offsetMax = Vector2.zero;
                AddLeaderboardTextChrome(rank, new Color(0f, 0f, 0f, 0.32f), new Color(0f, 0f, 0f, 0.12f), new Vector2(0f, -1f), new Vector2(0.5f, -0.5f));

                GameObject avatarGroup = new GameObject("AvatarGroup");
                avatarGroup.transform.SetParent(row.transform, false);
                RectTransform avatarRt = avatarGroup.AddComponent<RectTransform>();
                avatarRt.anchorMin = new Vector2(0.150f, 0.10f);
                avatarRt.anchorMax = new Vector2(0.265f, 0.90f);
                avatarRt.offsetMin = avatarRt.offsetMax = Vector2.zero;
                Image avatarBacker = avatarGroup.AddComponent<Image>();
                avatarBacker.color = new Color(0.06f, 0.07f, 0.17f, 0.94f);
                avatarBacker.raycastTarget = false;
                Outline avatarBackerOutline = avatarGroup.AddComponent<Outline>();
                avatarBackerOutline.effectColor = new Color(1f, 0.76f, 0.22f, 0.62f);
                avatarBackerOutline.effectDistance = new Vector2(2f, -2f);

                GameObject avatarClip = new GameObject("AvatarClip");
                avatarClip.transform.SetParent(avatarRt, false);
                RectTransform avatarClipRt = avatarClip.AddComponent<RectTransform>();
                UIManager.Stretch(avatarClipRt, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);

                Image avatarClipImage = avatarClip.AddComponent<Image>();
                avatarClipImage.sprite = GetLeaderboardAvatarMaskSprite();
                avatarClipImage.color = Color.white;
                avatarClipImage.raycastTarget = false;

                Mask avatarClipMask = avatarClip.AddComponent<Mask>();
                avatarClipMask.showMaskGraphic = false;

                Image avatarIcon = UIManager.CreateImage("AvatarIcon", avatarClipRt, Color.white);
                avatarIcon.preserveAspect = true;
                UIManager.Stretch(avatarIcon.rectTransform, new Vector2(-0.06f, -0.06f), new Vector2(1.12f, 1.06f), Vector2.zero, Vector2.zero);
                avatarIcon.color = new Color(1f, 1f, 1f, 0.3f); // Placeholder alpha

                Image avatarFrame = UIManager.CreateImage("AvatarFrame", avatarRt, Color.white);
                avatarFrame.preserveAspect = true;
                avatarFrame.rectTransform.anchorMin = new Vector2(-0.05f, -0.05f);
                avatarFrame.rectTransform.anchorMax = new Vector2(1.05f, 1.05f);
                avatarFrame.rectTransform.offsetMin = avatarFrame.rectTransform.offsetMax = Vector2.zero;
                avatarFrame.enabled = false;

                Text nameText = UIManager.CreateText("Name", row.transform, runtimeFont, 14, TextAnchor.MiddleLeft, LeaderboardPremiumText);
                nameText.fontStyle = FontStyle.Bold;
                nameText.resizeTextForBestFit = true;
                UIManager.SetScaledBestFit(nameText, 12, 14, UIFontRole.Popup);
                nameText.rectTransform.anchorMin = new Vector2(0.300f, 0f);
                nameText.rectTransform.anchorMax = new Vector2(0.675f, 1f);
                nameText.rectTransform.offsetMin = nameText.rectTransform.offsetMax = Vector2.zero;
                AddLeaderboardTextChrome(nameText, new Color(0f, 0f, 0f, 0.30f), new Color(0f, 0f, 0f, 0.14f), new Vector2(0f, -1f), new Vector2(0.5f, -0.5f));

                Text scoreText = UIManager.CreateText("Score", row.transform, runtimeFont, 14, TextAnchor.MiddleRight, LeaderboardPremiumCyan);
                scoreText.fontStyle = FontStyle.Bold;
                scoreText.rectTransform.anchorMin = new Vector2(0.700f, 0f);
                scoreText.rectTransform.anchorMax = new Vector2(0.940f, 1f);
                scoreText.rectTransform.offsetMin = scoreText.rectTransform.offsetMax = Vector2.zero;
                AddLeaderboardTextChrome(scoreText, new Color(0f, 0f, 0f, 0.32f), new Color(0.28f, 0.94f, 0.46f, 0.14f), new Vector2(0f, -1f), new Vector2(0.5f, -0.5f));

                leaderboardRowBgs.Add(row);
                leaderboardRowOutlines.Add(ownOutline);
                leaderboardRankTexts.Add(rank);
                leaderboardAvatarIcons.Add(avatarIcon);
                leaderboardAvatarFrames.Add(avatarFrame);
                leaderboardAvatarBackers.Add(avatarBacker);
                leaderboardNameTexts.Add(nameText);
                leaderboardScoreTexts.Add(scoreText);
            }
        }

        private static Sprite GetLeaderboardAvatarMaskSprite()
        {
            if (leaderboardAvatarMaskSprite != null)
            {
                return leaderboardAvatarMaskSprite;
            }

            const int size = 96;
            const float radius = (size - 2f) * 0.5f;
            Vector2 center = new((size - 1f) * 0.5f, (size - 1f) * 0.5f);
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                name = "TowerMaze_LeaderboardAvatarMask",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(radius - distance + 1f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, true);
            leaderboardAvatarMaskSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            leaderboardAvatarMaskSprite.name = "TowerMaze_LeaderboardAvatarMask";
            return leaderboardAvatarMaskSprite;
        }

        private string GetLeaderboardPlaceholderName(int rank)
        {
            return $"{TranslateText("Oyuncu", "Player", "Jugador")} {rank}";
        }

        private bool IsOwnLeaderboardEntry(LeaderboardEntry entry, string localUid, string localProfileName, string localPlayerName)
        {
            if (!string.IsNullOrWhiteSpace(localUid) &&
                !string.IsNullOrWhiteSpace(entry.ownerId) &&
                string.Equals(entry.ownerId, localUid, StringComparison.Ordinal))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(localProfileName) &&
                string.Equals(entry.label, localProfileName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(localPlayerName) &&
                   string.Equals(entry.label, localPlayerName, StringComparison.OrdinalIgnoreCase);
        }

        private Sprite ResolveLeaderboardAvatar(LeaderboardEntry entry, bool isOwnEntry)
        {
            EnsureProfileAvatarSprites();
            if (isOwnEntry && profileManager != null && profileAvatarSprites != null && profileAvatarSprites.Count > 0)
            {
                int selectedIndex = Mathf.Clamp(profileManager.SelectedAvatarIndex, 0, profileAvatarSprites.Count - 1);
                return profileAvatarSprites[selectedIndex];
            }

            if (entry.hasProfileAvatar && profileAvatarSprites != null && profileAvatarSprites.Count > 0)
            {
                int avatarIndex = Mathf.Clamp(entry.avatarIndex, 0, profileAvatarSprites.Count - 1);
                return profileAvatarSprites[avatarIndex];
            }

            return Resources.Load<Sprite>("TowerMaze/UITheme/icon_avatar_default");
        }

        private string GetLeaderboardDisplayName(LeaderboardEntry entry, int rank, bool isOwnEntry)
        {
            if (isOwnEntry && profileManager != null && !string.IsNullOrWhiteSpace(profileManager.PlayerName))
            {
                return profileManager.PlayerName.ToUpperInvariant();
            }

            return string.IsNullOrWhiteSpace(entry.label) ? GetLeaderboardPlaceholderName(rank) : entry.label;
        }

        private void EnsureProfileAvatarSprites()
        {
            if (profileAvatarSprites != null && profileAvatarSprites.Count > 0)
            {
                return;
            }

            profileAvatarSprites = ProfileAvatarLibrary.LoadSprites();
        }

        private string GetMissionProgressSuffix()
        {
            return TranslateText("tamamlandi", "completed", "completado");
        }

        private void ApplyLocalizedTexts()
        {
            RefreshGraphicsQualityLabel();
            if (startButtonLabelText != null)
            {
                startButtonLabelText.text = TranslateText("BASLA", "START", "INICIAR");
            }

            if (captionText != null)
            {
                captionText.text = FormatBestCaption(cachedBestScoreValue);
            }

            if (shopButtonText != null)
            {
                shopButtonText.text = TranslateText("MAGAZA", "SHOP", "TIENDA");
            }

            if (missionsButtonText != null)
            {
                missionsButtonText.text = TranslateText("GOREVLER", "MISSIONS", "MISIONES");
            }

            RefreshDailyChallengeButton();

            if (settingsTitleText != null)
            {
                settingsTitleText.text = TranslateText("AYARLAR", "SETTINGS", "AJUSTES");
            }

            if (settingsAudioHeaderText != null)
            {
                settingsAudioHeaderText.text = TranslateText("SES", "AUDIO", "SONIDO");
            }

            if (settingsSoundLabelText != null)
            {
                settingsSoundLabelText.text = TranslateText("SES", "SOUND", "SONIDO");
            }

            if (settingsVibrationLabelText != null)
            {
                settingsVibrationLabelText.text = TranslateText("TITRESIM", "VIBRATION", "VIBRACION");
            }

            string onLabel = TranslateText("ACIK", "ON", "SI");
            string offLabel = TranslateText("KAPALI", "OFF", "NO");
            if (settingsSoundOnText != null) settingsSoundOnText.text = onLabel;
            if (settingsSoundOffText != null) settingsSoundOffText.text = offLabel;
            if (settingsVibOnText != null) settingsVibOnText.text = onLabel;
            if (settingsVibOffText != null) settingsVibOffText.text = offLabel;

            ApplySettingsToggleVisuals();

            if (languageLabelText != null)
            {
                languageLabelText.text = TranslateText("DIL", "LANGUAGE", "IDIOMA");
            }

            if (privacyButtonText != null)
            {
                privacyButtonText.text = GetPrivacyPolicyButtonLabel();
            }

            if (missionsTitleText != null)
            {
                missionsTitleText.text = TranslateText("Gunluk Gorevler", "Daily Missions", "Misiones Diarias");
            }

            if (missionsSubtitleText != null)
            {
                missionsSubtitleText.text = TranslateText("Her gun sifirlanir", "Resets every day", "Se reinicia cada dia");
            }

            if (leaderboardTitleText != null)
            {
                leaderboardTitleText.text = TranslateText("LIDERLIK", "LEADERBOARD", "RANKING");
            }

            if (leaderboardCloseButtonText != null)
            {
                leaderboardCloseButtonText.text = TranslateText("KAPAT", "CLOSE", "CERRAR");
            }

            if (leaderboardSubtitleText != null)
            {
                leaderboardSubtitleText.text = FormatLeaderboardSubtitle(cachedBestScoreValue, cachedOwnRankText);
            }

            RefreshLeaderboardRows(resetPosition: false);

            if (privacyTitleText != null)
            {
                privacyTitleText.text = GetPrivacyPolicyTitle();
            }

            if (privacySubtitleText != null)
            {
                privacySubtitleText.text = GetPrivacyPolicySubtitle();
            }

            if (privacyBodyText != null)
            {
                privacyBodyText.text = GetPrivacyPolicyBody();
                RefreshPrivacyScrollLayout(resetPosition: false);
            }

            if (missionClaimButtonTexts != null)
            {
                for (int index = 0; index < missionClaimButtonTexts.Length; index++)
                {
                    if (missionClaimButtonTexts[index] != null)
                    {
                        missionClaimButtonTexts[index].text = TranslateText("AL \u2713", "CLAIM \u2713", "COBRAR \u2713");
                    }
                }
            }

            RefreshMissionCards();
        }

        private void RefreshMissionCards()
        {
            if (missionTitleTexts == null || missionProgressTexts == null || missionProgressFills == null || missionRewardTexts == null || missionStatusTexts == null)
            {
                return;
            }

            int missionCount = missionTitleTexts.Length;
            for (int i = 0; i < missionCount; i++)
            {
                bool hasMission = cachedDailyMissions != null && i < cachedDailyMissions.Count;
                if (!hasMission)
                {
                    missionIds[i] = string.Empty;
                    missionTitleTexts[i].text = TranslateText("Gorev yok", "No mission", "Sin mision");
                    missionProgressTexts[i].text = $"0/0 {GetMissionProgressSuffix()}";
                    missionProgressFills[i].rectTransform.anchorMax = new Vector2(0f, 1f);
                    missionRewardTexts[i].text = $"+0 {GetCoinWord()}";
                    missionStatusTexts[i].text = TranslateText("PASIF", "INACTIVE", "INACTIVA");
                    missionStatusTexts[i].color = UIStyle.PopupTextDim;
                    missionStatusTexts[i].gameObject.SetActive(true);
                    if (missionClaimBtns != null && i < missionClaimBtns.Length && missionClaimBtns[i] != null)
                    {
                        missionClaimBtns[i].SetActive(false);
                    }

                    continue;
                }

                DailyMissionState mission = cachedDailyMissions[i];
                missionIds[i] = mission.id;
                missionTitleTexts[i].text = FormatMissionDescription(mission);
                int displayedProgress = mission.targetValue > 0
                    ? Mathf.Clamp(mission.progressValue, 0, mission.targetValue)
                    : Mathf.Max(0, mission.progressValue);
                missionProgressTexts[i].text = $"{displayedProgress}/{mission.targetValue} {GetMissionProgressSuffix()}";
                float progress = mission.targetValue > 0 ? Mathf.Clamp01((float)displayedProgress / mission.targetValue) : 0f;
                missionProgressFills[i].rectTransform.anchorMax = new Vector2(progress, 1f);
                missionRewardTexts[i].text = $"+{mission.rewardEmber} {GetCoinWord()}";

                bool completedUnclaimed = mission.IsCompleted && !mission.claimed;
                bool claimed = mission.claimed;
                missionStatusTexts[i].text = claimed
                    ? TranslateText("ALINDI", "CLAIMED", "RECLAMADA")
                    : (completedUnclaimed ? TranslateText("HAZIR! TIKLA", "READY! TAP", "LISTA! TOCA") : TranslateText("DEVAM", "IN PROGRESS", "EN CURSO"));
                missionStatusTexts[i].color = claimed ? UIStyle.PopupTextDim : (completedUnclaimed ? UIStyle.Owned : UIStyle.PopupTextDim);
                missionStatusTexts[i].gameObject.SetActive(true);

                // Hide the legacy separate claim button; we now use the orange reward
                // pill itself as the claim button.
                if (missionClaimBtns != null && i < missionClaimBtns.Length && missionClaimBtns[i] != null)
                {
                    missionClaimBtns[i].SetActive(false);
                }

                if (missionRewardButtons != null && i < missionRewardButtons.Length && missionRewardButtons[i] != null)
                {
                    bool canClaim = completedUnclaimed && _onClaimMission != null;
                    missionRewardButtons[i].interactable = canClaim;
                }
                if (missionRewardBackgrounds != null && i < missionRewardBackgrounds.Length && missionRewardBackgrounds[i] != null)
                {
                    if (claimed)
                    {
                        missionRewardBackgrounds[i].color = new Color(0.6f, 0.6f, 0.6f, 0.55f);
                    }
                    else if (completedUnclaimed)
                    {
                        missionRewardBackgrounds[i].color = Color.white;
                    }
                    else
                    {
                        missionRewardBackgrounds[i].color = new Color(1f, 1f, 1f, 0.7f);
                    }
                }
                if (missionRewardTexts[i] != null)
                {
                    missionRewardTexts[i].color = claimed ? new Color(1f, 1f, 1f, 0.5f) : Color.white;
                }
            }
        }

        private void ApplySettingsToggleVisuals()
        {
            Color onTint = new(1f, 0.48f, 0.10f, 1f);
            Color offTint = new(0.54f, 0.22f, 0.82f, 0.78f);

            if (settingsSoundToggleBg != null)
            {
                ApplyMainPremiumSurface(settingsSoundToggleBg, cachedSoundEnabled ? "main_premium_cta" : "main_premium_button", cachedSoundEnabled ? "out_btn_orange" : "out_btn_purple", preserveAspect: true);
                settingsSoundToggleBg.color = cachedSoundEnabled ? onTint : offTint;
                settingsSoundToggleBg.rectTransform.localScale = Vector3.one;
                if (settingsSoundToggleText != null)
                {
                    settingsSoundToggleText.text = cachedSoundEnabled ? TranslateText("ACIK", "ON", "ACTIVO") : TranslateText("KAPALI", "OFF", "INACTIVO");
                    settingsSoundToggleText.color = cachedSoundEnabled ? Color.white : new Color(1f, 1f, 1f, 0.72f);
                    settingsSoundToggleText.fontStyle = FontStyle.Bold;
                }
            }

            if (settingsVibToggleBg != null)
            {
                ApplyMainPremiumSurface(settingsVibToggleBg, cachedVibrationEnabled ? "main_premium_cta" : "main_premium_button", cachedVibrationEnabled ? "out_btn_orange" : "out_btn_purple", preserveAspect: true);
                settingsVibToggleBg.color = cachedVibrationEnabled ? onTint : offTint;
                settingsVibToggleBg.rectTransform.localScale = Vector3.one;
                if (settingsVibToggleText != null)
                {
                    settingsVibToggleText.text = cachedVibrationEnabled ? TranslateText("ACIK", "ON", "ACTIVO") : TranslateText("KAPALI", "OFF", "INACTIVO");
                    settingsVibToggleText.color = cachedVibrationEnabled ? Color.white : new Color(1f, 1f, 1f, 0.72f);
                    settingsVibToggleText.fontStyle = FontStyle.Bold;
                }
            }
        }

        private string FormatMissionDescription(DailyMissionState mission)
        {
            return mission.type switch
            {
                DailyMissionType.ReachHeightInRun => TranslateText($"Tek kosuda {mission.targetValue}m'ye ulas", $"Reach {mission.targetValue}m in one run", $"Alcanza {mission.targetValue}m en una partida"),
                DailyMissionType.ReachZoneInRun => TranslateText($"Bolge {mission.targetValue}'e ulas", $"Reach Zone {mission.targetValue}", $"Llega a la Zona {mission.targetValue}"),
                DailyMissionType.CompleteRuns => TranslateText($"{mission.targetValue} kosu tamamla", $"Complete {mission.targetValue} runs", $"Completa {mission.targetValue} partidas"),
                DailyMissionType.SurviveRushEvents => TranslateText($"{mission.targetValue} rush atlat", $"Survive {mission.targetValue} rushes", $"Sobrevive {mission.targetValue} rushes"),
                DailyMissionType.FinishWithoutContinue => TranslateText($"{mission.secondaryTargetValue}m ustunde {mission.targetValue} kosuyu devamsiz bitir", $"Finish {mission.targetValue} runs above {mission.secondaryTargetValue}m without continue", $"Termina {mission.targetValue} partidas por encima de {mission.secondaryTargetValue}m sin continuar"),
                DailyMissionType.StayNearLavaSeconds => TranslateText($"{mission.targetValue}s lavaya yakin kal", $"Stay near lava for {mission.targetValue}s", $"Permanece cerca de la lava durante {mission.targetValue}s"),
                DailyMissionType.SetNewBest => TranslateText("Yeni en iyi skor yap", "Set a new best", "Consigue un nuevo record"),
                DailyMissionType.PlayDailyChallenge => TranslateText("Gunluk meydan okumayi oyna", "Play the daily challenge", "Juega el desafio diario"),
                DailyMissionType.ReachHeightInDailyChallenge => TranslateText($"Gunluk meydan okumada {mission.targetValue}m'ye ulas", $"Reach {mission.targetValue}m in daily challenge", $"Alcanza {mission.targetValue}m en el desafio diario"),
                DailyMissionType.ReachHeightUnderTime => TranslateText($"{mission.secondaryTargetValue}s altinda {mission.targetValue}m'ye ulas", $"Reach {mission.targetValue}m under {mission.secondaryTargetValue}s", $"Alcanza {mission.targetValue}m en menos de {mission.secondaryTargetValue}s"),
                DailyMissionType.CompleteRunsWithModifier => TranslateText(
                    $"{EconomyManager.GetModifierDisplayName(ParseModifierType(mission.contextValue))} ile {mission.targetValue} kosu bitir",
                    $"Finish {mission.targetValue} {EconomyManager.GetModifierDisplayName(ParseModifierType(mission.contextValue))} run{(mission.targetValue > 1 ? "s" : string.Empty)}",
                    $"Completa {mission.targetValue} partida{(mission.targetValue > 1 ? "s" : string.Empty)} con {EconomyManager.GetModifierDisplayName(ParseModifierType(mission.contextValue))}"),
                _ => string.IsNullOrWhiteSpace(mission.description) ? TranslateText("Gorev", "Mission", "Mision") : mission.description,
            };
        }

        private static RunModifierType ParseModifierType(string value)
        {
            return Enum.TryParse(value, true, out RunModifierType parsed) ? parsed : RunModifierType.None;
        }

        private string GetCoinWord()
        {
            return TranslateText("COIN", "COIN", "MONEDA");
        }

        private string GetPrivacyPolicyTitle()
        {
            return TranslateText("Gizlilik Politikasi", "Privacy Policy", "Politica de Privacidad");
        }

        private string GetPrivacyPolicySubtitle()
        {
            return TranslateText("Son guncelleme: 28 Mart 2026", "Last updated: March 28, 2026", "Ultima actualizacion: 28 de marzo de 2026");
        }

        private string GetPrivacyPolicyButtonLabel()
        {
            return TranslateText("GIZLILIK", "PRIVACY", "PRIVACIDAD");
        }

        private string GetPrivacyPolicyBody()
        {
            return TranslateText(
                "Tower Maze Gizlilik Politikasi\n\n" +
                "Tower Maze, oyunun calismasi ve oyuncu deneyimini desteklemek icin sinirli veri isleyebilir.\n\n" +
                "1. Toplanan veriler\n" +
                "- Cihazinizda saklanan ilerleme, ayarlar, can durumu, skinler ve oyun ekonomisi.\n" +
                "- Odullu reklamlar ve uygulama ici satin alimlar icin gerekli teknik islem bilgileri.\n" +
                "- Liderlik tablosu ve bulut kaydi aciksa skor, oyuncu etiketi ve senkronizasyon verileri.\n\n" +
                "2. Verilerin kullanim amaci\n" +
                "- Oyunu kaydetmek ve geri yuklemek.\n" +
                "- Reklam odullerini ve satin alimlari islemek.\n" +
                "- Liderlik tablosu, bulut kaydi ve hesap senkronizasyonu saglamak.\n" +
                "- Guvenlik, hata ayiklama ve hizmet surekliligini desteklemek.\n\n" +
                "3. Ucuncu taraf servisler\n" +
                "Oyun reklam, satin alma, platform servisleri ve bulut / leaderboard ozellikleri icin ucuncu taraf servisler kullanabilir. Bu servisler kendi gizlilik politikalarina tabi olabilir.\n\n" +
                "4. Veri saklama\n" +
                "Veriler cihazinizda ve ilgili servisler etkinse bulutta saklanabilir. Uygulamayi silmeniz, uygulama verilerini temizlemeniz veya bulut kaydini sifirlamaniz halinde bu veriler kaldirilabilir.\n\n" +
                "5. Cocuklar ve ebeveynler\n" +
                "Oyunun kullanimi yasadiginiz bolgedeki kurallara uygun olmalidir. Cocuk kullanicilar icin ebeveyn / veli gozetimi onerilir.\n\n" +
                "6. Politika degisiklikleri\n" +
                "Bu gizlilik politikasi zaman zaman guncellenebilir. Oyunda gorunen son surum gecerli kabul edilir.\n\n" +
                "Iletisim\n" +
                "Gizlilik ile ilgili sorular icin oyunun magaza sayfasindaki destek kanalini kullanin.\n\n" +
                "─────────────\n" +
                "Muzik Telifleri\n" +
                "─────────────\n" +
                "\"A Journey Awaits!\" - Pierre Bondoerffer (pbondoer) - CC-BY-SA 3.0\n" +
                "\"Snow May Never End\" - Sindwiller - CC-BY-SA 3.0\n" +
                "\"Early Rain\" - Pyoescd-Association - CC-BY 4.0\n" +
                "\"Mysterious Ambience\" - cynicmusic - CC0\n" +
                "\"Starfield Romance\" - Yoiyami - CC0\n" +
                "Kaynak: opengameart.org",
                "Privacy Policy\n\n" +
                "Tower Maze may process limited data to run the game and support core player features.\n\n" +
                "1. Data we may collect\n" +
                "- Progress, settings, lives, skins, and economy data stored on your device.\n" +
                "- Technical transaction data needed for rewarded ads and in app purchases.\n" +
                "- Scores, player labels, and sync data when leaderboard or cloud save features are enabled.\n\n" +
                "2. Why this data is used\n" +
                "- To save and restore your game.\n" +
                "- To process rewarded ads and purchases.\n" +
                "- To provide leaderboard, cloud save, and account sync features.\n" +
                "- To support security, debugging, and service reliability.\n\n" +
                "3. Third party services\n" +
                "The game may use third party services for ads, purchases, platform features, and cloud / leaderboard functions. Those services may handle data under their own privacy policies.\n\n" +
                "4. Data storage\n" +
                "Data may be stored on your device and, when enabled, in cloud services. Data may be removed if you delete the app, clear app data, or reset cloud save data.\n\n" +
                "5. Children and parents\n" +
                "Use of the game should follow the rules that apply in your region. Parent or guardian supervision is recommended for children.\n\n" +
                "6. Changes to this policy\n" +
                "This privacy policy may be updated from time to time. The latest version shown in the game is the active version.\n\n" +
                "Contact\n" +
                "For privacy questions, use the support contact listed on the game's store page.\n\n" +
                "─────────────\n" +
                "Music Credits\n" +
                "─────────────\n" +
                "\"A Journey Awaits!\" by Pierre Bondoerffer (pbondoer) - CC-BY-SA 3.0\n" +
                "\"Snow May Never End\" by Sindwiller - CC-BY-SA 3.0\n" +
                "\"Early Rain\" by Pyoescd-Association - CC-BY 4.0\n" +
                "\"Mysterious Ambience\" by cynicmusic - CC0\n" +
                "\"Starfield Romance\" by Yoiyami - CC0\n" +
                "Source: opengameart.org",
                "Politica de Privacidad\n\n" +
                "Tower Maze puede procesar datos limitados para hacer funcionar el juego y sus funciones principales.\n\n" +
                "1. Datos que pueden recopilarse\n" +
                "- Progreso, ajustes, vidas, aspectos y datos de economia guardados en tu dispositivo.\n" +
                "- Datos tecnicos necesarios para anuncios con recompensa y compras dentro de la aplicacion.\n" +
                "- Puntuaciones, nombre del jugador y datos de sincronizacion cuando se usan tablas de clasificacion o guardado en la nube.\n\n" +
                "2. Para que se usan\n" +
                "- Para guardar y restaurar tu partida.\n" +
                "- Para procesar anuncios con recompensa y compras.\n" +
                "- Para ofrecer tablas de clasificacion, guardado en la nube y sincronizacion.\n" +
                "- Para seguridad, depuracion y estabilidad del servicio.\n\n" +
                "3. Servicios de terceros\n" +
                "El juego puede usar servicios de terceros para anuncios, compras, funciones de plataforma y servicios de nube / clasificacion. Esos servicios pueden tratar datos segun sus propias politicas.\n\n" +
                "4. Almacenamiento de datos\n" +
                "Los datos pueden guardarse en tu dispositivo y, si esta activo, en servicios en la nube. Pueden eliminarse al borrar la app, limpiar los datos o reiniciar el guardado en la nube.\n\n" +
                "5. Menores y familias\n" +
                "El uso del juego debe respetar las reglas aplicables en tu region. Se recomienda supervision de padres o tutores para menores.\n\n" +
                "6. Cambios en esta politica\n" +
                "Esta politica puede actualizarse ocasionalmente. La ultima version mostrada en el juego sera la version vigente.\n\n" +
                "Contacto\n" +
                "Para consultas de privacidad, usa el canal de soporte indicado en la pagina de la tienda del juego.\n\n" +
                "─────────────\n" +
                "Creditos Musicales\n" +
                "─────────────\n" +
                "\"A Journey Awaits!\" por Pierre Bondoerffer (pbondoer) - CC-BY-SA 3.0\n" +
                "\"Snow May Never End\" por Sindwiller - CC-BY-SA 3.0\n" +
                "\"Early Rain\" por Pyoescd-Association - CC-BY 4.0\n" +
                "\"Mysterious Ambience\" por cynicmusic - CC0\n" +
                "\"Starfield Romance\" por Yoiyami - CC0\n" +
                "Fuente: opengameart.org");
        }

        private void ApplyPortraitLayout()
        {
            float aspect = Screen.height / Mathf.Max(1f, (float)Screen.width);
            bool compactPortrait = aspect < 1.75f;
            bool tallPortrait = aspect > 2.05f;
            float topRowMinY = compactPortrait ? 0.895f : 0.905f;
            float topRowMaxY = compactPortrait ? 0.98f : 0.985f;
            float avatarMinX = compactPortrait ? 0.020f : 0.025f;
            float avatarMaxX = compactPortrait ? 0.205f : 0.200f;
            float lifeBarMinX = compactPortrait ? 0.240f : 0.235f;
            float lifeBarMaxX = compactPortrait ? 0.660f : 0.655f;
            float sideButtonMaxX = compactPortrait ? 0.405f : (tallPortrait ? 0.385f : 0.395f);
            float leaderboardMinX = compactPortrait ? 0.705f : 0.72f;
            float leaderboardMaxX = compactPortrait ? 0.855f : 0.865f;
            float settingsMinX = compactPortrait ? 0.865f : 0.875f;
            float settingsMaxX = compactPortrait ? 0.995f : 0.995f;

            float dailyChallengeMinY = compactPortrait ? 0.812f : (tallPortrait ? 0.828f : 0.818f);
            float dailyChallengeMaxY = compactPortrait ? 0.872f : (tallPortrait ? 0.888f : 0.878f);
            float dcHeight = dailyChallengeMaxY - dailyChallengeMinY;
            float endlessMaxY = dailyChallengeMinY - 0.010f;
            float endlessMinY = endlessMaxY - dcHeight;
            float logoMinY = compactPortrait ? 0.462f : (tallPortrait ? 0.492f : 0.480f);
            float logoBaseMaxY = compactPortrait ? 0.835f : (tallPortrait ? 0.885f : 0.865f);
            float logoMaxY = Mathf.Min(logoBaseMaxY, endlessMinY - (compactPortrait ? 0.008f : 0.010f));
            float startMinY = compactPortrait ? 0.395f : (tallPortrait ? 0.432f : 0.420f);
            float startMaxY = compactPortrait ? 0.508f : (tallPortrait ? 0.548f : 0.532f);
            float captionMinY = compactPortrait ? 0.35f : (tallPortrait ? 0.39f : 0.385f);
            float captionMaxY = compactPortrait ? 0.42f : (tallPortrait ? 0.46f : 0.445f);
            float secondaryMinY = compactPortrait ? 0.245f : (tallPortrait ? 0.305f : 0.285f);
            float secondaryMaxY = compactPortrait ? 0.335f : (tallPortrait ? 0.405f : 0.385f);
            int logoFontSize = compactPortrait ? 116 : (tallPortrait ? 140 : 128);

            if (profileAvatarRt != null)
            {
                profileAvatarRt.anchorMin = new Vector2(avatarMinX, topRowMinY - 0.018f);
                profileAvatarRt.anchorMax = new Vector2(avatarMaxX, topRowMaxY + 0.004f);
                profileAvatarRt.offsetMin = profileAvatarRt.offsetMax = Vector2.zero;
            }

            if (startLifeBarRt != null)
            {
                startLifeBarRt.anchorMin = new Vector2(lifeBarMinX, topRowMinY);
                startLifeBarRt.anchorMax = new Vector2(lifeBarMaxX, topRowMaxY);
            }

            if (dailyChallengeButtonRt != null)
            {
                dailyChallengeButtonRt.anchorMin = new Vector2(0.03f, dailyChallengeMinY);
                dailyChallengeButtonRt.anchorMax = new Vector2(sideButtonMaxX, dailyChallengeMaxY);
                dailyChallengeButtonRt.offsetMin = dailyChallengeButtonRt.offsetMax = Vector2.zero;
            }

            if (endlessButtonRt != null)
            {
                endlessButtonRt.anchorMin = new Vector2(0.03f, endlessMinY);
                endlessButtonRt.anchorMax = new Vector2(sideButtonMaxX, endlessMaxY);
                endlessButtonRt.offsetMin = endlessButtonRt.offsetMax = Vector2.zero;
            }

            if (leaderboardButtonRt != null)
            {
                leaderboardButtonRt.anchorMin = new Vector2(leaderboardMinX, topRowMinY);
                leaderboardButtonRt.anchorMax = new Vector2(leaderboardMaxX, topRowMaxY);
            }

            if (settingsButtonRt != null)
            {
                settingsButtonRt.anchorMin = new Vector2(settingsMinX, topRowMinY);
                settingsButtonRt.anchorMax = new Vector2(settingsMaxX, topRowMaxY);
            }

            if (logoRt != null)
            {
                logoRt.anchorMin = new Vector2(-0.12f, logoMinY);
                logoRt.anchorMax = new Vector2(1.12f, logoMaxY);
            }

            if (logoTextLayers.Count > 0)
            {
                for (int index = 0; index < logoTextLayers.Count; index++)
                {
                    if (logoTextLayers[index] != null)
                    {
                        UIManager.SetScaledFontSize(logoTextLayers[index], logoFontSize);
                    }
                }
            }

            if (startButtonRt != null)
            {
                startButtonRt.anchorMin = new Vector2(0.205f, startMinY);
                startButtonRt.anchorMax = new Vector2(0.795f, startMaxY);
            }

            if (captionText != null)
            {
                captionText.rectTransform.anchorMin = new Vector2(0.18f, captionMinY);
                captionText.rectTransform.anchorMax = new Vector2(0.82f, captionMaxY);
            }

            if (secondaryRowRt != null)
            {
                secondaryRowRt.anchorMin = new Vector2(0.105f, secondaryMinY);
                secondaryRowRt.anchorMax = new Vector2(0.895f, secondaryMaxY);
            }

            if (settingsPanelRt != null)
            {
                settingsPanelRt.anchorMin = compactPortrait ? new Vector2(0.05f, 0.20f) : new Vector2(0.08f, 0.22f);
                settingsPanelRt.anchorMax = compactPortrait ? new Vector2(0.95f, 0.88f) : new Vector2(0.92f, 0.86f);
            }

            if (leaderboardSheet != null)
            {
                leaderboardSheet.anchorMin = compactPortrait ? new Vector2(0.060f, 0.130f) : new Vector2(0.16f, 0.12f);
                leaderboardSheet.anchorMax = compactPortrait ? new Vector2(0.940f, 0.900f) : new Vector2(0.84f, 0.90f);
            }

            if (missionsSheet != null)
            {
                missionsSheet.anchorMin = compactPortrait ? new Vector2(0.04f, 0.08f) : new Vector2(0.06f, 0.10f);
                missionsSheet.anchorMax = compactPortrait ? new Vector2(0.96f, 0.90f) : new Vector2(0.94f, 0.88f);
            }

            if (privacySheet != null)
            {
                privacySheet.anchorMin = compactPortrait ? new Vector2(0.03f, 0.04f) : new Vector2(0.06f, 0.08f);
                privacySheet.anchorMax = compactPortrait ? new Vector2(0.97f, 0.96f) : new Vector2(0.94f, 0.92f);
            }
        }

        public void UpdateMissionCountdown(TimeSpan remaining)
        {
            if (missionCountdownText == null) return;
            missionCountdownText.text = $"{(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }

        private void RefreshPrivacyScrollLayout(bool resetPosition)
        {
            if (privacyBodyText == null || privacyBodyRect == null || privacyScrollRect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(privacyBodyRect);

            float viewportHeight = privacyScrollRect.viewport != null ? privacyScrollRect.viewport.rect.height : 0f;
            float contentHeight = Mathf.Max(privacyBodyText.preferredHeight + 24f, viewportHeight + 1f);
            privacyBodyRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(privacyBodyRect);

            if (resetPosition)
            {
                privacyScrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void EnsureMissionCountdownTicker()
        {
            if (!isActiveAndEnabled || missionCountdownRoutine != null)
            {
                return;
            }

            missionCountdownRoutine = StartCoroutine(MissionCountdownTicker());
        }

        private IEnumerator MissionCountdownTicker()
        {
            while (isActiveAndEnabled)
            {
                UpdateMissionCountdown(GetTimeUntilNextLocalDay());
                yield return new WaitForSecondsRealtime(1f);
            }

            missionCountdownRoutine = null;
        }

        private static TimeSpan GetTimeUntilNextLocalDay()
        {
            DateTime now = DateTime.Now;
            DateTime nextDay = now.Date.AddDays(1);
            TimeSpan remaining = nextDay - now;
            return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
        }
    }
}
