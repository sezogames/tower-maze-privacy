# TowerMaze Unity Design System Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Apply the TowerMaze Figma design system (purple accent, flat cards, Outfit font) to the Unity game's procedural UI code.

**Architecture:** Add a `UIColors` static class as the single color source-of-truth, add `CreateFlatSprite` to replace glossy sprite generation, swap the font load path to Outfit-Bold, then update all 11 screen controllers to use `UIColors.*`.

**Tech Stack:** Unity (C#), Legacy UnityEngine.UI, Resources.Load, procedural UI (no prefabs)

**Spec:** `docs/superpowers/specs/2026-03-17-unity-design-system-impl.md`

---

## Pre-requisite: Drop Outfit-Bold.ttf (manual step)

Before starting any tasks, download **Outfit Bold** from Google Fonts:

1. Visit fonts.google.com/specimen/Outfit → Download family → extract ZIP
2. Find `static/Outfit-Bold.ttf` inside the extracted folder
3. Copy to: `Assets/Resources/TowerMaze/UITheme/Outfit-Bold.ttf`
4. In Unity Editor: right-click the file → **Reimport**. Verify it appears in the Project panel.

> **If you skip this, Task 5 will cause a null font and all text will be invisible at runtime.**

---

## Chunk 1: Foundation — UIColors + CreateFlatSprite

### Task 1: Add UIColors static class

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs` (insert before line 20, `public sealed class UIManager`)

- [ ] **Step 1: Find the insert point**

  Open `UISystems.cs`. The file structure near the top is:
  - Line ~12: `namespace TowerMaze {`
  - Lines ~13–18: `enum ShopCatalogType { ... }` (closing `}` at line 18)
  - Line 20: `public sealed class UIManager : MonoBehaviour`

  Insert `UIColors` at **namespace scope**, between line 18 (closing `}` of `ShopCatalogType`) and line 20 (`UIManager`). Do NOT insert inside `UIManager`'s opening brace.

- [ ] **Step 2: Insert UIColors class**

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

- [ ] **Step 3: Verify compilation**

  Save the file. Check the Unity Console for errors. Expected: no compile errors.

- [ ] **Step 4: Commit**

  ```bash
  git add Assets/Scripts/TowerMaze/Runtime/UISystems.cs
  git commit -m "feat: add UIColors static class with design token constants"
  ```

---

### Task 2: Add CreateFlatSprite method

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs` (insert after `CreateRoundedSprite` closing brace, line ~959)

- [ ] **Step 1: Find the insert point**

  In `UISystems.cs`, find `private static Sprite CreateRoundedSprite(` at line 917. Locate its closing `}`. Insert `CreateFlatSprite` immediately after it.

- [ ] **Step 2: Insert CreateFlatSprite**

  ```csharp
  private static Sprite CreateFlatSprite(string name, int size, int radius, Color color)
  {
      if (themedSpriteCache.TryGetValue(name, out var cached)) return cached;

      int r = Mathf.Min(radius, size / 2);
      var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
      tex.filterMode = FilterMode.Bilinear;
      var pixels = new Color32[size * size];
      // Uses sub-pixel SDF (same approach as CreateRoundedSprite) for anti-aliased corners.
      for (int y = 0; y < size; y++)
      {
          for (int x = 0; x < size; x++)
          {
              bool inCornerRegion = (x < r || x >= size - r) && (y < r || y >= size - r);
              float alpha;
              if (inCornerRegion)
              {
                  float cx = (x < r) ? r : size - r - 1;
                  float cy = (y < r) ? r : size - r - 1;
                  float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                  alpha = Mathf.Clamp01(r + 0.5f - dist); // sub-pixel anti-alias
              }
              else
              {
                  alpha = 1f;
              }
              pixels[y * size + x] = new Color32(
                  (byte)(color.r * 255),
                  (byte)(color.g * 255),
                  (byte)(color.b * 255),
                  (byte)(color.a * alpha * 255)
              );
          }
      }

      tex.SetPixels32(pixels);
      tex.Apply();

      int b = size / 4;
      var sprite = Sprite.Create(
          tex,
          new Rect(0, 0, size, size),
          new Vector2(0.5f, 0.5f),
          100f, 0,
          SpriteMeshType.FullRect,
          new Vector4(b, b, b, b)
      );
      sprite.name = name;
      themedSpriteCache[name] = sprite;
      return sprite;
  }
  ```

