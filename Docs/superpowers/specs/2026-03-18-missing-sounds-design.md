# Missing Sounds Design

**Date:** 2026-03-18

## Goal

Add sound feedback for four in-game events that currently trigger visual toasts but have no audio: zone transitions, mission completion, new best score, and coin rewards.

## Approach

Event-based methods on `AudioManager` — one dedicated `Play*()` method per event, consistent with existing methods (`PlayWallBump`, `PlayFailCue`, `PlayContinueCue`). Each method loads its clip from `Resources/TowerMaze/Sounds/` via `Resources.Load<AudioClip>` in `Awake()`. All methods use the existing `PlayClip()` helper which already enforces the `SoundEnabled` flag.

## Sound Mapping

| Event | Kenney File | AudioManager Method | Volume | Call Site |
|-------|-------------|---------------------|--------|-----------|
| Zone geçişi | `switch-a.ogg` | `PlayZoneReached()` | 0.7f | `RunSystems.cs` ~line 2647, after `QueueRewardToast("ZONE X")` |
| Görev tamamlama | `tap-a.ogg` | `PlayMissionComplete()` | 0.85f | `RunSystems.cs` ~line 3052, after mission toast |
| Yeni rekor | `switch-b.ogg` | `PlayNewBest()` | 0.9f | `RunSystems.cs` in `FailRun()`, **before** `scoreManager.CommitBest()` (line ~2905) — after CommitBest the property becomes false |
| Coin ödülü | `tap-b.ogg` | `PlayReward()` | 0.75f | `RunSystems.cs` ~line 2891 (rewarded ad) and ~line 3057 (daily challenge reward) |

**Not included:** Continue satın alma (~line 2742) ve extra life satın alma (~line 2763) — bunlar harcama eylemleri, ödül değil.

## New Best Call Site Detail

```csharp
// In FailRun(), before CommitBest():
if (activeRunMode == RunMode.Normal && scoreManager.IsNewBestThisRun)
{
    audioManager.PlayNewBest();
}
if (activeRunMode == RunMode.Normal) { scoreManager.CommitBest(); }
```

`IsNewBestThisRun` is `CurrentScore > persistedBestScore + 0.001f` — after `CommitBest()` persists the score, this becomes false. Must check before commit.

## Files Changed

- Copy 4 `.ogg` files from `Assets/ThirdParty/UI/Kenney/Sounds/` to `Assets/Resources/TowerMaze/Sounds/`:
  - `switch-a.ogg`, `switch-b.ogg`, `tap-a.ogg`, `tap-b.ogg`
- `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs` — `AudioManager` class:
  - Add 4 `AudioClip` fields
  - Load all 4 in `Awake()` via `Resources.Load`
  - Add 4 public `Play*()` methods
- `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs` — `RunManager` class:
  - 4 call sites added (zone transition, mission complete, new best, coin reward ×2)

## Volume Reference

Base `uiAudioSource.volume = 0.32f`. Volume scales are relative to this:
- `PlayWallBump` = 0.65f, `PlayContinueCue` = 0.9f, `PlayButtonClick` = 0.75f
- New sounds follow the same range (0.7–0.9f)
