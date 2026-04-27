using UnityEngine;

namespace TowerMaze
{
    public readonly struct MazeSettings
    {
        public readonly float pathTwistiness;
        public readonly float branchDensity;
        public readonly float deadEndDensity;
        public readonly float decisionDensity;
        public readonly int minDecisionPoints;
        public readonly int minDeadEnds;

        public MazeSettings(
            float pathTwistiness,
            float branchDensity,
            float deadEndDensity,
            float decisionDensity,
            int minDecisionPoints,
            int minDeadEnds)
        {
            this.pathTwistiness = Mathf.Clamp01(pathTwistiness);
            this.branchDensity = Mathf.Clamp01(branchDensity);
            this.deadEndDensity = Mathf.Clamp01(deadEndDensity);
            this.decisionDensity = Mathf.Clamp01(decisionDensity);
            this.minDecisionPoints = Mathf.Max(0, minDecisionPoints);
            this.minDeadEnds = Mathf.Max(0, minDeadEnds);
        }
    }
}