- [ ] **Step 3: Verify compilation**

  Save. Check Unity Console. Expected: no errors.

- [ ] **Step 4: Commit**

  ```bash
  git add Assets/Scripts/TowerMaze/Runtime/UISystems.cs
  git commit -m "feat: add CreateFlatSprite for flat rounded UI elements"
  ```

---

## Chunk 2: Sprite System Update

### Task 3: Replace GetThemeButtonSprite and GetThemePanelSprite

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs` (lines 745–815)

- [ ] **Step 1: Replace GetThemeButtonSprite body**

  Find `private static Sprite GetThemeButtonSprite(Color targetColor)` at line 745. Replace the **entire method body** (everything between the braces) with:

  ```csharp
  string key = "btn_flat_" + ColorUtility.ToHtmlStringRGB(targetColor);
  return CreateFlatSprite(key, 64, 32, targetColor);
  ```

- [ ] **Step 2: Replace GetThemePanelSprite body**

  Find `private static Sprite GetThemePanelSprite(Color targetColor)` at line 766. Replace the **entire method body** with:

  ```csharp
  string key = "panel_flat_" + ColorUtility.ToHtmlStringRGB(targetColor);
  return CreateFlatSprite(key, 64, 18, targetColor);
  ```

- [ ] **Step 3: Verify compilation**

  Save. Check Unity Console. Expected: no errors.

- [ ] **Step 4: Commit**

  ```bash
  git add Assets/Scripts/TowerMaze/Runtime/UISystems.cs
  git commit -m "feat: replace HSV-based sprite selection with flat sprite generator"
  ```

---

### Task 4: Retire CreateGlossyPanelSprite + simplify text styling

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs` (lines 960, 1033, 1061)

- [ ] **Step 1: Add retirement comment to CreateGlossyPanelSprite**

  Find `private static Sprite CreateGlossyPanelSprite(` at line 960. Add directly above it:

  ```csharp
  // RETIRED: replaced by CreateFlatSprite. Kept for rollback. Do not add new callers.
  ```

- [ ] **Step 2: Simplify StyleToyText**

  Find `internal static void StyleToyText(Text text, Color outlineColor, Vector2 outlineDistance, Color shadowColor, Vector2 shadowDistance)` at line 1033. Inside the method body, remove any lines that add `Outline` or `Shadow` components (calls to `AddComponent<Outline>()`, `AddComponent<Shadow>()`, and any `.effectColor`/`.effectDistance` assignments on those components). Leave all other logic (font size checks, etc.) untouched.

  > **Do NOT change the method signature.** The `outlineColor`, `outlineDistance`, `shadowColor`, `shadowDistance` parameters will become no-ops — that is intentional. Do NOT update call sites. There are ~30 call sites across the file; leave them all unchanged.

- [ ] **Step 3: Simplify StyleButtonLabel**

  Find `internal static void StyleButtonLabel(` at line 1061. Same as Step 2 — remove `Outline`/`Shadow` component additions from the body only. Do NOT change the method signature or any call sites.

- [ ] **Step 4: Verify compilation**

  Save. Check Unity Console. Expected: no errors.

- [ ] **Step 5: Commit**

  ```bash
  git add Assets/Scripts/TowerMaze/Runtime/UISystems.cs
  git commit -m "refactor: retire glossy sprite path, remove text outline/shadow effects"
  ```

---

## Chunk 3: Font Swap

### Task 5: Update font load paths

