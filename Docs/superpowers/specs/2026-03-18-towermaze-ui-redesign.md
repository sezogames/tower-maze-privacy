# TowerMaze UI Redesign — Design Spec
**Date:** 2026-03-18
**Status:** Approved
**Scope:** Full UI redesign + monetization polish

---

## Context

The existing UI (UISystems.cs, 3341 lines) is functional but needs a complete visual refresh focused on:
- Minimal, premium aesthetic
- Monetization conversion (shop, ads)
- Player engagement (juice, micro-interactions)
- Consistency across all screens

All UI is **code-generated at runtime** — no prefabs. The redesign modifies the existing controller classes and is organized into separate files.

---

## Decisions Summary

| Topic | Decision |
|---|---|
| Implementation | Split UISystems.cs into 7 files (standalone classes, not partial) |
| Font | Keep Outfit-Bold (already in Resources) |
| Animation system | Unity Coroutines only — no DOTween dependency |
| Main Menu layout | Centered stack |
| Main Menu bg | Deep Purple #2D1B69 |
| Game Over title | "YOU MELTED" (see localisation note §4.3) |
| Game Over bg | Dark #1A0F2E |
| Game Over layout | Score + mini stats card + CONTINUE + Retry |
| Shop bg | Dark Purple #1A0A35 |
| Shop layout | Featured BEST VALUE top + list below |
| Skins | 2-column grid |
| HUD addition | Height progress bar with milestone markers |
| Currency display | "Ember" renamed to "Coin" in all UI text (see §0) |

---

## 0. Currency Rename: Ember → Coin

**Decision:** All UI text and icons will display "Coin" / 🪙 instead of "Ember". This is a display-layer rename only.

**Scope of change:**
- UI text labels: `emberText` → coin display
- Icon: replace `ember_icon_hq` sprite with coin icon (or relabel asset)
- `EconomyManager.EmberBalance` property name stays unchanged in code
- `PlayerPrefs` key `"TowerMaze.EmberBalance"` stays unchanged (no save-file migration needed)
- Localisation strings that say "Ember" → update to "Coin" / "Para" / "Moneda" in TR/EN/ES

This is a display change only. No economic logic changes.

---

## 1. Color System

### Rule: Color = Meaning
- **Purple = Brand / UI identity** — navigation, selected state, progress
- **Orange = Action / Purchase** — START, CONTINUE, buy buttons
- **Gold = Value / Reward** — coins, BEST VALUE badge, earned items

### Palette

| Token | Hex | Usage |
|---|---|---|
| `brand` | `#7C4DFF` | Identity, tabs, selected, progress bar |
| `menuBg` | `#2D1B69` | Main menu background |
| `shopBg` | `#1A0A35` | Shop + skins background |
| `hudBg` | `#0F0A1E` | In-game HUD background |
| `failBg` | `#1A0F2E` | Game over background |
| `action` | `#FF9F0A` | START, CONTINUE, buy buttons |
| `actionLight` | `#FFB340` | Buy button gradient end |
| `gold` | `#FFD60A` | Coins, BEST VALUE badge |
| `owned` | `#10B981` | Owned badge, claim button |
| `danger` | `#EF4444` | Lava distance indicator |
| `surfaceDark` | `rgba(255,255,255,0.07)` | Cards on dark bg |
| `surfaceDark2` | `rgba(255,255,255,0.04)` | Locked/secondary cards |
| `borderDark` | `rgba(255,255,255,0.06)` | Card borders on dark bg |
| `textPrimary` | `#FFFFFF` | Primary text on dark |
| `textDim` | `rgba(255,255,255,0.35)` | Secondary text on dark |
| `textFaint` | `rgba(255,255,255,0.30)` | Captions, labels |

### Rules
- Max 2 colors + 1 accent per screen
- No gradients except: buy button (`action` → `actionLight`) and BEST VALUE card (very subtle `#2D1060` → `shopBg`)
- All screens use dark backgrounds

---

## 2. Typography

- **Font:** Outfit-Bold (loaded from `Resources/TowerMaze/UITheme/Outfit-Bold.ttf`)
- **Title (screen):** 28–32px, weight 900
- **Button:** 13–15px, weight 700–800
- **Body / secondary:** 10–12px, weight 600
- **Label / caption:** 9–10px, weight 600, letter-spacing 1–2px
- Max 2 font sizes per screen section

