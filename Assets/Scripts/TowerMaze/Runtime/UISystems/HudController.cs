using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace TowerMaze
{
    public sealed class UIHudController : MonoBehaviour
    {
        private Text scoreText;
        private Text scoreLabelText;
        private Text bestScoreText;
        private Text lavaText;
        private RectTransform lavaPillRect;
        private Text coinText;
        private Image progressFill;
        private List<Image> milestoneTicks = new();
        private GameConfig gameConfig;
        private ScoreManager scoreManager;
        private EconomyManager economyManager;
        private RewardToastController rewardToastController;
        private Button pauseButton;
        private LifeBarUI lifeBarUI;
        private const float LavaPillMinWidth = 88f;
        private const float LavaPillHorizontalPadding = 20f;
        private const float JoystickBaseSize = 168f;
        private const float JoystickThumbSize = 76f;
        private const float JoystickRadius = 48f;
        private const float JoystickDeadZone = 0.12f;
        private PlayerController playerController;
        private RectTransform joystickBaseRect;
        private RectTransform joystickThumbRect;
        private bool joystickActive;
        private bool joystickUsingMouse;
        private int joystickTouchId = -1;
        private Vector2 joystickOriginLocal;
        private readonly List<RaycastResult> pointerRaycastResults = new();
        private static Sprite joystickBaseSprite;
        private static Sprite joystickThumbSprite;

        public void Initialize(Font font, ThemeDefinition theme, Action onPause,
            PlayerController player = null, Action soundCallback = null, GameConfig config = null, ScoreManager scoreMgr = null)
        {
            gameConfig = config;
            scoreManager = scoreMgr;
            playerController = player;
            if (scoreManager != null)
                scoreManager.OnMilestonePassed += HandleMilestonePassed;

            // No full-screen background: 3D game world is visible through the HUD during gameplay.

            // ── Left edge progress bar: 4px wide ────────────────────────────────
            var barRoot = new GameObject("ProgressBar");
            barRoot.transform.SetParent(transform, false);
            var barRt = barRoot.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 0f);
            barRt.anchorMax = new Vector2(0f, 1f);
            barRt.offsetMin = new Vector2(7f,  0f);
            barRt.offsetMax = new Vector2(11f, 0f);
            var barTrack = barRoot.AddComponent<Image>();
            barTrack.color = new Color(1f, 1f, 1f, 0.06f);
            barTrack.raycastTarget = false;

            // Fill (anchored bottom, anchorMax.y driven by score / milestoneMax)
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(barRoot.transform, false);
            progressFill = fillGo.AddComponent<Image>();
            progressFill.color = UIStyle.Brand;
            progressFill.raycastTarget = false;
            var fillRt = progressFill.rectTransform;
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(1f, 0f);
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
            fillRt.pivot = new Vector2(0.5f, 0f);

            // Milestone ticks
            if (config != null && config.heightMilestones != null)
            {
                foreach (int mh in config.heightMilestones)
                {
                    float max = config.milestoneMax > 0f ? config.milestoneMax : 200f;
                    float yFrac = Mathf.Clamp01((float)mh / max);
                    var tickGo = new GameObject($"Tick_{mh}m");
                    tickGo.transform.SetParent(barRoot.transform, false);
                    var tick = tickGo.AddComponent<Image>();
                    tick.color = new Color(1f, 1f, 1f, 0.30f);
                    tick.raycastTarget = false;
                    var tickRt = tick.rectTransform;
                    tickRt.anchorMin = new Vector2(0f, yFrac);
                    tickRt.anchorMax = new Vector2(2f, yFrac);
                    tickRt.offsetMin = new Vector2(0f, -0.5f);
                    tickRt.offsetMax = new Vector2(8f,  0.5f);
                    milestoneTicks.Add(tick);
                }
            }

            // ── Top row ──────────────────────────────────────────────────────────
            Image lifeBarCard = UIManager.CreateCard("LifeBarCard", transform, UIStyle.SurfaceDark, UIStyle.BorderDark);
            lifeBarCard.rectTransform.anchorMin = new Vector2(0.03f, 0.90f);
            lifeBarCard.rectTransform.anchorMax = new Vector2(0.41f, 0.985f);
            lifeBarCard.rectTransform.offsetMin = lifeBarCard.rectTransform.offsetMax = Vector2.zero;
            lifeBarUI = lifeBarCard.gameObject.AddComponent<LifeBarUI>();
            lifeBarUI.Initialize(font);

            // Pause button — top-right
            var pauseGo = new GameObject("PauseBtn");
            pauseGo.transform.SetParent(transform, false);
            var pauseImg = pauseGo.AddComponent<Image>();
            pauseImg.color = new Color(1f, 1f, 1f, 0.07f);
            var pauseRt = pauseImg.rectTransform;
            pauseRt.anchorMin = new Vector2(0.89f, 0.935f);
            pauseRt.anchorMax = new Vector2(0.98f, 0.975f);
            pauseRt.offsetMin = pauseRt.offsetMax = Vector2.zero;
            pauseButton = pauseGo.AddComponent<Button>();
            UIManager.BindButton(pauseButton, onPause, soundCallback);
            var pauseLbl = UIManager.CreateText("PauseIcon", pauseGo.transform, font, 14,
                TextAnchor.MiddleCenter, UIStyle.TextDim);
            pauseLbl.text = "||";
            UIManager.Stretch(pauseLbl.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image bestScoreCard = UIManager.CreateCard("BestScoreBar", transform, UIStyle.SurfaceDark, UIStyle.BorderDark);
            bestScoreCard.rectTransform.anchorMin = new Vector2(0.67f, 0.885f);
            bestScoreCard.rectTransform.anchorMax = new Vector2(0.98f, 0.925f);
            bestScoreCard.rectTransform.offsetMin = bestScoreCard.rectTransform.offsetMax = Vector2.zero;
            bestScoreText = UIManager.CreateText("BestScoreText", bestScoreCard.transform, font, 12,
                TextAnchor.MiddleCenter, Color.white);
            bestScoreText.fontStyle = FontStyle.Bold;
            bestScoreText.text = "BEST 0m";
            UIManager.Stretch(bestScoreText.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(8f, 0f), new Vector2(-8f, 0f));
            UIManager.SetScaledBestFit(bestScoreText, 9, 12);

            // ── Score label + big score ──────────────────────────────────────────
            scoreLabelText = UIManager.CreateText("ScoreLabel", transform, font, 11,
                TextAnchor.MiddleLeft, new Color(0.486f, 0.435f, 0.627f, 1f));
            scoreLabelText.rectTransform.anchorMin = new Vector2(0.045f, 0.80f);
            scoreLabelText.rectTransform.anchorMax = new Vector2(0.55f,  0.835f);
            scoreLabelText.rectTransform.offsetMin = scoreLabelText.rectTransform.offsetMax = Vector2.zero;

            scoreText = UIManager.CreateText("Score", transform, font, 48,
                TextAnchor.MiddleLeft, Color.white);
            scoreText.fontStyle = FontStyle.Bold;
            scoreText.rectTransform.anchorMin = new Vector2(0.04f, 0.70f);
            scoreText.rectTransform.anchorMax = new Vector2(0.90f, 0.80f);
            scoreText.rectTransform.offsetMin = scoreText.rectTransform.offsetMax = Vector2.zero;

            // ── Lava pill ────────────────────────────────────────────────────────
            var lavaGo = new GameObject("LavaPill");
            lavaGo.transform.SetParent(transform, false);
            var lavaRt = lavaGo.AddComponent<RectTransform>();
            lavaRt.anchorMin = new Vector2(0.04f, 0.635f);
            lavaRt.anchorMax = new Vector2(0.04f, 0.695f);
            lavaRt.offsetMin = lavaRt.offsetMax = Vector2.zero;
            lavaRt.pivot = new Vector2(0f, 0.5f);
            lavaRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 120f);
            lavaPillRect = lavaRt;
            lavaText = UIManager.CreateText("LavaText", lavaGo.transform, font, 13,
                TextAnchor.MiddleLeft, UIStyle.Danger);
            UIManager.Stretch(lavaText.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(8f, 0f), new Vector2(-8f, 0f));

            ApplyLocalizedTexts();
        }

        public void SetDependencies(EconomyManager eco, RewardToastController toast)
        {
            economyManager = eco;
            rewardToastController = toast;
            lifeBarUI?.SetEconomyManager(economyManager);
        }

        private void OnEnable()
        {
            ApplyLocalizedTexts();
        }

        private void Update()
        {
        }

        public void SetValues(float score, float bestScore, float runTime, int zoneIndex,
            float lavaGap, float gapDangerNormalized, bool isNewBest, bool showControlsHint,
            string bestLabel = null)
        {
            if (scoreText != null)
                scoreText.text = $"{score:0}m";

            if (bestScoreText != null)
            {
                string label = bestLabel ?? UILanguage.Translate("EN IYI", "BEST", "MEJOR");
                bestScoreText.text = $"{label} {bestScore:0}m";
            }

            if (lavaText != null)
            {
                lavaText.text = $"{UILanguage.Translate("LAV", "LAVA", "LAVA")} +{lavaGap:0.0}m";
                UpdateLavaPillWidth();
            }

            if (progressFill != null && gameConfig != null && gameConfig.milestoneMax > 0f)
            {
                float frac = Mathf.Clamp01(score / gameConfig.milestoneMax);
                progressFill.rectTransform.anchorMax = new Vector2(1f, frac);
            }

            if (coinText != null && economyManager != null)
                coinText.text = $"{UILanguage.Translate("COINLER", "COINS", "MONEDAS")} {economyManager.EmberBalance}";
        }

        public void SpawnCoinFloat(int amount, MonoBehaviour runner)
        {
            if (coinText == null) return;
            var go = new GameObject("CoinFloat");
            go.transform.SetParent(coinText.transform.parent, false);
            var t = go.AddComponent<Text>();
            t.text       = $"+{amount} {UILanguage.Translate("COIN", "COIN", "MONEDA")}";
            t.font       = coinText.font;
            UIManager.SetScaledFontSize(t, 13);
            t.fontStyle  = FontStyle.Bold;
            t.color      = UIStyle.Gold;
            t.alignment  = TextAnchor.MiddleCenter;
            t.rectTransform.anchoredPosition = coinText.rectTransform.anchoredPosition;
            t.rectTransform.sizeDelta        = new Vector2(80, 24);
            runner.StartCoroutine(UIStyle.CoinFloat(t));
        }

        private void HandleMilestonePassed(int heightMeters)
        {
            rewardToastController?.Enqueue($"{heightMeters}m!", "", UIStyle.Brand);

            if (gameConfig != null && gameConfig.heightMilestones != null)
            {
                int idx = System.Array.IndexOf(gameConfig.heightMilestones, heightMeters);
                if (idx >= 0 && idx < milestoneTicks.Count)
                    StartCoroutine(FlashTick(milestoneTicks[idx]));
            }
        }

        private IEnumerator FlashTick(Image tick)
        {
            float t = 0f;
            Color baseColor = new Color(1f, 1f, 1f, 0.30f);
            while (t < 0.4f)
            {
                tick.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.30f, 0f, t / 0.4f));
                t += Time.deltaTime;
                yield return null;
            }
            tick.color = baseColor;
        }

        private void ApplyLocalizedTexts()
        {
            if (scoreLabelText != null)
            {
                scoreLabelText.text = UILanguage.Translate("SKOR", "SCORE", "PUNTUACION");
            }

            if (bestScoreText != null && string.IsNullOrWhiteSpace(bestScoreText.text))
            {
                bestScoreText.text = $"{UILanguage.Translate("EN IYI", "BEST", "MEJOR")} 0m";
            }

            if (lavaText != null && string.IsNullOrWhiteSpace(lavaText.text))
            {
                lavaText.text = $"{UILanguage.Translate("LAV", "LAVA", "LAVA")} +0.0m";
            }

            UpdateLavaPillWidth();
        }

        private void UpdateLavaPillWidth()
        {
            if (lavaPillRect == null || lavaText == null)
            {
                return;
            }

            float preferredWidth = Mathf.Max(0f, lavaText.preferredWidth);
            float width = Mathf.Max(LavaPillMinWidth, preferredWidth + LavaPillHorizontalPadding);
            lavaPillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        private void BuildFloatingJoystick(ThemeDefinition theme)
        {
            Image baseImage = UIManager.CreateImage("FloatingJoystickBase", transform, new Color(1f, 1f, 1f, 0.12f));
            baseImage.sprite = GetJoystickBaseSprite();
            baseImage.raycastTarget = false;
            joystickBaseRect = baseImage.rectTransform;
            joystickBaseRect.anchorMin = new Vector2(0.5f, 0.5f);
            joystickBaseRect.anchorMax = new Vector2(0.5f, 0.5f);
            joystickBaseRect.pivot = new Vector2(0.5f, 0.5f);
            joystickBaseRect.sizeDelta = new Vector2(JoystickBaseSize, JoystickBaseSize);
            joystickBaseRect.gameObject.SetActive(false);

            Color thumbColor = theme != null ? theme.accentColor : UIStyle.Brand;
            Image thumbImage = UIManager.CreateImage("FloatingJoystickThumb", transform, new Color(thumbColor.r, thumbColor.g, thumbColor.b, 0.92f));
            thumbImage.sprite = GetJoystickThumbSprite();
            thumbImage.raycastTarget = false;
            joystickThumbRect = thumbImage.rectTransform;
            joystickThumbRect.anchorMin = new Vector2(0.5f, 0.5f);
            joystickThumbRect.anchorMax = new Vector2(0.5f, 0.5f);
            joystickThumbRect.pivot = new Vector2(0.5f, 0.5f);
            joystickThumbRect.sizeDelta = new Vector2(JoystickThumbSize, JoystickThumbSize);
            joystickThumbRect.gameObject.SetActive(false);
        }

        private void HandleFloatingJoystick()
        {
            if (playerController == null || joystickBaseRect == null || joystickThumbRect == null)
            {
                return;
            }

            if (Time.timeScale <= 0f)
            {
                DeactivateJoystick();
                return;
            }

            bool touchHandled = HandleTouchJoystick();
            if (!touchHandled)
            {
                HandleMouseJoystick();
            }
        }

        private bool HandleTouchJoystick()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                if (!joystickUsingMouse)
                {
                    DeactivateJoystick();
                }

                return false;
            }

            if (joystickActive && !joystickUsingMouse)
            {
                TouchControl activeTouch = FindTouchById(touchscreen, joystickTouchId);
                if (activeTouch != null && activeTouch.press.isPressed)
                {
                    UpdateJoystick(activeTouch.position.ReadValue());
                    return true;
                }

                DeactivateJoystick();
            }

            for (int index = 0; index < touchscreen.touches.Count; index++)
            {
                TouchControl touch = touchscreen.touches[index];
                if (!touch.press.wasPressedThisFrame)
                {
                    continue;
                }

                Vector2 screenPosition = touch.position.ReadValue();
                if (IsPointerOverInteractiveUi(screenPosition))
                {
                    continue;
                }

                ActivateJoystick(screenPosition, false, touch.touchId.ReadValue());
                UpdateJoystick(screenPosition);
                return true;
            }

            return false;
        }

        private void HandleMouseJoystick()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                if (joystickUsingMouse)
                {
                    DeactivateJoystick();
                }

                return;
            }

            if (joystickUsingMouse)
            {
                if (mouse.leftButton.isPressed)
                {
                    UpdateJoystick(mouse.position.ReadValue());
                }
                else
                {
                    DeactivateJoystick();
                }

                return;
            }

            if (!mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            Vector2 screenPosition = mouse.position.ReadValue();
            if (IsPointerOverInteractiveUi(screenPosition))
            {
                return;
            }

            ActivateJoystick(screenPosition, true, -1);
            UpdateJoystick(screenPosition);
        }

        private void ActivateJoystick(Vector2 screenPosition, bool useMouse, int touchId)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)transform,
                    screenPosition,
                    null,
                    out joystickOriginLocal))
            {
                return;
            }

            joystickActive = true;
            joystickUsingMouse = useMouse;
            joystickTouchId = touchId;
            joystickBaseRect.anchoredPosition = joystickOriginLocal;
            joystickThumbRect.anchoredPosition = joystickOriginLocal;
            joystickBaseRect.gameObject.SetActive(true);
            joystickThumbRect.gameObject.SetActive(true);
            playerController.SetVirtualJoystickState(true, Vector2.zero);
        }

        private void UpdateJoystick(Vector2 screenPosition)
        {
            if (!joystickActive)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)transform,
                    screenPosition,
                    null,
                    out Vector2 pointerLocal))
            {
                return;
            }

            Vector2 delta = pointerLocal - joystickOriginLocal;
            Vector2 clampedDelta = Vector2.ClampMagnitude(delta, JoystickRadius);
            joystickThumbRect.anchoredPosition = joystickOriginLocal + clampedDelta;

            Vector2 normalized = clampedDelta / Mathf.Max(1f, JoystickRadius);
            float magnitude = normalized.magnitude;
            if (magnitude < JoystickDeadZone)
            {
                normalized = Vector2.zero;
            }
            else
            {
                float scaledMagnitude = Mathf.InverseLerp(JoystickDeadZone, 1f, magnitude);
                normalized = normalized.normalized * scaledMagnitude;
            }

            playerController.SetVirtualJoystickState(true, normalized);
        }

        private void DeactivateJoystick()
        {
            if (!joystickActive)
            {
                return;
            }

            joystickActive = false;
            joystickUsingMouse = false;
            joystickTouchId = -1;
            if (joystickBaseRect != null)
            {
                joystickBaseRect.gameObject.SetActive(false);
            }

            if (joystickThumbRect != null)
            {
                joystickThumbRect.gameObject.SetActive(false);
            }

            playerController?.SetVirtualJoystickState(false, Vector2.zero);
        }

        private bool IsPointerOverInteractiveUi(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            pointerRaycastResults.Clear();
            PointerEventData pointerEvent = new(eventSystem)
            {
                position = screenPosition
            };

            eventSystem.RaycastAll(pointerEvent, pointerRaycastResults);
            for (int index = 0; index < pointerRaycastResults.Count; index++)
            {
                GameObject hitObject = pointerRaycastResults[index].gameObject;
                if (hitObject == null)
                {
                    continue;
                }

                if (hitObject.GetComponentInParent<Selectable>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static TouchControl FindTouchById(Touchscreen touchscreen, int touchId)
        {
            for (int index = 0; index < touchscreen.touches.Count; index++)
            {
                TouchControl touch = touchscreen.touches[index];
                if (touch.touchId.ReadValue() == touchId)
                {
                    return touch;
                }
            }

            return null;
        }

        private static Sprite GetJoystickBaseSprite()
        {
            joystickBaseSprite ??= CreateJoystickSprite("TowerMaze_JoystickBase", 128);
            return joystickBaseSprite;
        }

        private static Sprite GetJoystickThumbSprite()
        {
            joystickThumbSprite ??= CreateJoystickSprite("TowerMaze_JoystickThumb", 96);
            return joystickThumbSprite;
        }

        private static Sprite CreateJoystickSprite(string name, int size)
        {
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false, true)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = (size * 0.5f) - 1f;
            float feather = Mathf.Max(2f, size * 0.08f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01((radius - distance) / feather);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private void OnDestroy()
        {
            DeactivateJoystick();
            if (scoreManager != null)
                scoreManager.OnMilestonePassed -= HandleMilestonePassed;
        }
    }
}