> **Pre-check:** Confirm `Assets/Resources/TowerMaze/UITheme/Outfit-Bold.ttf` exists before this task. If not, complete the Pre-requisite at the top of this plan first.

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs` (line 79)
- Modify: `Assets/Scripts/TowerMaze/Runtime/TowerMazeBootstrapper.cs` (line ~83)

- [ ] **Step 1: Update UISystems.cs — runtimeFont load**

  In `UIManager.Initialize()`, line 79:

  ```csharp
  // Before:
  runtimeFont = Resources.Load<Font>("TowerMaze/UITheme/Kenney Future");
  // After:
  runtimeFont = Resources.Load<Font>("TowerMaze/UITheme/Outfit-Bold");
  ```

- [ ] **Step 2: Update TowerMazeBootstrapper.cs**

  Search the file for `"TowerMaze/UITheme/Kenney Future"`. Replace every occurrence with `"TowerMaze/UITheme/Outfit-Bold"`.

- [ ] **Step 3: Search for any remaining references**

  Run a project-wide search for `Kenney Future` in all `.cs` files. Replace any remaining instances with `Outfit-Bold`.

  ```bash
  grep -r "Kenney Future" Assets/Scripts/ --include="*.cs"
  ```

  Expected: no results.

  > **Do NOT delete `Kenney Future.ttf`** from `Assets/Resources/TowerMaze/UITheme/`. Keep the file in place — Unity's resource dependency scanner or any `.meta`/`.asset` files may still reference it.

- [ ] **Step 4: Verify in Play Mode**

  Enter Unity Play Mode. Check Console. Expected: no `NullReferenceException` on font, no "Failed to load" warnings. If you see these, confirm the file is at the exact path `Assets/Resources/TowerMaze/UITheme/Outfit-Bold.ttf`.

- [ ] **Step 5: Commit**

  ```bash
  git add Assets/Scripts/TowerMaze/Runtime/UISystems.cs \
          Assets/Scripts/TowerMaze/Runtime/TowerMazeBootstrapper.cs
  git commit -m "feat: swap Kenney Future font for Outfit-Bold"
  ```

---

## Chunk 4: Screen Color Updates — Part 1

### Task 6: StartScreenController colors

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs` — `StartScreenController` class (line 2239)

- [ ] **Step 1: Update screen background**

  Find the root panel / screen background Image color in `StartScreenController.Initialize()` or `BuildUI()`. Set its color to `UIColors.Surface`.

- [ ] **Step 2: Update card colors**

  Find all `CreateCard(` calls. Set their fill color argument to `UIColors.Card`.

- [ ] **Step 3: Update card accent borders**

  - Mission card left accent strip → `UIColors.Primary`
  - Daily challenge card left accent strip → `UIColors.Warning`

- [ ] **Step 4: Update primary button (OYNA/play)**

  Find the main play button creation. Set its color argument to `UIColors.Primary`.

- [ ] **Step 5: Update secondary buttons (SHOP, TOP RUNS)**

  Set their fill to `UIColors.Card`, text color to `UIColors.Primary`.

- [ ] **Step 6: Update text colors**

  | Text element | Color |
  |---|---|
  | Score value | `UIColors.TextDark` |
  | Score "m" unit | `UIColors.Primary` |
  | Section labels (BEST SCORE, DAILY MISSION…) | `UIColors.Primary` |
  | Body/description text | `UIColors.TextMid` |
  | Caption text | `UIColors.TextDim` |

- [ ] **Step 7: Update bottom pills**

  - Life pill background → `UIColors.Card`, text → `UIColors.TextDark`
  - Ember pill background → `UIColors.Primary`, text → `Color.white`

- [ ] **Step 8: Verify in Play Mode**

  Enter Play Mode. Go to main menu. Expected: light purple-white background (`#F8F7FF`), purple OYNA button, white cards, dark text, Outfit font. No golden/yellow colors.

- [ ] **Step 9: Commit**

  ```bash
  git add Assets/Scripts/TowerMaze/Runtime/UISystems.cs
  git commit -m "feat: apply design system colors to StartScreenController"
  ```

---

### Task 7: FailScreenController colors

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs` — `FailScreenController` class (line 2765)

- [ ] **Step 1: Update background + overlay**

  Background → `UIColors.Surface`. Dark overlay rectangle → `Color.black` at 40% alpha (`new Color(0, 0, 0, 0.4f)`).

- [ ] **Step 2: Update score display**

  Score value → `UIColors.TextDark`. Score "m" unit → `UIColors.Primary`.

- [ ] **Step 3: Update semantic buttons**

  | Button | Color |
  |---|---|
  | YENİDEN DENE | `UIColors.Danger` |
  | DEVAM ET | `UIColors.Success` |
  | Ana Menü (ghost) | transparent fill, text `UIColors.TextDim` |

- [ ] **Step 4: Update cards**

  - Stat cards (TIME, ZONE) → `UIColors.Card`
  - Reward card → `UIColors.Card`
  - Mission card → `UIColors.Card` with left border `UIColors.Primary`

- [ ] **Step 5: Update badge + text**

  | Element | Color |
  |---|---|
  | "New Best!" badge background | `UIColors.SuccessBg` |
  | "New Best!" badge text | `UIColors.SuccessText` |
  | ZONE value | `UIColors.Primary` |
  | TIME value | `UIColors.TextDark` |
  | Reward value | `UIColors.TextDark` |
  | Label texts | `UIColors.TextDim` |

- [ ] **Step 6: Verify in Play Mode**

  Trigger a game over. Expected: light background, red YENİDEN DENE, green DEVAM ET, white stat cards.

- [ ] **Step 7: Commit**

  ```bash
  git add Assets/Scripts/TowerMaze/Runtime/UISystems.cs
  git commit -m "feat: apply design system colors to FailScreenController"
  ```

---

### Task 8: UIHudController colors

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs` — `UIHudController` class (line 1214)

