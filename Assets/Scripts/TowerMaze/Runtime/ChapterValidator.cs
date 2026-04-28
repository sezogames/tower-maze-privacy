using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerMaze
{
    /// <summary>
    /// Foundation adapter for chapter-mode maze solvability validation.
    /// Generates the chapter's maze (across multiple stacked segments) and flattens
    /// the per-segment <see cref="SegmentData"/> into a single 2D cell-wall grid.
    /// The actual A* solver and persistence layers (Tasks 3.2, 3.3) build on top of
    /// the grid produced by <see cref="BuildPreviewGrid"/>.
    /// </summary>
    public sealed class ChapterValidator
    {
        public readonly struct CellWalls
        {
            public readonly bool wallN;
            public readonly bool wallS;
            public readonly bool wallE;
            public readonly bool wallW;

            public CellWalls(bool n, bool s, bool e, bool w)
            {
                wallN = n;
                wallS = s;
                wallE = e;
                wallW = w;
            }
        }

        private readonly GameConfig config;
        private readonly DifficultyProfile difficultyProfile;
        private readonly ThemeDefinition theme;
        private readonly MazeGenerator mazeGenerator = new MazeGenerator();

        public ChapterValidator(GameConfig config, DifficultyProfile difficultyProfile, ThemeDefinition theme)
        {
            this.config = config;
            this.difficultyProfile = difficultyProfile;
            this.theme = theme;
        }

        /// <summary>
        /// Builds a flattened cell-wall grid for a chapter's maze up to <paramref name="targetHeight"/>.
        /// Rows count from the base of the tower upward. Columns wrap circularly (east/west).
        /// Wall convention: <c>wallN[r,c]==true</c> means a wall exists between cells
        /// (r, c) and (r+1, c). A boundary "wall" is asserted whenever either side is not
        /// an open path cell in the underlying <see cref="SegmentData"/>.
        /// </summary>
        public CellWalls[,] BuildPreviewGrid(
            int seed,
            MazeSettings mazeSettings,
            float targetHeight,
            out int rows,
            out int cols)
        {
            int rowsPerSegment = Mathf.Max(1, config.mazeHeightCells);
            int segmentsForHeight = Mathf.Max(1, Mathf.CeilToInt(targetHeight / Mathf.Max(0.0001f, config.segmentHeight)));
            // +2 segments of buffer so goal cells exist above the requested height.
            int targetSegments = segmentsForHeight + 2;

            cols = Mathf.Max(2, config.mazeWidthCells);
            rows = targetSegments * rowsPerSegment;

            CellWalls[,] grid = new CellWalls[rows, cols];

            // Cache each segment's per-cell openness so cross-segment seam walls can be
            // computed by looking at the bottom row of segment N+1 versus the top row of N.
            bool[][,] segmentOpen = new bool[targetSegments][,];

            int entryColumn = cols / 2;
            for (int seg = 0; seg < targetSegments; seg++)
            {
                int zoneIndex = seg / Mathf.Max(1, config.segmentsPerZone);
                int segmentSeed = seed ^ (seg * 31);
                SegmentData data = seg == 0
                    ? mazeGenerator.CreateTutorialSegment(config, theme, seg, entryColumn)
                    : mazeGenerator.GenerateWithSettings(config, mazeSettings, theme, seg, zoneIndex, entryColumn, segmentSeed);

                bool[,] openMask = new bool[rowsPerSegment, cols];
                for (int r = 0; r < rowsPerSegment; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        openMask[r, c] = data.IsOpen(r, c);
                    }
                }
                segmentOpen[seg] = openMask;

                entryColumn = data.exitColumn;
            }

            for (int seg = 0; seg < targetSegments; seg++)
            {
                bool[,] openMask = segmentOpen[seg];
                for (int r = 0; r < rowsPerSegment; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        int globalRow = (seg * rowsPerSegment) + r;
                        grid[globalRow, c] = ComputeWalls(seg, r, c, segmentOpen, rowsPerSegment, cols, targetSegments);
                    }
                }
            }

            return grid;
        }

        /// <summary>
        /// A cell carries a wall on side X when itself is not open OR the neighbor on side X
        /// is not open. East/west wrap modulo <paramref name="cols"/>. North/south crossing
        /// a segment boundary reads from the adjacent segment's mask.
        /// </summary>
        private static CellWalls ComputeWalls(
            int seg,
            int localRow,
            int col,
            bool[][,] segmentOpen,
            int rowsPerSegment,
            int cols,
            int totalSegments)
        {
            bool selfOpen = segmentOpen[seg][localRow, col];

            // North neighbor: localRow + 1, possibly into next segment.
            bool northOpen;
            if (localRow + 1 < rowsPerSegment)
            {
                northOpen = segmentOpen[seg][localRow + 1, col];
            }
            else if (seg + 1 < totalSegments)
            {
                northOpen = segmentOpen[seg + 1][0, col];
            }
            else
            {
                northOpen = false;
            }

            // South neighbor: localRow - 1, possibly into previous segment.
            bool southOpen;
            if (localRow - 1 >= 0)
            {
                southOpen = segmentOpen[seg][localRow - 1, col];
            }
            else if (seg - 1 >= 0)
            {
                southOpen = segmentOpen[seg - 1][rowsPerSegment - 1, col];
            }
            else
            {
                southOpen = false;
            }

            int eastCol = (col + 1) % cols;
            int westCol = ((col - 1) % cols + cols) % cols;
            bool eastOpen = segmentOpen[seg][localRow, eastCol];
            bool westOpen = segmentOpen[seg][localRow, westCol];

            return new CellWalls(
                n: !(selfOpen && northOpen),
                s: !(selfOpen && southOpen),
                e: !(selfOpen && eastOpen),
                w: !(selfOpen && westOpen));
        }

        /// <summary>
        /// Runs A* on the previewed maze for a single seed and returns the optimal
        /// traversal time. <see cref="float.PositiveInfinity"/> means the goal is unreachable.
        /// Use this when you want to derive sinkSpeed from the actual maze rather than rely
        /// on the formula's mazeEfficiency estimate.
        /// </summary>
        public float MeasureOptimalTime(int seed, MazeSettings mazeSettings, float targetHeight, float ballPlayerSpeed)
        {
            float cellHeight = config.CellHeight;
            float edgeCost = cellHeight / Mathf.Max(0.01f, ballPlayerSpeed);
            CellWalls[,] grid = BuildPreviewGrid(seed, mazeSettings, targetHeight, out int rows, out int cols);
            int targetRow = Mathf.Min(rows - 1, Mathf.CeilToInt(targetHeight / Mathf.Max(0.0001f, cellHeight)));
            return AStarMinTime(grid, rows, cols, targetRow, edgeCost);
        }

        private const int MaxAttempts = 16;
        private const float LavaHeadStart = 8f;
        private const string KeyValidatedFlag = "TowerMaze.ChaptersValidated.v1";
        private const string KeySeedAttemptPrefix = "TowerMaze.ChapterSeedAttempt.";

        /// <summary>
        /// Validates every chapter once, persisting the accepted attempt index per chapter
        /// to PlayerPrefs so subsequent boots reproduce the same vetted seed. Yields once
        /// per 10 chapters to keep the splash overlay responsive. Idempotent: a flagged
        /// run short-circuits immediately.
        /// </summary>
        public IEnumerator ValidateAll(int baseSeed, float ballPlayerSpeed, Action<float> progressCallback)
        {
            if (PlayerPrefs.GetInt(KeyValidatedFlag, 0) == 1)
            {
                progressCallback?.Invoke(1f);
                yield break;
            }

            for (int n = 1; n <= ChapterManager.TotalChapters; n++)
            {
                float c = ChapterManager.ComputeComplexity(n);
                float targetHeight = ChapterManager.ComputeTargetHeight(n);
                float sinkSpeed = ChapterManager.ComputeSinkSpeed(n, ballPlayerSpeed);
                float safetyMargin = ChapterManager.ComputeSafetyMargin(c);
                MazeSettings settings = ChapterManager.ComputeMazeSettings(n);

                TryValidateChapter(n, baseSeed, targetHeight, settings, sinkSpeed, safetyMargin, ballPlayerSpeed, out int attempt);
                PlayerPrefs.SetInt(KeySeedAttemptPrefix + n, attempt);

                if (n % 10 == 0)
                {
                    PlayerPrefs.Save();
                    progressCallback?.Invoke(n / (float)ChapterManager.TotalChapters);
                    yield return null;
                }
            }

            PlayerPrefs.SetInt(KeyValidatedFlag, 1);
            PlayerPrefs.Save();
            progressCallback?.Invoke(1f);
        }

        /// <summary>
        /// Re-rolls the chapter seed up to <see cref="MaxAttempts"/> times until the
        /// optimal traversal time (A* on the flattened cell-wall grid) fits inside the
        /// lava budget multiplied by <paramref name="safetyMargin"/>. The accepted attempt
        /// index is returned so callers can persist it and reproduce the validated seed.
        /// </summary>
        public bool TryValidateChapter(
            int chapterIndex,
            int baseSeed,
            float targetHeight,
            MazeSettings mazeSettings,
            float sinkSpeed,
            float safetyMargin,
            float ballPlayerSpeed,
            out int validatedAttempt)
        {
            return TryValidateChapter(chapterIndex, baseSeed, targetHeight, mazeSettings, sinkSpeed,
                safetyMargin, ballPlayerSpeed, out validatedAttempt, out _);
        }

        /// <summary>
        /// Same as <see cref="TryValidateChapter"/> but also reports the A* optimal traversal
        /// time of the validated seed. Pre-bake tooling uses this to derive a sinkSpeed that
        /// matches the actual maze (sinkSpeed = (targetHeight+headStart) / (optimalTime*safety))
        /// instead of the formula-estimated value.
        /// </summary>
        public bool TryValidateChapter(
            int chapterIndex,
            int baseSeed,
            float targetHeight,
            MazeSettings mazeSettings,
            float sinkSpeed,
            float safetyMargin,
            float ballPlayerSpeed,
            out int validatedAttempt,
            out float validatedOptimalTime)
        {
            float cellHeight = config.CellHeight;
            float edgeCost = cellHeight / Mathf.Max(0.01f, ballPlayerSpeed);
            float lavaBudget = (targetHeight + LavaHeadStart) / Mathf.Max(0.01f, sinkSpeed);

            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                int seed = (baseSeed * 31) ^ (chapterIndex * 7919) ^ (attempt * 12911);
                CellWalls[,] grid = BuildPreviewGrid(seed, mazeSettings, targetHeight, out int rows, out int cols);

                int targetRow = Mathf.Min(rows - 1, Mathf.CeilToInt(targetHeight / Mathf.Max(0.0001f, cellHeight)));
                float optimalTime = AStarMinTime(grid, rows, cols, targetRow, edgeCost);
                if (float.IsPositiveInfinity(optimalTime)) continue;

                if (optimalTime * safetyMargin <= lavaBudget)
                {
                    validatedAttempt = attempt;
                    validatedOptimalTime = optimalTime;
                    return true;
                }
            }

            Debug.LogError($"[ChapterValidator] Chapter {chapterIndex} failed validation after {MaxAttempts} attempts");
            validatedAttempt = MaxAttempts - 1;
            validatedOptimalTime = float.PositiveInfinity;
            return false;
        }

        /// <summary>
        /// A* on the cell grid with admissible heuristic <c>(targetRow - row) * edgeCost</c>.
        /// Start frontier: the entire bottom row (player x-position is unconstrained at spawn).
        /// Goal: any cell whose row index is &gt;= <paramref name="targetRow"/>. Edge cost is
        /// uniform; obstacle is the wall flag on the side of travel. East/west wrap modulo cols.
        /// </summary>
        private static float AStarMinTime(CellWalls[,] grid, int rows, int cols, int targetRow, float edgeCost)
        {
            float[,] cost = new float[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    cost[r, c] = float.PositiveInfinity;

            var open = new MinHeap();
            for (int c = 0; c < cols; c++)
            {
                cost[0, c] = 0f;
                open.Push(targetRow * edgeCost, 0, c);
            }

            while (open.Count > 0)
            {
                open.Pop(out int row, out int col, out float priority);
                float baseCost = cost[row, col];
                // Stale entry — a cheaper path was found after this one was queued.
                if (priority > baseCost + Mathf.Max(0, targetRow - row) * edgeCost + 1e-5f) continue;

                if (row >= targetRow) return baseCost;

                CellWalls walls = grid[row, col];

                if (!walls.wallN && row + 1 < rows)
                    Relax(grid, cost, open, row + 1, col, baseCost + edgeCost, targetRow, edgeCost);
                if (!walls.wallS && row - 1 >= 0)
                    Relax(grid, cost, open, row - 1, col, baseCost + edgeCost, targetRow, edgeCost);
                if (!walls.wallE)
                {
                    int eastCol = (col + 1) % cols;
                    Relax(grid, cost, open, row, eastCol, baseCost + edgeCost, targetRow, edgeCost);
                }
                if (!walls.wallW)
                {
                    int westCol = ((col - 1) % cols + cols) % cols;
                    Relax(grid, cost, open, row, westCol, baseCost + edgeCost, targetRow, edgeCost);
                }
            }

            return float.PositiveInfinity;
        }

        private static void Relax(CellWalls[,] grid, float[,] cost, MinHeap open,
            int toRow, int toCol, float newCost, int targetRow, float edgeCost)
        {
            if (newCost >= cost[toRow, toCol]) return;
            cost[toRow, toCol] = newCost;
            float h = Mathf.Max(0, targetRow - toRow) * edgeCost;
            open.Push(newCost + h, toRow, toCol);
        }

        /// <summary>
        /// Min-heap keyed on f = g + h. Stores (row, col) payload directly to avoid tuple
        /// allocations. Stale entries are filtered at pop time via cost-table re-check.
        /// </summary>
        private sealed class MinHeap
        {
            private readonly List<Entry> heap = new List<Entry>(256);

            public int Count => heap.Count;

            public void Push(float priority, int row, int col)
            {
                heap.Add(new Entry { priority = priority, row = row, col = col });
                SiftUp(heap.Count - 1);
            }

            public void Pop(out int row, out int col, out float priority)
            {
                Entry root = heap[0];
                row = root.row;
                col = root.col;
                priority = root.priority;
                int last = heap.Count - 1;
                heap[0] = heap[last];
                heap.RemoveAt(last);
                if (heap.Count > 0) SiftDown(0);
            }

            private void SiftUp(int i)
            {
                while (i > 0)
                {
                    int parent = (i - 1) >> 1;
                    if (heap[parent].priority <= heap[i].priority) break;
                    (heap[parent], heap[i]) = (heap[i], heap[parent]);
                    i = parent;
                }
            }

            private void SiftDown(int i)
            {
                int n = heap.Count;
                while (true)
                {
                    int left = (i << 1) + 1;
                    int right = left + 1;
                    int smallest = i;
                    if (left < n && heap[left].priority < heap[smallest].priority) smallest = left;
                    if (right < n && heap[right].priority < heap[smallest].priority) smallest = right;
                    if (smallest == i) break;
                    (heap[smallest], heap[i]) = (heap[i], heap[smallest]);
                    i = smallest;
                }
            }

            private struct Entry
            {
                public float priority;
                public int row;
                public int col;
            }
        }
    }
}
