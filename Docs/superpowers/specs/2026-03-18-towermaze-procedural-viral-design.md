# TowerMaze — Procedural, Difficulty & Viral Extension Design

**Date:** 2026-03-18
**Status:** Approved
**Scope:** Extend existing TowerMaze game with pattern library, near-miss/combo feedback, biome progression, and competitive viral layer.
**Approach:** ProceduralOrchestrator (Approach B) — new manager layer, existing systems untouched.
**Target:** Modern mid/high-end devices (iPhone 12+, flagship Android).

---

## 1. Context — What Already Exists

The following systems are already implemented and must NOT be replaced:

| System | Location | Notes |
|--------|----------|-------|
| Procedural maze gen | `MazeGenerator.cs` | DFS with weighted neighbors, 3 difficulty bands |
| Height-based difficulty | `DifficultyProfile.asset` | Bands: 0–20m, 20–60m, 60m+ |
| Tower segment streaming | `TowerSystems.cs → TowerGenerator` | Object pooling, dynamic difficulty updates |
| Pressure mechanics | `TowerSystems.cs` | Lava rise, rotation, rushes, control flip |
| Ember economy + skins | `MonetizationSystems.cs` | 7 skins, IAP, rewarded ads |
| Leaderboard | `CloudSystems.cs` | PlayFab sync |
| Daily missions/challenges | `RunSystems.cs → RunManager` | Various mission types |
| Visual theming | `EnvironmentBackdropController.cs` | Backdrop gradient, ember particles, floating ruins |
| Camera shake | `CameraFollowController.cs` | `Shake(float duration, float magnitude)` already exists |
| Audio | `AudioManager` (referenced across systems) | Wall bump, UI sounds |
| Run results UI | `UISystems.cs → UIManager` | Score, height, ember reward screen |

---

## 2. Architecture — ProceduralOrchestrator

A single new manager class intercepts every segment request from `TowerGenerator` and acts as a director.

```text
TowerGenerator
    └── ProceduralOrchestrator
            ├── MazeGenerator          (existing — normal generated segments)
            ├── PatternLibrary         (new — scripted pattern segments)
            └── RiskRewardBuilder      (new — branching coin/safe segments)
```

`ProceduralOrchestrator` is initialized in `TowerMazeBootstrapper` with one additional line. It receives references to `TowerGenerator`, `MazeGenerator`, `ScoreManager`, and `PlayerController` via an `Initialize()` method — consistent with the existing manager pattern used throughout the codebase (e.g. `TowerGenerator.Initialize()`, `PlayerController.Initialize()`).

**No changes to `MazeGenerator.cs`, `TowerGenerator.cs`, or `PlayerController.cs` internals.**

The only additions to existing files are:

- One `LastWallProximity` float property exposed on `PlayerController` (read-only, set internally)
- One `OnMovementResult(bool clean)` event on `PlayerController` (fires after each move)
- One orchestrator init line in `TowerMazeBootstrapper`
- One `TransitionToBiome()` method on `EnvironmentBackdropController`

---

## 3. Phase 1 — Pattern Library + Risk/Reward Paths

### 3.1 PatternDefinition ScriptableObject

```text
PatternDefinition
├── patternType: PatternType (Straight, ZigZag, Spiral, SplitPath, RiskReward)
├── segmentData: SegmentData (pre-authored maze cell grid)
├── minHeightToAppear: float
├── weight: float (selection probability)
└── description: string (editor label only)
```

**Pattern authoring workflow:**
The full 28×16 cell grid (448 cells) is not hand-editable in the Inspector. A small `PatternEditorWindow` (`Editor/PatternEditorWindow.cs`) will be built alongside Phase 1. It provides a 2D grid painter (Wall / Path / MainPath per cell) that serializes directly into `SegmentData`. Patterns are authored once in the Unity Editor and saved as assets — no runtime authoring required. For the 10 launch patterns, the `RiskReward` types are generated at runtime by `RiskRewardBuilder` (not authored in the editor) and do not require the tool.

**Minimum pattern set (10 patterns at launch):**

