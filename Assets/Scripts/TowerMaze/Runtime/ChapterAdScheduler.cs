using UnityEngine;

namespace TowerMaze
{
    /// <summary>
    /// Pure-logic scheduler that decides whether the just-completed chapter should
    /// trigger an interstitial. Cadence is bracketed by chapter range:
    ///   1..60    -> every 5
    ///   61..120  -> every 4
    ///   121..200 -> every 3
    ///   201..500 -> every 2
    /// Suppressed when the player owns NoAds, when a tier milestone (50, 100, ...)
    /// just completed, when the chapter just had its first clear, when the chapter
    /// run failed, or when the cooldown since the last interstitial has not elapsed.
    /// </summary>
    public sealed class ChapterAdScheduler
    {
        public const float MinCooldownSeconds = 60f;

        private float lastShownRealtime = -1000f;

        public bool ShouldShowInterstitial(
            int chapterIndex,
            bool isFirstClear,
            bool isFail,
            bool isTierMilestone,
            bool hasNoAds,
            float nowRealtimeSinceStartup)
        {
            if (hasNoAds) return false;
            if (isFail) return false;
            if (isFirstClear) return false;
            if (isTierMilestone) return false;

            int cadence = GetCadenceFor(chapterIndex);
            if (cadence <= 0) return false;
            if ((chapterIndex % cadence) != 0) return false;

            if (nowRealtimeSinceStartup - lastShownRealtime < MinCooldownSeconds) return false;
            return true;
        }

        public void NotifyShown(float nowRealtimeSinceStartup)
        {
            lastShownRealtime = nowRealtimeSinceStartup;
        }

        public static int GetCadenceFor(int chapterIndex)
        {
            if (chapterIndex <= 0) return 0;
            if (chapterIndex <= 60) return 5;
            if (chapterIndex <= 120) return 4;
            if (chapterIndex <= 200) return 3;
            return 2;
        }
    }
}
