using UnityEngine;

namespace TowerMaze
{
    public class ChapterManager : MonoBehaviour
    {
        public readonly struct ChapterDefinition
        {
            public readonly int Index;
            public readonly float TargetHeight;
            public readonly int Seed;
            public readonly string DisplayName;
            public readonly float DifficultyOffset;
            public readonly int ZoneOffset;

            public ChapterDefinition(int index, float targetHeight, int seed, float difficultyOffset, int zoneOffset)
            {
                Index = index;
                TargetHeight = targetHeight;
                Seed = seed;
                DisplayName = $"LEVEL {index}";
                DifficultyOffset = difficultyOffset;
                ZoneOffset = zoneOffset;
            }
        }

        public const int TotalChapters = 50;
        private const float ZoneHeight = 24f;
        private const string KeyUnlocked = "TowerMaze.UnlockedChapters";
        private const string KeyBestPrefix = "TowerMaze.ChapterBest.";

        public int UnlockedUpTo { get; private set; }
        public int ActiveChapterIndex { get; private set; }

        private ChapterDefinition[] _chapters;

        public void Initialize(int baseSeed)
        {
            UnlockedUpTo = PlayerPrefs.GetInt(KeyUnlocked, 1);
            _chapters = new ChapterDefinition[TotalChapters];
            for (int i = 1; i <= TotalChapters; i++)
            {
                float targetHeight = ComputeTargetHeight(i);
                int seed = (baseSeed * 31) ^ (i * 7919);
                float diffOffset = ComputeDifficultyOffset(i);
                int zoneOff = ComputeZoneOffset(i);
                _chapters[i - 1] = new ChapterDefinition(i, targetHeight, seed, diffOffset, zoneOff);
            }
        }

        private static float ComputeDifficultyOffset(int n)
        {
            if (n <= 10) return (n - 1) * 8f;
            if (n <= 25) return 72f + (n - 11) * 12f;
            return 252f + (n - 26) * 16f;
        }

        private static int ComputeZoneOffset(int n) => (n - 1) / 3;

        private static float ComputeTargetHeight(int n)
        {
            float zones;
            if (n <= 10)
                zones = 2 + (n - 1);
            else if (n <= 25)
                zones = 12 + (n - 11) * 2;
            else
                zones = 44 + (n - 26) * 3;
            return zones * ZoneHeight;
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
    }
}
