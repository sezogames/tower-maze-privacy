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

            // Dark card background (UIStyle.SurfaceDark — semi-transparent white on dark bg)
            backgroundImage = container.AddComponent<Image>();
            backgroundImage.color = new Color(UIStyle.SurfaceDark.r, UIStyle.SurfaceDark.g, UIStyle.SurfaceDark.b, 0f);

            // Title: bright white
            titleText = UIManager.CreateText("RewardToastTitle", container.transform, font, 34, TextAnchor.UpperCenter, UIStyle.TextPrimary);
            titleText.fontStyle = FontStyle.Bold;
            UIManager.Stretch(titleText.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(18f, -10f), new Vector2(-18f, -4f));

            // Subtitle: gold (coin-colored)
            subtitleText = UIManager.CreateText("RewardToastSubtitle", container.transform, font, 28, TextAnchor.LowerCenter, UIStyle.Gold);
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
            Text title = UIManager.CreateText("PauseTitle", panel.transform, font, 36, TextAnchor.MiddleCenter, UIStyle.TextPrimary);
            title.text = "PAUSED";
            title.fontStyle = FontStyle.Bold;
            UIManager.Stretch(title.rectTransform,
                new Vector2(0f, 0.72f), new Vector2(1f, 1f),
                new Vector2(24f, 0f), new Vector2(-24f, 0f));

            // Resume button — orange gradient (dominant action)
            GameObject resumeGo = new("ResumeBtn");
            resumeGo.transform.SetParent(panel.transform, false);
            GradientImage resumeGrad = resumeGo.AddComponent<GradientImage>();
            resumeGrad.colorTop = UIStyle.ActionLight;
            resumeGrad.colorBottom = UIStyle.Action;
            Button resumeBtn = resumeGo.AddComponent<Button>();
            resumeBtn.targetGraphic = resumeGrad;
            RectTransform resumeRt = resumeGo.GetComponent<RectTransform>();
            resumeRt.anchorMin = new Vector2(0.1f, 0.42f);
            resumeRt.anchorMax = new Vector2(0.9f, 0.68f);
            resumeRt.offsetMin = Vector2.zero;
            resumeRt.offsetMax = Vector2.zero;
            Text resumeLbl = UIManager.CreateText(resumeGo.transform, "Label", "▶ DEVAM ET",
                16, FontStyle.Bold, Color.white, font, TextAnchor.MiddleCenter);
            UIManager.Stretch(resumeLbl.rectTransform);
            UIManager.BindButton(resumeBtn, () =>
            {
                _runner.StartCoroutine(UIStyle.ButtonPress(resumeRt));
                buttonClickSound?.Invoke();
                onResume?.Invoke();
            });

            // Main menu button — secondary (semi-transparent dark)
            GameObject menuGo = UIManager.CreateSecondaryButton(panel.transform, "MainMenuBtn", "ANA MENÜ", font);
            RectTransform menuRt = menuGo.GetComponent<RectTransform>();
            menuRt.anchorMin = new Vector2(0.1f, 0.1f);
            menuRt.anchorMax = new Vector2(0.9f, 0.36f);
            menuRt.offsetMin = Vector2.zero;
            menuRt.offsetMax = Vector2.zero;
            Button menuBtn = menuGo.GetComponent<Button>();
            UIManager.BindButton(menuBtn, () =>
            {
                _runner.StartCoroutine(UIStyle.ButtonPress(menuRt));
                buttonClickSound?.Invoke();
                onReturnToMenu?.Invoke();
            });
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
            Button closeBtn = UIManager.CreateButton("UpsellClose", cardObj.transform, runtimeFont, "✕", UIStyle.SurfaceDark, UIStyle.TextDim);
            Text closeBtnLabel = closeBtn.GetComponentInChildren<Text>();
            closeBtnLabel.fontSize = 18;
            closeBtnLabel.fontStyle = FontStyle.Bold;
            RectTransform closeBtnRt = (RectTransform)closeBtn.transform;
            closeBtnRt.anchorMin = new Vector2(1f, 1f);
            closeBtnRt.anchorMax = new Vector2(1f, 1f);
            closeBtnRt.pivot = new Vector2(0.5f, 0.5f);
            closeBtnRt.anchoredPosition = new Vector2(-20f, -20f);
            closeBtnRt.sizeDelta = new Vector2(36f, 36f);
            UIManager.BindButton(closeBtn, () =>
            {
                _runner.StartCoroutine(UIStyle.ButtonPress(closeBtnRt));
                buttonClickSound?.Invoke();
                OnCloseClicked();
            });

            // Image frame (top 28% of card) — dark surface
            previewFrame = UIManager.CreateCard("UpsellPreviewFrame", cardObj.transform, UIStyle.SurfaceDark2, UIStyle.BorderDark);
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

            // Title — bright white text
            titleText = UIManager.CreateText("UpsellTitle", cardObj.transform, runtimeFont, 21, TextAnchor.MiddleCenter, UIStyle.TextPrimary);
            titleText.fontStyle = FontStyle.Bold;
            UIManager.Stretch(titleText.rectTransform, new Vector2(0.04f, 0.46f), new Vector2(0.96f, 0.62f), Vector2.zero, Vector2.zero);

            // Description — dim text
            descText = UIManager.CreateText("UpsellDesc", cardObj.transform, runtimeFont, 15, TextAnchor.MiddleCenter, UIStyle.TextDim);
            descText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIManager.Stretch(descText.rectTransform, new Vector2(0.04f, 0.32f), new Vector2(0.96f, 0.47f), Vector2.zero, Vector2.zero);

            // Price — gold color
            priceText = UIManager.CreateText("UpsellPrice", cardObj.transform, runtimeFont, 17, TextAnchor.MiddleCenter, UIStyle.Gold);
            priceText.fontStyle = FontStyle.Bold;
            UIManager.Stretch(priceText.rectTransform, new Vector2(0.04f, 0.18f), new Vector2(0.96f, 0.32f), Vector2.zero, Vector2.zero);

            // Buy button — orange gradient with ButtonPress animation
            GameObject buyGo = new("UpsellBuy");
            buyGo.transform.SetParent(cardObj.transform, false);
            GradientImage buyGrad = buyGo.AddComponent<GradientImage>();
            buyGrad.colorTop = UIStyle.ActionLight;
            buyGrad.colorBottom = UIStyle.Action;
            buyButton = buyGo.AddComponent<Button>();
            buyButton.targetGraphic = buyGrad;
            RectTransform buyRt = buyGo.GetComponent<RectTransform>();
            buyRt.anchorMin = new Vector2(0.08f, 0.04f);
            buyRt.anchorMax = new Vector2(0.92f, 0.17f);
            buyRt.offsetMin = Vector2.zero;
            buyRt.offsetMax = Vector2.zero;
            Text buyLabel = UIManager.CreateText(buyGo.transform, "Label", "SATIN AL",
                18, FontStyle.Bold, Color.white, runtimeFont, TextAnchor.MiddleCenter);
            UIManager.Stretch(buyLabel.rectTransform);
            UIManager.BindButton(buyButton, () =>
            {
                _runner.StartCoroutine(UIStyle.ButtonPress(buyRt));
                buttonClickSound?.Invoke();
                OnBuyClicked();
            });
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
            fillImage.color = UIColors.PrimaryLight;
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

    // ─── Temporary stub — superseded by inline leaderboard in StartScreen.cs (Task 6) ───
    // Kept here so the original StartScreenController compiles until Task 5 rewrites it.
    public class LeaderboardPanelController : MonoBehaviour
    {
        public void Initialize(Font font, ThemeDefinition theme, string title) { }
        public void SetEntries(System.Collections.Generic.IReadOnlyList<LeaderboardEntry> entries) { }
    }

    public sealed class NicknamePopupController : MonoBehaviour
    {
        private Action<string> onConfirm;
        private InputField inputField;
        private Button confirmButton;

        public void Initialize(Font font, ThemeDefinition theme, Action<string> onConfirmCallback)
        {
            onConfirm = onConfirmCallback;

            foreach (Transform child in transform) { Destroy(child.gameObject); }

            var overlay = UIManager.CreateImage(transform, "Overlay", new Color(0, 0, 0, 0.85f));
            UIManager.Stretch(overlay.rectTransform);

            var card = new GameObject("Card");
            card.transform.SetParent(transform, false);
            var cardImg = card.AddComponent<Image>();
            UICandySkin.ApplyCandyPanel(cardImg);
            cardImg.color = new Color(0.92f, 0.88f, 1f, 0.98f);
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.sizeDelta = new Vector2(320, 280);

            var cardLayout = card.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(24, 24, 32, 24);
            cardLayout.spacing = 16;
            cardLayout.childAlignment = TextAnchor.UpperCenter;
            cardLayout.childControlHeight = false;
            cardLayout.childForceExpandHeight = false;

            var title = UILanguage.Translate("ADINI GIR", "ENTER YOUR NAME", "INGRESA TU NOMBRE");
            var titleTxt = UIManager.CreateText(card.transform, "Title", title, 18, FontStyle.Bold, Color.white, font);
            titleTxt.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.5f);

            var inputGo = new GameObject("NicknameInput");
            inputGo.transform.SetParent(card.transform, false);
            var inputBg = inputGo.AddComponent<Image>();
            inputBg.color = new Color(1, 1, 1, 0.15f);
            inputGo.AddComponent<LayoutElement>().preferredHeight = 44;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(inputGo.transform, false);
            var inputText = textGo.AddComponent<Text>();
            inputText.font = font;
            inputText.fontSize = 16;
            inputText.fontStyle = FontStyle.Bold;
            inputText.color = Color.white;
            inputText.alignment = TextAnchor.MiddleCenter;
            inputText.supportRichText = false;
            UIManager.Stretch(inputText.rectTransform, 8, 8, 4, 4);

            var placeholderGo = new GameObject("Placeholder");
            placeholderGo.transform.SetParent(inputGo.transform, false);
            var placeholder = placeholderGo.AddComponent<Text>();
            placeholder.font = font;
            placeholder.fontSize = 16;
            placeholder.fontStyle = FontStyle.Italic;
            placeholder.color = new Color(1, 1, 1, 0.35f);
            placeholder.alignment = TextAnchor.MiddleCenter;
            placeholder.text = UILanguage.Translate("TAKMA AD", "NICKNAME", "APODO");
            UIManager.Stretch(placeholder.rectTransform, 8, 8, 4, 4);

            inputField = inputGo.AddComponent<InputField>();
            inputField.textComponent = inputText;
            inputField.placeholder = placeholder;
            inputField.characterLimit = 12;
            inputField.contentType = InputField.ContentType.Alphanumeric;
            inputField.onValueChanged.AddListener(OnInputChanged);

            var confirmGo = UIManager.CreateActionButton(card.transform, "ConfirmBtn",
                UILanguage.Translate("ONAYLA", "CONFIRM", "CONFIRMAR"),
                font, UIStyle.Action, UIStyle.ActionLight, 44, 20);
            confirmButton = confirmGo.GetComponent<Button>();
            confirmButton.interactable = false;
            UIManager.BindButton(confirmButton, OnConfirmClicked);
        }

        private void OnInputChanged(string value)
        {
            string cleaned = System.Text.RegularExpressions.Regex.Replace(value, @"[^A-Za-z0-9_]", "");
            if (cleaned != value)
            {
                inputField.text = cleaned;
                return;
            }

            confirmButton.interactable = cleaned.Length >= 2;
        }

        private void OnConfirmClicked()
        {
            string value = inputField.text.Trim().ToUpperInvariant();
            if (value.Length < 2 || value.Length > 12) return;
            onConfirm?.Invoke(value);
            Destroy(gameObject);
        }
    }
}
