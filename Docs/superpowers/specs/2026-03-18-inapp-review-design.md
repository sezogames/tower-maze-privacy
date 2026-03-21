# In-App Review Prompt Design

**Date:** 2026-03-18

## Goal

Show the native store review prompt once, after the player's 5th run, at the natural pause when returning to the main menu.

## Trigger Conditions

- `TotalRuns >= 5` (already tracked in `EconomyManager.TotalRuns`, incremented via `IncrementTotalRuns()`)
- Never shown before (`PlayerPrefs` flag not set)
- Shown in `RunManager.ReturnToMainMenu()` — player has finished the fail screen, calm moment

## Persistence

- Key: `"TowerMaze.ReviewRequested"` (int, 0 = not shown, 1 = shown)
- Two new methods on `EconomyManager`:
  - `ShouldRequestReview()` → `TotalRuns >= 5 && PlayerPrefs.GetInt("TowerMaze.ReviewRequested", 0) == 0`
  - `MarkReviewRequested()` → sets key to 1 and calls `PlayerPrefs.Save()`

## Platform Implementation

```csharp
#if UNITY_IOS
    UnityEngine.iOS.Device.RequestStoreReview();
#elif UNITY_ANDROID
    Application.OpenURL("market://details?id=" + Application.identifier);
#endif
```

- **iOS:** Built-in Unity API, no package needed. System throttles display (max 3×/year).
- **Android:** Opens Play Store listing. No extra package needed. Works even before bundle ID is finalised (uses `Application.identifier` at runtime).

## Call Site

In `RunManager.ReturnToMainMenu()` (~line 2797), after `audioManager.SetMusicMode(Menu)` and before `uiManager.ShowStart(...)`:

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

## Files Changed

- `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs` — `EconomyManager`: add `ShouldRequestReview()` and `MarkReviewRequested()`
- `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs` — `RunManager.ReturnToMainMenu()`: add review trigger block

## Constraints

- Show exactly once — `MarkReviewRequested()` is called before the platform API, so even if the API fails the flag is set
- No UI changes — native system dialog handles everything
- No new packages or dependencies
