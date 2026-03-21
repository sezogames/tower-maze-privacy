using System;
using UnityEngine;

namespace TowerMaze
{
    public sealed class LifeManager : MonoBehaviour
    {
        public const int MaxLives = 3;
        public static readonly TimeSpan LifeRegenInterval = TimeSpan.FromHours(4);

        private const string InitializedKey = "TowerMaze.MobileLife.Initialized";
        private const string LivesKey = "TowerMaze.MobileLife.CurrentLives";
        private const string RegenStartTicksKey = "TowerMaze.MobileLife.RegenStartTicksUtc";
        private const float TimerBroadcastIntervalSeconds = 1f;

        [SerializeField] private bool persistentSingleton = true;
        [SerializeField, Range(1, MaxLives)] private int defaultLivesOnFirstLaunch = MaxLives;

        private int currentLives;
        private long regenStartUtcTicks;
        private float nextTimerBroadcast;

        public static LifeManager Instance { get; private set; }

        public event Action<int, int> LivesChanged;
        public event Action<TimeSpan> NextLifeTimerChanged;

        public int CurrentLives => currentLives;
        public bool HasLives => currentLives > 0;
        public bool IsFull => currentLives >= MaxLives;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (persistentSingleton)
            {
                DontDestroyOnLoad(gameObject);
            }

            LoadState();
            RefreshLivesFromOfflineProgress();
            BroadcastLives();
            BroadcastTimer();
        }

        private void Update()
        {
            if (RefreshLivesFromOfflineProgress())
            {
                BroadcastLives();
            }

            nextTimerBroadcast -= Time.unscaledDeltaTime;
            if (nextTimerBroadcast <= 0f)
            {
                nextTimerBroadcast = TimerBroadcastIntervalSeconds;
                BroadcastTimer();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus)
            {
                if (RefreshLivesFromOfflineProgress())
                {
                    BroadcastLives();
                }

                BroadcastTimer();
            }
            else
            {
                SaveState();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                SaveState();
                return;
            }

            if (RefreshLivesFromOfflineProgress())
            {
                BroadcastLives();
            }

            BroadcastTimer();
        }

        public bool TryConsumeLife(int amount = 1)
        {
            amount = Mathf.Max(1, amount);
            RefreshLivesFromOfflineProgress();

            if (currentLives < amount)
            {
                BroadcastTimer();
                return false;
            }

            bool wasFull = currentLives >= MaxLives;
            currentLives = Mathf.Clamp(currentLives - amount, 0, MaxLives);

            if (currentLives < MaxLives && (wasFull || regenStartUtcTicks <= 0L))
            {
                regenStartUtcTicks = GetUtcNowTicks();
            }

            SaveState();
            BroadcastLives();
            BroadcastTimer();
            return true;
        }

        public void GrantLives(int amount = 1)
        {
            amount = Mathf.Max(1, amount);
            RefreshLivesFromOfflineProgress();

            currentLives = Mathf.Clamp(currentLives + amount, 0, MaxLives);
            if (currentLives >= MaxLives)
            {
                regenStartUtcTicks = 0L;
            }
            else if (regenStartUtcTicks <= 0L)
            {
                regenStartUtcTicks = GetUtcNowTicks();
            }

            SaveState();
            BroadcastLives();
            BroadcastTimer();
        }

        public TimeSpan GetTimeUntilNextLife()
        {
            if (RefreshLivesFromOfflineProgress())
            {
                BroadcastLives();
            }

            if (currentLives >= MaxLives)
            {
                return TimeSpan.Zero;
            }

            long nowTicks = GetUtcNowTicks();
            long startTicks = regenStartUtcTicks > 0L ? regenStartUtcTicks : nowTicks;
            long targetTicks = startTicks + LifeRegenInterval.Ticks;
            long remainingTicks = Math.Max(0L, targetTicks - nowTicks);
            return TimeSpan.FromTicks(remainingTicks);
        }

        public string GetNextLifeCountdownLabel()
        {
            TimeSpan remaining = GetTimeUntilNextLife();
            if (remaining <= TimeSpan.Zero)
            {
                return currentLives >= MaxLives ? "FULL" : "00:00:00";
            }

            return $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        }

        private void LoadState()
        {
            bool initialized = PlayerPrefs.GetInt(InitializedKey, 0) == 1;
            if (!initialized)
            {
                currentLives = Mathf.Clamp(defaultLivesOnFirstLaunch, 1, MaxLives);
                regenStartUtcTicks = 0L;
                PlayerPrefs.SetInt(InitializedKey, 1);
                SaveState();
                return;
            }

            currentLives = Mathf.Clamp(PlayerPrefs.GetInt(LivesKey, MaxLives), 0, MaxLives);
            if (!long.TryParse(PlayerPrefs.GetString(RegenStartTicksKey, "0"), out regenStartUtcTicks))
            {
                regenStartUtcTicks = 0L;
            }
        }

        private bool RefreshLivesFromOfflineProgress()
        {
            if (currentLives >= MaxLives)
            {
                if (regenStartUtcTicks != 0L)
                {
                    regenStartUtcTicks = 0L;
                    SaveState();
                }

                return false;
            }

            long nowTicks = GetUtcNowTicks();
            if (regenStartUtcTicks <= 0L)
            {
                regenStartUtcTicks = nowTicks;
                SaveState();
                return false;
            }

            long elapsedTicks = Math.Max(0L, nowTicks - regenStartUtcTicks);
            long intervalTicks = LifeRegenInterval.Ticks;
            int restoredLives = (int)(elapsedTicks / intervalTicks);
            if (restoredLives <= 0)
            {
                return false;
            }

            int previousLives = currentLives;
            currentLives = Mathf.Clamp(currentLives + restoredLives, 0, MaxLives);
            if (currentLives >= MaxLives)
            {
                regenStartUtcTicks = 0L;
            }
            else
            {
                regenStartUtcTicks += restoredLives * intervalTicks;
            }

            bool changed = currentLives != previousLives;
            if (changed)
            {
                SaveState();
            }

            return changed;
        }

        private void BroadcastLives()
        {
            LivesChanged?.Invoke(currentLives, MaxLives);
        }

        private void BroadcastTimer()
        {
            NextLifeTimerChanged?.Invoke(GetTimeUntilNextLife());
        }

        private void SaveState()
        {
            PlayerPrefs.SetInt(LivesKey, Mathf.Clamp(currentLives, 0, MaxLives));
            PlayerPrefs.SetString(RegenStartTicksKey, regenStartUtcTicks.ToString());
            PlayerPrefs.Save();
        }

        private static long GetUtcNowTicks()
        {
            return DateTime.UtcNow.Ticks;
        }
    }
}
