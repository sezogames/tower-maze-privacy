using UnityEngine;

namespace TowerMaze
{
    public readonly struct FlipSettings
    {
        public readonly bool enabled;
        public readonly int startZone;
        public readonly int repeatEveryZones;
        public readonly float duration;
        public readonly float warningDuration;
        public readonly float durationIncreasePerTrigger;

        public FlipSettings(
            bool enabled,
            int startZone,
            int repeatEveryZones,
            float duration,
            float warningDuration,
            float durationIncreasePerTrigger)
        {
            this.enabled = enabled;
            this.startZone = Mathf.Max(1, startZone);
            this.repeatEveryZones = Mathf.Max(1, repeatEveryZones);
            this.duration = Mathf.Max(0f, duration);
            this.warningDuration = Mathf.Max(0f, warningDuration);
            this.durationIncreasePerTrigger = Mathf.Max(0f, durationIncreasePerTrigger);
        }

        public static FlipSettings Disabled => new FlipSettings(false, int.MaxValue, 1, 0f, 0f, 0f);
    }
}
