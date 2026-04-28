using UnityEngine;

namespace TowerMaze
{
    [CreateAssetMenu(menuName = "TowerMaze/Chapter Seed Table", fileName = "ChapterSeedTable")]
    public sealed class ChapterSeedTable : ScriptableObject
    {
        [SerializeField] private int[] attempts = new int[ChapterManager.TotalChapters];

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

        public void EnsureSize()
        {
            if (attempts == null || attempts.Length != ChapterManager.TotalChapters)
            {
                System.Array.Resize(ref attempts, ChapterManager.TotalChapters);
            }
        }
    }
}
