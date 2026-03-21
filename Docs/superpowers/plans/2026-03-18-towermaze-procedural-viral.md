# TowerMaze Procedural & Viral Extension Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend TowerMaze with a procedural pattern library, near-miss/combo feedback, biome visual progression, and a competitive rank/share layer — all without replacing any existing systems.

**Architecture:** A new `ProceduralOrchestrator` (plain C# class) sits between `TowerGenerator` and `MazeGenerator` via an `ISegmentProvider` interface injected into `TowerGenerator`. All other new systems (`NearMissSystem`, `ComboSystem`, `BiomeSystem`) are MonoBehaviours initialized in `TowerMazeBootstrapper`. Static utility classes (`RankSystem`, `ShareSystem`, `RiskRewardBuilder`) have no MonoBehaviour lifecycle.

**Tech Stack:** Unity C#, Unity Test Framework (NUnit EditMode tests), TextMeshPro, MaterialPropertyBlock, Unity Purchasing (existing), PlayFab (existing), Android share intent, iOS UIActivityViewController via P/Invoke.

**Spec:** `docs/superpowers/specs/2026-03-18-towermaze-procedural-viral-design.md`

---

## File Map

### New Files
| Path | Type | Responsibility |
|------|------|---------------|
| `Assets/Scripts/TowerMaze/Runtime/ISegmentProvider.cs` | Interface | Contract between TowerGenerator and ProceduralOrchestrator |
| `Assets/Scripts/TowerMaze/Runtime/ProceduralOrchestrator.cs` | Plain C# | Director: routes segment requests to MazeGenerator or PatternLibrary |
| `Assets/Scripts/TowerMaze/Runtime/PatternDefinition.cs` | ScriptableObject | Single authored pattern (type, grid, weight, minHeight) |
| `Assets/Scripts/TowerMaze/Runtime/PatternLibrary.cs` | ScriptableObject | Ordered list of PatternDefinition assets |
| `Assets/Scripts/TowerMaze/Runtime/RiskRewardBuilder.cs` | Static utility | Generates dual-corridor SegmentData at runtime |
| `Assets/Scripts/TowerMaze/Runtime/CoinPickup.cs` | MonoBehaviour | Trigger collider, ember award, pool return |
| `Assets/Scripts/TowerMaze/Runtime/NearMissSystem.cs` | MonoBehaviour | Reads LastWallProximity, triggers slow-mo + shake + audio |
| `Assets/Scripts/TowerMaze/Runtime/ComboSystem.cs` | MonoBehaviour | Tracks clean-move streak, drives trail/light/TextMeshPro feedback |
| `Assets/Scripts/TowerMaze/Runtime/BiomeDefinition.cs` | ScriptableObject | Per-biome visual parameters |
| `Assets/Scripts/TowerMaze/Runtime/BiomeSystem.cs` | MonoBehaviour | Polls height, triggers TransitionToBiome |
| `Assets/Scripts/TowerMaze/Runtime/RankSystem.cs` | Static utility | Height → tier enum + percentile int |
| `Assets/Scripts/TowerMaze/Runtime/ShareSystem.cs` | Static utility | Native OS share sheet (Android/iOS/Editor) |
| `Assets/Scripts/TowerMaze/Editor/PatternEditorWindow.cs` | EditorWindow | 2D grid painter for PatternDefinition assets |
| `Assets/Plugins/iOS/NativeShare.mm` | ObjC plugin | UIActivityViewController wrapper |
| `Assets/Tests/EditMode/TowerMazeEditModeTests.cs` | NUnit EditMode | Shared file hosting: PatternLibraryTests, RiskRewardBuilderTests, BiomeSystemLogicTests, RankSystemTests |
| `Assets/Tests/EditMode/TowerMazeTests.asmdef` | Assembly def | Test assembly definition for all EditMode tests |

### Modified Files
| Path | Change |
|------|--------|
| `Assets/Scripts/TowerMaze/Runtime/TowerSystems.cs` | Add `ISegmentProvider SegmentProvider` property; modify `SpawnSegment` to call it |
| `Assets/Scripts/TowerMaze/Runtime/PlayerController.cs` | Add `LastWallProximity` float + `OnMovementResult(bool)` event |
| `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs` | Add `CurrentHeightMetres` and `IsNewBestThisRun` properties to ScoreManager |
| `Assets/Scripts/TowerMaze/Runtime/EnvironmentBackdropController.cs` | Add `TransitionToBiome(BiomeDefinition, float)` method + `_transitionT` timer |
| `Assets/Scripts/TowerMaze/Runtime/ConfigData.cs` | Add `percentileBuckets`, `patternInjectionInterval`, biome list to GameConfig |
| `Assets/Scripts/TowerMaze/Runtime/UISystems.cs` | Add rank badge, percentile bar, share button to run results screen |
| `Assets/Scripts/TowerMaze/Runtime/TowerMazeBootstrapper.cs` | Init ProceduralOrchestrator, NearMissSystem, ComboSystem, BiomeSystem |

---

## Chunk 1: Foundation — Interfaces, Data Structures & Minimal Existing-File Changes

### Task 1: Add ISegmentProvider interface

**Files:**
- Create: `Assets/Scripts/TowerMaze/Runtime/ISegmentProvider.cs`

- [ ] **Step 1: Create the interface**

```csharp
// Assets/Scripts/TowerMaze/Runtime/ISegmentProvider.cs
namespace TowerMaze
{
    public interface ISegmentProvider
    {
        SegmentData GetSegment(int segmentIndex, int entryColumn);
    }
}
```

- [ ] **Step 2: Verify it compiles — open Unity and check Console for errors**

---

### Task 2: Add `SegmentProvider` hook to TowerGenerator

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/TowerSystems.cs`

- [ ] **Step 1: Open `TowerSystems.cs`. Find the `TowerGenerator` class. Add the property after the existing private fields (around line 660):**

```csharp
// Add inside TowerGenerator class, after existing fields:
public ISegmentProvider SegmentProvider { get; set; }
```

- [ ] **Step 2: Find `SpawnSegment(int segmentIndex)`. Locate the line that calls `mazeGenerator.Generate(...)` or `CreateTutorialSegment(...)` for non-tutorial segments. Wrap the non-tutorial path with a provider check:**

The existing code likely looks like:
```csharp
private void SpawnSegment(int segmentIndex)
{
    SegmentData data;
    if (segmentIndex == 0)
        data = mazeGenerator.CreateTutorialSegment(gameConfig, theme, segmentIndex, lastExitColumn);
    else
        data = mazeGenerator.Generate(gameConfig, difficultyProfile, theme, segmentIndex, zoneIndex, lastExitColumn, seed + segmentIndex);
    // ... rest of method
}
```

Change the `else` branch to:
```csharp
    else if (SegmentProvider != null)
        data = SegmentProvider.GetSegment(segmentIndex, lastExitColumn);
    else
        data = mazeGenerator.Generate(gameConfig, difficultyProfile, theme, segmentIndex, zoneIndex, lastExitColumn, seed + segmentIndex);
```

- [ ] **Step 3: Open Unity, check Console — zero errors**

- [ ] **Step 4: Commit**
```bash
git add Assets/Scripts/TowerMaze/Runtime/ISegmentProvider.cs Assets/Scripts/TowerMaze/Runtime/TowerSystems.cs
git commit -m "feat: add ISegmentProvider hook to TowerGenerator"
```

---

### Task 3: Add `LastWallProximity` and `OnMovementResult` to PlayerController

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/PlayerController.cs`

- [ ] **Step 1: Open `PlayerController.cs`. Add two members after existing private fields:**

```csharp
// Public surface for NearMissSystem and ComboSystem
public float LastWallProximity { get; private set; }
public event System.Action<bool> OnMovementResult;
```

- [ ] **Step 2: Verify `IsPathOpen` accepts a third float parameter. Open `TowerSystems.cs`, find `TowerGenerator.IsPathOpen`. Confirm the signature is:**
```csharp
public bool IsPathOpen(float angleDegrees, float towerHeight, float angleClearanceDegrees = 0f, float heightClearance = 0f)
```
If the signature differs, note the actual third parameter name before proceeding.

- [ ] **Step 3: Find `TryMove(float targetAngle, float targetHeight)`. Modify it to compute and store wall proximity:**

```csharp
private bool TryMove(float targetAngle, float targetHeight)
{
    bool open = towerGenerator.IsPathOpen(targetAngle, targetHeight);
    // Probe increasing clearance to estimate distance to nearest wall.
    // LastWallProximity: 0.0 = wide open space, 1.0 = player is right against a wall.
    float clearance = 1f; // assume open until proven otherwise
    for (float c = 0.05f; c <= 0.5f; c += 0.05f)
    {
        if (!towerGenerator.IsPathOpen(targetAngle, targetHeight, c * gameConfig.AnglePerCell))
        {
            clearance = c / 0.5f; // 0.1 = very tight, 1.0 = touching wall
            break;
        }
    }
    // If move was blocked, proximity is maximum (1.0); otherwise use clearance probe
    LastWallProximity = open ? clearance : 1f;
    return open;
}
```

**Semantics:** `LastWallProximity` near **1.0 = very close to a wall** (danger zone). Near **0.0 = open space**. `NearMissSystem` fires when this value > 0.85 (close but not blocked).

- [ ] **Step 4: Find the `Tick(bool canMove)` method. Wire `_blockedThisTick` and fire `OnMovementResult` at the end of each tick:**

Add private field near other fields:
```csharp
private bool _blockedThisTick;
```

At the **start** of `Tick()` (first line in the method body):
```csharp
_blockedThisTick = false;
```

Inside `TriggerBlockedFeedback()` (first line):
```csharp
_blockedThisTick = true;
```

At the **end** of `Tick()` (last line before closing brace):
```csharp
OnMovementResult?.Invoke(!_blockedThisTick);
```

Event semantics: fires once per `Tick()` call. `true` = clean move, `false` = any sub-step was blocked this tick.

- [ ] **Step 5: Open Unity, check Console — zero errors**

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/PlayerController.cs
git commit -m "feat: expose LastWallProximity and OnMovementResult on PlayerController"
```

---

### Task 4: Add `CurrentHeightMetres` to ScoreManager

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs`

- [ ] **Step 1: Open `RunSystems.cs`. Find the `ScoreManager` class. Find the field that stores current height/score (likely `currentScore` or similar, described as "height in centimeters"). Add a property:**

```csharp
// Inside ScoreManager class:
// Score is stored in centimetres — expose metres for BiomeSystem
public float CurrentHeightMetres => currentScore / 100f;
```

**Note:** If the backing field is not `currentScore`, use whatever field `CurrentScore` returns. The expression is always `<raw_value> / 100f`.

- [ ] **Step 2: Open Unity, check Console — zero errors**

- [ ] **Step 3: Commit**
```bash
git add Assets/Scripts/TowerMaze/Runtime/RunSystems.cs
git commit -m "feat: expose CurrentHeightMetres on ScoreManager"
```

---

### Task 5: Add new fields to GameConfig / ConfigData

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/ConfigData.cs`

- [ ] **Step 1: Open `ConfigData.cs`. Find the `GameConfig` class. Add these fields after existing fields:**

```csharp
[Header("Procedural Orchestrator")]
[Tooltip("Every N segments, try to inject a scripted pattern. Random range.")]
public int patternInjectionIntervalMin = 4;
public int patternInjectionIntervalMax = 6;

[Header("Rank / Percentile")]
[Tooltip("Heights (metres) at p10, p20, ... p100. Updated from leaderboard.")]
public float[] percentileBuckets = { 5f, 15f, 30f, 50f, 80f, 120f, 180f, 250f, 350f, 500f };
```

- [ ] **Step 2: Open Unity, check Console — zero errors**

- [ ] **Step 3: Commit**
```bash
git add Assets/Scripts/TowerMaze/Runtime/ConfigData.cs
git commit -m "feat: add patternInjectionInterval and percentileBuckets to GameConfig"
```

---

## Chunk 2: ProceduralOrchestrator + Pattern Library + RiskReward + Coins

### Task 6: Create PatternDefinition and PatternLibrary ScriptableObjects

**Files:**
- Create: `Assets/Scripts/TowerMaze/Runtime/PatternDefinition.cs`
- Create: `Assets/Scripts/TowerMaze/Runtime/PatternLibrary.cs`

- [ ] **Step 1: Create PatternDefinition**

```csharp
// Assets/Scripts/TowerMaze/Runtime/PatternDefinition.cs
using UnityEngine;

namespace TowerMaze
{
    public enum PatternType { Straight, ZigZag, Spiral, SplitPath, RiskReward }

    [CreateAssetMenu(menuName = "TowerMaze/Pattern Definition", fileName = "NewPattern")]
    public class PatternDefinition : ScriptableObject
    {
        public PatternType patternType;
        public SegmentData segmentData;
        [Range(0f, 1f)] public float weight = 0.5f;
        public float minHeightToAppear = 0f;
        [TextArea] public string description;
    }
}
```

- [ ] **Step 2: Create PatternLibrary**

```csharp
// Assets/Scripts/TowerMaze/Runtime/PatternLibrary.cs
using UnityEngine;

namespace TowerMaze
{
    [CreateAssetMenu(menuName = "TowerMaze/Pattern Library", fileName = "PatternLibrary")]
    public class PatternLibrary : ScriptableObject
    {
        public PatternDefinition[] patterns = System.Array.Empty<PatternDefinition>();

        public PatternDefinition PickEligible(float currentHeight, System.Random rng)
        {
            // Collect eligible patterns (height requirement met)
            var eligible = new System.Collections.Generic.List<PatternDefinition>();
            float totalWeight = 0f;
            foreach (var p in patterns)
            {
                if (p != null && currentHeight >= p.minHeightToAppear)
                {
                    eligible.Add(p);
                    totalWeight += p.weight;
                }
            }
            if (eligible.Count == 0 || totalWeight <= 0f) return null;

            // Weighted random selection
            float roll = (float)rng.NextDouble() * totalWeight;
            float cumulative = 0f;
            foreach (var p in eligible)
            {
                cumulative += p.weight;
                if (roll <= cumulative) return p;
            }
            return eligible[eligible.Count - 1];
        }
    }
}
```

- [ ] **Step 3: Open Unity, check Console — zero errors**

- [ ] **Step 4: Commit**
```bash
git add Assets/Scripts/TowerMaze/Runtime/PatternDefinition.cs Assets/Scripts/TowerMaze/Runtime/PatternLibrary.cs
git commit -m "feat: add PatternDefinition and PatternLibrary ScriptableObjects"
```

---

### Task 7: Write and run EditMode tests for PatternLibrary.PickEligible

**Files:**
- Create: `Assets/Tests/EditMode/TowerMazeEditModeTests.cs` — shared file that hosts all EditMode test classes
- Create: `Assets/Tests/EditMode/TowerMazeTests.asmdef`

**Note on file naming:** The test file is named `TowerMazeEditModeTests.cs` and will host multiple test classes (`PatternLibraryTests`, `RiskRewardBuilderTests`, `BiomeSystemLogicTests`, `RankSystemTests`). This avoids confusion between file names and class names.

- [ ] **Step 1: Create test assembly definition at `Assets/Tests/EditMode/TowerMazeTests.asmdef`:**

```json
{
    "name": "TowerMaze.Tests.EditMode",
    "references": [
        "TowerMaze.Runtime"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

**Note:** Replace `"TowerMaze.Runtime"` with the actual assembly name. Open `Assets/Scripts/TowerMaze/Runtime` and look for an `.asmdef` file — use its `name` value. If no asmdef exists, remove the `references` array entry and the tests will use the default assembly.

- [ ] **Step 2: Create test file `Assets/Tests/EditMode/TowerMazeEditModeTests.cs`:**

```csharp
using NUnit.Framework;
using TowerMaze;

public class PatternLibraryTests
{
    [Test]
    public void PickEligible_ReturnsNull_WhenNoPatterns()
    {
        var lib = UnityEngine.ScriptableObject.CreateInstance<PatternLibrary>();
        lib.patterns = System.Array.Empty<PatternDefinition>();
        var result = lib.PickEligible(0f, new System.Random(42));
        Assert.IsNull(result);
        UnityEngine.Object.DestroyImmediate(lib);
    }

    [Test]
    public void PickEligible_SkipsPatterns_BelowMinHeight()
    {
        var lib = UnityEngine.ScriptableObject.CreateInstance<PatternLibrary>();
        var p = UnityEngine.ScriptableObject.CreateInstance<PatternDefinition>();
        p.weight = 1f;
        p.minHeightToAppear = 100f;
        lib.patterns = new[] { p };

        var result = lib.PickEligible(50f, new System.Random(42));
        Assert.IsNull(result);
        UnityEngine.Object.DestroyImmediate(p);
        UnityEngine.Object.DestroyImmediate(lib);
    }

    [Test]
    public void PickEligible_ReturnsPattern_WhenHeightMet()
    {
        var lib = UnityEngine.ScriptableObject.CreateInstance<PatternLibrary>();
        var p = UnityEngine.ScriptableObject.CreateInstance<PatternDefinition>();
        p.weight = 1f;
        p.minHeightToAppear = 0f;
        lib.patterns = new[] { p };

        var result = lib.PickEligible(50f, new System.Random(42));
        Assert.AreEqual(p, result);
        UnityEngine.Object.DestroyImmediate(p);
        UnityEngine.Object.DestroyImmediate(lib);
    }
}
```

- [ ] **Step 3: In Unity, open Window → General → Test Runner → EditMode tab → Run All**
Expected: 3 tests PASS

- [ ] **Step 4: Commit**

```bash
git add Assets/Tests/
git commit -m "test: add TowerMaze EditMode test assembly and PatternLibrary tests"
```

---

### Task 8: Create RiskRewardBuilder

**Files:**
- Create: `Assets/Scripts/TowerMaze/Runtime/RiskRewardBuilder.cs`

- [ ] **Step 1: Add `RiskRewardBuilderTests` class to `TowerMazeEditModeTests.cs` (append after the existing `PatternLibraryTests` class):**

**Note:** `SegmentData.IsOpen(int row, int col)` already exists on the class — no addition needed. `coinColumns` and `coinRows` arrays are added to `SegmentData` in Step 4 below (before tests run).

```csharp
public class RiskRewardBuilderTests
{
    private static GameConfig MakeConfig()
    {
        var cfg = UnityEngine.ScriptableObject.CreateInstance<GameConfig>();
        // GameConfig.mazeWidthCells and mazeHeightCells default to 28 and 16.
        // If fields are not set by CreateInstance, set them here:
        // cfg.mazeWidthCells = 28; cfg.mazeHeightCells = 16;
        return cfg;
    }

    [Test]
    public void Build_HasTwoVerticalCorridors()
    {
        var cfg = MakeConfig();
        var data = RiskRewardBuilder.Build(cfg, segmentIndex: 5, entryColumn: 14, seed: 42);

        Assert.AreEqual(cfg.mazeWidthCells, data.width);
        Assert.AreEqual(cfg.mazeHeightCells, data.height);
        Assert.IsTrue(data.IsOpen(0, data.entryColumn));
        Assert.IsTrue(data.IsOpen(data.height - 1, data.exitColumn));
        UnityEngine.Object.DestroyImmediate(cfg);
    }

    [Test]
    public void Build_HasCoinPositions()
    {
        var cfg = MakeConfig();
        var data = RiskRewardBuilder.Build(cfg, segmentIndex: 5, entryColumn: 14, seed: 42);
        Assert.IsNotNull(data.coinColumns);
        Assert.GreaterOrEqual(data.coinColumns.Length, 3);
        UnityEngine.Object.DestroyImmediate(cfg);
    }
}
```

- [ ] **Step 2: Run the test — expect COMPILE ERROR (RiskRewardBuilder and coinColumns don't exist yet)**

- [ ] **Step 3: Create `RiskRewardBuilder.cs`:**

```csharp
// Assets/Scripts/TowerMaze/Runtime/RiskRewardBuilder.cs
using UnityEngine;

namespace TowerMaze
{
    public static class RiskRewardBuilder
    {
        /// <summary>
        /// Generates a segment with two parallel vertical corridors.
        /// Left corridor: narrow (1 cell wide) with coin pickups.
        /// Right corridor: wide (3 cells), no coins.
        /// Both converge at the exit row.
        /// </summary>
        public static SegmentData Build(GameConfig config, int segmentIndex, int entryColumn, int seed)
        {
            int w = config.mazeWidthCells;
            int h = config.mazeHeightCells;
            var data = new SegmentData();
            data.Initialize(w, h);
            data.segmentIndex = segmentIndex;
            data.difficultyTier = 1;

            // Place walls everywhere first
            for (int row = 0; row < h; row++)
                for (int col = 0; col < w; col++)
                    data.SetCell(row, col, MazeCellKind.Wall);

            // Left (narrow/risky) corridor: 1 cell wide, at entryColumn
            int leftCol = data.WrapColumn(entryColumn);
            // Right (wide/safe) corridor: 3 cells wide, offset by 6 cells
            int rightColCenter = data.WrapColumn(entryColumn + 6);

            for (int row = 0; row < h; row++)
            {
                // Narrow left corridor
                data.SetCell(row, leftCol, MazeCellKind.Path);
                // Wide right corridor (3 cells)
                data.SetCell(row, data.WrapColumn(rightColCenter - 1), MazeCellKind.Path);
                data.SetCell(row, rightColCenter, MazeCellKind.Path);
                data.SetCell(row, data.WrapColumn(rightColCenter + 1), MazeCellKind.Path);
            }

            // Open top and bottom rows fully between both corridors (merge point)
            int minCol = Mathf.Min(leftCol, data.WrapColumn(rightColCenter - 1));
            int maxCol = Mathf.Max(leftCol, data.WrapColumn(rightColCenter + 1));
            for (int col = minCol; col <= maxCol; col++)
            {
                data.SetCell(0, col, MazeCellKind.Path);
                data.SetCell(h - 1, col, MazeCellKind.Path);
            }

            data.entryColumn = leftCol;
            data.exitColumn  = leftCol;

            // Coin positions: rows 2, 4, 6 on the narrow corridor
            var rng = new System.Random(seed);
            int coinCount = 3 + rng.Next(3); // 3-5
            data.coinColumns = new int[coinCount];
            data.coinRows    = new int[coinCount];
            int step = Mathf.Max(1, (h - 2) / coinCount);
            for (int i = 0; i < coinCount; i++)
            {
                data.coinRows[i]    = 1 + i * step;
                data.coinColumns[i] = leftCol;
            }

            return data;
        }
    }
}
```

- [ ] **Step 4: Add coin arrays to SegmentData in `ConfigData.cs` (do this before Step 5):**

Open `ConfigData.cs`, find `SegmentData` class, add after existing fields:
```csharp
// Coin pickup positions (set by RiskRewardBuilder, null otherwise)
public int[] coinRows;
public int[] coinColumns;
```

- [ ] **Step 5: Run tests in Unity Test Runner — expect PASS**

- [ ] **Step 6: Commit**
```bash
git add Assets/Scripts/TowerMaze/Runtime/RiskRewardBuilder.cs Assets/Scripts/TowerMaze/Runtime/ConfigData.cs Assets/Tests/EditMode/RiskRewardBuilderTests.cs
git commit -m "feat: add RiskRewardBuilder with coin position data"
```

---

### Task 9: Create ProceduralOrchestrator

**Files:**
- Create: `Assets/Scripts/TowerMaze/Runtime/ProceduralOrchestrator.cs`

- [ ] **Step 1: Create the class:**

```csharp
// Assets/Scripts/TowerMaze/Runtime/ProceduralOrchestrator.cs
using UnityEngine;

namespace TowerMaze
{
    /// <summary>
    /// Director layer. Sits between TowerGenerator and MazeGenerator via ISegmentProvider.
    /// Decides whether each segment request returns a generated maze, a scripted pattern,
    /// or a risk/reward segment.
    /// </summary>
    public class ProceduralOrchestrator : ISegmentProvider
    {
        private readonly GameConfig _config;
        private readonly DifficultyProfile _difficultyProfile;
        private readonly ThemeDefinition _theme;
        private readonly MazeGenerator _mazeGenerator;
        private readonly PatternLibrary _patternLibrary; // nullable — no patterns if null
        private readonly ScoreManager _scoreManager;

        private System.Random _rng;
        private int _runSeed;
        private int _segmentsSinceLastPattern;
        private int _nextPatternInterval;
        private bool _lastWasPattern;

        public ProceduralOrchestrator(
            GameConfig config,
            DifficultyProfile difficultyProfile,
            ThemeDefinition theme,
            MazeGenerator mazeGenerator,
            PatternLibrary patternLibrary,
            ScoreManager scoreManager)
        {
            _config          = config;
            _difficultyProfile = difficultyProfile;
            _theme           = theme;
            _mazeGenerator   = mazeGenerator;
            _patternLibrary  = patternLibrary;
            _scoreManager    = scoreManager;
        }

        public void ResetRun(int seed)
        {
            _runSeed = seed;
            _rng = new System.Random(seed ^ 0xFACE);
            _segmentsSinceLastPattern = 0;
            _nextPatternInterval = NextInterval();
            _lastWasPattern = false;
        }

        // ISegmentProvider
        public SegmentData GetSegment(int segmentIndex, int entryColumn)
        {
            float heightM = _scoreManager?.CurrentHeightMetres ?? 0f;
            _segmentsSinceLastPattern++;

            bool shouldInject =
                !_lastWasPattern &&
                _patternLibrary != null &&
                _segmentsSinceLastPattern >= _nextPatternInterval;

            if (shouldInject)
            {
                var pattern = _patternLibrary.PickEligible(heightM, _rng);
                if (pattern != null)
                {
                    _segmentsSinceLastPattern = 0;
                    _nextPatternInterval = NextInterval();
                    _lastWasPattern = true;

                    // Return a copy with updated index/entry so TowerGenerator
                    // can align it to the tower correctly
                    var data = CloneWithEntry(pattern.segmentData, segmentIndex, entryColumn);
                    return data;
                }
            }

            _lastWasPattern = false;

            // Check for risk/reward injection (every 8-10 segments, regardless of pattern)
            bool isRiskReward =
                _patternLibrary != null &&
                heightM >= 15f &&
                _rng.NextDouble() < 0.15; // 15% chance when not injecting a pattern

            if (isRiskReward)
            {
                return RiskRewardBuilder.Build(_config, segmentIndex, entryColumn, _rng.Next());
            }

            // Default: normal procedural generation
            // segmentsPerZone is the confirmed field name in GameConfig (line 95 of ConfigData.cs)
            int zoneIndex = segmentIndex / Mathf.Max(1, _config.segmentsPerZone);
            // Use the same seed formula as the original TowerGenerator: runSeed + segmentIndex
            return _mazeGenerator.Generate(
                _config, _difficultyProfile, _theme,
                segmentIndex, zoneIndex, entryColumn,
                _runSeed + segmentIndex);
        }

        private int NextInterval() =>
            _rng.Next(_config.patternInjectionIntervalMin, _config.patternInjectionIntervalMax + 1);

        private static SegmentData CloneWithEntry(SegmentData source, int segmentIndex, int entryColumn)
        {
            if (source == null) return null;
            var clone = new SegmentData();
            clone.Initialize(source.width, source.height);
            clone.segmentIndex  = segmentIndex;
            clone.difficultyTier = source.difficultyTier;
            clone.coinRows    = source.coinRows;
            clone.coinColumns = source.coinColumns;

            // Mirror horizontally if entryColumn doesn't match source entryColumn
            int offset = entryColumn - source.entryColumn;
            for (int row = 0; row < source.height; row++)
                for (int col = 0; col < source.width; col++)
                    clone.SetCell(row, clone.WrapColumn(col + offset), source.GetCell(row, col));

            clone.entryColumn = entryColumn;
            clone.exitColumn  = clone.WrapColumn(source.exitColumn + offset);
            return clone;
        }
    }
}
```

**Note:** `MazeGenerator` is a stateless class — all inputs are passed to `Generate()` as parameters with no internal mutable state. Create `new MazeGenerator()` directly; no shared instance is needed.

- [ ] **Step 2: Open Unity, check Console — zero errors**

- [ ] **Step 3: Commit**
```bash
git add Assets/Scripts/TowerMaze/Runtime/ProceduralOrchestrator.cs
git commit -m "feat: implement ProceduralOrchestrator as ISegmentProvider"
```

---

### Task 10: Create CoinPickup MonoBehaviour

**Files:**
- Create: `Assets/Scripts/TowerMaze/Runtime/CoinPickup.cs`

- [ ] **Step 1: Create CoinPickup:**

```csharp
// Assets/Scripts/TowerMaze/Runtime/CoinPickup.cs
using UnityEngine;

namespace TowerMaze
{
    [RequireComponent(typeof(Collider))]
    public class CoinPickup : MonoBehaviour
    {
        private EconomyManager _economy;
        private int _emberValue = 5;
        private System.Action<CoinPickup> _returnToPool;
        private bool _collected;

        public void Initialize(EconomyManager economy, int emberValue, System.Action<CoinPickup> returnToPool)
        {
            _economy      = economy;
            _emberValue   = emberValue;
            _returnToPool = returnToPool;
            _collected    = false;
            gameObject.SetActive(true);
        }

        public void Deactivate()
        {
            _collected = true;
            gameObject.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_collected) return;
            if (!other.CompareTag("Player")) return;
            _collected = true;
            _economy?.GrantEmber(_emberValue);
            _returnToPool?.Invoke(this);
        }
    }
}
```

**Note:** `EconomyManager.GrantEmber(int)` — confirmed method name from `RunSystems.cs` line 560. The player collider must be tagged "Player" — verify this tag in HeroVisualController's sphere setup before testing.

- [ ] **Step 2: Open Unity — zero compile errors**

- [ ] **Step 3: Commit**
```bash
git add Assets/Scripts/TowerMaze/Runtime/CoinPickup.cs
git commit -m "feat: add CoinPickup MonoBehaviour"
```

---

### Task 11: Create PatternEditorWindow

**Files:**
- Create: `Assets/Scripts/TowerMaze/Editor/PatternEditorWindow.cs`

- [ ] **Step 1: Create the Editor folder if it doesn't exist:**
```bash
mkdir -p "Assets/Scripts/TowerMaze/Editor"
```

- [ ] **Step 2: Create the window:**

```csharp
// Assets/Scripts/TowerMaze/Editor/PatternEditorWindow.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TowerMaze;

public class PatternEditorWindow : EditorWindow
{
    private PatternDefinition _target;
    private Vector2 _scroll;
    private int _paintKind = 1; // 0=Wall, 1=Path, 2=MainPath
    private static readonly Color[] KindColors =
        { Color.black, new Color(0.8f, 0.8f, 0.8f), new Color(1f, 0.85f, 0.3f) };
    private static readonly string[] KindLabels = { "Wall", "Path", "Main" };

    [MenuItem("TowerMaze/Pattern Editor")]
    public static void Open() => GetWindow<PatternEditorWindow>("Pattern Editor");

    private void OnGUI()
    {
        _target = (PatternDefinition)EditorGUILayout.ObjectField("Pattern", _target,
            typeof(PatternDefinition), false);
        if (_target == null) { EditorGUILayout.HelpBox("Select a PatternDefinition asset.", MessageType.Info); return; }

        var data = _target.segmentData;
        if (data == null) { EditorGUILayout.HelpBox("segmentData is null.", MessageType.Warning); return; }

        _paintKind = GUILayout.Toolbar(_paintKind, KindLabels);
        EditorGUILayout.Space(4);

        float cellSize = 18f;
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        for (int row = data.height - 1; row >= 0; row--)
        {
            GUILayout.BeginHorizontal();
            for (int col = 0; col < data.width; col++)
            {
                var kind = data.GetCell(row, col);
                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = KindColors[(int)kind];
                if (GUILayout.Button("", GUILayout.Width(cellSize), GUILayout.Height(cellSize)))
                {
                    Undo.RecordObject(_target, "Paint Cell");
                    data.SetCell(row, col, (MazeCellKind)_paintKind);
                    EditorUtility.SetDirty(_target);
                }
                GUI.backgroundColor = oldColor;
            }
            GUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Fill All Wall")) FillAll(MazeCellKind.Wall, data);
        if (GUILayout.Button("Fill All Path")) FillAll(MazeCellKind.Path, data);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField($"Entry: {data.entryColumn}  Exit: {data.exitColumn}");
        if (GUILayout.Button("Save")) AssetDatabase.SaveAssets();
    }

    private void FillAll(MazeCellKind kind, SegmentData data)
    {
        Undo.RecordObject(_target, "Fill All");
        for (int r = 0; r < data.height; r++)
            for (int c = 0; c < data.width; c++)
                data.SetCell(r, c, kind);
        EditorUtility.SetDirty(_target);
    }
}
#endif
```

- [ ] **Step 3: Open Unity → TowerMaze menu → Pattern Editor — window should open without errors**

- [ ] **Step 4: Commit**
```bash
git add Assets/Scripts/TowerMaze/Editor/PatternEditorWindow.cs
git commit -m "feat: add PatternEditorWindow for authoring pattern grids"
```

---

### Task 12: Wire ProceduralOrchestrator into Bootstrapper + ResetRun

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/TowerMazeBootstrapper.cs`

- [ ] **Step 1: Open `TowerMazeBootstrapper.cs`. After `towerGenerator.Initialize(...)`, add:**

`MazeGenerator` is stateless — `new MazeGenerator()` is the correct approach (no shared instance needed).

```csharp
// After towerGenerator.Initialize(...):
var patternLibrary = Resources.Load<PatternLibrary>("PatternLibrary"); // null is fine — patterns disabled if missing
var orchestrator = new ProceduralOrchestrator(
    gameConfig, difficultyProfile, themeDefinition,
    new MazeGenerator(),
    patternLibrary,
    scoreManager,
    economyManager);  // economyManager for CoinPool
towerGenerator.SegmentProvider = orchestrator;
_orchestrator = orchestrator;
```

Add field to Bootstrapper: `private ProceduralOrchestrator _orchestrator;`

- [ ] **Step 2: Find where `towerGenerator.ResetRun(seed)` is called. Add `_orchestrator.ResetRun(seed)` immediately after:**

```csharp
towerGenerator.ResetRun(seed);
_orchestrator.ResetRun(seed);
```

- [ ] **Step 3: Open Unity, enter Play mode — tower generates normally (no regression)**

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/TowerMazeBootstrapper.cs
git commit -m "feat: wire ProceduralOrchestrator and CoinPool into bootstrapper"
```

---

## Chunk 3: NearMissSystem + ComboSystem

### Task 13: Create NearMissSystem

**Files:**
- Create: `Assets/Scripts/TowerMaze/Runtime/NearMissSystem.cs`

- [ ] **Step 1: Create the class:**

```csharp
// Assets/Scripts/TowerMaze/Runtime/NearMissSystem.cs
using UnityEngine;

namespace TowerMaze
{
    public class NearMissSystem : MonoBehaviour
    {
        private PlayerController _player;
        private CameraFollowController _camera;
        private AudioManager _audio;

        private const float ProximityThreshold = 0.15f;
        private const float CooldownSeconds    = 1.5f;
        private const float SlowTimeScale      = 0.35f;
        private const float SlowDuration       = 0.12f;
        private const float RecoverDuration    = 0.08f;
        private const float ShakeDuration      = 0.15f;
        private const float ShakeMagnitude     = 0.08f;

        private float _cooldownRemaining;
        private float _slowTimer;    // >0 while in slow phase
        private float _recoverTimer; // >0 while recovering
        private bool  _inSlow;

        public void Initialize(PlayerController player, CameraFollowController camera, AudioManager audio)
        {
            _player = player;
            _camera = camera;
            _audio  = audio;
        }

        private void Update()
        {
            if (_cooldownRemaining > 0f)
                _cooldownRemaining -= Time.unscaledDeltaTime;

            HandleSlowMotion();
        }

        private void LateUpdate()
        {
            // Detection runs in LateUpdate so PlayerController.Tick() has already run this frame.
            // LastWallProximity: 1.0 = right against a wall, 0.0 = open space.
            // Near-miss fires when player is very close (>0.85) but was NOT blocked this tick.
            if (_player == null || _cooldownRemaining > 0f || _inSlow) return;
            if (_player.LastWallProximity < 0.85f) return;

            TriggerNearMiss();
        }

        private void TriggerNearMiss()
        {
            _cooldownRemaining = CooldownSeconds;
            _slowTimer         = SlowDuration;
            _inSlow            = true;
            Time.timeScale     = SlowTimeScale;
            Time.fixedDeltaTime = 0.02f * SlowTimeScale;

            _camera?.Shake(ShakeDuration, ShakeMagnitude);
            _audio?.PlayOneShot("near_miss");
        }

        private void HandleSlowMotion()
        {
            if (_inSlow)
            {
                _slowTimer -= Time.unscaledDeltaTime;
                if (_slowTimer <= 0f)
                {
                    _inSlow       = false;
                    _recoverTimer = RecoverDuration;
                }
            }

            if (_recoverTimer > 0f)
            {
                _recoverTimer -= Time.unscaledDeltaTime;
                float t = 1f - (_recoverTimer / RecoverDuration);
                Time.timeScale = Mathf.Lerp(SlowTimeScale, 1f, t);
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
                if (_recoverTimer <= 0f)
                {
                    Time.timeScale      = 1f;
                    Time.fixedDeltaTime = 0.02f;
                }
            }
        }

        private void OnDestroy()
        {
            // Guarantee timeScale is restored if this object is destroyed mid-effect
            Time.timeScale      = 1f;
            Time.fixedDeltaTime = 0.02f;
        }
    }
}
```

**Note:** `AudioManager.PlayOneShot(string)` — verify the correct method name and signature on the existing `AudioManager`. If it uses a different API (e.g., `Play("near_miss")` or an enum), adjust accordingly. Add a "near_miss" audio clip to the project if it doesn't exist (a short ~0.1s clip).

**Note:** `CameraFollowController.Shake(float duration, float magnitude)` — signature confirmed from spec review.

- [ ] **Step 2: Open Unity — zero compile errors**

- [ ] **Step 3: Commit**
```bash
git add Assets/Scripts/TowerMaze/Runtime/NearMissSystem.cs
git commit -m "feat: implement NearMissSystem with slow-mo and camera shake"
```

---

### Task 14: Create ComboSystem

**Files:**
- Create: `Assets/Scripts/TowerMaze/Runtime/ComboSystem.cs`

- [ ] **Step 1: Create the class:**

```csharp
// Assets/Scripts/TowerMaze/Runtime/ComboSystem.cs
using UnityEngine;
using TMPro;

namespace TowerMaze
{
    public class ComboSystem : MonoBehaviour
    {
        private PlayerController _player;
        private HeroVisualController _heroVisual;

        private int   _streak;
        private float _labelFadeTimer;
        private const float LabelLifetime = 2f;

        // World-space TextMeshPro label (created dynamically)
        private TextMeshPro _label;
        private float _labelAlpha;

        // Streak thresholds
        private const int ThresholdSmooth      = 5;
        private const int ThresholdOnFire      = 10;
        private const int ThresholdUnstoppable = 20;

        public void Initialize(PlayerController player, HeroVisualController heroVisual)
        {
            _player     = player;
            _heroVisual = heroVisual;

            _player.OnMovementResult += HandleMovementResult;

            // Create floating label
            var labelGO = new GameObject("ComboLabel");
            labelGO.transform.SetParent(transform);
            _label = labelGO.AddComponent<TextMeshPro>();
            _label.fontSize = 3f;
            _label.alignment = TextAlignmentOptions.Center;
            _label.gameObject.SetActive(false);
        }

        private void HandleMovementResult(bool clean)
        {
            if (clean)
            {
                _streak++;
                CheckThresholds();
            }
            else
            {
                _streak = 0;
                ApplyStreakVisuals(0);
            }
        }

        private void CheckThresholds()
        {
            if (_streak == ThresholdSmooth || _streak == ThresholdOnFire || _streak == ThresholdUnstoppable ||
                (_streak > ThresholdUnstoppable && _streak % 5 == 0))
            {
                ShowLabel();
            }
            ApplyStreakVisuals(_streak);
        }

        private void ApplyStreakVisuals(int streak)
        {
            if (_heroVisual == null) return;

            if (streak >= ThresholdUnstoppable)
                _heroVisual.SetComboLevel(2);
            else if (streak >= ThresholdOnFire)
                _heroVisual.SetComboLevel(1);
            else if (streak >= ThresholdSmooth)
                _heroVisual.SetComboLevel(0);
            else
                _heroVisual.SetComboLevel(-1);
        }

        private void ShowLabel()
        {
            _label.gameObject.SetActive(true);
            _label.text  = $"x{_streak} \u2736";
            _labelFadeTimer = LabelLifetime;
            _labelAlpha     = 1f;
            Color c = _label.color;
            c.a = 1f;
            _label.color = c;
        }

        private void LateUpdate()
        {
            if (_player != null)
            {
                // Position label above player in world space
                _label.transform.position = _player.transform.position + Vector3.up * 0.5f;
            }

            if (_labelFadeTimer > 0f)
            {
                _labelFadeTimer -= Time.deltaTime;
                float fade = Mathf.Clamp01(_labelFadeTimer / LabelLifetime);
                Color c = _label.color;
                c.a = fade;
                _label.color = c;
                if (_labelFadeTimer <= 0f)
                    _label.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_player != null)
                _player.OnMovementResult -= HandleMovementResult;
        }
    }
}
```

- [ ] **Step 2: Open `HeroVisualController.cs`. Find the actual field names for the trail renderer, point light, and the existing VFX particle system. Then add `SetComboLevel` and helpers:**

Add field near other fields:
```csharp
private int _comboLevel = -1;
private float _baseLightIntensity;
```

Cache baseline in `Initialize()` (after existing init code):
```csharp
if (_pointLight != null) _baseLightIntensity = _pointLight.intensity;
```

Add methods:
```csharp
public void SetComboLevel(int level)
{
    if (level == _comboLevel) return;
    _comboLevel = level;
    switch (level)
    {
        case -1:
            if (_trailRenderer != null) SetTrailAlpha(0.4f);
            if (_pointLight    != null) _pointLight.intensity = _baseLightIntensity;
            break;
        case 0:
            if (_trailRenderer != null) SetTrailAlpha(0.7f);
            break;
        case 1:
            if (_trailRenderer != null) SetTrailAlpha(1f);
            if (_pointLight    != null) _pointLight.intensity = _baseLightIntensity * 1.5f;
            break;
        case 2:
            if (_trailRenderer != null) SetTrailAlpha(1f);
            if (_pointLight    != null) _pointLight.intensity = _baseLightIntensity * 2f;
            // Burst: re-use the existing skin VFX particle system.
            // Find the field that holds the hero's particle system (likely _vfxParticles or similar).
            // Call .Emit(15) or .Play() on it for a one-shot burst.
            _vfxParticles?.Emit(15);
            break;
    }
}

private void SetTrailAlpha(float alpha)
{
    var g = _trailRenderer.colorGradient;
    var alphaKeys = new GradientAlphaKey[]
    {
        new GradientAlphaKey(alpha, 0f),
        new GradientAlphaKey(0f, 1f)
    };
    g.SetKeys(g.colorKeys, alphaKeys);
    _trailRenderer.colorGradient = g;
}
```

**Field name note:** `_trailRenderer`, `_pointLight`, `_vfxParticles` — match to the actual private field names in `HeroVisualController`. The exploration summary confirmed a `TrailRenderer`, `PointLight`, and a particle system exist on that class. If the VFX particle system field name differs, find it by searching for `ParticleSystem` field declarations in the file.

- [ ] **Step 4: Open Unity — zero compile errors**

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/ComboSystem.cs Assets/Scripts/TowerMaze/Runtime/HeroVisualController.cs
git commit -m "feat: implement ComboSystem and add SetComboLevel to HeroVisualController"
```

---

### Task 15: Wire NearMissSystem and ComboSystem in Bootstrapper

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/TowerMazeBootstrapper.cs`

- [ ] **Step 1: Add initialization after existing manager setup:**

```csharp
// After playerController.Initialize(...):

// cameraFollow is declared on line 78 of TowerMazeBootstrapper as a local variable:
//   CameraFollowController cameraFollow = EnsureComponent<CameraFollowController>(mainCamera.transform);
// If wiring happens in a different scope, use FindObjectOfType<CameraFollowController>() as fallback.

// NearMissSystem
var nearMissGO = new GameObject("NearMissSystem");
nearMissGO.transform.SetParent(transform);
var nearMiss = nearMissGO.AddComponent<NearMissSystem>();
nearMiss.Initialize(playerController, cameraFollow, audioManager);

// ComboSystem — heroVisual is the HeroVisualController on the player ball.
// Find it via playerController.GetComponentInChildren<HeroVisualController>()
// or store it when it is created earlier in Bootstrapper Awake().
var heroVisual = playerController.GetComponentInChildren<HeroVisualController>();
var comboGO = new GameObject("ComboSystem");
comboGO.transform.SetParent(transform);
var combo = comboGO.AddComponent<ComboSystem>();
combo.Initialize(playerController, heroVisual);
```

- [ ] **Step 2: Enter Play mode in Unity. Play for ~30 seconds. Observe:**
  - Move very close to a wall (without dying) — expect brief slow-motion
  - Move cleanly for 5+ steps — expect combo label to appear above ball
  - Hit a wall — expect combo to reset

- [ ] **Step 3: Commit**
```bash
git add Assets/Scripts/TowerMaze/Runtime/TowerMazeBootstrapper.cs
git commit -m "feat: wire NearMissSystem and ComboSystem in bootstrapper"
```

---

## Chunk 4: Biome Progression

### Task 16: Create BiomeDefinition ScriptableObject

**Files:**
- Create: `Assets/Scripts/TowerMaze/Runtime/BiomeDefinition.cs`

- [ ] **Step 1: Create the class:**

```csharp
// Assets/Scripts/TowerMaze/Runtime/BiomeDefinition.cs
using UnityEngine;

namespace TowerMaze
{
    [CreateAssetMenu(menuName = "TowerMaze/Biome Definition", fileName = "NewBiome")]
    public class BiomeDefinition : ScriptableObject
    {
        public string biomeName = "Unnamed Biome";
        public float triggerHeight = 0f;          // metres
        public float transitionDuration = 8f;      // seconds

        [Header("Backdrop Gradient (4 stops: bottom to top)")]
        public Color gradientBottom  = new Color(0.2f, 0.05f, 0.0f);
        public Color gradientLow     = new Color(0.4f, 0.1f, 0.0f);
        public Color gradientHigh    = new Color(0.6f, 0.15f, 0.0f);
        public Color gradientTop     = new Color(0.1f, 0.02f, 0.0f);

        [Header("Tower Material Tints")]
        public Color wallTint = new Color(0.15f, 0.05f, 0.02f);
        public Color pathTint = new Color(0.8f, 0.4f, 0.1f);

        [Header("Lighting")]
        public Color ambientLightColor = new Color(0.3f, 0.15f, 0.05f);

        [Header("Particles")]
        public Color particleColor = new Color(1f, 0.4f, 0.1f);
        [Range(0.1f, 3f)]
        public float particleRateMultiplier = 1f;
    }
}
```

- [ ] **Step 2: Create 3 biome assets in `Assets/Resources/Biomes/` (create folder first):**

Right-click `Assets/Resources/Biomes/` → Create → TowerMaze → Biome Definition for each:

**VolcanicBiome.asset** (triggerHeight = 0):
- gradientBottom: `(0.18, 0.04, 0.01)` — deep dark red
- gradientLow:    `(0.45, 0.10, 0.02)` — burnt orange
- gradientHigh:   `(0.60, 0.18, 0.04)` — bright orange
- gradientTop:    `(0.10, 0.02, 0.01)` — near-black red
- wallTint:       `(0.15, 0.05, 0.02)` — dark charcoal-red
- pathTint:       `(0.80, 0.40, 0.10)` — glowing amber
- ambientLight:   `(0.30, 0.12, 0.04)` — warm ember fill
- particleColor:  `(1.00, 0.40, 0.10)` — orange embers
- particleRate:   `1.0`

**AshfallBiome.asset** (triggerHeight = 50):
- gradientBottom: `(0.10, 0.07, 0.12)` — dark purple-grey
- gradientLow:    `(0.22, 0.16, 0.28)` — muted lavender
- gradientHigh:   `(0.35, 0.28, 0.40)` — ash purple
- gradientTop:    `(0.06, 0.04, 0.08)` — near-black purple
- wallTint:       `(0.18, 0.14, 0.22)` — grey-purple
- pathTint:       `(0.65, 0.55, 0.75)` — pale lavender glow
- ambientLight:   `(0.15, 0.10, 0.20)` — cool purple fill
- particleColor:  `(0.80, 0.70, 0.90)` — pale ash particles
- particleRate:   `1.5`

**VoidPeakBiome.asset** (triggerHeight = 100):
- gradientBottom: `(0.02, 0.02, 0.06)` — near-black
- gradientLow:    `(0.04, 0.06, 0.18)` — deep blue-black
- gradientHigh:   `(0.06, 0.15, 0.40)` — electric blue-dark
- gradientTop:    `(0.01, 0.01, 0.04)` — void black
- wallTint:       `(0.05, 0.05, 0.12)` — dark void
- pathTint:       `(0.20, 0.60, 1.00)` — electric blue
- ambientLight:   `(0.04, 0.08, 0.20)` — cold blue fill
- particleColor:  `(0.40, 0.80, 1.00)` — frost blue
- particleRate:   `2.0`

- [ ] **Step 3: Commit assets and script**
```bash
git add Assets/Scripts/TowerMaze/Runtime/BiomeDefinition.cs Assets/TowerMaze/Config/
git commit -m "feat: add BiomeDefinition ScriptableObject and 3 launch biome assets"
```

---

### Task 17: Extend EnvironmentBackdropController with TransitionToBiome

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/EnvironmentBackdropController.cs`

- [ ] **Step 1: Open `EnvironmentBackdropController.cs`. Add these fields after existing fields:**

```csharp
// Biome transition state
private BiomeDefinition _fromBiome;
private BiomeDefinition _toBiome;
private float _transitionT;
private bool  _isTransitioning;
private float _transitionDuration;

// Cache for material property block
private MaterialPropertyBlock _mpb;

// References needed for tinting — find the existing fields for backdrop material and tower materials
// These names may differ; check the file for actual field names:
// _backdropMaterial, _glowMaterial, _emberParticleSystem (or similar)
```

- [ ] **Step 2: Add the public method:**

```csharp
public void TransitionToBiome(BiomeDefinition target, float duration)
{
    if (target == null) return;
    _fromBiome          = _currentBiome; // store current as "from"
    _toBiome            = target;
    _transitionT        = 0f;
    _transitionDuration = duration;
    _isTransitioning    = true;
    _currentBiome       = target;
}

private BiomeDefinition _currentBiome;
```

- [ ] **Step 3: In the existing `Update()` method, add the transition tick at the end:**

```csharp
// At the end of existing Update():
if (_isTransitioning)
    TickBiomeTransition();
```

- [ ] **Step 4: Read `BuildBackdrop()` in `EnvironmentBackdropController.cs`. Identify:**
  - The `MeshRenderer` or `Material` field used for the backdrop quad (search for `backdropMaterial`, `_backdropRenderer`, or `MeshRenderer` assignments in `BuildBackdrop`)
  - The shader property names for gradient colors (search for `SetColor` or `_Color` in `BuildBackdrop`)
  - The `ParticleSystem` field for embers (search for `ParticleSystem` field declarations)
  - The `TowerMaterials` reference used in `EnvironmentBackdropController` (search for `TowerMaterials` or `towerMaterials`)

  Write down the actual field names before Step 5.

- [ ] **Step 5: Add `TickBiomeTransition` and `ApplyBackdropGradient` using the field names found in Step 4:**

```csharp
private void TickBiomeTransition()
{
    _transitionT += Time.deltaTime / _transitionDuration;
    float t = Mathf.Clamp01(_transitionT);

    if (_fromBiome != null && _toBiome != null)
    {
        // Always lerp ambient light (works even with panoramic skybox)
        RenderSettings.ambientLight = Color.Lerp(
            _fromBiome.ambientLightColor, _toBiome.ambientLightColor, t);

        if (built) // only when procedural backdrop is active (not panoramic skybox)
        {
            ApplyBackdropGradient(
                Color.Lerp(_fromBiome.gradientBottom, _toBiome.gradientBottom, t),
                Color.Lerp(_fromBiome.gradientLow,    _toBiome.gradientLow,    t),
                Color.Lerp(_fromBiome.gradientHigh,   _toBiome.gradientHigh,   t),
                Color.Lerp(_fromBiome.gradientTop,    _toBiome.gradientTop,    t));

            // Wall/path tint via TowerMaterials (MaterialPropertyBlock — zero GC)
            // Replace _towerMaterials with the actual field name found in Step 4
            if (_towerMaterials != null)
            {
                _towerMaterials.SetWallTint(Color.Lerp(_fromBiome.wallTint, _toBiome.wallTint, t));
                _towerMaterials.SetPathTint(Color.Lerp(_fromBiome.pathTint, _toBiome.pathTint, t));
            }

            // Ember particle color
            // Replace _emberParticles with the actual ParticleSystem field name
            if (_emberParticles != null)
            {
                var main = _emberParticles.main;
                main.startColor = Color.Lerp(_fromBiome.particleColor, _toBiome.particleColor, t);
                var emission = _emberParticles.emission;
                emission.rateOverTime = Mathf.Lerp(
                    _fromBiome.particleRateMultiplier * _baseEmberRate,
                    _toBiome.particleRateMultiplier   * _baseEmberRate, t);
            }
        }
    }

    if (t >= 1f) _isTransitioning = false;
}

private void ApplyBackdropGradient(Color bottom, Color low, Color high, Color top)
{
    // Use the actual renderer/material field name identified in Step 4.
    // If the backdrop uses a Texture2D baked gradient, use the pixel approach:
    //   _backdropTexture.SetPixels(new[]{ bottom, low, high, top });
    //   _backdropTexture.Apply();
    // If it uses per-material color properties, use MaterialPropertyBlock:
    if (_mpb == null) _mpb = new MaterialPropertyBlock();
    // Replace _backdropRenderer and property names with values from Step 4:
    // _backdropRenderer.GetPropertyBlock(_mpb);
    // _mpb.SetColor("_ColorBottom", bottom);
    // _mpb.SetColor("_ColorLow",    low);
    // _mpb.SetColor("_ColorHigh",   high);
    // _mpb.SetColor("_ColorTop",    top);
    // _backdropRenderer.SetPropertyBlock(_mpb);
}
```

**Note:** `TowerMaterials.SetWallTint` / `SetPathTint` — check whether these methods exist. If `TowerMaterials` exposes tint color properties directly, set them and call `RefreshMaterials()`. If not, add minimal setters. Cache `_baseEmberRate` in `Initialize()` from the particle system's `emission.rateOverTime.constant`.

**Note on `_currentBiome` field placement:** Move the `private BiomeDefinition _currentBiome;` declaration to the fields block at Step 1 (with the other new fields), not inline inside `TransitionToBiome`.

- [ ] **Step 5: Open Unity — zero errors. Enter Play mode — environment renders normally.**

- [ ] **Step 6: Commit**
```bash
git add Assets/Scripts/TowerMaze/Runtime/EnvironmentBackdropController.cs
git commit -m "feat: add TransitionToBiome with manual-timer lerp to EnvironmentBackdropController"
```

---

### Task 18: Create BiomeSystem

**Files:**
- Create: `Assets/Scripts/TowerMaze/Runtime/BiomeSystem.cs`

- [ ] **Step 1: Add `BiomeSystemLogicTests` class to `TowerMazeEditModeTests.cs`:**

```csharp
// Add to the existing test file:
public class BiomeSystemLogicTests
{
    // Pure logic test: given a list of biome definitions, find the active one by height
    [Test]
    public void FindActiveBiome_ReturnsHighestBelowHeight()
    {
        var b0 = UnityEngine.ScriptableObject.CreateInstance<BiomeDefinition>();
        b0.triggerHeight = 0f;
        var b1 = UnityEngine.ScriptableObject.CreateInstance<BiomeDefinition>();
        b1.triggerHeight = 50f;
        var b2 = UnityEngine.ScriptableObject.CreateInstance<BiomeDefinition>();
        b2.triggerHeight = 100f;

        var biomes = new[] { b0, b1, b2 };
        var active = BiomeSystem.FindActiveBiome(biomes, 75f);
        Assert.AreEqual(b1, active);

        UnityEngine.Object.DestroyImmediate(b0);
        UnityEngine.Object.DestroyImmediate(b1);
        UnityEngine.Object.DestroyImmediate(b2);
    }
}
```

- [ ] **Step 2: Run test — expect FAIL (BiomeSystem doesn't exist)**

- [ ] **Step 3: Create `BiomeSystem.cs`:**

```csharp
// Assets/Scripts/TowerMaze/Runtime/BiomeSystem.cs
using UnityEngine;

namespace TowerMaze
{
    public class BiomeSystem : MonoBehaviour
    {
        private ScoreManager _scoreManager;
        private EnvironmentBackdropController _backdrop;
        private BiomeDefinition[] _biomes; // sorted ascending by triggerHeight

        private BiomeDefinition _activeBiome;
        private bool[] _triggered; // one flag per biome — prevents re-trigger

        public void Initialize(
            ScoreManager scoreManager,
            EnvironmentBackdropController backdrop,
            BiomeDefinition[] biomes)
        {
            _scoreManager = scoreManager;
            _backdrop     = backdrop;

            // Sort by triggerHeight ascending
            _biomes   = (BiomeDefinition[])biomes.Clone();
            System.Array.Sort(_biomes, (a, b) => a.triggerHeight.CompareTo(b.triggerHeight));
            _triggered = new bool[_biomes.Length];

            // Activate biome 0 immediately (no transition)
            if (_biomes.Length > 0)
            {
                _activeBiome  = _biomes[0];
                _triggered[0] = true;
                _backdrop?.TransitionToBiome(_biomes[0], 0.1f);
            }
        }

        private void Update()
        {
            if (_scoreManager == null || _biomes == null) return;
            float heightM = _scoreManager.CurrentHeightMetres;

            for (int i = 1; i < _biomes.Length; i++)
            {
                if (!_triggered[i] && heightM >= _biomes[i].triggerHeight)
                {
                    _triggered[i] = true;
                    _activeBiome  = _biomes[i];
                    _backdrop?.TransitionToBiome(_biomes[i], _biomes[i].transitionDuration);
                    break; // one transition at a time
                }
            }
        }

        // Static utility for testing
        public static BiomeDefinition FindActiveBiome(BiomeDefinition[] biomes, float heightM)
        {
            BiomeDefinition result = null;
            foreach (var b in biomes)
                if (b != null && heightM >= b.triggerHeight)
                    result = b;
            return result;
        }
    }
}
```

- [ ] **Step 4: Run tests — expect PASS**

- [ ] **Step 5: Wire BiomeSystem in Bootstrapper — add to `TowerMazeBootstrapper.cs` after existing init:**

```csharp
// Load biome definitions from Resources (or assign via Inspector)
var biomes = Resources.LoadAll<BiomeDefinition>("Biomes");
if (biomes == null || biomes.Length == 0)
{
    // Fallback: load by name
    biomes = new BiomeDefinition[]
    {
        Resources.Load<BiomeDefinition>("VolcanicBiome"),
        Resources.Load<BiomeDefinition>("AshfallBiome"),
        Resources.Load<BiomeDefinition>("VoidPeakBiome"),
    };
}

var biomeGO = new GameObject("BiomeSystem");
biomeGO.transform.SetParent(transform);
var biomeSystem = biomeGO.AddComponent<BiomeSystem>();
biomeSystem.Initialize(scoreManager, environmentBackdrop, biomes);
```

**Note:** Place biome assets in `Assets/Resources/Biomes/` for `Resources.LoadAll` to find them. Move the 3 assets created in Task 16 there.

- [ ] **Step 6: Enter Play mode. Climb to 50m — sky/colors should gradually shift. Climb to 100m — second shift.**

- [ ] **Step 7: Commit**
```bash
git add Assets/Scripts/TowerMaze/Runtime/BiomeSystem.cs Assets/Tests/EditMode/RiskRewardBuilderTests.cs Assets/Scripts/TowerMaze/Runtime/TowerMazeBootstrapper.cs Assets/Resources/
git commit -m "feat: implement BiomeSystem with height-triggered biome transitions"
```

---

## Chunk 5: Rank System, Share, and UI

### Task 19: Implement RankSystem with tests

**Files:**
- Create: `Assets/Scripts/TowerMaze/Runtime/RankSystem.cs`
- Modify: `Assets/Tests/EditMode/TowerMazeEditModeTests.cs` (append `RankSystemTests` class)

- [ ] **Step 1: Append `RankSystemTests` class to `TowerMazeEditModeTests.cs`:**

```csharp
public class RankSystemTests
{
    [Test] public void GetTier_Stone_Below30()   => Assert.AreEqual(RankTier.Stone,   RankSystem.GetTier(0f));
    [Test] public void GetTier_Bronze_At30()     => Assert.AreEqual(RankTier.Bronze,  RankSystem.GetTier(30f));
    [Test] public void GetTier_Silver_At75()     => Assert.AreEqual(RankTier.Silver,  RankSystem.GetTier(75f));
    [Test] public void GetTier_Gold_At150()      => Assert.AreEqual(RankTier.Gold,    RankSystem.GetTier(150f));
    [Test] public void GetTier_Obsidian_At300()  => Assert.AreEqual(RankTier.Obsidian,RankSystem.GetTier(300f));
    [Test] public void GetTier_Gold_Below300()   => Assert.AreEqual(RankTier.Gold,    RankSystem.GetTier(299f));

    [Test]
    public void GetPercentile_BelowFirstBucket_Returns10()
    {
        var buckets = new float[] { 5f, 15f, 30f, 50f, 80f, 120f, 180f, 250f, 350f, 500f };
        Assert.AreEqual(10, RankSystem.GetPercentile(3f, buckets));
    }

    [Test]
    public void GetPercentile_AboveLastBucket_Returns100()
    {
        var buckets = new float[] { 5f, 15f, 30f, 50f, 80f, 120f, 180f, 250f, 350f, 500f };
        Assert.AreEqual(100, RankSystem.GetPercentile(600f, buckets));
    }

    [Test]
    public void GetPercentile_ExactBucket_ReturnsCorrectDecile()
    {
        var buckets = new float[] { 5f, 15f, 30f, 50f, 80f, 120f, 180f, 250f, 350f, 500f };
        // bucket[4] = 80f → player at p50
        Assert.AreEqual(50, RankSystem.GetPercentile(80f, buckets));
    }

    [Test]
    public void UpdateBuckets_Slot9_IsMaxScore()
    {
        // Use a local test-only adapter — avoids dependency on the real LeaderboardEntry type
        var entries = new System.Collections.Generic.List<TestLeaderboardEntry>
        {
            new TestLeaderboardEntry(10f), new TestLeaderboardEntry(20f),
            new TestLeaderboardEntry(30f), new TestLeaderboardEntry(40f),
            new TestLeaderboardEntry(50f), new TestLeaderboardEntry(60f),
            new TestLeaderboardEntry(70f), new TestLeaderboardEntry(80f),
            new TestLeaderboardEntry(90f), new TestLeaderboardEntry(100f),
        };
        var buckets = new float[10];
        RankSystem.UpdateBucketsFromLeaderboard(entries, buckets);
        Assert.AreEqual(100f, buckets[9]);
    }

    // Local adapter satisfying ILeaderboardEntry — no dependency on PlayFab types
    private class TestLeaderboardEntry : ILeaderboardEntry
    {
        private readonly float _scoreMetres;
        public TestLeaderboardEntry(float scoreMetres) => _scoreMetres = scoreMetres;
        public float ScoreMetres => _scoreMetres;
    }
}
```

- [ ] **Step 2: Run tests — expect FAIL (RankSystem doesn't exist)**

- [ ] **Step 3: Create `RankSystem.cs`:**

```csharp
// Assets/Scripts/TowerMaze/Runtime/RankSystem.cs
using System.Collections.Generic;

namespace TowerMaze
{
    public enum RankTier { Stone, Bronze, Silver, Gold, Obsidian }

    public static class RankSystem
    {
        // Tier thresholds (metres)
        private static readonly float[] TierThresholds = { 0f, 30f, 75f, 150f, 300f };

        public static RankTier GetTier(float heightMetres)
        {
            RankTier tier = RankTier.Stone;
            for (int i = 0; i < TierThresholds.Length; i++)
                if (heightMetres >= TierThresholds[i])
                    tier = (RankTier)i;
            return tier;
        }

        /// <summary>
        /// Binary search the bucket array. Returns 10, 20, ... 100.
        /// bucket[i] = height at p(i+1)*10.
        /// </summary>
        public static int GetPercentile(float heightMetres, float[] buckets)
        {
            if (buckets == null || buckets.Length == 0) return 0;
            if (heightMetres >= buckets[buckets.Length - 1]) return 100;

            int lo = 0, hi = buckets.Length - 1;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (buckets[mid] <= heightMetres) lo = mid + 1;
                else hi = mid;
            }
            // lo is the first bucket above heightMetres → player is below p(lo+1)*10
            return lo * 10; // p10, p20, etc.
        }

        /// <summary>
        /// Populate percentile buckets from a leaderboard entry list.
        /// slot i = score at floor((i+1) * N / 10) - 1, slot 9 = max score.
        /// Scores are expected in centimetres (as stored in PlayFab); convert to metres.
        /// </summary>
        public static void UpdateBucketsFromLeaderboard<T>(
            List<T> entries,
            float[] buckets) where T : ILeaderboardEntry
        {
            if (entries == null || entries.Count == 0 || buckets == null) return;
            int n = entries.Count;
            for (int i = 0; i < buckets.Length - 1; i++)
            {
                int idx = Mathf.FloorToInt((float)(i + 1) * n / buckets.Length) - 1;
                idx = UnityEngine.Mathf.Clamp(idx, 0, n - 1);
                buckets[i] = entries[idx].ScoreMetres;
            }
            // Slot 9 always = max (leaderboard top entry)
            buckets[buckets.Length - 1] = entries[entries.Count - 1].ScoreMetres;
        }
    }

    // Adapter interface so RankSystem doesn't depend on the concrete PlayFab type
    public interface ILeaderboardEntry { float ScoreMetres { get; } }
}
```

**Note:** `ILeaderboardEntry.ScoreMetres` — the existing leaderboard entry type stores score in centimetres. You will need to add a small adapter. In the test, make `LeaderboardEntry` implement `ILeaderboardEntry` by adding a property `float ScoreMetres => score / 100f;` or use a wrapper. Adjust to match the actual existing type.

- [ ] **Step 4: Run tests — expect PASS**

- [ ] **Step 5: Wire bucket updates in `CloudSystems.cs`:**

Find where the PlayFab leaderboard response is processed (after `GetLeaderboard` callback). Add:
```csharp
// After leaderboard entries are received:
RankSystem.UpdateBucketsFromLeaderboard(leaderboardEntries, gameConfig.percentileBuckets);
```

The existing leaderboard entry type may not implement `ILeaderboardEntry`. If needed, add a small adapter in `CloudSystems.cs`:
```csharp
private class LeaderboardEntryAdapter : ILeaderboardEntry
{
    private readonly float _scoreMetres;
    public LeaderboardEntryAdapter(float scoreMetres) => _scoreMetres = scoreMetres;
    public float ScoreMetres => _scoreMetres;
}
```
Pass `entries.Select(e => new LeaderboardEntryAdapter(e.StatValue / 100f)).ToList()` (adjust `StatValue` / divisor to match the actual PlayFab score field and unit).

- [ ] **Step 5a: Open Unity — zero compile errors in `CloudSystems.cs`**

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/RankSystem.cs Assets/Tests/EditMode/TowerMazeEditModeTests.cs Assets/Scripts/TowerMaze/Runtime/CloudSystems.cs
git commit -m "feat: implement RankSystem with tier and percentile calculation; wire bucket updates from leaderboard"
```

---

### Task 20: Implement ShareSystem

**Files:**
- Create: `Assets/Scripts/TowerMaze/Runtime/ShareSystem.cs`
- Create: `Assets/Plugins/iOS/NativeShare.mm`

- [ ] **Step 1: Create `NativeShare.mm`:**

```objc
// Assets/Plugins/iOS/NativeShare.mm
#import <UIKit/UIKit.h>

extern "C" {
    void ShowShareSheet(const char* textCStr)
    {
        NSString* text = [NSString stringWithUTF8String:textCStr];
        UIActivityViewController* vc =
            [[UIActivityViewController alloc] initWithActivityItems:@[text] applicationActivities:nil];
        UIViewController* root = [UIApplication sharedApplication].keyWindow.rootViewController;
        // iPad popover fix
        if (vc.popoverPresentationController)
        {
            vc.popoverPresentationController.sourceView = root.view;
            vc.popoverPresentationController.sourceRect =
                CGRectMake(root.view.bounds.size.width / 2,
                           root.view.bounds.size.height,
                           1, 1);
        }
        [root presentViewController:vc animated:YES completion:nil];
    }
}
```

- [ ] **Step 2: Create `ShareSystem.cs`:**

```csharp
// Assets/Scripts/TowerMaze/Runtime/ShareSystem.cs
using UnityEngine;

namespace TowerMaze
{
    public static class ShareSystem
    {
#if UNITY_IOS && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void ShowShareSheet(string text);
#endif

        public static void Share(string text)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            ShareAndroid(text);
#elif UNITY_IOS && !UNITY_EDITOR
            ShowShareSheet(text);
#else
            GUIUtility.systemCopyBuffer = text;
            Debug.Log($"[ShareSystem] Copied to clipboard: {text}");
#endif
        }

        private static void ShareAndroid(string text)
        {
            using var intent = new AndroidJavaObject("android.content.Intent");
            intent.Call<AndroidJavaObject>("setAction",
                new AndroidJavaClass("android.content.Intent").GetStatic<string>("ACTION_SEND"));
            intent.Call<AndroidJavaObject>("setType", "text/plain");
            intent.Call<AndroidJavaObject>("putExtra",
                new AndroidJavaClass("android.content.Intent").GetStatic<string>("EXTRA_TEXT"),
                text);

            var chooser = new AndroidJavaClass("android.content.Intent")
                .CallStatic<AndroidJavaObject>("createChooser", intent, "Share via");

            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            activity.Call("startActivity", chooser);
        }

        public static string BuildShareText(float heightMetres, RankTier tier)
        {
            return $"I climbed {heightMetres:F0}m in Tower Maze and reached {tier} rank! Can you beat me?";
        }
    }
}
```

- [ ] **Step 3: Open Unity — zero compile errors**

- [ ] **Step 4: Commit**
```bash
git add Assets/Scripts/TowerMaze/Runtime/ShareSystem.cs Assets/Plugins/iOS/NativeShare.mm
git commit -m "feat: implement ShareSystem for Android/iOS/Editor"
```

---

### Task 21: Extend post-run UI with rank badge, percentile bar, share button

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs`
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs`

- [ ] **Step 1: Add `IsNewBestThisRun` to `ScoreManager` in `RunSystems.cs`**

Open `RunSystems.cs`. Find `ScoreManager` class. Locate where the existing best-height tracking is updated at run end (search for `bestHeight`, `_bestHeight`, or `personalBest`). Add a property and set it there:

```csharp
// Add to ScoreManager fields:
private bool _isNewBestThisRun;

// Add to ScoreManager properties (alongside CurrentHeightMetres):
public bool IsNewBestThisRun => _isNewBestThisRun;

// In the method that commits/finalises the run score — set it:
// (find where _bestHeight is compared or updated, e.g. in CommitRun or OnRunEnd)
_isNewBestThisRun = CurrentHeightMetres > previousBest;

// Reset at run start (find where scores are reset at new run):
_isNewBestThisRun = false;
```

- [ ] **Step 2: Open Unity — zero compile errors**

- [ ] **Step 3: Commit `RunSystems.cs`**

```bash
git add Assets/Scripts/TowerMaze/Runtime/RunSystems.cs
git commit -m "feat: add IsNewBestThisRun property to ScoreManager"
```

- [ ] **Step 4: Open `UISystems.cs`. Find the run results screen setup — the method that shows the game-over / run-end panel. This is likely in `UIManager` class, probably a method like `ShowRunResults(...)` or `ShowGameOver(...)`. Identify the results panel `Transform` (search for `_resultsPanel`, `resultsPanel`, or the variable name assigned to the results screen root).**

- [ ] **Step 5: Add the following private helpers to `UIManager` (follow the existing pattern in that file for creating TMP labels and buttons):**

```csharp
// Add these private helpers to UIManager:

private TextMeshProUGUI GetOrCreateLabel(Transform parent, string name)
{
    var existing = parent.Find(name);
    if (existing != null) return existing.GetComponent<TextMeshProUGUI>();
    var go = new GameObject(name);
    go.transform.SetParent(parent, worldPositionStays: false);
    var tmp = go.AddComponent<TextMeshProUGUI>();
    tmp.fontSize = 24f;
    tmp.alignment = TextAlignmentOptions.Center;
    return tmp;
}

private UnityEngine.UI.Button GetOrCreateButton(Transform parent, string name, string label)
{
    var existing = parent.Find(name);
    if (existing != null) return existing.GetComponent<UnityEngine.UI.Button>();
    var go = new GameObject(name);
    go.transform.SetParent(parent, worldPositionStays: false);
    var img = go.AddComponent<UnityEngine.UI.Image>();
    img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
    var btn = go.AddComponent<UnityEngine.UI.Button>();
    var labelGO = new GameObject("Label");
    labelGO.transform.SetParent(go.transform, worldPositionStays: false);
    var tmp = labelGO.AddComponent<TextMeshProUGUI>();
    tmp.text = label;
    tmp.fontSize = 20f;
    tmp.alignment = TextAlignmentOptions.Center;
    return btn;
}

private void AnimatePercentileFill(Transform parent, float fillTarget)
{
    const string barName = "PercentileBar";
    var existing = parent.Find(barName);
    UnityEngine.UI.Image bar;
    if (existing != null)
    {
        bar = existing.GetComponent<UnityEngine.UI.Image>();
    }
    else
    {
        var go = new GameObject(barName);
        go.transform.SetParent(parent, worldPositionStays: false);
        bar = go.AddComponent<UnityEngine.UI.Image>();
        bar.type = UnityEngine.UI.Image.Type.Filled;
        bar.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        bar.fillAmount = 0f;
        bar.color = new Color(0.9f, 0.7f, 0.1f);
    }
    // Animate fill over 1 second using a coroutine
    StartCoroutine(AnimateFillCoroutine(bar, fillTarget, 1f));
}

private System.Collections.IEnumerator AnimateFillCoroutine(
    UnityEngine.UI.Image bar, float target, float duration)
{
    float elapsed = 0f;
    float start = bar.fillAmount;
    while (elapsed < duration)
    {
        elapsed += Time.unscaledDeltaTime;
        bar.fillAmount = Mathf.Lerp(start, target, elapsed / duration);
        yield return null;
    }
    bar.fillAmount = target;
}

private void SetScaleInAnimation(Transform t)
{
    t.localScale = Vector3.zero;
    StartCoroutine(ScaleInCoroutine(t));
}

private System.Collections.IEnumerator ScaleInCoroutine(Transform t)
{
    float elapsed = 0f;
    const float duration = 0.3f;
    while (elapsed < duration)
    {
        elapsed += Time.unscaledDeltaTime;
        float s = Mathf.SmoothStep(0f, 1f, elapsed / duration);
        t.localScale = Vector3.one * s;
        yield return null;
    }
    t.localScale = Vector3.one;
}

private static Color GetTierColor(RankTier tier) => tier switch
{
    RankTier.Stone    => new Color(0.62f, 0.62f, 0.62f),
    RankTier.Bronze   => new Color(0.80f, 0.50f, 0.20f),
    RankTier.Silver   => new Color(0.75f, 0.75f, 0.75f),
    RankTier.Gold     => new Color(1.00f, 0.84f, 0.00f),
    RankTier.Obsidian => new Color(0.10f, 0.10f, 0.10f),
    _                 => Color.white
};
```

- [ ] **Step 6: Add `ShowRankOnResults` call. Find the method in `UIManager` that shows run results (search for `heightText` or `bestHeight` assignment on the results panel). Immediately after the height text is set, add:**

```csharp
// Add call after height text is assigned in ShowRunResults / ShowGameOver:
ShowRankOnResults(_resultsPanel, scoreManager.CurrentHeightMetres,
    gameConfig.percentileBuckets, scoreManager.IsNewBestThisRun);
```

Then add the `ShowRankOnResults` method body:

```csharp
private void ShowRankOnResults(Transform resultsPanel, float heightMetres,
    float[] percentileBuckets, bool isPersonalBest)
{
    var tier = RankSystem.GetTier(heightMetres);

    var rankText = GetOrCreateLabel(resultsPanel, "RankLabel");
    rankText.text = $"<b>{tier}</b>";
    rankText.color = GetTierColor(tier);
    SetScaleInAnimation(rankText.transform);

    int percentile = RankSystem.GetPercentile(heightMetres, percentileBuckets);
    if (percentile > 0)
    {
        var percentileText = GetOrCreateLabel(resultsPanel, "PercentileLabel");
        percentileText.text = $"Better than {percentile}% of players";
        AnimatePercentileFill(resultsPanel, percentile / 100f);
    }

    if (isPersonalBest)
    {
        var shareBtn = GetOrCreateButton(resultsPanel, "ShareButton", "Share Score");
        shareBtn.onClick.RemoveAllListeners();
        shareBtn.onClick.AddListener(() =>
            ShareSystem.Share(ShareSystem.BuildShareText(heightMetres, tier)));
    }
}
```

**Note on rank badge sprites:** The spec describes a "sprite per tier" badge. For launch, the tier name text label with `GetTierColor` is a functional stand-in. When art assets are ready, replace `GetOrCreateLabel` for the badge with a `UnityEngine.UI.Image` component and assign the sprite: `image.sprite = Resources.Load<Sprite>($"Badges/{tier}Badge")`. Place badge sprites in `Assets/Resources/Badges/` and name them `StoneBadge`, `BronzeBadge`, etc.

- [ ] **Step 7: Open Unity — zero compile errors. Enter Play mode, finish a run — results screen shows tier label, percentile bar animates, share button visible on PB.**

- [ ] **Step 8: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/UISystems.cs
git commit -m "feat: add rank badge, percentile bar, and share button to run results UI"
```

---

### Task 22: Final integration test + cleanup

- [ ] **Step 1: Run all EditMode tests in Test Runner — all should PASS**

- [ ] **Step 2: Enter Play mode. Verify end-to-end:**
  - [ ] Tower generates correctly — patterns occasionally appear
  - [ ] Risk/reward segment appears with dual corridors
  - [ ] Near-miss triggers slow-motion (brush close to a wall)
  - [ ] Combo label appears after 5 clean moves, brightens at 10, bursts at 20
  - [ ] Colors shift gradually around 50m height
  - [ ] Colors shift again around 100m height
  - [ ] Die and see results screen: rank tier shown, percentile bar (may say 0% until leaderboard fills), share button appears on PB

- [ ] **Step 3: Build for Android (or iOS) and test on device. Verify:**
  - [ ] Share button triggers native share sheet
  - [ ] No GC allocation spikes during biome transitions (use Unity Profiler)
  - [ ] No frame drops during pattern injection

- [ ] **Step 4: Final commit**

```bash
git add \
  Assets/Scripts/TowerMaze/Runtime/ISegmentProvider.cs \
  Assets/Scripts/TowerMaze/Runtime/ProceduralOrchestrator.cs \
  Assets/Scripts/TowerMaze/Runtime/PatternDefinition.cs \
  Assets/Scripts/TowerMaze/Runtime/PatternLibrary.cs \
  Assets/Scripts/TowerMaze/Runtime/RiskRewardBuilder.cs \
  Assets/Scripts/TowerMaze/Runtime/CoinPickup.cs \
  Assets/Scripts/TowerMaze/Runtime/NearMissSystem.cs \
  Assets/Scripts/TowerMaze/Runtime/ComboSystem.cs \
  Assets/Scripts/TowerMaze/Runtime/BiomeDefinition.cs \
  Assets/Scripts/TowerMaze/Runtime/BiomeSystem.cs \
  Assets/Scripts/TowerMaze/Runtime/RankSystem.cs \
  Assets/Scripts/TowerMaze/Runtime/ShareSystem.cs \
  Assets/Scripts/TowerMaze/Editor/PatternEditorWindow.cs \
  Assets/Plugins/iOS/NativeShare.mm \
  Assets/Tests/EditMode/TowerMazeEditModeTests.cs \
  Assets/Tests/EditMode/TowerMazeTests.asmdef \
  Assets/Scripts/TowerMaze/Runtime/TowerSystems.cs \
  Assets/Scripts/TowerMaze/Runtime/PlayerController.cs \
  Assets/Scripts/TowerMaze/Runtime/RunSystems.cs \
  Assets/Scripts/TowerMaze/Runtime/EnvironmentBackdropController.cs \
  Assets/Scripts/TowerMaze/Runtime/ConfigData.cs \
  Assets/Scripts/TowerMaze/Runtime/UISystems.cs \
  Assets/Scripts/TowerMaze/Runtime/TowerMazeBootstrapper.cs \
  Assets/Scripts/TowerMaze/Runtime/CloudSystems.cs \
  Assets/Resources/
git commit -m "feat: complete TowerMaze procedural/viral extension — all 4 phases"
```

---

## Known Implementation Notes

1. **`TowerGenerator.SpawnSegment` exact implementation** — The code review identified that `SpawnSegment` calls `mazeGenerator.Generate(...)` or `CreateTutorialSegment(...)`. Read the actual lines before modifying to ensure the `SegmentProvider` null-check is placed correctly (after tutorial check, before normal generate).

2. **`HeroVisualController` field names** — The trail renderer, point light, and particle system fields may be named differently than assumed. Read `HeroVisualController.cs` before implementing `SetComboLevel`.

3. **`EnvironmentBackdropController.BuildBackdrop` internals** — The gradient is applied via procedural textures or material properties. Read `BuildBackdrop()` to determine whether `ApplyBackdropGradient` should set material properties or re-bake a `Texture2D`.

4. **`EconomyManager.AwardEmber` signature** — Verify method name/signature before using in `CoinPickup`.

5. **`LeaderboardEntry` type** — The existing type may not implement `ILeaderboardEntry`. Add `ScoreMetres` as an extension method or adapt `UpdateBucketsFromLeaderboard` to accept `Func<T, float>` score extractor.

6. **`AudioManager.PlayOneShot` for near_miss** — Verify the AudioManager's API. A "near_miss" audio clip needs to be added to `Assets/Audio/` and registered with AudioManager's clip list.
