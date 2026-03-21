using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TowerMaze
{
    public sealed class LifeBarUI : MonoBehaviour
    {
        [SerializeField] private Text livesText;
        [SerializeField] private Text timerText;
        [SerializeField, Min(0.25f)] private float refreshIntervalSeconds = 1f;

        private EconomyManager economyManager;
        private LifeManager lifeManager;
        private Coroutine refreshRoutine;

        public void Initialize(Font font)
        {
            Text titleText = UIManager.CreateText("LifeBarTitle", transform, font, 9, TextAnchor.MiddleLeft, UIStyle.TextDim);
            titleText.text = "LIFE BAR";
            titleText.rectTransform.anchorMin = new Vector2(0.06f, 0.66f);
            titleText.rectTransform.anchorMax = new Vector2(0.96f, 0.94f);
            titleText.rectTransform.offsetMin = titleText.rectTransform.offsetMax = Vector2.zero;

            livesText = UIManager.CreateText("LifeBarLives", transform, font, 12, TextAnchor.MiddleLeft, UIStyle.TextPrimary);
            livesText.fontStyle = FontStyle.Bold;
            livesText.rectTransform.anchorMin = new Vector2(0.06f, 0.34f);
            livesText.rectTransform.anchorMax = new Vector2(0.96f, 0.70f);
            livesText.rectTransform.offsetMin = livesText.rectTransform.offsetMax = Vector2.zero;

            timerText = UIManager.CreateText("LifeBarTimer", transform, font, 10, TextAnchor.MiddleLeft, UIStyle.Gold);
            timerText.rectTransform.anchorMin = new Vector2(0.06f, 0.08f);
            timerText.rectTransform.anchorMax = new Vector2(0.96f, 0.36f);
            timerText.rectTransform.offsetMin = timerText.rectTransform.offsetMax = Vector2.zero;

            RefreshView();
        }

        public void SetEconomyManager(EconomyManager manager)
        {
            if (economyManager == manager)
            {
                return;
            }

            if (economyManager != null)
            {
                economyManager.StateChanged -= HandleStateChanged;
            }

            economyManager = manager;
            if (economyManager != null)
            {
                economyManager.StateChanged += HandleStateChanged;
            }

            RefreshView();
        }

        public void SetLifeManager(LifeManager manager)
        {
            if (lifeManager == manager)
            {
                return;
            }

            if (lifeManager != null)
            {
                lifeManager.LivesChanged -= HandleLifeChanged;
                lifeManager.NextLifeTimerChanged -= HandleLifeTimerChanged;
            }

            lifeManager = manager;
            if (lifeManager != null)
            {
                lifeManager.LivesChanged += HandleLifeChanged;
                lifeManager.NextLifeTimerChanged += HandleLifeTimerChanged;
            }

            RefreshView();
        }

        private void OnEnable()
        {
            if (refreshRoutine != null)
            {
                StopCoroutine(refreshRoutine);
            }

            refreshRoutine = StartCoroutine(RefreshLoop());
            RefreshView();
        }

        private void OnDisable()
        {
            if (refreshRoutine != null)
            {
                StopCoroutine(refreshRoutine);
                refreshRoutine = null;
            }
        }

        private void OnDestroy()
        {
            if (economyManager != null)
            {
                economyManager.StateChanged -= HandleStateChanged;
            }

            if (lifeManager != null)
            {
                lifeManager.LivesChanged -= HandleLifeChanged;
                lifeManager.NextLifeTimerChanged -= HandleLifeTimerChanged;
            }
        }

        private IEnumerator RefreshLoop()
        {
            WaitForSecondsRealtime wait = new(Mathf.Max(0.25f, refreshIntervalSeconds));
            while (true)
            {
                RefreshView();
                yield return wait;
            }
        }

        private void HandleStateChanged()
        {
            RefreshView();
        }

        private void HandleLifeChanged(int currentLives, int maxLives)
        {
            RefreshView();
        }

        private void HandleLifeTimerChanged(TimeSpan remaining)
        {
            RefreshView();
        }

        private void RefreshView()
        {
            if (livesText == null || timerText == null)
            {
                return;
            }

            int currentLives;
            int maxLives;
            TimeSpan remaining;

            if (economyManager != null)
            {
                maxLives = EconomyManager.MaxLifeCount;
                currentLives = economyManager.RemainingLives;
                remaining = economyManager.GetTimeUntilNextLife();
            }
            else if (lifeManager != null)
            {
                maxLives = LifeManager.MaxLives;
                currentLives = lifeManager.CurrentLives;
                remaining = lifeManager.GetTimeUntilNextLife();
            }
            else
            {
                maxLives = EconomyManager.MaxLifeCount;
                currentLives = 0;
                remaining = TimeSpan.Zero;
            }

            livesText.text = $"Lives: {currentLives}/{maxLives}";

            if (currentLives >= maxLives)
            {
                timerText.text = "Next life in: Full";
                return;
            }

            timerText.text = $"Next life in: {FormatCountdown(remaining)}";
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
