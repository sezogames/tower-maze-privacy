# Normal Mode Isolation & High Stakes Rebalance Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make Normal mode a clean modifier-free experience whose stats (best score, leaderboard) are fully isolated from Daily Challenge runs, and rebalance HighStakes to be a fair risk/reward modifier.

**Architecture:** All changes are in `RunSystems.cs` — one file. `ScoreManager` is a nested class within it. No new abstractions; surgical edits to existing methods. No automated test runner exists; verification is manual play-test in Unity Editor.

**Tech Stack:** Unity C#, `RunSystems.cs`

**Spec:** `docs/superpowers/specs/2026-03-16-normal-mode-isolation-design.md`

---

## Chunk 1: Modifier changes (Normal = no modifiers, HighStakes rebalance)

### Task 1: Normal mode always starts with no modifiers

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs:3051-3056`

- [ ] **Step 1: Make the change**

In `ConfigureRunModifiers()`, replace the entire `else` block (L3051–3056):

```csharp
// Before
else
{
    System.Random modifierRandom = new(runSeed ^ Environment.TickCount);
    primaryRunModifier = modifierRandom.Next(0, 2) == 0 ? RunModifierType.Slipstream : RunModifierType.HighStakes;
    secondaryRunModifier = RunModifierType.None;
}

// After
else
{
    primaryRunModifier = RunModifierType.None;
    secondaryRunModifier = RunModifierType.None;
}
```

- [ ] **Step 2: Play-test**

  1. Open Unity Editor, press Play
  2. Click the Start button to begin a Normal run
  3. Let the run start and watch the countdown toast
  4. **Expected:** No modifier toast appears (no "SLIPSTREAM" or "HIGH STAKES" banner)
  5. Repeat 3 times — every Normal run must have no modifier

---

### Task 2: HighStakes rebalance — reduced lava, faster movement

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs:3073-3075` (sink multiplier)
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs:3079-3088` (horizontal speed)
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs:3090-3099` (climb speed)

- [ ] **Step 3: Reduce sink multiplier**

In `ApplyModifier()` at L3074:
```csharp
// Before
case RunModifierType.HighStakes:
    sinkModifierMultiplier *= 1.18f;

// After
case RunModifierType.HighStakes:
    sinkModifierMultiplier *= 1.08f;
```

- [ ] **Step 4: Add horizontal speed bonus**

In `GetHorizontalSpeedMultiplier()`, add after the existing Slipstream block (L3084–3086):
```csharp
if (primaryRunModifier == RunModifierType.HighStakes || secondaryRunModifier == RunModifierType.HighStakes)
{
    multiplier *= 1.10f;
}
```

Full method after change:
```csharp
private float GetHorizontalSpeedMultiplier()
{
    float multiplier = 1f;
    if (primaryRunModifier == RunModifierType.Slipstream || secondaryRunModifier == RunModifierType.Slipstream)
    {
        multiplier *= 1.35f;
    }
    if (primaryRunModifier == RunModifierType.HighStakes || secondaryRunModifier == RunModifierType.HighStakes)
    {
        multiplier *= 1.10f;
    }
    return multiplier;
}
```

- [ ] **Step 5: Add climb speed bonus**

In `GetClimbSpeedMultiplier()`, add after the existing Slipstream block (L3094–3096):
```csharp
if (primaryRunModifier == RunModifierType.HighStakes || secondaryRunModifier == RunModifierType.HighStakes)
{
    multiplier *= 1.10f;
}
```

Full method after change:
```csharp
private float GetClimbSpeedMultiplier()
{
    float multiplier = 1f;
    if (primaryRunModifier == RunModifierType.Slipstream || secondaryRunModifier == RunModifierType.Slipstream)
    {
        multiplier *= 1.28f;
    }
    if (primaryRunModifier == RunModifierType.HighStakes || secondaryRunModifier == RunModifierType.HighStakes)
    {
        multiplier *= 1.10f;
    }
    return multiplier;
}
```

- [ ] **Step 6: Play-test HighStakes in Daily Challenge**

  1. Start a Daily Challenge run (which uses modifiers)
  2. If today's modifier is HighStakes: verify movement feels slightly faster and lava rises less aggressively than before
  3. **Expected:** Noticeably more survivable than before, with slightly faster player movement

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/RunSystems.cs
git commit -m "feat: normal mode always no modifiers; rebalance HighStakes (lava 1.08x, movement 1.10x)"
```

---

## Chunk 2: Best score isolation

### Task 3: Best score only commits for Normal runs; Daily resets in-memory best

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs:2587` (CommitBest guard)
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs:1596-1600` (ScoreManager.ResetRun)

- [ ] **Step 1: Guard CommitBest for Normal mode only**

In the fail handler around L2587, change:
```csharp
// Before
scoreManager.CommitBest();

