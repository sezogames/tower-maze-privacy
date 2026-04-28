using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


namespace TowerMaze
{
    internal enum RushState
    {
        Idle,
        Warning,
        Active,
    }

    internal enum ControlFlipState
    {
        Idle,
        Warning,
        Active,
    }

    public enum RunMode
    {
        Normal,
        DailyChallenge,
        Chapter,
    }

    public enum RunModifierType
    {
        None,
        Slipstream,
        HighStakes,
    }

    [System.Serializable]
    public struct LeaderboardEntry
    {
        public float height;
        public float timeSeconds;
        public string label;

        public LeaderboardEntry(float height, float timeSeconds, string label = "")
        {
            this.height = height;
            this.timeSeconds = timeSeconds;
            this.label = label ?? string.Empty;
        }
    }

    [System.Serializable]
    internal sealed class LeaderboardSaveData
    {
        public List<LeaderboardEntry> entries = new();
    }

    public enum ShopActionResult
    {
        None,
        Purchased,
        Equipped,
        InsufficientFunds,
    }

    [System.Serializable]
    public struct DailyChallengeStatus
    {
        public string dateKey;
        public int seed;
        public int targetHeight;
        public int firstClearReward;
        public float bestHeight;
        public float bestTimeSeconds;
        public bool rewardClaimed;
        public RunModifierType primaryModifier;
        public RunModifierType secondaryModifier;
    }

    [System.Serializable]
    internal sealed class DailyChallengeSaveData
    {
        public DailyChallengeStatus status;
    }

    public readonly struct NextUnlockStatus
    {
        public readonly bool HasTarget;
        public readonly string DisplayName;
        public readonly int Price;
        public readonly int RemainingCoins;
        public readonly bool IsTowerItem;

        public NextUnlockStatus(bool hasTarget, string displayName, int price, int remainingCoins, bool isTowerItem)
        {
            HasTarget = hasTarget;
            DisplayName = displayName;
            Price = price;
            RemainingCoins = remainingCoins;
            IsTowerItem = isTowerItem;
        }
    }

