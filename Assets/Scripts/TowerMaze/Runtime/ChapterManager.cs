using System;
using System.Collections;
using UnityEngine;

namespace TowerMaze
{
    public class ChapterManager : MonoBehaviour
    {
        public readonly struct ChapterDefinition
        {
            public readonly int Index;
            public readonly int TierIndex;
            public readonly float Complexity;
            public readonly float TargetHeight;
            public readonly float SinkSpeed;
            public readonly MazeSettings MazeSettings;
            public readonly int Seed;
            public readonly string DisplayName;

            public ChapterDefinition(
                int index,
                int tierIndex,
                float complexity,
                float targetHeight,
                float sinkSpeed,
                MazeSettings mazeSettings,
                int seed)
            {
                Index = index;
                TierIndex = tierIndex;
                Complexity = complexity;
                TargetHeight = targetHeight;
                SinkSpeed = sinkSpeed;
                MazeSettings = mazeSettings;
                Seed = seed;
                DisplayName = $"LEVEL {index}";
            }
        }

        public const int TotalChapters = 500;
        public const int ChaptersPerTier = 50;
        public const int TotalTiers = TotalChapters / ChaptersPerTier;
        private const float HMin = 50f;
        private const float HMax = 500f;
        private const float LavaHeadStart = 8f;
        private const float SafetyMarginCh1 = 1.30f;
        private const float SafetyMarginCh500 = 1.05f;
        private const float MazeEffMax = 0.95f;
        private const float MazeEffMin = 0.50f;
        private const float DefaultBallPlayerSpeed = 4f;
        private const string KeyUnlocked = "TowerMaze.UnlockedChapters";
        private const string KeyBestPrefix = "TowerMaze.ChapterBest.";
        private const string KeySeedAttemptPrefix = "TowerMaze.ChapterSeedAttempt.";

        public int UnlockedUpTo { get; private set; }
        public int ActiveChapterIndex { get; private set; }

        private ChapterDefinition[] _chapters;

        public void Initialize(int baseSeed, float ballPlayerSpeed)
        {
            UnlockedUpTo = PlayerPrefs.GetInt(KeyUnlocked, 1);
            _chapters = new ChapterDefinition[TotalChapters];
            for (int i = 1; i <= TotalChapters; i++)
            {
                int tier = ComputeTierIndex(i);
                float complexity = ComputeComplexity(i);
                float targetHeight = ComputeTargetHeight(i);
                float sinkSpeed = ComputeSinkSpeed(i, ballPlayerSpeed);
                MazeSettings mazeSettings = ComputeMazeSettings(i);
                int attempt = PlayerPrefs.GetInt(KeySeedAttemptPrefix + i, 0);
                int seed = ComputeSeed(baseSeed, i, attempt);
                _chapters[i - 1] = new ChapterDefinition(i, tier, complexity, targetHeight, sinkSpeed, mazeSettings, seed);
            }
        }

        [System.Obsolete("Use Initialize(baseSeed, ballPlayerSpeed). Kept for compile compatibility during migration.")]
        public void Initialize(int baseSeed) => Initialize(baseSeed, DefaultBallPlayerSpeed);

        /// <summary>
        /// Runs the chapter validator (one-shot, cached via PlayerPrefs flag) and then
        /// builds the chapter table from the validated seed attempts. Use this on first
        /// boot so the splash overlay can drive a progress bar while seeds bake.
        /// </summary>
        public IEnumerator InitializeAsync(
            int baseSeed,
            float ballPlayerSpeed,
            GameConfig config,
            DifficultyProfile difficultyProfile,
            ThemeDefinition theme,
            Action<float> progressCallback)
        {
            var validator = new ChapterValidator(config, difficultyProfile, theme);
            yield return validator.ValidateAll(baseSeed, ballPlayerSpeed, progressCallback);
            Initialize(baseSeed, ballPlayerSpeed);
        }

        public ChapterDefinition GetChapter(int index)
        {
            index = Mathf.Clamp(index, 1, TotalChapters);
            return _chapters[index - 1];
        }

        public float GetBestHeight(int index)
        {
            return PlayerPrefs.GetFloat(KeyBestPrefix + index, 0f);
        }

        public bool IsUnlocked(int index)
        {
            return index <= UnlockedUpTo;
        }

        public bool IsCompleted(int index)
        {
            return GetBestHeight(index) >= GetChapter(index).TargetHeight;
        }

        public void SetActiveChapter(int index)
        {
            ActiveChapterIndex = Mathf.Clamp(index, 1, TotalChapters);
        }

        public void RecordChapterBest(int index, float height)
        {
            string key = KeyBestPrefix + index;
            if (height > PlayerPrefs.GetFloat(key, 0f))
            {
                PlayerPrefs.SetFloat(key, height);
                PlayerPrefs.Save();
            }
        }

        public void RecordChapterComplete(int index, float reachedHeight)
        {
            RecordChapterBest(index, reachedHeight);
            if (index == UnlockedUpTo && index < TotalChapters)
            {
                UnlockedUpTo = index + 1;
                PlayerPrefs.SetInt(KeyUnlocked, UnlockedUpTo);
                PlayerPrefs.Save();
            }
        }

        internal static float Smoothstep(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        internal static int ComputeTierIndex(int n) => ((n - 1) / ChaptersPerTier) + 1;

        internal static float ComputeNormalizedT(int n) => (n - 1) / (float)(TotalChapters - 1);

        internal static float ComputeComplexity(int n) => Smoothstep(ComputeNormalizedT(n));

        internal static float ComputeTargetHeight(int n)
        {
            float s = Smoothstep(ComputeNormalizedT(n));
            return Mathf.Lerp(HMin, HMax, s);
        }

        internal static float ComputeMazeEfficiency(float c) => Mathf.Lerp(MazeEffMax, MazeEffMin, c);

        internal static float ComputeSafetyMargin(float c) => Mathf.Lerp(SafetyMarginCh1, SafetyMarginCh500, c);

        internal static float ComputeSinkSpeed(int n, float ballPlayerSpeed)
        {
            float c = ComputeComplexity(n);
            float h = ComputeTargetHeight(n);
            float playerEff = ballPlayerSpeed * ComputeMazeEfficiency(c);
            float expectedTime = h / Mathf.Max(0.01f, playerEff);
            float safety = ComputeSafetyMargin(c);
            return (h + LavaHeadStart) / Mathf.Max(0.01f, expectedTime * safety);
        }

        internal static MazeSettings ComputeMazeSettings(int n)
        {
            float c = ComputeComplexity(n);
            return new MazeSettings(
                pathTwistiness:    Mathf.Lerp(0.18f, 0.65f, c),
                branchDensity:     Mathf.Lerp(0.30f, 0.78f, c),
                deadEndDensity:    Mathf.Lerp(0.18f, 0.72f, c),
                decisionDensity:   Mathf.Lerp(0.24f, 0.66f, c),
                minDecisionPoints: Mathf.RoundToInt(Mathf.Lerp(2f, 6f, c)),
                minDeadEnds:       Mathf.RoundToInt(Mathf.Lerp(1f, 7f, c)));
        }

        internal static int ComputeSeed(int baseSeed, int n, int attempt)
        {
            return (baseSeed * 31) ^ (n * 7919) ^ (attempt * 12911);
        }
    }
}