| Name | Type | Description | Min Height |
|------|------|-------------|------------|
| HighwayRun | Straight | Wide clear corridor, fast traversal | 0m |
| SharpZigZag | ZigZag | Forced left-right alternation | 10m |
| GentleSpiral | Spiral | Gradual wrap around tower | 30m |
| TightSpiral | Spiral | Fast wrap, narrower | 80m |
| ForkChoice | SplitPath | Two equal paths reconverge | 20m |
| DeadEndGauntlet | ZigZag | Multiple fake exits, one true path | 50m |
| RiskRewardEasy | RiskReward | Narrow coin path + wide safe path | 15m |
| RiskRewardHard | RiskReward | Very narrow coin path + coins worth 3x | 60m |
| OpenChamber | Straight | Wide open segment, breathing room | 0m |
| TightCorridor | Straight | Single-cell-wide path, high tension | 40m |

### 3.2 Injection Logic

- Every 4–6 segments (random, configurable in `GameConfig`), Orchestrator may inject a scripted pattern
- Eligible patterns filtered by `minHeightToAppear <= currentHeight`
- Selected by weighted random from eligible set
- No two scripted patterns back-to-back — always ≥1 generated segment between them
- Entry/exit column matching: Orchestrator reads previous segment's exit column and selects/rotates pattern to align

### 3.3 RiskRewardBuilder

Generates a `SegmentData` with two parallel vertical corridors:

- **Left corridor:** narrow (1 cell wide), contains 3–5 coin pickups
- **Right corridor:** wide (3 cells), no coins
- Both converge at exit

`CoinPickup` component on each coin: simple trigger collider, awards ember on collect, returns to pool on collection or when segment despawns. Coins rendered as small glowing spheres using the existing ember material.

---

## 4. Phase 2 — Near-Miss System + Combo Moments

### 4.1 NearMissSystem

**Detection:**
`PlayerController` exposes `LastWallProximity` (float, 0–1 normalized distance to nearest wall after each move). If `LastWallProximity < 0.15f` AND no collision occurred → near-miss.

**Response (simultaneous, 3 effects):**

| Effect | Implementation | Parameters |
|--------|---------------|------------|
| Slow-motion | `Time.timeScale = 0.35f` for 0.12s, lerp back over 0.08s | Handled in `NearMissSystem.Update()` |
| Camera shake | `CameraFollowController.Shake(duration: 0.15f, magnitude: 0.08f)` | Uses existing `Shake(float duration, float magnitude)` signature |
| Audio | `AudioManager.Play("near_miss")` | New short whoosh/heartbeat clip |

**Cooldown:** 1.5 seconds between triggers. Prevents spam in tight corridors.

### 4.2 ComboSystem

**Tracking:** Listens to `PlayerController.OnMovementResult(bool clean)` event.

**Streak thresholds:**

| Streak | Label | Visual Effect |
|--------|-------|---------------|
| 5 | Smooth | Trail pulse (opacity +30%) |
| 10 | On Fire | Trail brightens, point light intensity +50% |
| 20+ | Unstoppable | Trail + particle burst every 5 moves |

**UI:** World-space `TextMeshPro` floating above player ball. Fades in on threshold hit, displays `"x10 ✦"` style label, auto-fades after 2 seconds with no new milestone. No persistent HUD element.

**Break condition:** Wall hit or fall. Resets streak to 0, no penalty. No break animation needed.

**Event semantics:** `OnMovementResult(bool clean)` fires once per `PlayerController.Tick()` call. `clean = false` if any sub-step was blocked during that tick (i.e., `TriggerBlockedFeedback` would have fired). This means one break event per frame regardless of how many sub-steps failed.

---

## 5. Phase 3 — Biome Progression

### 5.1 BiomeDefinition ScriptableObject

```text
BiomeDefinition
├── biomeName: string
├── triggerHeight: float
├── transitionDuration: float (default 8s)
├── backdropGradientColors: Color[4]
├── wallTint: Color
├── pathTint: Color
├── ambientLightColor: Color
├── particleColor: Color
└── particleRateMultiplier: float
```

**3 launch biomes:**

| Biome | Height | Palette | Atmosphere |
|-------|--------|---------|------------|
| Volcanic | 0–50m | Deep reds/oranges | Warm ember, ash particles (existing) |
| Ashfall | 50–100m | Desaturated purples/greys | Cool rim light, denser ember, fog tint |
| Void Peak | 100m+ | Near-black + electric blue | Cold fill, flicker, frost particle layer |

