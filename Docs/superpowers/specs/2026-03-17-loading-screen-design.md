# TowerMaze Loading Screen — Design Spec

**Date:** 2026-03-17
**Status:** Revised v2

---

## Context

TowerMaze is a Unity 6 mobile game with a single scene (`TowerMazePrototype.unity`). All initialization is synchronous inside `TowerMazeBootstrapper.Awake()` (ExecutionOrder: -100). There is currently no splash/loading screen — the game jumps straight to the Start Screen.

The goal is to display the TowerMaze key art (tower over lava, 1440×2560) as a branded splash experience at every app launch, with an animated logo and spinning loader.

---

## Scope

Two layers:
1. **Unity built-in Splash Screen** — shown by the engine before the first scene loads
2. **Custom in-game SplashScreenController** — shown during `TowerMazeBootstrapper.Awake()` initialization, bridges to Start Screen

---

## Architecture

### New: `SplashScreenController` (added to `UISystems.cs`)

```
SplashScreenController : MonoBehaviour
├── Own Canvas (sortOrder = 100, ScreenSpaceOverlay)
├── RawImage — full-screen background (SplashBackground texture, or black fallback)
├── Text — "TOWERMAZE" (Kenney Future font, orange #FF7A00)
├── Image — spinner ring (circular fill animation using UITheme's panel sprite as ring base)
└── CanvasGroup — used for fade in/out
```

**Public API:**
```csharp
// onComplete is called when splash finishes its full lifecycle (min time + fade-out)
void Initialize(Font font, Texture2D backgroundTexture, Action onComplete)
bool IsVisible { get; }
```

**Behavior — self-managing lifecycle:**
- On `Initialize()`: starts a coroutine that:
  1. Fades in CanvasGroup alpha 0→1 over 0.3s
  2. Animates TOWERMAZE text: scale 0.8→1.0 + alpha 0→1 over 0.5s
  3. Waits until the minimum display time of 2.5s has elapsed from `Initialize()` call
  4. Fades out CanvasGroup alpha 1→0 over 0.5s
  5. Calls `onComplete`, then destroys the splash GameObject
- `IsVisible` returns true while the coroutine is active
- No external caller is needed to trigger hide — the controller is self-dismissing
- If `backgroundTexture` is null (asset missing), a solid black background is used as fallback

**Spinner:** A new `Assets/Resources/TowerMaze/UITheme/SpinnerRing.png` sprite is required — a white circle/ring on a transparent background. Import as Sprite. Used as `Image` with `Image.Type = Filled`, `FillMethod = Radial360`, `FillAmount = 0.75`. Rotated continuously via `Update()` at 360°/s. Size: 80×80px anchored to bottom-center.

---

### Modified: `UIManager` (in `UISystems.cs`)

New field:
```csharp
private Action pendingShowStart;
private bool splashComplete;
```

`Initialize()` signature gains `bool splashActive` as first parameter:
```csharp
public void Initialize(bool splashActive, Font font, ThemeDefinition theme, ...)
```
- Sets initial `splashComplete` state: `splashComplete = !splashActive`
  - `splashActive: true` → `splashComplete = false` → `ShowStart()` defers until `OnSplashComplete()` fires
  - `splashActive: false` → `splashComplete = true` → `ShowStart()` executes immediately (test/skip mode)
- Do NOT add `onSplashComplete` as a parameter here — the callback is wired exclusively through `splash.Initialize()`

New internal method called by the splash callback:
```csharp
internal void OnSplashComplete() {
    splashComplete = true;
    pendingShowStart?.Invoke();
    pendingShowStart = null;
}
```

`ShowStart()` guard (one-shot, first boot only):
```csharp
public void ShowStart(...) {
    if (!splashComplete) {
        // Splash still showing on first boot — defer
        pendingShowStart = () => ShowStartInternal(...);
        return;
    }
    ShowStartInternal(...); // All subsequent ShowStart() calls (retry, back to menu) go here directly
}
```
After `splashComplete = true`, all future `ShowStart()` calls bypass the guard and execute immediately — this does not affect the return-to-menu or retry flows.

---

### Modified: `TowerMazeBootstrapper` (in `TowerMazeBootstrapper.cs`)

**Placement:** The splash creation block must be inserted AFTER `uiManager` is assigned and BEFORE any `Initialize()` calls. In the current `Awake()`, `uiManager` is assigned on line 79 (`UIManager uiManager = EnsureComponent<UIManager>(uiRoot)`). Insert the splash block immediately after that line and before `towerGenerator.Initialize(...)`.

```csharp
// Immediately after: UIManager uiManager = EnsureComponent<UIManager>(uiRoot);
// uiManager is now non-null — safe to capture in the lambda below.

// Load splash assets (font loaded early; UIManager.Initialize will also load it later — intentional)
Texture2D splashTex = Resources.Load<Texture2D>("TowerMaze/UITheme/SplashBackground"); // may be null
Font splashFont = Resources.Load<Font>("TowerMaze/UITheme/Kenney Future");

SplashScreenController splash = new GameObject("SplashScreen")
    .AddComponent<SplashScreenController>();
// No DontDestroyOnLoad needed — single-scene project, splash destroys itself after use
splash.Initialize(splashFont, splashTex, onComplete: () => uiManager.OnSplashComplete());
```

