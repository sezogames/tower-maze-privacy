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
        private Action buttonClickSound;
        private Font runtimeFont;
        private Text bestScoreText;
        private Text captionText;
        private RectTransform startButtonRt;
        private LifeBarUI startLifeBarUI;
        private GameObject settingsPanel;
        private RectTransform settingsPanelRt;
        private CanvasGroup settingsPanelCanvasGroup;
        private Coroutine settingsPanelRoutine;
        private Image settingsSoundOnBg;
        private Image settingsSoundOffBg;
        private Image settingsVibOnBg;
        private Image settingsVibOffBg;
        private bool cachedSoundEnabled;
        private bool cachedVibrationEnabled;
        private Coroutine pulseCoroutine;
        private RectTransform leaderboardSheet;
        private RectTransform missionsSheet;
        private Text missionCountdownText;
        private System.Action<string> _onClaimMission;
        private GameObject[] missionClaimBtns;
        private string[] missionIds;
        private Text[] missionTitleTexts;
        private Text[] missionProgressTexts;
        private Image[] missionProgressFills;
        private Text[] missionRewardTexts;
        private Text[] missionStatusTexts;
        private Image[] leaderboardRowBgs;
        private Text[] leaderboardRankTexts;
        private Text[] leaderboardNameTexts;
        private Text[] leaderboardScoreTexts;
        private readonly string[] supportedLanguageCodes = { "TR", "EN", "ES" };
        private Image[] languageChipBackgrounds;
        private Text[] languageChipTexts;

        public void Initialize(Font font, ThemeDefinition theme, EconomyManager economy,
            Action onPlay, Action onPlayDailyChallenge, Action onOpenShop, Action onClaimChest,
            Action onToggleSound, Action onToggleVibration, Action onRerollMissions,
            Action onButtonClick = null)
        {
            economyManager = economy;
            buttonClickSound = onButtonClick;
            runtimeFont = font;

            // ── Full-screen background ─────────────────────────────────────────────
            Image bg = UIManager.CreateImage("StartBg", transform, UIStyle.MenuBg);
            UIManager.Stretch(bg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image lifeBarCard = UIManager.CreateCard("StartLifeBarCard", transform, UIStyle.SurfaceDark, UIStyle.BorderDark);
            lifeBarCard.rectTransform.anchorMin = new Vector2(0.03f, 0.89f);
            lifeBarCard.rectTransform.anchorMax = new Vector2(0.38f, 0.98f);
            lifeBarCard.rectTransform.offsetMin = lifeBarCard.rectTransform.offsetMax = Vector2.zero;
            startLifeBarUI = lifeBarCard.gameObject.AddComponent<LifeBarUI>();
            startLifeBarUI.Initialize(font);
            startLifeBarUI.SetEconomyManager(economyManager);

            // ── Top bar ───────────────────────────────────────────────────────────
            // Leaderboard icon button (🏅)
            var lbBtnGo = CreateIconCircle("LeaderboardBtn", transform, font, "\U0001f3c5",
                ShowLeaderboardSheet, buttonClickSound);
            var lbRt = lbBtnGo.GetComponent<RectTransform>();
            lbRt.anchorMin = new Vector2(0.74f, 0.89f);
            lbRt.anchorMax = new Vector2(0.86f, 0.98f);
            lbRt.offsetMin = lbRt.offsetMax = Vector2.zero;

            // Settings icon button (⚙)
            var settingsBtnGo = CreateIconCircle("SettingsBtn", transform, font, "\u2699",
                ShowSettingsPanel, buttonClickSound);
            var settingsRt = settingsBtnGo.GetComponent<RectTransform>();
            settingsRt.anchorMin = new Vector2(0.87f, 0.89f);
            settingsRt.anchorMax = new Vector2(0.99f, 0.98f);
            settingsRt.offsetMin = settingsRt.offsetMax = Vector2.zero;
            lbBtnGo.transform.SetAsLastSibling();
            settingsBtnGo.transform.SetAsLastSibling();

            // ── Logo ──────────────────────────────────────────────────────────────
            Text logo = UIManager.CreateText("Logo", transform, font, 36,
                TextAnchor.MiddleCenter, UIStyle.TextPrimary);
            logo.text = "TOWER MAZE";
            logo.fontStyle = FontStyle.Bold;
            logo.rectTransform.anchorMin = new Vector2(0.15f, 0.80f);
            logo.rectTransform.anchorMax = new Vector2(0.85f, 0.90f);
            logo.rectTransform.offsetMin = logo.rectTransform.offsetMax = Vector2.zero;

            // ── START button (GradientImage orange, Pulse on enable) ───────────
            var startGo = new GameObject("StartButton");
            startGo.transform.SetParent(transform, false);
            startButtonRt = startGo.AddComponent<RectTransform>();
            startButtonRt.anchorMin = new Vector2(0.20f, 0.65f);
            startButtonRt.anchorMax = new Vector2(0.80f, 0.74f);
            startButtonRt.offsetMin = startButtonRt.offsetMax = Vector2.zero;
            var startGrad = startGo.AddComponent<GradientImage>();
            startGrad.colorBottom = UIStyle.Action;
            startGrad.colorTop    = UIStyle.ActionLight;
            var startBtn = startGo.AddComponent<Button>();
            UIManager.BindButton(startBtn,
                () => { StartCoroutine(UIStyle.ButtonPress(startButtonRt)); onPlay?.Invoke(); }, null);
            var startLabel = UIManager.CreateText("Label", startGo.transform, font, 15,
                TextAnchor.MiddleCenter, Color.white);
            startLabel.text = "START";
            startLabel.fontStyle = FontStyle.Bold;
            UIManager.Stretch(startLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // ── Best score caption below START ────────────────────────────────
            captionText = UIManager.CreateText("BestCaption", transform, font, 12,
                TextAnchor.MiddleCenter, UIStyle.TextPrimary);
            captionText.text = "Best: 0m";
            captionText.rectTransform.anchorMin = new Vector2(0.20f, 0.60f);
            captionText.rectTransform.anchorMax = new Vector2(0.80f, 0.64f);
            captionText.rectTransform.offsetMin = captionText.rectTransform.offsetMax = Vector2.zero;

            // ── Secondary row: [SHOP] [📋 MISSIONS] at 65% alpha ─────────────
            var secondaryRow = new GameObject("SecondaryRow");
            secondaryRow.transform.SetParent(transform, false);
            var rowCg = secondaryRow.AddComponent<CanvasGroup>();
            rowCg.alpha = 0.65f;
            var rowRt = secondaryRow.AddComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0.20f, 0.52f);
            rowRt.anchorMax = new Vector2(0.80f, 0.59f);
            rowRt.offsetMin = rowRt.offsetMax = Vector2.zero;

            var shopBtnGo = CreateSecondaryButton("ShopBtn", secondaryRow.transform, font, "SHOP",
                () => onOpenShop?.Invoke(), buttonClickSound);
            var missionsBtnGo = CreateSecondaryButton("MissionsBtn", secondaryRow.transform, font,
                "\U0001f4cb MISSIONS", ShowMissionsSheet, buttonClickSound);
            LayoutTwoChildren(rowRt, shopBtnGo.GetComponent<RectTransform>(),
                missionsBtnGo.GetComponent<RectTransform>(), 8f);

            // ── Settings panel (built once, starts offscreen) ─────────────────
            BuildSettingsPanel(font, onToggleSound, onToggleVibration);

            // ── Bottom sheets (leaderboard + missions) ────────────────────────
            BuildLeaderboardSheet(font);
            BuildMissionsSheet(font);
        }

        private void OnEnable()
        {
            if (startButtonRt == null) return;
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            pulseCoroutine = StartCoroutine(UIStyle.Pulse(startButtonRt, 1f, 1.05f, 1.4f));
        }

        private void OnDisable()
        {
            if (pulseCoroutine != null) { StopCoroutine(pulseCoroutine); pulseCoroutine = null; }
            if (settingsPanelRoutine != null) { StopCoroutine(settingsPanelRoutine); settingsPanelRoutine = null; }
        }

        public void SetState(float bestScore, int emberBalance,
            IReadOnlyList<LeaderboardEntry> leaderboardEntries,
            IReadOnlyList<DailyMissionState> dailyMissions,
            DailyChestStatus chestStatus, DailyChallengeStatus challengeStatus,
            int missionRerollCost, bool soundEnabled, bool vibrationEnabled)
        {
            if (bestScoreText != null)
                bestScoreText.text = $"\U0001f3c6 {bestScore:0}m";
            if (captionText != null)
                captionText.text = $"Best: {bestScore:0}m";

            cachedSoundEnabled = soundEnabled;
            cachedVibrationEnabled = vibrationEnabled;

            // Update ON/OFF chips for Sound
            if (settingsSoundOnBg != null) settingsSoundOnBg.color = cachedSoundEnabled ? UIStyle.Brand : UIStyle.SurfaceDark;
            if (settingsSoundOffBg != null) settingsSoundOffBg.color = cachedSoundEnabled ? UIStyle.SurfaceDark : UIStyle.Brand;
            // Update ON/OFF chips for Vibration
            if (settingsVibOnBg != null) settingsVibOnBg.color = cachedVibrationEnabled ? UIStyle.Brand : UIStyle.SurfaceDark;
            if (settingsVibOffBg != null) settingsVibOffBg.color = cachedVibrationEnabled ? UIStyle.SurfaceDark : UIStyle.Brand;
            startLifeBarUI?.SetEconomyManager(economyManager);
            RefreshLanguageTabs();

            // Populate leaderboard rows and highlight local player's entry in brand purple.
            if (leaderboardRowBgs != null && leaderboardEntries != null)
            {
                string localPlayerName = PlayerPrefs.GetString("PlayerName", "");
                for (int i = 0; i < leaderboardRowBgs.Length; i++)
                {
                    if (i < leaderboardEntries.Count)
                    {
                        var entry = leaderboardEntries[i];
                        bool isOwnEntry = !string.IsNullOrEmpty(localPlayerName) && entry.label == localPlayerName;
                        leaderboardRowBgs[i].color = isOwnEntry
                            ? new Color(UIStyle.Brand.r, UIStyle.Brand.g, UIStyle.Brand.b, 0.15f)
                            : UIStyle.SurfaceDark;
                        leaderboardRankTexts[i].color = isOwnEntry ? UIStyle.Brand : (i == 0 ? UIStyle.Gold : UIStyle.TextDim);
                        leaderboardNameTexts[i].text = entry.label;
                        leaderboardScoreTexts[i].text = $"{entry.height:0}m";
                    }
                    else
                    {
                        leaderboardRowBgs[i].color = UIStyle.SurfaceDark;
                        leaderboardRankTexts[i].color = i == 0 ? UIStyle.Gold : UIStyle.TextDim;
                        leaderboardNameTexts[i].text = "---";
                        leaderboardScoreTexts[i].text = "0m";
                    }
                }
            }

            // Populate mission cards.
            if (missionTitleTexts != null && missionProgressTexts != null && missionProgressFills != null && missionRewardTexts != null && missionStatusTexts != null)
            {
                int missionCount = missionTitleTexts.Length;
                for (int i = 0; i < missionCount; i++)
                {
                    bool hasMission = dailyMissions != null && i < dailyMissions.Count;
                    if (!hasMission)
                    {
                        missionIds[i] = string.Empty;
                        missionTitleTexts[i].text = "No mission";
                        missionProgressTexts[i].text = "0/0";
                        missionProgressFills[i].rectTransform.anchorMax = new Vector2(0f, 1f);
                        missionRewardTexts[i].text = "+0 COIN";
                        missionStatusTexts[i].text = "INACTIVE";
                        missionStatusTexts[i].color = UIStyle.TextDim;
                        if (missionClaimBtns != null && i < missionClaimBtns.Length && missionClaimBtns[i] != null)
                        {
                            missionClaimBtns[i].SetActive(false);
                        }

                        continue;
                    }

                    DailyMissionState mission = dailyMissions[i];
                    missionIds[i] = mission.id;
                    missionTitleTexts[i].text = mission.description;
                    missionProgressTexts[i].text = $"{mission.progressValue}/{mission.targetValue}";
                    float progress = mission.targetValue > 0 ? Mathf.Clamp01((float)mission.progressValue / mission.targetValue) : 0f;
                    missionProgressFills[i].rectTransform.anchorMax = new Vector2(progress, 1f);
                    missionRewardTexts[i].text = $"+{mission.rewardEmber} COIN";

                    bool completedUnclaimed = mission.IsCompleted && !mission.claimed;
                    bool claimed = mission.claimed;
                    missionStatusTexts[i].text = claimed ? "CLAIMED" : (completedUnclaimed ? "COMPLETED" : "IN PROGRESS");
                    missionStatusTexts[i].color = claimed ? UIStyle.TextDim : (completedUnclaimed ? UIStyle.Owned : UIStyle.TextPrimary);
                    if (missionClaimBtns != null && i < missionClaimBtns.Length && missionClaimBtns[i] != null)
                    {
                        missionClaimBtns[i].SetActive(false);
                    }
                }
            }

            UpdateMissionCountdown(GetTimeUntilNextLocalDay());
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private GameObject CreateIconCircle(string name, Transform parent, Font font,
            string icon, Action onClick, Action sound)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.10f);
            RectTransform rt = go.GetComponent<RectTransform>();
            var btn = go.AddComponent<Button>();
            UIManager.BindButton(btn, () => {
                StartCoroutine(UIStyle.ButtonPress(go.GetComponent<RectTransform>())); onClick?.Invoke();
            }, sound);
            var lbl = UIManager.CreateText("Icon", go.transform, font, 13,
                TextAnchor.MiddleCenter, UIStyle.TextPrimary);
            lbl.text = icon;
            UIManager.Stretch(lbl.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return go;
        }

        private GameObject CreateSecondaryButton(string name, Transform parent, Font font,
            string label, Action onClick, Action sound)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.10f);
            var btn = go.AddComponent<Button>();
            UIManager.BindButton(btn, () => {
                StartCoroutine(UIStyle.ButtonPress(go.GetComponent<RectTransform>())); onClick?.Invoke();
            }, sound);
            var lbl = UIManager.CreateText("Label", go.transform, font, 10,
                TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.70f));
            lbl.text = label;
            UIManager.Stretch(lbl.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(8f, 0f), new Vector2(-8f, 0f));
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

        private void BuildSettingsPanel(Font font, Action onToggleSound, Action onToggleVibration)
        {
            // Dimmed overlay backer
            GameObject overlay = new GameObject("SettingsOverlay");
            overlay.transform.SetParent(transform, false);
            var overlayRt = overlay.AddComponent<RectTransform>();
            UIManager.Stretch(overlayRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.AddComponent<Image>().color = new Color(0, 0, 0, 0.75f);
            settingsPanel = overlay;
            settingsPanelCanvasGroup = overlay.AddComponent<CanvasGroup>();
            settingsPanelCanvasGroup.alpha = 0f;

            // Centered Modal Card
            GameObject card = new GameObject("SettingsCard");
            card.transform.SetParent(overlay.transform, false);
            settingsPanelRt = card.AddComponent<RectTransform>();
            settingsPanelRt.anchorMin = new Vector2(0.12f, 0.40f);
            settingsPanelRt.anchorMax = new Vector2(0.88f, 0.78f);
            settingsPanelRt.offsetMin = settingsPanelRt.offsetMax = Vector2.zero;
            card.AddComponent<Image>().color = UIStyle.ShopBg;

            var title = UIManager.CreateText("Title", card.transform, font, 18, TextAnchor.MiddleCenter, UIStyle.TextPrimary);
            title.text = "SETTINGS";
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.anchorMin = new Vector2(0f, 0.88f);
            title.rectTransform.anchorMax = new Vector2(1f, 0.98f);
            title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;

            var closeGo = CreateIconCircle("CloseBtn", card.transform, font, "\u00d7",
                HideSettingsPanel, buttonClickSound);
            var closeRt = closeGo.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.85f, 0.88f);
            closeRt.anchorMax = new Vector2(0.97f, 0.97f);
            closeRt.offsetMin = closeRt.offsetMax = Vector2.zero;

            BuildSettingsToggleRow("Sound", font, card.transform,
                ref settingsSoundOnBg, ref settingsSoundOffBg, 0.70f,
                () => { onToggleSound?.Invoke(); });

            BuildSettingsToggleRow("Vibration", font, card.transform,
                ref settingsVibOnBg, ref settingsVibOffBg, 0.55f,
                () => { onToggleVibration?.Invoke(); });

            // Language section
            var langLabel = UIManager.CreateText(card.transform, "LangLabel", "LANGUAGE",
                10, FontStyle.Bold, UIStyle.TextFaint, font, TextAnchor.MiddleLeft);
            langLabel.rectTransform.anchorMin = new Vector2(0.06f, 0.38f);
            langLabel.rectTransform.anchorMax = new Vector2(0.60f, 0.44f);
            langLabel.rectTransform.offsetMin = langLabel.rectTransform.offsetMax = Vector2.zero;

            Image langCard = UIManager.CreateCard("LanguageCard", card.transform, UIStyle.SurfaceDark, UIStyle.BorderDark);
            langCard.rectTransform.anchorMin = new Vector2(0.06f, 0.22f);
            langCard.rectTransform.anchorMax = new Vector2(0.95f, 0.38f);
            langCard.rectTransform.offsetMin = langCard.rectTransform.offsetMax = Vector2.zero;

            languageChipBackgrounds = new Image[supportedLanguageCodes.Length];
            languageChipTexts = new Text[supportedLanguageCodes.Length];
            for (int index = 0; index < supportedLanguageCodes.Length; index++)
            {
                SpawnLangButton(langCard.transform, supportedLanguageCodes[index], font, index);
            }

            RefreshLanguageTabs();

            settingsPanelRt.anchoredPosition = new Vector2(1200f, 0f);
            settingsPanel.SetActive(false);
        }

        private void BuildSettingsToggleRow(string labelStr, Font font, Transform parent,
            ref Image onChipBg, ref Image offChipBg, float anchorYMid, Action onToggle)
        {
            float h = 0.07f;
            var lbl = UIManager.CreateText("Lbl_" + labelStr, parent, font, 12,
                TextAnchor.MiddleLeft, UIStyle.TextPrimary);
            lbl.text = labelStr.ToUpper();
            lbl.rectTransform.anchorMin = new Vector2(0.06f, anchorYMid - h * 0.5f);
            lbl.rectTransform.anchorMax = new Vector2(0.60f, anchorYMid + h * 0.5f);
            lbl.rectTransform.offsetMin = lbl.rectTransform.offsetMax = Vector2.zero;

            // ON chip
            var onGo = new GameObject("On_" + labelStr);
            onGo.transform.SetParent(parent, false);
            onChipBg = onGo.AddComponent<Image>();
            onChipBg.color = UIStyle.Brand; // active by default; updated in SetState
            var onRt = onChipBg.rectTransform;
            onRt.anchorMin = new Vector2(0.63f, anchorYMid - 0.04f);
            onRt.anchorMax = new Vector2(0.78f, anchorYMid + 0.04f);
            onRt.offsetMin = onRt.offsetMax = Vector2.zero;
            var onBtn = onGo.AddComponent<Button>(); onBtn.targetGraphic = onChipBg;
            UIManager.BindButton(onBtn, () => { onToggle?.Invoke(); }, null);
            var onLbl = UIManager.CreateText("Lbl", onGo.transform, font, 11,
                TextAnchor.MiddleCenter, Color.white);
            onLbl.text = "ON";
            UIManager.Stretch(onLbl.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // OFF chip
            var offGo = new GameObject("Off_" + labelStr);
            offGo.transform.SetParent(parent, false);
            offChipBg = offGo.AddComponent<Image>();
            offChipBg.color = UIStyle.SurfaceDark; // inactive by default; updated in SetState
            var offRt = offChipBg.rectTransform;
            offRt.anchorMin = new Vector2(0.80f, anchorYMid - 0.04f);
            offRt.anchorMax = new Vector2(0.95f, anchorYMid + 0.04f);
            offRt.offsetMin = offRt.offsetMax = Vector2.zero;
            var offBtn = offGo.AddComponent<Button>(); offBtn.targetGraphic = offChipBg;
            UIManager.BindButton(offBtn, () => { onToggle?.Invoke(); }, null);
            var offLbl = UIManager.CreateText("Lbl", offGo.transform, font, 11,
                TextAnchor.MiddleCenter, UIStyle.TextDim);
            offLbl.text = "OFF";
            UIManager.Stretch(offLbl.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private void SpawnLangButton(Transform parent, string code, Font font, int index)
        {
            var go = new GameObject($"Lang_{code}");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            var rt = go.GetComponent<RectTransform>();
            float slotWidth = 1f / Mathf.Max(1, supportedLanguageCodes.Length);
            rt.anchorMin = new Vector2(slotWidth * index + 0.015f, 0.16f);
            rt.anchorMax = new Vector2(slotWidth * (index + 1) - 0.015f, 0.84f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var txt = UIManager.CreateText(go.transform, "T", code,
                12, FontStyle.Bold, Color.white, font, TextAnchor.MiddleCenter);
            UIManager.Stretch(txt.rectTransform);
            UIManager.BindButton(btn, () => {
                PlayerPrefs.SetString("Language", code);
                PlayerPrefs.Save();
                RefreshLanguageTabs();
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

            string currentCode = PlayerPrefs.GetString("Language", "TR");
            for (int index = 0; index < supportedLanguageCodes.Length; index++)
            {
                bool active = string.Equals(currentCode, supportedLanguageCodes[index], StringComparison.OrdinalIgnoreCase);
                if (languageChipBackgrounds[index] != null)
                {
                    languageChipBackgrounds[index].color = active ? UIStyle.Brand : new Color(1f, 1f, 1f, 0.12f);
                }

                if (languageChipTexts[index] != null)
                {
                    languageChipTexts[index].color = active ? Color.white : UIStyle.TextPrimary;
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
            if (leaderboardSheet == null) return;
            leaderboardSheet.gameObject.SetActive(true);
            float h = leaderboardSheet.rect.height > 1f ? leaderboardSheet.rect.height : 800f;
            StartCoroutine(UIStyle.SlideUp(leaderboardSheet, h, 0.25f));
        }

        private void ShowMissionsSheet()
        {
            if (missionsSheet == null) return;
            missionsSheet.gameObject.SetActive(true);
            float h = missionsSheet.rect.height > 1f ? missionsSheet.rect.height : 900f;
            StartCoroutine(UIStyle.SlideUp(missionsSheet, h, 0.25f));
        }

        private IEnumerator CloseSheet(RectTransform sheet)
        {
            float h = sheet.rect.height > 1f ? sheet.rect.height : 800f;
            yield return StartCoroutine(UIStyle.SlideDown(sheet, h, 0.20f));
            sheet.gameObject.SetActive(false);
        }

        private void CreateLeaderboardFlag(Transform parent, Color flagColor)
        {
            Image pole = UIManager.CreateImage("FlagPole", parent, new Color(1f, 1f, 1f, 0.45f));
            pole.rectTransform.anchorMin = new Vector2(0.03f, 0.24f);
            pole.rectTransform.anchorMax = new Vector2(0.035f, 0.78f);
            pole.rectTransform.offsetMin = pole.rectTransform.offsetMax = Vector2.zero;
            pole.raycastTarget = false;

            Image pennant = UIManager.CreateImage("FlagPennant", parent, flagColor);
            pennant.rectTransform.anchorMin = new Vector2(0.035f, 0.55f);
            pennant.rectTransform.anchorMax = new Vector2(0.11f, 0.76f);
            pennant.rectTransform.offsetMin = pennant.rectTransform.offsetMax = Vector2.zero;
            pennant.raycastTarget = false;
        }

        private void BuildLeaderboardSheet(Font font)
        {
            var go = new GameObject("LeaderboardSheet");
            go.transform.SetParent(transform, false);
            leaderboardSheet = go.AddComponent<RectTransform>();
            leaderboardSheet.anchorMin = new Vector2(0f, 0f);
            leaderboardSheet.anchorMax = new Vector2(1f, 0.65f);
            leaderboardSheet.offsetMin = leaderboardSheet.offsetMax = Vector2.zero;
            go.AddComponent<CanvasGroup>().alpha = 0f;
            go.AddComponent<Image>().color = UIStyle.ShopBg;

            var handle = UIManager.CreateImage("Handle", go.transform, new Color(1f, 1f, 1f, 0.15f));
            handle.rectTransform.anchorMin = new Vector2(0.35f, 0.965f);
            handle.rectTransform.anchorMax = new Vector2(0.65f, 0.985f);
            handle.rectTransform.offsetMin = handle.rectTransform.offsetMax = Vector2.zero;
            handle.raycastTarget = false;

            var title = UIManager.CreateText("Title", go.transform, font, 14,
                TextAnchor.MiddleLeft, UIStyle.TextPrimary);
            title.text = "\U0001f3c5 Top Runs";
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.anchorMin = new Vector2(0.04f, 0.87f);
            title.rectTransform.anchorMax = new Vector2(0.80f, 0.96f);
            title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;

            const int rowCount = 5;
            leaderboardRowBgs = new Image[rowCount];
            leaderboardRankTexts = new Text[rowCount];
            leaderboardNameTexts = new Text[rowCount];
            leaderboardScoreTexts = new Text[rowCount];

            for (int i = 0; i < rowCount; i++)
            {
                float yTop = 0.86f - i * 0.155f;
                Image row = UIManager.CreateCard($"Row{i}", go.transform, UIStyle.SurfaceDark, UIStyle.BorderDark);
                row.rectTransform.anchorMin = new Vector2(0.04f, yTop - 0.14f);
                row.rectTransform.anchorMax = new Vector2(0.96f, yTop);
                row.rectTransform.offsetMin = row.rectTransform.offsetMax = Vector2.zero;
                leaderboardRowBgs[i] = row;
                CreateLeaderboardFlag(row.transform, i == 0 ? UIStyle.Gold : UIStyle.Brand);

                var rank = UIManager.CreateText("Rank", row.transform, font, 11, TextAnchor.MiddleLeft,
                    i == 0 ? UIStyle.Gold : UIStyle.TextDim);
                rank.text = $"#{i + 1}";
                rank.rectTransform.anchorMin = new Vector2(0.12f, 0f);
                rank.rectTransform.anchorMax = new Vector2(0.24f, 1f);
                rank.rectTransform.offsetMin = rank.rectTransform.offsetMax = Vector2.zero;
                leaderboardRankTexts[i] = rank;

                var nameText = UIManager.CreateText("Name", row.transform, font, 10,
                    TextAnchor.MiddleLeft, UIStyle.TextPrimary);
                nameText.text = "---";
                nameText.rectTransform.anchorMin = new Vector2(0.25f, 0f);
                nameText.rectTransform.anchorMax = new Vector2(0.72f, 1f);
                nameText.rectTransform.offsetMin = nameText.rectTransform.offsetMax = Vector2.zero;
                leaderboardNameTexts[i] = nameText;

                var score = UIManager.CreateText("Score", row.transform, font, 10,
                    TextAnchor.MiddleRight, UIStyle.TextPrimary);
                score.fontStyle = FontStyle.Bold;
                score.text = "0m";
                score.rectTransform.anchorMin = new Vector2(0.73f, 0f);
                score.rectTransform.anchorMax = new Vector2(0.97f, 1f);
                score.rectTransform.offsetMin = score.rectTransform.offsetMax = Vector2.zero;
                leaderboardScoreTexts[i] = score;
            }

            var closeBtn = UIManager.CreateButton("CloseBtn", go.transform, font, "Close",
                UIStyle.SurfaceDark, UIStyle.TextPrimary);
            UIManager.Stretch((RectTransform)closeBtn.transform,
                new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.11f), Vector2.zero, Vector2.zero);
            UIManager.StyleButtonLabel(closeBtn, 11, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            UIManager.BindButton(closeBtn, () => StartCoroutine(CloseSheet(leaderboardSheet)), null);

            go.SetActive(false);
        }

private void BuildMissionsSheet(Font font)
        {
            GameObject overlay = new GameObject("MissionsOverlay");
            overlay.transform.SetParent(transform, false);
            RectTransform overlayRt = overlay.AddComponent<RectTransform>();
            UIManager.Stretch(overlayRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
            missionsSheet = overlayRt;
            overlay.AddComponent<CanvasGroup>().alpha = 0f;

            GameObject card = new GameObject("MissionsCard");
            card.transform.SetParent(overlay.transform, false);
            RectTransform cardRt = card.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.12f, 0.15f);
            cardRt.anchorMax = new Vector2(0.88f, 0.85f);
            cardRt.offsetMin = cardRt.offsetMax = Vector2.zero;
            card.AddComponent<Image>().color = UIStyle.ShopBg;

            Text title = UIManager.CreateText("Title", card.transform, font, 18, TextAnchor.MiddleLeft, UIStyle.TextPrimary);
            title.text = "DAILY MISSIONS";
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.anchorMin = new Vector2(0.06f, 0.90f);
            title.rectTransform.anchorMax = new Vector2(0.60f, 0.98f);
            title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;

            missionCountdownText = UIManager.CreateText("Countdown", card.transform, font, 11, TextAnchor.MiddleRight, UIStyle.TextDim);
            missionCountdownText.text = "23:59:59";
            missionCountdownText.rectTransform.anchorMin = new Vector2(0.60f, 0.90f);
            missionCountdownText.rectTransform.anchorMax = new Vector2(0.94f, 0.98f);
            missionCountdownText.rectTransform.offsetMin = missionCountdownText.rectTransform.offsetMax = Vector2.zero;

            GameObject closeGo = CreateIconCircle("CloseBtn", card.transform, font, "X",
                () => StartCoroutine(CloseSheet(missionsSheet)), buttonClickSound);
            RectTransform closeRt = closeGo.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.88f, 0.92f);
            closeRt.anchorMax = new Vector2(0.98f, 0.98f);
            closeRt.offsetMin = closeRt.offsetMax = Vector2.zero;

            GameObject listGo = new GameObject("List");
            listGo.transform.SetParent(card.transform, false);
            RectTransform listRt = listGo.AddComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0.04f, 0.05f);
            listRt.anchorMax = new Vector2(0.96f, 0.88f);
            listRt.offsetMin = listRt.offsetMax = Vector2.zero;

            const int missionCount = 3;
            missionClaimBtns = new GameObject[missionCount];
            missionIds = new string[missionCount];
            missionTitleTexts = new Text[missionCount];
            missionProgressTexts = new Text[missionCount];
            missionProgressFills = new Image[missionCount];
            missionRewardTexts = new Text[missionCount];
            missionStatusTexts = new Text[missionCount];

            for (int i = 0; i < missionCount; i++)
            {
                int idx = i;
                float yTop = 1f - i * 0.33f;
                Image missionCard = UIManager.CreateCard($"Mission{i}", listRt, UIStyle.SurfaceDark, UIStyle.BorderDark);
                missionCard.rectTransform.anchorMin = new Vector2(0f, yTop - 0.31f);
                missionCard.rectTransform.anchorMax = new Vector2(1f, yTop);
                missionCard.rectTransform.offsetMin = missionCard.rectTransform.offsetMax = Vector2.zero;

                Text mTitle = UIManager.CreateText("Title", missionCard.transform, font, 11, TextAnchor.UpperLeft, UIStyle.TextPrimary);
                mTitle.text = "Mission";
                mTitle.fontStyle = FontStyle.Bold;
                mTitle.rectTransform.anchorMin = new Vector2(0.04f, 0.58f);
                mTitle.rectTransform.anchorMax = new Vector2(0.75f, 0.93f);
                mTitle.rectTransform.offsetMin = mTitle.rectTransform.offsetMax = Vector2.zero;
                missionTitleTexts[i] = mTitle;

                Text progress = UIManager.CreateText("Progress", missionCard.transform, font, 9, TextAnchor.UpperRight, UIStyle.TextDim);
                progress.text = "0/0";
                progress.rectTransform.anchorMin = new Vector2(0.76f, 0.60f);
                progress.rectTransform.anchorMax = new Vector2(0.96f, 0.93f);
                progress.rectTransform.offsetMin = progress.rectTransform.offsetMax = Vector2.zero;
                missionProgressTexts[i] = progress;

                Image barTrack = UIManager.CreateImage("BarTrack", missionCard.transform, new Color(1f, 1f, 1f, 0.08f));
                barTrack.rectTransform.anchorMin = new Vector2(0.04f, 0.34f);
                barTrack.rectTransform.anchorMax = new Vector2(0.96f, 0.42f);
                barTrack.rectTransform.offsetMin = barTrack.rectTransform.offsetMax = Vector2.zero;
                barTrack.raycastTarget = false;

                Image barFill = UIManager.CreateImage("BarFill", barTrack.transform, UIStyle.Brand);
                barFill.rectTransform.anchorMin = new Vector2(0f, 0f);
                barFill.rectTransform.anchorMax = new Vector2(0f, 1f);
                barFill.rectTransform.offsetMin = barFill.rectTransform.offsetMax = Vector2.zero;
                barFill.raycastTarget = false;
                missionProgressFills[i] = barFill;

                Image rewardBg = UIManager.CreateImage("RewardBg", missionCard.transform, new Color(UIStyle.Gold.r, UIStyle.Gold.g, UIStyle.Gold.b, 0.15f));
                rewardBg.rectTransform.anchorMin = new Vector2(0.04f, 0.06f);
                rewardBg.rectTransform.anchorMax = new Vector2(0.42f, 0.31f);
                rewardBg.rectTransform.offsetMin = rewardBg.rectTransform.offsetMax = Vector2.zero;
                rewardBg.raycastTarget = false;

                Text rewardText = UIManager.CreateText("Reward", rewardBg.transform, font, 9, TextAnchor.MiddleCenter, UIStyle.Gold);
                rewardText.text = "+0 COIN";
                UIManager.Stretch(rewardText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                missionRewardTexts[i] = rewardText;

                Text statusText = UIManager.CreateText("Status", missionCard.transform, font, 9, TextAnchor.MiddleCenter, UIStyle.TextPrimary);
                statusText.text = "IN PROGRESS";
                statusText.rectTransform.anchorMin = new Vector2(0.45f, 0.06f);
                statusText.rectTransform.anchorMax = new Vector2(0.96f, 0.31f);
                statusText.rectTransform.offsetMin = statusText.rectTransform.offsetMax = Vector2.zero;
                missionStatusTexts[i] = statusText;

                GameObject claimBtnGo = UIManager.CreateActionButton(missionCard.transform, "ClaimBtn", "CHECK CLAIM", font, UIStyle.Owned, UIStyle.Owned, 36, 0);
                RectTransform claimBtnRt = claimBtnGo.GetComponent<RectTransform>();
                claimBtnRt.anchorMin = new Vector2(0.45f, 0.06f);
                claimBtnRt.anchorMax = new Vector2(0.96f, 0.31f);
                claimBtnRt.offsetMin = claimBtnRt.offsetMax = Vector2.zero;
                UIManager.BindButton(claimBtnGo.GetComponent<Button>(), () => _onClaimMission?.Invoke(missionIds[idx]));
                claimBtnGo.SetActive(false);
                missionClaimBtns[i] = claimBtnGo;
            }

            overlay.SetActive(false);
        }

        public void UpdateMissionCountdown(TimeSpan remaining)
        {
            if (missionCountdownText == null) return;
            missionCountdownText.text = $"{(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
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
