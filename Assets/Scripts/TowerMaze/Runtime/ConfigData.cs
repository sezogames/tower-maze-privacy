using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerMaze
{
    public enum MazeCellKind
    {
        Wall = 0,
        Path = 1,
        MainPath = 2
    }

    [CreateAssetMenu(menuName = "TowerMaze/Game Config", fileName = "GameConfig")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Tower")]
        [Min(1f)] public float towerRadius = 4f;
        [Min(1f)] public float segmentHeight = 8f;
        [Min(6)] public int mazeWidthCells = 28;
        [Min(4)] public int mazeHeightCells = 16;
        [Min(0f)] public float pathRecessDepth = 0.58f;
        [Min(0.2f)] public float pathInsetThickness = 0.74f;
        [Min(0.2f)] public float wallThickness = 1.16f;
        [Range(0.5f, 1.05f)] public float pathCellPadding = 1f;
        [Range(0.5f, 1.05f)] public float wallCellPadding = 1f;
        [Min(0f)] public float cellWidthOverlap = 0.035f;
        [Min(0f)] public float cellHeightOverlap = 0.028f;
        [Min(0f)] public float cellDepthOverlap = 0.02f;
        [Range(0f, 0.45f)] public float wallCornerRoundness = 0.16f;
        [Range(1, 8)] public int wallCornerSegments = 5;

        [Header("Movement")]
        [Min(0.5f)] public float climbSpeed = 2.88f;
        [Min(10f)] public float horizontalSpeedDegrees = 91.8f;
        [Min(0.1f)] public float dragSensitivity = 2.1f;
        [Range(0f, 0.5f)] public float verticalDragDeadZone = 0.08f;
        [Min(0f)] public float heroSurfaceOffset = 0.05f;
        [Range(0.1f, 1f)] public float heroCollisionWidthCells = 0.66f;
        [Range(0.1f, 1f)] public float heroCollisionHeightCells = 0.92f;

        [Header("Run Pressure")]
        [Min(0f)] public float baseRotationSpeed = 8f;
        [Min(0f)] public float baseSinkSpeed = 0.35f;
        [Min(0f)] public float sinkAccelerationPerMinute = 0f;
        [Min(0f)] public float lavaFailGrace = 0.2f;
        [Min(0f)] public float startingGrace = 2.5f;
        [Min(0f)] public float continueLiftCells = 3f;
        [Min(0)] public int continueCount = 1;

        [Header("Lava Rush")]
        [Min(0f)] public float rushStartDelay = 11f;
        [Min(1f)] public float rushIntervalMin = 12.5f;
        [Min(1f)] public float rushIntervalMax = 15.5f;
        [Min(0f)] public float rushIntervalReductionAtMaxHeight = 2.5f;
        [Min(1f)] public float rushIntervalReductionHeight = 90f;
        [Min(0f)] public float rushWarningDuration = 0.9f;
        [Min(0f)] public float rushDuration = 4f;
        [Min(1f)] public float rushWarningSpeedMultiplier = 1.2f;
        [Min(1f)] public float rushSpeedMultiplier = 1.85f;
        [Min(0f)] public float rushContinueGrace = 5f;

        [Header("Run Flow")]
        [Min(0f)] public float startCountdownSeconds = 3f;
        [Min(0f)] public float countdownGoSeconds = 0.6f;

        [Header("Rewarded Ads")]
        public bool useSimulatedRewardedAds = true;
        [Min(0f)] public float simulatedRewardedDuration = 0.8f;
        public string androidRewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";
        public string iosRewardedAdUnitId = "ca-app-pub-3940256099942544/1712485313";

        [Header("Cloud / PlayFab")]
        public bool enablePlayFabCloudSync;
        public string playFabTitleId = string.Empty;
        public string playFabSaveKey = "towermaze_save";
        public string playFabStatisticName = "best_height_cm";
        [Range(3, 20)] public int playFabLeaderboardSize = 5;
        public string playFabCustomIdOverride = string.Empty;

        [Header("Control Flip")]
        [Min(1)] public int controlFlipStartZone = 5;
        [Min(1)] public int controlFlipRepeatEveryZones = 3;
        [Min(0f)] public float controlFlipWarningDuration = 0.8f;
        [Min(0f)] public float controlFlipDuration = 8f;
        [Min(0f)] public float controlFlipDurationIncreasePerTrigger = 1f;

        [Header("Streaming")]
        [Min(2)] public int initialSegments = 4;
        [Min(1)] public int spawnAheadSegments = 3;
        [Min(1)] public int keepBehindSegments = 2;
        [Min(1)] public int maxRegenerationAttempts = 16;
        public int seed = 1347;

        [Header("Maze Progression")]
        [Min(1)] public int segmentsPerZone = 3;
        [Range(0f, 0.3f)] public float zoneComplexityStep = 0.06f;
        [Min(0)] public int zoneDecisionPointStep = 1;
        [Min(0)] public int zoneDeadEndStep = 1;

        [Header("Feedback")]
        [Min(0f)] public float nearLavaDistance = 5f;
        [Min(0f)] public float failVfxDuration = 0.9f;

        [Header("HUD")]
        [Tooltip("Max height value for the HUD progress bar (m). Default 200.")]
        public float heightProgressMax = 200f;
        [Tooltip("Max height used for milestone progress bar (m). Defaults to heightProgressMax.")]
        public float milestoneMax = 200f;
        [Tooltip("Heights (m) at which milestone toasts are shown.")]
        public int[] heightMilestones = new int[] { 25, 50, 100 };
        [Tooltip("Minimum seconds after fail before retry button becomes active.")]
        public float failToRetryDelay = 1.5f;

        public float CellHeight => segmentHeight / mazeHeightCells;
        public float AnglePerCell => 360f / mazeWidthCells;
        public float Circumference => Mathf.PI * 2f * towerRadius;
        public float CellArcWidth => Circumference / mazeWidthCells;
        public float PathOuterRadius => Mathf.Max(0.1f, towerRadius - pathRecessDepth);
        public float HeroLaneRadius => PathOuterRadius + heroSurfaceOffset;
        public float ZoneHeight => segmentHeight * Mathf.Max(1, segmentsPerZone);
    }

    [Serializable]
    public struct DifficultySettings
    {
        [Range(0f, 1f)] public float pathTwistiness;
        [Range(0f, 1f)] public float branchDensity;
        [Range(0f, 1f)] public float deadEndDensity;
        [Range(0f, 1f)] public float decisionDensity;
        public float rotationSpeed;
        public float sinkSpeed;
        public int minimumDecisionPoints;
        public int minimumDeadEnds;
    }

    [Serializable]
    public sealed class DifficultyBand
    {
        [Min(0f)] public float minHeight;
        [Min(0f)] public float maxHeight = 20f;
        public DifficultySettings settings = new DifficultySettings
        {
            pathTwistiness = 0.25f,
            branchDensity = 0.42f,
            deadEndDensity = 0.28f,
            decisionDensity = 0.32f,
            rotationSpeed = 8f,
            sinkSpeed = 0.35f,
            minimumDecisionPoints = 3,
            minimumDeadEnds = 2,
        };

        public bool Includes(float height)
        {
            return height >= minHeight && height < maxHeight;
        }
    }

    [CreateAssetMenu(menuName = "TowerMaze/Difficulty Profile", fileName = "DifficultyProfile")]
    public sealed class DifficultyProfile : ScriptableObject
    {
        [SerializeField] private List<DifficultyBand> bands = new()
        {
            new DifficultyBand
            {
                minHeight = 0f,
                maxHeight = 20f,
                settings = new DifficultySettings
                {
                    pathTwistiness = 0.24f,
                    branchDensity = 0.42f,
                    deadEndDensity = 0.28f,
                    decisionDensity = 0.32f,
                    rotationSpeed = 8f,
                    sinkSpeed = 0.35f,
                    minimumDecisionPoints = 3,
                    minimumDeadEnds = 2,
                }
            },
            new DifficultyBand
            {
                minHeight = 20f,
                maxHeight = 60f,
                settings = new DifficultySettings
                {
                    pathTwistiness = 0.38f,
                    branchDensity = 0.56f,
                    deadEndDensity = 0.44f,
                    decisionDensity = 0.44f,
                    rotationSpeed = 10f,
                    sinkSpeed = 0.3875f,
                    minimumDecisionPoints = 4,
                    minimumDeadEnds = 4,
                }
            },
            new DifficultyBand
            {
                minHeight = 60f,
                maxHeight = 10000f,
                settings = new DifficultySettings
                {
                    pathTwistiness = 0.5f,
                    branchDensity = 0.68f,
                    deadEndDensity = 0.58f,
                    decisionDensity = 0.56f,
                    rotationSpeed = 12.5f,
                    sinkSpeed = 0.4375f,
                    minimumDecisionPoints = 5,
                    minimumDeadEnds = 5,
                }
            }
        };

        public IReadOnlyList<DifficultyBand> Bands => bands;

        public DifficultySettings Evaluate(float height)
        {
            foreach (DifficultyBand band in bands)
            {
                if (band.Includes(height))
                {
                    return band.settings;
                }
            }

            return bands.Count > 0 ? bands[^1].settings : default;
        }

        public int GetBandIndex(float height)
        {
            for (int i = 0; i < bands.Count; i++)
            {
                if (bands[i].Includes(height))
                {
                    return i;
                }
            }

            return Mathf.Max(0, bands.Count - 1);
        }
    }

    [CreateAssetMenu(menuName = "TowerMaze/Theme Definition", fileName = "ThemeDefinition")]
    public sealed class ThemeDefinition : ScriptableObject
    {
        public string themeId = "volcanic";

        [Header("Environment")]
        public Color skyColor = new(0.38f, 0.2f, 0.12f, 1f);
        public Color fogColor = new(0.35f, 0.19f, 0.15f, 1f);
        public Color towerPathColor = Color.white;
        public Color towerMainPathColor = Color.white;
        public Color towerWallColor = new(0.19f, 0.16f, 0.15f, 1f);
        public Color lavaColor = new(1f, 0.39f, 0.08f, 1f);
        public Color lavaEmissionColor = new(1f, 0.42f, 0.08f, 1f);
        public Color accentColor = new(0.486f, 0.227f, 0.929f, 1f); // #7C3AED
        public Color nearLavaOverlay = new(1f, 0.32f, 0.18f, 0.4f);

        [Header("Tower Textures")]
        public Texture2D towerWallBaseMap;
        public Texture2D towerWallNormalMap;
        public Vector2 towerWallTextureScale = new(2.2f, 0.75f);
        public Texture2D towerPathBaseMap;
        public Texture2D towerPathNormalMap;
        public Vector2 towerPathTextureScale = new(1.75f, 0.9f);
        public Texture2D towerMainPathBaseMap;
        public Texture2D towerMainPathNormalMap;
        public Vector2 towerMainPathTextureScale = new(1.85f, 0.95f);

        [Header("Hero")]
        public Color heroPrimary = new(0.12f, 0.12f, 0.14f, 1f);
        public Color heroSecondary = new(0.93f, 0.41f, 0.19f, 1f);
        public Color heroAccent = new(0.96f, 0.83f, 0.58f, 1f);
    }

    [Serializable]
    public sealed class SegmentData
    {
        [SerializeField] private MazeCellKind[] cells = Array.Empty<MazeCellKind>();

        public string themeId = string.Empty;
        public int seed;
        public int segmentIndex;
        public int difficultyTier;
        public int width;
        public int height;
        public int entryColumn;
        public int exitColumn;
        public float rotationModifier = 1f;

        public void Initialize(int width, int height)
        {
            this.width = width;
            this.height = height;
            cells = new MazeCellKind[width * height];
        }

        public MazeCellKind GetCell(int row, int column)
        {
            if (cells.Length == 0)
            {
                return MazeCellKind.Wall;
            }

            int wrappedColumn = WrapColumn(column);
            if (row < 0 || row >= height)
            {
                return MazeCellKind.Wall;
            }

            return cells[(row * width) + wrappedColumn];
        }

        public void SetCell(int row, int column, MazeCellKind cellKind)
        {
            if (cells.Length == 0)
            {
                return;
            }

            int wrappedColumn = WrapColumn(column);
            if (row < 0 || row >= height)
            {
                return;
            }

            cells[(row * width) + wrappedColumn] = cellKind;
        }

        public bool IsOpen(int row, int column)
        {
            MazeCellKind cellKind = GetCell(row, column);
            return cellKind == MazeCellKind.Path || cellKind == MazeCellKind.MainPath;
        }

        public int WrapColumn(int column)
        {
            if (width <= 0)
            {
                return 0;
            }

            int wrapped = column % width;
            return wrapped < 0 ? wrapped + width : wrapped;
        }

        public int CountDecisionPoints()
        {
            int count = 0;
            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    if (!IsOpen(row, column))
                    {
                        continue;
                    }

                    int neighbors = 0;
                    if (IsOpen(row + 1, column)) neighbors++;
                    if (IsOpen(row - 1, column)) neighbors++;
                    if (IsOpen(row, column + 1)) neighbors++;
                    if (IsOpen(row, column - 1)) neighbors++;

                    if (neighbors >= 3)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        public int CountDeadEnds()
        {
            int count = 0;
            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    if (!IsOpen(row, column))
                    {
                        continue;
                    }

                    int neighbors = 0;
                    if (IsOpen(row + 1, column)) neighbors++;
                    if (IsOpen(row - 1, column)) neighbors++;
                    if (IsOpen(row, column + 1)) neighbors++;
                    if (IsOpen(row, column - 1)) neighbors++;

                    if (neighbors == 1)
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }
}
