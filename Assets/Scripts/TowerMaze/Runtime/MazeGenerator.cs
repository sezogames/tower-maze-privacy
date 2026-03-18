using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerMaze
{
    public sealed class MazeGenerator
    {
        private readonly struct LogicalNode
        {
            public readonly int Column;
            public readonly int Row;

            public LogicalNode(int column, int row)
            {
                Column = column;
                Row = row;
            }
        }

        private static readonly Vector2Int[] NeighborOffsets =
        {
            new(1, 0),
            new(-1, 0),
            new(0, 1),
            new(0, -1),
        };

        public SegmentData CreateTutorialSegment(GameConfig config, ThemeDefinition theme, int segmentIndex, int entryColumn)
        {
            DifficultySettings tutorialSettings = new()
            {
                pathTwistiness = 0.18f,
                branchDensity = 0.2f,
                deadEndDensity = 0.24f,
                decisionDensity = 0.2f,
                rotationSpeed = config.baseRotationSpeed,
                sinkSpeed = config.baseSinkSpeed,
                minimumDecisionPoints = 2,
                minimumDeadEnds = 2,
            };

            return GenerateWrappedMaze(config, theme, segmentIndex, entryColumn, config.seed + segmentIndex, 0, tutorialSettings, true);
        }

        public SegmentData Generate(GameConfig config, DifficultyProfile difficultyProfile, ThemeDefinition theme, int segmentIndex, int zoneIndex, int entryColumn, int seed)
        {
            float segmentHeight = segmentIndex * config.segmentHeight;
            DifficultySettings settings = ApplyZoneComplexity(config, difficultyProfile.Evaluate(segmentHeight), zoneIndex);
            int tier = difficultyProfile.GetBandIndex(segmentHeight);

            for (int attempt = 0; attempt < config.maxRegenerationAttempts; attempt++)
            {
                SegmentData data = GenerateWrappedMaze(
                    config,
                    theme,
                    segmentIndex,
                    entryColumn,
                    seed + (attempt * 97),
                    tier,
                    settings,
                    false);

                if (Validate(data, settings))
                {
                    return data;
                }
            }

            return GenerateWrappedMaze(config, theme, segmentIndex, entryColumn, seed + 701, tier, settings, false);
        }

        private SegmentData GenerateWrappedMaze(
            GameConfig config,
            ThemeDefinition theme,
            int segmentIndex,
            int entryColumn,
            int seed,
            int tier,
            DifficultySettings settings,
            bool tutorial)
        {
            SegmentData data = new()
            {
                themeId = theme.themeId,
                seed = seed,
                segmentIndex = segmentIndex,
                difficultyTier = tier,
            };
            data.Initialize(config.mazeWidthCells, config.mazeHeightCells);

            int logicalColumns = Mathf.Max(2, config.mazeWidthCells / 2);
            int logicalRows = Mathf.Max(2, Mathf.CeilToInt(config.mazeHeightCells / 2f));
            int entryNodeColumn = WrapLogicalColumn(Mathf.RoundToInt(entryColumn / 2f), logicalColumns);

            System.Random random = new(seed);
            CarvePerfectMaze(data, logicalColumns, logicalRows, entryNodeColumn, random, settings, tutorial);
            OpenExtraConnections(data, logicalColumns, logicalRows, random, settings, tutorial);

            data.entryColumn = LogicalToGridColumn(entryNodeColumn);
            data.exitColumn = LogicalToGridColumn(SelectExitNodeColumn(data, logicalColumns, logicalRows, entryNodeColumn, tutorial));
            OpenTopExit(data, logicalRows, data.exitColumn);
            MarkMainPath(data, data.entryColumn, data.exitColumn);
            return data;
        }

        private static void CarvePerfectMaze(
            SegmentData data,
            int logicalColumns,
            int logicalRows,
            int entryNodeColumn,
            System.Random random,
            DifficultySettings settings,
            bool tutorial)
        {
            bool[,] visited = new bool[logicalRows, logicalColumns];
            Stack<LogicalNode> stack = new();

            visited[0, entryNodeColumn] = true;
            OpenNode(data, entryNodeColumn, 0);
            stack.Push(new LogicalNode(entryNodeColumn, 0));

            while (stack.Count > 0)
            {
                LogicalNode current = stack.Peek();
                List<LogicalNode> unvisitedNeighbors = GetUnvisitedNeighbors(current, visited, logicalColumns, logicalRows);
                if (unvisitedNeighbors.Count == 0)
                {
                    stack.Pop();
                    continue;
                }

                LogicalNode? previous = stack.Count > 1 ? stack.ToArray()[1] : null;
                LogicalNode next = ChooseNextNeighbor(current, previous, unvisitedNeighbors, logicalColumns, logicalRows, random, settings, tutorial);
                visited[next.Row, next.Column] = true;
                OpenConnection(data, current, next, logicalColumns);
                OpenNode(data, next.Column, next.Row);
                stack.Push(next);
            }
        }

        private static List<LogicalNode> GetUnvisitedNeighbors(LogicalNode current, bool[,] visited, int logicalColumns, int logicalRows)
        {
            List<LogicalNode> neighbors = new();
            foreach (Vector2Int offset in NeighborOffsets)
            {
                int nextRow = current.Row + offset.y;
                if (nextRow < 0 || nextRow >= logicalRows)
                {
                    continue;
                }

                int nextColumn = WrapLogicalColumn(current.Column + offset.x, logicalColumns);
                if (visited[nextRow, nextColumn])
                {
                    continue;
                }

                neighbors.Add(new LogicalNode(nextColumn, nextRow));
            }

            return neighbors;
        }

        private static LogicalNode ChooseNextNeighbor(
            LogicalNode current,
            LogicalNode? previous,
            List<LogicalNode> neighbors,
            int logicalColumns,
            int logicalRows,
            System.Random random,
            DifficultySettings settings,
            bool tutorial)
        {
            if (!tutorial)
            {
                float totalWeight = 0f;
                float[] weights = new float[neighbors.Count];
                Vector2Int previousDirection = previous.HasValue
                    ? GetDirection(previous.Value, current, logicalColumns)
                    : Vector2Int.zero;

                for (int i = 0; i < neighbors.Count; i++)
                {
                    LogicalNode neighbor = neighbors[i];
                    Vector2Int nextDirection = GetDirection(current, neighbor, logicalColumns);
                    bool isVerticalAdvance = neighbor.Row > current.Row;
                    bool isSideStep = neighbor.Row == current.Row;
                    bool keepsDirection = previous.HasValue && nextDirection == previousDirection;

                    float weight = 1f;
                    if (isVerticalAdvance)
                    {
                        weight += Mathf.Lerp(0.3f, 1.05f, settings.decisionDensity);
                    }

                    if (isSideStep)
                    {
                        weight += Mathf.Lerp(0.15f, 1.1f, settings.pathTwistiness);
                    }

                    if (keepsDirection)
                    {
                        weight += Mathf.Lerp(0.9f, 0.2f, settings.pathTwistiness);
                    }
                    else if (previous.HasValue)
                    {
                        weight += Mathf.Lerp(0.1f, 0.95f, settings.pathTwistiness);
                    }

                    int distanceFromEntry = GetWrappedColumnDistance(current.Column, neighbor.Column, logicalColumns);
                    weight += distanceFromEntry * Mathf.Lerp(0.08f, 0.26f, settings.branchDensity);
                    weights[i] = Mathf.Max(0.05f, weight);
                    totalWeight += weights[i];
                }

                float pick = (float)random.NextDouble() * totalWeight;
                for (int i = 0; i < neighbors.Count; i++)
                {
                    pick -= weights[i];
                    if (pick <= 0f)
                    {
                        return neighbors[i];
                    }
                }

                return neighbors[^1];
            }

            neighbors.Sort((a, b) =>
            {
                int aVerticalPriority = a.Row > current.Row ? 0 : 1;
                int bVerticalPriority = b.Row > current.Row ? 0 : 1;
                if (aVerticalPriority != bVerticalPriority)
                {
                    return aVerticalPriority.CompareTo(bVerticalPriority);
                }

                int aDistanceToTop = logicalRows - a.Row;
                int bDistanceToTop = logicalRows - b.Row;
                return aDistanceToTop.CompareTo(bDistanceToTop);
            });

            int choiceCount = Mathf.Min(2, neighbors.Count);
            return neighbors[random.Next(0, choiceCount)];
        }

        private static void OpenExtraConnections(
            SegmentData data,
            int logicalColumns,
            int logicalRows,
            System.Random random,
            DifficultySettings settings,
            bool tutorial)
        {
            if (tutorial)
            {
                return;
            }

            List<(LogicalNode from, LogicalNode to)> candidates = new();
            for (int row = 0; row < logicalRows; row++)
            {
                for (int column = 0; column < logicalColumns; column++)
                {
                    LogicalNode current = new(column, row);
                    LogicalNode right = new(WrapLogicalColumn(column + 1, logicalColumns), row);
                    if (!IsConnectionOpen(data, current, right, logicalColumns))
                    {
                        candidates.Add((current, right));
                    }

                    if (row + 1 >= logicalRows)
                    {
                        continue;
                    }

                    LogicalNode up = new(column, row + 1);
                    if (!IsConnectionOpen(data, current, up, logicalColumns))
                    {
                        candidates.Add((current, up));
                    }
                }
            }

            float connectorDensity = Mathf.Clamp01((settings.branchDensity * 0.55f) + (settings.decisionDensity * 0.45f));
            int extraConnections = Mathf.Clamp(
                Mathf.RoundToInt(candidates.Count * Mathf.Lerp(0.04f, 0.2f, connectorDensity)),
                0,
                candidates.Count);

            for (int i = 0; i < extraConnections; i++)
            {
                int pickIndex = random.Next(i, candidates.Count);
                (candidates[i], candidates[pickIndex]) = (candidates[pickIndex], candidates[i]);
                OpenConnection(data, candidates[i].from, candidates[i].to, logicalColumns);
            }
        }

        private static void OpenNode(SegmentData data, int logicalColumn, int logicalRow)
        {
            data.SetCell(logicalRow * 2, LogicalToGridColumn(logicalColumn), MazeCellKind.Path);
        }

        private static void OpenConnection(SegmentData data, LogicalNode from, LogicalNode to, int logicalColumns)
        {
            int fromGridColumn = LogicalToGridColumn(from.Column);
            int fromGridRow = from.Row * 2;

            if (from.Row == to.Row)
            {
                int direction = GetWrappedHorizontalDirection(from.Column, to.Column, logicalColumns);
                int passageColumn = data.WrapColumn(fromGridColumn + direction);
                data.SetCell(fromGridRow, passageColumn, MazeCellKind.Path);
                return;
            }

            int passageRow = fromGridRow + (to.Row > from.Row ? 1 : -1);
            data.SetCell(passageRow, fromGridColumn, MazeCellKind.Path);
        }

        private static int SelectExitNodeColumn(SegmentData data, int logicalColumns, int logicalRows, int entryNodeColumn, bool tutorial)
        {
            int topNodeRow = Mathf.Min(data.height - 1, (logicalRows - 1) * 2);
            int entryGridColumn = LogicalToGridColumn(entryNodeColumn);

            int selectedColumn = entryNodeColumn;
            int selectedScore = tutorial ? int.MaxValue : int.MinValue;

            for (int logicalColumn = 0; logicalColumn < logicalColumns; logicalColumn++)
            {
                int targetGridColumn = LogicalToGridColumn(logicalColumn);
                int pathLength = GetShortestPathLength(data, entryGridColumn, 0, targetGridColumn, topNodeRow);
                if (pathLength < 0)
                {
                    continue;
                }

                int wrappedDistance = GetWrappedColumnDistance(entryGridColumn, targetGridColumn, data.width);
                int score = pathLength + (wrappedDistance * 2);

                if (tutorial)
                {
                    if (score < selectedScore)
                    {
                        selectedScore = score;
                        selectedColumn = logicalColumn;
                    }
                }
                else if (score > selectedScore)
                {
                    selectedScore = score;
                    selectedColumn = logicalColumn;
                }
            }

            return selectedColumn;
        }

        private static void OpenTopExit(SegmentData data, int logicalRows, int exitColumn)
        {
            int topNodeRow = Mathf.Min(data.height - 1, (logicalRows - 1) * 2);
            for (int row = topNodeRow; row < data.height; row++)
            {
                data.SetCell(row, exitColumn, MazeCellKind.Path);
            }
        }

        private static void MarkMainPath(SegmentData data, int entryColumn, int exitColumn)
        {
            Vector2Int start = new(data.WrapColumn(entryColumn), 0);
            Vector2Int target = new(data.WrapColumn(exitColumn), data.height - 1);
            Queue<Vector2Int> queue = new();
            Dictionary<int, int> parents = new();
            int startKey = Encode(start.x, start.y, data.width);
            int targetKey = Encode(target.x, target.y, data.width);

            queue.Enqueue(start);
            parents[startKey] = -1;

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                int currentKey = Encode(current.x, current.y, data.width);
                if (currentKey == targetKey)
                {
                    break;
                }

                foreach (Vector2Int neighbor in GetOpenNeighbors(data, current.x, current.y))
                {
                    int neighborKey = Encode(neighbor.x, neighbor.y, data.width);
                    if (parents.ContainsKey(neighborKey))
                    {
                        continue;
                    }

                    parents[neighborKey] = currentKey;
                    queue.Enqueue(neighbor);
                }
            }

            if (!parents.ContainsKey(targetKey))
            {
                return;
            }

            int traceKey = targetKey;
            while (traceKey >= 0)
            {
                int row = traceKey / data.width;
                int column = traceKey % data.width;
                data.SetCell(row, column, MazeCellKind.MainPath);
                traceKey = parents.TryGetValue(traceKey, out int parentKey) ? parentKey : -1;
            }
        }

        private static int GetShortestPathLength(SegmentData data, int startColumn, int startRow, int targetColumn, int targetRow)
        {
            Queue<Vector2Int> queue = new();
            Dictionary<int, int> distanceByCell = new();
            Vector2Int start = new(data.WrapColumn(startColumn), startRow);
            int startKey = Encode(start.x, start.y, data.width);
            int targetKey = Encode(data.WrapColumn(targetColumn), targetRow, data.width);

            queue.Enqueue(start);
            distanceByCell[startKey] = 0;

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                int currentKey = Encode(current.x, current.y, data.width);
                if (currentKey == targetKey)
                {
                    return distanceByCell[currentKey];
                }

                foreach (Vector2Int neighbor in GetOpenNeighbors(data, current.x, current.y))
                {
                    int neighborKey = Encode(neighbor.x, neighbor.y, data.width);
                    if (distanceByCell.ContainsKey(neighborKey))
                    {
                        continue;
                    }

                    distanceByCell[neighborKey] = distanceByCell[currentKey] + 1;
                    queue.Enqueue(neighbor);
                }
            }

            return -1;
        }

        private static IEnumerable<Vector2Int> GetOpenNeighbors(SegmentData data, int column, int row)
        {
            int[] rowOffsets = { 1, -1 };
            foreach (int rowOffset in rowOffsets)
            {
                int nextRow = row + rowOffset;
                if (nextRow >= 0 && nextRow < data.height && data.IsOpen(nextRow, column))
                {
                    yield return new Vector2Int(column, nextRow);
                }
            }

            int leftColumn = data.WrapColumn(column - 1);
            if (data.IsOpen(row, leftColumn))
            {
                yield return new Vector2Int(leftColumn, row);
            }

            int rightColumn = data.WrapColumn(column + 1);
            if (data.IsOpen(row, rightColumn))
            {
                yield return new Vector2Int(rightColumn, row);
            }
        }

        private static int GetWrappedHorizontalDirection(int fromColumn, int toColumn, int logicalColumns)
        {
            if (WrapLogicalColumn(fromColumn + 1, logicalColumns) == toColumn)
            {
                return 1;
            }

            return -1;
        }

        private static bool IsConnectionOpen(SegmentData data, LogicalNode from, LogicalNode to, int logicalColumns)
        {
            int fromGridColumn = LogicalToGridColumn(from.Column);
            int fromGridRow = from.Row * 2;

            if (from.Row == to.Row)
            {
                int direction = GetWrappedHorizontalDirection(from.Column, to.Column, logicalColumns);
                return data.IsOpen(fromGridRow, data.WrapColumn(fromGridColumn + direction));
            }

            int passageRow = fromGridRow + (to.Row > from.Row ? 1 : -1);
            return data.IsOpen(passageRow, fromGridColumn);
        }

        private static Vector2Int GetDirection(LogicalNode from, LogicalNode to, int logicalColumns)
        {
            if (from.Row != to.Row)
            {
                return new Vector2Int(0, Math.Sign(to.Row - from.Row));
            }

            int columnDelta = GetWrappedHorizontalDirection(from.Column, to.Column, logicalColumns);
            return new Vector2Int(columnDelta, 0);
        }

        private static int WrapLogicalColumn(int column, int logicalColumns)
        {
            int wrapped = column % logicalColumns;
            return wrapped < 0 ? wrapped + logicalColumns : wrapped;
        }

        private static int LogicalToGridColumn(int logicalColumn)
        {
            return logicalColumn * 2;
        }

        private static int GetWrappedColumnDistance(int fromColumn, int toColumn, int width)
        {
            int direct = Mathf.Abs(toColumn - fromColumn);
            return Mathf.Min(direct, width - direct);
        }

        private static DifficultySettings ApplyZoneComplexity(GameConfig config, DifficultySettings settings, int zoneIndex)
        {
            if (zoneIndex <= 0)
            {
                return settings;
            }

            int progressionTier = Mathf.Min(zoneIndex, 8);
            float complexityBonus = progressionTier * config.zoneComplexityStep;
            settings.pathTwistiness = Mathf.Clamp01(settings.pathTwistiness + complexityBonus);
            settings.branchDensity = Mathf.Clamp01(settings.branchDensity + (complexityBonus * 0.9f));
            settings.deadEndDensity = Mathf.Clamp01(settings.deadEndDensity + (complexityBonus * 0.7f));
            settings.decisionDensity = Mathf.Clamp01(settings.decisionDensity + complexityBonus);
            settings.minimumDecisionPoints += progressionTier * config.zoneDecisionPointStep;
            settings.minimumDeadEnds += progressionTier * config.zoneDeadEndStep;
            return settings;
        }

        private static bool Validate(SegmentData data, DifficultySettings settings)
        {
            if (!HasPathToExit(data))
            {
                return false;
            }

            if (data.CountDecisionPoints() < settings.minimumDecisionPoints)
            {
                return false;
            }

            if (data.CountDeadEnds() < settings.minimumDeadEnds)
            {
                return false;
            }

            return true;
        }

        private static bool HasPathToExit(SegmentData data)
        {
            Queue<Vector2Int> queue = new();
            HashSet<int> visited = new();
            Vector2Int start = new(data.WrapColumn(data.entryColumn), 0);
            queue.Enqueue(start);
            visited.Add(Encode(start.x, start.y, data.width));

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                if (current.y == data.height - 1 && current.x == data.WrapColumn(data.exitColumn))
                {
                    return true;
                }

                foreach (Vector2Int neighbor in GetOpenNeighbors(data, current.x, current.y))
                {
                    int encoded = Encode(neighbor.x, neighbor.y, data.width);
                    if (visited.Add(encoded))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return false;
        }

        private static int Encode(int column, int row, int width)
        {
            return (row * width) + column;
        }
    }
}
