# TowerMaze Full Polish — Design Spec

**Date:** 2026-03-18
**Status:** Approved
**Supersedes:** `2026-03-18-towermaze-ui-redesign.md`
**Scope:** Foundation refactor + full UI redesign + gameplay retention polish

---

## 0. Overview

A single consolidated spec covering all five improvement areas:

1. UI Architecture (foundation)
2. Screen redesigns (Start → HUD → Game Over → Shop → Skins → Popups)
3. Micro-interactions
4. Leaderboard + Missions popups
5. Gameplay retention polish

**Approach:** Foundation First (Approach B). Build a clean, compiling foundation before touching any screen visuals. Apply redesign screen by screen with clean checkpoints. Gameplay retention comes last.

---

## 1. Architecture

### 1.1 File Split

Refactor `UISystems.cs` (3,341 lines) into standalone files under:

```text
Assets/Scripts/TowerMaze/Runtime/UISystems/
```

| File | Contents |
| --- | --- |
| `UIStyle.cs` | `UIColors`, `UIFonts`, `UIMetrics`, `GradientImage`, static helpers, coroutine animation helpers |
| `UIManager.cs` | Canvas setup, screen routing, `Initialize()`, `ShowXxx()` |
| `StartScreen.cs` | `StartScreenController` — main menu, settings panel |
| `FailScreen.cs` | `FailScreenController` — Game Over screen |
| `ShopScreen.cs` | `ShopScreenController` — coin/ball/tower shop + skins grid |
| `HudController.cs` | `UIHudController` — in-game HUD |
| `PopupControllers.cs` | `CountdownController`, `PauseScreenController`, `RushWarningController`, `ControlFlipController`, `RewardToastController`, `IAPUpsellController`, `SplashScreenController` |

**Notes on specific controllers:**

- `SplashScreenController` moves to `PopupControllers.cs` as a **pure file move only** — no behavioral changes, no new fields, no visual changes in Chunk 0.
- The existing `leaderboardPanel` and missions ribbon inside `StartScreenController` are **removed** in Chunk 6 and replaced by the bottom-sheet popups defined in §5.6. These two pieces of UI must not coexist.

**Guardrail:** `UISystems.cs` is **not deleted** until full parity is confirmed (game runs, all screens open/close correctly, zero compile errors).

### 1.2 UIStyle.cs — Minimal Token System

`UIStyle.cs` contains only what is **actually referenced** by at least one controller. No speculative helpers.

**`UIColors` — static readonly Color fields (no inline hex strings elsewhere):**

| Token | Hex / RGBA | Usage |
| --- | --- | --- |
| `Brand` | `#7C4DFF` | Progress bar, selected state, tabs, score text |
| `MenuBg` | `#2D1B69` | Main menu background |
| `ShopBg` | `#1A0A35` | Shop + skins background |
| `HudBg` | `#0F0A1E` | In-game HUD background |
| `FailBg` | `#1A0F2E` | Game Over background |
| `Action` | `#FF9F0A` | Gradient buttons (start color) |
| `ActionLight` | `#FFB340` | Gradient buttons (end color) |
| `Gold` | `#FFD60A` | Coins, BEST VALUE badge |
| `Owned` | `#10B981` | Owned badge, claim button |
| `Danger` | `#EF4444` | Lava distance indicator |
| `SurfaceDark` | `rgba(255,255,255,0.07)` | Cards on dark backgrounds |
| `SurfaceDark2` | `rgba(255,255,255,0.04)` | Locked/secondary cards |
| `BorderDark` | `rgba(255,255,255,0.06)` | Card borders on dark backgrounds |
| `TextPrimary` | `#FFFFFF` | Primary text on dark |
| `TextDim` | `rgba(255,255,255,0.35)` | Secondary text on dark |
| `TextFaint` | `rgba(255,255,255,0.30)` | Captions, labels |

**`UIFonts`:** Uses `Resources.Load<Font>("TowerMaze/UITheme/Outfit-Bold")` with fallback to `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` — identical to the current loading strategy in `UIManager.Initialize()` (line 116–117 of `UISystems.cs`) to ensure zero visual change on Chunk 0.

