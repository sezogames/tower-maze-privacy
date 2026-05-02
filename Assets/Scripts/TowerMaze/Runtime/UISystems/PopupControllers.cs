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
            UIManager.SetScaledFontSize(countdownText, isGo ? 150 : 180);
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
            UIManager.Stretch(containerRect, new Vector2(0.5f, 0.80f), new Vector2(0.5f, 0.80f), new Vector2(-234f, -69f), new Vector2(234f, 69f));

            // Dark card background (UIStyle.SurfaceDark — semi-transparent white on dark bg)
            backgroundImage = container.AddComponent<Image>();
            backgroundImage.color = new Color(UIStyle.SurfaceDark.r, UIStyle.SurfaceDark.g, UIStyle.SurfaceDark.b, 0f);

            // Title: bright white
            titleText = UIManager.CreateText("RewardToastTitle", container.transform, font, 36, TextAnchor.UpperCenter, UIStyle.TextPrimary);
            titleText.fontStyle = FontStyle.Bold;
            UIManager.Stretch(titleText.rectTransform, new Vector2(0f, 0.50f), new Vector2(1f, 1f), new Vector2(22f, -14f), new Vector2(-22f, -6f));

            // Subtitle: gold (coin-colored)
            subtitleText = UIManager.CreateText("RewardToastSubtitle", container.transform, font, 30, TextAnchor.LowerCenter, UIStyle.Gold);
            UIManager.Stretch(subtitleText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.52f), new Vector2(22f, 10f), new Vector2(-22f, -12f));

            UIManager.ApplyPopupTextRoles(transform);
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
            background.a = 0f;
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
            titleText.color = UIStyle.TextPrimary;
            subtitleText.color = UIStyle.Gold;
            backgroundImage.color = new Color(UIStyle.SurfaceDark.r, UIStyle.SurfaceDark.g, UIStyle.SurfaceDark.b, 0f);
            transform.GetChild(0).localScale = Vector3.one * 0.96f;
            displayRemaining = DisplayDuration;
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
        private MonoBehaviour _runner;
        private Text titleText;
        private Text resumeLabel;
        private Text menuLabel;

        public void Initialize(Font font, ThemeDefinition theme, Action onResume, Action onReturnToMenu, Action onButtonClick = null)
        {
            buttonClickSound = onButtonClick;
            _runner = this;

            // Full-screen dark overlay (UIStyle.HudBg = #0F0A1E, semi-transparent)
            Image dim = UIManager.CreateImage("PauseDim", transform, new Color(0.059f, 0.039f, 0.118f, 0.88f));
            UIManager.Stretch(dim.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            dim.raycastTarget = true;

            // Center panel — dark shop background
            Image panel = UIManager.CreateCard("PausePanel", transform, UIStyle.ShopBg, UIStyle.BorderDark);
            UIManager.Stretch(panel.rectTransform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-200f, -180f), new Vector2(200f, 180f));

            // Title
            titleText = UIManager.CreateText("PauseTitle", panel.transform, font, 36, TextAnchor.MiddleCenter, UIStyle.TextPrimary);
            titleText.fontStyle = FontStyle.Bold;
            titleText.resizeTextForBestFit = true;
            titleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            titleText.verticalOverflow = VerticalWrapMode.Truncate;
            UIManager.SetScaledBestFit(titleText, 22, 36, UIFontRole.Popup);
            UIManager.Stretch(titleText.rectTransform,
                new Vector2(0f, 0.72f), new Vector2(1f, 1f),
                new Vector2(24f, 0f), new Vector2(-24f, 0f));

            // Resume button — jelly orange (dominant action)
            GameObject resumeGo = new("ResumeBtn");
            resumeGo.transform.SetParent(panel.transform, false);
            Image resumeImg = resumeGo.AddComponent<Image>();
            UICandySkin.ApplyCandyButton(resumeImg, "out_btn_orange", new Vector4(150f, 130f, 150f, 130f), 350f);
            Button resumeBtn = resumeGo.AddComponent<Button>();
            resumeBtn.targetGraphic = resumeImg;
            RectTransform resumeRt = resumeGo.GetComponent<RectTransform>();
            resumeRt.anchorMin = new Vector2(0.1f, 0.42f);
            resumeRt.anchorMax = new Vector2(0.9f, 0.68f);
            resumeRt.offsetMin = Vector2.zero;
            resumeRt.offsetMax = Vector2.zero;
            Text resumeLbl = UIManager.CreateText(resumeGo.transform, "Label", "▶ DEVAM ET",
                20, FontStyle.Bold, Color.white, font, TextAnchor.MiddleCenter);
            resumeLbl.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(resumeLbl, 16, 20, UIFontRole.Button);
            UIManager.Stretch(resumeLbl.rectTransform);
            Shadow resumeLblShadow = resumeLbl.gameObject.AddComponent<Shadow>();
            resumeLblShadow.effectColor = new Color(0.48f, 0.22f, 0f, 0.55f);
            resumeLblShadow.effectDistance = new Vector2(0f, -2f);
            resumeLabel = resumeLbl;
            UIManager.BindButton(resumeBtn, () =>
            {
                _runner.StartCoroutine(UIStyle.ButtonPress(resumeRt));
                buttonClickSound?.Invoke();
                onResume?.Invoke();
            });

            // Main menu button — jelly purple (secondary)
            GameObject menuGo = new("MainMenuBtn");
            menuGo.transform.SetParent(panel.transform, false);
            Image menuImg = menuGo.AddComponent<Image>();
            UICandySkin.ApplyCandyButton(menuImg, "out_btn_purple", new Vector4(150f, 160f, 150f, 160f), 350f);
            Button menuBtn = menuGo.AddComponent<Button>();
            menuBtn.targetGraphic = menuImg;
            RectTransform menuRt = (RectTransform)menuGo.transform;
            menuRt.anchorMin = new Vector2(0.1f, 0.1f);
            menuRt.anchorMax = new Vector2(0.9f, 0.36f);
            menuRt.offsetMin = Vector2.zero;
            menuRt.offsetMax = Vector2.zero;
            Text menuLbl = UIManager.CreateText(menuGo.transform, "Label", "ANA MENÜ",
                18, FontStyle.Bold, Color.white, font, TextAnchor.MiddleCenter);
            menuLbl.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(menuLbl, 14, 20, UIFontRole.Button);
            UIManager.Stretch(menuLbl.rectTransform);
            Shadow menuLblShadow = menuLbl.gameObject.AddComponent<Shadow>();
            menuLblShadow.effectColor = new Color(0.16f, 0.08f, 0.25f, 0.55f);
            menuLblShadow.effectDistance = new Vector2(0f, -2f);
            menuLabel = menuLbl;
            UIManager.BindButton(menuBtn, () =>
            {
                _runner.StartCoroutine(UIStyle.ButtonPress(menuRt));
                buttonClickSound?.Invoke();
                onReturnToMenu?.Invoke();
            });

            ApplyLocalizedTexts();
            UIManager.ApplyPopupTextRoles(transform);
        }

        private void OnEnable()
        {
            ApplyLocalizedTexts();
        }

        private void ApplyLocalizedTexts()
        {
            if (titleText != null)
            {
                titleText.text = UILanguage.Translate("DURAKLATILDI", "PAUSED", "PAUSADO");
            }

            if (resumeLabel != null)
            {
                resumeLabel.text = UILanguage.Translate("DEVAM ET", "RESUME", "REANUDAR");
            }

            if (menuLabel != null)
            {
                menuLabel.text = UILanguage.Translate("ANA MENU", "MAIN MENU", "MENU PRINCIPAL");
            }
        }
    }

    public sealed class ControlFlipController : MonoBehaviour
    {
        private RectTransform badgeRoot;
        private Image badgeBg;
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

            GameObject badgeObj = new("ControlFlipBadge");
            badgeRoot = badgeObj.AddComponent<RectTransform>();
            badgeRoot.SetParent(transform, false);
            badgeRoot.anchorMin = new Vector2(0.5f, 0.64f);
            badgeRoot.anchorMax = new Vector2(0.5f, 0.64f);
            badgeRoot.pivot = new Vector2(0.5f, 0.5f);
            badgeRoot.anchoredPosition = Vector2.zero;
            badgeRoot.sizeDelta = new Vector2(468f, 138f);

            badgeBg = UIManager.CreateImage("ControlFlipBg", badgeRoot, new Color(0.16f, 0.11f, 0.21f, 0.94f));
            badgeBg.sprite = Resources.Load<Sprite>("TowerMaze/UITheme/panel_dark_hq");
            badgeBg.type = Image.Type.Sliced;
            UIManager.Stretch(badgeBg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            badgeBg.raycastTarget = false;

            timerTrack = UIManager.CreateImage("ControlFlipTrack", badgeRoot, new Color(1f, 1f, 1f, 0.14f));
            UIManager.Stretch(timerTrack.rectTransform, new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.29f), Vector2.zero, Vector2.zero);
            timerTrack.raycastTarget = false;

            timerFill = UIManager.CreateImage("ControlFlipFill", timerTrack.transform, new Color(1f, 0.76f, 0.28f, 1f));
            timerFill.type = Image.Type.Filled;
            timerFill.fillMethod = Image.FillMethod.Horizontal;
            timerFill.fillOrigin = 0;
            timerFill.fillAmount = 1f;
            UIManager.Stretch(timerFill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            timerFill.raycastTarget = false;

            warningText = UIManager.CreateText("ControlFlipText", badgeRoot, font, 34, TextAnchor.MiddleCenter, Color.white);
            warningText.fontStyle = FontStyle.Bold;
            warningText.text = "CONTROL FLIP";
            warningText.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(warningText, 26, 34, UIFontRole.Popup);
            UIManager.Stretch(warningText.rectTransform, new Vector2(0.06f, 0.36f), new Vector2(0.94f, 0.86f), Vector2.zero, Vector2.zero);
        }

        public void SetState(bool controlsFlipped, float pulse, float remainingNormalized, float totalDurationSeconds)
        {
            float clampedPulse = Mathf.Clamp01(pulse);
            Color overlayColor = controlsFlipped ? activeColor : warningColor;
            overlayColor.a = controlsFlipped ? Mathf.Lerp(0.08f, 0.18f, clampedPulse) : Mathf.Lerp(0.05f, 0.13f, clampedPulse);
            pulseImage.color = overlayColor;
            badgeBg.color = controlsFlipped
                ? Color.Lerp(new Color(0.28f, 0.10f, 0.12f, 0.95f), new Color(0.40f, 0.12f, 0.12f, 0.98f), clampedPulse)
                : Color.Lerp(new Color(0.14f, 0.11f, 0.24f, 0.93f), new Color(0.20f, 0.15f, 0.31f, 0.96f), clampedPulse);
            badgeRoot.localScale = Vector3.one * Mathf.Lerp(0.98f, 1.05f, clampedPulse);
            timerFill.color = controlsFlipped ? new Color(1f, 0.28f, 0.28f, 1f) : new Color(1f, 0.72f, 0.28f, 1f);
            timerTrack.color = controlsFlipped
                ? new Color(1f, 1f, 1f, 0.20f)
                : new Color(1f, 1f, 1f, 0.14f);
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
        private Image accentBar;
        private Image timerFill;
        private Text hurryText;

        private static readonly Color PanelCore = new(0.08f, 0.06f, 0.12f, 0.88f);
        private static readonly Color RushAccent = new(1f, 0.32f, 0.26f, 1f);
        private static readonly Color WarningAccent = new(1f, 0.78f, 0.26f, 1f);

        public void Initialize(Font font, ThemeDefinition theme)
        {
            GameObject rootObj = new GameObject("RushBadge");
            badgeRoot = rootObj.AddComponent<RectTransform>();
            badgeRoot.SetParent(transform, false);
            badgeRoot.anchorMin = new Vector2(0.5f, 0.64f);
            badgeRoot.anchorMax = new Vector2(0.5f, 0.64f);
            badgeRoot.pivot = new Vector2(0.5f, 0.5f);
            badgeRoot.anchoredPosition = Vector2.zero;
            badgeRoot.sizeDelta = new Vector2(440f, 72f);

            // Single dark glass pill
            badgeBg = UIManager.CreateImage("BadgeBg", badgeRoot, PanelCore);
            badgeBg.sprite = Resources.Load<Sprite>("TowerMaze/UITheme/panel_dark_hq");
            badgeBg.type = Image.Type.Sliced;
            UIManager.Stretch(badgeBg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            badgeBg.raycastTarget = false;

            Shadow badgeShadow = badgeBg.gameObject.AddComponent<Shadow>();
            badgeShadow.effectColor = new Color(0f, 0f, 0f, 0.42f);
            badgeShadow.effectDistance = new Vector2(0f, -3f);

            // Main text — centered
            hurryText = UIManager.CreateText("HurryText", badgeRoot, font, 28, TextAnchor.MiddleCenter, Color.white);
            hurryText.fontStyle = FontStyle.Bold;
            hurryText.text = UILanguage.Translate("HIZLAN", "HURRY UP", "ACELERA");
            hurryText.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(hurryText, 18, 30, UIFontRole.Popup);
            hurryText.rectTransform.anchorMin = new Vector2(0.04f, 0.20f);
            hurryText.rectTransform.anchorMax = new Vector2(0.96f, 1f);
            hurryText.rectTransform.offsetMin = Vector2.zero;
            hurryText.rectTransform.offsetMax = Vector2.zero;

            // Bottom accent bar (doubles as timer) — thin, modern progress line
            accentBar = UIManager.CreateImage("AccentBar", badgeRoot, new Color(1f, 1f, 1f, 0.08f));
            accentBar.rectTransform.anchorMin = new Vector2(0.12f, 0.08f);
            accentBar.rectTransform.anchorMax = new Vector2(0.88f, 0.16f);
            accentBar.rectTransform.offsetMin = Vector2.zero;
            accentBar.rectTransform.offsetMax = Vector2.zero;
            accentBar.raycastTarget = false;

            timerFill = UIManager.CreateImage("TimerFill", accentBar.transform, WarningAccent);
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
            Color accent = rushActive ? RushAccent : WarningAccent;

            // Panel color: core + very subtle accent-tinted breathing when rush active
            Color corePulse = Color.Lerp(PanelCore, new Color(accent.r, accent.g, accent.b, PanelCore.a), rushActive ? (p * 0.14f) : 0f);
            badgeBg.color = corePulse;

            // Small scale breathing — no thick glow, no halo
            badgeRoot.localScale = Vector3.one * Mathf.Lerp(0.99f, rushActive ? 1.035f : 1.018f, p);

            // Timer fill
            timerFill.color = accent;
            timerFill.fillAmount = Mathf.Clamp01(remainingNormalized);

            hurryText.text = rushActive
                ? UILanguage.Translate("HIZLAN", "HURRY UP", "ACELERA")
                : UILanguage.Translate("HAZIR OL", "GET READY", "PREPARATE");
            hurryText.color = Color.Lerp(Color.white, accent, rushActive ? (p * 0.18f) : 0f);
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
        private Text closeButtonLabel;
        private Text titleText;
        private Text descText;
        private Text priceText;
        private Button buyButton;
        private Text buyButtonLabel;
        private string currentOfferId;
        private MonoBehaviour _runner;

        public void Initialize(Font font, ThemeDefinition themeDefinition, Action<string> purchaseCallback, Action onButtonClick = null)
        {
            buttonClickSound = onButtonClick;
            runtimeFont = font;
            onPurchase = purchaseCallback;
            _runner = this;
            BuildUI();
        }

        private void BuildUI()
        {
            RectTransform rt = (RectTransform)transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Dark semi-transparent backdrop
            Image backdrop = gameObject.AddComponent<Image>();
            backdrop.color = new Color(0.059f, 0.039f, 0.118f, 0.88f);
            backdrop.raycastTarget = true;

            // Card — dark shop surface
            GameObject cardObj = new("UpsellCard");
            cardObj.transform.SetParent(transform, false);
            card = cardObj.AddComponent<Image>();
            UIManager.ApplyCardSurface(card, UIStyle.SurfaceDark);
            RectTransform cardRt = (RectTransform)cardObj.transform;
            cardRt.anchorMin = new Vector2(0.08f, 0.22f);
            cardRt.anchorMax = new Vector2(0.92f, 0.78f);
            cardRt.offsetMin = Vector2.zero;
            cardRt.offsetMax = Vector2.zero;

            // X (close) button — top-right corner of card
            Button closeBtn = UIManager.CreateCandyCloseButton("UpsellClose", cardObj.transform, runtimeFont, 16);
            closeButtonLabel = closeBtn.GetComponentInChildren<Text>();
            RectTransform closeBtnRt = (RectTransform)closeBtn.transform;
            closeBtnRt.anchorMin = new Vector2(1f, 1f);
            closeBtnRt.anchorMax = new Vector2(1f, 1f);
            closeBtnRt.pivot = new Vector2(1f, 1f);
            closeBtnRt.anchoredPosition = new Vector2(-22f, -18f);
            closeBtnRt.sizeDelta = new Vector2(54f, 54f);
            UIManager.BindButton(closeBtn, () =>
            {
                _runner.StartCoroutine(UIStyle.ButtonPress(closeBtnRt));
                buttonClickSound?.Invoke();
                OnCloseClicked();
            });

            // Image frame — expanded to fill top of card as a square
            previewFrame = UIManager.CreateCard("UpsellPreviewFrame", cardObj.transform, UIStyle.SurfaceDark2, UIStyle.BorderDark);
            previewFrame.raycastTarget = false;
            UIManager.Stretch(previewFrame.rectTransform, new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero);

            // Product image inside frame — AspectRatioFitter prevents distortion
            GameObject previewObj = new("UpsellPreview");
            previewObj.transform.SetParent(previewFrame.transform, false);
            previewImage = previewObj.AddComponent<RawImage>();
            previewImage.raycastTarget = false;
            AspectRatioFitter arf = previewObj.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            arf.aspectRatio = 1f;
            RectTransform previewRt = (RectTransform)previewObj.transform;
            previewRt.anchorMin = Vector2.zero;
            previewRt.anchorMax = Vector2.one;
            previewRt.offsetMin = Vector2.zero;
            previewRt.offsetMax = Vector2.zero;

            // Title, Description, and Price text blocks removed based on user request ('giri alanı istemiyorum')

            // Buy button — orange gradient with ButtonPress animation
            GameObject buyGo = new("UpsellBuy");
            buyGo.transform.SetParent(cardObj.transform, false);
            GradientImage buyGrad = buyGo.AddComponent<GradientImage>();
            buyGrad.colorTop = UIStyle.ActionLight;
            buyGrad.colorBottom = UIStyle.Action;
            buyButton = buyGo.AddComponent<Button>();
            buyButton.targetGraphic = buyGrad;
            RectTransform buyRt = buyGo.GetComponent<RectTransform>();
            buyRt.anchorMin = new Vector2(0.08f, 0.05f);
            buyRt.anchorMax = new Vector2(0.92f, 0.18f);
            buyRt.offsetMin = Vector2.zero;
            buyRt.offsetMax = Vector2.zero;
            buyButtonLabel = UIManager.CreateText(buyGo.transform, "Label", GetBuyButtonLabel(),
                22, FontStyle.Bold, Color.white, runtimeFont, TextAnchor.MiddleCenter);
            buyButtonLabel.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(buyButtonLabel, 18, 22, UIFontRole.Button);
            UIManager.Stretch(buyButtonLabel.rectTransform);
            UIManager.BindButton(buyButton, () =>
            {
                _runner.StartCoroutine(UIStyle.ButtonPress(buyRt));
                buttonClickSound?.Invoke();
                OnBuyClicked();
            });

            closeBtn.transform.SetAsLastSibling();
            UIManager.ApplyPopupTextRoles(transform);
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
            // Removed text updates as elements are no longer present
            if (buyButtonLabel != null)
            {
                buyButtonLabel.text = $"{offer.displayName} - {offer.priceLabel}";
            }

            // Apply offer-specific colors
            Color offerCardColor = ShopScreenController.GetCoinOfferCardColor(offer);
            UIManager.ApplyCardSurface(card, offerCardColor);
            UIManager.ApplyCardSurface(previewFrame, ShopScreenController.GetCoinOfferPreviewFrameColor(offer));
            ApplyReadableTextTheme(offerCardColor);

            // Load product image
            Texture2D tex = ShopScreenController.GetCoinPackPreviewTexture(offer);
            previewImage.texture = tex != null ? tex : Texture2D.whiteTexture;
            previewImage.color = tex != null ? ShopScreenController.GetCoinOfferPreviewTint(offer) : new Color(0.18f, 0.22f, 0.32f, 1f);

            gameObject.SetActive(true);
        }

        private void ApplyReadableTextTheme(Color offerCardColor)
        {
            if (closeButtonLabel != null)
            {
                closeButtonLabel.color = Color.white;
            }
        }

        private static float GetRelativeLuminance(Color color)
        {
            return (0.2126f * color.r) + (0.7152f * color.g) + (0.0722f * color.b);
        }

        private static string FormatOfferDescription(CoinPackOffer offer)
        {
            string text = !string.IsNullOrWhiteSpace(offer.bonusLabel) ? offer.bonusLabel : offer.badgeLabel;
            if (string.IsNullOrWhiteSpace(text))
            {
                return UILanguage.Translate("Ozel teklif", "Special offer", "Oferta especial");
            }

            return text.Replace("  |  ", "\n").Replace(" | ", "\n");
        }

        private static string GetBuyButtonLabel()
        {
            return UILanguage.Translate("SATIN AL", "BUY NOW", "COMPRAR");
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

            GameObject trackObj = new("LoadingTrack");
            trackObj.transform.SetParent(transform, false);
            RectTransform trackRect = trackObj.AddComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0.16f, 0f);
            trackRect.anchorMax = new Vector2(0.84f, 0f);
            trackRect.pivot = new Vector2(0.5f, 0.5f);
            trackRect.anchoredPosition = new Vector2(0f, 64f);
            trackRect.sizeDelta = new Vector2(0f, 16f);
            Image trackImage = trackObj.AddComponent<Image>();
            trackImage.color = new Color(1f, 1f, 1f, 0.16f);
            trackImage.raycastTarget = false;

            GameObject fillObj = new("LoadingFill");
            fillObj.transform.SetParent(trackObj.transform, false);
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillRect.pivot = new Vector2(0f, 0.5f);
            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = UIStyle.Action;
            fillImage.raycastTarget = false;

            StartCoroutine(SplashRoutine(rootGroup, spinnerObj, fillRect, onComplete));
        }

        private System.Collections.IEnumerator SplashRoutine(
            CanvasGroup rootGroup,
            GameObject spinnerObj,
            RectTransform progressFill,
            Action onComplete)
        {
            const float fixedDurationSeconds = 10f;
            float elapsed = 0f;
            float spinnerAngle = 0f;
            while (elapsed < fixedDurationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / fixedDurationSeconds);
                if (progressFill != null)
                {
                    progressFill.anchorMax = new Vector2(progress, 1f);
                }

                spinnerAngle -= 360f * Time.unscaledDeltaTime;
                if (spinnerObj != null) spinnerObj.transform.localRotation = Quaternion.Euler(0f, 0f, spinnerAngle);
                yield return null;
                if (this == null) yield break;
            }

            if (progressFill != null)
            {
                progressFill.anchorMax = Vector2.one;
            }

            isVisible = false;
            onComplete?.Invoke();
            Destroy(gameObject);
        }
    }


    public sealed class ReviewPopupController : MonoBehaviour
    {
        private Action onRate;
        private Action onLater;
        private Action onNever;

        public void Initialize(Font font, ThemeDefinition theme, Action onRate, Action onLater, Action onNever)
        {
            this.onRate = onRate;
            this.onLater = onLater;
            this.onNever = onNever;

            foreach (Transform child in transform) { Destroy(child.gameObject); }

            // Root Overlay
            var overlay = UIManager.CreateImage(transform, "Overlay", new Color(0, 0, 0, 0.85f));
            UIManager.Stretch(overlay.rectTransform);

            // Card
            var card = new GameObject("Card");
            card.transform.SetParent(transform, false);
            var cardImg = card.AddComponent<Image>();
            UICandySkin.ApplyCandyPanel(cardImg);
            cardImg.color = new Color(0.92f, 0.88f, 1f, 0.98f); // Soft lilac tint for the candy panel
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.sizeDelta = new Vector2(320, 380);

            var cardLayout = card.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(24, 24, 32, 24);
            cardLayout.spacing = 16;
            cardLayout.childAlignment = TextAnchor.UpperCenter;
            cardLayout.childControlHeight = false;
            cardLayout.childForceExpandHeight = false;

            // Title
            var title = UILanguage.Translate("TOWER MAZE'I SEVDİN Mİ?", "DO YOU LOVE TOWER MAZE?", "¿TE GUSTA TOWER MAZE?");
            var titleTxt = UIManager.CreateText(card.transform, "Title", title.ToUpper(), 18, FontStyle.Bold, Color.white, font);
            titleTxt.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.5f);

            // Stars placeholder (Visual only)
            var starsContainer = new GameObject("Stars");
            starsContainer.transform.SetParent(card.transform, false);
            var starsLayout = starsContainer.AddComponent<HorizontalLayoutGroup>();
            starsLayout.spacing = 6;
            starsLayout.childAlignment = TextAnchor.MiddleCenter;
            starsLayout.childControlWidth = true;
            starsLayout.childForceExpandWidth = false;
            for (int i = 0; i < 5; i++)
            {
                var star = UIManager.CreateText(starsContainer.transform, $"Star_{i}", "★", 34, FontStyle.Normal, new Color(1f, 0.84f, 0f), font);
                star.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.3f);
            }

            // Description
            var desc = UILanguage.Translate(
                "FİKİRLERİN BİZİM İÇİN ÇOK DEĞERLİ!\nDESTEK OLMAK İÇİN OYLAR MISIN?",
                "YOUR FEEDBACK IS VERY VALUABLE!\nWOULD YOU SUPPORT US WITH A RATING?",
                "¡TU OPINIÓN ES MUY VALIOSA!\n¿NOS APOYARÍAS CON UNA CALIFICACIÓN?");
            var descTxt = UIManager.CreateText(card.transform, "Desc", desc, 13, FontStyle.Normal, new Color(1, 1, 1, 0.7f), font);
            descTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 60;
            descTxt.alignment = TextAnchor.UpperCenter;
            descTxt.lineSpacing = 1.2f;

            // Actions Container
            var actions = new GameObject("Actions");
            actions.transform.SetParent(card.transform, false);
            var actionsLayout = actions.AddComponent<VerticalLayoutGroup>();
            actionsLayout.spacing = 8;
            actionsLayout.childControlWidth = true;
            actionsLayout.childForceExpandWidth = true;

            // Rate Button
            var rateLabel = UILanguage.Translate("HEMEN PUAN VER", "RATE NOW", "CALIFICAR AHORA");
            GameObject rateBtn = UIManager.CreateActionButton(actions.transform, "RateBtn",
                UILanguage.Translate("PUAN VER", "RATE NOW", "CALIFICAR"),
                font, UIStyle.Action, UIStyle.ActionLight, 44, 20);
            UIManager.BindButton(rateBtn.GetComponent<Button>(), onRate);

            // Row for Later and Never
            var row = new GameObject("Row");
            row.transform.SetParent(actions.transform, false);
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandWidth = true;

            var laterLabel = UILanguage.Translate("DAHA SONRA", "LATER", "MÁS TARDE");
            var laterBtnGo = UIManager.CreateSecondaryButton(row.transform, "LaterBtn", laterLabel, font);
            UIManager.BindButton(laterBtnGo.GetComponent<Button>(), onLater);

            var neverLabel = UILanguage.Translate("BİR DAHA SORMA", "NEVER", "NUNCA");
            var neverBtnGo = UIManager.CreateSecondaryButton(row.transform, "NeverBtn", neverLabel, font);
            UIManager.BindButton(neverBtnGo.GetComponent<Button>(), onNever);
        }
    }

    public sealed class NicknamePopupController : MonoBehaviour
    {
        private static readonly Vector4 SheetPanelSlice = new(220f, 220f, 220f, 220f);
        private static readonly Vector4 SheetRowSlice = new(160f, 160f, 160f, 160f);
        private static readonly Vector4 OrangeButtonSlice = new(150f, 130f, 150f, 130f);
        private static readonly Vector4 PurpleButtonSlice = new(150f, 160f, 150f, 160f);

        private Action<string, Action<bool, string>> onConfirm;
        private InputField inputField;
        private Button confirmButton;
        private Image confirmButtonImage;
        private Text confirmButtonLabel;
        private Text helperText;
        private Text counterText;
        private bool isSubmitting;

        public void Initialize(Font font, ThemeDefinition theme, Action<string, Action<bool, string>> onConfirmCallback)
        {
            onConfirm = onConfirmCallback;

            foreach (Transform child in transform) { Destroy(child.gameObject); }

            var overlay = UIManager.CreateImage(transform, "Overlay", new Color(UIStyle.MenuBg.r, UIStyle.MenuBg.g, UIStyle.MenuBg.b, 0.80f));
            UIManager.Stretch(overlay.rectTransform);
            overlay.raycastTarget = true;

            var card = new GameObject("Card");
            card.transform.SetParent(transform, false);
            var cardRt = card.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.08f, 0.32f);
            cardRt.anchorMax = new Vector2(0.92f, 0.72f);
            cardRt.offsetMin = Vector2.zero;
            cardRt.offsetMax = Vector2.zero;
            var cardImg = card.AddComponent<Image>();
            SetupSheetPanel(cardImg);
            cardImg.color = new Color(1f, 1f, 1f, 0.98f);

            var cardLayout = card.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(20, 20, 18, 18);
            cardLayout.spacing = 10;
            cardLayout.childAlignment = TextAnchor.UpperCenter;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            var titleRibbon = new GameObject("TitleRibbon");
            titleRibbon.transform.SetParent(card.transform, false);
            titleRibbon.AddComponent<LayoutElement>().preferredHeight = 52f;
            var titleRibbonImg = titleRibbon.AddComponent<Image>();
            SetupPurpleRibbon(titleRibbonImg);

            Image titleIcon = UIManager.CreateImage("TitleIcon", titleRibbon.transform, Color.white);
            Sprite titleSprite = UICandySkin.GetSprite("out_icon_flag", 100f);
            if (titleSprite != null)
            {
                titleIcon.sprite = titleSprite;
                titleIcon.type = Image.Type.Simple;
                titleIcon.preserveAspect = true;
            }
            else
            {
                UICandySkin.ApplyCandyOrb(titleIcon);
            }
            UIManager.Stretch(titleIcon.rectTransform, new Vector2(0.05f, 0.18f), new Vector2(0.19f, 0.84f), Vector2.zero, Vector2.zero);
            titleIcon.raycastTarget = false;

            var title = UILanguage.Translate("LIDER ADINI SEC", "CHOOSE YOUR TAG", "ELIGE TU NOMBRE");
            var titleTxt = UIManager.CreateText("Title", titleRibbon.transform, font, 21, TextAnchor.MiddleLeft, Color.white, UIFontRole.Popup);
            titleTxt.text = title.ToUpperInvariant();
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(titleTxt, 16, 21, UIFontRole.Popup);
            UIManager.Stretch(titleTxt.rectTransform, new Vector2(0.20f, 0.08f), new Vector2(0.92f, 0.92f), new Vector2(6f, 0f), new Vector2(-12f, 0f));
            Shadow titleShadow = titleTxt.gameObject.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0.18f, 0.06f, 0.28f, 0.42f);
            titleShadow.effectDistance = new Vector2(0f, -3f);

            string subtitle = UILanguage.Translate(
                "Skor tabelesinde bu isim gorunecek.",
                "This name will appear on the global leaderboard.",
                "Este nombre aparecera en la clasificacion global.");
            var subtitleTxt = UIManager.CreateText("Subtitle", card.transform, font, 14, TextAnchor.MiddleCenter, UIStyle.PopupTextDim, UIFontRole.Popup);
            subtitleTxt.text = subtitle;
            subtitleTxt.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(subtitleTxt, 12, 14, UIFontRole.Popup);
            subtitleTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            subtitleTxt.alignment = TextAnchor.MiddleCenter;
            subtitleTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;

            var logoShell = new GameObject("LogoShell");
            logoShell.transform.SetParent(card.transform, false);
            logoShell.AddComponent<LayoutElement>().preferredHeight = 180f;

            var logoBacker = logoShell.AddComponent<Image>();
            SetupSheetRow(logoBacker, new Color(0.98f, 0.97f, 1f, 0.96f));
            Shadow logoBackerShadow = logoShell.AddComponent<Shadow>();
            logoBackerShadow.effectColor = new Color(0.12f, 0.04f, 0.22f, 0.12f);
            logoBackerShadow.effectDistance = new Vector2(0f, -4f);

            Image logoImage = UIManager.CreateImage("TowerMazeLogo", logoShell.transform, Color.white);
            Sprite logoSprite = UICandySkin.GetSprite("CandyTowerMazeLogo", 100f)
                ?? UICandySkin.GetSprite("OrangeTowerMazeLogo", 100f)
                ?? UICandySkin.GetSprite("TowerLogoOrange", 100f);
            if (logoSprite != null)
            {
                logoImage.sprite = logoSprite;
                logoImage.type = Image.Type.Simple;
                logoImage.preserveAspect = true;
                logoImage.color = Color.white;
            }
            UIManager.Stretch(logoImage.rectTransform, Vector2.zero, Vector2.one, new Vector2(-36f, -26f), new Vector2(36f, 26f));
            logoImage.raycastTarget = false;

            var inputGo = new GameObject("NicknameInput");
            inputGo.transform.SetParent(card.transform, false);
            inputGo.AddComponent<LayoutElement>().preferredHeight = 80f;
            var inputBg = inputGo.AddComponent<Image>();
            SetupSheetRow(inputBg, new Color(1f, 1f, 1f, 0.96f));
            Shadow inputShadow = inputGo.AddComponent<Shadow>();
            inputShadow.effectColor = new Color(0.08f, 0.02f, 0.16f, 0.15f);
            inputShadow.effectDistance = new Vector2(0f, -4f);

            var fieldLabel = UIManager.CreateText("FieldLabel", inputGo.transform, font, 12, TextAnchor.MiddleLeft, UIStyle.PopupTextDim, UIFontRole.Popup);
            fieldLabel.text = UILanguage.Translate("TAKMA AD", "NICKNAME", "APODO");
            fieldLabel.fontStyle = FontStyle.Bold;
            fieldLabel.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(fieldLabel, 10, 12, UIFontRole.Popup);
            UIManager.Stretch(fieldLabel.rectTransform, new Vector2(0.08f, 0.60f), new Vector2(0.56f, 0.88f), Vector2.zero, Vector2.zero);

            counterText = UIManager.CreateText("Counter", inputGo.transform, font, 11, TextAnchor.MiddleRight, UIStyle.PopupTextDim, UIFontRole.Popup);
            counterText.fontStyle = FontStyle.Bold;
            counterText.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(counterText, 9, 11, UIFontRole.Popup);
            UIManager.Stretch(counterText.rectTransform, new Vector2(0.62f, 0.60f), new Vector2(0.92f, 0.88f), Vector2.zero, Vector2.zero);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(inputGo.transform, false);
            var inputText = textGo.AddComponent<Text>();
            inputText.font = font;
            inputText.fontSize = 18;
            inputText.fontStyle = FontStyle.Bold;
            inputText.color = UIStyle.PopupText;
            inputText.alignment = TextAnchor.MiddleCenter;
            inputText.supportRichText = false;
            inputText.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(inputText, 15, 18, UIFontRole.Popup);
            UIManager.Stretch(inputText.rectTransform, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.62f), Vector2.zero, Vector2.zero);

            var placeholderGo = new GameObject("Placeholder");
            placeholderGo.transform.SetParent(inputGo.transform, false);
            var placeholder = placeholderGo.AddComponent<Text>();
            placeholder.font = font;
            placeholder.fontSize = 16;
            placeholder.fontStyle = FontStyle.Italic;
            placeholder.color = new Color(UIStyle.PopupTextDim.r, UIStyle.PopupTextDim.g, UIStyle.PopupTextDim.b, 0.70f);
            placeholder.alignment = TextAnchor.MiddleCenter;
            placeholder.text = UILanguage.Translate("SEZO", "SEZO", "SEZO");
            placeholder.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(placeholder, 14, 16, UIFontRole.Popup);
            UIManager.Stretch(placeholder.rectTransform, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.62f), Vector2.zero, Vector2.zero);

            inputField = inputGo.AddComponent<InputField>();
            inputField.textComponent = inputText;
            inputField.placeholder = placeholder;
            inputField.characterLimit = 12;
            inputField.contentType = InputField.ContentType.Standard;
            inputField.lineType = InputField.LineType.SingleLine;
            inputField.customCaretColor = true;
            inputField.caretColor = UIStyle.Action;
            inputField.selectionColor = new Color(UIStyle.Action.r, UIStyle.Action.g, UIStyle.Action.b, 0.22f);
            inputField.onValueChanged.AddListener(OnInputChanged);

            helperText = UIManager.CreateText("HelperText", card.transform, font, 12, TextAnchor.MiddleCenter, UIStyle.PopupTextDim, UIFontRole.Popup);
            helperText.resizeTextForBestFit = true;
            helperText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIManager.SetScaledBestFit(helperText, 10, 12, UIFontRole.Popup);
            helperText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

            var confirmSpacer = new GameObject("ConfirmSpacer");
            confirmSpacer.transform.SetParent(card.transform, false);
            confirmSpacer.AddComponent<LayoutElement>().preferredHeight = 12f;

            var confirmGo = new GameObject("ConfirmBtn");
            confirmGo.transform.SetParent(card.transform, false);
            confirmGo.AddComponent<LayoutElement>().preferredHeight = 54f;
            confirmButtonImage = confirmGo.AddComponent<Image>();
            SetupActionButton(confirmButtonImage);
            confirmButton = confirmGo.AddComponent<Button>();
            confirmButton.targetGraphic = confirmButtonImage;
            confirmButton.transition = Selectable.Transition.ColorTint;
            confirmButton.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock confirmColors = confirmButton.colors;
            confirmColors.normalColor = Color.white;
            confirmColors.highlightedColor = new Color(1f, 1f, 1f, 0.96f);
            confirmColors.pressedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            confirmColors.disabledColor = new Color(1f, 1f, 1f, 0.50f);
            confirmButton.colors = confirmColors;

            confirmButtonLabel = UIManager.CreateText("ConfirmLabel", confirmGo.transform, font, 20, TextAnchor.MiddleCenter, Color.white, UIFontRole.Button);
            confirmButtonLabel.text = UILanguage.Translate("KAYDET VE GIR", "SAVE AND JOIN", "GUARDAR Y ENTRAR");
            confirmButtonLabel.fontStyle = FontStyle.Bold;
            confirmButtonLabel.resizeTextForBestFit = true;
            UIManager.SetScaledBestFit(confirmButtonLabel, 15, 20, UIFontRole.Button);
            UIManager.Stretch(confirmButtonLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            confirmButton.interactable = false;
            UIManager.BindButton(confirmButton, OnConfirmClicked);
            UpdateInputState(string.Empty);
            UIManager.ApplyPopupTextRoles(transform);
            inputField.ActivateInputField();
        }

        private void OnInputChanged(string value)
        {
            string cleaned = System.Text.RegularExpressions.Regex.Replace(value.ToUpperInvariant(), @"[^A-Z0-9_]", "");
            if (cleaned != value)
            {
                inputField.text = cleaned;
                return;
            }

            UpdateInputState(cleaned);
        }

        private void OnConfirmClicked()
        {
            string value = inputField.text.Trim().ToUpperInvariant();
            if (isSubmitting || value.Length < 2 || value.Length > 12) return;

            isSubmitting = true;
            if (inputField != null)
            {
                inputField.interactable = false;
            }

            if (helperText != null)
            {
                helperText.text = UILanguage.Translate("ISIM KONTROL EDILIYOR...", "CHECKING NAME...", "COMPROBANDO NOMBRE...");
                helperText.color = UIStyle.PopupTextDim;
            }

            if (confirmButtonLabel != null)
            {
                confirmButtonLabel.text = UILanguage.Translate("KONTROL EDILIYOR", "CHECKING", "VERIFICANDO");
            }

            UpdateInputState(value);

            if (onConfirm != null)
            {
                onConfirm.Invoke(value, HandleConfirmResult);
                return;
            }

            HandleConfirmResult(true, string.Empty);
        }

        private void HandleConfirmResult(bool success, string message)
        {
            if (success)
            {
                Destroy(gameObject);
                return;
            }

            isSubmitting = false;
            if (inputField != null)
            {
                inputField.interactable = true;
                inputField.ActivateInputField();
            }

            // FirebaseCloudManager encodes a duplicate-with-suggestion as
            // "SUGGEST:USTAB42" in the error message — split it out here so we can
            // populate the input with a one-tap accept instead of a flat reject.
            if (!string.IsNullOrWhiteSpace(message) && message.StartsWith(FirebaseCloudManager.SuggestionPrefix))
            {
                string suggestion = message.Substring(FirebaseCloudManager.SuggestionPrefix.Length);
                if (!string.IsNullOrWhiteSpace(suggestion))
                {
                    if (inputField != null)
                    {
                        inputField.text = suggestion;
                    }
                    UpdateInputState(suggestion);
                    if (helperText != null)
                    {
                        helperText.text = string.Format(UILanguage.Translate(
                            "BU ISIM ALINMIS. {0} UYGUN — ISTERSEN ONAYLA.",
                            "TAKEN. {0} IS FREE — TAP CONFIRM TO USE IT.",
                            "OCUPADO. {0} ESTA LIBRE — CONFIRMA PARA USARLO."), suggestion);
                        helperText.color = UIStyle.Gold;
                    }
                    return;
                }
            }

            UpdateInputState(inputField != null ? inputField.text.Trim().ToUpperInvariant() : string.Empty);
            if (helperText != null && !string.IsNullOrWhiteSpace(message))
            {
                helperText.text = message;
                helperText.color = UIStyle.Danger;
            }
        }

        private void UpdateInputState(string currentValue)
        {
            int count = currentValue.Length;
            bool isValid = count >= 2 && count <= 12;
            confirmButton.interactable = isValid && !isSubmitting;

            if (confirmButtonImage != null)
            {
                confirmButtonImage.color = isValid && !isSubmitting ? Color.white : new Color(1f, 1f, 1f, 0.52f);
            }

            if (confirmButtonLabel != null && !isSubmitting)
            {
                confirmButtonLabel.text = UILanguage.Translate("KAYDET VE GIR", "SAVE AND JOIN", "GUARDAR Y ENTRAR");
                confirmButtonLabel.color = isValid ? Color.white : new Color(1f, 1f, 1f, 0.72f);
            }

            if (counterText != null)
            {
                counterText.text = $"{count}/12";
                counterText.color = isValid || count == 0 ? UIStyle.PopupTextDim : UIStyle.Danger;
            }

            if (helperText != null && !isSubmitting)
            {
                helperText.text = isValid
                    ? UILanguage.Translate("Skor tabelesinde bu isim gorunecek.", "This tag will show on the leaderboard.", "Este nombre aparecera en la clasificacion.")
                    : UILanguage.Translate("2-12 karakter kullan. Harf, rakam ve _ desteklenir.", "Use 2-12 characters. Letters, numbers, and _ are allowed.", "Usa 2-12 caracteres. Se permiten letras, numeros y _.");
                helperText.color = isValid || count == 0 ? UIStyle.PopupTextDim : UIStyle.Danger;
            }
        }

        private static void SetupSheetPanel(Image image)
        {
            UICandySkin.ApplyCandyButton(image, "sheet_modal_panel", SheetPanelSlice, 220f);
        }

        private static void SetupSheetRow(Image image, Color tint)
        {
            UICandySkin.ApplyCandyButton(image, "sheet_modal_row", SheetRowSlice, 220f);
            image.color = tint;
        }

        private static void SetupPurpleRibbon(Image image)
        {
            UICandySkin.ApplyCandyButton(image, "out_btn_purple", PurpleButtonSlice, 350f);
        }

        private static void SetupActionButton(Image image)
        {
            UICandySkin.ApplyCandyButton(image, "out_btn_orange", OrangeButtonSlice, 350f);
        }
    }
}