The existing `VolcanicTheme.asset` is a `ThemeDefinition` ScriptableObject with a different field set (wall textures, hero colors, normal maps, etc.) — it is **not** directly convertible to `BiomeDefinition`. A new `VolcanicBiome.asset` (`BiomeDefinition`) must be manually authored, sourcing the gradient, wall tint, and particle color values from the corresponding visual properties of `VolcanicTheme.asset`. `VolcanicTheme.asset` itself is left untouched.

### 5.2 Transition Mechanism

`EnvironmentBackdropController.TransitionToBiome(BiomeDefinition target, float duration)`:

- Lerps backdrop gradient colors (4 stops) using `Mathf.Lerp` over `duration` seconds
- Lerps `TowerMaterials` wall/path tint via `MaterialPropertyBlock` — zero GC
- Lerps ambient light color via `RenderSettings.ambientLight`
- Lerps ember particle color + rate multiplier
- All lerps driven by a single `t` value in `Update()` via a manual timer (`_transitionT` float incremented each frame). A coroutine is explicitly avoided: if the GameObject is deactivated during a transition, a coroutine silently stops and leaves `RenderSettings.ambientLight` in a partially lerped state — a global Unity setting that persists across scene reloads. The manual timer in `Update()` is immune to this risk.

**BiomeSystem** polls `ScoreManager.CurrentHeightMetres` directly in its own `Update()` loop — this is the player's altitude in metres, distinct from the point-based `CurrentScore`. `ScoreManager` has no per-frame height event (only a `StateChanged` event that fires at run commit), so polling is appropriate: one float read per frame, one boundary check, no GC. `BiomeDefinition.triggerHeight` is in metres and compared directly against `CurrentHeightMetres`. Triggers `TransitionToBiome()` once per biome boundary crossing; a boolean flag per biome prevents re-triggering. If `ScoreManager` does not yet expose `CurrentHeightMetres` as a public property, it must be added to the modified-files list (see Section 7).

**Panoramic skybox guard:** `EnvironmentBackdropController.TransitionToBiome()` must first check whether the procedural backdrop was built (i.e., `built == true` from the procedural path vs. the `TryApplyDownloadedSkybox()` early-exit path). If a panoramic skybox is active, material lerps are skipped; only `RenderSettings.ambientLight` and fog color are transitioned.

---

## 6. Phase 4 — Competitive Layer (Rank, Percentile, Share)

### 6.1 RankSystem

**Tier table:**

