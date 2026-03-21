# TowerMaze Unity Design System Implementation Spec

**Date:** 2026-03-17
**Scope:** Apply the Figma design system (colors, font, flat UI style) to the Unity game's procedural UI code.
**Approach:** Hardcoded Color constants + flat sprite generation + font swap. No JSON parsing at runtime.
**Primary file:** `Assets/Scripts/TowerMaze/Runtime/UISystems.cs` (3,246 lines)
**Secondary file:** `Assets/Scripts/TowerMaze/Runtime/TowerMazeBootstrapper.cs`
**Config file:** `Assets/Scripts/TowerMaze/Runtime/ConfigData.cs` (ThemeDefinition)

---

## 1. UIColors Static Class

Add `static class UIColors` inside `UISystems.cs` (near the top, before `UIManager`). All 22 color constants from `design-tokens.json`. Every hardcoded `new Color(...)` in the file is replaced with a `UIColors.*` reference.

```csharp
static class UIColors
{
    // Primary
    public static readonly Color Primary      = new Color(0.486f, 0.227f, 0.929f, 1f); // #7C3AED
    public static readonly Color PrimaryLight  = new Color(0.655f, 0.545f, 0.980f, 1f); // #A78BFA
    public static readonly Color PrimaryBg     = new Color(0.929f, 0.914f, 0.996f, 1f); // #EDE9FE

    // Surface
    public static readonly Color Surface       = new Color(0.973f, 0.969f, 1.000f, 1f); // #F8F7FF
    public static readonly Color Card          = Color.white;                             // #FFFFFF
    public static readonly Color Divider       = new Color(0.941f, 0.933f, 1.000f, 1f); // #F0EEFF

    // Text
    public static readonly Color TextDark      = new Color(0.067f, 0.067f, 0.067f, 1f); // #111111
    public static readonly Color TextMid       = new Color(0.333f, 0.333f, 0.333f, 1f); // #555555
    public static readonly Color TextDim       = new Color(0.667f, 0.667f, 0.667f, 1f); // #AAAAAA

    // Semantic
    public static readonly Color Success       = new Color(0.063f, 0.725f, 0.506f, 1f); // #10B981
    public static readonly Color SuccessBg     = new Color(0.820f, 0.980f, 0.898f, 1f); // #D1FAE5
    public static readonly Color SuccessText   = new Color(0.020f, 0.588f, 0.412f, 1f); // #059669
    public static readonly Color Danger        = new Color(0.937f, 0.267f, 0.267f, 1f); // #EF4444
    public static readonly Color DangerBg      = new Color(0.996f, 0.886f, 0.886f, 1f); // #FEE2E2
    public static readonly Color DangerText    = new Color(0.863f, 0.149f, 0.149f, 1f); // #DC2626
    public static readonly Color Warning       = new Color(0.961f, 0.620f, 0.043f, 1f); // #F59E0B
    public static readonly Color WarningBg     = new Color(0.996f, 0.953f, 0.784f, 1f); // #FEF3C7
    public static readonly Color WarningText   = new Color(0.851f, 0.467f, 0.024f, 1f); // #D97706

    // HUD
    public static readonly Color HudBg         = new Color(0.059f, 0.039f, 0.118f, 1f); // #0F0A1E
    public static readonly Color HudCard        = new Color(0.110f, 0.078f, 0.200f, 1f); // #1C1433
    public static readonly Color HudBorder      = new Color(0.176f, 0.125f, 0.314f, 1f); // #2D2050
    public static readonly Color HudTextDim     = new Color(0.486f, 0.435f, 0.627f, 1f); // #7C6FA0
}
```

---

## 2. Flat Sprite Generator

### 2a. New method: `CreateFlatSprite`

Add to `UIManager` static methods section (near `CreateRoundedSprite`):

```csharp
static Sprite CreateFlatSprite(string name, int size, int radius, Color color)
```

- Draws a solid-color rounded rectangle into a `Texture2D`
- Radius clamped to `size / 2` (supports pill shape when radius = 999)
- Returns a `Sprite` with 9-slice border set to `(size/4, size/4, size/4, size/4)`
- Result cached in `themedSpriteCache` (the existing `static Dictionary<string, Sprite>` on `UIManager`; reuse `LoadGeneratedThemeSprite` wrapper)

### 2b. Update `GetThemeButtonSprite(Color color)`