---

## 3. Button System

### Primary — Brand Action
```
Background: #7C4DFF (solid)
Text: #FFFFFF, weight 700, 13px
Border radius: 16px
Height: 48px
Shadow: rgba(124,77,255,0.4) 0 4px 16px
Press: scale 1→0.95 in 0.08s, →1 in 0.12s, easeInOut
Haptic: light impact
```

### Secondary (dark screens)
```
Background: rgba(255,255,255,0.10)
Text: rgba(255,255,255,0.70), weight 600, 11–12px
Border radius: 14px
Height: 44px
No shadow
Press: scale 1→0.95→1, same timing as Primary
Haptic: none
```

### START / CONTINUE — Dominant Action
```
Background: linear gradient #FF9F0A → #FFB340 (left to right)
Text: #FFFFFF, weight 800
  START: 15px, height 56px
  CONTINUE: 15px, height 60px
Border radius: 16px
Shadow: rgba(255,159,10,0.55) 0 5px 22px
Idle animation: coroutine pulse (see §5)
Press: scale 1→0.95 in 0.08s, →1 in 0.12s
Haptic: medium impact on CONTINUE, light on START press
```

### Buy Button (Shop)
```
Background: linear gradient #FF9F0A → #FFB340
Text: #FFFFFF, weight 700, 11px
Border radius: 10px
Padding: 7px 12px
Min tap area: 44×44px (add transparent padding if needed)
Shadow: rgba(255,159,10,0.50) 0 3px 14px
Tap feedback: scale 1→1.05→1, 0.15s ease (expand then back)
Press: scale 1→0.95 in 0.08s, →1 in 0.12s (overrides tap on full press)
Haptic: light impact
```
> Note: "tap feedback" (1→1.05) fires on pointer down; "press" (1→0.95) fires on pointer up/confirm. They do not conflict — tap expand is a hover preview, press is the confirm squeeze.

---

## 4. Screen Designs

### 4.1 Main Menu
**Background:** `#2D1B69`

**Layout (top to bottom, centered, 16px horizontal padding):**
1. **Top bar** (absolute, top 16px):
   - Left: `🏆 103m` — `textFaint`, 10px/600
   - Right: `🏅` leaderboard icon + `⚙` settings icon — `rgba(255,255,255,0.10)` bg, 26×26px circles, 7px gap
2. **Logo:** "TOWER MAZE", 28px/900, white, text-align center, margin-top 16px from top bar
3. **Spacer:** 16px
4. **START button** (orange gradient, full width, pulsing)
5. **Best score caption:** "Best: 103m", `textFaint`, 10px, centered
6. **Secondary row** (65% opacity group):
   - [SHOP] [📋 MISSIONS] — equal width, `rgba(255,255,255,0.10)` bg, `rgba(255,255,255,0.70)` text, 10px/600, 11px padding vertical, radius 12px

**Settings panel** (existing, redesigned):
- Slides in from right (translateX, 0.25s ease) on ⚙ tap
- Background: `#1A0A35` panel overlapping main menu
- Sound toggle, vibration toggle: dark cards with `brand` color active state
- Language buttons: secondary style, active = `brand` bg
- Close: X icon top-right

**Leaderboard popup** (on 🏅 tap):
- Bottom sheet, `#1A0A35` bg, border-radius 20px top
- Handle bar: 32×4px, `rgba(255,255,255,0.15)`, centered, margin-bottom 12px
- Title: "🏅 Top Runs", 14px/800, white
- Subtitle: "Sen: Xm · #N. sıra", `textDim`, 10px
- List rows: `surfaceDark` cards, 10px radius, rank + name + score
  - Rank 1: `gold` text for rank number
  - Own rank: `brand` border 1.5px + `rgba(124,77,255,0.15)` bg
- Close button: secondary style, full width, bottom
- Open animation: translateY +100%→0 + opacity 0→1, 0.25s ease

