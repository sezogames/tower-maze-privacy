using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TowerMaze
{
    public sealed class FailScreenController : MonoBehaviour
    {
        private EconomyManager economyManager;
        private Action buttonClickSound;
        private Action retryRunAction;
        private Action continueRunAction;
        private Action watchLifeRefillAdAction;

        private Button continueButton;
        private Text continueLabel;
        private RectTransform retryWrapper;
        private Button watchAdRetryButton;
        private Text watchAdRetryLabel;
        private Text scoreValueText;
        private RectTransform scoreRt;
        private Text bestScoreValue;
        private Text coinValueText;
        private Text livesValueText;
        private Text nextLifeValueText;
        private Image bgPulseOverlay;
        private Coroutine lifeTimerRoutine;

        public void Initialize(Font font, ThemeDefinition theme, EconomyManager economy,
            Action onRetry, Action onContinue, Action onReturnToMenu,
            Action onClaimDoubleReward, Action onWatchLifeRefillAd, Action onBuyLifeRefillWithCoins,
            Action onButtonClick = null, float retryDelay = 0.3f)
        {
            economyManager = economy;
            buttonClickSound = onButtonClick;
            retryRunAction = onRetry;
            continueRunAction = onContinue;
            watchLifeRefillAdAction = onWatchLifeRefillAd;

            Image bg = UIManager.CreateImage("FailBg", transform, UIStyle.FailBg);
            UIManager.Stretch(bg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            bgPulseOverlay = UIManager.CreateImage("BgPulse", transform,
                new Color(UIStyle.Danger.r, UIStyle.Danger.g, UIStyle.Danger.b, 0f));
            UIManager.Stretch(bgPulseOverlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bgPulseOverlay.raycastTarget = false;

            Text title = UIManager.CreateText("Title", transform, font, 32, TextAnchor.MiddleCenter, UIStyle.TextPrimary);
            title.text = "YOU MELTED";
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.anchorMin = new Vector2(0.12f, 0.82f);
            title.rectTransform.anchorMax = new Vector2(0.88f, 0.90f);
            title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;

            scoreValueText = UIManager.CreateText("Score", transform, font, 52, TextAnchor.MiddleCenter, UIStyle.Brand);
            scoreValueText.fontStyle = FontStyle.Bold;
            scoreValueText.rectTransform.anchorMin = new Vector2(0.12f, 0.70f);
            scoreValueText.rectTransform.anchorMax = new Vector2(0.88f, 0.80f);
            scoreValueText.rectTransform.offsetMin = scoreValueText.rectTransform.offsetMax = Vector2.zero;
            scoreRt = scoreValueText.rectTransform;

            Image statsCard = UIManager.CreateCard("StatsCard", transform, UIStyle.SurfaceDark, UIStyle.BorderDark);
            statsCard.rectTransform.anchorMin = new Vector2(0.12f, 0.52f);
            statsCard.rectTransform.anchorMax = new Vector2(0.88f, 0.67f);
            statsCard.rectTransform.offsetMin = statsCard.rectTransform.offsetMax = Vector2.zero;

            Text bestLabel = UIManager.CreateText("BestLbl", statsCard.transform, font, 10, TextAnchor.MiddleLeft, UIStyle.TextDim);
            bestLabel.text = "Best";
            bestLabel.rectTransform.anchorMin = new Vector2(0.04f, 0.64f);
            bestLabel.rectTransform.anchorMax = new Vector2(0.45f, 0.94f);
            bestLabel.rectTransform.offsetMin = bestLabel.rectTransform.offsetMax = Vector2.zero;

            bestScoreValue = UIManager.CreateText("BestVal", statsCard.transform, font, 11, TextAnchor.MiddleRight, UIStyle.TextPrimary);
            bestScoreValue.fontStyle = FontStyle.Bold;
            bestScoreValue.rectTransform.anchorMin = new Vector2(0.50f, 0.64f);
            bestScoreValue.rectTransform.anchorMax = new Vector2(0.96f, 0.94f);
            bestScoreValue.rectTransform.offsetMin = bestScoreValue.rectTransform.offsetMax = Vector2.zero;

            Text coinsLabel = UIManager.CreateText("CoinsLbl", statsCard.transform, font, 10, TextAnchor.MiddleLeft, UIStyle.TextDim);
            coinsLabel.text = "Coins";
            coinsLabel.rectTransform.anchorMin = new Vector2(0.04f, 0.34f);
            coinsLabel.rectTransform.anchorMax = new Vector2(0.45f, 0.62f);
            coinsLabel.rectTransform.offsetMin = coinsLabel.rectTransform.offsetMax = Vector2.zero;

            coinValueText = UIManager.CreateText("CoinsVal", statsCard.transform, font, 11, TextAnchor.MiddleRight, UIStyle.Gold);
            coinValueText.fontStyle = FontStyle.Bold;
            coinValueText.rectTransform.anchorMin = new Vector2(0.50f, 0.34f);
            coinValueText.rectTransform.anchorMax = new Vector2(0.96f, 0.62f);
            coinValueText.rectTransform.offsetMin = coinValueText.rectTransform.offsetMax = Vector2.zero;

            Text livesLabel = UIManager.CreateText("LivesLbl", statsCard.transform, font, 10, TextAnchor.MiddleLeft, UIStyle.TextDim);
            livesLabel.text = "Lives";
            livesLabel.rectTransform.anchorMin = new Vector2(0.04f, 0.06f);
            livesLabel.rectTransform.anchorMax = new Vector2(0.45f, 0.32f);
            livesLabel.rectTransform.offsetMin = livesLabel.rectTransform.offsetMax = Vector2.zero;

            livesValueText = UIManager.CreateText("LivesVal", statsCard.transform, font, 11, TextAnchor.MiddleRight, UIStyle.TextPrimary);
            livesValueText.fontStyle = FontStyle.Bold;
            livesValueText.rectTransform.anchorMin = new Vector2(0.50f, 0.06f);
            livesValueText.rectTransform.anchorMax = new Vector2(0.96f, 0.32f);
            livesValueText.rectTransform.offsetMin = livesValueText.rectTransform.offsetMax = Vector2.zero;

            nextLifeValueText = UIManager.CreateText("NextLifeVal", transform, font, 11, TextAnchor.MiddleCenter, UIStyle.TextFaint);
            nextLifeValueText.rectTransform.anchorMin = new Vector2(0.12f, 0.47f);
            nextLifeValueText.rectTransform.anchorMax = new Vector2(0.88f, 0.51f);
            nextLifeValueText.rectTransform.offsetMin = nextLifeValueText.rectTransform.offsetMax = Vector2.zero;

            GameObject continueObject = new("ContinueBtn");
            continueObject.transform.SetParent(transform, false);
            RectTransform continueRt = continueObject.AddComponent<RectTransform>();
            continueRt.anchorMin = new Vector2(0.12f, 0.36f);
            continueRt.anchorMax = new Vector2(0.88f, 0.46f);
            continueRt.offsetMin = continueRt.offsetMax = Vector2.zero;
            GradientImage continueGradient = continueObject.AddComponent<GradientImage>();
            continueGradient.colorBottom = UIStyle.Action;
            continueGradient.colorTop = UIStyle.ActionLight;
            continueButton = continueObject.AddComponent<Button>();
            UIManager.BindButton(continueButton, HandleContinuePressed, buttonClickSound);
            continueLabel = UIManager.CreateText("Label", continueObject.transform, font, 14, TextAnchor.MiddleCenter, Color.white);
            continueLabel.fontStyle = FontStyle.Bold;
            continueLabel.text = "CONTINUE (2 ADS)";
            UIManager.Stretch(continueLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject retryObject = new("RetryWrapper");
            retryObject.transform.SetParent(transform, false);
            retryWrapper = retryObject.AddComponent<RectTransform>();
            retryWrapper.anchorMin = new Vector2(0.20f, 0.28f);
            retryWrapper.anchorMax = new Vector2(0.80f, 0.34f);
            retryWrapper.offsetMin = retryWrapper.offsetMax = Vector2.zero;
            Button retryButton = retryObject.AddComponent<Button>();
            UIManager.BindButton(retryButton, HandleRetryPressed, buttonClickSound);
            Text retryLabel = UIManager.CreateText("RetryLabel", retryObject.transform, font, 11, TextAnchor.MiddleCenter, UIStyle.TextFaint);
            retryLabel.text = "RETRY (-1 LIFE)";
            UIManager.Stretch(retryLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            retryWrapper.gameObject.SetActive(false);

            GameObject watchAdObject = new("WatchAdRetryBtn");
            watchAdObject.transform.SetParent(transform, false);
            RectTransform watchRt = watchAdObject.AddComponent<RectTransform>();
            watchRt.anchorMin = new Vector2(0.15f, 0.20f);
            watchRt.anchorMax = new Vector2(0.85f, 0.28f);
            watchRt.offsetMin = watchRt.offsetMax = Vector2.zero;
            Image watchBg = watchAdObject.AddComponent<Image>();
            watchBg.color = new Color(0.20f, 0.66f, 1f, 0.24f);
            watchAdRetryButton = watchAdObject.AddComponent<Button>();
            watchAdRetryButton.targetGraphic = watchBg;
            UIManager.BindButton(watchAdRetryButton, HandleWatchAdRetryPressed, buttonClickSound);
            watchAdRetryLabel = UIManager.CreateText("Label", watchAdObject.transform, font, 12, TextAnchor.MiddleCenter, UIStyle.TextPrimary);
            watchAdRetryLabel.text = "WATCH AD TO RETRY";
            watchAdRetryLabel.fontStyle = FontStyle.Bold;
            UIManager.Stretch(watchAdRetryLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            watchAdObject.SetActive(false);

            Button menuButton = UIManager.CreateButton("MenuBtn", transform, font, "MAIN MENU",
                new Color(1f, 1f, 1f, 0.05f), UIStyle.TextDim);
            UIManager.Stretch((RectTransform)menuButton.transform,
                new Vector2(0.25f, 0.10f), new Vector2(0.75f, 0.16f), Vector2.zero, Vector2.zero);
            UIManager.StyleButtonLabel(menuButton, 11, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            UIManager.BindButton(menuButton, () => onReturnToMenu?.Invoke(), buttonClickSound);
        }

        private void OnEnable()
        {
            if (lifeTimerRoutine != null)
            {
                StopCoroutine(lifeTimerRoutine);
            }

            lifeTimerRoutine = StartCoroutine(UpdateLifeTimerRoutine());
        }

        private void HandleContinuePressed()
        {
            if (continueButton != null)
            {
                StartCoroutine(UIStyle.ButtonPress(continueButton.GetComponent<RectTransform>()));
            }

            continueRunAction?.Invoke();
        }

        private void HandleRetryPressed()
        {
            retryRunAction?.Invoke();
        }

        private void HandleWatchAdRetryPressed()
        {
            watchLifeRefillAdAction?.Invoke();
        }

        public void SetState(float score, float bestScore, float runTime,
            IReadOnlyList<LeaderboardEntry> leaderboardEntries,
            int emberBalance, int rewardValue, int claimedReward,
            bool canContinue, bool canClaimDoubleReward,
            string bestDeltaText, string nextTarget, string modeSummaryText,
            int remainingLives, bool canWatchLifeRefillAd, bool canBuyLifeRefill,
            int lifeRefillCoinCost, bool hasContinueOption, int continueCoinCost)
        {
            if (scoreValueText != null)
            {
                scoreValueText.text = $"{score:0}m";
                StartCoroutine(UIStyle.ScorePop(scoreRt));
            }

            if (bestScoreValue != null)
            {
                bestScoreValue.text = $"{bestScore:0}m";
            }

            if (coinValueText != null)
            {
                int reward = claimedReward > 0 ? claimedReward : rewardValue;
                coinValueText.text = reward > 0 ? $"+{reward}" : $"{emberBalance}";
            }

            if (livesValueText != null)
            {
                livesValueText.text = $"{remainingLives}/{EconomyManager.MaxLifeCount}";
            }

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(canContinue);
            }

            if (continueLabel != null)
            {
                continueLabel.text = "CONTINUE (2 ADS)";
            }

            bool hasLives = remainingLives > 0;
            if (retryWrapper != null)
            {
                retryWrapper.gameObject.SetActive(hasLives);
                Button retryButton = retryWrapper.GetComponent<Button>();
                if (retryButton != null)
                {
                    retryButton.interactable = hasLives;
                }
            }

            if (watchAdRetryButton != null)
            {
                watchAdRetryButton.gameObject.SetActive(!hasLives);
                watchAdRetryButton.interactable = !hasLives && canWatchLifeRefillAd;
            }

            if (watchAdRetryLabel != null)
            {
                watchAdRetryLabel.text = canWatchLifeRefillAd ? "WATCH AD TO RETRY" : "AD NOT READY";
            }

            UpdateNextLifeTimerLabel();

            if (bgPulseOverlay != null)
            {
                StartCoroutine(UIStyle.BackgroundPulse(bgPulseOverlay,
                    new Color(UIStyle.Danger.r, UIStyle.Danger.g, UIStyle.Danger.b, 0.08f)));
            }
        }

        private IEnumerator UpdateLifeTimerRoutine()
        {
            WaitForSecondsRealtime wait = new(1f);
            while (true)
            {
                UpdateNextLifeTimerLabel();
                yield return wait;
            }
        }

        private void UpdateNextLifeTimerLabel()
        {
            if (nextLifeValueText == null || economyManager == null)
            {
                return;
            }

            TimeSpan remaining = economyManager.GetTimeUntilNextLife();
            if (remaining <= TimeSpan.Zero || economyManager.RemainingLives >= EconomyManager.MaxLifeCount)
            {
                nextLifeValueText.text = "NEXT LIFE: FULL";
                return;
            }

            nextLifeValueText.text = $"NEXT LIFE: {(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        }

        private void OnDisable()
        {
            if (lifeTimerRoutine != null)
            {
                StopCoroutine(lifeTimerRoutine);
                lifeTimerRoutine = null;
            }

            if (bgPulseOverlay != null)
            {
                bgPulseOverlay.color = new Color(UIStyle.Danger.r, UIStyle.Danger.g, UIStyle.Danger.b, 0f);
            }
        }
    }
}
