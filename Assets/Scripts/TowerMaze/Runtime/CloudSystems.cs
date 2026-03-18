using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace TowerMaze
{
    [Serializable]
    internal sealed class EconomyCloudSaveData
    {
        public int emberBalance;
        public int remainingLives;
        public long lifeRechargeStartTicks;
        public string equippedSkinId = string.Empty;
        public string equippedTowerSkinId = string.Empty;
        public List<string> ownedSkinIds = new();
        public List<string> ownedTowerSkinIds = new();
        public DailyMissionSaveData dailyMissionState = new();
        public DailyChallengeSaveData dailyChallengeState = new();
    }

    [Serializable]
    internal sealed class ScoreCloudSaveData
    {
        public float bestScore;
        public List<LeaderboardEntry> leaderboardEntries = new();
    }

    [Serializable]
    internal sealed class CoinStoreCloudSaveData
    {
        public List<string> ownedOfferIds = new();
    }

    [Serializable]
    internal sealed class TowerMazeCloudSaveData
    {
        public long updatedAtUtcTicks;
        public EconomyCloudSaveData economy = new();
        public ScoreCloudSaveData score = new();
        public CoinStoreCloudSaveData store = new();
    }

    public sealed class PlayFabCloudManager : MonoBehaviour
    {
        private const string LocalPayloadTicksKey = "TowerMaze.Cloud.LastPayloadTicks";

        [SerializeField] private bool autoSyncOnStateChange = true;
        [SerializeField] private bool verboseLogging;

        private GameConfig config;
        private EconomyManager economyManager;
        private ScoreManager scoreManager;
        private CoinStoreManager coinStoreManager;
        private UIManager uiManager;
        private string sessionTicket = string.Empty;
        private string playFabId = string.Empty;
        private bool initialized;
        private bool loggedIn;
        private bool syncInProgress;
        private bool suppressLocalChanges;
        private Coroutine queuedSyncCoroutine;

        public bool IsEnabled => config != null && config.enablePlayFabCloudSync && !string.IsNullOrWhiteSpace(config.playFabTitleId);
        public bool IsLoggedIn => loggedIn;

        public void Initialize(GameConfig gameConfig, EconomyManager economy, ScoreManager score, CoinStoreManager coinStore, UIManager ui)
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            config = gameConfig;
            economyManager = economy;
            scoreManager = score;
            coinStoreManager = coinStore;
            uiManager = ui;

            if (economyManager != null)
            {
                economyManager.StateChanged += HandleLocalStateChanged;
            }

            if (scoreManager != null)
            {
                scoreManager.StateChanged += HandleLocalStateChanged;
            }

            if (coinStoreManager != null)
            {
                coinStoreManager.StateChanged += HandleLocalStateChanged;
            }

            if (!IsEnabled)
            {
                if (verboseLogging)
                {
                    Debug.Log("PlayFab cloud sync disabled; using local save only.");
                }

                return;
            }

            StartCoroutine(LoginAndSyncRoutine());
        }

        private void OnDestroy()
        {
            if (economyManager != null)
            {
                economyManager.StateChanged -= HandleLocalStateChanged;
            }

            if (scoreManager != null)
            {
                scoreManager.StateChanged -= HandleLocalStateChanged;
            }

            if (coinStoreManager != null)
            {
                coinStoreManager.StateChanged -= HandleLocalStateChanged;
            }
        }

        private void HandleLocalStateChanged()
        {
            if (suppressLocalChanges || !IsEnabled)
            {
                return;
            }

            SetLocalPayloadTicks(DateTime.UtcNow.Ticks);
            if (!autoSyncOnStateChange || !loggedIn)
            {
                return;
            }

            QueueSync(1.25f);
        }

        private void QueueSync(float delaySeconds)
        {
            if (queuedSyncCoroutine != null)
            {
                StopCoroutine(queuedSyncCoroutine);
            }

            queuedSyncCoroutine = StartCoroutine(DelayedSyncRoutine(delaySeconds));
        }

        private IEnumerator DelayedSyncRoutine(float delaySeconds)
        {
            yield return new WaitForSecondsRealtime(delaySeconds);
            queuedSyncCoroutine = null;
            yield return PushLocalStateRoutine(true, true);
        }

        private IEnumerator LoginAndSyncRoutine()
        {
            string customId = ResolveCustomId();
            JObject response = null;
            string failureMessage = string.Empty;
            yield return SendPlayFabRequest(
                "Client/LoginWithCustomID",
                new
                {
                    TitleId = config.playFabTitleId,
                    CustomId = customId,
                    CreateAccount = true
                },
                null,
                root => response = root,
                error => failureMessage = error);

            if (response == null)
            {
                Debug.LogWarning($"PlayFab login skipped: {failureMessage}");
                yield break;
            }

            JToken data = response["data"];
            sessionTicket = data?["SessionTicket"]?.Value<string>() ?? string.Empty;
            playFabId = data?["PlayFabId"]?.Value<string>() ?? string.Empty;
            loggedIn = !string.IsNullOrWhiteSpace(sessionTicket);
            if (!loggedIn)
            {
                Debug.LogWarning("PlayFab login failed: missing session ticket.");
                yield break;
            }

            if (verboseLogging)
            {
                Debug.Log($"PlayFab login complete for {playFabId}.");
            }

            yield return InitialSyncRoutine();
        }

        private IEnumerator InitialSyncRoutine()
        {
            TowerMazeCloudSaveData cloudPayload = null;
            yield return FetchCloudSaveRoutine(payload => cloudPayload = payload);

            long localTicks = GetLocalPayloadTicks();
            if (cloudPayload != null && cloudPayload.updatedAtUtcTicks > localTicks)
            {
                suppressLocalChanges = true;
                try
                {
                    ImportCloudPayload(cloudPayload);
                }
                finally
                {
                    suppressLocalChanges = false;
                }

                SetLocalPayloadTicks(cloudPayload.updatedAtUtcTicks);
            }
            else
            {
                if (localTicks <= 0L)
                {
                    SetLocalPayloadTicks(DateTime.UtcNow.Ticks);
                }

                yield return PushLocalStateRoutine(true, true);
            }

            yield return RefreshLeaderboardRoutine();
        }

        private IEnumerator FetchCloudSaveRoutine(Action<TowerMazeCloudSaveData> onComplete)
        {
            JObject response = null;
            yield return SendPlayFabRequest(
                "Client/GetUserData",
                new
                {
                    Keys = new[] { ResolveSaveKey() }
                },
                sessionTicket,
                root => response = root,
                error =>
                {
                    if (verboseLogging)
                    {
                        Debug.LogWarning($"PlayFab cloud fetch failed: {error}");
                    }
                });

            if (response == null)
            {
                onComplete?.Invoke(null);
                yield break;
            }

            string rawValue = response["data"]?["Data"]?[ResolveSaveKey()]?["Value"]?.Value<string>();
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                onComplete?.Invoke(null);
                yield break;
            }

            TowerMazeCloudSaveData payload = null;
            try
            {
                payload = JsonConvert.DeserializeObject<TowerMazeCloudSaveData>(rawValue);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"PlayFab cloud payload parse failed: {exception.Message}");
            }

            onComplete?.Invoke(payload);
        }

        private IEnumerator PushLocalStateRoutine(bool pushSaveData, bool pushLeaderboard)
        {
            if (!loggedIn || syncInProgress)
            {
                yield break;
            }

            syncInProgress = true;
            try
            {
                if (pushSaveData)
                {
                    TowerMazeCloudSaveData payload = BuildLocalPayload();
                    SetLocalPayloadTicks(payload.updatedAtUtcTicks);
                    string payloadJson = JsonConvert.SerializeObject(payload);
                    yield return SendPlayFabRequest(
                        "Client/UpdateUserData",
                        new
                        {
                            Data = new Dictionary<string, string>
                            {
                                { ResolveSaveKey(), payloadJson }
                            },
                            Permission = "Private"
                        },
                        sessionTicket,
                        null,
                        error => Debug.LogWarning($"PlayFab cloud save failed: {error}"));
                }

                if (pushLeaderboard)
                {
                    int bestScoreValue = Mathf.Max(0, Mathf.RoundToInt(scoreManager != null ? scoreManager.PersistedBestScore * 100f : 0f));
                    if (bestScoreValue > 0)
                    {
                        yield return SendPlayFabRequest(
                            "Client/UpdatePlayerStatistics",
                            new
                            {
                                Statistics = new[]
                                {
                                    new
                                    {
                                        StatisticName = ResolveStatisticName(),
                                        Value = bestScoreValue
                                    }
                                }
                            },
                            sessionTicket,
                            null,
                            error => Debug.LogWarning($"PlayFab leaderboard submit failed: {error}"));
                    }

                    yield return RefreshLeaderboardRoutine();
                }
            }
            finally
            {
                syncInProgress = false;
            }
        }

        private IEnumerator RefreshLeaderboardRoutine()
        {
            JObject response = null;
            yield return SendPlayFabRequest(
                "Client/GetLeaderboard",
                new
                {
                    StatisticName = ResolveStatisticName(),
                    StartPosition = 0,
                    MaxResultsCount = Mathf.Clamp(config != null ? config.playFabLeaderboardSize : 5, 3, 20)
                },
                sessionTicket,
                root => response = root,
                error =>
                {
                    if (verboseLogging)
                    {
                        Debug.LogWarning($"PlayFab leaderboard fetch failed: {error}");
                    }
                });

            if (response == null || scoreManager == null)
            {
                yield break;
            }

            List<LeaderboardEntry> entries = new();
            JArray leaderboardItems = response["data"]?["Leaderboard"] as JArray;
            if (leaderboardItems != null)
            {
                for (int index = 0; index < leaderboardItems.Count; index++)
                {
                    JToken item = leaderboardItems[index];
                    int rank = item["Position"]?.Value<int>() ?? index;
                    int statValue = item["StatValue"]?.Value<int>() ?? 0;
                    string displayName = item["DisplayName"]?.Value<string>();
                    string label = BuildLeaderboardLabel(displayName, rank);
                    entries.Add(new LeaderboardEntry(statValue / 100f, 0f, label));
                }
            }

            if (entries.Count > 0)
            {
                scoreManager.SetCloudLeaderboardEntries(entries);
                uiManager?.UpdateCachedLeaderboard(scoreManager.BestScore, scoreManager.LeaderboardEntries);
            }
        }

        private TowerMazeCloudSaveData BuildLocalPayload()
        {
            long ticks = Math.Max(DateTime.UtcNow.Ticks, GetLocalPayloadTicks());
            return new TowerMazeCloudSaveData
            {
                updatedAtUtcTicks = ticks,
                economy = economyManager != null ? economyManager.ExportCloudData() : new EconomyCloudSaveData(),
                score = scoreManager != null ? scoreManager.ExportCloudData() : new ScoreCloudSaveData(),
                store = coinStoreManager != null ? coinStoreManager.ExportCloudData() : new CoinStoreCloudSaveData()
            };
        }

        private void ImportCloudPayload(TowerMazeCloudSaveData payload)
        {
            if (payload == null)
            {
                return;
            }

            economyManager?.ImportCloudData(payload.economy);
            scoreManager?.ImportCloudData(payload.score);
            coinStoreManager?.ImportCloudData(payload.store);
            uiManager?.UpdateCachedLeaderboard(scoreManager != null ? scoreManager.BestScore : 0f, scoreManager != null ? scoreManager.LeaderboardEntries : Array.Empty<LeaderboardEntry>());
        }

        private string ResolveCustomId()
        {
            if (config != null && !string.IsNullOrWhiteSpace(config.playFabCustomIdOverride))
            {
                return config.playFabCustomIdOverride.Trim();
            }

            string raw = $"{Application.identifier}_{SystemInfo.deviceUniqueIdentifier}";
            if (string.IsNullOrWhiteSpace(raw))
            {
                raw = $"{Application.identifier}_editor";
            }

            return raw.Replace(" ", "_");
        }

        private string ResolveSaveKey()
        {
            return config != null && !string.IsNullOrWhiteSpace(config.playFabSaveKey)
                ? config.playFabSaveKey.Trim()
                : "towermaze_save";
        }

        private string ResolveStatisticName()
        {
            return config != null && !string.IsNullOrWhiteSpace(config.playFabStatisticName)
                ? config.playFabStatisticName.Trim()
                : "best_height_cm";
        }

        private static string BuildLeaderboardLabel(string displayName, int rank)
        {
            string label = string.IsNullOrWhiteSpace(displayName) ? $"P{rank + 1}" : displayName.Trim();
            if (label.Length > 9)
            {
                label = label.Substring(0, 9);
            }

            return label.ToUpperInvariant();
        }

        private static long GetLocalPayloadTicks()
        {
            return long.TryParse(PlayerPrefs.GetString(LocalPayloadTicksKey, "0"), out long parsedTicks) ? parsedTicks : 0L;
        }

        private static void SetLocalPayloadTicks(long ticks)
        {
            PlayerPrefs.SetString(LocalPayloadTicksKey, ticks.ToString());
            PlayerPrefs.Save();
        }

        private IEnumerator SendPlayFabRequest(string path, object requestBody, string authToken, Action<JObject> onSuccess, Action<string> onFailure)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.playFabTitleId))
            {
                onFailure?.Invoke("TITLE ID MISSING");
                yield break;
            }

            string url = $"https://{config.playFabTitleId}.playfabapi.com/{path}";
            byte[] bodyBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(requestBody ?? new { }));
            using UnityWebRequest request = new(url, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrWhiteSpace(authToken))
            {
                request.SetRequestHeader("X-Authorization", authToken);
            }

            yield return request.SendWebRequest();

            string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            if (request.result != UnityWebRequest.Result.Success)
            {
                onFailure?.Invoke(string.IsNullOrWhiteSpace(responseText) ? request.error : responseText);
                yield break;
            }

            JObject root = null;
            try
            {
                root = string.IsNullOrWhiteSpace(responseText) ? new JObject() : JObject.Parse(responseText);
            }
            catch (Exception exception)
            {
                onFailure?.Invoke($"INVALID PLAYFAB RESPONSE: {exception.Message}");
                yield break;
            }

            if (request.responseCode >= 400 || root["error"] != null)
            {
                string errorMessage = root["errorMessage"]?.Value<string>() ?? root["error"]?.Value<string>() ?? $"HTTP {request.responseCode}";
                onFailure?.Invoke(errorMessage);
                yield break;
            }

            onSuccess?.Invoke(root);
        }
    }
}