- [ ] **Step 1: Update HUD background**

  Root background color → `UIColors.HudBg`.

- [ ] **Step 2: Update score/best score cards**

  Card fill → `UIColors.HudCard`. Card outline/border → `UIColors.HudBorder`. Label text (SKOR, EN İYİ) → `UIColors.HudTextDim`. Score value → `Color.white`. Best score value → `UIColors.PrimaryLight`.

- [ ] **Step 3: Update zone badge**

  Zone badge background → `UIColors.HudCard`. Zone text → `UIColors.PrimaryLight`.

- [ ] **Step 4: Update lava proximity bar**

  Container card → `UIColors.HudCard` with `UIColors.HudBorder` border. Label text → `UIColors.HudTextDim`. Fill bar color → `UIColors.Danger` (solid).

- [ ] **Step 5: Update New Best badge**

  Background → `UIColors.SuccessBg`. Text → `UIColors.SuccessText`.

- [ ] **Step 6: Verify in Play Mode**

  Start a game run. Expected: very dark purple background (`#0F0A1E`), dark purple score cards, light purple zone text, red lava bar.

- [ ] **Step 7: Commit**

  ```bash
  git add Assets/Scripts/TowerMaze/Runtime/UISystems.cs
  git commit -m "feat: apply design system colors to UIHudController"
  ```

---

## Chunk 5: Screen Color Updates — Part 2

### Task 9: ShopScreenController colors

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs` — `ShopScreenController` class (line 1556)

Note: This controller has several hardcoded colors. Key ones to find and replace:
- Line ~1931: `new Color(0.92f, 0.96f, 1f, 0.98f)` (item button fill) → `UIColors.Card`
- Line ~1931: `new Color(0.16f, 0.24f, 0.46f, 1f)` (item button text) → `UIColors.Primary`
- Line ~1968: `new Color(0.24f, 0.32f, 0.52f, 1f)` (secondary label) → `UIColors.TextDim`
- Line ~1973: `new Color(0.25f, 0.84f, 0.24f, 1f)` (price pill background, green) → `UIColors.Primary`
- Line ~1976: `Color.white` on price text — keep as `Color.white`
- Line ~1983: `new Color(1f, 0.68f, 0.18f, 1f)` (badge pill background, golden yellow) → `UIColors.SuccessBg`
- Line ~1986: `Color.black` on badge text → `UIColors.SuccessText`

- [ ] **Step 1: Update overlay and modal**

  Dark overlay → `Color.black` at 70% alpha. Modal/sheet background → `UIColors.Card`.

- [ ] **Step 2: Update tab bar**

  The three tabs (Coins, Balls, Towers) each have different active colors in the current code. Replace all three active tab colors with `UIColors.Primary` (text → `Color.white`) to unify them under the design system. Inactive tab → transparent, text → `UIColors.TextDim`.

- [ ] **Step 3: Update item list cards**

  Item card background → `UIColors.Card`. Item name text → `UIColors.TextDark`. Description/secondary label → `UIColors.TextDim`.

- [ ] **Step 4: Update item preview frame in ConfigureCatalogItem (~line 1817)**

  The `accentColor` parameter in `ConfigureCatalogItem` is passed from the shop's catalog data — leave it as-is. Only update the hardcoded fallback colors on lines ~1931 and ~1968.

- [ ] **Step 5: Update owned badge**

  Owned badge background → `UIColors.SuccessBg`. Badge text → `UIColors.SuccessText`.

- [ ] **Step 6: Update price button**

  Price button color → `UIColors.Primary`.

- [ ] **Step 7: Verify in Play Mode**

  Open shop. Expected: dark overlay, white modal, purple active tab, white item cards.

- [ ] **Step 8: Commit**

  ```bash
  git add Assets/Scripts/TowerMaze/Runtime/UISystems.cs
  git commit -m "feat: apply design system colors to ShopScreenController"
  ```

---

### Task 10: CountdownController + SplashScreenController

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs` — `CountdownController` (line 1086), `SplashScreenController` (line 3139)

