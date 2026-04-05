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

    public sealed class FirebaseCloudManager : MonoBehaviour
    {
        private const string PrefUid = "TowerMaze.Firebase.Uid";
        private const string PrefRefreshToken = "TowerMaze.Firebase.RefreshToken";
        private const string PrefIdToken = "TowerMaze.Firebase.IdToken";
        private const string PrefNickname = "TowerMaze.Firebase.Nickname";
        private const string LocalPayloadTicksKey = "TowerMaze.Cloud.LastPayloadTicks";

        [SerializeField] private bool autoSyncOnStateChange = true;
        [SerializeField] private bool verboseLogging;

        private GameConfig config;
        private EconomyManager economyManager;
        private ScoreManager scoreManager;
        private CoinStoreManager coinStoreManager;
        private UIManager uiManager;
        private string firebaseApiKey = string.Empty;
        private string firebaseProjectId = string.Empty;
        private string uid = string.Empty;
        private string idToken = string.Empty;
        private string refreshToken = string.Empty;
        private string nickname = string.Empty;
        private bool initialized;
        private bool loggedIn;
        private bool syncInProgress;
        private bool suppressLocalChanges;
        private Coroutine queuedSyncCoroutine;
        private bool nicknamePromptRequestedThisSession;

        public bool IsEnabled => config != null && config.enableFirebaseCloudSync && !string.IsNullOrWhiteSpace(firebaseApiKey);
        public bool IsLoggedIn => loggedIn;
        public event Action NicknameRequired;

        public void Initialize(GameConfig gameConfig, EconomyManager economy, ScoreManager score, CoinStoreManager coinStore, UIManager ui)
        {
            if (initialized) return;
            initialized = true;
            config = gameConfig;
            economyManager = economy;
            scoreManager = score;
            coinStoreManager = coinStore;
            uiManager = ui;

            LoadFirebaseConfig();
            nickname = PlayerPrefs.GetString(PrefNickname, string.Empty);

            if (economyManager != null) economyManager.StateChanged += HandleLocalStateChanged;
            if (scoreManager != null) scoreManager.StateChanged += HandleLocalStateChanged;
            if (coinStoreManager != null) coinStoreManager.StateChanged += HandleLocalStateChanged;

            if (!IsEnabled)
            {
                if (verboseLogging) Debug.Log("Firebase cloud sync disabled; using local save only.");
                return;
            }

            StartCoroutine(LoginAndSyncRoutine());
        }

        private void OnDestroy()
        {
            if (economyManager != null) economyManager.StateChanged -= HandleLocalStateChanged;
            if (scoreManager != null) scoreManager.StateChanged -= HandleLocalStateChanged;
            if (coinStoreManager != null) coinStoreManager.StateChanged -= HandleLocalStateChanged;
        }

        public void SetNickname(string newNickname)
        {
            if (string.IsNullOrWhiteSpace(newNickname)) return;
            nickname = newNickname.Trim().ToUpperInvariant();
            nicknamePromptRequestedThisSession = false;
            PlayerPrefs.SetString(PrefNickname, nickname);
            PlayerPrefs.Save();
            if (loggedIn) QueueSync(0.5f);
        }

        // ─── Firebase Config ─────────────────────────────────────────────────

        private void LoadFirebaseConfig()
        {
            TextAsset json = Resources.Load<TextAsset>("google-services");
            if (json == null)
            {
                Debug.LogWarning("Firebase: google-services.json not found in Resources.");
                return;
            }

            try
            {
                JObject root = JObject.Parse(json.text);
                firebaseProjectId = root["project_info"]?["project_id"]?.Value<string>() ?? string.Empty;
                JArray clients = root["client"] as JArray;
                if (clients != null && clients.Count > 0)
                {
                    JArray apiKeys = clients[0]["api_key"] as JArray;
                    if (apiKeys != null && apiKeys.Count > 0)
                    {
                        firebaseApiKey = apiKeys[0]["current_key"]?.Value<string>() ?? string.Empty;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Firebase: failed to parse google-services.json: {e.Message}");
            }
        }

        // ─── State Change Handling ───────────────────────────────────────────

        private void HandleLocalStateChanged()
        {
            if (suppressLocalChanges || !IsEnabled) return;
            SetLocalPayloadTicks(DateTime.UtcNow.Ticks);
            if (!autoSyncOnStateChange || !loggedIn) return;
            QueueSync(1.25f);
        }

        private void QueueSync(float delaySeconds)
        {
            if (queuedSyncCoroutine != null) StopCoroutine(queuedSyncCoroutine);
            queuedSyncCoroutine = StartCoroutine(DelayedSyncRoutine(delaySeconds));
        }

        private IEnumerator DelayedSyncRoutine(float delaySeconds)
        {
            yield return new WaitForSecondsRealtime(delaySeconds);
            queuedSyncCoroutine = null;
            yield return PushLocalStateRoutine(true, true);
        }

        // ─── Authentication ──────────────────────────────────────────────────

        private IEnumerator LoginAndSyncRoutine()
        {
            uid = PlayerPrefs.GetString(PrefUid, string.Empty);
            refreshToken = PlayerPrefs.GetString(PrefRefreshToken, string.Empty);
            idToken = PlayerPrefs.GetString(PrefIdToken, string.Empty);

            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                bool refreshed = false;
                yield return RefreshIdTokenRoutine(success => refreshed = success);
                if (!refreshed)
                {
                    Debug.LogWarning("Firebase token refresh failed; cloud sync disabled for this session.");
                    yield break;
                }
            }
            else
            {
                string url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={firebaseApiKey}";
                JObject body = new JObject { ["returnSecureToken"] = true };
                JObject response = null;
                yield return SendJsonRequest(url, "POST", body.ToString(), null,
                    root => response = root,
                    error => Debug.LogWarning($"Firebase signUp failed: {error}"));

                if (response == null) yield break;

                uid = response["localId"]?.Value<string>() ?? string.Empty;
                idToken = response["idToken"]?.Value<string>() ?? string.Empty;
                refreshToken = response["refreshToken"]?.Value<string>() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(idToken))
                {
                    Debug.LogWarning("Firebase signUp: missing uid or idToken.");
                    yield break;
                }

                PlayerPrefs.SetString(PrefUid, uid);
                PlayerPrefs.SetString(PrefRefreshToken, refreshToken);
                PlayerPrefs.SetString(PrefIdToken, idToken);
                PlayerPrefs.Save();
            }

            loggedIn = true;
            if (verboseLogging) Debug.Log($"Firebase login complete for {uid}.");
            yield return InitialSyncRoutine();
        }

        private IEnumerator RefreshIdTokenRoutine(Action<bool> onComplete)
        {
            string url = $"https://securetoken.googleapis.com/v1/token?key={firebaseApiKey}";
            string formBody = $"grant_type=refresh_token&refresh_token={UnityWebRequest.EscapeURL(refreshToken)}";

            using UnityWebRequest request = new(url, UnityWebRequest.kHttpVerbPOST);
            byte[] bodyBytes = Encoding.UTF8.GetBytes(formBody);
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(false);
                yield break;
            }

            try
            {
                JObject response = JObject.Parse(request.downloadHandler.text);
                idToken = response["id_token"]?.Value<string>() ?? string.Empty;
                refreshToken = response["refresh_token"]?.Value<string>() ?? string.Empty;
                uid = response["user_id"]?.Value<string>() ?? uid;

                PlayerPrefs.SetString(PrefUid, uid);
                PlayerPrefs.SetString(PrefRefreshToken, refreshToken);
                PlayerPrefs.SetString(PrefIdToken, idToken);
                PlayerPrefs.Save();

                onComplete?.Invoke(!string.IsNullOrWhiteSpace(idToken));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Firebase token refresh parse error: {e.Message}");
                onComplete?.Invoke(false);
            }
        }

        // ─── Initial Sync ────────────────────────────────────────────────────

        private IEnumerator InitialSyncRoutine()
        {
            TowerMazeCloudSaveData cloudPayload = null;
            yield return FetchCloudSaveRoutine(payload => cloudPayload = payload);

            long localTicks = GetLocalPayloadTicks();
            if (cloudPayload != null && cloudPayload.updatedAtUtcTicks > localTicks)
            {
                suppressLocalChanges = true;
                try { ImportCloudPayload(cloudPayload); }
                finally { suppressLocalChanges = false; }
                SetLocalPayloadTicks(cloudPayload.updatedAtUtcTicks);
            }
            else
            {
                if (localTicks <= 0L) SetLocalPayloadTicks(DateTime.UtcNow.Ticks);
                yield return PushLocalStateRoutine(true, true);
            }

            yield return RefreshLeaderboardRoutine();
            RequestNicknameIfNeeded();
        }

        // ─── Cloud Save ──────────────────────────────────────────────────────

        private IEnumerator FetchCloudSaveRoutine(Action<TowerMazeCloudSaveData> onComplete)
        {
            string url = FirestoreDocUrl("users", uid);
            JToken response = null;
            yield return SendFirestoreRequest(url, "GET", null,
                root => response = root,
                error =>
                {
                    if (verboseLogging) Debug.LogWarning($"Firebase cloud fetch failed: {error}");
                });

            if (response == null)
            {
                onComplete?.Invoke(null);
                yield break;
            }

            try
            {
                JToken fields = response["fields"];
                if (fields == null || fields["saveData"] == null)
                {
                    onComplete?.Invoke(null);
                    yield break;
                }

                string saveDataJson = fields["saveData"]?["stringValue"]?.Value<string>();
                if (string.IsNullOrWhiteSpace(saveDataJson))
                {
                    onComplete?.Invoke(null);
                    yield break;
                }

                TowerMazeCloudSaveData payload = JsonConvert.DeserializeObject<TowerMazeCloudSaveData>(saveDataJson);

                string cloudNickname = fields["nickname"]?["stringValue"]?.Value<string>();
                if (!string.IsNullOrWhiteSpace(cloudNickname) && string.IsNullOrWhiteSpace(nickname))
                {
                    nickname = cloudNickname;
                    PlayerPrefs.SetString(PrefNickname, nickname);
                    PlayerPrefs.Save();
                }

                onComplete?.Invoke(payload);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Firebase cloud payload parse failed: {e.Message}");
                onComplete?.Invoke(null);
            }
        }

        private IEnumerator PushLocalStateRoutine(bool pushSaveData, bool pushLeaderboard)
        {
            if (!loggedIn || syncInProgress) yield break;
            syncInProgress = true;

            try
            {
                if (pushSaveData)
                {
                    TowerMazeCloudSaveData payload = BuildLocalPayload();
                    SetLocalPayloadTicks(payload.updatedAtUtcTicks);
                    string payloadJson = JsonConvert.SerializeObject(payload);

                    JObject doc = new JObject
                    {
                        ["fields"] = new JObject
                        {
                            ["saveData"] = new JObject { ["stringValue"] = payloadJson },
                            ["nickname"] = new JObject { ["stringValue"] = nickname ?? string.Empty },
                            ["updatedAtUtcTicks"] = new JObject { ["integerValue"] = payload.updatedAtUtcTicks.ToString() }
                        }
                    };

                    string url = FirestoreDocUrl("users", uid);
                    yield return SendFirestoreRequest(url, "PATCH", doc.ToString(), null,
                        error => Debug.LogWarning($"Firebase cloud save failed: {error}"));
                }

                if (pushLeaderboard)
                {
                    int bestScoreValue = Mathf.Max(0, Mathf.RoundToInt(scoreManager != null ? scoreManager.PersistedBestScore * 100f : 0f));
                    if (bestScoreValue > 0)
                    {
                        RequestNicknameIfNeeded();
                        if (!string.IsNullOrWhiteSpace(nickname))
                        {
                            JObject leaderboardDoc = new JObject
                            {
                                ["fields"] = new JObject
                                {
                                    ["nickname"] = new JObject { ["stringValue"] = nickname },
                                    ["bestScore"] = new JObject { ["integerValue"] = bestScoreValue.ToString() },
                                    ["updatedAtUtcTicks"] = new JObject { ["integerValue"] = DateTime.UtcNow.Ticks.ToString() }
                                }
                            };

                            string leaderboardUrl = FirestoreDocUrl("leaderboard", uid);
                            yield return SendFirestoreRequest(leaderboardUrl, "PATCH", leaderboardDoc.ToString(), null,
                                error => Debug.LogWarning($"Firebase leaderboard submit failed: {error}"));
                        }
                    }

                    yield return RefreshLeaderboardRoutine();
                }
            }
            finally
            {
                syncInProgress = false;
            }
        }

        // ─── Leaderboard ─────────────────────────────────────────────────────

        private IEnumerator RefreshLeaderboardRoutine()
        {
            int limit = config != null ? Mathf.Clamp(config.firebaseLeaderboardSize, 3, 20) : 10;

            JObject query = new JObject
            {
                ["structuredQuery"] = new JObject
                {
                    ["from"] = new JArray { new JObject { ["collectionId"] = "leaderboard" } },
                    ["orderBy"] = new JArray
                    {
                        new JObject
                        {
                            ["field"] = new JObject { ["fieldPath"] = "bestScore" },
                            ["direction"] = "DESCENDING"
                        }
                    },
                    ["limit"] = limit
                }
            };

            string url = $"https://firestore.googleapis.com/v1/projects/{firebaseProjectId}/databases/(default)/documents:runQuery";
            JToken responseArray = null;
            yield return SendFirestoreRequest(url, "POST", query.ToString(),
                root => responseArray = root,
                error =>
                {
                    if (verboseLogging) Debug.LogWarning($"Firebase leaderboard fetch failed: {error}");
                },
                expectArray: true);

            if (responseArray == null || scoreManager == null) yield break;

            List<LeaderboardEntry> entries = new();
            JArray items = responseArray as JArray;
            if (items != null)
            {
                for (int index = 0; index < items.Count; index++)
                {
                    JToken item = items[index];
                    JToken fields = item["document"]?["fields"];
                    if (fields == null) continue;

                    int statValue = 0;
                    string scoreStr = fields["bestScore"]?["integerValue"]?.Value<string>();
                    if (scoreStr != null) int.TryParse(scoreStr, out statValue);

                    string displayName = fields["nickname"]?["stringValue"]?.Value<string>();
                    string label = BuildLeaderboardLabel(displayName, index);
                    entries.Add(new LeaderboardEntry(statValue / 100f, 0f, label));
                }
            }

            if (entries.Count > 0)
            {
                scoreManager.SetCloudLeaderboardEntries(entries);
                uiManager?.UpdateCachedLeaderboard(scoreManager.BestScore, scoreManager.LeaderboardEntries);
            }
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private string FirestoreDocUrl(string collection, string documentId)
        {
            return $"https://firestore.googleapis.com/v1/projects/{firebaseProjectId}/databases/(default)/documents/{collection}/{documentId}";
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
            if (payload == null) return;
            economyManager?.ImportCloudData(payload.economy);
            scoreManager?.ImportCloudData(payload.score);
            coinStoreManager?.ImportCloudData(payload.store);
            uiManager?.UpdateCachedLeaderboard(
                scoreManager != null ? scoreManager.BestScore : 0f,
                scoreManager != null ? scoreManager.LeaderboardEntries : Array.Empty<LeaderboardEntry>());
        }

        private static string BuildLeaderboardLabel(string displayName, int rank)
        {
            string label = string.IsNullOrWhiteSpace(displayName) ? $"P{rank + 1}" : displayName.Trim();
            if (label.Length > 9) label = label.Substring(0, 9);
            return label.ToUpperInvariant();
        }

        private void RequestNicknameIfNeeded()
        {
            int bestScoreValue = Mathf.Max(0, Mathf.RoundToInt(scoreManager != null ? scoreManager.PersistedBestScore * 100f : 0f));
            if (!loggedIn || nicknamePromptRequestedThisSession || !string.IsNullOrWhiteSpace(nickname) || bestScoreValue <= 0)
            {
                return;
            }

            nicknamePromptRequestedThisSession = true;
            NicknameRequired?.Invoke();
        }

        private static long GetLocalPayloadTicks()
        {
            return long.TryParse(PlayerPrefs.GetString(LocalPayloadTicksKey, "0"), out long parsed) ? parsed : 0L;
        }

        private static void SetLocalPayloadTicks(long ticks)
        {
            PlayerPrefs.SetString(LocalPayloadTicksKey, ticks.ToString());
            PlayerPrefs.Save();
        }

        // ─── HTTP Layer ──────────────────────────────────────────────────────

        private IEnumerator SendFirestoreRequest(string url, string method, string jsonBody, Action<JToken> onSuccess, Action<string> onFailure, bool expectArray = false)
        {
            yield return SendFirestoreRequestInternal(url, method, jsonBody, onSuccess, onFailure, expectArray, allowRetry: true);
        }

        private IEnumerator SendFirestoreRequestInternal(string url, string method, string jsonBody, Action<JToken> onSuccess, Action<string> onFailure, bool expectArray, bool allowRetry)
        {
            using UnityWebRequest request = new(url, method);
            if (jsonBody != null)
            {
                byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            }
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrWhiteSpace(idToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {idToken}");
            }

            yield return request.SendWebRequest();

            string responseText = request.downloadHandler?.text ?? string.Empty;

            if (request.responseCode == 401 && allowRetry)
            {
                bool refreshed = false;
                yield return RefreshIdTokenRoutine(success => refreshed = success);
                if (refreshed)
                {
                    yield return SendFirestoreRequestInternal(url, method, jsonBody, onSuccess, onFailure, expectArray, allowRetry: false);
                    yield break;
                }
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                onFailure?.Invoke(string.IsNullOrWhiteSpace(responseText) ? request.error : responseText);
                yield break;
            }

            try
            {
                if (expectArray)
                {
                    JArray array = JArray.Parse(responseText);
                    onSuccess?.Invoke(array);
                }
                else
                {
                    JObject root = string.IsNullOrWhiteSpace(responseText) ? new JObject() : JObject.Parse(responseText);
                    if (root["error"] != null)
                    {
                        string errorMessage = root["error"]?["message"]?.Value<string>() ?? $"HTTP {request.responseCode}";
                        onFailure?.Invoke(errorMessage);
                        yield break;
                    }
                    onSuccess?.Invoke(root);
                }
            }
            catch (Exception e)
            {
                onFailure?.Invoke($"INVALID RESPONSE: {e.Message}");
            }
        }

        private IEnumerator SendJsonRequest(string url, string method, string jsonBody, string authHeader, Action<JObject> onSuccess, Action<string> onFailure)
        {
            using UnityWebRequest request = new(url, method);
            if (jsonBody != null)
            {
                byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            }
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrWhiteSpace(authHeader))
            {
                request.SetRequestHeader("Authorization", authHeader);
            }

            yield return request.SendWebRequest();

            string responseText = request.downloadHandler?.text ?? string.Empty;
            if (request.result != UnityWebRequest.Result.Success)
            {
                onFailure?.Invoke(string.IsNullOrWhiteSpace(responseText) ? request.error : responseText);
                yield break;
            }

            try
            {
                JObject root = string.IsNullOrWhiteSpace(responseText) ? new JObject() : JObject.Parse(responseText);
                onSuccess?.Invoke(root);
            }
            catch (Exception e)
            {
                onFailure?.Invoke($"INVALID RESPONSE: {e.Message}");
            }
        }
    }
}