**`UIMetrics`:** `CardRadius = 14f`, `ButtonRadius = 16f`, `PillRadius = 999f`, `SmallRadius = 10f`.

### 1.3 GradientImage — UGUI Gradient Utility

UGUI's `Image` component does not support colour gradients natively. All gradient buttons (START, CONTINUE, buy buttons, BEST VALUE card background) use a `GradientImage` component defined in `UIStyle.cs`.

```csharp
// In UIStyle.cs
public sealed class GradientImage : MaskableGraphic
{
    public Color colorTop = Color.white;
    public Color colorBottom = Color.white;
    public float cornerRadius = 0f; // reserved, not used in v1

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = GetPixelAdjustedRect();
        // Four corners: bottom-left, bottom-right, top-right, top-left
        vh.AddVert(new Vector3(r.xMin, r.yMin), colorBottom, Vector2.zero);
        vh.AddVert(new Vector3(r.xMax, r.yMin), colorBottom, Vector2.zero);
        vh.AddVert(new Vector3(r.xMax, r.yMax), colorTop,    Vector2.zero);
        vh.AddVert(new Vector3(r.xMin, r.yMax), colorTop,    Vector2.zero);
        vh.AddTriangle(0, 2, 1);
        vh.AddTriangle(0, 3, 2);
    }
}
```

Usage: `CreateGradientButton(parent, UIColors.Action, UIColors.ActionLight)` adds a `GradientImage` instead of `Image` for the button background. Corner rounding is achieved by layering a `RectTransform` mask — v1 uses a child `Image` with sprite type = sliced + white rounded-rect sprite, masking the `GradientImage`.

### 1.4 Shadow and Glow — UGUI Approximations

CSS-style shadow syntax in §4–§5 (e.g. `rgba(124,77,255,0.40) 0 4px 16px`) describes **design intent**. UGUI's `Shadow` component supports a single offset vector and colour — no blur radius. The following approximation strategy applies project-wide:

| Effect type | UGUI implementation |
| --- | --- |
| Simple drop shadow (cards, secondary buttons) | Unity `Shadow` component, `effectDistance = new Vector2(0, -3)`, appropriate `effectColor` |
| Diffuse glow (Brand button, CONTINUE, score text) | Child `Image` (named "Glow") behind element, stretched by +16px each side via `offsetMin/offsetMax`, `Color = glowColor` at alpha 0.35f, no blur |
| Animated glow pulse (BEST VALUE card) | Same "Glow" child `Image`; `Image.color.a` animated via coroutine between 0.20f and 0.55f over 1.8s loop |
| Score text glow (Game Over) | `Shadow` component with `effectDistance = Vector2.zero` and `effectColor = rgba(124,77,255,0.40)` — ambient tint effect |

The "Glow" child pattern is added by a `UIStyle.CreateGlow(RectTransform target, Color glowColor, float expand)` helper that instantiates the child behind the target in the hierarchy.

### 1.5 Animation Helpers

All coroutine helpers live in `UIStyle.cs`. Screen-specific **sequencing** (ordering, delays, staggering) stays inside each screen controller — helpers are primitives only.

**Helpers provided:**

| Method | Signature | Description |
| --- | --- | --- |
| `AnimateScale` | `(Transform t, float from, float to, float duration, AnimationCurve curve)` | Generic scale tween |
| `AnimatePulse` | `(Transform t, float minScale, float maxScale, float halfPeriod)` | Infinite idle pulse loop |
| `AnimateButtonPress` | `(Transform t)` | Scale 1→0.95→1 (0.08s + 0.12s) |
| `AnimateFade` | `(CanvasGroup cg, float from, float to, float duration)` | Alpha tween |
| `AnimateSlideY` | `(RectTransform rt, float fromY, float toY, float duration)` | AnchoredPositionY tween |
| `AnimateSlideX` | `(RectTransform rt, float fromX, float toX, float duration)` | AnchoredPositionX tween (settings panel) |
| `AnimateCoinFloat` | `(Transform parent, Font font, int amount, Color goldColor)` | Spawns "+X" `Text` (goldColor) + small coin `Image` (32×32px, `coin_hq_single` sprite) as a row, floats up and fades, auto-destroys |
| `AnimateScorePop` | `(Transform t)` | Scale 0→1.15→1 (0.28s + 0.12s) |
| `AnimateBounce` | `(RectTransform rt, float offsetY, float period)` | Infinite Y bounce loop |
| `AnimateGlowPulse` | `(Image glowImage, float alphaMin, float alphaMax, float period)` | Infinite alpha pulse on a Glow child Image |
| `CreateGlow` | `(RectTransform target, Color glowColor, float expand)` | Creates a Glow child Image behind target |

