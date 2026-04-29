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
            public readonly FlipSettings FlipSettings;
            public readonly int Seed;
            public readonly string DisplayName;

            public ChapterDefinition(
                int index,
                int tierIndex,
                float complexity,
                float targetHeight,
                float sinkSpeed,
                MazeSettings mazeSettings,
                FlipSettings flipSettings,
                int seed)
            {
                Index = index;
                TierIndex = tierIndex;
                Complexity = complexity;
                TargetHeight = targetHeight;
                SinkSpeed = sinkSpeed;
                MazeSettings = mazeSettings;
                FlipSettings = flipSettings;
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
        // Safety margin = how much slower than the optimal solver path the player is allowed
        // to be before the lava catches them. 2.5x at chapter 1 gives beginners ~150% extra
        // time to reach the goal; 1.15x at chapter 500 keeps experts on edge.
        private const float SafetyMarginCh1 = 2.50f;
        private const float SafetyMarginCh500 = 1.15f;
        // Calibrated from in-game measurement: player vertical progress through a maze
        // with circumferential detours averages ~55% of climbSpeed at low complexity and
        // ~30% at high complexity. The 0.95/0.50 range from the spec was over-optimistic
        // and produced sinkSpeeds that were unbeatable even with optimal play.
        private const float MazeEffMax = 0.55f;
        private const float MazeEffMin = 0.30f;
        // Control flip is suppressed entirely below this chapter so new players can learn
        // the core climbing mechanic before inverted controls debut. From chapter 16
        // onward, flip parameters lerp on the same smoothstep complexity curve as
        // MazeSettings/SinkSpeed.
        public const int FlipFirstChapter = 16;
        private const int FlipStartZoneEarly = 8;
        private const int FlipStartZoneLate = 4;
        private const int FlipRepeatEveryEarly = 5;
        private const int FlipRepeatEveryLate = 3;
        private const float FlipDurationEarly = 5f;
        private const float FlipDurationLate = 10f;
        private const float FlipWarningEarly = 1.2f;
        private const float FlipWarningLate = 0.6f;
        private const float FlipScalingEarly = 0f;
        private const float FlipScalingLate = 1.5f;
        private const string KeyUnlocked = "TowerMaze.UnlockedChapters";
        private const string KeyBestPrefix = "TowerMaze.ChapterBest.";
        private const string KeySeedAttemptPrefix = "TowerMaze.ChapterSeedAttempt.";

        public int UnlockedUpTo { get; private set; }
        public int ActiveChapterIndex { get; private set; }

        private ChapterDefinition[] _chapters;

        public void Initialize(int baseSeed, float ballPlayerSpeed, ChapterSeedTable preValidatedTable = null)
        {
            UnlockedUpTo = PlayerPrefs.GetInt(KeyUnlocked, 1);
            _chapters = new ChapterDefinition[TotalChapters];
            for (int i = 1; i <= TotalChapters; i++)
            {
                int tier = ComputeTierIndex(i);
                float complexity = ComputeComplexity(i);
                float targetHeight = ComputeTargetHeight(i);
                MazeSettings mazeSettings = ComputeMazeSettings(i);
                FlipSettings flipSettings = ComputeFlipSettings(i);
                int attempt = preValidatedTable != null
                    ? preValidatedTable.GetAttempt(i)
                    : PlayerPrefs.GetInt(KeySeedAttemptPrefix + i, 0);
                int seed = ComputeSeed(baseSeed, i, attempt);

                // Prefer the solver-derived sinkSpeed baked by PreValidateChaptersTool;
                // fall back to the formula estimate when no table or no baked value exists.
                float bakedSink = preValidatedTable != null ? preValidatedTable.GetSinkSpeed(i) : 0f;
                float sinkSpeed = bakedSink > 0f ? bakedSink : ComputeSinkSpeed(i, ballPlayerSpeed);

                _chapters[i - 1] = new ChapterDefinition(i, tier, complexity, targetHeight, sinkSpeed, mazeSettings, flipSettings, seed);
            }
        }

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

        public static float Smoothstep(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        public static int ComputeTierIndex(int n) => ((n - 1) / ChaptersPerTier) + 1;

        public static float ComputeNormalizedT(int n) => (n - 1) / (float)(TotalChapters - 1);

        public static float ComputeComplexity(int n) => Smoothstep(ComputeNormalizedT(n));

        public static float ComputeTargetHeight(int n)
        {
            float s = Smoothstep(ComputeNormalizedT(n));
            return Mathf.Lerp(HMin, HMax, s);
        }

        public static float ComputeMazeEfficiency(float c) => Mathf.Lerp(MazeEffMax, MazeEffMin, c);

        public static float ComputeSafetyMargin(float c) => Mathf.Lerp(SafetyMarginCh1, SafetyMarginCh500, c);

        public static float ComputeSinkSpeed(int n, float ballPlayerSpeed)
        {
            float c = ComputeComplexity(n);
            float h = ComputeTargetHeight(n);
            float playerEff = ballPlayerSpeed * ComputeMazeEfficiency(c);
            float expectedTime = h / Mathf.Max(0.01f, playerEff);
            float safety = ComputeSafetyMargin(c);
            return (h + LavaHeadStart) / Mathf.Max(0.01f, expectedTime * safety);
        }

        public static FlipSettings ComputeFlipSettings(int n)
        {
            if (n < FlipFirstChapter) return FlipSettings.Disabled;
            float c = ComputeComplexity(n);
            return new FlipSettings(
                enabled: true,
                startZone: Mathf.RoundToInt(Mathf.Lerp(FlipStartZoneEarly, FlipStartZoneLate, c)),
                repeatEveryZones: Mathf.RoundToInt(Mathf.Lerp(FlipRepeatEveryEarly, FlipRepeatEveryLate, c)),
                duration: Mathf.Lerp(FlipDurationEarly, FlipDurationLate, c),
                warningDuration: Mathf.Lerp(FlipWarningEarly, FlipWarningLate, c),
                durationIncreasePerTrigger: Mathf.Lerp(FlipScalingEarly, FlipScalingLate, c));
        }

        public static MazeSettings ComputeMazeSettings(int n)
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

        public static int ComputeSeed(int baseSeed, int n, int attempt)
        {
            return (baseSeed * 31) ^ (n * 7919) ^ (attempt * 12911);
        }
    }
}
