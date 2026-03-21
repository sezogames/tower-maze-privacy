# Life Refill Auto-Retry Design

**Date:** 2026-03-16
**Status:** Approved

## Problem

When a player runs out of lives on the fail screen, they can watch an ad or spend coins to refill a life. After the refill succeeds, the game currently re-displays the fail screen — forcing the player to press RETRY a second time. This extra step is unnecessary friction.

## Goal

After a successful life refill (via ad or coins) from the fail screen, automatically start the next run without requiring an additional button press.

## Solution

**File:** `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs`

Modify the success path of `WatchAdForLifeRefill()` and `BuyLifeRefillWithCoins()` so that when in `RunState.Failed`, call `RetryRun()` directly instead of `RefreshLifeRefillPrompt(true)`.

### WatchAdForLifeRefill — success callback change (L2440–2451)

```
Before: GrantLife() → toast → RefreshLifeRefillPrompt(true)
After:  GrantLife() → toast → state == Failed ? RetryRun() : RefreshLifeRefillPrompt(true)
```

Only the success callback body changes. The guard at L2434–2438 (checking `RemainingLives >= MaxLifeCount`, null checks, `CanShowRewarded`) and the failure callback (`if (!success) RefreshLifeRefillPrompt()`) are **not modified**.

### BuyLifeRefillWithCoins — success path change (L2454–2470)

```
Before: toast → RefreshLifeRefillPrompt(true)
After:  toast → state == Failed ? RetryRun() : RefreshLifeRefillPrompt(true)
```

The guard paths (null checks, `TryBuyLifeRefill` failure) are **not modified**.

## How RetryRun() Works

`RetryRun()` (L2388) executes this sequence:
1. `pendingRunMode = activeRunMode` — inherits the current run mode (Normal or DailyChallenge). Since `activeRunMode` is set when a run starts and only cleared on `ReturnToMainMenu`, it is always valid while in `RunState.Failed`.
2. `TryConsumeLifeForRequestedRun()` — consumes one life. This is the **intended pairing** with the `GrantLife()` called just before: grant +1, then consume -1, net zero on the balance but the run is now authorized to start. If `economyManager` is null, `TryConsumeLife()` returns `true` unconditionally (L2750), so the null case is safe.
3. `ResolvePendingFailedRun(false)` — clears the failed run state and rewards.
4. `PrepareFreshRun()` — resets tower, score, lava.
5. `BeginCountdown()` — dismisses the fail screen UI and shows the 3-2-1 countdown HUD.

### TryConsumeLifeForRequestedRun failure after grant

If `TryConsumeLifeForRequestedRun()` returns `false` (which should not happen since a life was just granted), `TryConsumeLifeForRequestedRun()` calls `RefreshLifeRefillPrompt()` internally before returning `false`, then `RetryRun()` returns early. The player retains the granted life and sees the fail screen again. This is a safe fallback, not a regression.

## Edge Cases

| Scenario | Behavior |
|---|---|
| Ad fails / is skipped | No change — failure callback calls `RefreshLifeRefillPrompt()` as before |
| Not enough coins | No change — error toast shown, method returns early |
| `TryConsumeLifeForRequestedRun()` fails unexpectedly | `TryConsumeLifeForRequestedRun()` calls `RefreshLifeRefillPrompt()` then returns `false`; `RetryRun()` returns early; life is retained |
| Refill called outside fail screen (e.g. main menu life panel) | `state != Failed` → `RefreshLifeRefillPrompt(true)` called → navigates to `ShowStart(...)` with current score/economy state, identical to today's behavior |

## Scope

- 2 methods modified in `RunSystems.cs` (success paths only)
- No UI changes required — `BeginCountdown()` handles the fail screen dismissal
- No new parameters, flags, or abstractions introduced
