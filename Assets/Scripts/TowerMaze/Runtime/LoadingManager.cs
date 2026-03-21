using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TowerMaze
{
    public sealed class LoadingManager : MonoBehaviour
    {
        private const float FixedDurationSeconds = 10f;

        [SerializeField] private GameObject loadingRoot;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Text progressLabel;
        [SerializeField, Min(FixedDurationSeconds)] private float fixedLoadingDurationSeconds = FixedDurationSeconds;
        [SerializeField] private bool playOnStart;
        [SerializeField] private string sceneToLoadOnStart;

        private Coroutine activeLoadingRoutine;

        public bool IsLoading => activeLoadingRoutine != null;

        public event Action FixedLoadingCompleted;

        private void Awake()
        {
            if (loadingRoot != null)
            {
                loadingRoot.SetActive(false);
            }
            SetProgress(0f);
        }

        private void Start()
        {
            if (playOnStart)
            {
                if (string.IsNullOrWhiteSpace(sceneToLoadOnStart))
                {
                    BeginFixedLoading(null);
                }
                else
                {
                    BeginFixedLoadingAndLoadScene(sceneToLoadOnStart);
                }
            }
        }

        public void BeginFixedLoading(Action onCompleted)
        {
            RestartRoutine(RunFixedLoadingRoutine(onCompleted));
        }

        public void BeginFixedLoadingAndLoadScene(string sceneName)
        {
            BeginFixedLoading(() =>
            {
                if (!string.IsNullOrWhiteSpace(sceneName))
                {
                    SceneManager.LoadScene(sceneName);
                }
            });
        }

        private void RestartRoutine(IEnumerator routine)
        {
            if (activeLoadingRoutine != null)
            {
                StopCoroutine(activeLoadingRoutine);
            }

            activeLoadingRoutine = StartCoroutine(routine);
        }

        private IEnumerator RunFixedLoadingRoutine(Action onCompleted)
        {
            SetLoadingVisible(true);
            SetProgress(0f);

            fixedLoadingDurationSeconds = FixedDurationSeconds;
            float elapsed = 0f;
            while (elapsed < fixedLoadingDurationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / fixedLoadingDurationSeconds);
                SetProgress(progress);
                yield return null;
            }

            SetProgress(1f);
            FixedLoadingCompleted?.Invoke();
            SetLoadingVisible(false);
            onCompleted?.Invoke();

            activeLoadingRoutine = null;
        }

        private void SetProgress(float normalizedProgress)
        {
            float clamped = Mathf.Clamp01(normalizedProgress);
            if (progressBar != null)
            {
                progressBar.value = clamped;
            }

            if (progressLabel != null)
            {
                progressLabel.text = $"{Mathf.RoundToInt(clamped * 100f)}%";
            }
        }

        private void SetLoadingVisible(bool visible)
        {
            if (loadingRoot != null)
            {
                loadingRoot.SetActive(visible);
            }
        }
    }
}
