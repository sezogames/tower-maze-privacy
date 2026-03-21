using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TowerMaze
{
    public sealed class UIHudController : MonoBehaviour
    {
        private Text scoreText;
        private Text lavaText;
        private Text coinText;
        private Image progressFill;
        private List<Image> milestoneTicks = new();
        private GameConfig gameConfig;
        private ScoreManager scoreManager;
        private EconomyManager economyManager;
        private RewardToastController rewardToastController;
        private Button pauseButton;
        private LifeBarUI lifeBarUI;

        public void Initialize(Font font, ThemeDefinition theme, Action onPause,
            Action soundCallback = null, GameConfig config = null, ScoreManager scoreMgr = null)
        {
            gameConfig = config;
            scoreManager = scoreMgr;
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

            coinText = UIManager.CreateText("CoinText", transform, font, 9,
                TextAnchor.MiddleLeft, Color.white);
            coinText.rectTransform.anchorMin = new Vector2(0.42f, 0.935f);
            coinText.rectTransform.anchorMax = new Vector2(0.76f, 0.975f);
            coinText.rectTransform.offsetMin = coinText.rectTransform.offsetMax = Vector2.zero;

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
            var pauseLbl = UIManager.CreateText("PauseIcon", pauseGo.transform, font, 12,
                TextAnchor.MiddleCenter, UIStyle.TextDim);
            pauseLbl.text = "||";
            UIManager.Stretch(pauseLbl.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // ── Score label + big score ──────────────────────────────────────────
            var scoreLabel = UIManager.CreateText("ScoreLabel", transform, font, 9,
                TextAnchor.MiddleLeft, new Color(0.486f, 0.435f, 0.627f, 1f));
            scoreLabel.text = "SCORE";
            scoreLabel.rectTransform.anchorMin = new Vector2(0.045f, 0.80f);
            scoreLabel.rectTransform.anchorMax = new Vector2(0.55f,  0.835f);
            scoreLabel.rectTransform.offsetMin = scoreLabel.rectTransform.offsetMax = Vector2.zero;

            scoreText = UIManager.CreateText("Score", transform, font, 48,
                TextAnchor.MiddleLeft, Color.white);
            scoreText.fontStyle = FontStyle.Bold;
            scoreText.rectTransform.anchorMin = new Vector2(0.04f, 0.70f);
            scoreText.rectTransform.anchorMax = new Vector2(0.90f, 0.80f);
            scoreText.rectTransform.offsetMin = scoreText.rectTransform.offsetMax = Vector2.zero;

            // ── Lava pill ────────────────────────────────────────────────────────
            var lavaGo = new GameObject("LavaPill");
            lavaGo.transform.SetParent(transform, false);
            var lavaBg = lavaGo.AddComponent<Image>();
            lavaBg.color = new Color(1f, 1f, 1f, 0.07f);
            var lavaRt = lavaBg.rectTransform;
            lavaRt.anchorMin = new Vector2(0.04f, 0.635f);
            lavaRt.anchorMax = new Vector2(0.58f, 0.695f);
            lavaRt.offsetMin = lavaRt.offsetMax = Vector2.zero;
            lavaText = UIManager.CreateText("LavaText", lavaGo.transform, font, 11,
                TextAnchor.MiddleLeft, UIStyle.Danger);
            UIManager.Stretch(lavaText.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(8f, 0f), new Vector2(-8f, 0f));
        }

        public void SetDependencies(EconomyManager eco, RewardToastController toast)
        {
            economyManager = eco;
            rewardToastController = toast;
            lifeBarUI?.SetEconomyManager(economyManager);
        }

        public void SetValues(float score, float bestScore, float runTime, int zoneIndex,
            float lavaGap, float gapDangerNormalized, bool isNewBest, bool showControlsHint)
        {
            if (scoreText != null)
                scoreText.text = $"{score:0}m";

            if (lavaText != null)
                lavaText.text = $"LAVA +{lavaGap:0.0}m";

            if (progressFill != null && gameConfig != null && gameConfig.milestoneMax > 0f)
            {
                float frac = Mathf.Clamp01(score / gameConfig.milestoneMax);
                progressFill.rectTransform.anchorMax = new Vector2(1f, frac);
            }

            if (coinText != null && economyManager != null)
                coinText.text = $"COINS {economyManager.EmberBalance}";
        }

        public void SpawnCoinFloat(int amount, MonoBehaviour runner)
        {
            if (coinText == null) return;
            var go = new GameObject("CoinFloat");
            go.transform.SetParent(coinText.transform.parent, false);
            var t = go.AddComponent<Text>();
            t.text       = $"+{amount} COINS";
            t.font       = coinText.font;
            t.fontSize   = 11;
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

        private void OnDestroy()
        {
            if (scoreManager != null)
                scoreManager.OnMilestonePassed -= HandleMilestonePassed;
        }
    }
}
