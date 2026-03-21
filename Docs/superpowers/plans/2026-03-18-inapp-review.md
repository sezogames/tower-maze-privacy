# In-App Review Prompt Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Show the native store review prompt exactly once after the player's 5th run, when returning to the main menu.

**Architecture:** Add two methods to `EconomyManager` (`ShouldRequestReview`, `MarkReviewRequested`) using the existing PlayerPrefs pattern, then call them in `RunManager.ReturnToMainMenu()` with platform-specific `#if` blocks for iOS and Android.

**Tech Stack:** Unity C#, `UnityEngine.iOS.Device.RequestStoreReview()`, `Application.OpenURL`, `PlayerPrefs`.

---

## Chunk 1: EconomyManager Methods + RunManager Trigger

### Task 1: Add ShouldRequestReview and MarkReviewRequested to EconomyManager

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs` (EconomyManager class, after `IncrementTotalRuns()` ~line 1744)

Existing pattern to follow — `IncrementTotalRuns()` at line ~1739:
```csharp
public void IncrementTotalRuns()
{
    TotalRuns++;
    PlayerPrefs.SetInt(TotalRunsKey, TotalRuns);
}
```

Existing PlayerPrefs key format: `"TowerMaze.FeatureName"` (e.g. `"TowerMaze.TotalRuns"` at line ~300).

- [ ] **Step 1: Add the ReviewRequestedKey constant**

In the `EconomyManager` private constants block (near `TotalRunsKey` ~line 300), add:

```csharp
private const string ReviewRequestedKey = "TowerMaze.ReviewRequested";
```

- [ ] **Step 2: Add ShouldRequestReview method**

After `IncrementTotalRuns()` (~line 1744), add:

```csharp
public bool ShouldRequestReview()
{
    return TotalRuns >= 5 && PlayerPrefs.GetInt(ReviewRequestedKey, 0) == 0;
}
```

- [ ] **Step 3: Add MarkReviewRequested method**

Immediately after `ShouldRequestReview()`, add:

```csharp
public void MarkReviewRequested()
{
    PlayerPrefs.SetInt(ReviewRequestedKey, 1);
    PlayerPrefs.Save();
}
```

- [ ] **Step 4: Verify compiles in Unity Editor**

Switch to Unity. Confirm no compile errors in Console.

---

### Task 2: Trigger review prompt in RunManager.ReturnToMainMenu()

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs` (RunManager class, `ReturnToMainMenu()` ~line 2817)

`ReturnToMainMenu()` currently has this structure (~lines 2833–2843):
```csharp
audioManager.SetMusicMode(AudioManager.MusicMode.Menu);
uiManager.ShowStart(...);
```

Insert the review block **between** these two calls.

- [ ] **Step 1: Add the review trigger block**

After `audioManager.SetMusicMode(AudioManager.MusicMode.Menu);` and before `uiManager.ShowStart(...)`, add:

```csharp
if (economyManager.ShouldRequestReview())
{
    economyManager.MarkReviewRequested();
#if UNITY_IOS
    UnityEngine.iOS.Device.RequestStoreReview();
#elif UNITY_ANDROID
    Application.OpenURL("market://details?id=" + Application.identifier);
#endif
}
```

- [ ] **Step 2: Verify compiles in Unity Editor**

Confirm no compile errors.

- [ ] **Step 3: Test in Editor**

In Unity Editor (simulates no specific platform):
- Set `TotalRuns` to 4 via PlayerPrefs (use a temporary debug line or Unity's PlayerPrefs editor window)
- Play the game, complete a run, return to menu — confirm no prompt (TotalRuns becomes 5 but the `#if` blocks won't fire in Editor for mobile platforms)
- Verify `"TowerMaze.ReviewRequested"` key does NOT get set until `ShouldRequestReview()` returns true

To test on device: build for iOS or Android with TotalRuns = 4 saved, complete one run, return to menu — native review dialog should appear.

- [ ] **Step 4: Verify idempotency**

After the prompt fires once, `"TowerMaze.ReviewRequested"` = 1 in PlayerPrefs. Returning to menu again must NOT trigger the prompt a second time.