    public readonly struct RunSummary
    {
        public readonly float Height;
        public readonly int ZoneNumber;
        public readonly float RunTime;
        public readonly bool UsedContinue;
        public readonly bool IsNewBest;
        public readonly bool IsDailyChallenge;
        public readonly int RushesSurvived;
        public readonly float NearLavaSeconds;
        public readonly RunModifierType PrimaryModifier;
        public readonly RunModifierType SecondaryModifier;

        public RunSummary(
            float height,
            int zoneNumber,
            float runTime,
            bool usedContinue,
            bool isNewBest,
            bool isDailyChallenge,
            int rushesSurvived,
            float nearLavaSeconds,
            RunModifierType primaryModifier,
            RunModifierType secondaryModifier)
        {
            Height = height;
            ZoneNumber = zoneNumber;
            RunTime = runTime;
            UsedContinue = usedContinue;
            IsNewBest = isNewBest;
            IsDailyChallenge = isDailyChallenge;
            RushesSurvived = rushesSurvived;
            NearLavaSeconds = nearLavaSeconds;
            PrimaryModifier = primaryModifier;
            SecondaryModifier = secondaryModifier;
        }

        public bool UsesModifier(string modifierId)
        {
            if (string.IsNullOrWhiteSpace(modifierId))
            {
                return false;
            }

            return string.Equals(PrimaryModifier.ToString(), modifierId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(SecondaryModifier.ToString(), modifierId, StringComparison.OrdinalIgnoreCase);
        }
    }

    public readonly struct DailyChallengeRewardResult
    {
        public readonly int RewardCoins;
        public readonly bool IsNewDailyBest;
        public readonly float PreviousBestHeight;
        public readonly DailyChallengeStatus Status;

        public DailyChallengeRewardResult(int rewardCoins, bool isNewDailyBest, float previousBestHeight, DailyChallengeStatus status)
        {
            RewardCoins = rewardCoins;
            IsNewDailyBest = isNewDailyBest;
            PreviousBestHeight = previousBestHeight;
            Status = status;
        }
    }

    [System.Serializable]
    public struct BallSkinDefinition
    {
        public string id;
        public string displayName;
        public int priceEmber;
        public Color baseColor;
        public Color emissionColor;
        public bool unlockedByDefault;
        public string baseMapResourcePath;
        public string normalMapResourcePath;
        public string emissionMapResourcePath;
        public Vector2 textureScale;
        public float metallic;
        public float smoothness;
        public float normalStrength;
        public float emissionIntensity;
        public string iapProductId;
        public bool isPopular;

        public BallSkinDefinition(
            string id,
            string displayName,
            int priceEmber,
            Color baseColor,
            Color emissionColor,
            bool unlockedByDefault = false,
            string baseMapResourcePath = "",
            string normalMapResourcePath = "",
            string emissionMapResourcePath = "",
            Vector2 textureScale = default,
            float metallic = 0f,
            float smoothness = 0.5f,
            float normalStrength = 1f,
            float emissionIntensity = 1f,
            string iapProductId = "",
            bool isPopular = false)
        {
            this.id = id;
            this.displayName = displayName;
            this.priceEmber = priceEmber;
            this.baseColor = baseColor;
            this.emissionColor = emissionColor;
            this.unlockedByDefault = unlockedByDefault;
            this.baseMapResourcePath = baseMapResourcePath;
            this.normalMapResourcePath = normalMapResourcePath;
            this.emissionMapResourcePath = emissionMapResourcePath;
            this.textureScale = textureScale == default ? Vector2.one : textureScale;
            this.metallic = metallic;
            this.smoothness = smoothness;
            this.normalStrength = normalStrength;
            this.emissionIntensity = emissionIntensity;
            this.iapProductId = iapProductId;
            this.isPopular = isPopular;
        }
    }

    [System.Serializable]
    public struct TowerSkinDefinition
    {
        public string id;
        public string displayName;
        public int priceEmber;
        public bool unlockedByDefault;
        public bool useUnifiedTextureSet;
        public Color wallTint;
        public Color pathTint;
        public Color mainPathTint;
        public string wallBaseMapResourcePath;
        public string wallNormalMapResourcePath;
        public Vector2 wallTextureScale;
        public string pathBaseMapResourcePath;
        public string pathNormalMapResourcePath;
        public Vector2 pathTextureScale;
        public string mainPathBaseMapResourcePath;
        public string mainPathNormalMapResourcePath;
        public Vector2 mainPathTextureScale;
        public string iapProductId;
        public bool isPopular;

        public TowerSkinDefinition(
            string id,
            string displayName,
            int priceEmber,
            Color wallTint,
            Color pathTint,
            Color mainPathTint,
            bool unlockedByDefault = false,
            bool useUnifiedTextureSet = true,
            string wallBaseMapResourcePath = "",
            string wallNormalMapResourcePath = "",
            Vector2 wallTextureScale = default,
            string pathBaseMapResourcePath = "",
            string pathNormalMapResourcePath = "",
            Vector2 pathTextureScale = default,
            string mainPathBaseMapResourcePath = "",
            string mainPathNormalMapResourcePath = "",
            Vector2 mainPathTextureScale = default,
            string iapProductId = "",
            bool isPopular = false)
        {
            this.id = id;
            this.displayName = displayName;
            this.priceEmber = priceEmber;
            this.wallTint = wallTint;
            this.pathTint = pathTint;
            this.mainPathTint = mainPathTint;
            this.unlockedByDefault = unlockedByDefault;
            this.useUnifiedTextureSet = useUnifiedTextureSet;
            this.wallBaseMapResourcePath = wallBaseMapResourcePath;
            this.wallNormalMapResourcePath = wallNormalMapResourcePath;
            this.wallTextureScale = wallTextureScale == default ? Vector2.one : wallTextureScale;
            this.pathBaseMapResourcePath = pathBaseMapResourcePath;
            this.pathNormalMapResourcePath = pathNormalMapResourcePath;
            this.pathTextureScale = pathTextureScale == default ? Vector2.one : pathTextureScale;
            this.mainPathBaseMapResourcePath = mainPathBaseMapResourcePath;
            this.mainPathNormalMapResourcePath = mainPathNormalMapResourcePath;
            this.mainPathTextureScale = mainPathTextureScale == default ? Vector2.one : mainPathTextureScale;
            this.iapProductId = iapProductId;
            this.isPopular = isPopular;
        }
    }

    [System.Serializable]
    internal sealed class SkinInventorySaveData
    {
        public List<string> ownedSkinIds = new();
    }

    public enum ReviewPromptState
    {
        None,
        Rated,
        Dismissed,
        Never
    }

    public sealed class EconomyManager : MonoBehaviour
    {
        private const string EmberBalanceKey = "TowerMaze.EmberBalance";
        private const string RemainingLivesKey = "TowerMaze.RemainingLives";
        private const string LifeRechargeStartTicksKey = "TowerMaze.LifeRechargeStartTicks";
        private const string OwnedSkinsKey = "TowerMaze.OwnedSkins";
        private const string EquippedSkinKey = "TowerMaze.EquippedSkin";
        private const string OwnedTowerSkinsKey = "TowerMaze.OwnedTowerSkins";
        private const string EquippedTowerSkinKey = "TowerMaze.EquippedTowerSkin";
        private const string DailyMissionKey = "TowerMaze.DailyMissionState";
        private const string DailyChallengeKey = "TowerMaze.DailyChallengeState";
        private const string TotalRunsKey = "TowerMaze.TotalRuns";
        private const string ReviewRequestedKey = "TowerMaze.ReviewRequested";
        private const int ShopCoinBoostReward = 100;
        public const int ContinueCoinCost = 900;
        public const int MaxLifeCount = 3;
        public const int LifeRefillCoinCost = 250;
        public const int LifeRefillAmount = 1;
        private static readonly TimeSpan LifeRechargeInterval = TimeSpan.FromHours(4);

        private readonly List<BallSkinDefinition> skins = new();
        private readonly HashSet<string> ownedSkinIds = new();
        private readonly List<TowerSkinDefinition> towerSkins = new();
        private readonly HashSet<string> ownedTowerSkinIds = new();
        private readonly List<DailyMissionState> dailyMissions = new();
        private string dailyDateKey;
        private string lastFreeChestClaimDateKey;
        private string lastBonusChestClaimDateKey;
        private int missionRerollCount;
        private DailyChallengeStatus dailyChallengeStatus;
        private int remainingLives;
        private long lifeRechargeStartTicks;

        public int EmberBalance { get; private set; }
        public int TotalRuns { get; private set; }
        public int RemainingLives
        {
            get
            {
                RefreshLifeRegenIfNeeded();
                return remainingLives;
            }
            private set => remainingLives = value;
        }
        public string EquippedSkinId { get; private set; }
        public string EquippedTowerSkinId { get; private set; }
        public IReadOnlyList<BallSkinDefinition> Skins
        {
            get
            {
                EnsureCatalogBuilt();
                return skins;
            }
        }

        public IReadOnlyList<TowerSkinDefinition> TowerSkins
        {
            get
            {
                EnsureCatalogBuilt();
                return towerSkins;
            }
        }
        public IReadOnlyList<DailyMissionState> DailyMissions => dailyMissions;
        public DailyChallengeStatus DailyChallengeStatus
        {
            get
            {
                RefreshDailyContentIfNeeded();
                return dailyChallengeStatus;
            }
        }
        public event Action<int> EmberBalanceChanged;
        public event Action<TowerSkinDefinition> EquippedTowerSkinChanged;
        public event Action StateChanged;

        public void Initialize()
        {
            BuildCatalog();
            EmberBalance = Mathf.Max(0, PlayerPrefs.GetInt(EmberBalanceKey, 0));
            TotalRuns = Mathf.Max(0, PlayerPrefs.GetInt(TotalRunsKey, 0));
            remainingLives = Mathf.Clamp(PlayerPrefs.GetInt(RemainingLivesKey, MaxLifeCount), 0, MaxLifeCount);
            lifeRechargeStartTicks = long.TryParse(PlayerPrefs.GetString(LifeRechargeStartTicksKey, "0"), out long parsedTicks) ? parsedTicks : 0L;
            LoadOwnedSkins();
            LoadOwnedTowerSkins();
            LoadDailyState();
            LoadDailyChallengeState();
            RefreshDailyContentIfNeeded();
            RefreshLifeRegenIfNeeded();
            NotificationManager.RequestPermissions();

            foreach (BallSkinDefinition skin in skins)
            {
                if (skin.unlockedByDefault)
                {
                    ownedSkinIds.Add(skin.id);
                }
            }

            foreach (TowerSkinDefinition towerSkin in towerSkins)
            {
                if (towerSkin.unlockedByDefault)
                {
                    ownedTowerSkinIds.Add(towerSkin.id);
                }
            }

            EquippedSkinId = PlayerPrefs.GetString(EquippedSkinKey, skins.Count > 0 ? skins[0].id : string.Empty);
            if (!IsOwnedSkin(EquippedSkinId) && skins.Count > 0)
            {
                EquippedSkinId = skins[0].id;
            }

            EquippedTowerSkinId = PlayerPrefs.GetString(EquippedTowerSkinKey, towerSkins.Count > 0 ? towerSkins[0].id : string.Empty);
            if (!IsOwnedTowerSkin(EquippedTowerSkinId) && towerSkins.Count > 0)
            {
                EquippedTowerSkinId = towerSkins[0].id;
            }

            SaveState();
            NotifyEmberBalanceChanged();
            NotifyTowerSkinChanged();
        }

        public bool HasPlayedDailyChallengeToday
        {
            get
            {
                RefreshDailyContentIfNeeded();
                return dailyChallengeStatus.bestHeight > 0.1f;
            }
        }

        public bool ShouldShowReviewPrompt()
        {
            if (PlayerPrefs.GetInt(ReviewRequestedKey, (int)ReviewPromptState.None) != (int)ReviewPromptState.None)
            {
                return false;
            }

            return TotalRuns >= 5;
        }

        public void SetReviewState(ReviewPromptState state)
        {
            PlayerPrefs.SetInt(ReviewRequestedKey, (int)state);
            PlayerPrefs.Save();
        }

        public int CalculateRunReward(float height, int zoneNumber, float runTime)
        {
            return 25;
        }

        public int GetMissionRerollCost()
        {
            RefreshDailyContentIfNeeded();
            return 420 + (missionRerollCount * 180);
        }

        public int GetShopCoinBoostReward()
        {
            return ShopCoinBoostReward;
        }

        public TimeSpan GetTimeUntilNextLife()
        {
            RefreshLifeRegenIfNeeded();
            if (remainingLives >= MaxLifeCount)
            {
                return TimeSpan.Zero;
            }

            if (lifeRechargeStartTicks <= 0L)
            {
                return LifeRechargeInterval;
            }

            long targetTicks = lifeRechargeStartTicks + LifeRechargeInterval.Ticks;
            long remainingTicks = Math.Max(0L, targetTicks - DateTime.UtcNow.Ticks);
            return TimeSpan.FromTicks(remainingTicks);
        }

        internal EconomyCloudSaveData ExportCloudData()
        {
            RefreshDailyContentIfNeeded();
            RefreshLifeRegenIfNeeded();
            EnsureCatalogBuilt();

            return new EconomyCloudSaveData
            {
                emberBalance = EmberBalance,
                remainingLives = remainingLives,
                lifeRechargeStartTicks = lifeRechargeStartTicks,
                equippedSkinId = EquippedSkinId ?? string.Empty,
                equippedTowerSkinId = EquippedTowerSkinId ?? string.Empty,
                ownedSkinIds = new List<string>(ownedSkinIds),
                ownedTowerSkinIds = new List<string>(ownedTowerSkinIds),
                dailyMissionState = new DailyMissionSaveData
                {
                    dateKey = dailyDateKey ?? string.Empty,
                    missions = new List<DailyMissionState>(dailyMissions),
                    lastFreeChestClaimDateKey = lastFreeChestClaimDateKey ?? string.Empty,
                    lastBonusChestClaimDateKey = lastBonusChestClaimDateKey ?? string.Empty,
                    rerollCount = missionRerollCount,
                },
                dailyChallengeState = new DailyChallengeSaveData
                {
                    status = dailyChallengeStatus
                }
            };
        }

        internal void ImportCloudData(EconomyCloudSaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            EnsureCatalogBuilt();
            EmberBalance = Mathf.Max(0, saveData.emberBalance);
            remainingLives = Mathf.Clamp(saveData.remainingLives, 0, MaxLifeCount);
            lifeRechargeStartTicks = Math.Max(0L, saveData.lifeRechargeStartTicks);

            ownedSkinIds.Clear();
            if (saveData.ownedSkinIds != null)
            {
                for (int index = 0; index < saveData.ownedSkinIds.Count; index++)
                {
                    string skinId = saveData.ownedSkinIds[index];
                    if (!string.IsNullOrWhiteSpace(skinId))
                    {
                        ownedSkinIds.Add(skinId);
                    }
                }
            }

            ownedTowerSkinIds.Clear();
            if (saveData.ownedTowerSkinIds != null)
            {
                for (int index = 0; index < saveData.ownedTowerSkinIds.Count; index++)
                {
                    string towerSkinId = saveData.ownedTowerSkinIds[index];
                    if (!string.IsNullOrWhiteSpace(towerSkinId))
                    {
                        ownedTowerSkinIds.Add(towerSkinId);
                    }
                }
            }

            for (int index = 0; index < skins.Count; index++)
            {
                if (skins[index].unlockedByDefault)
                {
                    ownedSkinIds.Add(skins[index].id);
                }
            }

            for (int index = 0; index < towerSkins.Count; index++)
            {
                if (towerSkins[index].unlockedByDefault)
                {
                    ownedTowerSkinIds.Add(towerSkins[index].id);
                }
            }

            EquippedSkinId = !string.IsNullOrWhiteSpace(saveData.equippedSkinId) && IsOwnedSkin(saveData.equippedSkinId)
                ? saveData.equippedSkinId
                : (skins.Count > 0 ? skins[0].id : string.Empty);
            EquippedTowerSkinId = !string.IsNullOrWhiteSpace(saveData.equippedTowerSkinId) && IsOwnedTowerSkin(saveData.equippedTowerSkinId)
                ? saveData.equippedTowerSkinId
                : (towerSkins.Count > 0 ? towerSkins[0].id : string.Empty);

            dailyMissions.Clear();
            DailyMissionSaveData missionState = saveData.dailyMissionState;
            dailyDateKey = missionState?.dateKey ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
            lastFreeChestClaimDateKey = missionState?.lastFreeChestClaimDateKey ?? string.Empty;
            lastBonusChestClaimDateKey = missionState?.lastBonusChestClaimDateKey ?? string.Empty;
            missionRerollCount = Mathf.Max(0, missionState?.rerollCount ?? 0);
            if (missionState?.missions != null)
            {
                dailyMissions.AddRange(missionState.missions);
            }

            dailyChallengeStatus = saveData.dailyChallengeState?.status ?? default;
            RefreshLifeRegenIfNeeded();
            SaveState();
            NotifyEmberBalanceChanged();
            NotifyTowerSkinChanged();
        }

        public bool ClaimShopCoinBoost()
        {
            GrantEmber(ShopCoinBoostReward);
            return true;
        }

        public bool TryConsumeLife()
        {
            RefreshLifeRegenIfNeeded();
            if (remainingLives <= 0)
            {
                return false;
            }

            bool wasFull = remainingLives >= MaxLifeCount;
            remainingLives--;
            if (wasFull || lifeRechargeStartTicks <= 0L)
            {
                lifeRechargeStartTicks = DateTime.UtcNow.Ticks;
            }
            SaveState();
            NotificationManager.ScheduleLivesFullNotification(DateTime.UtcNow + LifeRechargeInterval);
            return true;
        }

        public void GrantLife(int amount = LifeRefillAmount)
        {
            if (amount <= 0)
            {
                return;
            }

            RefreshLifeRegenIfNeeded();
            remainingLives = Mathf.Clamp(remainingLives + amount, 0, MaxLifeCount);
            if (remainingLives >= MaxLifeCount)
            {
                lifeRechargeStartTicks = 0L;
                NotificationManager.CancelLivesFullNotification();
            }
            else
            {
                if (lifeRechargeStartTicks <= 0L)
                {
                    lifeRechargeStartTicks = DateTime.UtcNow.Ticks;
                }
                NotificationManager.ScheduleLivesFullNotification(DateTime.UtcNow + LifeRechargeInterval);
            }
            SaveState();
        }

        public bool TryBuyLifeRefill(out int spentCoins)
        {
            spentCoins = LifeRefillCoinCost;
            RefreshLifeRegenIfNeeded();
            if (EmberBalance < spentCoins || remainingLives >= MaxLifeCount)
            {
                return false;
            }

            EmberBalance -= spentCoins;
            remainingLives = Mathf.Clamp(remainingLives + LifeRefillAmount, 0, MaxLifeCount);
            if (remainingLives >= MaxLifeCount)
            {
                lifeRechargeStartTicks = 0L;
            }
            SaveState();
            NotifyEmberBalanceChanged();
            return true;
        }

        public bool TryBuyContinue(out int spentCoins)
        {
            spentCoins = ContinueCoinCost;
            if (EmberBalance < spentCoins)
            {
                return false;
            }

            EmberBalance -= spentCoins;
            SaveState();
            NotifyEmberBalanceChanged();
            return true;
        }

        public bool TryRerollDailyMissions(out int spentCoins)
        {
            RefreshDailyContentIfNeeded();
            spentCoins = GetMissionRerollCost();
            if (EmberBalance < spentCoins)
            {
                return false;
            }

            EmberBalance -= spentCoins;
            missionRerollCount++;
            dailyMissions.Clear();
            GenerateDailyMissions(GetTodayKey(), missionRerollCount);
            SaveState();
            NotifyEmberBalanceChanged();
            return true;
        }

        public NextUnlockStatus GetNextUnlockStatus()
        {
            EnsureCatalogBuilt();

            string bestName = string.Empty;
            int bestPrice = int.MaxValue;
            bool bestIsTower = false;

            foreach (BallSkinDefinition skin in skins)
            {
                if (skin.unlockedByDefault || IsOwnedSkin(skin.id))
                {
                    continue;
                }

                if (skin.priceEmber < bestPrice)
                {
                    bestName = skin.displayName;
                    bestPrice = skin.priceEmber;
                    bestIsTower = false;
                }
            }

            foreach (TowerSkinDefinition towerSkin in towerSkins)
            {
                if (towerSkin.unlockedByDefault || IsOwnedTowerSkin(towerSkin.id))
                {
                    continue;
                }

                if (towerSkin.priceEmber < bestPrice)
                {
                    bestName = towerSkin.displayName;
                    bestPrice = towerSkin.priceEmber;
                    bestIsTower = true;
                }
            }

            if (string.IsNullOrWhiteSpace(bestName))
            {
                return new NextUnlockStatus(false, "ALL ITEMS OWNED", 0, 0, false);
            }

            return new NextUnlockStatus(true, bestName, bestPrice, Mathf.Max(0, bestPrice - EmberBalance), bestIsTower);
        }

        public DailyMissionRewardResult RegisterCompletedRun(RunSummary summary)
        {
            RefreshDailyContentIfNeeded();
            int grantedReward = 0;
            int completedMissionCount = 0;

            for (int index = 0; index < dailyMissions.Count; index++)
            {
                DailyMissionState mission = dailyMissions[index];
                if (mission.claimed)
                {
                    continue;
                }

                switch (mission.type)
                {
                    case DailyMissionType.ReachHeightInRun:
                        mission.progressValue = Mathf.Max(mission.progressValue, Mathf.FloorToInt(summary.Height));
                        break;

                    case DailyMissionType.ReachZoneInRun:
                        mission.progressValue = Mathf.Max(mission.progressValue, summary.ZoneNumber);
                        break;

                    case DailyMissionType.CompleteRuns:
                        mission.progressValue += 1;
                        break;

                    case DailyMissionType.SurviveRushEvents:
                        mission.progressValue += summary.RushesSurvived;
                        break;

                    case DailyMissionType.FinishWithoutContinue:
                        if (!summary.UsedContinue && summary.Height >= Mathf.Max(8f, mission.secondaryTargetValue))
                        {
                            mission.progressValue += 1;
                        }
                        break;

                    case DailyMissionType.StayNearLavaSeconds:
                        mission.progressValue += Mathf.FloorToInt(summary.NearLavaSeconds);
                        break;

                    case DailyMissionType.SetNewBest:
                        if (summary.IsNewBest)
                        {
                            mission.progressValue = mission.targetValue;
                        }
                        break;

                    case DailyMissionType.PlayDailyChallenge:
                        if (summary.IsDailyChallenge)
                        {
                            mission.progressValue += 1;
                        }
                        break;

                    case DailyMissionType.ReachHeightInDailyChallenge:
                        if (summary.IsDailyChallenge)
                        {
                            mission.progressValue = Mathf.Max(mission.progressValue, Mathf.FloorToInt(summary.Height));
                        }
                        break;

                    case DailyMissionType.ReachHeightUnderTime:
                        if (summary.Height >= mission.targetValue && summary.RunTime <= Mathf.Max(1, mission.secondaryTargetValue))
                        {
                            mission.progressValue = mission.targetValue;
                        }
                        break;

                    case DailyMissionType.CompleteRunsWithModifier:
                        if (summary.UsesModifier(mission.contextValue))
                        {
                            mission.progressValue += 1;
                        }
                        break;
                }

                // Mission completion no longer auto-grants coins. The reward is only
                // granted when the player taps the claim button (ClaimMissionReward).
                // If they don't claim before the daily reset, the reward is forfeited.
                if (mission.progressValue >= mission.targetValue && !mission.claimed)
                {
                    completedMissionCount++;
                }

                dailyMissions[index] = mission;
            }

            SaveDailyState();
            // Return 0 granted reward — claim is now manual.
            return new DailyMissionRewardResult(0, completedMissionCount);
        }

        public DailyMissionRewardResult ClaimMissionReward(string missionId)
        {
            if (string.IsNullOrEmpty(missionId))
            {
                return new DailyMissionRewardResult(0, 0);
            }

            RefreshDailyContentIfNeeded();
            for (int index = 0; index < dailyMissions.Count; index++)
            {
                DailyMissionState mission = dailyMissions[index];
                if (mission.id != missionId) continue;
                if (mission.claimed) return new DailyMissionRewardResult(0, 0);
                if (mission.progressValue < mission.targetValue) return new DailyMissionRewardResult(0, 0);

                mission.claimed = true;
                dailyMissions[index] = mission;
                int reward = mission.rewardEmber;
                if (reward > 0)
                {
                    GrantEmber(reward);
                }
                SaveDailyState();
                return new DailyMissionRewardResult(reward, 1);
            }
            return new DailyMissionRewardResult(0, 0);
        }

        public DailyChallengeRewardResult RegisterDailyChallengeRun(RunSummary summary)
        {
            RefreshDailyContentIfNeeded();
            DailyChallengeStatus status = dailyChallengeStatus;
            float previousBest = status.bestHeight;
            bool newBest = summary.IsDailyChallenge && summary.Height > status.bestHeight + 0.001f;
            int reward = 0;

            if (!summary.IsDailyChallenge)
            {
                return new DailyChallengeRewardResult(0, false, previousBest, status);
            }

            if (newBest)
            {
                status.bestHeight = summary.Height;
                status.bestTimeSeconds = summary.RunTime;
            }

            if (!status.rewardClaimed && summary.Height >= status.targetHeight)
            {
                reward = status.firstClearReward;
                status.rewardClaimed = true;
            }

            dailyChallengeStatus = status;
            SaveDailyChallengeState();
            return new DailyChallengeRewardResult(reward, newBest, previousBest, status);
        }

        public DailyChestStatus GetDailyChestStatus()
        {
            RefreshDailyContentIfNeeded();
            string today = GetTodayKey();
            int freeReward = GetChestReward(today, false);
            int bonusReward = GetChestReward(today, true);

            if (lastFreeChestClaimDateKey != today)
            {
                return new DailyChestStatus(true, false, "DAILY CHEST", $"+{freeReward} COIN", freeReward);
            }

            if (lastBonusChestClaimDateKey != today)
            {
                return new DailyChestStatus(true, true, "BONUS CHEST", $"WATCH AD  +{bonusReward} COIN", bonusReward);
            }

            return new DailyChestStatus(false, false, "CHEST TOMORROW", "ALL CLAIMED TODAY", 0);
        }

        public int ClaimFreeDailyChest()
        {
            RefreshDailyContentIfNeeded();
            string today = GetTodayKey();
            if (lastFreeChestClaimDateKey == today)
            {
                return 0;
            }

            int reward = GetChestReward(today, false);
            lastFreeChestClaimDateKey = today;
            GrantEmber(reward);
            SaveDailyState();
            return reward;
        }

        public int ClaimBonusDailyChest()
        {
            RefreshDailyContentIfNeeded();
            string today = GetTodayKey();
            if (lastFreeChestClaimDateKey != today || lastBonusChestClaimDateKey == today)
            {
                return 0;
            }

            int reward = GetChestReward(today, true);
            lastBonusChestClaimDateKey = today;
            GrantEmber(reward);
            SaveDailyState();
            return reward;
        }

        public void GrantEmber(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            EmberBalance += amount;
            PlayerPrefs.SetInt(EmberBalanceKey, EmberBalance);
            PlayerPrefs.Save();
            NotifyEmberBalanceChanged();
        }

        public bool IsOwnedSkin(string skinId)
        {
            return !string.IsNullOrWhiteSpace(skinId) && ownedSkinIds.Contains(skinId);
        }

        public BallSkinDefinition GetEquippedSkin()
        {
            return GetSkin(EquippedSkinId);
        }

        public BallSkinDefinition GetSkin(string skinId)
        {
            for (int index = 0; index < skins.Count; index++)
            {
                if (skins[index].id == skinId)
                {
                    return skins[index];
                }
            }

            return skins.Count > 0 ? skins[0] : default;
        }

        public bool IsOwnedTowerSkin(string skinId)
        {
            return !string.IsNullOrWhiteSpace(skinId) && ownedTowerSkinIds.Contains(skinId);
        }

        public TowerSkinDefinition GetEquippedTowerSkin()
        {
            return GetTowerSkin(EquippedTowerSkinId);
        }

        public TowerSkinDefinition GetTowerSkin(string skinId)
        {
            for (int index = 0; index < towerSkins.Count; index++)
            {
                if (towerSkins[index].id == skinId)
                {
                    return towerSkins[index];
                }
            }

            return towerSkins.Count > 0 ? towerSkins[0] : default;
        }

        public ShopActionResult PurchaseOrEquipSkin(string skinId)
        {
            BallSkinDefinition skin = GetSkin(skinId);
            if (string.IsNullOrWhiteSpace(skin.id))
            {
                return ShopActionResult.None;
            }

            if (!IsOwnedSkin(skin.id))
            {
                if (EmberBalance < skin.priceEmber)
                {
                    return ShopActionResult.InsufficientFunds;
                }

                EmberBalance -= skin.priceEmber;
                ownedSkinIds.Add(skin.id);
                EquippedSkinId = skin.id;
                SaveState();
                NotifyEmberBalanceChanged();
                return ShopActionResult.Purchased;
            }

            if (EquippedSkinId == skin.id)
            {
                return ShopActionResult.None;
            }

            EquippedSkinId = skin.id;
            SaveState();
            return ShopActionResult.Equipped;
        }

        public ShopActionResult PurchaseOrEquipTowerSkin(string skinId)
        {
            TowerSkinDefinition towerSkin = GetTowerSkin(skinId);
            if (string.IsNullOrWhiteSpace(towerSkin.id))
            {
                return ShopActionResult.None;
            }

            if (!IsOwnedTowerSkin(towerSkin.id))
            {
                if (EmberBalance < towerSkin.priceEmber)
                {
                    return ShopActionResult.InsufficientFunds;
                }

                EmberBalance -= towerSkin.priceEmber;
                ownedTowerSkinIds.Add(towerSkin.id);
                EquippedTowerSkinId = towerSkin.id;
                SaveState();
                NotifyEmberBalanceChanged();
                NotifyTowerSkinChanged();
                return ShopActionResult.Purchased;
            }

            if (EquippedTowerSkinId == towerSkin.id)
            {
                return ShopActionResult.None;
            }

            EquippedTowerSkinId = towerSkin.id;
            SaveState();
            NotifyTowerSkinChanged();
            return ShopActionResult.Equipped;
        }

        public bool GrantSkinOwnership(string skinId, bool equip = false)
        {
            BallSkinDefinition skin = GetSkin(skinId);
            if (string.IsNullOrWhiteSpace(skin.id))
            {
                return false;
            }

            bool changed = ownedSkinIds.Add(skin.id);
            if (equip)
            {
                EquippedSkinId = skin.id;
            }

            if (changed || equip)
            {
                SaveState();
            }

            return changed || equip;
        }

        public bool GrantTowerSkinOwnership(string skinId, bool equip = false)
        {
            TowerSkinDefinition towerSkin = GetTowerSkin(skinId);
            if (string.IsNullOrWhiteSpace(towerSkin.id))
            {
                return false;
            }

            bool changed = ownedTowerSkinIds.Add(towerSkin.id);
            if (equip)
            {
                EquippedTowerSkinId = towerSkin.id;
            }

            if (changed || equip)
            {
                SaveState();
                NotifyTowerSkinChanged();
            }

            return changed || equip;
        }

        private void BuildCatalog()
        {
            if (skins.Count > 0 && towerSkins.Count > 0)
            {
                return;
            }

            if (skins.Count == 0)
            {
                skins.Add(new BallSkinDefinition(
                    "obsidian",
                    "Obsidian Vein",
                    0,
                    new Color(0.08f, 0.07f, 0.08f, 1f),
                    new Color(1f, 0.35f, 0.1f, 1f),
                    true,
                    "TowerMaze/BallSkins/Marble013/Marble013_2K-JPG_Color",
                    "TowerMaze/BallSkins/Marble013/Marble013_2K-JPG_NormalGL",
                    textureScale: new Vector2(2.4f, 2.4f),
                    metallic: 0.08f,
                    smoothness: 0.9f,
                    normalStrength: 0.75f,
                    emissionIntensity: 1.05f));
                skins.Add(new BallSkinDefinition(
                    "molten_core",
                    "Molten Core",
                    4000,
                    new Color(0.42f, 0.12f, 0.06f, 1f),
                    new Color(1f, 0.44f, 0.14f, 1f),
                    baseMapResourcePath: "TowerMaze/BallSkins/Lava004/Lava004_2K-JPG_Color",
                    normalMapResourcePath: "TowerMaze/BallSkins/Lava004/Lava004_2K-JPG_NormalGL",
                    emissionMapResourcePath: "TowerMaze/BallSkins/Lava004/Lava004_2K-JPG_Emission",
                    textureScale: new Vector2(1.8f, 1.8f),
                    metallic: 0.02f,
                    smoothness: 0.82f,
                    normalStrength: 0.9f,
                    emissionIntensity: 1.35f));
                skins.Add(new BallSkinDefinition(
                    "ash_marble",
                    "Ash Marble",
                    6500,
                    new Color(0.35f, 0.35f, 0.38f, 1f),
                    new Color(1f, 0.72f, 0.4f, 1f),
                    baseMapResourcePath: "TowerMaze/BallSkins/Marble004/Marble004_2K-JPG_Color",
                    normalMapResourcePath: "TowerMaze/BallSkins/Marble004/Marble004_2K-JPG_NormalGL",
                    textureScale: new Vector2(2.1f, 2.1f),
                    metallic: 0.03f,
                    smoothness: 0.86f,
                    normalStrength: 0.78f,
                    emissionIntensity: 0.95f));
                skins.Add(new BallSkinDefinition(
                    "hazard_neon",
                    "Hazard Neon",
                    9000,
                    new Color(1f, 1f, 1f, 1f),
                    new Color(0.42f, 0.96f, 1f, 1f),
                    baseMapResourcePath: "TowerMaze/BallSkins/Neon/NeonBaseMap",
                    normalMapResourcePath: "",
                    emissionMapResourcePath: "TowerMaze/BallSkins/Neon/NeonBaseMap",
                    textureScale: new Vector2(0.65f, 0.65f),
                    metallic: 0.05f,
                    smoothness: 0.92f,
                    normalStrength: 0f,
                    emissionIntensity: 2.4f));
                skins.Add(new BallSkinDefinition(
                    "forge_bronze",
                    "Forge Bronze",
                    12000,
                    new Color(0.42f, 0.26f, 0.14f, 1f),
                    new Color(1f, 0.68f, 0.3f, 1f),
                    baseMapResourcePath: "TowerMaze/BallSkins/Metal043A/Metal043A_2K-JPG_Color",
                    normalMapResourcePath: "TowerMaze/BallSkins/Metal043A/Metal043A_2K-JPG_NormalGL",
                    textureScale: new Vector2(3f, 3f),
                    metallic: 0.92f,
                    smoothness: 0.82f,
                    normalStrength: 1f,
                    emissionIntensity: 1.15f));
                skins.Add(new BallSkinDefinition(
                    "relic_gold",
                    "Relic Gold",
                    16000,
                    new Color(0.6f, 0.46f, 0.2f, 1f),
                    new Color(1f, 0.84f, 0.3f, 1f),
                    baseMapResourcePath: "TowerMaze/BallSkins/Metal044A/Metal044A_2K-JPG_Color",
                    normalMapResourcePath: "TowerMaze/BallSkins/Metal044A/Metal044A_2K-JPG_NormalGL",
                    textureScale: new Vector2(3.2f, 3.2f),
                    metallic: 0.98f,
                    smoothness: 0.88f,
                    normalStrength: 1.05f,
                    emissionIntensity: 1.2f));
                skins.Add(new BallSkinDefinition(
                    "void_ice",
                    "Void Ice",
                    22000,
                    new Color(0.6f, 0.7f, 0.8f, 1f),
                    new Color(0.4f, 0.86f, 1f, 1f),
                    baseMapResourcePath: "TowerMaze/BallSkins/Snow003/Snow003_2K-JPG_Color",
                    normalMapResourcePath: "TowerMaze/BallSkins/Snow003/Snow003_2K-JPG_NormalGL",
                    textureScale: new Vector2(2.5f, 2.5f),
                    metallic: 0.04f,
                    smoothness: 0.72f,
                    normalStrength: 0.82f,
                    emissionIntensity: 1.08f));
                // ─── PREMIUM IAP-ONLY SKINS ───────────────────────────────
                skins.Add(new BallSkinDefinition(
                    "solar_crown",
                    "Solar Crown",
                    0,
                    new Color(1f, 0.84f, 0.28f, 1f),
                    new Color(1f, 0.78f, 0.10f, 1f),
                    baseMapResourcePath: "TowerMaze/BallSkins/Gold/GoldBaseMap",
                    normalMapResourcePath: "",
                    textureScale: new Vector2(1.2f, 1.2f),
                    metallic: 0.85f,
                    smoothness: 0.95f,
                    normalStrength: 0f,
                    emissionIntensity: 3.5f,
                    iapProductId: "towermaze.skin.solar_crown"));
                skins.Add(new BallSkinDefinition(
                    "dark_sovereign",
                    "Dark Sovereign",
                    0,
                    new Color(0.16f, 0.12f, 0.20f, 1f),
                    new Color(0.62f, 0.18f, 1f, 1f),
                    baseMapResourcePath: "TowerMaze/BallSkins/Plastic006/Plastic006_2K-JPG_Color",
                    normalMapResourcePath: "TowerMaze/BallSkins/Plastic006/Plastic006_2K-JPG_NormalGL",
                    textureScale: new Vector2(2.4f, 2.4f),
                    metallic: 0.65f,
                    smoothness: 0.98f,
                    normalStrength: 0.6f,
                    emissionIntensity: 2.8f,
                    iapProductId: "towermaze.skin.dark_sovereign"));

                // --- NEW PREMIUM BALL SKINS ---
                skins.Add(new BallSkinDefinition(
                    "silver_mirror",
                    "Silver Mirror",
                    0,
                    new Color(0.92f, 0.94f, 0.98f, 1f),
                    new Color(0.55f, 0.75f, 1f, 1f),
                    baseMapResourcePath: "TowerMaze/BallSkins/Silver/SilverBaseMap",
                    textureScale: new Vector2(1f, 1f),
                    metallic: 0.98f,
                    smoothness: 0.98f,
                    normalStrength: 0f,
                    emissionIntensity: 0.7f,
                    iapProductId: "towermaze.skin.silver"));

                skins.Add(new BallSkinDefinition(
                    "golden_glory",
                    "Golden Glory",
                    0,
                    new Color(1f, 0.78f, 0.18f, 1f),
                    Color.black,
                    baseMapResourcePath: "TowerMaze/BallSkins/Metal044A/Metal044A_2K-JPG_Color",
                    normalMapResourcePath: "TowerMaze/BallSkins/Metal044A/Metal044A_2K-JPG_NormalGL",
                    textureScale: new Vector2(2.2f, 2.2f),
                    metallic: 0.95f,
                    smoothness: 0.92f,
                    normalStrength: 0.85f,
                    emissionIntensity: 0.4f,
                    iapProductId: "towermaze.skin.gold"));

                skins.Add(new BallSkinDefinition(
                    "checker_classic",
                    "Checker Classic",
                    7500,
                    Color.white,
                    Color.black,
                    baseMapResourcePath: "TowerMaze/BallSkins/Checker/CheckerBaseMap",
                    metallic: 0f,
                    smoothness: 0.5f));
                
                skins.Add(new BallSkinDefinition(
                    "neon_ball",
                    "Neon Pro",
                    0,
                    new Color(1f, 0.65f, 0.95f, 1f),
                    new Color(1f, 0.18f, 0.78f, 1f),
                    baseMapResourcePath: "TowerMaze/BallSkins/Neon/NeonBaseMap",
                    normalMapResourcePath: "",
                    emissionMapResourcePath: "TowerMaze/BallSkins/Neon/NeonBaseMap",
                    textureScale: new Vector2(0.55f, 0.55f),
                    metallic: 0.05f,
                    smoothness: 0.94f,
                    normalStrength: 0f,
                    emissionIntensity: 2.6f,
                    iapProductId: "towermaze.bundle.neon_pro"));
            }

            if (towerSkins.Count == 0)
            {
                towerSkins.Add(new TowerSkinDefinition(
                    "vault_core",
                    "Vault Core",
                    0,
                    new Color(0.54f, 0.37f, 0.16f, 1f),
                    new Color(0.86f, 0.68f, 0.2f, 1f),
                    new Color(1f, 0.86f, 0.34f, 1f),
                    true));
                towerSkins.Add(new TowerSkinDefinition(
                    "obsidian_gate",
                    "Obsidian Gate",
                    7000,
                    new Color(0.16f, 0.16f, 0.18f, 1f),
                    new Color(0.44f, 0.45f, 0.5f, 1f),
                    new Color(0.78f, 0.84f, 0.9f, 1f),
                    wallBaseMapResourcePath: "TowerMaze/BallSkins/Marble013/Marble013_2K-JPG_Color",
                    wallNormalMapResourcePath: "TowerMaze/BallSkins/Marble013/Marble013_2K-JPG_NormalGL",
                    wallTextureScale: new Vector2(1.6f, 0.8f),
                    pathBaseMapResourcePath: "TowerMaze/BallSkins/Marble004/Marble004_2K-JPG_Color",
                    pathNormalMapResourcePath: "TowerMaze/BallSkins/Marble004/Marble004_2K-JPG_NormalGL",
                    pathTextureScale: new Vector2(1.85f, 0.95f),
                    mainPathBaseMapResourcePath: "TowerMaze/BallSkins/Metal043A/Metal043A_2K-JPG_Color",
                    mainPathNormalMapResourcePath: "TowerMaze/BallSkins/Metal043A/Metal043A_2K-JPG_NormalGL",
                    mainPathTextureScale: new Vector2(2.1f, 1f)));
                towerSkins.Add(new TowerSkinDefinition(
                    "forge_spire",
                    "Forge Spire",
                    12000,
                    new Color(0.56f, 0.34f, 0.18f, 1f),
                    new Color(0.84f, 0.56f, 0.22f, 1f),
                    new Color(1f, 0.78f, 0.32f, 1f),
                    wallBaseMapResourcePath: "TowerMaze/BallSkins/Metal043A/Metal043A_2K-JPG_Color",
                    wallNormalMapResourcePath: "TowerMaze/BallSkins/Metal043A/Metal043A_2K-JPG_NormalGL",
                    wallTextureScale: new Vector2(2f, 0.85f),
                    pathBaseMapResourcePath: "TowerMaze/BallSkins/Metal044A/Metal044A_2K-JPG_Color",
                    pathNormalMapResourcePath: "TowerMaze/BallSkins/Metal044A/Metal044A_2K-JPG_NormalGL",
                    pathTextureScale: new Vector2(2.15f, 1f),
                    mainPathBaseMapResourcePath: "TowerMaze/BallSkins/Lava004/Lava004_2K-JPG_Color",
                    mainPathNormalMapResourcePath: "TowerMaze/BallSkins/Lava004/Lava004_2K-JPG_NormalGL",
                    mainPathTextureScale: new Vector2(1.65f, 0.9f)));
                towerSkins.Add(new TowerSkinDefinition(
                    "frost_keep",
                    "Frost Keep",
                    18000,
                    new Color(0.3f, 0.4f, 0.48f, 1f),
                    new Color(0.72f, 0.88f, 0.96f, 1f),
                    new Color(0.92f, 0.97f, 1f, 1f),
                    wallBaseMapResourcePath: "TowerMaze/BallSkins/Snow003/Snow003_2K-JPG_Color",
                    wallNormalMapResourcePath: "TowerMaze/BallSkins/Snow003/Snow003_2K-JPG_NormalGL",
                    wallTextureScale: new Vector2(1.5f, 0.76f),
                    pathBaseMapResourcePath: "TowerMaze/BallSkins/Marble004/Marble004_2K-JPG_Color",
                    pathNormalMapResourcePath: "TowerMaze/BallSkins/Marble004/Marble004_2K-JPG_NormalGL",
                    pathTextureScale: new Vector2(1.9f, 0.92f),
                    mainPathBaseMapResourcePath: "TowerMaze/BallSkins/Snow003/Snow003_2K-JPG_Color",
                    mainPathNormalMapResourcePath: "TowerMaze/BallSkins/Snow003/Snow003_2K-JPG_NormalGL",
                    mainPathTextureScale: new Vector2(1.85f, 0.9f)));
                // ─── PREMIUM IAP-ONLY TOWER SKINS ────────────────────────
                towerSkins.Add(new TowerSkinDefinition(
                    "gilded_sanctum",
                    "Gilded Sanctum",
                    0,
                    new Color(0.68f, 0.50f, 0.08f, 1f),
                    new Color(0.86f, 0.70f, 0.18f, 1f),
                    new Color(1f, 0.88f, 0.32f, 1f),
                    wallBaseMapResourcePath: "TowerMaze/BallSkins/Metal044A/Metal044A_2K-JPG_Color",
                    wallNormalMapResourcePath: "TowerMaze/BallSkins/Metal044A/Metal044A_2K-JPG_NormalGL",
                    wallTextureScale: new Vector2(2.2f, 1.0f),
                    pathBaseMapResourcePath: "TowerMaze/BallSkins/Metal044A/Metal044A_2K-JPG_Color",
                    pathNormalMapResourcePath: "TowerMaze/BallSkins/Metal044A/Metal044A_2K-JPG_NormalGL",
                    pathTextureScale: new Vector2(2.4f, 1.1f),
                    mainPathBaseMapResourcePath: "TowerMaze/BallSkins/Metal044A/Metal044A_2K-JPG_Color",
                    mainPathNormalMapResourcePath: "TowerMaze/BallSkins/Metal044A/Metal044A_2K-JPG_NormalGL",
                    mainPathTextureScale: new Vector2(2.6f, 1.1f),
                    iapProductId: "towermaze.skin.gilded_sanctum"));
                towerSkins.Add(new TowerSkinDefinition(
                    "shadow_citadel",
                    "Shadow Citadel",
                    0,
                    new Color(0.05f, 0.04f, 0.08f, 1f),
                    new Color(0.12f, 0.10f, 0.18f, 1f),
                    new Color(0.5f, 0.1f, 0.82f, 1f),
                    wallBaseMapResourcePath: "TowerMaze/BallSkins/Metal043A/Metal043A_2K-JPG_Color",
                    wallNormalMapResourcePath: "TowerMaze/BallSkins/Metal043A/Metal043A_2K-JPG_NormalGL",
                    wallTextureScale: new Vector2(2.0f, 0.9f),
                    pathBaseMapResourcePath: "TowerMaze/BallSkins/Marble013/Marble013_2K-JPG_Color",
                    pathNormalMapResourcePath: "TowerMaze/BallSkins/Marble013/Marble013_2K-JPG_NormalGL",
                    pathTextureScale: new Vector2(1.8f, 0.85f),
                    mainPathBaseMapResourcePath: "TowerMaze/BallSkins/Metal044A/Metal044A_2K-JPG_Color",
                    mainPathNormalMapResourcePath: "TowerMaze/BallSkins/Metal044A/Metal044A_2K-JPG_NormalGL",
                    mainPathTextureScale: new Vector2(2.1f, 0.95f),
                    iapProductId: "towermaze.skin.shadow_citadel"));
                // ─── COIN-PURCHASABLE THEMES ─────────────────────────────
                towerSkins.Add(new TowerSkinDefinition(
                    "grass_meadow",
                    "Grass Meadow",
                    24000,
                    new Color(0.6f, 0.9f, 0.4f, 1f),       // Wall tint (Greenish)
                    new Color(0.65f, 0.55f, 0.45f, 1f),    // Path tint (Soil Brown)
                    new Color(0.55f, 0.45f, 0.35f, 1f),    // Main Path tint (Darker Soil)
                    unlockedByDefault: false,
                    useUnifiedTextureSet: false,
                    wallBaseMapResourcePath: "TowerMaze/TowerSkins/GrassTheme/GrassBaseMap",
                    wallTextureScale: new Vector2(2.5f, 1.25f),
                    pathBaseMapResourcePath: "TowerMaze/BallSkins/Marble013/Marble013_2K-JPG_Color",
                    pathNormalMapResourcePath: "TowerMaze/BallSkins/Marble013/Marble013_2K-JPG_NormalGL",
                    pathTextureScale: new Vector2(2.0f, 1.0f),
                    mainPathBaseMapResourcePath: "TowerMaze/BallSkins/Marble013/Marble013_2K-JPG_Color",
                    mainPathNormalMapResourcePath: "TowerMaze/BallSkins/Marble013/Marble013_2K-JPG_NormalGL",
                    mainPathTextureScale: new Vector2(2.2f, 1.1f)));

                towerSkins.Add(new TowerSkinDefinition(
                    "magma_forge",
                    "Magma Forge",
                    24000,
                    new Color(1.0f, 0.6f, 0.2f, 1f),       // Wall tint (Magma Orange)
                    new Color(0.15f, 0.12f, 0.1f, 1f),     // Path tint (Dark Obsidian)
                    new Color(0.1f, 0.08f, 0.06f, 1f),     // Main Path tint (Deeper Obsidian)
                    unlockedByDefault: false,
                    useUnifiedTextureSet: false,
                    wallBaseMapResourcePath: "TowerMaze/TowerSkins/MagmaForge/MagmaForge",
                    wallTextureScale: new Vector2(2.0f, 1.0f),
                    pathBaseMapResourcePath: "TowerMaze/BallSkins/Metal043A/Metal043A_2K-JPG_Color",
                    pathNormalMapResourcePath: "TowerMaze/BallSkins/Metal043A/Metal043A_2K-JPG_NormalGL",
                    pathTextureScale: new Vector2(1.8f, 0.9f),
                    mainPathBaseMapResourcePath: "TowerMaze/BallSkins/Metal043A/Metal043A_2K-JPG_Color",
                    mainPathNormalMapResourcePath: "TowerMaze/BallSkins/Metal043A/Metal043A_2K-JPG_NormalGL",
                    mainPathTextureScale: new Vector2(2.0f, 1.0f)));

                // --- NEW PREMIUM TOWER SKINS ---
                towerSkins.Add(new TowerSkinDefinition(
                    "silver_spire",
                    "Silver Spire",
                    0,
                    new Color(0.9f, 0.9f, 1.0f, 1f),
                    new Color(0.85f, 0.85f, 0.9f, 1f),
                    new Color(0.8f, 0.8f, 0.85f, 1f),
                    unlockedByDefault: false,
                    useUnifiedTextureSet: true,
                    wallBaseMapResourcePath: "TowerMaze/BallSkins/Silver/SilverBaseMap",
                    pathBaseMapResourcePath: "TowerMaze/BallSkins/Silver/SilverBaseMap",
                    mainPathBaseMapResourcePath: "TowerMaze/BallSkins/Silver/SilverBaseMap",
                    iapProductId: "towermaze.skin.silver_tower"));

                towerSkins.Add(new TowerSkinDefinition(
                    "golden_bastion",
                    "Golden Bastion",
                    0,
                    new Color(1f, 0.84f, 0.2f, 1f),
                    new Color(0.9f, 0.7f, 0.1f, 1f),
                    new Color(0.8f, 0.6f, 0.05f, 1f),
                    unlockedByDefault: false,
                    useUnifiedTextureSet: true,
                    wallBaseMapResourcePath: "TowerMaze/BallSkins/Gold/GoldBaseMap",
                    pathBaseMapResourcePath: "TowerMaze/BallSkins/Gold/GoldBaseMap",
                    mainPathBaseMapResourcePath: "TowerMaze/BallSkins/Gold/GoldBaseMap",
                    iapProductId: "towermaze.skin.gold_tower"));

                towerSkins.Add(new TowerSkinDefinition(
                    "checker_fortress",
                    "Checker Fortress",
                    7000,
                    Color.white,
                    new Color(0.9f, 0.9f, 0.9f, 1f),
                    new Color(0.8f, 0.8f, 0.8f, 1f),
                    unlockedByDefault: false,
                    useUnifiedTextureSet: true,
                    wallBaseMapResourcePath: "TowerMaze/BallSkins/Checker/CheckerBaseMap",
                    pathBaseMapResourcePath: "TowerMaze/BallSkins/Checker/CheckerBaseMap",
                    mainPathBaseMapResourcePath: "TowerMaze/BallSkins/Checker/CheckerBaseMap"));
            }
        }

        private void EnsureCatalogBuilt()
        {
            if (skins.Count == 0 || towerSkins.Count == 0)
            {
                BuildCatalog();
            }
        }

        private void LoadOwnedSkins()
        {
            ownedSkinIds.Clear();
            string json = PlayerPrefs.GetString(OwnedSkinsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            SkinInventorySaveData saveData = JsonUtility.FromJson<SkinInventorySaveData>(json);
            if (saveData?.ownedSkinIds == null)
            {
                return;
            }

            foreach (string skinId in saveData.ownedSkinIds)
            {
                if (!string.IsNullOrWhiteSpace(skinId))
                {
                    ownedSkinIds.Add(skinId);
                }
            }
        }

        private void LoadOwnedTowerSkins()
        {
            ownedTowerSkinIds.Clear();
            string json = PlayerPrefs.GetString(OwnedTowerSkinsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            SkinInventorySaveData saveData = JsonUtility.FromJson<SkinInventorySaveData>(json);
            if (saveData?.ownedSkinIds == null)
            {
                return;
            }

            foreach (string skinId in saveData.ownedSkinIds)
            {
                if (!string.IsNullOrWhiteSpace(skinId))
                {
                    ownedTowerSkinIds.Add(skinId);
                }
            }
        }

        private void LoadDailyState()
        {
            dailyMissions.Clear();
            DailyMissionSaveData saveData = JsonUtility.FromJson<DailyMissionSaveData>(PlayerPrefs.GetString(DailyMissionKey, string.Empty));
            if (saveData == null)
            {
                return;
            }

            dailyDateKey = saveData.dateKey;
            lastFreeChestClaimDateKey = saveData.lastFreeChestClaimDateKey;
            lastBonusChestClaimDateKey = saveData.lastBonusChestClaimDateKey;
            missionRerollCount = saveData.rerollCount;
            if (saveData.missions != null)
            {
                dailyMissions.AddRange(saveData.missions);
            }
        }

        private void LoadDailyChallengeState()
        {
            DailyChallengeSaveData saveData = JsonUtility.FromJson<DailyChallengeSaveData>(PlayerPrefs.GetString(DailyChallengeKey, string.Empty));
            if (saveData == null)
            {
                dailyChallengeStatus = default;
                return;
            }

            dailyChallengeStatus = saveData.status;
        }

        private void RefreshDailyContentIfNeeded()
        {
            string today = GetTodayKey();
            bool missionsDirty = dailyDateKey != today || dailyMissions.Count < 4;
            bool challengeDirty = dailyChallengeStatus.dateKey != today || dailyChallengeStatus.seed == 0;

            if (challengeDirty)
            {
                GenerateDailyChallenge(today);
                SaveDailyChallengeState();
            }

            if (missionsDirty)
            {
                dailyDateKey = today;
                missionRerollCount = 0;
                dailyMissions.Clear();
                GenerateDailyMissions(today, 0);
                SaveDailyState();
            }

            if (dailyChallengeStatus.firstClearReward <= 0)
            {
                int[] rewardTiers = { 25, 50, 75, 100 };
                int seed = HashSeedFromText(dailyChallengeStatus.dateKey ?? string.Empty, 409);
                dailyChallengeStatus.firstClearReward = rewardTiers[Mathf.Abs(seed) % rewardTiers.Length];
                SaveDailyChallengeState();
            }
        }

        private void GenerateDailyMissions(string dateKey, int rerollSalt)
        {
            List<DailyMissionState> pool = BuildDailyMissionPool(dateKey, rerollSalt);
            if (pool.Count == 0)
            {
                return;
            }

            System.Random random = new(HashSeedFromText(dateKey, 173 + (rerollSalt * 41)));
            while (dailyMissions.Count < 4 && pool.Count > 0)
            {
                int selection = random.Next(0, pool.Count);
                dailyMissions.Add(pool[selection]);
                pool.RemoveAt(selection);
            }
        }

        private void SaveDailyState()
        {
            DailyMissionSaveData saveData = new()
            {
                dateKey = dailyDateKey,
                missions = new List<DailyMissionState>(dailyMissions),
                lastFreeChestClaimDateKey = lastFreeChestClaimDateKey,
                lastBonusChestClaimDateKey = lastBonusChestClaimDateKey,
                rerollCount = missionRerollCount
            };
            PlayerPrefs.SetString(DailyMissionKey, JsonUtility.ToJson(saveData));
            PlayerPrefs.Save();
            NotifyStateChanged();
        }

        private void SaveDailyChallengeState()
        {
            DailyChallengeSaveData saveData = new()
            {
                status = dailyChallengeStatus
            };
            PlayerPrefs.SetString(DailyChallengeKey, JsonUtility.ToJson(saveData));
            PlayerPrefs.Save();
            NotifyStateChanged();
        }

        private static int GetChestReward(string dateKey, bool bonus)
        {
            int seed = bonus ? 97 : 59;
            for (int index = 0; index < dateKey.Length; index++)
            {
                seed = (seed * 31) + dateKey[index];
            }

            System.Random random = new(seed);
            int[] tiers = { 25, 50, 75, 100 };
            return bonus ? tiers[random.Next(0, tiers.Length)] : 140 + random.Next(0, 61);
        }

        private static string GetTodayKey()
        {
            DateTime today = DateTime.UtcNow;
            int daysToMonday = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            DateTime monday = today.AddDays(-daysToMonday);
            return monday.ToString("yyyyMMdd");
        }

        // Returns 0..8 based on how many weeks the player has been playing.
        // Week 0 = first week ever → easiest targets. Week 8+ = max difficulty.
        private static int GetDifficultyLevel(string weekKey)
        {
            const string FirstWeekPrefKey = "TM_FirstWeekKey";
            string firstWeek = PlayerPrefs.GetString(FirstWeekPrefKey, string.Empty);
            if (string.IsNullOrEmpty(firstWeek))
            {
                PlayerPrefs.SetString(FirstWeekPrefKey, weekKey);
                PlayerPrefs.Save();
                return 0;
            }

            if (DateTime.TryParseExact(weekKey, "yyyyMMdd", null,
                    System.Globalization.DateTimeStyles.None, out DateTime current) &&
                DateTime.TryParseExact(firstWeek, "yyyyMMdd", null,
                    System.Globalization.DateTimeStyles.None, out DateTime first))
            {
                int weeks = (int)(current - first).TotalDays / 7;
                return Mathf.Clamp(weeks, 0, 8);
            }

            return 0;
        }

        private void GenerateDailyChallenge(string dateKey)
        {
            int seed = HashSeedFromText(dateKey, 409);
            int targetHeight = 28 + Mathf.Abs(seed % 11) * 2;
            RunModifierType primaryModifier = (Mathf.Abs(seed) % 2) == 0 ? RunModifierType.Slipstream : RunModifierType.HighStakes;
            RunModifierType secondaryModifier = primaryModifier == RunModifierType.Slipstream ? RunModifierType.HighStakes : RunModifierType.Slipstream;
            int[] rewardTiers = { 25, 50, 75, 100 };
            int firstClearReward = rewardTiers[Mathf.Abs(seed) % rewardTiers.Length];
            dailyChallengeStatus = new DailyChallengeStatus
            {
                dateKey = dateKey,
                seed = seed == int.MinValue ? int.MaxValue : Mathf.Abs(seed),
                targetHeight = targetHeight,
                firstClearReward = firstClearReward,
                bestHeight = 0f,
                bestTimeSeconds = 0f,
                rewardClaimed = false,
                primaryModifier = primaryModifier,
                secondaryModifier = secondaryModifier,
            };
        }

        private List<DailyMissionState> BuildDailyMissionPool(string dateKey, int rerollSalt)
        {
            System.Random random = new(HashSeedFromText(dateKey, 61 + (rerollSalt * 59)));
            List<DailyMissionState> pool = new();
            int challengeTarget = dailyChallengeStatus.targetHeight > 0 ? dailyChallengeStatus.targetHeight : 28;
            RunModifierType featuredModifier = random.Next(0, 2) == 0 ? RunModifierType.Slipstream : RunModifierType.HighStakes;

            int[] coinTiers = { 100, 150, 200 };
            int diff = GetDifficultyLevel(dateKey);

            // At diff=0 (week 1) the full array is available.
            // At diff=8 (week 8+) only the harder top half is selectable.
            int Pick(int[] arr)
            {
                int lo = diff * (arr.Length / 2) / 8;
                return arr[lo + random.Next(0, arr.Length - lo)];
            }

            int heightTarget = Pick(new[] { 20, 24, 28, 32, 36, 40 });
            pool.Add(CreateMission(dateKey, "height", DailyMissionType.ReachHeightInRun, $"Reach {heightTarget}m in one run", heightTarget, 0, coinTiers[random.Next(0, 3)]));

            int zoneTarget = Pick(new[] { 2, 3, 4, 5, 6 });
            pool.Add(CreateMission(dateKey, "zone", DailyMissionType.ReachZoneInRun, $"Reach Zone {zoneTarget}", zoneTarget, 0, coinTiers[random.Next(0, 3)]));

            int runsTarget = Pick(new[] { 3, 4, 5, 6, 7 });
            pool.Add(CreateMission(dateKey, "runs", DailyMissionType.CompleteRuns, $"Complete {runsTarget} runs", runsTarget, 0, coinTiers[random.Next(0, 3)]));

            int rushTarget = Pick(new[] { 1, 2, 3, 4 });
            pool.Add(CreateMission(dateKey, "rush", DailyMissionType.SurviveRushEvents, $"Survive {rushTarget} rushes", rushTarget, 0, coinTiers[random.Next(0, 3)]));

            int cleanRuns = Pick(new[] { 1, 2, 3 });
            int cleanHeight = Pick(new[] { 10, 14, 18 });
            pool.Add(CreateMission(dateKey, "clean", DailyMissionType.FinishWithoutContinue, $"Finish {cleanRuns} runs above {cleanHeight}m without continue", cleanRuns, cleanHeight, coinTiers[random.Next(0, 3)]));

            int lavaSeconds = Pick(new[] { 4, 6, 8, 10, 12 });
            pool.Add(CreateMission(dateKey, "lava", DailyMissionType.StayNearLavaSeconds, $"Stay near lava for {lavaSeconds}s", lavaSeconds, 0, coinTiers[random.Next(0, 3)]));

            pool.Add(CreateMission(dateKey, "best", DailyMissionType.SetNewBest, "Set a new best", 1, 0, coinTiers[random.Next(0, 3)]));

            pool.Add(CreateMission(dateKey, "challenge_runs", DailyMissionType.PlayDailyChallenge, "Play the daily challenge", 1, 0, coinTiers[random.Next(0, 3)]));

            pool.Add(CreateMission(dateKey, "challenge_height", DailyMissionType.ReachHeightInDailyChallenge, $"Reach {challengeTarget}m in daily challenge", challengeTarget, 0, coinTiers[random.Next(0, 3)]));

            int speedHeight = Pick(new[] { 18, 22, 26, 30 });
            int speedTime = Pick(new[] { 22, 20, 18, 16 }); // reversed: higher diff → lower time limit (harder)
            pool.Add(CreateMission(dateKey, "speed", DailyMissionType.ReachHeightUnderTime, $"Reach {speedHeight}m under {speedTime}s", speedHeight, speedTime, coinTiers[random.Next(0, 3)]));

            int modifierRuns = Pick(new[] { 1, 2, 3 });
            pool.Add(CreateMission(
                dateKey,
                "modifier",
                DailyMissionType.CompleteRunsWithModifier,
                $"Finish {modifierRuns} {GetModifierDisplayName(featuredModifier)} run{(modifierRuns > 1 ? "s" : string.Empty)}",
                modifierRuns,
                0,
                coinTiers[random.Next(0, 3)],
                featuredModifier.ToString()));

            return pool;
        }

        private static DailyMissionState CreateMission(string dateKey, string suffix, DailyMissionType type, string description, int targetValue, int secondaryTargetValue, int reward, string contextValue = "")
        {
            return new DailyMissionState
            {
                id = $"{dateKey}_{suffix}",
                type = type,
                description = description,
                targetValue = targetValue,
                progressValue = 0,
                secondaryTargetValue = secondaryTargetValue,
                rewardEmber = reward,
                contextValue = contextValue,
                claimed = false
            };
        }

        private static int HashSeedFromText(string text, int baseSeed)
        {
            int seed = baseSeed;
            for (int index = 0; index < text.Length; index++)
            {
                seed = (seed * 31) + text[index];
            }

            return seed;
        }

        public static string GetModifierDisplayName(RunModifierType modifier)
        {
            return modifier switch
            {
                RunModifierType.Slipstream => "Slipstream",
                RunModifierType.HighStakes => "High Stakes",
                _ => "None",
            };
        }

        private void SaveState()
        {
            PlayerPrefs.SetInt(EmberBalanceKey, EmberBalance);
            PlayerPrefs.SetInt(RemainingLivesKey, remainingLives);
            PlayerPrefs.SetString(LifeRechargeStartTicksKey, lifeRechargeStartTicks.ToString());
            PlayerPrefs.SetString(EquippedSkinKey, EquippedSkinId ?? string.Empty);
            PlayerPrefs.SetString(EquippedTowerSkinKey, EquippedTowerSkinId ?? string.Empty);
            SkinInventorySaveData saveData = new()
            {
                ownedSkinIds = new List<string>(ownedSkinIds)
            };
            PlayerPrefs.SetString(OwnedSkinsKey, JsonUtility.ToJson(saveData));
            SkinInventorySaveData towerSaveData = new()
            {
                ownedSkinIds = new List<string>(ownedTowerSkinIds)
            };
            PlayerPrefs.SetString(OwnedTowerSkinsKey, JsonUtility.ToJson(towerSaveData));
            PlayerPrefs.Save();
            SaveDailyState();
            SaveDailyChallengeState();
            NotifyStateChanged();
        }

        public void IncrementTotalRuns()
        {
            TotalRuns++;
            PlayerPrefs.SetInt(TotalRunsKey, TotalRuns);
            PlayerPrefs.Save();
        }

        public bool ShouldRequestReview()
        {
            return TotalRuns >= 5 && PlayerPrefs.GetInt(ReviewRequestedKey, 0) == 0;
        }

        public void MarkReviewRequested()
        {
            PlayerPrefs.SetInt(ReviewRequestedKey, 1);
            PlayerPrefs.Save();
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke();
        }

        private void RefreshLifeRegenIfNeeded()
        {
            if (remainingLives >= MaxLifeCount)
            {
                if (lifeRechargeStartTicks != 0L)
                {
                    lifeRechargeStartTicks = 0L;
                    SaveState();
                }

                return;
            }

            if (lifeRechargeStartTicks <= 0L)
            {
                lifeRechargeStartTicks = DateTime.UtcNow.Ticks;
                SaveState();
                return;
            }

            long nowTicks = DateTime.UtcNow.Ticks;
            if (nowTicks <= lifeRechargeStartTicks)
            {
                return;
            }

            long elapsedTicks = nowTicks - lifeRechargeStartTicks;
            long rechargeTicks = LifeRechargeInterval.Ticks;
            int restoredLives = (int)(elapsedTicks / rechargeTicks);
            if (restoredLives <= 0)
            {
                return;
            }

            remainingLives = Mathf.Clamp(remainingLives + restoredLives, 0, MaxLifeCount);
            if (remainingLives >= MaxLifeCount)
            {
                lifeRechargeStartTicks = 0L;
            }
            else
            {
                lifeRechargeStartTicks += rechargeTicks * restoredLives;
            }

            SaveState();
        }

        private void NotifyEmberBalanceChanged()
        {
            EmberBalanceChanged?.Invoke(EmberBalance);
        }

        private void NotifyTowerSkinChanged()
        {
            EquippedTowerSkinChanged?.Invoke(GetEquippedTowerSkin());
        }
    }

    public sealed class ScoreManager : MonoBehaviour
    {
        private const string BestScoreKey = "TowerMaze.BestScore";
        private const string LeaderboardKey = "TowerMaze.Leaderboard";
        private const int MaxLeaderboardEntries = 5;

        private float persistedBestScore;
        private readonly List<LeaderboardEntry> leaderboardEntries = new();
        private readonly List<LeaderboardEntry> cloudLeaderboardEntries = new();

        public float CurrentScore { get; private set; }
        public float CurrentRunTime { get; private set; }
        public float BestScore { get; private set; }
        public float PersistedBestScore => persistedBestScore;
        public bool IsNewBestThisRun => CurrentScore > persistedBestScore + 0.001f;
        public IReadOnlyList<LeaderboardEntry> LeaderboardEntries => cloudLeaderboardEntries.Count > 0 ? cloudLeaderboardEntries : leaderboardEntries;
        public event Action StateChanged;
        private GameConfig config;
        private bool[] milestoneFired; // allocated in Initialize when config != null
        public event Action<int> OnMilestonePassed;

        public void Initialize(GameConfig gameConfig = null)
        {
            config = gameConfig;
            if (config != null && config.heightMilestones != null)
                milestoneFired = new bool[config.heightMilestones.Length];
            persistedBestScore = PlayerPrefs.GetFloat(BestScoreKey, 0f);
            BestScore = persistedBestScore;
            LoadLeaderboard();
            if (leaderboardEntries.Count > 0)
            {
                BestScore = Mathf.Max(BestScore, leaderboardEntries[0].height);
            }
        }

        public void ResetRun()
        {
            CurrentScore = 0f;
            CurrentRunTime = 0f;
            BestScore = persistedBestScore;
            if (milestoneFired != null)
                System.Array.Clear(milestoneFired, 0, milestoneFired.Length);
        }

        public void Tick(float towerHeight, float elapsedTime)
        {
            CurrentScore = Mathf.Max(CurrentScore, towerHeight);
            CurrentRunTime = Mathf.Max(0f, elapsedTime);
            BestScore = Mathf.Max(BestScore, CurrentScore);

            if (config != null && milestoneFired != null)
            {
                for (int i = 0; i < config.heightMilestones.Length; i++)
                {
                    if (!milestoneFired[i] && towerHeight >= config.heightMilestones[i])
                    {
                        milestoneFired[i] = true;
                        OnMilestonePassed?.Invoke(config.heightMilestones[i]);
                    }
                }
            }
        }

        public void CommitBest()
        {
            if (BestScore <= persistedBestScore)
            {
                return;
            }

            persistedBestScore = BestScore;
            PlayerPrefs.SetFloat(BestScoreKey, BestScore);
            PlayerPrefs.Save();
            StateChanged?.Invoke();
        }

        public void CommitLeaderboardEntry()
        {
            if (CurrentScore <= 0.01f)
            {
                return;
            }

            leaderboardEntries.Add(new LeaderboardEntry(CurrentScore, CurrentRunTime));
            leaderboardEntries.Sort(CompareEntries);
            if (leaderboardEntries.Count > MaxLeaderboardEntries)
            {
                leaderboardEntries.RemoveRange(MaxLeaderboardEntries, leaderboardEntries.Count - MaxLeaderboardEntries);
            }

            SaveLeaderboard();
            StateChanged?.Invoke();
        }

        internal ScoreCloudSaveData ExportCloudData()
        {
            return new ScoreCloudSaveData
            {
                bestScore = persistedBestScore,
                leaderboardEntries = new List<LeaderboardEntry>(leaderboardEntries)
            };
        }

        internal void ImportCloudData(ScoreCloudSaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            persistedBestScore = Mathf.Max(0f, saveData.bestScore);
            BestScore = persistedBestScore;
            leaderboardEntries.Clear();
            if (saveData.leaderboardEntries != null)
            {
                leaderboardEntries.AddRange(saveData.leaderboardEntries);
                leaderboardEntries.Sort(CompareEntries);
                if (leaderboardEntries.Count > MaxLeaderboardEntries)
                {
                    leaderboardEntries.RemoveRange(MaxLeaderboardEntries, leaderboardEntries.Count - MaxLeaderboardEntries);
                }
            }

            SaveBestScoreAndLeaderboard();
            StateChanged?.Invoke();
        }

        public void SetCloudLeaderboardEntries(IReadOnlyList<LeaderboardEntry> entries)
        {
            cloudLeaderboardEntries.Clear();
            if (entries != null)
            {
                cloudLeaderboardEntries.AddRange(entries);
            }
        }

        private void LoadLeaderboard()
        {
            leaderboardEntries.Clear();
            string json = PlayerPrefs.GetString(LeaderboardKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            LeaderboardSaveData saveData = JsonUtility.FromJson<LeaderboardSaveData>(json);
            if (saveData?.entries == null || saveData.entries.Count == 0)
            {
                return;
            }

            leaderboardEntries.AddRange(saveData.entries);
            leaderboardEntries.Sort(CompareEntries);
            if (leaderboardEntries.Count > MaxLeaderboardEntries)
            {
                leaderboardEntries.RemoveRange(MaxLeaderboardEntries, leaderboardEntries.Count - MaxLeaderboardEntries);
            }
        }

        private void SaveLeaderboard()
        {
            LeaderboardSaveData saveData = new()
            {
                entries = new List<LeaderboardEntry>(leaderboardEntries)
            };
            PlayerPrefs.SetString(LeaderboardKey, JsonUtility.ToJson(saveData));
            PlayerPrefs.Save();
        }

        private void SaveBestScoreAndLeaderboard()
        {
            PlayerPrefs.SetFloat(BestScoreKey, persistedBestScore);
            LeaderboardSaveData saveData = new()
            {
                entries = new List<LeaderboardEntry>(leaderboardEntries)
            };
            PlayerPrefs.SetString(LeaderboardKey, JsonUtility.ToJson(saveData));
            PlayerPrefs.Save();
        }

        private static int CompareEntries(LeaderboardEntry left, LeaderboardEntry right)
        {
            int heightComparison = right.height.CompareTo(left.height);
            if (heightComparison != 0)
            {
                return heightComparison;
            }

            return left.timeSeconds.CompareTo(right.timeSeconds);
        }
    }

    public sealed class AudioManager : MonoBehaviour
    {
        public enum MusicMode
        {
            None,
            Menu,
            Gameplay,
        }

        private const int SampleRate = 44100;

        private AudioSource uiAudioSource;
        private AudioSource musicAudioSource;
        private AudioClip countdownTickClip;
        private AudioClip countdownGoClip;
        private AudioClip continueClip;
        private AudioClip failClip;
        private AudioClip meltFailClip;
        private AudioClip wallBumpClip;
        private AudioClip buttonClickClip;
        private AudioClip zoneReachedClip;
        private AudioClip missionCompleteClip;
        private AudioClip newBestClip;
        private AudioClip rewardClip;
        private AudioClip rushAlarmClip;
        private AudioClip menuMusicClip;
        private AudioClip gameplayMusicClip;
        private AudioSource alarmAudioSource;
        private MusicMode currentMusicMode = MusicMode.None;

        public bool SoundEnabled { get; private set; } = true;
        public bool VibrationEnabled { get; private set; } = true;
        public float NearLavaIntensity { get; private set; }

        private void Awake()
        {
            uiAudioSource = GetComponent<AudioSource>();
            if (uiAudioSource == null)
            {
                uiAudioSource = gameObject.AddComponent<AudioSource>();
            }

            uiAudioSource.playOnAwake = false;
            uiAudioSource.loop = false;
            uiAudioSource.spatialBlend = 0f;
            uiAudioSource.volume = 0.32f;

            musicAudioSource = gameObject.AddComponent<AudioSource>();
            musicAudioSource.playOnAwake = false;
            musicAudioSource.loop = true;
            musicAudioSource.spatialBlend = 0f;
            musicAudioSource.volume = 0.2f;

            alarmAudioSource = gameObject.AddComponent<AudioSource>();
            alarmAudioSource.playOnAwake = false;
            alarmAudioSource.loop = true;
            alarmAudioSource.spatialBlend = 0f;
            alarmAudioSource.volume = 0.22f;

            countdownTickClip = CreateToneClip("CountdownTick", 760f, 0.08f, 0.14f);
            countdownGoClip = CreateToneClip("CountdownGo", 1040f, 0.16f, 0.2f);
            continueClip = CreateToneClip("Continue", 920f, 0.12f, 0.18f);
            failClip = CreateToneClip("Fail", 220f, 0.18f, 0.16f);
            meltFailClip = CreateMeltClip("MeltFail", 1.1f, 0.2f);
            wallBumpClip    = CreateToneClip("WallBump", 310f, 0.045f, 0.12f);
            buttonClickClip = Resources.Load<AudioClip>("TowerMaze/Sounds/click-a");
            zoneReachedClip     = Resources.Load<AudioClip>("TowerMaze/Sounds/switch-a");
            missionCompleteClip = Resources.Load<AudioClip>("TowerMaze/Sounds/tap-a");
            newBestClip         = Resources.Load<AudioClip>("TowerMaze/Sounds/switch-b");
            rewardClip          = Resources.Load<AudioClip>("TowerMaze/Sounds/tap-b");
            rushAlarmClip   = CreateSirenClip("RushAlarm", 1.25f, 520f, 860f, 0.14f);
            menuMusicClip = Resources.Load<AudioClip>("TowerMaze/Music/empacotatron_menu");
            gameplayMusicClip = Resources.Load<AudioClip>("TowerMaze/Music/empacotatron_loop");
            alarmAudioSource.clip = rushAlarmClip;
        }

        public void SetSoundEnabled(bool enabled)
        {
            SoundEnabled = enabled;
            if (!enabled)
            {
                uiAudioSource.Stop();
                musicAudioSource.Stop();
                alarmAudioSource.Stop();
                return;
            }

            UpdateMusicPlayback();
        }

        public void SetVibrationEnabled(bool enabled)
        {
            VibrationEnabled = enabled;
        }

        public void SetNearLavaIntensity(float intensity)
        {
            NearLavaIntensity = intensity;
        }

        public void PlayFailCue()
        {
            PlayClip(failClip, 0.82f);
            PlayClip(meltFailClip, 1f);
            TriggerVibration();
        }

        public void PlayContinueCue()
        {
            PlayClip(continueClip, 0.9f);
        }

        public void PlayCountdownTick()
        {
            PlayClip(countdownTickClip, 0.85f);
            TriggerVibration();
        }

        public void PlayCountdownGo()
        {
            PlayClip(countdownGoClip, 1f);
            TriggerVibration();
        }

        public void PlayWallBump()
        {
            PlayClip(wallBumpClip, 0.65f);
        }

        public void PlayButtonClick()
        {
            PlayClip(buttonClickClip, 0.75f);
        }

        public void PlayZoneReached()
        {
            PlayClip(zoneReachedClip, 0.7f);
        }

        public void PlayMissionComplete()
        {
            PlayClip(missionCompleteClip, 0.85f);
        }

        public void PlayNewBest()
        {
            PlayClip(newBestClip, 0.9f);
        }

        public void PlayReward()
        {
            PlayClip(rewardClip, 0.75f);
        }

        public void StartRushAlarm()
        {
            if (!SoundEnabled || alarmAudioSource == null || rushAlarmClip == null || alarmAudioSource.isPlaying)
            {
                return;
            }

            alarmAudioSource.Play();
            TriggerVibration();
        }

        public void StopRushAlarm()
        {
            if (alarmAudioSource != null && alarmAudioSource.isPlaying)
            {
                alarmAudioSource.Stop();
            }
        }

        public void SetMusicMode(MusicMode mode)
        {
            currentMusicMode = mode;
            UpdateMusicPlayback();
        }

        private void PlayClip(AudioClip clip, float volumeScale)
        {
            if (!SoundEnabled || clip == null || uiAudioSource == null)
            {
                return;
            }

            uiAudioSource.PlayOneShot(clip, volumeScale);
        }

        private void UpdateMusicPlayback()
        {
            if (musicAudioSource == null)
            {
                return;
            }

            if (!SoundEnabled)
            {
                musicAudioSource.Stop();
                return;
            }

            AudioClip targetClip = currentMusicMode switch
            {
                MusicMode.Menu => menuMusicClip,
                MusicMode.Gameplay => gameplayMusicClip,
                _ => null,
            };

            if (targetClip == null)
            {
                musicAudioSource.Stop();
                return;
            }

            if (musicAudioSource.clip == targetClip && musicAudioSource.isPlaying)
            {
                return;
            }

            musicAudioSource.clip = targetClip;
            musicAudioSource.Play();
        }

        private void TriggerVibration()
        {
            if (!VibrationEnabled)
            {
                return;
            }

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        private static AudioClip CreateToneClip(string clipName, float frequency, float durationSeconds, float amplitude)
        {
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(durationSeconds * SampleRate));
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleCount;
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t));
                samples[i] = Mathf.Sin((2f * Mathf.PI * frequency * i) / SampleRate) * amplitude * envelope;
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateSirenClip(string clipName, float durationSeconds, float minFrequency, float maxFrequency, float amplitude)
        {
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(durationSeconds * SampleRate));
            float[] samples = new float[sampleCount];
            float phase = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleCount;
                float sweep = 0.5f + (0.5f * Mathf.Sin(t * Mathf.PI * 2f));
                float frequency = Mathf.Lerp(minFrequency, maxFrequency, sweep);
                phase += (2f * Mathf.PI * frequency) / SampleRate;
                float wobble = 0.78f + (0.22f * Mathf.Sin(t * Mathf.PI * 4f));
                samples[i] = Mathf.Sin(phase) * amplitude * wobble;
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateMeltClip(string clipName, float durationSeconds, float amplitude)
        {
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(durationSeconds * SampleRate));
            float[] samples = new float[sampleCount];
            System.Random random = new(14891);
            float tonalPhase = 0f;
            float filteredNoise = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / Mathf.Max(1f, sampleCount - 1f);
                float envelope = Mathf.Pow(1f - t, 1.8f);
                float tonalFrequency = Mathf.Lerp(280f, 86f, t);
                tonalPhase += (2f * Mathf.PI * tonalFrequency) / SampleRate;

                float rawNoise = ((float)random.NextDouble() * 2f) - 1f;
                float smoothing = Mathf.Lerp(0.08f, 0.28f, 0.5f + (0.5f * Mathf.Sin(t * Mathf.PI * 10f)));
                filteredNoise = Mathf.Lerp(filteredNoise, rawNoise, smoothing);

                float fizz = filteredNoise * amplitude * envelope * (0.7f + (0.3f * Mathf.Sin(t * Mathf.PI * 18f)));
                float bubble = Mathf.Sin(tonalPhase) * amplitude * 0.45f * Mathf.Pow(1f - t, 2.6f);
                float hiss = rawNoise * amplitude * 0.08f * Mathf.Sin(t * Mathf.PI * 36f) * envelope;
                samples[i] = Mathf.Clamp(fizz + bubble + hiss, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }

    public sealed class LavaController : MonoBehaviour
    {
        private const string LavaDetailTextureResourcePath = "TowerMaze/Lava/LavaTexture_HighQuality";
        private const string LavaBaseTextureResourcePath = "TowerMaze/BallSkins/Lava004/Lava004_2K-JPG_Color";
        private const string LavaEmissionTextureResourcePath = "TowerMaze/BallSkins/Lava004/Lava004_2K-JPG_Emission";
        private const string LavaNormalMapResourcePath = "TowerMaze/BallSkins/Lava004/Lava004_2K-JPG_NormalGL";

        [SerializeField] private Transform lavaVisualRoot;

        private GameConfig config;
        private ThemeDefinition theme;
        private float failTimer;
        private float graceTimer;
        private Material lavaPoolMaterial;
        private Material lavaGlowMaterial;
        private Material lavaRimMaterial;
        private Material lavaHeatShimmerMaterial;
        private Transform lavaPoolTransform;
        private Transform lavaGlowTransform;
        private Transform lavaRimTransform;
        private ParticleSystem lavaHeatShimmerParticles;
        private Texture2D lavaBaseTexture;
        private Texture2D lavaEmissionTexture;
        private Texture2D lavaDetailTexture;
        private Texture2D lavaNormalMap;
        private Vector2 poolScrollOffset;
        private Vector2 glowScrollOffset;
        private float rushIntensityCached;
        private static Texture2D lavaRimTexture;
        private static Texture2D lavaShimmerTexture;

        public float SurfaceHeight => transform.position.y;

        public void Initialize(GameConfig gameConfig, ThemeDefinition themeDefinition)
        {
            config = gameConfig;
            theme = themeDefinition;
            lavaBaseTexture = Resources.Load<Texture2D>(LavaBaseTextureResourcePath);
            lavaEmissionTexture = Resources.Load<Texture2D>(LavaEmissionTextureResourcePath);
            lavaDetailTexture = Resources.Load<Texture2D>(LavaDetailTextureResourcePath);
            lavaNormalMap = Resources.Load<Texture2D>(LavaNormalMapResourcePath);
            BuildVisual();
        }


        public void SetVisualActive(bool active)
        {
            if (lavaVisualRoot != null) lavaVisualRoot.gameObject.SetActive(active);
        }


        public float GetSurfaceHeightInReferenceSpace(Transform reference)
        {
            if (reference == null)
            {
                return SurfaceHeight;
            }

            return reference.InverseTransformPoint(transform.position).y;
        }

        private void Update()
        {
            float rushMult = Mathf.Lerp(0.9f, 1.9f, rushIntensityCached);
            float time = Time.time;

            if (lavaPoolMaterial != null)
            {
                poolScrollOffset += new Vector2(0.012f, 0.004f) * Time.deltaTime * rushMult;
                Vector2 emissionOffset = (poolScrollOffset * 1.35f) + new Vector2(Mathf.Sin(time * 0.23f), Mathf.Cos(time * 0.19f)) * 0.015f;
                lavaPoolMaterial?.SetTextureOffset("_BaseMap", poolScrollOffset);
                lavaPoolMaterial?.SetTextureOffset("_EmissionMap", emissionOffset);
                lavaPoolMaterial?.SetTextureOffset("_BumpMap", poolScrollOffset * 0.85f);
            }

            if (lavaGlowTransform != null)
            {
                lavaGlowTransform.Rotate(Vector3.up, -4f * Time.deltaTime * rushMult, Space.Self);
            }

            if (lavaRimTransform != null)
            {
                lavaRimTransform.Rotate(Vector3.up, 2.1f * Time.deltaTime * rushMult, Space.Self);
            }

            if (lavaGlowMaterial != null)
            {
                glowScrollOffset += new Vector2(-0.009f, 0.006f) * Time.deltaTime * rushMult;
                lavaGlowMaterial?.SetTextureOffset("_BaseMap", glowScrollOffset);
                float glowPulse = 0.9f + (Mathf.Sin(time * (1.8f + (rushIntensityCached * 1.2f))) * 0.08f);
                Color glowColor = Color.Lerp(theme.lavaColor, theme.lavaEmissionColor, 0.55f);
                glowColor.a = Mathf.Lerp(0.18f, 0.34f, rushIntensityCached) * glowPulse;
                lavaGlowMaterial.SetColor("_BaseColor", glowColor);
            }

            if (lavaRimMaterial != null)
            {
                Vector2 rimOffset = new Vector2(-glowScrollOffset.y, glowScrollOffset.x) * 0.45f;
                lavaRimMaterial.SetTextureOffset("_BaseMap", rimOffset);
                float rimPulse = 0.92f + (Mathf.Sin(time * (2.4f + rushIntensityCached)) * 0.08f);
                Color rimColor = Color.Lerp(theme.lavaColor, theme.lavaEmissionColor, 0.72f);
                rimColor.a = Mathf.Lerp(0.24f, 0.4f, rushIntensityCached) * rimPulse;
                lavaRimMaterial.SetColor("_BaseColor", rimColor);
            }

            UpdateHeatShimmerState(rushIntensityCached);
        }

        public void ResetState()
        {
            failTimer = 0f;
            graceTimer = 0f;
        }

        public void BeginGrace(float duration)
        {
            graceTimer = Mathf.Max(graceTimer, duration);
            failTimer = 0f;
        }

        public void SetRushIntensity(float intensity)
        {
            float clamped = Mathf.Clamp01(intensity);
            rushIntensityCached = clamped;
            if (lavaPoolMaterial != null)
            {
                lavaPoolMaterial.SetColor("_EmissionColor", theme.lavaEmissionColor * Mathf.Lerp(2.4f, 4.35f, clamped));
            }

            if (lavaGlowMaterial != null)
            {
                Color glowColor = Color.Lerp(theme.lavaColor, theme.lavaEmissionColor, 0.55f);
                glowColor.a = Mathf.Lerp(0.18f, 0.34f, clamped);
                lavaGlowMaterial.SetColor("_BaseColor", glowColor);
            }

            if (lavaGlowTransform != null)
            {
                float scale = Mathf.Lerp(20.8f, 22.7f, clamped);
                lavaGlowTransform.localScale = new Vector3(scale, 0.035f, scale);
            }

            if (lavaRimTransform != null)
            {
                float scale = Mathf.Lerp(19.9f, 21.3f, clamped);
                lavaRimTransform.localScale = new Vector3(scale, 0.022f, scale);
            }

            if (lavaRimMaterial != null)
            {
                Color rimColor = Color.Lerp(theme.lavaColor, theme.lavaEmissionColor, 0.72f);
                rimColor.a = Mathf.Lerp(0.24f, 0.4f, clamped);
                lavaRimMaterial.SetColor("_BaseColor", rimColor);
            }

            UpdateHeatShimmerState(clamped);
        }

        public bool Tick(PlayerController player, out float heatIntensity)
        {
            if (config == null || player == null)
            {
                heatIntensity = 0f;
                return false;
            }

            float distanceToBallBottom = player.WorldBottomHeight - SurfaceHeight;
            heatIntensity = 1f - Mathf.Clamp01(distanceToBallBottom / Mathf.Max(0.01f, config.nearLavaDistance));

            if (graceTimer > 0f)
            {
                graceTimer -= Time.deltaTime;
                failTimer = 0f;
                return false;
            }

            if (distanceToBallBottom <= 0f)
            {
                failTimer += Time.deltaTime;
                return failTimer >= config.lavaFailGrace;
            }

            failTimer = 0f;
            return false;
        }

        private void BuildVisual()
        {
            lavaVisualRoot ??= transform;

            foreach (Transform child in lavaVisualRoot)
            {
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            GameObject pool = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pool.name = "LavaPool";
            pool.transform.SetParent(lavaVisualRoot, false);
            pool.transform.localScale = new Vector3(18.6f, 0.08f, 18.6f);
            pool.transform.localPosition = new Vector3(0f, -0.08f, 0f);
            lavaPoolTransform = pool.transform;

            Collider collider = pool.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(collider);
                }
                else
                {
                    DestroyImmediate(collider);
                }
            }

            Material material = RuntimeMaterialFactory.CreateLit(theme, "TowerMaze_LavaPool");
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_Color", Color.white);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", theme.lavaEmissionColor * 2.4f);
            if (lavaBaseTexture != null)
            {
                material.SetTexture("_BaseMap", lavaBaseTexture);
                material.mainTexture = lavaBaseTexture;
                material.SetTextureScale("_BaseMap", new Vector2(1.55f, 1.55f));
            }

            if (lavaEmissionTexture != null)
            {
                material.SetTexture("_EmissionMap", lavaEmissionTexture);
                material.SetTextureScale("_EmissionMap", new Vector2(1.95f, 1.95f));
            }

            if (lavaNormalMap != null)
            {
                material.EnableKeyword("_NORMALMAP");
                material.SetTexture("_BumpMap", lavaNormalMap);
                material.SetTextureScale("_BumpMap", new Vector2(1.7f, 1.7f));
                material.SetFloat("_BumpScale", 0.72f);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.42f);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0.08f);
            }

            Renderer renderer = pool.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            lavaPoolMaterial = material;

            GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            glow.name = "LavaGlow";
            glow.transform.SetParent(lavaVisualRoot, false);
            glow.transform.localScale = new Vector3(20.8f, 0.035f, 20.8f);
            glow.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            lavaGlowTransform = glow.transform;
            Collider glowCollider = glow.GetComponent<Collider>();
            if (glowCollider != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(glowCollider);
                }
                else
                {
                    DestroyImmediate(glowCollider);
                }
            }

            Material glowMaterial = RuntimeMaterialFactory.CreateAdditive(theme, "TowerMaze_LavaGlow");
            glowMaterial.SetColor("_BaseColor", new Color(theme.lavaEmissionColor.r, theme.lavaEmissionColor.g, theme.lavaEmissionColor.b, 0.18f));
            if (lavaDetailTexture != null)
            {
                glowMaterial.SetTexture("_BaseMap", lavaDetailTexture);
                glowMaterial.mainTexture = lavaDetailTexture;
                glowMaterial.SetTextureScale("_BaseMap", new Vector2(1.35f, 1.35f));
            }
            else if (lavaEmissionTexture != null)
            {
                glowMaterial.SetTexture("_BaseMap", lavaEmissionTexture);
                glowMaterial.mainTexture = lavaEmissionTexture;
                glowMaterial.SetTextureScale("_BaseMap", new Vector2(1.65f, 1.65f));
            }

            Renderer glowRenderer = glow.GetComponent<Renderer>();
            glowRenderer.sharedMaterial = glowMaterial;
            glowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            glowRenderer.receiveShadows = false;
            lavaGlowMaterial = glowMaterial;

            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "LavaRim";
            rim.transform.SetParent(lavaVisualRoot, false);
            rim.transform.localScale = new Vector3(19.9f, 0.022f, 19.9f);
            rim.transform.localPosition = new Vector3(0f, 0.055f, 0f);
            lavaRimTransform = rim.transform;
            Collider rimCollider = rim.GetComponent<Collider>();
            if (rimCollider != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(rimCollider);
                }
                else
                {
                    DestroyImmediate(rimCollider);
                }
            }

