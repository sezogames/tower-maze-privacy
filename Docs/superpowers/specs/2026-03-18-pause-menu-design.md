# Pause Menu Design

**Date:** 2026-03-18

## Goal

Add a pause button to the gameplay HUD that freezes the game and shows a minimal overlay with Resume and Return to Main Menu options.

## Trigger

- **Pause button:** `II` button added to `UIHudController`, bottom-left corner (anchored at approx (0.03, 0.03)–(0.13, 0.072)), positioned to the left of the existing controls hint card.
- **Android back button:** `Input.GetKeyDown(KeyCode.Escape)` in `RunManager.Update()` toggles pause.
- **Only available during `RunState.Running`** — ignored during Countdown, StartScreen, Failed.

## Pause / Resume Behavior

**On pause:**
1. `Time.timeScale = 0` — freezes physics, Update tick accumulation, lava, player
2. `AudioListener.pause = true` — silences all audio
3. `uiManager.ShowPause()` — shows `PauseScreenController` overlay on top of HUD

**On resume:**
1. `uiManager.HidePause()` — hides overlay
2. `AudioListener.pause = false` — restores audio
3. `Time.timeScale = 1` — resumes simulation

## New Components

### PauseScreenController (in UISystems.cs)

A new sealed class following the existing screen controller pattern:
- Created in `UIManager.Initialize()` as a child of the canvas, always rendered above HUD
- Hidden by default (`gameObject.SetActive(false)`)
- Contains two buttons: **DEVAM ET** (Resume) and **ANA MENÜ** (Return to Main Menu)
- Receives `onResume: Action` and `onReturnToMenu: Action` callbacks in `Initialize()`
- Exposed via `UIManager.ShowPause()` / `UIManager.HidePause()`

### UIHudController pause button

- Small `II` button created in `UIHudController.Initialize()`, anchored bottom-left
- Receives `onPause: Action` callback; wired with `UIManager.BindButton()`
- `SetPauseCallback(Action onPause)` method called from UIManager after HUD is created

## RunManager Changes

- Add `private bool isPaused` field
- Add `PauseRun()` and `ResumeRun()` methods
- In `RunManager.Update()`, before the `Running` state tick:
  - Check `Input.GetKeyDown(KeyCode.Escape)` → toggle pause
  - If `isPaused`, skip all tick logic (score, lava, player input)
- `ReturnToMainMenu()` must reset `isPaused = false` and `Time.timeScale = 1` before transitioning

## UIManager Changes

- Add `private PauseScreenController pauseScreenController` field
- Create `pauseScreenController` in `Initialize()` after HUD, pass `onResume` and `onReturnToMenu` callbacks
- Add `public void ShowPause()` → `pauseScreenController.gameObject.SetActive(true)`
- Add `public void HidePause()` → `pauseScreenController.gameObject.SetActive(false)`
- `UIManager.Initialize()` gains two new parameters: `Action onPause` and `Action onResume`
  - `onPause` is passed to HUD's pause button
  - `onResume` is passed to `PauseScreenController`

## TowerMazeBootstrapper Changes

- Pass `runManager.PauseRun` and `runManager.ResumeRun` to `uiManager.Initialize()`

## Files Changed

- `Assets/Scripts/TowerMaze/Runtime/UISystems.cs`
  - `UIManager`: new `PauseScreenController` field, `ShowPause()`, `HidePause()`, updated `Initialize()` signature
  - `UIHudController`: pause button + `SetPauseCallback()`
  - New `PauseScreenController` class
- `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs`
  - `RunManager`: `isPaused`, `PauseRun()`, `ResumeRun()`, back-button handling in `Update()`, timeScale reset in `ReturnToMainMenu()`
- `Assets/Scripts/TowerMaze/Runtime/TowerMazeBootstrapper.cs`
  - Pass `runManager.PauseRun` / `runManager.ResumeRun` to `uiManager.Initialize()`

## Constraints

- `Time.timeScale` must be reset to `1` in `ReturnToMainMenu()` to avoid a frozen main menu if the user returns while paused
- Pause is **not** available during Countdown or Failed states
- `AudioListener.pause` pauses all audio globally — no need to touch `AudioManager` individually