**Missions popup** (on 📋 tap):
- Same bottom-sheet style as leaderboard
- Title: "📋 Daily Missions" left + countdown `HH:MM:SS` right (`textDim`, 10px)
- Max 2 missions displayed (if data has more, show first 2 only — silent truncation)
- Each mission card (`surfaceDark`):
  - Title (11px/700, white) + progress `"X/Y completed"` (9px, `textDim`)
  - Reward chip: `rgba(255,214,10,0.15)` bg, `gold` text, "+X 🪙" right-aligned
  - Progress bar: 4px, `rgba(255,255,255,0.08)` track, `brand` fill, radius 4px
  - If complete: green CLAIM button (`owned` bg, white text, 10px, full width, radius 10px)
- Open animation: same as leaderboard

### 4.2 HUD (In-Game)
**Background:** `#0F0A1E`

**Layout:**
- **Left edge progress bar:** 4px wide, full screen height, positioned x=7px
  - Track: `rgba(255,255,255,0.06)`
  - Fill: `brand` (#7C4DFF), grows from bottom as score increases
  - Max value: `GameConfig.heightProgressMax` (new config field, default 200m)
    - If no config field exists, use hardcoded 200m; bar clamps at 100%
  - **Milestone ticks:** at 25m, 50m, 100m — 1px horizontal line, 8px wide, `rgba(255,255,255,0.30)`, extends left of bar
- **Top row** (padding-left 18px, padding-right 14px):
  - Left: 🪙 icon (`gold`) + balance value (white, 9px/700)
  - Right: ⚙ settings icon (`rgba(255,255,255,0.07)` bg, 26px circle)
- **Center** (padding-left 14px):
  - "SCORE" label: `#7C6FA0`, 9px/600, letter-spacing 1px
  - Score: 48px/900, white
  - Lava distance pill: `rgba(255,255,255,0.07)` bg, `danger` text, "🌋 +Xm"
- **Bottom** (padding-left 14px): PAUSE button (secondary style, compact)
- **Coin gain float:** "+X 🪙" positioned near coin balance, `gold` color (see §5)

> Note: HUD score is 48px; Game Over score is 56px. This is intentional — Game Over emphasises the number as the emotional climax of the run.

### 4.3 Game Over — "YOU MELTED"
**Background:** `#1A0F2E`

**Localisation:**
| Locale | String |
|---|---|
| EN | YOU MELTED |
| TR | ERİDİN |
| ES | TE DERRETISTE |

**Layout (centered, all items):**
1. **Title:** "YOU MELTED", 10px/800, `textFaint`, letter-spacing 2.5px
2. **Score:** 56px/900, `brand` (#7C4DFF), text-shadow `rgba(124,77,255,0.40)` 0 0 30px
   - **Entry animation** (coroutine): scale 0→1.15 in 0.28s (ease-out), →1.0 in 0.12s (ease-in). Total 0.4s. No DOTween needed.
3. **Stats card** (`surfaceDark`, `borderDark` border 1px, radius 14px):
   - Row 1: "Best" (`textDim`, 10px) · value (white, 11px/700)
   - Divider: `rgba(255,255,255,0.06)` 1px
   - Row 2: "Coins" (`textDim`, 10px) · "+X 🪙" (`gold`, 11px/700)
4. **CONTINUE button** (orange gradient, 60px height, full width, dominant)
5. **Retry:** plain text, `rgba(255,255,255,0.30)`, 11px/600, centered
   - **Tap area:** invisible RectTransform of 44px height wrapping the text (do not reduce visual size)

**Background entry effect** (coroutine):
- On screen show: overlay `rgba(239,68,68,0.08)` fades in over 0.3s, holds 0.5s, fades out 0.4s. Runs once.

### 4.4 Shop
**Background:** `#1A0A35`

**Header (14px padding):**
- "SHOP" — 16px/800, white
- Coin balance pill: `rgba(255,214,10,0.15)` bg, `gold` text, `rgba(255,214,10,0.30)` border 1px

**Tab bar:** 4px gap between tabs
- Active: `brand` bg, white text, 9px/700
- Inactive: `rgba(255,255,255,0.08)` bg, `rgba(255,255,255,0.40)` text, 9px/600
- **Tab switch animation:** content area fades out (opacity →0, 0.1s) then fades in (→1, 0.15s) with new content

**BEST VALUE card (top):**
- Size: ~10% taller than regular cards (regular ≈ 54px height → BEST VALUE ≈ 60px)
- Background: gradient `#2D1060 → #1A0A35`, border `rgba(255,214,10,0.25)` 1px
- Glow animation: `box-shadow rgba(255,214,10,0.30)` → `rgba(255,214,10,0.70)`, 1.8s ease loop
- **Badge:** "⭐ BEST VALUE" pill, `gold` bg, `#08041a` text (dark), 9px/900, centered top (absolute, -11px from card top), bounce animation (§5)
- Price button: orange gradient, 12px/700, padding 7px 12px, radius 10px
- Other items: 85% opacity (`alpha = 0.85f` on CanvasGroup or Color.a)

**Regular item cards:** `surfaceDark`, `borderDark` border, radius 14px, 10px vertical padding
- Layout: icon (30px) · title + subtitle · price/owned right
- Owned: `owned` color "✓ Owned", 9px/700
- Coin price: `gold` "🪙 X", 9px/700
- IAP locked: 40% opacity, 🔒 icon replacing item image

### 4.5 Skins / Balls Grid
**Background:** `#1A0A35`

**Header:** same as Shop header

**Grid:** 2 columns, 8px gap

**Card states:**

| State | Background | Border | Glow | Scale | Opacity |
|---|---|---|---|---|---|
| Selected + owned | `rgba(124,77,255,0.15)` | 2px `brand` | `rgba(124,77,255,0.35)` 0 0 16px | 1.05 | 100% |
| Owned, not selected | `surfaceDark` | `borderDark` 1px | none | 1.0 | 100% |
| Buyable (coin) | `surfaceDark` | `borderDark` 1px | none | 1.0 | 100% |
| IAP locked | `surfaceDark2` | `rgba(255,255,255,0.05)` 1px | none | 1.0 | 40% |

**Card contents:** icon (46×46px, radius 12px) · name (9px/700, white) · status (owned/price)

**Interactions:**
- Tap: scale 1→0.95→1, 0.1s
- Select: border + glow fade in, 0.2s ease; scale →1.05, 0.15s
- Deselect: border + glow fade out, 0.2s; scale →1.0
- Unlock: radial particle burst from card center, 0.4s (4–6 small circle particles, `gold` color, scale 0→1→0, translateY -20px)

---

## 5. Micro-Interactions (Coroutine Implementations)

All animations implemented as Unity Coroutines using `Mathf.Lerp` / `AnimationCurve`. No external animation library required.

### Pulse (START idle)
```
Loop forever:
  t=0→0.7s: scale 1.0 → 1.05 (ease-in-out)
  t=0.7→1.4s: scale 1.05 → 1.0 (ease-in-out)
Shadow: rgba(255,159,10,0.45) at scale 1.0 → rgba(255,159,10,0.75) at scale 1.05
```

### Button Press (universal)
```
On pointer down:  scale → 0.95, duration 0.08s, ease-in
On pointer up:    scale → 1.00, duration 0.12s, ease-out
```

### Buy Button Tap Expand
```
On pointer down:  scale → 1.05, duration 0.08s, ease-out
On pointer up:    scale → 0.95, duration 0.06s, ease-in → scale → 1.00, 0.10s ease-out
```

### Score Pop (Game Over entry)
```
t=0.00→0.28s: scale 0.0 → 1.15, ease-out (decelerate)
t=0.28→0.40s: scale 1.15 → 1.00, ease-in (snap back)
```

### BEST VALUE Badge Bounce
```
Loop forever:
  t=0→0.45s: translateY 0 → -5px, ease-out
  t=0.45→1.30s: translateY -5px → 0, ease-in-out
```

### BEST VALUE Glow Pulse
```
Loop forever:
  t=0→0.9s: box-shadow spread rgba(255,214,10,0.30) → rgba(255,214,10,0.70)
  t=0.9→1.8s: reverse
```

### Coin Gain Float
```
Spawn text "+X 🪙" at coin icon position, gold color, 11px/800
t=0.0→0.3s: translateY 0 → -22px, scale 1.0 → 1.2, opacity 1.0 → 1.0
t=0.3→1.2s: translateY -22px → -44px, scale 1.2 → 1.0, opacity 1.0 → 0.0
Destroy on complete.
```

### Screen Transition (fade)
```
On hide: CanvasGroup.alpha 1 → 0, 0.20s ease-in
On show: CanvasGroup.alpha 0 → 1, 0.25s ease-out
```

### Popup Open (bottom sheet)
```
Starting state: anchoredPositionY = -sheetHeight, alpha = 0
t=0→0.25s: anchoredPositionY → 0, alpha → 1, ease-out
```

### Popup Close
```
t=0→0.20s: anchoredPositionY → -sheetHeight, alpha → 0, ease-in
Destroy/hide on complete.
```

### Skin Select
```
Border color alpha: 0 → 1, 0.20s ease
Glow: 0 → 16px spread, 0.20s ease
Scale: 1.0 → 1.05, 0.15s ease-out
```

### Skin Unlock Burst
```
Spawn 5 circle particles (4px, gold) at card center
Each particle: scale 0→1 in 0.1s, translateY random(-20 to -35px) + translateX random(-15 to 15px) over 0.4s, opacity 1→0 over 0.4s
Stagger: 0–0.05s random delay per particle
```

### Game Over Background Pulse
```
Overlay: Image, rgba(239,68,68,0.0), covers screen, pointer-pass-through
t=0.0→0.30s: alpha 0 → 0.08
t=0.30→0.80s: hold
t=0.80→1.20s: alpha 0.08 → 0.0
Run once on screen entry.
```

### Tab Switch (Shop)
```
t=0→0.10s: content CanvasGroup.alpha 1 → 0
Swap content
t=0.10→0.25s: content CanvasGroup.alpha 0 → 1
```

---

## 6. Haptic Reference

| Trigger | Intensity |
|---|---|
| START press | Light |
| CONTINUE press | Medium |
| Buy button confirm | Light |
| Mission CLAIM | Light |
| Skin select | Light |
| Skin unlock | Medium |
| All other buttons | None |

---

## 7. File Structure

Refactor `UISystems.cs` into **standalone files** (not partial classes). Each controller class is un-sealed to allow future extension but remains a standalone class. `UIManager` references controller instances as before.

```
Assets/Scripts/TowerMaze/Runtime/UISystems/
  UIStyle.cs          — UIColors (new tokens), UIFonts, UIMetrics,
                        static helpers: CreateImage, CreateButton,
                        CreateCard, CreateDarkCard, CreateBuyButton,
                        CreateGoldPill, CreateText, Stretch, BindButton,
                        StyleButtonLabel, AnimatePulse, AnimateBounce,
                        AnimateCoinFloat, AnimateScorePop, AnimateButtonPress
  UIManager.cs        — Canvas setup, screen routing, Initialize(), ShowXxx()
  StartScreen.cs      — StartScreenController (main menu, settings panel,
                        leaderboard popup, missions popup)
  FailScreen.cs       — FailScreenController (You Melted)
  ShopScreen.cs       — ShopScreenController (coins/balls/towers + skins grid)
  HudController.cs    — UIHudController (in-game HUD + progress bar)
  PopupControllers.cs — CountdownController, PauseScreenController,
                        RushWarningController, ControlFlipController,
                        RewardToastController, IAPUpsellController,
                        SplashScreenController
```

`UIColors` static class in `UIStyle.cs` replaces current `UIColors` with the new token system. All hex values and alpha values are `static readonly Color` fields, not inline hex strings.

---

## 8. Verification

**Pass criteria:**

1. Main menu opens with `#2D1B69` bg and pulsing orange START button visible within 1 frame of screen show.
2. Tap 🏅 → leaderboard bottom sheet slides up smoothly (no stutter at 60fps on mid-range Android).
3. Tap 📋 MISSIONS → missions popup renders correctly with max 2 items.
4. Start game → HUD shows 4px progress bar on left edge with 3 milestone ticks visible.
5. Pick up coins → "+X 🪙" float animation plays without spawning persistent objects (no leak).
6. Die → "YOU MELTED" screen: score pop animation completes in ~0.4s, background red pulse fires once.
7. Open Shop → BEST VALUE card is visually largest item, badge bouncing, glow pulsing; other items visibly secondary (85% opacity).
8. Tap a buy button → scale press response in < 1 frame; haptic fires on device.
9. Open Skins grid → selected item has glow + 1.05 scale; locked items at 40% opacity.
10. No white or light (`#F5F5F7`) backgrounds visible on any screen after redesign.
11. Tab switch in Shop shows fade transition, not instant swap.
12. Settings gear → settings panel slides in; language/sound toggles styled with dark cards.