Replace the current HSV-detection + Resource-loading logic with direct `CreateFlatSprite` calls:

- Any color → `CreateFlatSprite("btn_flat_" + ColorUtility.ToHtmlStringRGB(color), 64, 32, color)`
- The caller (button controller) already passes the intended fill color, so no hue detection is needed

### 2c. Update `GetThemePanelSprite(Color color)`

Same replacement:

- Any color → `CreateFlatSprite("panel_flat_" + ColorUtility.ToHtmlStringRGB(color), 64, 18, color)`
- Remove `IsYellowThemeColor()`, `IsLightPanelColor()`, `IsDarkPanelColor()` — no longer needed

### 2d. Retire `CreateGlossyPanelSprite`

Add a comment above the method — do not delete (safe rollback path). `[Obsolete]` is not useful on a `private static` method:

```csharp
// RETIRED: replaced by CreateFlatSprite. Kept for rollback. Do not add new callers.
private static Sprite CreateGlossyPanelSprite(...)
```

No callers remain after 2b/2c are updated.

### 2e. Simplify `StyleToyText` and `StyleButtonLabel`

- Remove `Outline` component addition — Outfit doesn't need stroke
- Remove `Shadow` component addition from text — clean sans-serif renders without it
- Keep `Shadow` on card/button frames (UI Image components) — soft drop shadow stays

---

## 3. Font Swap

### 3a. Asset

Download `Outfit-Bold.ttf` from Google Fonts. Place at:
```
Assets/Resources/TowerMaze/UITheme/Outfit-Bold.ttf
```

### 3b. Load path update

Two files to update:

**`TowerMazeBootstrapper.cs`:**
```csharp
// Before:
var font = Resources.Load<Font>("TowerMaze/UITheme/Kenney Future");
// After:
var font = Resources.Load<Font>("TowerMaze/UITheme/Outfit-Bold");
```

**`UISystems.cs`** — `UIManager.Initialize()` (line ~79), field `runtimeFont` — this is the primary font used by every controller, not a fallback:
```csharp
// Before:
runtimeFont = Resources.Load<Font>("TowerMaze/UITheme/Kenney Future");
// After:
runtimeFont = Resources.Load<Font>("TowerMaze/UITheme/Outfit-Bold");
```

`Kenney Future.ttf` stays in the folder — no deletion.

---

## 4. Screen-by-Screen Color Updates

Replace all hardcoded `new Color(...)` values in each controller with `UIColors.*`:

### `StartScreenController`
| Element | Color |
|---|---|
| Screen background | `UIColors.Surface` |
| Best score card | `UIColors.Card` |
| Mission cards | `UIColors.Card` |
| Card accent border | `UIColors.Primary` |
| Primary button (OYNA) | `UIColors.Primary` |
| Secondary buttons | `UIColors.Card` + `UIColors.Primary` border |
| Score value text | `UIColors.TextDark` |
| Body text | `UIColors.TextMid` |
| Label text | `UIColors.TextDim` |
| Primary label text | `UIColors.Primary` |

### `FailScreenController`
| Element | Color |
|---|---|
| Screen background | `UIColors.Surface` |
| YENİDEN DENE button | `UIColors.Danger` |
| DEVAM ET button | `UIColors.Success` |
| Ana Menü button | transparent / `UIColors.TextDim` |
| Score text | `UIColors.TextDark` |
| Score unit ("m") | `UIColors.Primary` |
| Reward text | `UIColors.TextDark` |
| Stat cards | `UIColors.Card` |

### `UIHudController`
| Element | Color |
|---|---|
| Screen background | `UIColors.HudBg` |
| Score/best cards | `UIColors.HudCard` + `UIColors.HudBorder` stroke |
| Card label text | `UIColors.HudTextDim` |
| Score value | `Color.white` |
| Best value / zone | `UIColors.PrimaryLight` |
| Lava bar track | `UIColors.HudCard` |
| Lava bar fill | `UIColors.Danger` (solid — legacy UI Image does not support horizontal gradients natively) |
| New Best badge | `UIColors.SuccessBg` / `UIColors.SuccessText` |

### `ShopScreenController`
| Element | Color |
|---|---|
| Overlay | `Color.black` at 70% alpha |
| Modal background | `UIColors.Card` |
| Active tab | `UIColors.Primary` |
| Inactive tab | transparent / `UIColors.TextDim` |
| Item card | `UIColors.Card` |
| Owned badge | `UIColors.SuccessBg` / `UIColors.SuccessText` |
| Price button | `UIColors.Primary` |