No DOTween dependency. All use `Mathf.Lerp` inside Unity Coroutines.

---

## 2. Color System Rule

- **Purple = Brand / UI identity** — progress bar, selected state, score
- **Orange = Action / Purchase** — START, CONTINUE, buy buttons
- **Gold = Value / Reward** — coins, BEST VALUE, earned state

Max 2 colors + 1 accent per screen. All screens use dark backgrounds.

---

## 3. Typography

- **Font:** Outfit-Bold (loaded via `UIFonts`)
- **Screen title:** 28–32px, weight 900
- **Button:** 13–15px, weight 700–800
- **Body / secondary:** 10–12px, weight 600
- **Label / caption:** 9–10px, weight 600, letter-spacing 1–2px
- Max 2 font sizes per screen section

---

## 4. Button System

### Primary (Brand)

```text
Background: GradientImage colorTop=#7C4DFF colorBottom=#7C4DFF (solid)
Text: white, 13px, weight 700
Radius: 16px | Height: 48px
Glow child: Brand at alpha 0.35f, expand 16px
Press: AnimateButtonPress
Haptic: vibrate (AudioManager.TriggerVibration)
```

### Secondary (dark screens)

```text
Background: Image color=rgba(255,255,255,0.10)
Text: rgba(255,255,255,0.70), 11-12px, weight 600
Radius: 14px | Height: 44px
No shadow, no glow
Press: AnimateButtonPress
Haptic: none
```

### START / CONTINUE — Dominant Action

```text
Background: GradientImage colorBottom=Action(#FF9F0A) colorTop=ActionLight(#FFB340)
Text: white, weight 800
  START: 15px, height 56px
  CONTINUE: 15px, height 60px
Radius: 16px
Glow child: Action at alpha 0.45f, expand 18px
Idle animation: AnimatePulse(1.0, 1.05, 0.7s) -- no icon on button face
Press: AnimateButtonPress
Haptic: vibrate (CONTINUE only; START: none)
```

### Buy Button (Shop)

```text
Background: GradientImage colorBottom=Action colorTop=ActionLight
Text: white, 11px, weight 700
Radius: 10px | Padding: 7px 12px | Min tap area: 44x44px
Glow child: Action at alpha 0.40f, expand 12px
Tap down: scale -> 1.05 in 0.08s
Tap up: scale -> 0.95 in 0.06s -> 1.0 in 0.10s
Haptic: vibrate
```

---

## 5. Screen Designs

### 5.1 Start Screen (Chunk 1)

**Background:** `#2D1B69`

**Layout (top to bottom, 16px horizontal padding):**

1. **Top bar** (absolute, top 16px):
   - Left: `🏆 Xm` best score — `TextFaint`, 10px/600. **Appears once only — not repeated anywhere else on this screen.**
   - Right: `🏅` leaderboard icon + `⚙` settings icon — `rgba(255,255,255,0.10)` bg, 26×26px circles, 7px gap
2. **Logo:** "TOWER MAZE", 28px/900, white, centered, margin-top 16px
3. **Spacer:** 16px
4. **START button** — GradientImage orange, 56px height, full width, `AnimatePulse` idle. No icon on button face.
5. **Secondary row** (parent `CanvasGroup.alpha = 0.65f`):
   - `[SHOP]` `[📋 MISSIONS]` — equal width, `rgba(255,255,255,0.10)` bg, `rgba(255,255,255,0.70)` text, 10px/600, radius 12px, 10px vertical padding