| Tier | Min Height | Badge Color |
|------|------------|-------------|
| Stone | 0m | Grey (#9E9E9E) |
| Bronze | 30m | Bronze (#CD7F32) |
| Silver | 75m | Silver (#C0C0C0) |
| Gold | 150m | Gold (#FFD700) |
| Obsidian | 300m | #1A1A1A + ember glow |

**Percentile calculation:**

- `GameConfig` stores a `float[] percentileBuckets` (10 entries representing heights at p10, p20, …, p100), with hardcoded defaults: `[5, 15, 30, 50, 80, 120, 180, 250, 350, 500]`
- At run end, `CloudSystems` already fetches the top-N PlayFab leaderboard entries. `RankSystem.UpdateBucketsFromLeaderboard(List<LeaderboardEntry> entries)` maps the returned scores linearly across the 10 bucket slots: slot `i` = score at index `floor((i + 1) * entries.Count / 10) - 1`, with slot 9 always set to `entries[entries.Count - 1].score` (the leaderboard maximum) as an explicit special case. This ensures the top decile is accurately bounded. This is a best-effort approximation using available data; bucket accuracy improves as the leaderboard grows.
- `RankSystem.GetPercentile(float height)` binary-searches the bucket array and returns a 0–100 int (e.g., height below bucket[0] = bottom 10%, above bucket[9] = top 100%)
- Fallback: if `percentileBuckets` is null/empty, show tier only (no percentile bar)

### 6.2 Post-Run Screen Extensions

Three new elements added to the existing `UIManager` run results layout:

1. **Rank badge** — sprite (per tier) + tier name label, animates scale-in on show
2. **Percentile bar** — horizontal fill bar (0–100%), animates fill over 1s, label: `"Better than 78% of players"`
3. **Share button** — visible only on personal best (`ScoreManager.IsNewBestThisRun`)

### 6.3 ShareSystem

**Trigger:** Personal best only.

**Share text:** `"I climbed {height}m in Tower Maze and reached {tier} rank! Can you beat me?"`

**Platform implementation:**

| Platform | Method |
|----------|--------|
| Android | `AndroidJavaClass` share intent (standard Unity pattern, no extra SDK) |
| iOS | Native share sheet via a `NativeShare.mm` UnitySendMessage plugin. The plugin does not pre-exist in the project and must be created at `Assets/Plugins/iOS/NativeShare.mm`. It exposes one C function: `void ShowShareSheet(const char* text)` — calls `UIActivityViewController` and presents it on the root view controller. `ShareSystem` invokes it via `[DllImport("__Internal")] static extern void ShowShareSheet(string text)`. |
| Editor/fallback | `GUIUtility.systemCopyBuffer` (copies to clipboard) |

`ShareSystem` is a static utility class with a single `Share(string text)` method — no MonoBehaviour, no persistent state.

---

## 7. New Files Summary

| File | Type | Purpose |
|------|------|---------|
| `ProceduralOrchestrator.cs` | Plain C# Manager | Director layer above TowerGenerator, initialized via `Initialize()` |
| `PatternLibrary.cs` | ScriptableObject | Holds list of PatternDefinition assets |
| `PatternDefinition.cs` | ScriptableObject | Single pattern asset (type, grid, weight) |
| `RiskRewardBuilder.cs` | Static utility | Generates risk/reward SegmentData at runtime |
| `CoinPickup.cs` | MonoBehaviour | Coin trigger, ember award, pool return |
| `NearMissSystem.cs` | MonoBehaviour | Detects near-miss, triggers slow-mo + shake |
| `ComboSystem.cs` | MonoBehaviour | Tracks streak, drives trail/UI feedback |
| `BiomeDefinition.cs` | ScriptableObject | Per-biome visual parameters |
| `BiomeSystem.cs` | MonoBehaviour | Height monitor, triggers biome transitions |
| `RankSystem.cs` | Static utility | Height → tier + percentile calculation |
| `ShareSystem.cs` | Static utility | Native OS share sheet |
| `PatternEditorWindow.cs` | Editor EditorWindow | 2D grid painter for authoring PatternDefinition assets |
| `NativeShare.mm` | iOS native plugin | `UIActivityViewController` wrapper, exposes `ShowShareSheet(const char*)` |

**Modified files (minimal changes):**

| File | Change |
|------|--------|
| `PlayerController.cs` | Add `LastWallProximity` float + `OnMovementResult` event |
| `RunSystems.cs` (`ScoreManager`) | Add `CurrentHeightMetres` public property (player altitude in metres) |
| `TowerMazeBootstrapper.cs` | Init `ProceduralOrchestrator`, `NearMissSystem`, `ComboSystem`, `BiomeSystem` |
| `EnvironmentBackdropController.cs` | Add `TransitionToBiome()` method |
| `UISystems.cs` | Add rank badge, percentile bar, share button to run results screen |
| `ConfigData.cs` | Add `percentileBuckets` to `GameConfig`; add biome list |
| `GameConfig.asset` | Wire new config fields |

---

## 8. Phased Implementation Order

| Phase | Systems | Dependency |
|-------|---------|------------|
| 1 | ProceduralOrchestrator + PatternLibrary + RiskRewardBuilder + CoinPickup | None |
| 2 | NearMissSystem + ComboSystem | Requires `PlayerController` events (added in Phase 1 prep) |
| 3 | BiomeDefinition + BiomeSystem + EnvironmentBackdropController extension | None |
| 4 | RankSystem + ShareSystem + UI extensions | Requires existing PlayFab leaderboard |

Each phase is independently shippable and testable.

---

## 9. Performance Constraints

- All lerps use `Mathf.Lerp` / `Color.Lerp` on cached values — no GC per frame
- `MaterialPropertyBlock` used for all material tint changes — no material instantiation
- Coin pickups use a `CoinPool` (pre-warmed, size 20) — no runtime `Instantiate` during gameplay
- `Time.timeScale` slow-mo: max 0.12s duration, 1.5s cooldown — no sustained frame budget impact
- Pattern injection happens at segment spawn time (already a pool swap, not a hot path)
- `percentileBuckets` lookup is a binary search on 10 floats — negligible cost