### `CountdownController`
| Element | Color |
|---|---|
| Background | `UIColors.HudBg` |
| Count number | `Color.white` |
| GO! text | `UIColors.PrimaryLight` |

### `SplashScreenController`
| Element | Color |
|---|---|
| Background | `UIColors.HudBg` |
| TOWER text | `Color.white` |
| MAZE text | `UIColors.PrimaryLight` |
| Spinner ring | `UIColors.PrimaryLight` |

### `IAPUpsellController`
| Element | Color |
|---|---|
| Backdrop | `Color.black` at 70% alpha |
| Modal | `UIColors.Card` |
| Title text | `UIColors.TextDark` |
| Description | `UIColors.TextMid` |
| Price button | `UIColors.Primary` |

### `RewardToastController`
| Element | Color |
|---|---|
| Toast background | `UIColors.Card` |
| Title text | `UIColors.TextDark` |
| Subtitle text | `UIColors.TextDim` |

### `LeaderboardPanelController`
| Element | Color |
|---|---|
| Panel background | `UIColors.Card` |
| Title text | `UIColors.TextDark` |
| Score text | `UIColors.Primary` |
| Player name | `UIColors.TextDark` |
| Rank label | `UIColors.TextDim` |
| Current player row | `UIColors.PrimaryBg` |

### `RushWarningController`
| Element | Color |
|---|---|
| Overlay tint | `UIColors.Warning` at 20% alpha |
| Card | `UIColors.Card` |
| Warning title | `UIColors.Warning` |
| Body text | `UIColors.TextMid` |

### `ControlFlipController`
| Element | Color |
|---|---|
| Overlay tint | `UIColors.PrimaryBg` at 60% alpha |
| Card | `UIColors.Card` |
| Title | `UIColors.Primary` |
| Body | `UIColors.TextDim` |
| GOT IT button | `UIColors.Primary` |

---

## 5. ThemeDefinition Default Color

Two steps required — the C# default alone is not enough because `VolcanicTheme.asset` has `accentColor` serialized and overrides the C# default at runtime:

**Step 1 — Update C# default in `ConfigData.cs`** (affects newly created ThemeDefinition assets):
```csharp
// Before (golden yellow):
public Color accentColor = new Color(1f, 0.69f, 0.19f);
// After (deep purple):
public Color accentColor = new Color(0.486f, 0.227f, 0.929f); // #7C3AED
```

**Step 2 — Update `VolcanicTheme.asset` in the Unity Inspector:**
Open `Assets/TowerMaze/Config/VolcanicTheme.asset` → select it → in the Inspector, set `Accent Color` to `(R: 0.486, G: 0.227, B: 0.929, A: 1)`. This is required because the serialized asset value overrides the C# default.

Note: `accentColor` is also read by `EnvironmentBackdropController` for 3D scene tinting. Changing it to purple will tint the environment. If this is undesirable, skip Step 2 and instead update each controller that reads `theme.accentColor` (e.g. `CountdownController.numberColor` at line ~1094) to use `UIColors.Primary` directly instead of `theme.accentColor`.

---

## 6. What Is NOT Changed

- UI layout dimensions, padding, font sizes — unchanged
- `ThemeDefinition` environment colors (sky, fog, lava, tower) — unchanged. `accentColor` is changed (see Section 5); all other environment colors stay.
- Hero colors (`heroPrimary`, `heroSecondary`, `heroAccent`) — unchanged
- `CreateRoundedSprite()` — kept as-is, used internally by `CreateFlatSprite`
- `LoadThemeSprite()` / `LoadGeneratedThemeSprite()` caches — kept as-is
- Existing sprite PNG files in UITheme — kept, no deletion
- `IsYellowThemeColor()`, `IsLightPanelColor()`, `IsDarkPanelColor()` — kept but unused (safe to delete later)

---

## 7. Success Criteria

- Game runs without null reference errors on font/sprite load
- All 11 screens display the purple accent, light surface backgrounds, and dark HUD
- No yellow/golden colors remain in UI (except environment: lava, coins)
- Outfit font renders correctly on all text elements
- Cards display flat white with soft shadow (no gloss gradient)
- Buttons display flat colored with soft shadow (no gloss gradient)