- [ ] **Step 1: CountdownController — decouple from theme.accentColor**

  At line 1094, `countdownText` is created with `theme.accentColor`. At lines 1096–1097, `numberColor` and `goColor` are set.

  Replace as follows:
  ```csharp
  // Line 1094 — change to:
  countdownText = UIManager.CreateText("CountdownText", transform, font, 180, TextAnchor.MiddleCenter, UIColors.Primary);
  // Line 1096 — change to:
  numberColor = UIColors.Primary;
  // Line 1097 — change to:
  goColor = UIColors.PrimaryLight;
  ```

  Also set background → `UIColors.HudBg`.

- [ ] **Step 2: SplashScreenController**

  Background → `UIColors.HudBg`. "TOWER" text → `Color.white`. "MAZE" text → `UIColors.PrimaryLight`. Spinner ring color → `UIColors.PrimaryLight`.

- [ ] **Step 3: Verify in Play Mode**

  Observe splash screen and countdown. Expected: dark HUD background, white "TOWER", light purple "MAZE", purple countdown number.

- [ ] **Step 4: Commit**

  ```bash
  git add Assets/Scripts/TowerMaze/Runtime/UISystems.cs
  git commit -m "feat: apply design system colors to Countdown and Splash controllers"
  ```

---

### Task 11: IAPUpsellController + RewardToastController

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs` — `IAPUpsellController` (line 2973), `RewardToastController` (line 1110)

Key hardcoded colors to replace in `IAPUpsellController`:
- Line ~3018: `new Color(1f, 1f, 1f, 0.92f)` (close button fill) → `UIColors.Card`
- Line ~3050: `Color.black` (title text) → `UIColors.TextDark`
- Line ~3055: `new Color(0.30f, 0.26f, 0.14f, 1f)` (desc text) → `UIColors.TextMid`
- Line ~3060: `new Color(0.10f, 0.44f, 0.10f, 1f)` (price text) → `Color.white`
- Line ~3065: `new Color(0.18f, 0.72f, 0.22f, 1f)` (buy button fill) → `UIColors.Primary`

- [ ] **Step 1: IAPUpsellController**

  | Line | Old value | New value |
  |---|---|---|
  | ~3003 | `new Color(0f, 0f, 0f, 0.68f)` (backdrop) | `new Color(0f, 0f, 0f, 0.7f)` |
  | ~3010 | `ApplyCardSurface(card, new Color(0.97f, 0.96f, 0.92f, 0.99f))` | replace Color arg with `UIColors.Card` |
  | ~3018 | `new Color(1f, 1f, 1f, 0.92f)` (close button) | `UIColors.Card` |
  | ~3050 | `Color.black` (title text) | `UIColors.TextDark` |
  | ~3055 | `new Color(0.30f, 0.26f, 0.14f, 1f)` (desc text) | `UIColors.TextMid` |
  | ~3060 | `new Color(0.10f, 0.44f, 0.10f, 1f)` (price text) | `Color.white` |
  | ~3065 | `new Color(0.18f, 0.72f, 0.22f, 1f)` (buy button) | `UIColors.Primary` |

- [ ] **Step 2: RewardToastController**

  - Toast background → `UIColors.Card`
  - Title text → `UIColors.TextDark`
  - Subtitle text → `UIColors.TextDim`
  - Accent/icon color → `UIColors.Warning` (coin icon color)

- [ ] **Step 3: Verify in Play Mode**

  Trigger an IAP upsell and a reward toast. Expected: white modal with purple buy button; white toast with dark text.

- [ ] **Step 4: Commit**

  ```bash
  git add Assets/Scripts/TowerMaze/Runtime/UISystems.cs
  git commit -m "feat: apply design system colors to IAPUpsell and RewardToast controllers"
  ```

---

### Task 12: LeaderboardPanelController + RushWarningController + ControlFlipController

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs` — three controllers (lines 1348, 1472, 1418)

- [ ] **Step 1: LeaderboardPanelController (line 1348)**

  | Element | Color |
  |---|---|
  | Panel background | `UIColors.Card` |
  | Title text | `UIColors.TextDark` |
  | Player name | `UIColors.TextDark` |
  | Score text | `UIColors.Primary` |
  | Rank label | `UIColors.TextDim` |
  | Current player row highlight | `UIColors.PrimaryBg` |