// After
if (activeRunMode == RunMode.Normal) { scoreManager.CommitBest(); }
```

⚠️ Important: `BuildBestDeltaText()` is called at L2584, three lines before this. Do NOT reorder these calls — the delta text must be captured before CommitBest runs.

- [ ] **Step 2: Reset in-memory BestScore on ResetRun**

In `ScoreManager.ResetRun()` at L1596–1600:
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

This prevents a high Daily Challenge score from inflating the Normal mode best shown in the HUD on the next run.

> **Note:** `Initialize()` at L1592 sets `BestScore = Mathf.Max(BestScore, leaderboardEntries[0].height)`, which can make `BestScore` higher than `persistedBestScore` on first load if the leaderboard top entry is higher. After this change, `ResetRun()` will reset `BestScore` to `persistedBestScore` at the start of each run — meaning the HUD "best" target reflects the raw Normal-mode persisted score, not the leaderboard-derived value. This is intentional: Normal mode isolation means the persisted float is the authoritative best.

- [ ] **Step 3: Play-test**

  1. Note your current Normal mode best score (shown on the start screen)
  2. Play a Daily Challenge run and reach a higher height than your Normal best
  3. Fail / end the run
  4. Return to the start screen
  5. **Expected:** The best score shown is still your original Normal best, NOT the Daily Challenge height
  6. Play a Normal run and reach a new personal best
  7. **Expected:** The best score updates correctly

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/RunSystems.cs
git commit -m "feat: best score only persists and displays from Normal mode runs"
```

---

## Chunk 3: Leaderboard isolation

### Task 4: Leaderboard only updated by Normal mode runs

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs:2592-2594` (fail handler direct call)
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs:2685-2694` (CommitPendingLeaderboardIfNeeded)

- [ ] **Step 1: Guard direct CommitLeaderboardEntry in fail handler**

At L2592–2594:
```csharp
// Before
if (!pendingLeaderboardCommit)
{
    scoreManager.CommitLeaderboardEntry();
}

// After
if (!pendingLeaderboardCommit && activeRunMode == RunMode.Normal)
{
    scoreManager.CommitLeaderboardEntry();
}
```

- [ ] **Step 2: Guard CommitPendingLeaderboardIfNeeded**

This method is called from all deferred paths (continue, rewarded ad callback, ResolvePendingFailedRun). Guarding it here covers all of them.

```csharp
// Before
private void CommitPendingLeaderboardIfNeeded()
{
    if (!pendingLeaderboardCommit)
    {
        return;
    }

    scoreManager.CommitLeaderboardEntry();
    pendingLeaderboardCommit = false;
}

// After
private void CommitPendingLeaderboardIfNeeded()
{
    if (!pendingLeaderboardCommit)
    {
        return;
    }

    if (activeRunMode == RunMode.Normal)
    {
        scoreManager.CommitLeaderboardEntry();
    }
    pendingLeaderboardCommit = false;
}
```

- [ ] **Step 3: Play-test**

  1. Note current leaderboard entries on the start screen (TOP RUNS)
  2. Play a Daily Challenge run and reach a height that would normally appear on the leaderboard
  3. Return to the start screen
  4. **Expected:** TOP RUNS leaderboard is unchanged
  5. Play a Normal run and reach a new height
  6. **Expected:** New entry appears in TOP RUNS

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/RunSystems.cs
git commit -m "feat: leaderboard (TOP RUNS) only updated by Normal mode runs"
```

---

## Chunk 4: NEW BEST display suppression

### Task 5: Suppress "NEW BEST" HUD card and text for non-Normal runs

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs:2353` (UpdateHud)
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs:2636` (ShowCountdown GO!)
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs:2653` (ShowCountdown tick)
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs:2670` (ShowCountdown GO! alt path)
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs:2682` (ShowCountdown number)
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs:3113` (BuildRunSummary)
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs:3125` (BuildBestDeltaText)

Replace every `scoreManager.IsNewBestThisRun` with `(activeRunMode == RunMode.Normal && scoreManager.IsNewBestThisRun)` at all 6 call sites, and guard the NEW BEST branch in `BuildBestDeltaText`.

- [ ] **Step 1: Guard UpdateHud call (L2353)**

```csharp
// Before
scoreManager.IsNewBestThisRun,

// After
activeRunMode == RunMode.Normal && scoreManager.IsNewBestThisRun,
```

- [ ] **Step 2: Guard four ShowCountdown calls (L2636, L2653, L2670, L2682)**

Each of these lines contains `scoreManager.IsNewBestThisRun` as a parameter. Replace each with `activeRunMode == RunMode.Normal && scoreManager.IsNewBestThisRun`.

- [ ] **Step 3: Guard BuildRunSummary (L3113)**

```csharp
// Before
scoreManager.IsNewBestThisRun,

// After
activeRunMode == RunMode.Normal && scoreManager.IsNewBestThisRun,
```

- [ ] **Step 4: Guard BuildBestDeltaText (method starts L3121, edit at L3125)**

```csharp
// Before
if (delta >= 0f)
{
    return $"NEW BEST  +{delta:0.0}m";
}

// After
if (delta >= 0f && activeRunMode == RunMode.Normal)
{
    return $"NEW BEST  +{delta:0.0}m";
}
```

- [ ] **Step 5: Play-test**

  1. Play a Daily Challenge run and reach a height above your Normal mode personal best
  2. During the run: **Expected:** No green "NEW BEST" card appears in the HUD
  3. On the fail screen: **Expected:** Shows "BEST DELTA -Xm" (or similar), NOT "NEW BEST +Xm"
  4. Play a Normal run and beat your personal best
  5. During the run: **Expected:** Green "NEW BEST" card appears correctly
  6. On the fail screen: **Expected:** Shows "NEW BEST +Xm"

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/RunSystems.cs
git commit -m "feat: suppress NEW BEST display and HUD card for non-Normal mode runs"
```
