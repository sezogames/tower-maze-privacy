using UnityEngine;

namespace TowerMaze
{
    [CreateAssetMenu(menuName = "TowerMaze/Chapter Seed Table", fileName = "ChapterSeedTable")]
    public sealed class ChapterSeedTable : ScriptableObject
    {
        [SerializeField] private int[] attempts = new int[ChapterManager.TotalChapters];
        [SerializeField] private float[] sinkSpeeds = new float[ChapterManager.TotalChapters];

        public int GetAttempt(int chapterIndex)
        {
            int i = chapterIndex - 1;
            if (attempts == null || i < 0 || i >= attempts.Length) return 0;
            return attempts[i];
        }

        public void SetAttempt(int chapterIndex, int attempt)
        {
            int i = chapterIndex - 1;
            if (attempts == null || i < 0 || i >= attempts.Length) return;
            attempts[i] = attempt;
        }

        // Returns 0 when no solver-derived value has been baked. Callers should treat
        // a non-positive value as "fall back to ChapterManager formula sinkSpeed".
        public float GetSinkSpeed(int chapterIndex)
        {
            int i = chapterIndex - 1;
            if (sinkSpeeds == null || i < 0 || i >= sinkSpeeds.Length) return 0f;
            return sinkSpeeds[i];
        }

        public void SetSinkSpeed(int chapterIndex, float sinkSpeed)
        {
            int i = chapterIndex - 1;
            if (sinkSpeeds == null || i < 0 || i >= sinkSpeeds.Length) return;
            sinkSpeeds[i] = sinkSpeed;
        }

        public void EnsureSize()
        {
            if (attempts == null || attempts.Length != ChapterManager.TotalChapters)
            {
                System.Array.Resize(ref attempts, ChapterManager.TotalChapters);
            }
            if (sinkSpeeds == null || sinkSpeeds.Length != ChapterManager.TotalChapters)
            {
                System.Array.Resize(ref sinkSpeeds, ChapterManager.TotalChapters);
            }
        }
    }
}
