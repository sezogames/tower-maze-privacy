using System;
using UnityEngine;
using UnityEngine.UI;

namespace TowerMaze
{
    public sealed class DeathScreenUI : MonoBehaviour
    {
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private Text livesText;
        [SerializeField] private Text nextLifeCountdownText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text continueRequirementText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button watchAdRetryButton;

        private GameManager gameManager;
        private LifeManager lifeManager;
        private bool busy;
        private bool subscribed;

        private void Awake()
        {
            if (rootPanel == null)
            {
                rootPanel = gameObject;
            }

            if (retryButton != null)
            {
                retryButton.onClick.AddListener(HandleRetryPressed);
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(HandleContinuePressed);
            }

            if (watchAdRetryButton != null)
            {
                watchAdRetryButton.onClick.AddListener(HandleWatchAdRetryPressed);
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        public void Initialize(GameManager manager, LifeManager livesManager)
        {
            gameManager = manager;
            lifeManager = livesManager;
            Subscribe();
            Refresh();
        }

        public void Show()
        {
            if (rootPanel != null)
            {
                rootPanel.SetActive(true);
            }

            Refresh();
        }

        public void Hide()
        {
            if (rootPanel != null)
            {
                rootPanel.SetActive(false);
            }
        }

        public void SetBusy(bool value)
        {
            busy = value;
            RefreshButtons();
        }

        public void Refresh()
        {
            int currentLives = lifeManager != null ? lifeManager.CurrentLives : 0;
            int maxLives = LifeManager.MaxLives;
            TimeSpan nextLife = lifeManager != null ? lifeManager.GetTimeUntilNextLife() : TimeSpan.Zero;

            if (livesText != null)
            {
                livesText.text = $"Lives: {currentLives}/{maxLives}";
            }

            if (nextLifeCountdownText != null)
            {
                if (currentLives >= maxLives)
                {
                    nextLifeCountdownText.text = "Next Life: FULL";
                }
                else
                {
                    nextLifeCountdownText.text = $"Next Life: {FormatCountdown(nextLife)}";
                }
            }

            if (continueRequirementText != null)
            {
                int requiredAds = gameManager != null ? gameManager.ContinueAdsRequired : 2;
                continueRequirementText.text = $"Continue requires {requiredAds} rewarded ads";
            }

            if (statusText != null)
            {
                statusText.text = currentLives > 0
                    ? "Retry costs 1 life."
                    : "No lives left. Watch 1 rewarded ad to retry.";
            }

            RefreshButtons();
        }

        private void RefreshButtons()
        {
            int currentLives = lifeManager != null ? lifeManager.CurrentLives : 0;
            bool hasLives = currentLives > 0;

            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(hasLives);
                retryButton.interactable = hasLives && !busy;
            }

            if (watchAdRetryButton != null)
            {
                watchAdRetryButton.gameObject.SetActive(!hasLives);
                watchAdRetryButton.interactable = !hasLives && !busy;
            }

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
                continueButton.interactable = !busy;
            }
        }

        private void HandleRetryPressed()
        {
            gameManager?.HandleRetryPressed();
        }

        private void HandleContinuePressed()
        {
            gameManager?.HandleContinuePressed();
        }

        private void HandleWatchAdRetryPressed()
        {
            gameManager?.HandleWatchAdRetryPressed();
        }

        private void Subscribe()
        {
            if (lifeManager == null || subscribed)
            {
                return;
            }

            lifeManager.LivesChanged += OnLivesChanged;
            lifeManager.NextLifeTimerChanged += OnNextLifeTimerChanged;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (lifeManager == null || !subscribed)
            {
                return;
            }

            lifeManager.LivesChanged -= OnLivesChanged;
            lifeManager.NextLifeTimerChanged -= OnNextLifeTimerChanged;
            subscribed = false;
        }

        private void OnLivesChanged(int current, int max)
        {
            Refresh();
        }

        private void OnNextLifeTimerChanged(TimeSpan remaining)
        {
            if (nextLifeCountdownText == null)
            {
                return;
            }

            int currentLives = lifeManager != null ? lifeManager.CurrentLives : 0;
            if (currentLives >= LifeManager.MaxLives)
            {
                nextLifeCountdownText.text = "Next Life: FULL";
            }
            else
            {
                nextLifeCountdownText.text = $"Next Life: {FormatCountdown(remaining)}";
            }
        }

        private static string FormatCountdown(TimeSpan value)
        {
            if (value <= TimeSpan.Zero)
            {
                return "00:00:00";
            }

            return $"{(int)value.TotalHours:00}:{value.Minutes:00}:{value.Seconds:00}";
        }
    }
}
