using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerMaze
{
    public sealed class GameManager : MonoBehaviour
    {
        [Header("Managers")]
        [SerializeField] private LifeManager lifeManager;
        [SerializeField] private AdManager adManager;
        [SerializeField] private LoadingManager loadingManager;
        [SerializeField] private DeathScreenUI deathScreenUI;

        [Header("Gameplay References")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Behaviour[] pauseOnDeathBehaviours;

        [Header("Flow")]
        [SerializeField] private bool pauseGameUsingTimeScale = true;
        [SerializeField, Min(1)] private int continueAdsRequired = 2;
        [SerializeField] private bool playFixedLoadingOnStart;
        [SerializeField] private string retrySceneOverride;

        private bool inDeathState;
        private bool actionInProgress;
        private bool hasCheckpoint;
        private Vector3 checkpointPosition;
        private Quaternion checkpointRotation = Quaternion.identity;
        private bool hasDeathPoint;
        private Vector3 deathPosition;
        private Quaternion deathRotation = Quaternion.identity;
        private float cachedTimeScaleBeforePause = 1f;

        public int ContinueAdsRequired => continueAdsRequired;

        private void Awake()
        {
            lifeManager ??= LifeManager.Instance != null ? LifeManager.Instance : FindObjectOfType<LifeManager>();
            adManager ??= FindObjectOfType<AdManager>();
            loadingManager ??= FindObjectOfType<LoadingManager>(true);
            deathScreenUI ??= FindObjectOfType<DeathScreenUI>(true);
        }

        private void Start()
        {
            if (deathScreenUI != null)
            {
                deathScreenUI.Initialize(this, lifeManager);
                deathScreenUI.Hide();
            }

            if (playFixedLoadingOnStart && loadingManager != null)
            {
                SetGameplayEnabled(false);
                loadingManager.BeginFixedLoading(() => SetGameplayEnabled(true));
            }
        }

        private void OnDisable()
        {
            if (pauseGameUsingTimeScale && Time.timeScale <= 0f)
            {
                Time.timeScale = Mathf.Max(0.01f, cachedTimeScaleBeforePause);
            }
        }

        public void SetPlayerTransform(Transform player)
        {
            playerTransform = player;
        }

        public void RegisterCheckpoint(Transform checkpoint)
        {
            if (checkpoint == null)
            {
                return;
            }

            RegisterCheckpoint(checkpoint.position, checkpoint.rotation);
        }

        public void RegisterCheckpoint(Vector3 position, Quaternion rotation)
        {
            hasCheckpoint = true;
            checkpointPosition = position;
            checkpointRotation = rotation;
        }

        public void ClearCheckpoint()
        {
            hasCheckpoint = false;
        }

        public void PlayerDied()
        {
            if (playerTransform != null)
            {
                PlayerDied(playerTransform.position, playerTransform.rotation);
                return;
            }

            PlayerDied(Vector3.zero, Quaternion.identity);
        }

        public void PlayerDied(Vector3 position, Quaternion rotation)
        {
            if (inDeathState)
            {
                return;
            }

            inDeathState = true;
            actionInProgress = false;
            hasDeathPoint = true;
            deathPosition = position;
            deathRotation = rotation;

            PauseGameplayForDeath();
            deathScreenUI?.SetBusy(false);
            deathScreenUI?.Show();
        }

        public void HandleRetryPressed()
        {
            if (!inDeathState || actionInProgress)
            {
                return;
            }

            if (lifeManager == null)
            {
                RetryCurrentLevel();
                return;
            }

            if (lifeManager.TryConsumeLife())
            {
                RetryCurrentLevel();
            }
            else
            {
                HandleWatchAdRetryPressed();
            }
        }

        public void HandleWatchAdRetryPressed()
        {
            if (!inDeathState || actionInProgress)
            {
                return;
            }

            if (lifeManager != null && lifeManager.HasLives)
            {
                HandleRetryPressed();
                return;
            }

            if (adManager == null || !adManager.CanShowRewardedAd)
            {
                return;
            }

            actionInProgress = true;
            deathScreenUI?.SetBusy(true);

            adManager.ShowRewardedAd(RewardedAdRequestType.Retry, success =>
            {
                actionInProgress = false;
                deathScreenUI?.SetBusy(false);

                if (!success)
                {
                    deathScreenUI?.Refresh();
                    return;
                }

                RetryCurrentLevel();
            });
        }

        public void HandleContinuePressed()
        {
            if (!inDeathState || actionInProgress)
            {
                return;
            }

            if (adManager == null || !adManager.CanShowRewardedAd)
            {
                return;
            }

            actionInProgress = true;
            deathScreenUI?.SetBusy(true);

            adManager.ShowRewardedAds(continueAdsRequired, RewardedAdRequestType.Continue, success =>
            {
                actionInProgress = false;
                deathScreenUI?.SetBusy(false);

                if (!success)
                {
                    deathScreenUI?.Refresh();
                    return;
                }

                ContinueAfterDeath();
            });
        }

        private void ContinueAfterDeath()
        {
            inDeathState = false;
            actionInProgress = false;
            RespawnAtContinuePoint();
            ResumeGameplayFromDeath();
            deathScreenUI?.Hide();
        }

        private void RetryCurrentLevel()
        {
            inDeathState = false;
            actionInProgress = false;
            ResumeGameplayFromDeath();
            deathScreenUI?.Hide();

            string sceneName = string.IsNullOrWhiteSpace(retrySceneOverride)
                ? SceneManager.GetActiveScene().name
                : retrySceneOverride;

            if (loadingManager != null)
            {
                loadingManager.BeginFixedLoadingAndLoadScene(sceneName);
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        private void PauseGameplayForDeath()
        {
            SetGameplayEnabled(false);

            if (!pauseGameUsingTimeScale)
            {
                return;
            }

            cachedTimeScaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
        }

        private void ResumeGameplayFromDeath()
        {
            if (pauseGameUsingTimeScale)
            {
                Time.timeScale = Mathf.Max(0.01f, cachedTimeScaleBeforePause);
            }

            SetGameplayEnabled(true);
        }

        private void SetGameplayEnabled(bool enabled)
        {
            if (pauseOnDeathBehaviours == null)
            {
                return;
            }

            for (int index = 0; index < pauseOnDeathBehaviours.Length; index++)
            {
                if (pauseOnDeathBehaviours[index] != null)
                {
                    pauseOnDeathBehaviours[index].enabled = enabled;
                }
            }
        }

        private void RespawnAtContinuePoint()
        {
            if (playerTransform == null)
            {
                return;
            }

            Vector3 spawnPosition = hasCheckpoint ? checkpointPosition : deathPosition;
            Quaternion spawnRotation = hasCheckpoint ? checkpointRotation : deathRotation;
            if (!hasCheckpoint && !hasDeathPoint)
            {
                spawnPosition = playerTransform.position;
                spawnRotation = playerTransform.rotation;
            }

            playerTransform.SetPositionAndRotation(spawnPosition, spawnRotation);

            Rigidbody body = playerTransform.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }
    }
}