**Settings panel** (⚙ tap):

- Slides in from right via `AnimateSlideX`
- Background: `#1A0A35`
- Sound toggle, vibration toggle: dark cards with `Brand` active state
- Language buttons: secondary style, active = `Brand` bg
- Close: X icon top-right

---

### 5.2 HUD (Chunk 2)

**Background:** `#0F0A1E`

**Left edge progress bar:**

- 4px wide, full screen height, x=7px
- Track `Image`: `rgba(255,255,255,0.06)`
- Fill `Image`: `Brand` (#7C4DFF), anchored to bottom, height driven by `currentHeight / GameConfig.milestoneMax` clamped 0–1
- **Milestone ticks:** one child `Image` per entry in `GameConfig.heightMilestones`. Each tick: 8px wide × 1px tall, `rgba(255,255,255,0.30)`, positioned left-flush within the 4px bar. Y position = `(milestoneHeight / milestoneMax) * barHeight`. On milestone pass: `AnimateFade` alpha 1→0 over 0.4s, reset to 0.30f.

**Top row** (padding-left 18px, padding-right 14px):

- Left: 🪙 (`Gold`) + balance (white, 9px/700)
- Right: ⚙ circle, `rgba(255,255,255,0.07)` bg, 26px

**Center** (padding-left 14px):

- "SCORE" label: `#7C6FA0`, 9px/600, letter-spacing 1px
- Score: 48px/900, white
- Lava pill: `rgba(255,255,255,0.07)` bg, `Danger` text — format: `🌋 Xm`

**Bottom** (padding-left 14px): PAUSE button (secondary style, compact)

**Coin balance:** Updated statically via `EconomyManager.EmberBalanceChanged` as today. No mid-run float animation on the HUD — coin float fires on the fail screen when the run reward is shown (see §8.5).

---

### 5.3 Game Over (Chunk 3)

**Background:** `#1A0F2E`

**Layout (centered):**

1. **Title:** "TOO SLOW", 28px/900, white, letter-spacing 1.5px. **No subtitle line.**
2. **Score:** 56px/900, `Brand` (#7C4DFF). Glow: `Shadow` component, `effectDistance = Vector2.zero`, `effectColor = rgba(124,77,255,0.40)`. Entry: `AnimateScorePop`.
3. **Stats card** (`SurfaceDark`, `BorderDark` 1px, radius 14px):
   - Row 1: "Best" (`TextDim`, 10px) · value (white, 11px/700)
   - Divider: `rgba(255,255,255,0.06)` 1px
   - Row 2: "Coins" (`TextDim`, 10px) · "+X 🪙" (`Gold`, 11px/700)
4. **CONTINUE button** — GradientImage orange, 60px height, full width
5. **Retry** — plain `Text`, `rgba(255,255,255,0.30)`, 11px/600, centered. Invisible `RectTransform` wrapper of 44px height for tap area. Retry wrapper is disabled initially, enabled by coroutine after `GameConfig.failToRetryDelay` seconds.

**Background pulse** (runs once on screen entry via coroutine in `FailScreenController.Show()`):

- Overlay `Image`, full-screen, `raycastTarget = false`
- `AnimateFade`: alpha 0→0.08 over 0.3s, hold 0.5s, 0.08→0 over 0.4s. `Color = Danger`.

---

### 5.4 Shop — Offers (Chunk 4)

**Background:** `#1A0A35`

**Header (14px padding):**

- "SHOP" — 16px/800, white
- Coin balance pill: `rgba(255,214,10,0.15)` bg, `Gold` text, `rgba(255,214,10,0.30)` border 1px

**Tab bar:**

- Active: `Brand` bg, white, 9px/700
- Inactive: `rgba(255,255,255,0.12)` bg, `rgba(255,255,255,0.55)` text, 9px/600
- Tab switch: content area `CanvasGroup.alpha` 1→0 (0.1s) + swap content + 0→1 (0.15s)

**BEST VALUE card:**

- Height ~15% taller than regular cards
- 8px extra top margin above card (gives badge visual room)
- Background: `GradientImage` colorBottom=`#1A0A35` colorTop=`#2D1060`
- Border: `rgba(255,214,10,0.25)` 1px
- Glow child: `Gold` at alpha 0.20f, expand 14px; animated via `AnimateGlowPulse(glowImage, 0.20f, 0.55f, 1.8f)`
- **Badge:** "⭐ BEST VALUE" pill — `Gold` bg, `#08041a` text, 9px/900, absolute -11px from card top center; `AnimateBounce(rt, -5f, 1.3f)`
- Price button: buy button style (GradientImage orange)
- Other items: `CanvasGroup.alpha = 0.85f`

**Regular item cards:**

- `SurfaceDark` bg Image, `BorderDark` border (1px child Image outline), radius 14px, 10px vertical padding
- Layout: icon (30px) · title + subtitle · price/owned right
- Owned: `Owned` color "✓ Owned", 9px/700
- Coin price: `Gold` "🪙 X", 9px/700

---

### 5.5 Skins Grid (Chunk 5)

**Background:** `#1A0A35`

**Grid:** 2 columns, 8px gap

**Card states:**

| State | Background | Border | Glow child | Scale | Opacity | Label |
| --- | --- | --- | --- | --- | --- | --- |
| Selected + owned | `rgba(124,77,255,0.15)` | 2px `Brand` | `Brand` alpha 0.35f, expand 14px | 1.05 | 100% | "✓ Selected" `#A78BFA` |
| Owned, not selected | `SurfaceDark` | `BorderDark` 1px | none | 1.0 | 100% | "Owned" `TextDim` |
| Buyable (coin) | `SurfaceDark` | `BorderDark` 1px | none | 1.0 | 100% | "🪙 X" `Gold` |
| IAP locked | `SurfaceDark2` | `rgba(255,255,255,0.05)` 1px | none | 1.0 | 55% | "🔒 Premium" `rgba(255,255,255,0.45)` italic 8px |

> Locked opacity 55% (up from 40%) — skin art stays readable while clearly unavailable. "🔒 Premium" replaces "IAP" to feel aspirational, not transactional.

**Interactions:**

- Tap: `AnimateButtonPress`
- Select: border Image alpha 0→1 + glow child alpha 0→0.35f (0.2s); scale →1.05 (0.15s)
- Deselect: reverse
- Unlock burst: 5 child `Image` particles (4px circle, `Gold`) spawned at card center — each: scale 0→1 (0.1s), translateY random(-20 to -35px) + translateX random(-15 to 15px) over 0.4s, alpha 1→0 over 0.4s, 0–0.05s random delay stagger, destroyed on complete

---

### 5.6 Leaderboard + Missions Popups (Chunk 6)

Added to `StartScreen.cs`. The **existing `leaderboardPanel`** and **existing missions ribbon** in `StartScreenController` are removed in this chunk and replaced by these bottom sheets. Both pieces of existing UI must be deleted — they must not coexist with the new sheets.

**Shared bottom sheet style:**

- Background: `#1A0A35`, border-radius 20px top corners
- Handle: 32×4px `Image`, `rgba(255,255,255,0.15)`, centered top
- Open: `AnimateSlideY(-sheetHeight, 0, 0.25s)` + `AnimateFade(0, 1, 0.25s)`
- Close: `AnimateSlideY(0, -sheetHeight, 0.20s)` + `AnimateFade(1, 0, 0.20s)` → `SetActive(false)`

**Leaderboard sheet** (🏅 tap):

- Title: "🏅 Top Runs", 14px/800, white
- Subtitle: "Your best: Xm · Rank #N", `TextDim`, 10px
- Entry rows: `SurfaceDark` card, radius 10px — rank + name + score
  - Rank 1: `Gold` text for rank number
  - Own rank: `Brand` 1.5px border + `rgba(124,77,255,0.15)` bg
- Close: secondary button, full width, bottom

**Missions sheet** (📋 MISSIONS tap):

- Title left: "📋 Daily Missions", 14px/800, white
- Title right: countdown `HH:MM:SS`, `TextDim`, 10px
- Max 2 mission cards (silent truncation)
- Each card (`SurfaceDark`, radius 12px):
  - Title (11px/700, white) · progress "X/Y" (9px, `TextDim`)
  - Reward chip: `rgba(255,214,10,0.15)` bg, `Gold` text, "+X 🪙"
  - Progress bar: 4px, `rgba(255,255,255,0.08)` track, `Brand` fill
  - If complete: CLAIM button (`Owned` bg, white, 10px, full width, radius 10px); haptic on claim
- Close: secondary button, full width, bottom

---

## 6. Micro-Interactions Summary

| Trigger | Animation |
| --- | --- |
| START idle | `AnimatePulse(1.0, 1.05, 0.7s)` |
| Any button press | `AnimateButtonPress` (1→0.95→1) |
| Buy button tap | Scale 1→1.05 (0.08s) down; 1.05→0.95→1.0 up |
| Score pop (Game Over) | `AnimateScorePop` (0→1.15→1.0, 0.4s) |
| BEST VALUE badge | `AnimateBounce(rt, -5f, 1.3f)` |
| BEST VALUE glow | `AnimateGlowPulse(glowImage, 0.20f, 0.55f, 1.8f)` |
| Run reward revealed (fail screen) | `AnimateCoinFloat` on coin stat in `FailScreenController.Show()` |
| Screen show/hide | `AnimateFade` (0.20s hide, 0.25s show) |
| Popup open/close | `AnimateSlideY` + `AnimateFade` (0.25s / 0.20s) |
| Skin select | Border + glow Image alpha 0→1, scale →1.05 (0.2s / 0.15s) |
| Skin deselect | Reverse |
| Skin unlock | 5-particle burst (see §5.5) |
| Milestone pass (HUD) | Tick `Image` `AnimateFade` 1→0 over 0.4s, reset to 0.30f |
| Game Over bg | Red overlay pulse, once on entry (see §5.3) |
| Tab switch (Shop) | `CanvasGroup` 1→0→1 fade with content swap |
| Settings panel in/out | `AnimateSlideX` |

---

## 7. Haptics

The existing haptic system is `AudioManager.TriggerVibration()` — a single undifferentiated vibrate call with no intensity parameter. No new haptics API is added.

| Trigger | Haptic |
| --- | --- |
| CONTINUE press | Vibrate |
| Buy button confirm | Vibrate |
| Mission CLAIM | Vibrate |
| Skin unlock | Vibrate |
| All other buttons | None |

---

## 8. Gameplay Retention (Chunk 7)

### 8.1 Config Fields (add to ConfigData.cs)

Add a new `[Header("Milestones")]` section to `GameConfig` in `ConfigData.cs`:

```csharp
[Header("Milestones")]
public int[] heightMilestones = new[] { 25, 50, 100 };
[Min(1f)] public float milestoneMax = 200f;
```

Also add `failToRetryDelay` to the existing `[Header("Run Flow")]` section:

```csharp
[Min(0f)] public float failToRetryDelay = 0.3f;
```

> `failToRetryDelay` is a **new field** — there is currently no retry delay in the codebase. The existing `HandleRetryPressed` path in `FailScreenController` invokes `retryRunAction` immediately. This field controls a new guard added in §8.3.

**`heightMilestones` and `milestoneMax` are the single source of truth** for HUD ticks, milestone toast triggers, and progress bar max. No hardcoded values anywhere else.

### 8.2 Milestone Event

Add to `ScoreManager` in `RunSystems.cs`:

```csharp
// New fields in ScoreManager:
private GameConfig config;
private bool[] milestoneFired; // allocated in Initialize(), length = config.heightMilestones.Length

public event Action<int> OnMilestonePassed;

// Updated Initialize() — takes GameConfig:
public void Initialize(GameConfig gameConfig)
{
    config = gameConfig;
    milestoneFired = new bool[config.heightMilestones.Length];
    // ... existing init code unchanged
}

// Updated Tick() — adds milestone check after existing lines:
public void Tick(float towerHeight, float elapsedTime)
{
    CurrentScore = Mathf.Max(CurrentScore, towerHeight);
    CurrentRunTime = Mathf.Max(0f, elapsedTime);
    BestScore = Mathf.Max(BestScore, CurrentScore);

    for (int i = 0; i < config.heightMilestones.Length; i++)
    {
        if (!milestoneFired[i] && towerHeight >= config.heightMilestones[i])
        {
            milestoneFired[i] = true;
            OnMilestonePassed?.Invoke(config.heightMilestones[i]);
        }
    }
}

// Updated ResetRun() — clears fired flags:
public void ResetRun()
{
    CurrentScore = 0f;
    CurrentRunTime = 0f;
    BestScore = persistedBestScore;
    if (milestoneFired != null)
        Array.Clear(milestoneFired, 0, milestoneFired.Length);
}
```

`TowerMazeBootstrapper` passes `GameConfig` to `scoreManager.Initialize(gameConfig)` — update line 100 of `TowerMazeBootstrapper.cs` from `scoreManager.Initialize()` to `scoreManager.Initialize(gameConfig)`. The local variable is already named `gameConfig` at that scope (visible at line 97).

`UIHudController` subscribes in its `Initialize()`:

```csharp
scoreManager.OnMilestonePassed += HandleMilestonePassed;

private void HandleMilestonePassed(int heightMeters)
{
    rewardToastController.ShowToast($"{heightMeters}m!");
    FlashMilestoneTick(heightMeters); // local coroutine: AnimateFade on matching tick Image
}
```

### 8.3 Fast Retry Guard

`failToRetryDelay` (new `GameConfig` field, §8.1) defaults to `0.3f` seconds.

**Replacing the existing retry button:** The current `FailScreenController` uses a `Button` component (`retryButton`) with `retryButton.interactable` toggled at line 3032. As part of the Chunk 3 redesign (§5.3), the `retryButton` Button component is **replaced** by:

- A plain `Text` component (the visible "Retry" label, `rgba(255,255,255,0.30)`, 11px)
- An invisible `RectTransform` wrapper (`retryWrapper`, 44px height, `Button` component for tap detection)

The `retryButton.interactable` path at line 3032 is **removed** entirely in Chunk 3. The new guard is `SetActive` on `retryWrapper`.

`FailScreenController.Initialize()` receives `failToRetryDelay` as a `float` parameter (do not inject the full `GameConfig` into the UI layer):

```csharp
// FailScreenController.Initialize() signature — add float parameter:
public void Initialize(..., float failToRetryDelay)

// Store as a field:
private float failToRetryDelay;

// On Game Over screen shown:
retryWrapper.SetActive(false);
StartCoroutine(EnableRetryAfterDelay());

private IEnumerator EnableRetryAfterDelay()
{
    yield return new WaitForSeconds(failToRetryDelay);
    retryWrapper.SetActive(true);
}
```

`UIManager.Initialize()` passes `config.failToRetryDelay` when constructing `FailScreenController`. No change to `retryRunAction` invocation logic — only availability timing changes.

### 8.4 Difficulty Curve

`DifficultyProfile` is **already wired into `MazeGenerator`**. No code change needed.

Task: review `DifficultyProfile.asset` band boundaries in the Inspector:

- Band 0 (`minHeight=0`, `maxHeight=20`) should have gentle settings for new players reaching 0–30m
- Confirm no abrupt jump at any band boundary (e.g. `rotationSpeed` should not double between adjacent bands)
- Adjust asset values only if thresholds are too aggressive

### 8.5 Run Reward Coin Animation

There are **no mid-run coin pickups** in the codebase. All coin awards happen at run-end: `RunManager` calculates and grants `pendingRunReward` at fail time (lines 2964, 3125, 3129). The coin float animation therefore fires when the fail screen displays the run reward, not during active gameplay.

`AnimateCoinFloat` is invoked from `FailScreenController.Show(int coinReward)`:

```csharp
// In FailScreenController, near the coin stat display:
if (coinReward > 0)
    StartCoroutine(UIStyle.AnimateCoinFloat(coinStatTransform, font, coinReward, UIColors.Gold));
```

`UIManager.ShowFail()` already passes the coin amount to `FailScreenController` — confirm the parameter is threaded through to `Show()`.

The HUD coin balance display (§5.2 top row) remains **static** — it updates via `EconomyManager.EmberBalanceChanged` as it does today. No mid-run float animation on the HUD.

---

## 9. Engineering Guardrails

1. `UISystems.cs` stays until all 7 new files compile and game runs at full parity
2. `UIStyle.cs` only contains tokens/helpers that are actually used — no speculative additions
3. Animation helpers in `UIStyle.cs` are primitives only — sequencing logic stays inside screen controllers
4. No DOTween dependency — all motion via Unity Coroutines + `Mathf.Lerp`
5. All milestone values read from `GameConfig.heightMilestones` / `milestoneMax` — never hardcoded
6. Each chunk has a compile + visual checkpoint before the next begins
7. No existing serialized references or scene hierarchy are broken
8. `SplashScreenController` moves to `PopupControllers.cs` as a pure file move — no logic or visual changes in Chunk 0
9. Existing leaderboard panel and missions ribbon are removed in Chunk 6 before bottom sheets are added

---

## 10. Chunk Sequence

| Chunk | Scope | Files touched | Checkpoint |
| --- | --- | --- | --- |
| 0 | Foundation — `UIStyle.cs` + 7-file split. `UISystems.cs` kept. | New `UISystems/` directory | 0 errors. Play mode opens main menu. Zero visual change. |
| 1 | Start screen redesign | `StartScreen.cs` | Dark bg, START pulses, secondary buttons reduced. Settings panel works. |
| 2 | HUD redesign | `HudController.cs` | Progress bar visible, milestone ticks present, lava pill shows `🌋 Xm`. |
| 3 | Game Over redesign | `FailScreen.cs` | "TOO SLOW" title, score pop, CONTINUE dominant. Retry available after delay. |
| 4 | Shop offers | `ShopScreen.cs` | BEST VALUE card first + taller + glow. Tabs fade on switch. Others at 85% opacity. |
| 5 | Skins grid | `ShopScreen.cs` | All 4 card states render correctly. Select/deselect animates. Unlock burst fires. |
| 6 | Leaderboard + missions popups | `StartScreen.cs` | Old leaderboard panel + missions ribbon removed. Both bottom sheets slide in/out. |
| 7 | Gameplay retention | `ConfigData.cs`, `RunSystems.cs` | New `GameConfig` fields added. Milestone toasts fire. Retry delay ≤ 0.5s. Difficulty bands reviewed. |

---

## 11. Verification Criteria

1. All chunks produce 0 compile errors
2. No white/light (`#F5F5F7`+) backgrounds visible on any screen
3. START button pulse starts immediately on screen appear (first pulse within first rendered frame after `SetActive(true)`)
4. HUD progress bar and milestone ticks visible in-game; ticks read from `GameConfig.heightMilestones`
5. Game Over title reads "TOO SLOW" — no subtitle line
6. Score pop animation completes in ~0.4s
7. Red background pulse fires once on Game Over entry, not on subsequent shows
8. Shop BEST VALUE card is taller than regular cards, first in list, others at 85% opacity
9. Inactive shop tabs text readable (alpha 0.55)
10. Skins locked state at 55% opacity with "🔒 Premium" label
11. Leaderboard bottom sheet slides up and down smoothly; old leaderboard panel is gone
12. Missions bottom sheet renders up to 2 cards correctly; old missions ribbon is gone
13. Milestone toasts fire at correct heights matching `GameConfig.heightMilestones`
14. Retry tap area inactive for `GameConfig.failToRetryDelay` seconds after fail
15. `EconomyManager.RunCoinEarned` fires only during active run coin awards — not on shop purchases
16. `UISystems.cs` not deleted until criteria 1–15 all pass
