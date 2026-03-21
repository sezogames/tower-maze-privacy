# Life Refill Auto-Retry Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** After a successful life refill (ad or coin) on the fail screen, automatically start the next run without requiring a second button press.

**Architecture:** Modify the success paths of `WatchAdForLifeRefill()` and `BuyLifeRefillWithCoins()` in `RunSystems.cs` — when in `RunState.Failed`, call `RetryRun()` directly instead of `RefreshLifeRefillPrompt(true)`. No new abstractions; 2 lines changed.

**Tech Stack:** Unity C#, no automated test runner for game logic (manual play-test verification)

**Spec:** `docs/superpowers/specs/2026-03-16-life-refill-auto-retry-design.md`

---

## Chunk 1: Both method changes + verification

### Task 1: Modify `WatchAdForLifeRefill()` success callback

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs:2450`

Current code at L2450:
```csharp
RefreshLifeRefillPrompt(true);
```

- [ ] **Step 1: Make the change**

Replace line 2450 in `RunSystems.cs`:

```csharp
// Before
RefreshLifeRefillPrompt(true);

// After
if (state == RunState.Failed) { RetryRun(); } else { RefreshLifeRefillPrompt(true); }
```

The full success callback (L2442–2451) after the change:
```csharp
if (!success)
{
    RefreshLifeRefillPrompt();
    return;
}

economyManager.GrantLife();
uiManager.QueueRewardToast("EXTRA LIFE", $"+{EconomyManager.LifeRefillAmount} LIFE", new Color(0.36f, 0.9f, 0.48f, 1f));
if (state == RunState.Failed) { RetryRun(); } else { RefreshLifeRefillPrompt(true); }
```

---

### Task 2: Modify `BuyLifeRefillWithCoins()` success path

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs:2470`

Current code at L2470:
```csharp
RefreshLifeRefillPrompt(true);
```

- [ ] **Step 2: Make the change**

Replace line 2470 in `RunSystems.cs`:

```csharp
// Before
RefreshLifeRefillPrompt(true);

// After
if (state == RunState.Failed) { RetryRun(); } else { RefreshLifeRefillPrompt(true); }
```

The full success path (L2462–2470) after the change:
```csharp
if (!economyManager.TryBuyLifeRefill(out int spentCoins))
{
    uiManager.QueueRewardToast("NEED COIN", $"{EconomyManager.LifeRefillCoinCost} COIN REQUIRED", new Color(1f, 0.42f, 0.36f, 1f));
    RefreshLifeRefillPrompt();
    return;
}

uiManager.QueueRewardToast("EXTRA LIFE", $"-{spentCoins} COIN  +{EconomyManager.LifeRefillAmount} LIFE", new Color(0.36f, 0.9f, 0.48f, 1f));
if (state == RunState.Failed) { RetryRun(); } else { RefreshLifeRefillPrompt(true); }
```

---

### Task 3: Build and verify in Unity

- [ ] **Step 3: Open Unity and check for compile errors**

Open Unity Editor. Check the Console window for any compilation errors. Expected: no errors.

- [ ] **Step 4: Play-test — ad path**

  1. Run the game in the Editor
  2. Die until lives reach 0 (fail screen appears with "WATCH AD +1 LIFE" button)
  3. Click "WATCH AD +1 LIFE"
  4. Complete or mock the ad
  5. **Expected:** countdown starts immediately, no second button press required
  6. **Verify:** life balance decremented back to 0 after run starts (grant +1, consume -1)

- [ ] **Step 5: Play-test — coin path**

  1. Ensure coin balance ≥ 250 (LifeRefillCoinCost)
  2. Die until lives reach 0
  3. Click "NEED 250 COIN" button (should show as active when coins are sufficient)
  4. **Expected:** 250 coins deducted, countdown starts immediately
  5. **Verify:** life balance still 0 after run starts (grant +1, consume -1)

- [ ] **Step 6: Play-test — regression check (life refill outside fail screen)**

  1. Navigate to main menu life panel (if accessible)
  2. Watch ad or buy life from there
  3. **Expected:** old behavior preserved — start screen shown, no auto-run

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/RunSystems.cs
git commit -m "feat: auto-retry run after life refill ad or coin purchase on fail screen"
```
