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
        public string equippedAvatarFrameId = string.Empty;
        public List<string> ownedSkinIds = new();
        public List<string> ownedTowerSkinIds = new();
        public List<string> ownedAvatarFrameIds = new();
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
        private ChapterManager chapterManager;
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
        private Coroutine loginSyncCoroutine;
        private bool nicknamePromptRequestedThisSession;
        private bool nicknameReservationInProgress;

        public bool IsEnabled => config != null && config.enableFirebaseCloudSync && !string.IsNullOrWhiteSpace(firebaseApiKey);
        public bool IsLoggedIn => loggedIn;
        public event Action NicknameRequired;

        // Wire protocol between TrySetNickname and the popup UI. When the chosen name
        // is taken AND the auto-suggest probe found a free alternative, the error
        // callback receives "SUGGEST:USTAB42" so the popup can render an accept button
        // for the suggestion without us having to break the existing callback contract.
        public const string SuggestionPrefix = "SUGGEST:";

        public void Initialize(GameConfig gameConfig, EconomyManager economy, ScoreManager score, CoinStoreManager coinStore, UIManager ui, ChapterManager chapter = null)
        {
            if (initialized) return;
            initialized = true;
            config = gameConfig;
            economyManager = economy;
            scoreManager = score;
            coinStoreManager = coinStore;
            uiManager = ui;
            chapterManager = chapter;

            LoadFirebaseConfig();
            nickname = PlayerPrefs.GetString(PrefNickname, string.Empty);

            if (economyManager != null) economyManager.StateChanged += HandleLocalStateChanged;
            if (scoreManager != null) scoreManager.StateChanged += HandleLocalStateChanged;
            if (coinStoreManager != null) coinStoreManager.StateChanged += HandleLocalStateChanged;
            if (chapterManager != null) chapterManager.UnlockedUpToChanged += HandleChapterProgressChanged;

            if (!IsEnabled)
            {
                if (verboseLogging) Debug.Log("Firebase cloud sync disabled; using local save only.");
                return;
            }

            loginSyncCoroutine = StartCoroutine(LoginAndSyncRoutine());
        }

        private void OnDestroy()
        {
            if (loginSyncCoroutine != null) StopCoroutine(loginSyncCoroutine);
            if (queuedSyncCoroutine != null) StopCoroutine(queuedSyncCoroutine);
            if (economyManager != null) economyManager.StateChanged -= HandleLocalStateChanged;
            if (scoreManager != null) scoreManager.StateChanged -= HandleLocalStateChanged;
            if (coinStoreManager != null) coinStoreManager.StateChanged -= HandleLocalStateChanged;
            if (chapterManager != null) chapterManager.UnlockedUpToChanged -= HandleChapterProgressChanged;
        }

        // Hook for ChapterManager.UnlockedUpToChanged. Routes through the same queued
        // sync pipeline used for endless score commits so we coalesce rapid bumps and
        // pick up the new chapterBest in PushLocalStateRoutine's leaderboard PATCH.
        private void HandleChapterProgressChanged(int newUnlockedUpTo)
        {
            if (suppressLocalChanges) return;
            if (autoSyncOnStateChange) QueueSync(0.5f);
        }

        public void SetNickname(string newNickname)
        {
            string normalizedNickname = NormalizeNickname(newNickname);
            if (string.IsNullOrWhiteSpace(normalizedNickname)) return;
            ApplyNicknameLocally(normalizedNickname);
            if (loggedIn) QueueSync(0.5f);
        }

        public void TrySetNickname(string newNickname, Action<bool, string> onComplete)
        {
            string normalizedNickname = NormalizeNickname(newNickname);
            if (normalizedNickname.Length < 2 || normalizedNickname.Length > 12)
            {
                onComplete?.Invoke(false, GetNicknameValidationMessage());
                return;
            }

            if (ProfanityFilter.IsProfane(normalizedNickname))
            {
                onComplete?.Invoke(false, GetNicknameProfaneMessage());
                return;
            }

            if (nicknameReservationInProgress)
            {
                onComplete?.Invoke(false, GetNicknameBusyMessage());
                return;
            }

            if (!IsEnabled || !loggedIn)
            {
                // Offline: check local leaderboard cache for duplicates before allowing
                if (IsNicknameInLocalLeaderboard(normalizedNickname))
                {
                    string offlineSuggestion = GenerateOfflineNicknameSuggestion(normalizedNickname);
                    onComplete?.Invoke(false, FormatTakenMessage(offlineSuggestion));
                    return;
                }
                ApplyNicknameLocally(normalizedNickname);
                onComplete?.Invoke(true, string.Empty);
                return;
            }

            StartCoroutine(TrySetNicknameRoutine(normalizedNickname, onComplete));
        }

        /// <summary>
        /// Proactive entry point used by TowerMazeBootstrapper on first launch — fires
        /// the popup as soon as we know the player has no nickname, instead of waiting
        /// for them to post a score. Idempotent within a session via the same flag the
        /// reactive RequestNicknameIfNeeded path uses.
        /// </summary>
        public void RequestNicknameNow()
        {
            if (nicknamePromptRequestedThisSession) return;
            if (!string.IsNullOrWhiteSpace(nickname)) return;
            nicknamePromptRequestedThisSession = true;
            NicknameRequired?.Invoke();
        }

        // Append a 2-3 digit random suffix so the popup can offer "USTAB42" when
        // "USTAB" is taken. We do not verify availability of the suggestion here in
        // the offline branch — the user will hit Firestore on the next attempt.
        private static string GenerateOfflineNicknameSuggestion(string baseName)
        {
            int suffix = UnityEngine.Random.Range(10, 1000);
            string candidate = baseName + suffix;
            // Trim to the same 12-char max the validator enforces.
            if (candidate.Length > 12) candidate = candidate.Substring(0, 12);
            return candidate;
        }

        private string FormatTakenMessage(string suggestion)
        {
            if (string.IsNullOrWhiteSpace(suggestion)) return GetNicknameTakenMessage();
            return SuggestionPrefix + suggestion;
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
            yield return RefreshChapterLeaderboardRoutine();
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
                    int chapterBestValue = chapterManager != null ? Mathf.Max(0, chapterManager.UnlockedUpTo) : 0;
                    // Include chapter-only players (no endless score yet) so the chapter
                    // tab can show them once they progress past chapter 1.
                    bool hasAnyProgress = bestScoreValue > 0 || chapterBestValue > 1;
                    if (hasAnyProgress)
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
                                    ["chapterBest"] = new JObject { ["integerValue"] = chapterBestValue.ToString() },
                                    ["avatarFrame"] = new JObject { ["stringValue"] = economyManager != null ? economyManager.EquippedAvatarFrameId : "none" },
                                    ["updatedAtUtcTicks"] = new JObject { ["integerValue"] = DateTime.UtcNow.Ticks.ToString() }
                                }
                            };

                            string leaderboardUrl = FirestoreDocUrl("leaderboard", uid);
                            yield return SendFirestoreRequest(leaderboardUrl, "PATCH", leaderboardDoc.ToString(), null,
                                error => Debug.LogWarning($"Firebase leaderboard submit failed: {error}"));
                        }
                    }

                    yield return RefreshLeaderboardRoutine();
                    yield return RefreshChapterLeaderboardRoutine();
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
                    string avatarFrameId = fields["avatarFrame"]?["stringValue"]?.Value<string>() ?? "none";
                    string label = BuildLeaderboardLabel(displayName, index);
                    entries.Add(new LeaderboardEntry(statValue / 100f, 0f, label, avatarFrameId));
                }
            }

            if (entries.Count > 0)
            {
                scoreManager.SetCloudLeaderboardEntries(entries);
                uiManager?.UpdateCachedLeaderboard(scoreManager.BestScore, scoreManager.LeaderboardEntries);
            }
        }

        // Same Firestore collection as endless, just sorted by chapterBest. We pack
        // the chapter number into the LeaderboardEntry.height field — the StartScreen
        // formatter knows the active tab and renders "CH N" instead of "Nm".
        private IEnumerator RefreshChapterLeaderboardRoutine()
        {
            if (scoreManager == null) yield break;
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
                            ["field"] = new JObject { ["fieldPath"] = "chapterBest" },
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
                    if (verboseLogging) Debug.LogWarning($"Firebase chapter leaderboard fetch failed: {error}");
                },
                expectArray: true);

            if (responseArray == null) yield break;

            List<LeaderboardEntry> entries = new();
            JArray items = responseArray as JArray;
            if (items != null)
            {
                for (int index = 0; index < items.Count; index++)
                {
                    JToken item = items[index];
                    JToken fields = item["document"]?["fields"];
                    if (fields == null) continue;

                    int chapterValue = 0;
                    string chapterStr = fields["chapterBest"]?["integerValue"]?.Value<string>();
                    if (chapterStr != null) int.TryParse(chapterStr, out chapterValue);
                    // Skip docs with no chapter progress so the tab isn't padded with zeros.
                    if (chapterValue <= 0) continue;

                    string displayName = fields["nickname"]?["stringValue"]?.Value<string>();
                    string avatarFrameId = fields["avatarFrame"]?["stringValue"]?.Value<string>() ?? "none";
                    string label = BuildLeaderboardLabel(displayName, index);
                    entries.Add(new LeaderboardEntry(chapterValue, 0f, label, avatarFrameId));
                }
            }

            scoreManager.SetCloudChapterLeaderboardEntries(entries);
            uiManager?.UpdateChapterLeaderboard(entries);
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

        private IEnumerator TrySetNicknameRoutine(string requestedNickname, Action<bool, string> onComplete)
        {
            nicknameReservationInProgress = true;

            try
            {
                if (string.Equals(nickname, requestedNickname, StringComparison.Ordinal))
                {
                    onComplete?.Invoke(true, string.Empty);
                    yield break;
                }

                bool reservationFound = false;
                string reservationOwnerUid = string.Empty;
                string reservationError = null;
                yield return FetchNicknameReservationOwnerRoutine(
                    requestedNickname,
                    (found, ownerUid) =>
                    {
                        reservationFound = found;
                        reservationOwnerUid = ownerUid ?? string.Empty;
                    },
                    error => reservationError = error);

                if (!string.IsNullOrWhiteSpace(reservationError))
                {
                    onComplete?.Invoke(false, GetNicknameServiceUnavailableMessage());
                    yield break;
                }

                if (reservationFound)
                {
                    if (!string.Equals(reservationOwnerUid, uid, StringComparison.Ordinal))
                    {
                        string suggestion = string.Empty;
                        yield return TryProbeNicknameSuggestionRoutine(requestedNickname, s => suggestion = s);
                        onComplete?.Invoke(false, FormatTakenMessage(suggestion));
                        yield break;
                    }

                    ApplyNicknameLocally(requestedNickname);
                    if (loggedIn) QueueSync(0.1f);
                    onComplete?.Invoke(true, string.Empty);
                    yield break;
                }

                string legacyOwnerUid = string.Empty;
                string legacyError = null;
                yield return QueryUserOwnerByNicknameRoutine(
                    requestedNickname,
                    ownerUid => legacyOwnerUid = ownerUid ?? string.Empty,
                    error => legacyError = error);

                if (!string.IsNullOrWhiteSpace(legacyError))
                {
                    onComplete?.Invoke(false, GetNicknameServiceUnavailableMessage());
                    yield break;
                }

                if (!string.IsNullOrWhiteSpace(legacyOwnerUid) &&
                    !string.Equals(legacyOwnerUid, uid, StringComparison.Ordinal))
                {
                    string suggestion = string.Empty;
                    yield return TryProbeNicknameSuggestionRoutine(requestedNickname, s => suggestion = s);
                    onComplete?.Invoke(false, FormatTakenMessage(suggestion));
                    yield break;
                }

                // Also check the leaderboard collection for legacy users whose
                // nickname may not have a nicknames/ reservation yet.
                string leaderboardOwnerUid = string.Empty;
                string leaderboardError = null;
                yield return QueryLeaderboardOwnerByNicknameRoutine(
                    requestedNickname,
                    ownerUid => leaderboardOwnerUid = ownerUid ?? string.Empty,
                    error => leaderboardError = error);

                if (!string.IsNullOrWhiteSpace(leaderboardError))
                {
                    onComplete?.Invoke(false, GetNicknameServiceUnavailableMessage());
                    yield break;
                }

                if (!string.IsNullOrWhiteSpace(leaderboardOwnerUid) &&
                    !string.Equals(leaderboardOwnerUid, uid, StringComparison.Ordinal))
                {
                    string suggestion = string.Empty;
                    yield return TryProbeNicknameSuggestionRoutine(requestedNickname, s => suggestion = s);
                    onComplete?.Invoke(false, FormatTakenMessage(suggestion));
                    yield break;
                }

                bool claimed = false;
                string claimError = null;
                yield return CreateNicknameReservationRoutine(
                    requestedNickname,
                    success => claimed = success,
                    error => claimError = error);

                if (!claimed)
                {
                    reservationFound = false;
                    reservationOwnerUid = string.Empty;
                    reservationError = null;
                    yield return FetchNicknameReservationOwnerRoutine(
                        requestedNickname,
                        (found, ownerUid) =>
                        {
                            reservationFound = found;
                            reservationOwnerUid = ownerUid ?? string.Empty;
                        },
                        error => reservationError = error);

                    if (reservationFound && string.Equals(reservationOwnerUid, uid, StringComparison.Ordinal))
                    {
                        claimed = true;
                    }
                    else if (reservationFound)
                    {
                        string suggestion = string.Empty;
                        yield return TryProbeNicknameSuggestionRoutine(requestedNickname, s => suggestion = s);
                        onComplete?.Invoke(false, FormatTakenMessage(suggestion));
                        yield break;
                    }
                    else
                    {
                        if (verboseLogging && !string.IsNullOrWhiteSpace(claimError))
                        {
                            Debug.LogWarning($"Firebase nickname reservation failed: {claimError}");
                        }

                        onComplete?.Invoke(false, GetNicknameServiceUnavailableMessage());
                        yield break;
                    }
                }

                string previousNickname = nickname;
                ApplyNicknameLocally(requestedNickname);
                if (loggedIn) QueueSync(0.1f);

                if (!string.IsNullOrWhiteSpace(previousNickname) &&
                    !string.Equals(previousNickname, requestedNickname, StringComparison.Ordinal))
                {
                    StartCoroutine(ReleaseNicknameReservationRoutine(previousNickname));
                }

                onComplete?.Invoke(true, string.Empty);
            }
            finally
            {
                nicknameReservationInProgress = false;
            }
        }

        // Probe a single random suffix (e.g. baseName + "42") against the reservation
        // collection. Used by the duplicate-detection paths in TrySetNicknameRoutine to
        // surface a free alternative without forcing the player to invent one. The
        // probe is best-effort: a service error or another collision returns an empty
        // suggestion and the popup falls back to the standard "name taken" message.
        private IEnumerator TryProbeNicknameSuggestionRoutine(string baseName, Action<string> onSuggestion)
        {
            int suffix = UnityEngine.Random.Range(10, 1000);
            string candidate = baseName + suffix;
            if (candidate.Length > 12) candidate = candidate.Substring(0, 12);

            bool found = false;
            string error = null;
            yield return FetchNicknameReservationOwnerRoutine(
                candidate,
                (isFound, ownerUid) => found = isFound,
                err => error = err);

            if (string.IsNullOrWhiteSpace(error) && !found)
            {
                onSuggestion?.Invoke(candidate);
            }
            else
            {
                onSuggestion?.Invoke(string.Empty);
            }
        }

        private IEnumerator FetchNicknameReservationOwnerRoutine(
            string requestedNickname,
            Action<bool, string> onComplete,
            Action<string> onFailure)
        {
            string url = FirestoreDocUrl("nicknames", requestedNickname);
            JToken response = null;
            string errorMessage = null;

            yield return SendFirestoreRequest(
                url,
                "GET",
                null,
                root => response = root,
                error => errorMessage = error);

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                if (errorMessage.IndexOf("NOT_FOUND", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    onComplete?.Invoke(false, string.Empty);
                    yield break;
                }

                onFailure?.Invoke(errorMessage);
                yield break;
            }

            string ownerUid = response?["fields"]?["uid"]?["stringValue"]?.Value<string>() ?? string.Empty;
            onComplete?.Invoke(true, ownerUid);
        }

        private IEnumerator QueryUserOwnerByNicknameRoutine(
            string requestedNickname,
            Action<string> onComplete,
            Action<string> onFailure)
        {
            JObject query = new JObject
            {
                ["structuredQuery"] = new JObject
                {
                    ["from"] = new JArray { new JObject { ["collectionId"] = "users" } },
                    ["where"] = new JObject
                    {
                        ["fieldFilter"] = new JObject
                        {
                            ["field"] = new JObject { ["fieldPath"] = "nickname" },
                            ["op"] = "EQUAL",
                            ["value"] = new JObject { ["stringValue"] = requestedNickname }
                        }
                    },
                    ["limit"] = 1
                }
            };

            string url = $"https://firestore.googleapis.com/v1/projects/{firebaseProjectId}/databases/(default)/documents:runQuery";
            JToken responseArray = null;
            string errorMessage = null;

            yield return SendFirestoreRequest(
                url,
                "POST",
                query.ToString(),
                root => responseArray = root,
                error => errorMessage = error,
                expectArray: true);

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                onFailure?.Invoke(errorMessage);
                yield break;
            }

            string ownerUid = string.Empty;
            if (responseArray is JArray items)
            {
                for (int index = 0; index < items.Count; index++)
                {
                    string documentName = items[index]?["document"]?["name"]?.Value<string>() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(documentName))
                    {
                        ownerUid = GetDocumentIdFromPath(documentName);
                        break;
                    }
                }
            }

            onComplete?.Invoke(ownerUid);
        }

        private IEnumerator QueryLeaderboardOwnerByNicknameRoutine(
            string requestedNickname,
            Action<string> onComplete,
            Action<string> onFailure)
        {
            JObject query = new JObject
            {
                ["structuredQuery"] = new JObject
                {
                    ["from"] = new JArray { new JObject { ["collectionId"] = "leaderboard" } },
                    ["where"] = new JObject
                    {
                        ["fieldFilter"] = new JObject
                        {
                            ["field"] = new JObject { ["fieldPath"] = "nickname" },
                            ["op"] = "EQUAL",
                            ["value"] = new JObject { ["stringValue"] = requestedNickname }
                        }
                    },
                    ["limit"] = 1
                }
            };

            string url = $"https://firestore.googleapis.com/v1/projects/{firebaseProjectId}/databases/(default)/documents:runQuery";
            JToken responseArray = null;
            string errorMessage = null;

            yield return SendFirestoreRequest(
                url,
                "POST",
                query.ToString(),
                root => responseArray = root,
                error => errorMessage = error,
                expectArray: true);

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                onFailure?.Invoke(errorMessage);
                yield break;
            }

            string ownerUid = string.Empty;
            if (responseArray is JArray items)
            {
                for (int index = 0; index < items.Count; index++)
                {
                    string documentName = items[index]?["document"]?["name"]?.Value<string>() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(documentName))
                    {
                        ownerUid = GetDocumentIdFromPath(documentName);
                        break;
                    }
                }
            }

            onComplete?.Invoke(ownerUid);
        }

        private IEnumerator CreateNicknameReservationRoutine(
            string requestedNickname,
            Action<bool> onComplete,
            Action<string> onFailure)
        {
            JObject commitBody = new JObject
            {
                ["writes"] = new JArray
                {
                    new JObject
                    {
                        ["update"] = new JObject
                        {
                            ["name"] = FirestoreDocumentName("nicknames", requestedNickname),
                            ["fields"] = new JObject
                            {
                                ["uid"] = new JObject { ["stringValue"] = uid },
                                ["nickname"] = new JObject { ["stringValue"] = requestedNickname },
                                ["updatedAtUtcTicks"] = new JObject { ["integerValue"] = DateTime.UtcNow.Ticks.ToString() }
                            }
                        },
                        ["currentDocument"] = new JObject { ["exists"] = false }
                    }
                }
            };

            string url = $"https://firestore.googleapis.com/v1/projects/{firebaseProjectId}/databases/(default)/documents:commit";
            JToken response = null;
            string errorMessage = null;

            yield return SendFirestoreRequest(
                url,
                "POST",
                commitBody.ToString(),
                root => response = root,
                error => errorMessage = error);

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                onFailure?.Invoke(errorMessage);
                onComplete?.Invoke(false);
                yield break;
            }

            onComplete?.Invoke(response != null);
        }

        private IEnumerator ReleaseNicknameReservationRoutine(string reservedNickname)
        {
            if (string.IsNullOrWhiteSpace(reservedNickname) || !loggedIn || !IsEnabled)
            {
                yield break;
            }

            bool reservationFound = false;
            string reservationOwnerUid = string.Empty;
            string reservationError = null;
            yield return FetchNicknameReservationOwnerRoutine(
                reservedNickname,
                (found, ownerUid) =>
                {
                    reservationFound = found;
                    reservationOwnerUid = ownerUid ?? string.Empty;
                },
                error => reservationError = error);

            if (!reservationFound ||
                !string.IsNullOrWhiteSpace(reservationError) ||
                !string.Equals(reservationOwnerUid, uid, StringComparison.Ordinal))
            {
                yield break;
            }

            string url = FirestoreDocUrl("nicknames", reservedNickname);
            yield return SendFirestoreRequest(
                url,
                "DELETE",
                null,
                _ => { },
                error =>
                {
                    if (verboseLogging)
                    {
                        Debug.LogWarning($"Firebase nickname release failed: {error}");
                    }
                });
        }

        private void ApplyNicknameLocally(string normalizedNickname)
        {
            nickname = normalizedNickname;
            nicknamePromptRequestedThisSession = false;
            PlayerPrefs.SetString(PrefNickname, nickname);
            PlayerPrefs.Save();
        }

        private string FirestoreDocumentName(string collection, string documentId)
        {
            return $"projects/{firebaseProjectId}/databases/(default)/documents/{collection}/{documentId}";
        }

        private static string GetDocumentIdFromPath(string documentPath)
        {
            if (string.IsNullOrWhiteSpace(documentPath))
            {
                return string.Empty;
            }

            int lastSlashIndex = documentPath.LastIndexOf('/');
            return lastSlashIndex >= 0 && lastSlashIndex < documentPath.Length - 1
                ? documentPath.Substring(lastSlashIndex + 1)
                : documentPath;
        }

        private static string NormalizeNickname(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            StringBuilder builder = new();
            string trimmed = value.Trim().ToUpperInvariant();
            for (int index = 0; index < trimmed.Length; index++)
            {
                char current = trimmed[index];
                if ((current >= 'A' && current <= 'Z') ||
                    (current >= '0' && current <= '9') ||
                    current == '_')
                {
                    builder.Append(current);
                }
            }

            return builder.ToString();
        }

        private bool IsNicknameInLocalLeaderboard(string normalizedNickname)
        {
            if (scoreManager == null) return false;
            IReadOnlyList<LeaderboardEntry> entries = scoreManager.LeaderboardEntries;
            if (entries == null) return false;
            string localNick = string.IsNullOrWhiteSpace(nickname) ? string.Empty : nickname;
            for (int i = 0; i < entries.Count; i++)
            {
                string label = entries[i].label;
                if (string.IsNullOrWhiteSpace(label)) continue;
                // Skip if this entry is ours
                if (string.Equals(label, localNick, StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(label, normalizedNickname, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static string GetNicknameValidationMessage()
        {
            return UILanguage.Translate(
                "2-12 karakter kullan. Harf, rakam ve _ desteklenir.",
                "Use 2-12 characters. Letters, numbers, and _ are allowed.",
                "Usa 2-12 caracteres. Se permiten letras, numeros y _.");
        }

        private static string GetNicknameTakenMessage()
        {
            return UILanguage.Translate(
                "BU ISIM KULLANIMDA. BASKA BIR ISIM DENE.",
                "THIS NAME IS ALREADY TAKEN. TRY ANOTHER ONE.",
                "ESTE NOMBRE YA ESTA EN USO. PRUEBA OTRO.");
        }

        private static string GetNicknameProfaneMessage()
        {
            return UILanguage.Translate(
                "BU ISIM UYGUN DEGIL. BASKA BIR ISIM DENE.",
                "THIS NAME ISN'T ALLOWED. TRY ANOTHER ONE.",
                "ESTE NOMBRE NO ESTA PERMITIDO. PRUEBA OTRO.");
        }

        private static string GetNicknameServiceUnavailableMessage()
        {
            return UILanguage.Translate(
                "ISIM DOGRULANAMADI. TEKRAR DENE.",
                "NAME CHECK FAILED. PLEASE TRY AGAIN.",
                "NO SE PUDO VALIDAR EL NOMBRE. INTENTALO DE NUEVO.");
        }

        private static string GetNicknameBusyMessage()
        {
            return UILanguage.Translate(
                "ISIM KONTROLU DEVAM EDIYOR...",
                "NAME CHECK ALREADY IN PROGRESS...",
                "LA VERIFICACION DEL NOMBRE YA ESTA EN CURSO...");
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