            Material rimMaterial = RuntimeMaterialFactory.CreateAdditive(theme, "TowerMaze_LavaRim");
            rimMaterial.renderQueue = 3002;
            ApplyMaterialColor(rimMaterial, new Color(theme.lavaEmissionColor.r, theme.lavaEmissionColor.g, theme.lavaEmissionColor.b, 0.24f));
            ApplyTexture(rimMaterial, GetLavaRimTexture());
            rimMaterial.SetTextureScale("_BaseMap", new Vector2(1.02f, 1.02f));
            Renderer rimRenderer = rim.GetComponent<Renderer>();
            rimRenderer.sharedMaterial = rimMaterial;
            rimRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rimRenderer.receiveShadows = false;
            lavaRimMaterial = rimMaterial;

            lavaHeatShimmerMaterial = RuntimeMaterialFactory.CreateAdditive(theme, "TowerMaze_LavaHeatShimmer");
            lavaHeatShimmerMaterial.renderQueue = 3003;
            ApplyMaterialColor(lavaHeatShimmerMaterial, new Color(theme.lavaColor.r, theme.lavaColor.g * 0.88f, theme.lavaEmissionColor.b, 0.2f));
            ApplyTexture(lavaHeatShimmerMaterial, GetLavaShimmerTexture());
            lavaHeatShimmerParticles = CreateHeatShimmerParticles();
            SetRushIntensity(0f);
        }

        private void UpdateHeatShimmerState(float intensity)
        {
            if (lavaHeatShimmerParticles == null)
            {
                return;
            }

            float clamped = Mathf.Clamp01(intensity);
            var emission = lavaHeatShimmerParticles.emission;
            emission.rateOverTime = Mathf.Lerp(9f, 15f, clamped);

            var noise = lavaHeatShimmerParticles.noise;
            noise.strength = Mathf.Lerp(0.18f, 0.3f, clamped);
            noise.scrollSpeed = Mathf.Lerp(0.32f, 0.58f, clamped);

            var main = lavaHeatShimmerParticles.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(
                Mathf.Lerp(0.42f, 0.58f, clamped),
                Mathf.Lerp(0.86f, 1.15f, clamped));

            if (lavaHeatShimmerMaterial != null)
            {
                Color shimmerColor = Color.Lerp(theme.lavaColor, theme.lavaEmissionColor, 0.5f);
                shimmerColor.a = Mathf.Lerp(0.12f, 0.2f, clamped);
                ApplyMaterialColor(lavaHeatShimmerMaterial, shimmerColor);
            }
        }

        private ParticleSystem CreateHeatShimmerParticles()
        {
            GameObject particlesObject = new("LavaHeatShimmer");
            particlesObject.transform.SetParent(lavaVisualRoot, false);
            particlesObject.transform.localPosition = new Vector3(0f, 0.06f, 0f);

            ParticleSystem particles = particlesObject.AddComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.material = lavaHeatShimmerMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.2f;
            renderer.velocityScale = 0.35f;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.42f, 0.86f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.82f);
            main.maxParticles = 42;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.68f, 0.26f, 0.16f),
                new Color(1f, 0.42f, 0.18f, 0.1f));

            var emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 9f;

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Donut;
            shape.radius = 8.95f;
            shape.donutRadius = 0.45f;
            shape.arc = 360f;
            shape.position = Vector3.zero;

            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.y = new ParticleSystem.MinMaxCurve(0.5f, 0.95f);
            velocity.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);

            var noise = particles.noise;
            noise.enabled = true;
            noise.strength = 0.18f;
            noise.frequency = 0.22f;
            noise.scrollSpeed = 0.32f;

            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.42f, 1f, 1.7f));

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.72f, 0.32f), 0f),
                    new GradientColorKey(new Color(0.96f, 0.42f, 0.14f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.16f, 0.18f),
                    new GradientAlphaKey(0.08f, 0.7f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = gradient;

            particles.Play();
            return particles;
        }

        private static void ApplyMaterialColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        private static void ApplyTexture(Material material, Texture texture)
        {
            if (material == null || texture == null)
            {
                return;
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }

            material.mainTexture = texture;
        }

        private static Texture2D GetLavaRimTexture()
        {
            if (lavaRimTexture != null)
            {
                return lavaRimTexture;
            }

            lavaRimTexture = BuildRingTexture("TowerMaze_LavaRimTexture", 256, 0.72f, 0.9f);
            return lavaRimTexture;
        }

        private static Texture2D GetLavaShimmerTexture()
        {
            if (lavaShimmerTexture != null)
            {
                return lavaShimmerTexture;
            }

            lavaShimmerTexture = BuildRadialTexture("TowerMaze_LavaShimmerTexture", 96, 0f, 3.1f);
            return lavaShimmerTexture;
        }

        private static Texture2D BuildRingTexture(string name, int size, float innerRadius, float outerRadius)
        {
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float inner = Mathf.SmoothStep(innerRadius - 0.1f, innerRadius + 0.02f, distance);
                    float outer = 1f - Mathf.SmoothStep(outerRadius - 0.08f, outerRadius + 0.02f, distance);
                    float alpha = Mathf.Clamp01(inner * outer);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D BuildRadialTexture(string name, int size, float innerRadius, float falloff)
        {
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float normalized = Mathf.InverseLerp(1f, innerRadius, distance);
                    float alpha = Mathf.Pow(Mathf.Clamp01(normalized), falloff);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, true);
            return texture;
        }
    }

    public enum RunState
    {
        Boot,
        StartScreen,
        Countdown,
        Running,
        Failed,
    }

    public sealed class RunManager : MonoBehaviour
    {
        [SerializeField] private RunState state = RunState.Boot;
        private bool isPaused;

        private GameConfig config;
        private TowerGenerator towerGenerator;
        private PlayerController playerController;
        private LavaController lavaController;
        private ScoreManager scoreManager;
        private EconomyManager economyManager;
        private RewardedAdManager rewardedAdManager;
        private AudioManager audioManager;
        private UIManager uiManager;
        private InAppReviewManager inAppReviewManager;
        private CoinStoreManager coinStoreManager;
        private InterstitialAdManager interstitialAdManager;
        private CameraFollowController cameraFollowController;
        private RunMode activeRunMode;
        private RunMode pendingRunMode = RunMode.Normal;
        private ChapterManager chapterManager;
        private RunModifierType primaryRunModifier;
        private RunModifierType secondaryRunModifier;
        private int remainingContinues;
        private bool continueAdFlowInProgress;
        private float countdownRemaining;
        private float countdownEndRealtime;
        private int lastCountdownDisplay;
        private bool countdownShowingGo;
        private RushState rushState;
        private ControlFlipState controlFlipState;
        private System.Random rushRandom;
        private float runElapsedTime;
        private float rushStateRemaining;
        private float nextRushTriggerTime;
        private float rushLockoutRemaining;
        private readonly HashSet<int> triggeredControlFlipZones = new();
        private float controlFlipStateRemaining;
        private float currentControlFlipDuration;
        private bool pendingLeaderboardCommit;
        private int pendingRunReward;
        private int claimedRewardAmountThisFail;
        private bool rewardClaimedThisFail;
        private int survivedRushCount;
        private float nearLavaSeconds;
        private bool usedContinueThisRun;
        private float currentRushMultiplier = 1f;
        private float sinkModifierMultiplier = 1f;
        private string cachedFailBestDeltaText = string.Empty;
        private string cachedFailNextTargetText = string.Empty;
        private string cachedFailModeSummaryText = string.Empty;
        private int lastZoneIndex = -1;

        public RunState State => state;


        private EnvironmentBackdropController backdropController;

        public void Initialize(
            GameConfig gameConfig,
            DifficultyProfile profile,
            ThemeDefinition themeDefinition,
            TowerGenerator generator,
            PlayerController player,
            LavaController lava,
            ScoreManager score,
            EconomyManager economy,
            CoinStoreManager coinStore,
            RewardedAdManager rewardedAds,
            AudioManager audio,
            UIManager ui,
            EnvironmentBackdropController backdrop = null,
            CameraFollowController cameraFollow = null,
            InAppReviewManager reviewManager = null,
            InterstitialAdManager interstitialAds = null,
            ChapterManager chapterManager = null)
        {
            config = gameConfig != null ? gameConfig : Resources.Load<GameConfig>("TowerMaze/GameConfig");
            towerGenerator = generator;
            playerController = player;
            lavaController = lava;
            scoreManager = score;
            economyManager = economy;
            coinStoreManager = coinStore;
            rewardedAdManager = rewardedAds;
            audioManager = audio;
            uiManager = ui;
            backdropController = backdrop;
            cameraFollowController = cameraFollow;
            inAppReviewManager = reviewManager;
            interstitialAdManager = interstitialAds;
            this.chapterManager = chapterManager;

            state = RunState.StartScreen;
            activeRunMode = RunMode.Normal;
            pendingRunMode = RunMode.Normal;
            remainingContinues = config.continueCount;
            ResetRushState();
            ResetControlFlipState();
            SetSimulationActive(false);
            countdownShowingGo = false;
            pendingLeaderboardCommit = false;
            audioManager.SetMusicMode(AudioManager.MusicMode.Menu);
            uiManager.ShowStart(scoreManager.BestScore, economyManager.EmberBalance, scoreManager.LeaderboardEntries, economyManager.DailyMissions, economyManager.GetDailyChestStatus(), economyManager.DailyChallengeStatus, economyManager.GetMissionRerollCost(), audioManager.SoundEnabled, audioManager.VibrationEnabled);
            SetStaticModeActive(true);
            PrepareFreshRun();

        }

        public void PauseRun()
        {
            if (state != RunState.Running) return;
            isPaused = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            uiManager.ShowPause();
        }

        public void ResumeRun()
        {
            if (!isPaused) return;
            uiManager.HidePause();
            AudioListener.pause = false;
            Time.timeScale = 1f;
            isPaused = false;
        }

        private void SetStaticModeActive(bool active)
        {
            if (lavaController != null) lavaController.SetVisualActive(!active);
            if (backdropController != null) backdropController.SetStaticMode(active);
        }


        private void Update()
        {
            if (state == RunState.StartScreen)
            {
                if (uiManager != null && uiManager.IsSplashComplete && !uiManager.IsShopOpen && playerController != null && playerController.HasStartIntent)
                {
                    StartRun();
                    return;
                }

                if (playerController != null) playerController.Tick(false);
                return;
            }

            if (state == RunState.Countdown)
            {
                UpdateCountdown();
                playerController?.Tick(false);
                return;
            }

            if (state != RunState.Running)
            {
                playerController?.Tick(false);
                return;
            }

            if (state == RunState.Running && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (isPaused) ResumeRun(); else PauseRun();
            }

            if (isPaused) return;

            runElapsedTime += Time.deltaTime;
            UpdateControlFlipState();
            UpdateRushState();
            towerGenerator.UpdateDifficulty(playerController.HeightOnTower);
            towerGenerator.UpdateStreaming(playerController.HeightOnTower);
            playerController.Tick(true);
            scoreManager.Tick(playerController.HeightOnTower, runElapsedTime);

            if (towerGenerator.TryCollectCoin(playerController.AngleAroundTower, playerController.HeightOnTower, out int reward, out Vector3 worldPos))
            {
                // User requested to halve the coin rewards for in-game collection
                int halvedReward = Mathf.Max(1, reward / 2);
                economyManager.GrantEmber(halvedReward);
                audioManager.PlayReward();
                uiManager.SpawnCoinFloat(halvedReward);
            }

            if (activeRunMode == RunMode.Chapter && chapterManager != null)
            {
                float chapterTarget = chapterManager.GetChapter(chapterManager.ActiveChapterIndex).TargetHeight;
                if (playerController.HeightOnTower >= chapterTarget)
                {
                    CompleteChapterRun();
                    return;
                }
            }

            if (lavaController != null)
            {
                bool failed = lavaController.Tick(playerController, out float heatIntensity);
                if (heatIntensity >= 0.72f)
                {
                    nearLavaSeconds += Time.deltaTime;
                }
                playerController.SetHeat(heatIntensity);
                if (audioManager != null) audioManager.SetNearLavaIntensity(heatIntensity);
                if (uiManager != null) uiManager.SetHeat(heatIntensity);

                int currentZone = GetCurrentZoneIndex();
                if (currentZone > lastZoneIndex && lastZoneIndex >= 0)
                {
                    if (uiManager != null) uiManager.QueueRewardToast($"ZONE {currentZone + 1}", "NEW ZONE", new Color(0.2f, 0.85f, 0.9f));
                    if (audioManager != null) audioManager.PlayZoneReached();
                }
                lastZoneIndex = currentZone;

                if (uiManager != null)
                {
                    bool isChapter = activeRunMode == RunMode.Chapter && chapterManager != null;
                    float hudBest = isChapter
                        ? chapterManager.GetChapter(chapterManager.ActiveChapterIndex).TargetHeight
                        : scoreManager.BestScore;
                    string hudBestLabel = isChapter ? UILanguage.Translate("HEDEF", "TARGET", "META") : null;
                    uiManager.UpdateHud(
                        scoreManager.CurrentScore,
                        hudBest,
                        scoreManager.CurrentRunTime,
                        currentZone,
                        GetLavaGap(),
                        GetGapDangerNormalized(),
                        activeRunMode == RunMode.Normal && scoreManager.IsNewBestThisRun,
                        ShouldShowControlsHint(),
                        hudBestLabel);
                }

                if (failed)
                {
                    FailRun();
                }
            }
        }


        public void StartRun()
        {
            pendingRunMode = RunMode.Normal;
            if (!TryConsumeLifeForRequestedRun())
            {
                return;
            }

            SetStaticModeActive(false);
            ResolvePendingFailedRun(false);
            PrepareFreshRun();

            BeginCountdown();
        }



        public void StartDailyChallenge()
        {
            pendingRunMode = RunMode.DailyChallenge;
            if (!TryConsumeLifeForRequestedRun())
            {
                return;
            }

            SetStaticModeActive(false);
            ResolvePendingFailedRun(false);
            PrepareFreshRun();
            BeginCountdown();
        }



        public void StartChapterRun(int chapterIndex)
        {
            if (chapterManager == null) return;
            chapterManager.SetActiveChapter(chapterIndex);
            pendingRunMode = RunMode.Chapter;
            if (!TryConsumeLifeForRequestedRun()) return;
            SetStaticModeActive(false);
            ResolvePendingFailedRun(false);
            PrepareFreshRun();
            BeginCountdown();
        }



        public void RetryRun()
        {
            pendingRunMode = activeRunMode;
            if (!TryConsumeLifeForRequestedRun())
            {
                // No lives. If rewarded ads aren't available either (e.g. dev testing,
                // unfilled ad inventory, offline), grant a free life so the player isn't
                // permanently stuck. When ads are available, fall through to fail screen
                // which prompts "watch ad" path.
                bool adsAvailable = rewardedAdManager != null && rewardedAdManager.CanShowRewarded;
                if (!adsAvailable && economyManager != null)
                {
                    economyManager.GrantLife();
                    if (!TryConsumeLifeForRequestedRun())
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            SetStaticModeActive(false);
            ResolvePendingFailedRun(false);
            PrepareFreshRun();
            BeginCountdown();
        }


        public void ContinueRun()
        {
            if (state != RunState.Failed || continueAdFlowInProgress)
            {
                return;
            }

            if (rewardedAdManager == null || !rewardedAdManager.CanShowRewarded)
            {
                uiManager.QueueRewardToast("AD NOT READY", "TRY AGAIN IN A MOMENT", new Color(1f, 0.42f, 0.36f, 1f));
                PresentFailScreen();
                return;
            }

            StartCoroutine(ContinueWithRewardedAdsRoutine());
        }

        private IEnumerator ContinueWithRewardedAdsRoutine()
        {
            continueAdFlowInProgress = true;

            bool firstDone = false;
            bool firstSuccess = false;
            rewardedAdManager.ShowRewarded(RewardedPlacement.LifeRefill, success =>
            {
                firstSuccess = success;
                firstDone = true;
            });

            while (!firstDone)
            {
                yield return null;
            }

            if (!firstSuccess)
            {
                continueAdFlowInProgress = false;
                PresentFailScreen();
                yield break;
            }

            bool secondDone = false;
            bool secondSuccess = false;
            rewardedAdManager.ShowRewarded(RewardedPlacement.LifeRefill, success =>
            {
                secondSuccess = success;
                secondDone = true;
            });

            while (!secondDone)
            {
                yield return null;
            }

            continueAdFlowInProgress = false;
            if (!secondSuccess)
            {
                PresentFailScreen();
                yield break;
            }

            remainingContinues = Mathf.Max(0, remainingContinues - 1);
            usedContinueThisRun = true;
            playerController.LiftToSafety(config.continueLiftCells * config.CellHeight);
            lavaController.BeginGrace(Mathf.Max(2.5f, config.startingGrace) + 0.75f);
            DelayRushAfterContinue();
            state = RunState.Running;
            SetSimulationActive(true);
            audioManager.PlayContinueCue();
            audioManager.SetMusicMode(AudioManager.MusicMode.Gameplay);
            uiManager.QueueRewardToast("CONTINUE", "2 ADS COMPLETED", new Color(0.36f, 0.9f, 0.48f, 1f));
            uiManager.ShowHud();
        }

        public void WatchAdForLifeRefill()
        {
            if (economyManager == null || economyManager.RemainingLives >= EconomyManager.MaxLifeCount)
            {
                RefreshLifeRefillPrompt();
                return;
            }

            if (rewardedAdManager == null || !rewardedAdManager.CanShowRewarded)
            {
                uiManager.QueueRewardToast("AD NOT READY", "TRY AGAIN IN A MOMENT", new Color(1f, 0.42f, 0.36f, 1f));
                RefreshLifeRefillPrompt();
                return;
            }

            rewardedAdManager.ShowRewarded(RewardedPlacement.LifeRefill, success =>
            {
                if (!success)
                {
                    uiManager.QueueRewardToast("AD FAILED", "RETRY AVAILABLE", new Color(1f, 0.42f, 0.36f, 1f));
                    RefreshLifeRefillPrompt();
                    return;
                }

                economyManager.GrantLife();
                uiManager.QueueRewardToast("EXTRA LIFE", $"+{EconomyManager.LifeRefillAmount} LIFE", new Color(0.36f, 0.9f, 0.48f, 1f));
                if (state == RunState.Failed) { RetryRun(); } else { StartPendingRun(); }
            });
        }

        public void BuyLifeRefillWithCoins()
        {
            if (economyManager == null || economyManager.RemainingLives >= EconomyManager.MaxLifeCount)
            {
                RefreshLifeRefillPrompt();
                return;
            }

            if (!economyManager.TryBuyLifeRefill(out int spentCoins))
            {
                uiManager.QueueRewardToast("NEED COIN", $"{EconomyManager.LifeRefillCoinCost} COIN REQUIRED", new Color(1f, 0.42f, 0.36f, 1f));
                RefreshLifeRefillPrompt();
                return;
            }

            uiManager.QueueRewardToast("EXTRA LIFE", $"-{spentCoins} COIN  +{EconomyManager.LifeRefillAmount} LIFE", new Color(0.36f, 0.9f, 0.48f, 1f));
            if (state == RunState.Failed) { RetryRun(); } else { StartPendingRun(); }
        }


        public void ReturnToMainMenu()
        {
            if (isPaused) ResumeRun();

            if (state == RunState.Failed)
            {
                ResolvePendingFailedRun(false);
            }

            SetStaticModeActive(true);
            PrepareFreshRun();
            countdownShowingGo = false;
            state = RunState.StartScreen;
            activeRunMode = RunMode.Normal;
            pendingRunMode = RunMode.Normal;
            SetSimulationActive(false);
            StopRushEffects();
            StopControlFlipEffects();
            audioManager.SetMusicMode(AudioManager.MusicMode.Menu);
            if (economyManager.ShouldRequestReview())
            {
                economyManager.MarkReviewRequested();
                inAppReviewManager?.RequestReview();
            }
            uiManager.ShowStart(
                scoreManager.BestScore,
                economyManager.EmberBalance,
                scoreManager.LeaderboardEntries,
                economyManager.DailyMissions,
                economyManager.GetDailyChestStatus(),
                economyManager.DailyChallengeStatus,
                economyManager.GetMissionRerollCost(),
                audioManager.SoundEnabled,
                audioManager.VibrationEnabled);
        }


        private void PrepareFreshRun()
        {
            if (config == null)
            {
                Debug.LogError("[RunManager] PrepareFreshRun skipped: config is null. Check Bootstrapper initialization.");
                return;
            }
            activeRunMode = pendingRunMode;
            remainingContinues = (activeRunMode == RunMode.DailyChallenge || activeRunMode == RunMode.Chapter) ? 0 : config.continueCount;
            scoreManager.ResetRun();
            int runSeed = activeRunMode switch
            {
                RunMode.DailyChallenge => economyManager.DailyChallengeStatus.seed,
                RunMode.Chapter => chapterManager != null ? chapterManager.GetChapter(chapterManager.ActiveChapterIndex).Seed : config.seed,
                _ => config.seed,
            };
            towerGenerator.ResetRun(runSeed);
            towerGenerator.UpdateDifficulty(0f);
            lavaController.ResetState();
            if (config != null) lavaController.BeginGrace(Mathf.Max(2.5f, config.startingGrace));
            ResetRushState();
            ResetControlFlipState();
            ConfigureRunModifiers(runSeed);

            float preferredAngle = towerGenerator.ColumnToAngleCenter(config.mazeWidthCells / 2);
            towerGenerator.TryFindOpenPosition(preferredAngle, config.CellHeight * 1.35f, out float startAngle, out float startHeight);
            playerController.ResetRunPosition(startAngle, startHeight);
            playerController.SetHeat(0f);
            playerController.SetControlsFlipped(false);
            playerController.SetMovementMultipliers(GetHorizontalSpeedMultiplier(), GetClimbSpeedMultiplier());
            audioManager.SetNearLavaIntensity(0f);
            uiManager.SetHeat(0f);
            uiManager.SetRushState(false, false, 0f, 0f);
            uiManager.SetControlFlipState(false, false, 0f, 0f, 0f);
            uiManager.UpdateHud(scoreManager.CurrentScore, scoreManager.BestScore, scoreManager.CurrentRunTime, 0, GetLavaGap(), GetGapDangerNormalized(), false, true);
            pendingLeaderboardCommit = false;
            pendingRunReward = 0;
            claimedRewardAmountThisFail = 0;
            rewardClaimedThisFail = false;
            survivedRushCount = 0;
            nearLavaSeconds = 0f;
            lastZoneIndex = 0;
            usedContinueThisRun = false;

            if (activeRunMode == RunMode.Chapter && chapterManager != null)
            {
                var ch = chapterManager.GetChapter(chapterManager.ActiveChapterIndex);
                towerGenerator.SetChapterMazeSettings(ch.MazeSettings);
                towerGenerator.SetChapterSinkSpeed(ch.SinkSpeed);
            }
            else
            {
                towerGenerator.ClearChapterMazeSettings();
                towerGenerator.ClearChapterSinkSpeed();
            }
        }

        public void ToggleSound()
        {
            audioManager.SetSoundEnabled(!audioManager.SoundEnabled);
            audioManager.SetMusicMode(GetMusicModeForState());
            if (state == RunState.StartScreen)
            {
                uiManager.ShowStart(scoreManager.BestScore, economyManager.EmberBalance, scoreManager.LeaderboardEntries, economyManager.DailyMissions, economyManager.GetDailyChestStatus(), economyManager.DailyChallengeStatus, economyManager.GetMissionRerollCost(), audioManager.SoundEnabled, audioManager.VibrationEnabled);
            }
        }

        public void ToggleVibration()
        {
            audioManager.SetVibrationEnabled(!audioManager.VibrationEnabled);
            if (state == RunState.StartScreen)
            {
                uiManager.ShowStart(scoreManager.BestScore, economyManager.EmberBalance, scoreManager.LeaderboardEntries, economyManager.DailyMissions, economyManager.GetDailyChestStatus(), economyManager.DailyChallengeStatus, economyManager.GetMissionRerollCost(), audioManager.SoundEnabled, audioManager.VibrationEnabled);
            }
        }

        public void ClaimDoubleReward()
        {
            if (state != RunState.Failed || pendingRunReward <= 0 || rewardClaimedThisFail || rewardedAdManager == null)
            {
                return;
            }

            rewardedAdManager.ShowRewarded(RewardedPlacement.DoubleRunReward, success =>
            {
                if (!success)
                {
                    return;
                }

                economyManager.GrantEmber(pendingRunReward * 2);
                CommitPendingLeaderboardIfNeeded();
                claimedRewardAmountThisFail = pendingRunReward * 2;
                pendingRunReward = 0;
                rewardClaimedThisFail = true;
                remainingContinues = 0;
                uiManager.QueueRewardToast("REWARDED", $"+{claimedRewardAmountThisFail} COIN", new Color(1f, 0.72f, 0.28f, 1f));
                audioManager.PlayReward();
                PresentFailScreen();
            });
        }

        private void FailRun()
        {
            if (isPaused) ResumeRun();
            state = RunState.Failed;
            SetSimulationActive(false);

            StopRushEffects();
            StopControlFlipEffects();
            cachedFailBestDeltaText = BuildBestDeltaText();
            cachedFailNextTargetText = BuildNextTargetText();
            cachedFailModeSummaryText = BuildRunModeSummaryText();
            if (activeRunMode == RunMode.Normal)
            {
                if (scoreManager.IsNewBestThisRun) { audioManager.PlayNewBest(); }
                scoreManager.CommitBest();
            }
            pendingRunReward = economyManager.CalculateRunReward(scoreManager.CurrentScore, GetCurrentZoneIndex() + 1, scoreManager.CurrentRunTime);
            claimedRewardAmountThisFail = 0;
            rewardClaimedThisFail = false;
            pendingLeaderboardCommit = rewardedAdManager != null && rewardedAdManager.CanShowRewarded;
            if (!pendingLeaderboardCommit && activeRunMode == RunMode.Normal)
            {
                scoreManager.CommitLeaderboardEntry();
            }
            audioManager.PlayFailCue();
            audioManager.SetMusicMode(AudioManager.MusicMode.Menu);
            if (activeRunMode == RunMode.Chapter && chapterManager != null)
            {
                int idx = chapterManager.ActiveChapterIndex;
                float target = chapterManager.GetChapter(idx).TargetHeight;
                chapterManager.RecordChapterBest(idx, scoreManager.CurrentScore);
                uiManager.ShowChapterFail(idx, scoreManager.CurrentScore, target, pendingRunReward, ReturnToMainMenu);
                return;
            }
            PresentFailScreen();
        }

        private void CompleteChapterRun()
        {
            if (isPaused) ResumeRun();
            state = RunState.Failed;
            SetSimulationActive(false);
            StopRushEffects();
            StopControlFlipEffects();

            int idx = chapterManager.ActiveChapterIndex;
            float height = playerController.HeightOnTower;
            chapterManager.RecordChapterComplete(idx, height);

            int emberReward = 100 * idx;
            economyManager.GrantEmber(emberReward);
            audioManager.SetMusicMode(AudioManager.MusicMode.Menu);

            bool nextUnlocked = chapterManager.IsUnlocked(idx + 1);
            bool isLastChapter = idx >= ChapterManager.TotalChapters;
            bool isTierMilestone = (idx % ChapterManager.ChaptersPerTier) == 0;

            if (isTierMilestone)
            {
                int tierIndex = idx / ChapterManager.ChaptersPerTier;
                int tierBonus = tierIndex * 500;
                economyManager.GrantEmber(tierBonus);
                uiManager.ShowTierCelebration(
                    tierIndex,
                    tierBonus,
                    isLastChapter,
                    () => { if (!isLastChapter) ReturnToMainMenu(); else ReturnToMainMenu(); });
                return;
            }

            uiManager.ShowChapterComplete(
                idx,
                height,
                chapterManager.GetChapter(idx).TargetHeight,
                emberReward,
                nextUnlocked,
                isLastChapter,
                ReturnToMainMenu,
                () => { if (!isLastChapter) StartChapterRun(idx + 1); else ReturnToMainMenu(); },
                () => uiManager.ShowChapterSelect(chapterManager, StartChapterRun));
        }

        private void SetSimulationActive(bool isActive)
        {
            towerGenerator.RotationController.SetSimulationActive(isActive);
            towerGenerator.SinkController.SetSimulationActive(isActive);
        }

        private void BeginCountdown()
        {
            countdownRemaining = Mathf.Max(0f, config.startCountdownSeconds);
            countdownShowingGo = false;
            lastCountdownDisplay = -1;
            countdownEndRealtime = Time.realtimeSinceStartup + countdownRemaining;
            state = countdownRemaining > 0.01f || config.countdownGoSeconds > 0.01f ? RunState.Countdown : RunState.Running;
            SetSimulationActive(false);
            StopRushEffects();
            StopControlFlipEffects();
            audioManager.SetMusicMode(AudioManager.MusicMode.Gameplay);
            QueueRunModifierToast();
            if (state == RunState.Countdown)
            {
                UpdateCountdownVisual();
            }

            if (state == RunState.Running)
            {
                playerController.ResetMovementDirectionToUp();
                SetSimulationActive(true);
                uiManager.ShowHud();
            }
        }

        private void UpdateCountdown()
        {
            if (countdownShowingGo)
            {
                countdownRemaining = Mathf.Max(0f, countdownEndRealtime - Time.realtimeSinceStartup);
                bool isChapterCd = activeRunMode == RunMode.Chapter && chapterManager != null;
                float cdBest = isChapterCd ? chapterManager.GetChapter(chapterManager.ActiveChapterIndex).TargetHeight : scoreManager.BestScore;
                string cdBestLabel = isChapterCd ? UILanguage.Translate("HEDEF", "TARGET", "META") : null;
                uiManager.ShowCountdown("GO!", true, scoreManager.CurrentScore, cdBest, scoreManager.CurrentRunTime, GetCurrentZoneIndex(), GetLavaGap(), GetGapDangerNormalized(), activeRunMode == RunMode.Normal && scoreManager.IsNewBestThisRun, true, cdBestLabel);

                if (countdownRemaining > 0f)
                {
                    return;
                }

                state = RunState.Running;
                playerController.ResetMovementDirectionToUp();
                SetSimulationActive(true);
                uiManager.ShowHud();
                ShowOnboardingTipIfNeeded();
                uiManager.UpdateHud(
                    scoreManager.CurrentScore,
                    scoreManager.BestScore,
                    scoreManager.CurrentRunTime,
                    GetCurrentZoneIndex(),
                    GetLavaGap(),
                    GetGapDangerNormalized(),
                    activeRunMode == RunMode.Normal && scoreManager.IsNewBestThisRun,
                    ShouldShowControlsHint());
                return;
            }

            countdownRemaining = Mathf.Max(0f, countdownEndRealtime - Time.realtimeSinceStartup);
            UpdateCountdownVisual();

            if (countdownRemaining > 0f)
            {
                return;
            }

            countdownShowingGo = true;
            countdownRemaining = Mathf.Max(0.01f, config.countdownGoSeconds);
            countdownEndRealtime = Time.realtimeSinceStartup + countdownRemaining;
            audioManager.PlayCountdownGo();
            {
                bool isChapterGo = activeRunMode == RunMode.Chapter && chapterManager != null;
                float goBest = isChapterGo ? chapterManager.GetChapter(chapterManager.ActiveChapterIndex).TargetHeight : scoreManager.BestScore;
                string goBestLabel = isChapterGo ? UILanguage.Translate("HEDEF", "TARGET", "META") : null;
                uiManager.ShowCountdown("GO!", true, scoreManager.CurrentScore, goBest, scoreManager.CurrentRunTime, GetCurrentZoneIndex(), GetLavaGap(), GetGapDangerNormalized(), activeRunMode == RunMode.Normal && scoreManager.IsNewBestThisRun, true, goBestLabel);
            }
        }

        private void UpdateCountdownVisual()
        {
            int displayValue = Mathf.Max(1, Mathf.CeilToInt(countdownRemaining));
            if (displayValue != lastCountdownDisplay)
            {
                lastCountdownDisplay = displayValue;
                audioManager.PlayCountdownTick();
            }

            bool isChapterTick = activeRunMode == RunMode.Chapter && chapterManager != null;
            float tickBest = isChapterTick ? chapterManager.GetChapter(chapterManager.ActiveChapterIndex).TargetHeight : scoreManager.BestScore;
            string tickBestLabel = isChapterTick ? UILanguage.Translate("HEDEF", "TARGET", "META") : null;
            uiManager.ShowCountdown(displayValue.ToString(), false, scoreManager.CurrentScore, tickBest, scoreManager.CurrentRunTime, GetCurrentZoneIndex(), GetLavaGap(), GetGapDangerNormalized(), activeRunMode == RunMode.Normal && scoreManager.IsNewBestThisRun, true, tickBestLabel);
        }

        private void CommitPendingLeaderboardIfNeeded()
        {
            if (!pendingLeaderboardCommit)
            {
                return;
            }

            if (activeRunMode == RunMode.Normal)
            {
                scoreManager.CommitLeaderboardEntry();
            }
            pendingLeaderboardCommit = false;
        }

        private AudioManager.MusicMode GetMusicModeForState()
        {
            return state == RunState.Running || state == RunState.Countdown
                ? AudioManager.MusicMode.Gameplay
                : AudioManager.MusicMode.Menu;
        }

        private void ResolvePendingFailedRun(bool doubled)
        {
            if (state != RunState.Failed)
            {
                CommitPendingLeaderboardIfNeeded();
                return;
            }

            RunSummary summary = BuildRunSummary();
            DailyMissionRewardResult missionReward = economyManager.RegisterCompletedRun(summary);
            DailyChallengeRewardResult challengeReward = economyManager.RegisterDailyChallengeRun(summary);
            int totalReward = challengeReward.RewardCoins;
            if (!rewardClaimedThisFail && pendingRunReward > 0)
            {
                economyManager.GrantEmber((doubled ? pendingRunReward * 2 : pendingRunReward) + totalReward);
            }
            else if (totalReward > 0)
            {
                economyManager.GrantEmber(totalReward);
            }

            // Mission completed but reward not auto-granted — player must tap claim button.
            if (missionReward.completedMissionCount > 0)
            {
                string title = missionReward.completedMissionCount > 1
                    ? $"{missionReward.completedMissionCount} MISSIONS READY"
                    : "MISSION READY";
                uiManager.QueueRewardToast(title, "TAP TO CLAIM", new Color(1f, 0.82f, 0.32f, 1f));
                audioManager.PlayMissionComplete();
            }

            if (challengeReward.RewardCoins > 0)
            {
                uiManager.QueueRewardToast("DAILY CHALLENGE", $"+{challengeReward.RewardCoins} COIN", new Color(0.32f, 0.82f, 1f, 1f));
                audioManager.PlayReward();
            }

            if (challengeReward.IsNewDailyBest)
            {
                uiManager.QueueRewardToast("DAILY BEST", $"{challengeReward.Status.bestHeight:0.0}m", new Color(0.48f, 0.72f, 1f, 1f));
            }

            CommitPendingLeaderboardIfNeeded();
            pendingRunReward = 0;
            claimedRewardAmountThisFail = 0;
            rewardClaimedThisFail = false;
        }

        private bool TryConsumeLifeForRequestedRun()
        {
            if (economyManager == null || economyManager.TryConsumeLife())
            {
                return true;
            }

            RefreshLifeRefillPrompt(returnToStartWhenNotFailed: false);
            return false;
        }
        private void StartPendingRun()
        {
            if (!TryConsumeLifeForRequestedRun())
            {
                return;
            }

            SetStaticModeActive(false);
            ResolvePendingFailedRun(false);
            PrepareFreshRun();
            BeginCountdown();
        }

        private void RefreshLifeRefillPrompt(bool returnToStartWhenNotFailed = false)
        {
            if (state == RunState.Failed)
            {
                PresentFailScreen();
                return;
            }

            if (returnToStartWhenNotFailed)
            {
                uiManager.ShowStart(
                    scoreManager.BestScore,
                    economyManager.EmberBalance,
                    scoreManager.LeaderboardEntries,
                    economyManager.DailyMissions,
                    economyManager.GetDailyChestStatus(),
                    economyManager.DailyChallengeStatus,
                    economyManager.GetMissionRerollCost(),
                    audioManager.SoundEnabled,
                    audioManager.VibrationEnabled);
                return;
            }

            cachedFailBestDeltaText = $"LIVES  {economyManager.RemainingLives}/{EconomyManager.MaxLifeCount}";
            cachedFailNextTargetText = "WATCH 1 AD TO RETRY";
            cachedFailModeSummaryText = pendingRunMode == RunMode.DailyChallenge ? "DAILY CHALLENGE LOCKED" : "NO LIVES LEFT";

            uiManager.ShowFail(
                0f,
                scoreManager.BestScore,
                0f,
                scoreManager.LeaderboardEntries,
                economyManager.EmberBalance,
                0,
                0,
                false,
                false,
                cachedFailBestDeltaText,
                cachedFailNextTargetText,
                cachedFailModeSummaryText,
                economyManager.RemainingLives,
                rewardedAdManager != null && rewardedAdManager.CanShowRewarded,
                false,
                0,
                false,
                EconomyManager.ContinueCoinCost);
        }

        private void PresentFailScreen()
        {
            bool hasContinueOption = true;
            bool canContinue = !continueAdFlowInProgress;
            uiManager.ShowFail(
                scoreManager.CurrentScore,
                scoreManager.BestScore,
                scoreManager.CurrentRunTime,
                scoreManager.LeaderboardEntries,
                economyManager.EmberBalance,
                pendingRunReward,
                claimedRewardAmountThisFail,
                canContinue,
                rewardedAdManager != null && rewardedAdManager.CanShowRewarded && pendingRunReward > 0 && !rewardClaimedThisFail,
                cachedFailBestDeltaText,
                cachedFailNextTargetText,
                cachedFailModeSummaryText,
                economyManager.RemainingLives,
                rewardedAdManager != null && rewardedAdManager.CanShowRewarded,
                false,
                0,
                hasContinueOption,
                0,
                showUpsell: true);
        }

        private void ResetRushState()
        {
            rushRandom = new System.Random((config.seed * 41) + 17);
            runElapsedTime = 0f;
            rushState = RushState.Idle;
            rushStateRemaining = 0f;
            nextRushTriggerTime = config.rushStartDelay;
            rushLockoutRemaining = 0f;
            StopRushEffects();
        }

        private void ResetControlFlipState()
        {
            triggeredControlFlipZones.Clear();
            controlFlipState = ControlFlipState.Idle;
            controlFlipStateRemaining = 0f;
            currentControlFlipDuration = config != null ? config.controlFlipDuration : 8f;
            StopControlFlipEffects();
        }

        private void UpdateControlFlipState()
        {
            switch (controlFlipState)
            {
                case ControlFlipState.Warning:
                    controlFlipStateRemaining -= Time.deltaTime;
                    uiManager.SetControlFlipState(true, false, GetControlFlipPulse(), controlFlipStateRemaining / Mathf.Max(0.01f, config.controlFlipWarningDuration), currentControlFlipDuration);
                    if (controlFlipStateRemaining <= 0f)
                    {
                        controlFlipState = ControlFlipState.Active;
                        controlFlipStateRemaining = currentControlFlipDuration;
                        playerController.SetControlsFlipped(true);
                    }

                    return;

                case ControlFlipState.Active:
                    controlFlipStateRemaining -= Time.deltaTime;
                    uiManager.SetControlFlipState(true, true, GetControlFlipPulse(), controlFlipStateRemaining / Mathf.Max(0.01f, currentControlFlipDuration), currentControlFlipDuration);
                    if (controlFlipStateRemaining <= 0f)
                    {
                        controlFlipState = ControlFlipState.Idle;
                        StopControlFlipEffects();
                    }

                    return;
            }

            StopControlFlipEffects();
            TryTriggerControlFlipForZone(GetCurrentZoneIndex() + 1);
        }

        private void UpdateRushState()
        {
            rushLockoutRemaining = Mathf.Max(0f, rushLockoutRemaining - Time.deltaTime);

            switch (rushState)
            {
                case RushState.Warning:
                    rushStateRemaining -= Time.deltaTime;
                    currentRushMultiplier = config.rushWarningSpeedMultiplier;
                    ApplySinkMultiplier();
                    lavaController.SetRushIntensity(0.45f + (GetRushPulse() * 0.15f));
                    uiManager.SetRushState(true, false, GetRushPulse(), rushStateRemaining / Mathf.Max(0.01f, config.rushWarningDuration));
                    if (rushStateRemaining <= 0f)
                    {
                        rushState = RushState.Active;
                        rushStateRemaining = config.rushDuration;
                    }

                    return;

                case RushState.Active:
                    rushStateRemaining -= Time.deltaTime;
                    currentRushMultiplier = config.rushSpeedMultiplier;
                    ApplySinkMultiplier();
                    lavaController.SetRushIntensity(0.78f + (GetRushPulse() * 0.22f));
                    uiManager.SetRushState(true, true, GetRushPulse(), rushStateRemaining / Mathf.Max(0.01f, config.rushDuration));
                    if (rushStateRemaining <= 0f)
                    {
                        survivedRushCount++;
                        rushState = RushState.Idle;
                        ScheduleNextRush();
                        StopRushEffects();
                    }

                    return;
            }

            currentRushMultiplier = 1f;
            ApplySinkMultiplier();
            lavaController.SetRushIntensity(0f);
            uiManager.SetRushState(false, false, 0f, 0f);

            if (rushLockoutRemaining > 0f)
            {
                return;
            }

            if (runElapsedTime >= nextRushTriggerTime)
            {
                BeginRushWarning();
            }
        }

        private void BeginRushWarning()
        {
            rushState = RushState.Warning;
            rushStateRemaining = config.rushWarningDuration;
            currentRushMultiplier = config.rushWarningSpeedMultiplier;
            ApplySinkMultiplier();
            audioManager.StartRushAlarm();
            uiManager.SetRushState(true, false, 1f, 1f);
        }

        private void TryTriggerControlFlipForZone(int zoneNumber)
        {
            if (zoneNumber < config.controlFlipStartZone)
            {
                return;
            }

            int repeatEvery = Mathf.Max(1, config.controlFlipRepeatEveryZones);
            if (((zoneNumber - config.controlFlipStartZone) % repeatEvery) != 0)
            {
                return;
            }

            if (!triggeredControlFlipZones.Add(zoneNumber))
            {
                return;
            }

            int triggerIndex = Mathf.Max(0, triggeredControlFlipZones.Count - 1);
            currentControlFlipDuration = config.controlFlipDuration + (triggerIndex * config.controlFlipDurationIncreasePerTrigger);
            controlFlipState = ControlFlipState.Warning;
            controlFlipStateRemaining = config.controlFlipWarningDuration;
            uiManager.SetControlFlipState(true, false, 1f, 1f, currentControlFlipDuration);
        }

        private void ScheduleNextRush()
        {
            float intervalReduction = Mathf.Lerp(
                0f,
                config.rushIntervalReductionAtMaxHeight,
                Mathf.Clamp01(playerController.HeightOnTower / Mathf.Max(1f, config.rushIntervalReductionHeight)));
            float minInterval = Mathf.Max(4f, config.rushIntervalMin - intervalReduction);
            float maxInterval = Mathf.Max(minInterval, config.rushIntervalMax - intervalReduction);
            float interval = Mathf.Lerp(minInterval, maxInterval, (float)rushRandom.NextDouble());
            nextRushTriggerTime = runElapsedTime + interval;
        }

        private void DelayRushAfterContinue()
        {
            rushState = RushState.Idle;
            rushStateRemaining = 0f;
            rushLockoutRemaining = config.rushContinueGrace;
            nextRushTriggerTime = Mathf.Max(nextRushTriggerTime, runElapsedTime + config.rushContinueGrace);
            StopRushEffects();
        }

        private void StopRushEffects()
        {
            currentRushMultiplier = 1f;
            ApplySinkMultiplier();
            lavaController?.SetRushIntensity(0f);
            uiManager?.SetRushState(false, false, 0f, 0f);
            audioManager?.StopRushAlarm();
        }

        private void StopControlFlipEffects()
        {
            playerController?.SetControlsFlipped(false);
            uiManager?.SetControlFlipState(false, false, 0f, 0f, 0f);
        }

        private float GetRushPulse()
        {
            return 0.5f + (0.5f * Mathf.Sin(runElapsedTime * 8f));
        }

        private float GetControlFlipPulse()
        {
            return 0.5f + (0.5f * Mathf.Sin(runElapsedTime * 10f));
        }

        private float GetLavaDisplayHeight()
        {
            if (lavaController == null || towerGenerator == null)
            {
                return 0f;
            }

            return lavaController.GetSurfaceHeightInReferenceSpace(towerGenerator.TowerSpace);
        }

        private float GetLavaGap()
        {
            return Mathf.Max(0f, playerController.BottomHeightOnTower - GetLavaDisplayHeight());
        }

        private float GetGapDangerNormalized()
        {
            return 1f - Mathf.Clamp01(GetLavaGap() / Mathf.Max(0.01f, config.nearLavaDistance));
        }

        private int GetCurrentZoneIndex()
        {
            return Mathf.Max(0, Mathf.FloorToInt(playerController.HeightOnTower / Mathf.Max(0.01f, config.ZoneHeight)));
        }

        private void ShowOnboardingTipIfNeeded()
        {
            int runs = economyManager.TotalRuns;
            string tip = runs switch
            {
                0 => "SWIPE TO MOVE",
                1 => "LAVA IS RISING — CLIMB!",
                2 => "RUSH INCOMING — SPEED UP!",
                _ => null
            };
            if (tip != null)
            {
                uiManager.QueueRewardToast(tip, string.Empty, new Color(1f, 0.85f, 0.2f));
            }
            economyManager.IncrementTotalRuns();
        }

        private void ConfigureRunModifiers(int runSeed)
        {
            if (activeRunMode == RunMode.DailyChallenge)
            {
                DailyChallengeStatus challengeStatus = economyManager.DailyChallengeStatus;
                primaryRunModifier = challengeStatus.primaryModifier;
                secondaryRunModifier = challengeStatus.secondaryModifier;
            }
            else
            {
                primaryRunModifier = RunModifierType.None;
                secondaryRunModifier = RunModifierType.None;
            }

            sinkModifierMultiplier = 1f;
            towerGenerator.RotationController.SetSpeedMultiplier(1f);

            ApplyModifier(primaryRunModifier);
            ApplyModifier(secondaryRunModifier);
            ApplySinkMultiplier();
        }

        private void ApplyModifier(RunModifierType modifier)
        {
            switch (modifier)
            {
                case RunModifierType.Slipstream:
                    break;

                case RunModifierType.HighStakes:
                    sinkModifierMultiplier *= 1.08f;
                    break;
            }
        }

        private float GetHorizontalSpeedMultiplier()
        {
            float multiplier = 1f;
            if (primaryRunModifier == RunModifierType.Slipstream || secondaryRunModifier == RunModifierType.Slipstream)
            {
                multiplier *= 1.35f;
            }
            if (primaryRunModifier == RunModifierType.HighStakes || secondaryRunModifier == RunModifierType.HighStakes)
            {
                multiplier *= 1.10f;
            }

            return multiplier;
        }

        private float GetClimbSpeedMultiplier()
        {
            float multiplier = 1f;
            if (primaryRunModifier == RunModifierType.Slipstream || secondaryRunModifier == RunModifierType.Slipstream)
            {
                multiplier *= 1.28f;
            }
            if (primaryRunModifier == RunModifierType.HighStakes || secondaryRunModifier == RunModifierType.HighStakes)
            {
                multiplier *= 1.10f;
            }

            return multiplier;
        }

        private void ApplySinkMultiplier()
        {
            towerGenerator?.SinkController.SetSpeedMultiplier(currentRushMultiplier * sinkModifierMultiplier);
        }

        private RunSummary BuildRunSummary()
        {
            return new RunSummary(
                scoreManager.CurrentScore,
                GetCurrentZoneIndex() + 1,
                scoreManager.CurrentRunTime,
                usedContinueThisRun,
                activeRunMode == RunMode.Normal && scoreManager.IsNewBestThisRun,
                activeRunMode == RunMode.DailyChallenge,
                survivedRushCount,
                nearLavaSeconds,
                primaryRunModifier,
                secondaryRunModifier);
        }

        private string BuildBestDeltaText()
        {
            float previousBest = scoreManager.PersistedBestScore;
            float delta = scoreManager.CurrentScore - previousBest;
            if (delta >= 0f && activeRunMode == RunMode.Normal)
            {
                return $"NEW BEST  +{delta:0.0}m";
            }

            return $"BEST DELTA  {delta:0.0}m";
        }

        private string BuildNextTargetText()
        {
            if (activeRunMode == RunMode.DailyChallenge)
            {
                DailyChallengeStatus status = economyManager.DailyChallengeStatus;
                float remainingHeight = Mathf.Max(0f, status.targetHeight - scoreManager.CurrentScore);
                if (!status.rewardClaimed && remainingHeight > 0.01f)
                {
                    return $"DAILY TARGET  {remainingHeight:0.0}m LEFT";
                }
            }

            NextUnlockStatus nextUnlock = economyManager.GetNextUnlockStatus();
            if (!nextUnlock.HasTarget)
            {
                return "NEXT TARGET  ALL ITEMS OWNED";
            }

            return $"NEXT UNLOCK  {nextUnlock.DisplayName}  {nextUnlock.RemainingCoins} COIN";
        }

        private string BuildRunModeSummaryText()
        {
            string modeLabel = activeRunMode == RunMode.DailyChallenge ? "DAILY" : "NORMAL";
            if (secondaryRunModifier != RunModifierType.None)
            {
                return $"{modeLabel}  {EconomyManager.GetModifierDisplayName(primaryRunModifier)} + {EconomyManager.GetModifierDisplayName(secondaryRunModifier)}";
            }

            if (primaryRunModifier != RunModifierType.None)
            {
                return $"{modeLabel}  {EconomyManager.GetModifierDisplayName(primaryRunModifier)}";
            }

            return modeLabel;
        }

        private void QueueRunModifierToast()
        {
            if (uiManager == null)
            {
                return;
            }

            if (activeRunMode == RunMode.DailyChallenge)
            {
                uiManager.QueueRewardToast(
                    "DAILY MODIFIERS",
                    $"{EconomyManager.GetModifierDisplayName(primaryRunModifier)} + {EconomyManager.GetModifierDisplayName(secondaryRunModifier)}",
                    new Color(0.38f, 0.76f, 1f, 1f));
                return;
            }

            if (primaryRunModifier != RunModifierType.None)
            {
                uiManager.QueueRewardToast(
                    "RUN MODIFIER",
                    EconomyManager.GetModifierDisplayName(primaryRunModifier),
                    new Color(0.34f, 0.9f, 0.76f, 1f));
            }
        }

        private bool ShouldShowControlsHint()
        {
            return state == RunState.Countdown || runElapsedTime < 6f;
        }
    }
}