`uiManager.Initialize()` call site update — pass `splashActive: true` as the first new argument:

```csharp
// Line 93 of TowerMazeBootstrapper.cs — add splashActive as first new argument
uiManager.Initialize(splashActive: true, themeDefinition, economyManager, ...existing args...);
```

`UIManager.Initialize()` uses `splashActive` to set its initial `splashComplete` state:

- `splashActive: true` → `splashComplete = false` (defer `ShowStart()` until `OnSplashComplete()` fires)
- `splashActive: false` → `splashComplete = true` (no deferral — `ShowStart()` works immediately; used in Editor test mode or if splash is skipped)

The `onComplete` callback already wired in `splash.Initialize()` is the sole trigger for `OnSplashComplete()`. There is no second path — do not add `onSplashComplete` as a parameter to `UIManager.Initialize()`.

---

## Asset

| File | Type | Import Settings | Notes |
|------|------|----------------|-------|
| `Assets/Resources/TowerMaze/UITheme/SplashBackground.png` | Texture2D | Texture Type: **Default** (not Sprite) | Loaded at runtime via `Resources.Load<Texture2D>()`. Also import a copy as Sprite separately if needed for Player Settings. |
| `Assets/Resources/TowerMaze/UITheme/SpinnerRing.png` | Sprite | Texture Type: **Sprite (2D and UI)**, Filter: Bilinear, PPU: 100 | White circle/ring on transparent background, ~256×256px. Used by `Image` component with `FillMethod = Radial360`. |

**Why Default (not Sprite):** `Resources.Load<Texture2D>()` returns null on Sprite-typed assets in Unity. Since `RawImage` requires a `Texture2D`, import type must be `Default`. For Player Settings splash, configure the background color to black and use the same image as a Sprite in a separate `SplashBackground_Sprite` asset, or use the `fullscreen background color` option.

---

## Data Flow

```
App launch
  → Unity Splash Screen (engine-level, ~2s)
  → Scene loads
  → TowerMazeBootstrapper.Awake() starts
      → SplashScreenController created, self-dismissing timer starts (2.5s)
        (splash closes over uiManager reference, calls OnSplashComplete when done)
      → EnsureRoots(), managers init, UIManager.Initialize() (splashActive: true)
      → RunManager.Initialize() → RunManager calls UIManager.ShowStart()
          → splashComplete == false → deferred into pendingShowStart
      → Splash 2.5s elapses → fade-out → OnSplashComplete() called
          → splashComplete = true → pendingShowStart() executes
  → Start Screen visible
  → All subsequent ShowStart() calls (retry, back to menu) bypass guard (splashComplete == true)
```

---

## Lifecycle Notes

- Splash canvas is **destroyed** (not deactivated) after fade-out to avoid a dormant high-sortOrder canvas interfering with raycasting
- `DontDestroyOnLoad` is **not used** — single-scene project, object lifetime is within the scene
- If `Resources.Load<Texture2D>()` returns null, splash still shows with a black background (no crash)
- Test with **standard Enter Play Mode** (domain reload + scene reload enabled) for first integration pass — Fast Enter Play Mode (domain reload disabled) can cause coroutine edge cases

---

## Files to Modify

| File | Change |
|------|--------|
| `Assets/Scripts/TowerMaze/Runtime/UISystems.cs` | (1) Add `SplashScreenController` class at end of file; (2) Add `pendingShowStart` / `splashComplete` fields to `UIManager`; (3) Modify `UIManager.Initialize()` to accept `splashActive` bool; (4) Add `OnSplashComplete()` method; (5) Modify `ShowStart()` to defer when `!splashComplete` |
| `Assets/Scripts/TowerMaze/Runtime/TowerMazeBootstrapper.cs` | (1) Add splash creation block at top of `Awake()`; (2) Update `uiManager.Initialize()` call on line ~93 to pass `splashActive: true` |
| `Assets/Resources/TowerMaze/UITheme/SplashBackground.png` | New file — copy TowerMaze key art here; import as Texture Type: **Default** |
| `Assets/Resources/TowerMaze/UITheme/SpinnerRing.png` | New file — white circle/ring on transparent background (~256×256px); import as Texture Type: **Sprite (2D and UI)**, Filter: Bilinear, PPU: 100 |
| Project Settings → Player → Splash Screen | Manual: disable Unity logo, set background to black, add key art |

---

## Verification

1. Enter Play Mode in Unity Editor (standard mode, not Fast Enter Play Mode)
2. Splash should immediately cover the screen with the TowerMaze image
3. TOWERMAZE text animates in (scale + fade), spinner rotates continuously
4. After ~2.5s, splash fades out and Start Screen appears
5. Confirm no null reference errors if `SplashBackground.png` is temporarily removed (black fallback)
6. Build to Android/iOS — verify Unity engine splash precedes the custom overlay
7. Navigate in-game: retry a run, return to main menu — confirm Start Screen appears normally without splash interference
