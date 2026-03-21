# Normal Mode Isolation & High Stakes Rebalance Design

**Date:** 2026-03-16
**Status:** Approved

## Problem

1. The Normal "Start" run randomly assigns Slipstream or HighStakes modifiers, making it unpredictable and unfair.
2. HighStakes modifier has only a penalty (lava ×1.18) with no compensating benefit, making progression impossible.
3. Best score and leaderboard are updated by all run modes, including Daily Challenge — this pollutes Normal mode progression stats.
4. The "NEW BEST" display and `IsNewBestThisRun` flag fire during Daily Challenge runs when the player beats their Normal persisted best, which is misleading since the best is not actually committed.

## Goals

- Normal "Start" button always launches a clean run with no modifiers.
- Slipstream and HighStakes only appear in Daily Challenge runs.
- Best score and leaderboard are only updated by Normal mode runs.
- Daily Challenge runs are for fun and coin earning only — they do not affect persistent progression stats.
- HighStakes rebalanced to be a fair risk/reward modifier (for Daily use).
- "NEW BEST" display and `IsNewBestThisRun` are suppressed for non-Normal runs.

---

## Changes

**File:** `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs`

---

### Change 1: Normal mode — no modifiers

**Location:** `ConfigureRunModifiers()` (L3043), `else` branch at L3051–3056

```csharp
// Before (Normal mode randomly assigns a modifier)
else
{
    System.Random modifierRandom = new(runSeed ^ Environment.TickCount);
    primaryRunModifier = modifierRandom.Next(0, 2) == 0 ? RunModifierType.Slipstream : RunModifierType.HighStakes;
    secondaryRunModifier = RunModifierType.None;
}

// After (Normal mode has no modifiers)
else
{
    primaryRunModifier = RunModifierType.None;
    secondaryRunModifier = RunModifierType.None;
}
```

---

### Change 2: HighStakes rebalance

**Locations:**
- `ApplyModifier()` L3073–3075
- `GetHorizontalSpeedMultiplier()` L3079
- `GetClimbSpeedMultiplier()` L3090

**`ApplyModifier` — reduce sink penalty:**
```csharp
// Before
case RunModifierType.HighStakes:
    sinkModifierMultiplier *= 1.18f;

// After
case RunModifierType.HighStakes:
    sinkModifierMultiplier *= 1.08f;
```

**`GetHorizontalSpeedMultiplier` — add movement bonus:**
```csharp
// Add after the existing Slipstream check
if (primaryRunModifier == RunModifierType.HighStakes || secondaryRunModifier == RunModifierType.HighStakes)
    multiplier *= 1.10f;
```

**`GetClimbSpeedMultiplier` — add climb bonus:**
```csharp
// Add after the existing Slipstream check
if (primaryRunModifier == RunModifierType.HighStakes || secondaryRunModifier == RunModifierType.HighStakes)
    multiplier *= 1.10f;
```

Net effect:
| Parameter | Before | After |
|---|---|---|
| Lava speed | ×1.18 | ×1.08 |
| Horizontal speed | ×1.00 | ×1.10 |
| Climb speed | ×1.00 | ×1.10 |

When Slipstream + HighStakes are combined (Daily Challenge), multipliers stack (consistent with existing behavior).

---

### Change 3: Leaderboard — Normal mode only

There are two guard locations covering all paths to `CommitLeaderboardEntry()`:

**Location A — direct call in fail handler (L2592–2594):**
```csharp
// Before
if (!pendingLeaderboardCommit)
    scoreManager.CommitLeaderboardEntry();

// After
if (!pendingLeaderboardCommit && activeRunMode == RunMode.Normal)
    scoreManager.CommitLeaderboardEntry();
```

**Location B — deferred call in `CommitPendingLeaderboardIfNeeded()` (L2685–2693):**

This single method is called from all deferred paths (continue handler, rewarded-ad callback, `ResolvePendingFailedRun`). Guarding it here covers all of them.

