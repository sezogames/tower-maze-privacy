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
    }
}