- [ ] **Step 2: RushWarningController (line 1472)**

  | Element | Color |
  |---|---|
  | Overlay tint | `UIColors.Warning` at 20% alpha |
  | Card background | `UIColors.Card` |
  | Warning title | `UIColors.Warning` |
  | Body text | `UIColors.TextMid` |

- [ ] **Step 3: ControlFlipController (line 1418)**

  | Element | Color |
  |---|---|
  | Overlay tint | `UIColors.PrimaryBg` at 60% alpha |
  | Card background | `UIColors.Card` |
  | Title text | `UIColors.Primary` |
  | Body text | `UIColors.TextDim` |
  | GOT IT button | `UIColors.Primary` |

- [ ] **Step 4: Verify in Play Mode**

  Trigger leaderboard, rush warning, and control flip. Expected: white panels with purple accents, orange rush warning title, purple control flip title.

- [ ] **Step 5: Commit**

  ```bash
  git add Assets/Scripts/TowerMaze/Runtime/UISystems.cs
  git commit -m "feat: apply design system colors to Leaderboard, RushWarning, ControlFlip"
  ```

---

## Chunk 6: ThemeDefinition + Final Verification

### Task 13: Update ThemeDefinition accentColor

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/ConfigData.cs`
- Modify (Inspector): `Assets/TowerMaze/Config/VolcanicTheme.asset`

- [ ] **Step 1: Update C# default in ConfigData.cs**

  Find the `accentColor` field in `ThemeDefinition`:
  ```csharp
  // Before (golden yellow):
  public Color accentColor = new Color(1f, 0.69f, 0.19f);
  // After (deep purple):
  public Color accentColor = new Color(0.486f, 0.227f, 0.929f, 1f); // #7C3AED
  ```

- [ ] **Step 2: Update VolcanicTheme.asset in Unity Inspector**

  In the Unity Editor Project panel, select `Assets/TowerMaze/Config/VolcanicTheme.asset`. In the Inspector, find **Accent Color** and set:
  - R: 0.486, G: 0.227, B: 0.929, A: 1

  Save (Ctrl+S). **Do not commit yet.**

- [ ] **Step 3: Verify in Play Mode before committing**

  Enter Play Mode. Check the 3D environment (backdrop, tower walls, etc.) for purple tinting.

  - If the tint looks **acceptable**: proceed to Step 4.
  - If the tint looks **wrong**: revert `VolcanicTheme.asset` to golden yellow in the Inspector. The UI is unaffected — all controllers already use `UIColors.Primary` directly after earlier tasks. Proceed to Step 4 with the golden value restored.

- [ ] **Step 4: Verify compilation**

  Check Unity Console. Expected: no errors.

- [ ] **Step 5: Commit**

  ```bash
  git add Assets/Scripts/TowerMaze/Runtime/ConfigData.cs \
          "Assets/TowerMaze/Config/VolcanicTheme.asset"
  git commit -m "feat: update ThemeDefinition accentColor to purple #7C3AED"
  ```

---

### Task 14: End-to-end verification

- [ ] **Step 1: Full Play Mode walkthrough**

  Enter Play Mode and visit every screen:

  | Screen | Expected |
  |---|---|
  | Splash | Dark `#0F0A1E` background, white TOWER, light purple MAZE, light purple spinner |
  | Main menu | Light `#F8F7FF` background, purple OYNA button, white cards, Outfit font |
  | Countdown | Dark `#0F0A1E` background, purple countdown number, light purple GO! |
  | Gameplay HUD | Dark `#0F0A1E` background, dark purple score cards, purple-light accents, red lava bar |
  | Fail screen | Light background, red YENİDEN DENE, green DEVAM ET, white cards |
  | Shop | Dark overlay, white modal, purple active tab |
  | Leaderboard | White panel, purple score text |
  | Rush warning | Orange warning title on white card |
  | Control flip | Purple title on white card |
  | IAP upsell | White modal, purple buy button |
  | Reward toast | White pill toast, dark text |

- [ ] **Step 2: Check for remaining yellow/golden colors**

  Scan all screens. Expected: no yellow or golden UI elements (lava/environment colors are intentionally unchanged).

- [ ] **Step 3: Check Unity Console**

  Expected: zero `NullReferenceException`, zero font/sprite load warnings.

- [ ] **Step 4: Final commit**

  ```bash
  git add .
  git commit -m "feat: complete TowerMaze Figma design system Unity implementation"
  ```