```csharp
// Before
private void CommitPendingLeaderboardIfNeeded()
{
    if (!pendingLeaderboardCommit) { return; }
    scoreManager.CommitLeaderboardEntry();
    pendingLeaderboardCommit = false;
}

// After
private void CommitPendingLeaderboardIfNeeded()
{
    if (!pendingLeaderboardCommit) { return; }
    if (activeRunMode == RunMode.Normal)
    {
        scoreManager.CommitLeaderboardEntry();
    }
    pendingLeaderboardCommit = false;
}
```

---

### Change 4: Best score — Normal mode only

**Location A — guard `CommitBest()` call (L2587):**
```csharp
// Before
scoreManager.CommitBest();

// After
if (activeRunMode == RunMode.Normal) { scoreManager.CommitBest(); }
```

**Location B — reset in-memory `BestScore` in `ResetRun()` (L1596):**

`Tick()` (L1602) updates `BestScore = Mathf.Max(BestScore, CurrentScore)` live during every run. Without this reset, a high Daily Challenge score inflates the in-memory `BestScore`, which the HUD shows as the Normal mode target on the next run. Resetting to `persistedBestScore` (the last committed Normal best) on each run start fixes this.

```csharp
// Before
public void ResetRun()
{
    CurrentScore = 0f;
    CurrentRunTime = 0f;
}

// After
public void ResetRun()
{
    CurrentScore = 0f;
    CurrentRunTime = 0f;
    BestScore = persistedBestScore;
}
```

---

### Change 5: Suppress "NEW BEST" display for non-Normal runs

`IsNewBestThisRun` is a `ScoreManager` property (`L1581: CurrentScore > persistedBestScore + 0.001f`). It drives the green "NEW BEST" card shown live in the HUD (`hudController.SetValues` → `newBestCard.gameObject.SetActive(isNewBest)`). It is passed raw to UI at 5 call sites in `RunManager`. All 5 must be guarded.

Replace every `scoreManager.IsNewBestThisRun` with `(activeRunMode == RunMode.Normal && scoreManager.IsNewBestThisRun)` at:

| Line | Context |
|---|---|
| L2353 | `UpdateHud` — live gameplay, called every frame |
| L2636 | `ShowCountdown("GO!", ...)` — GO! phase |
| L2653 | `ShowCountdown(...)` — countdown tick |
| L2670 | `ShowCountdown("GO!", ...)` — alternate GO! path |
| L2682 | `ShowCountdown(displayValue, ...)` — number phase |
| L3113 | `BuildRunSummary()` — end-of-run summary |

**`BuildBestDeltaText()` (L3121)** — fail screen "NEW BEST" text, also guarded:

```csharp
// Before
if (delta >= 0f)
    return $"NEW BEST  +{delta:0.0}m";

// After
if (delta >= 0f && activeRunMode == RunMode.Normal)
    return $"NEW BEST  +{delta:0.0}m";
```

Note: `BuildBestDeltaText()` is called at L2584, three lines before `CommitBest()` at L2587 — this ordering is intentional (capture the pre-commit delta first) and must not be changed.

---

## What Daily Challenge Affects

| Stat | Normal | Daily Challenge |
|---|---|---|
| Best score (persisted) | ✅ | ❌ |
| Leaderboard | ✅ | ❌ |
| "NEW BEST" display | ✅ | ❌ |
| Coin rewards | ✅ | ✅ |
| Daily mission progress | ✅ | ✅ |
| Daily challenge best height | — | ✅ (own stat) |

---

## Scope

- 1 method modified in `ScoreManager` (`ResetRun`)
- 11 locations modified in `RunManager`:
  - `ConfigureRunModifiers` else branch (L3051)
  - `ApplyModifier` HighStakes case (L3073)
  - `GetHorizontalSpeedMultiplier` (L3079)
  - `GetClimbSpeedMultiplier` (L3090)
  - Fail handler: `CommitBest` guard (L2587)
  - Fail handler: `CommitLeaderboardEntry` guard (L2592)
  - `CommitPendingLeaderboardIfNeeded` (L2685)
  - `BuildBestDeltaText` (L3121)
  - `IsNewBestThisRun` at `UpdateHud` (L2353)
  - `IsNewBestThisRun` at 4x `ShowCountdown` calls (L2636, L2653, L2670, L2682)
  - `IsNewBestThisRun` at `BuildRunSummary` (L3113)
- No new fields, parameters, or abstractions
