# TowerMaze UI Redesign Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Redesign TowerMaze's runtime-generated UI to a dark-purple premium aesthetic optimized for monetization, splitting the 3341-line UISystems.cs into 7 focused files.

**Architecture:** Create `Assets/Scripts/TowerMaze/Runtime/UISystems/` directory with standalone class files. `UIStyle.cs` provides the centralized token system and all coroutine animation helpers. Each screen controller is extracted from `UISystems.cs` into its own file and redesigned. No DOTween — all motion via Unity Coroutines. Build stays green after every task.

**Tech Stack:** Unity C#, UGUI (Canvas / RectTransform / Image / Text), Unity Coroutines, Outfit-Bold (already in `Resources/TowerMaze/UITheme/`)

**Spec:** `docs/superpowers/specs/2026-03-18-towermaze-ui-redesign.md`

---

## Chunk 1: Foundation — UIStyle.cs + File Structure

### Task 1: Create directory + UIStyle.cs

**Files:**

- Create: `Assets/Scripts/TowerMaze/Runtime/UISystems/UIStyle.cs`

**Acceptance criteria:** `UIStyle.Brand` returns `#7C4DFF`; project compiles with zero errors.

- [ ] **Step 1: Create the UISystems directory and UIStyle.cs**

```csharp
// Assets/Scripts/TowerMaze/Runtime/UISystems/UIStyle.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Centralized design token system and animation helpers for TowerMaze UI.
/// All hex values and alpha values are static readonly — no inline hex strings elsewhere.
/// </summary>
public static class UIStyle
{
    // ─── Color Tokens ───────────────────────────────────────────────────────
    public static readonly Color Brand        = Hex("#7C4DFF"); // identity / tabs / selected
    public static readonly Color MenuBg       = Hex("#2D1B69"); // main menu background
    public static readonly Color ShopBg       = Hex("#1A0A35"); // shop + skins background
    public static readonly Color HudBg        = Hex("#0F0A1E"); // in-game HUD background
    public static readonly Color FailBg       = Hex("#1A0F2E"); // game over background
    public static readonly Color Action       = Hex("#FF9F0A"); // START / CONTINUE / buy
    public static readonly Color ActionLight  = Hex("#FFB340"); // buy button gradient end
    public static readonly Color Gold         = Hex("#FFD60A"); // coins / BEST VALUE badge
    public static readonly Color Owned        = Hex("#10B981"); // owned badge / claim button
    public static readonly Color Danger       = Hex("#EF4444"); // lava distance indicator

    // Transparent variants (cannot be readonly because Color struct with alpha)
    public static Color SurfaceDark  => new Color(1f, 1f, 1f, 0.07f); // cards on dark bg
    public static Color SurfaceDark2 => new Color(1f, 1f, 1f, 0.04f); // locked / secondary
    public static Color BorderDark   => new Color(1f, 1f, 1f, 0.06f); // card borders
    public static Color TextPrimary  => Color.white;
    public static Color TextDim      => new Color(1f, 1f, 1f, 0.35f); // secondary text
    public static Color TextFaint    => new Color(1f, 1f, 1f, 0.30f); // captions / labels

    // ─── Metrics (pixels, Unity units match 1:1 at 1080p reference) ─────────
    public const int RadiusLg   = 16;
    public const int RadiusMd   = 14;
    public const int RadiusSm   = 10;
    public const int RadiusPill = 999;
    public const int PadH       = 14;   // horizontal screen padding
    public const int PadV       = 18;   // vertical screen padding
    public const int GridGap    = 8;    // skins grid gap
    public const int BtnHeightPrimary  = 48;
    public const int BtnHeightStart    = 56;
    public const int BtnHeightContinue = 60;

    // ─── Hex Parser ──────────────────────────────────────────────────────────
    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // COROUTINE ANIMATIONS
    // All coroutines are static — callers must StartCoroutine on a MonoBehaviour.
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Universal button press: scale 1→0.95 (0.08s) then →1 (0.12s).
    /// </summary>
    public static IEnumerator ButtonPress(RectTransform rt)
    {
        var original = rt.localScale;
        var pressed  = original * 0.95f;
        float t = 0f;
        while (t < 0.08f) { rt.localScale = Vector3.Lerp(original, pressed, t / 0.08f); t += Time.deltaTime; yield return null; }
        rt.localScale = pressed;
        t = 0f;
        while (t < 0.12f) { rt.localScale = Vector3.Lerp(pressed, original, t / 0.12f); t += Time.deltaTime; yield return null; }
        rt.localScale = original;
    }

    /// <summary>
    /// Buy button tap expand: scale 1→1.05 (0.08s) then press 1.05→0.95 (0.06s) then →1 (0.10s).
    /// </summary>
    public static IEnumerator BuyButtonTap(RectTransform rt)
    {
        var orig = rt.localScale;
        var big  = orig * 1.05f;
        var sm   = orig * 0.95f;
        float t = 0f;
        while (t < 0.08f) { rt.localScale = Vector3.Lerp(orig, big, t / 0.08f); t += Time.deltaTime; yield return null; }
        t = 0f;
        while (t < 0.06f) { rt.localScale = Vector3.Lerp(big, sm, t / 0.06f);  t += Time.deltaTime; yield return null; }
        t = 0f;
        while (t < 0.10f) { rt.localScale = Vector3.Lerp(sm, orig, t / 0.10f); t += Time.deltaTime; yield return null; }
        rt.localScale = orig;
    }

    /// <summary>
    /// Idle pulse loop for START/CONTINUE buttons (scale 1↔1.05, period 1.4s).
    /// Runs forever — stop by destroying the coroutine or the GameObject.
    /// </summary>
    public static IEnumerator Pulse(RectTransform rt, float min = 1f, float max = 1.05f, float period = 1.4f)
    {
        float half = period / 2f;
        while (true)
        {
            float t = 0f;
            while (t < half) { float s = Mathf.Lerp(min, max, Mathf.SmoothStep(0, 1, t / half)); rt.localScale = new Vector3(s, s, 1); t += Time.deltaTime; yield return null; }
            t = 0f;
            while (t < half) { float s = Mathf.Lerp(max, min, Mathf.SmoothStep(0, 1, t / half)); rt.localScale = new Vector3(s, s, 1); t += Time.deltaTime; yield return null; }
        }
    }

    /// <summary>
    /// Looping vertical bounce (BEST VALUE badge, amplitude 5px, period 1.3s).
    /// </summary>
    public static IEnumerator Bounce(RectTransform rt, float amplitude = 5f, float period = 1.3f)
    {
        var origin = rt.anchoredPosition;
        while (true)
        {
            float t = 0f, phase = period * 0.45f;
            while (t < phase) { rt.anchoredPosition = new Vector2(origin.x, origin.y - amplitude * Mathf.SmoothStep(0, 1, t / phase)); t += Time.deltaTime; yield return null; }
            t = 0f; phase = period * 0.55f;
            while (t < phase) { rt.anchoredPosition = new Vector2(origin.x, (origin.y - amplitude) + amplitude * Mathf.SmoothStep(0, 1, t / phase)); t += Time.deltaTime; yield return null; }
        }
    }

    /// <summary>
    /// Looping alpha glow pulse (BEST VALUE card glow image, 1.8s period).
    /// </summary>
    public static IEnumerator GlowPulse(Image glowImg, Color baseColor, float minA = 0.3f, float maxA = 0.7f, float period = 1.8f)
    {
        float half = period / 2f;
        while (true)
        {
            float t = 0f;
            while (t < half) { glowImg.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Lerp(minA, maxA, Mathf.SmoothStep(0, 1, t / half))); t += Time.deltaTime; yield return null; }
            t = 0f;
            while (t < half) { glowImg.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Lerp(maxA, minA, Mathf.SmoothStep(0, 1, t / half))); t += Time.deltaTime; yield return null; }
        }
    }

    /// <summary>
    /// Screen / panel fade in (CanvasGroup alpha 0→1, default 0.25s).
    /// </summary>
    public static IEnumerator FadeIn(CanvasGroup cg, float duration = 0.25f)
    {
        cg.alpha = 0f;
        float t = 0f;
        while (t < duration) { cg.alpha = Mathf.SmoothStep(0, 1, t / duration); t += Time.deltaTime; yield return null; }
        cg.alpha = 1f;
    }

    /// <summary>
    /// Screen / panel fade out (CanvasGroup alpha →0, default 0.20s).
    /// </summary>
    public static IEnumerator FadeOut(CanvasGroup cg, float duration = 0.20f)
    {
        float start = cg.alpha, t = 0f;
        while (t < duration) { cg.alpha = Mathf.Lerp(start, 0, t / duration); t += Time.deltaTime; yield return null; }
        cg.alpha = 0f;
    }

    /// <summary>
    /// Bottom sheet open: slide up from below AND fade in simultaneously (spec §5 "Popup Open").
    /// Requires a CanvasGroup on the sheet's root GameObject.
    /// </summary>
    public static IEnumerator SlideUp(RectTransform rt, float sheetHeight, float duration = 0.25f)
    {
        var cg = rt.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>();
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -sheetHeight);
        cg.alpha = 0f;
        float t = 0f;
        while (t < duration)
        {
            float p = Mathf.SmoothStep(0, 1, t / duration);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, Mathf.Lerp(-sheetHeight, 0, p));
            cg.alpha = p;
            t += Time.deltaTime;
            yield return null;
        }
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 0);
        cg.alpha = 1f;
    }

    /// <summary>
    /// Bottom sheet close: slide down AND fade out simultaneously (spec §5 "Popup Close").
    /// </summary>
    public static IEnumerator SlideDown(RectTransform rt, float sheetHeight, float duration = 0.20f)
    {
        var cg = rt.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>();
        float t = 0f;
        while (t < duration)
        {
            float p = t / duration;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, Mathf.Lerp(0, -sheetHeight, p));
            cg.alpha = 1f - p;
            t += Time.deltaTime;
            yield return null;
        }
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -sheetHeight);
        cg.alpha = 0f;
    }

    /// <summary>
    /// Score pop entry: scale 0→1.15 (0.28s ease-out) then 1.15→1 (0.12s ease-in).
    /// </summary>
    public static IEnumerator ScorePop(RectTransform rt)
    {
        rt.localScale = Vector3.zero;
        float t = 0f;
        while (t < 0.28f) { float s = Mathf.Lerp(0f, 1.15f, 1f - Mathf.Pow(1f - t / 0.28f, 3f)); rt.localScale = new Vector3(s, s, 1); t += Time.deltaTime; yield return null; }
        rt.localScale = new Vector3(1.15f, 1.15f, 1);
        t = 0f;
        while (t < 0.12f) { float s = Mathf.Lerp(1.15f, 1f, Mathf.Pow(t / 0.12f, 2f)); rt.localScale = new Vector3(s, s, 1); t += Time.deltaTime; yield return null; }
        rt.localScale = Vector3.one;
    }

    /// <summary>
    /// Coin gain float: spawns "+X 🪙" text, floats up 44px, scale 1→1.2→1, fades out. Auto-destroys.
    /// </summary>
    public static IEnumerator CoinFloat(Text floatText)
    {
        var rt = floatText.rectTransform;
        var startPos = rt.anchoredPosition;
        var baseColor = floatText.color;
        float elapsed = 0f, dur = 1.2f;
        while (elapsed < dur)
        {
            float p = elapsed / dur;
            float yOff = Mathf.Lerp(0, -44f, p);
            float scale = p < 0.25f ? Mathf.Lerp(1f, 1.2f, p / 0.25f) : Mathf.Lerp(1.2f, 1f, (p - 0.25f) / 0.75f);
            float alpha = p < 0.30f ? 1f : Mathf.Lerp(1f, 0f, (p - 0.30f) / 0.70f);
            rt.anchoredPosition = new Vector2(startPos.x, startPos.y - yOff);
            rt.localScale = new Vector3(scale, scale, 1);
            floatText.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Object.Destroy(floatText.gameObject);
    }

    /// <summary>
    /// One-shot background danger pulse (fade in 0.3s, hold 0.5s, fade out 0.4s).
    /// overlay.color.a should be 0 before calling.
    /// </summary>
    public static IEnumerator BackgroundPulse(Image overlay, Color pulseColor)
    {
        float t = 0f;
        while (t < 0.3f) { overlay.color = new Color(pulseColor.r, pulseColor.g, pulseColor.b, Mathf.Lerp(0, pulseColor.a, t / 0.3f)); t += Time.deltaTime; yield return null; }
        overlay.color = pulseColor;
        yield return new WaitForSeconds(0.5f);
        t = 0f;
        while (t < 0.4f) { overlay.color = new Color(pulseColor.r, pulseColor.g, pulseColor.b, Mathf.Lerp(pulseColor.a, 0, t / 0.4f)); t += Time.deltaTime; yield return null; }
        overlay.color = new Color(pulseColor.r, pulseColor.g, pulseColor.b, 0);
    }

    /// <summary>
    /// Tab content swap with fade: fade out 0.1s, call swap(), fade in 0.15s.
    /// </summary>
    public static IEnumerator TabSwitch(CanvasGroup cg, System.Action swap)
    {
        float t = 0f;
        float startA = cg.alpha;
        while (t < 0.10f) { cg.alpha = Mathf.Lerp(startA, 0, t / 0.10f); t += Time.deltaTime; yield return null; }
        cg.alpha = 0f;
        swap();
        t = 0f;
        while (t < 0.15f) { cg.alpha = Mathf.Lerp(0, 1, t / 0.15f); t += Time.deltaTime; yield return null; }
        cg.alpha = 1f;
    }
}
```

- [ ] **Step 2: Verify compilation in Unity Editor**

Open Unity Editor → check Console for errors. Expected: zero errors, UIStyle compiles cleanly.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/UISystems/UIStyle.cs
git commit -m "feat: add UIStyle design token system and coroutine animation helpers"
```

---

### Task 2: Split UISystems.cs — Extract UIManager

**Files:**

- Create: `Assets/Scripts/TowerMaze/Runtime/UISystems/UIManager.cs`
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs` (remove UIManager class body, leave other classes intact)

**Acceptance criteria:** Project compiles. `TowerMazeBootstrapper` still finds `UIManager`. All screens still work in Play Mode.

- [ ] **Step 1: Locate UIManager class boundaries**

```bash
grep -n "^public class UIManager\|^public sealed class UIManager" \
  Assets/Scripts/TowerMaze/Runtime/UISystems.cs
```

Note the start line. Then find the matching closing brace — it is the next top-level `^}` after all nested braces close. Confirm by checking the line after it is another `public class` declaration or EOF.

- [ ] **Step 2: Create UIManager.cs**

Create the file with all `using` statements from `UISystems.cs` plus the UIManager class body copied verbatim:

```csharp
// Assets/Scripts/TowerMaze/Runtime/UISystems/UIManager.cs
// UIManager — canvas setup, screen routing, Initialize()
// Visual redesign happens in individual screen files (Tasks 3–9).
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Paste the complete UIManager class from UISystems.cs here.
// Include every field, property, and method — no omissions.
// Class declaration: public class UIManager (remove sealed if present).
```

**Required static helpers on UIManager:** The tasks below call `UIManager.CreateImage`, `UIManager.CreateText`, `UIManager.Stretch`, and `UIManager.BindButton`. These must exist as `public static` methods on `UIManager`. Confirm after extract:

```bash
grep -n "public static.*CreateImage\|public static.*CreateText\|public static.*Stretch\|public static.*BindButton" \
  Assets/Scripts/TowerMaze/Runtime/UISystems/UIManager.cs
```

If any are missing, add them:

```csharp
public static Image CreateImage(Transform parent, string name, Color color)
{
    var go = new GameObject(name); go.transform.SetParent(parent, false);
    var img = go.AddComponent<Image>(); img.color = color; return img;
}
public static Text CreateText(Transform parent, string name, string text,
    int size, FontStyle style, Color color, Font font,
    TextAnchor anchor = TextAnchor.MiddleCenter)
{
    var go = new GameObject(name); go.transform.SetParent(parent, false);
    var t = go.AddComponent<Text>(); t.text = text; t.font = font;
    t.fontSize = size; t.fontStyle = style; t.color = color; t.alignment = anchor; return t;
}
// Stretch with default params covers both Stretch(rt) and Stretch(rt, l, r, t, b) call sites.
public static void Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
{
    rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
    rt.offsetMin = new Vector2(left, bottom); rt.offsetMax = new Vector2(-right, -top);
}
public static void BindButton(Button btn, System.Action onClick)
{
    btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(() => onClick?.Invoke());
}
```

- [ ] **Step 3: Remove UIManager from UISystems.cs**

Delete the `public class UIManager { ... }` block from `UISystems.cs`. Leave all other classes (StartScreenController, FailScreenController, etc.) intact.

- [ ] **Step 4: Verify compilation** — Console must show zero errors.

- [ ] **Step 5: Enter Play Mode — verify main menu appears as before**

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/UISystems/UIManager.cs
git add Assets/Scripts/TowerMaze/Runtime/UISystems.cs
git commit -m "refactor: extract UIManager into dedicated file"
```

---

### Task 3: Split UISystems.cs — Extract Remaining Controllers

**Files:**

- Create: `Assets/Scripts/TowerMaze/Runtime/UISystems/HudController.cs`
- Create: `Assets/Scripts/TowerMaze/Runtime/UISystems/StartScreen.cs`
- Create: `Assets/Scripts/TowerMaze/Runtime/UISystems/FailScreen.cs`
- Create: `Assets/Scripts/TowerMaze/Runtime/UISystems/ShopScreen.cs`
- Create: `Assets/Scripts/TowerMaze/Runtime/UISystems/PopupControllers.cs`
- Delete: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs`

**Acceptance criteria:** `UISystems.cs` deleted; project compiles; Play Mode works.

Extract **one class at a time**, verify compilation after each extraction. For each class, locate its boundaries first:

```bash
# Example: find UIHudController start + next top-level class to know the end
grep -n "^public.*class UIHudController\|^public.*class StartScreenController\|^public.*class FailScreenController\|^public.*class ShopScreenController" \
  Assets/Scripts/TowerMaze/Runtime/UISystems.cs
```

All new files share the same `using` block:

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
```

- [ ] **Step 1: Extract UIHudController → HudController.cs**

Create `Assets/Scripts/TowerMaze/Runtime/UISystems/HudController.cs` with the `using` block above, then paste the complete `UIHudController` class verbatim (no visual changes). Remove `UIHudController` from `UISystems.cs`. Open Unity Editor → check Console → zero errors. ✓

- [ ] **Step 2: Extract StartScreenController → StartScreen.cs**

Same process: create `Assets/Scripts/TowerMaze/Runtime/UISystems/StartScreen.cs`, paste `StartScreenController` verbatim, remove from `UISystems.cs`, verify compile. ✓

- [ ] **Step 3: Extract FailScreenController → FailScreen.cs**

Same process. ✓

- [ ] **Step 4: Extract ShopScreenController → ShopScreen.cs**

Same process. ✓

- [ ] **Step 5: Extract remaining classes → PopupControllers.cs**

Remaining classes to extract: `CountdownController`, `PauseScreenController`, `ControlFlipController`, `RushWarningController`, `RewardToastController`, `IAPUpsellController`, `SplashScreenController`.

> **Note on LeaderboardPanelController:** The existing `LeaderboardPanelController` in `UISystems.cs` is superseded by the new inline leaderboard bottom-sheet in `StartScreen.cs` (Task 6). Do NOT copy it to `PopupControllers.cs`. Delete it from `UISystems.cs` at this step. Its functionality is fully replaced by `BuildLeaderboardPopup()` in `StartScreen.cs`.

```csharp
// Assets/Scripts/TowerMaze/Runtime/UISystems/PopupControllers.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
// (paste all remaining controllers verbatim)
```

Remove from `UISystems.cs`. Compile. ✓

- [ ] **Step 6: Delete UISystems.cs** — it should now be empty (or contain only using statements).

```bash
rm "Assets/Scripts/TowerMaze/Runtime/UISystems.cs"
```

- [ ] **Step 7: Verify compilation + Play Mode** — all screens work as before.

- [ ] **Step 8: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/UISystems/
git rm Assets/Scripts/TowerMaze/Runtime/UISystems.cs
git commit -m "refactor: split UISystems.cs into 7 focused files (no visual changes)"
```

---

## Chunk 2: HUD Redesign

### Task 4: Redesign UIHudController

**Files:**

- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems/HudController.cs`
- Modify: `Assets/Scripts/TowerMaze/Runtime/ConfigData.cs` (add `heightProgressMax` field)

**Acceptance criteria (enter Play Mode and start a run):**

- Background is `#0F0A1E`
- Left edge: 4px purple vertical bar with 3 tick marks (visible at roughly 25/50/100m on the bar)
- Top row: 🪙 gold + balance, ⚙ right
- Center: "SCORE" label + large white score
- Lava distance pill visible
- On coin pickup: "+X 🪙" text floats up in gold and fades out

- [ ] **Step 1: Add `heightProgressMax` to GameConfig**

In `ConfigData.cs`, find `public class GameConfig` and add:

```csharp
[Tooltip("Max height value for the HUD progress bar (m). Default 200.")]
public float heightProgressMax = 200f;
```

- [ ] **Step 2: Add `UIManager.CreateIconButton` and `UIManager.CreateActionButton` to UIManager.cs first**

These helpers are needed by `UIHudController.Initialize()` below and are defined in Task 5. To keep the build green, add them to `UIManager.cs` now (or implement Task 5 Step 1 UIManager-helper block first, then return here). See Task 5 Step 1 for the complete method bodies.

- [ ] **Step 3: Rewrite UIHudController.Initialize()**

Replace the existing `Initialize()` body. Keep all existing field declarations and callback parameters — only rewrite the visual construction.

**Signature note:** The `/* ...existing params... */` placeholder covers the callbacks already wired in the original `UIHudController.Initialize()`. Before rewriting, confirm the existing signature:

```bash
grep -n "void Initialize" Assets/Scripts/TowerMaze/Runtime/UISystems/HudController.cs
```

In particular, `_onPause` must be a `System.Action` field stored before this method body runs. The `_runner` field must be a `MonoBehaviour` reference (typically `UIManager`), also stored at init time. Add `_config = config;` and `_runner = runner;` as the first lines if not already present.

```csharp
public void Initialize(Canvas canvas, Font font, GameConfig config, /* ...existing params... */)
{
    _config = config; // store for progress bar max

    // Root panel — full screen, dark bg
    var root = UIManager.CreateImage(canvas.transform, "HUD", UIStyle.HudBg);
    UIManager.Stretch(root.rectTransform);
    root.gameObject.SetActive(false);
    _root = root.gameObject;

    // ── Progress bar (left edge, 4px wide) ──────────────────────────────
    var pbTrack = UIManager.CreateImage(root.transform, "PBTrack", UIStyle.SurfaceDark);
    var pbTrackRt = pbTrack.rectTransform;
    pbTrackRt.anchorMin = new Vector2(0, 0);
    pbTrackRt.anchorMax = new Vector2(0, 1);
    pbTrackRt.offsetMin = new Vector2(7, 14);
    pbTrackRt.offsetMax = new Vector2(11, -14);

    _progressFill = UIManager.CreateImage(pbTrack.transform, "PBFill", UIStyle.Brand);
    var fillRt = _progressFill.rectTransform;
    fillRt.anchorMin = new Vector2(0, 0);
    fillRt.anchorMax = new Vector2(1, 0); // grows upward
    fillRt.offsetMin = Vector2.zero;
    fillRt.offsetMax = new Vector2(0, 0); // height set in SetScore()
    fillRt.pivot = new Vector2(0.5f, 0f);

    // Milestone ticks at 25m / 50m / 100m
    SpawnMilestoneTick(pbTrack.transform, 25f,  config.heightProgressMax, font);
    SpawnMilestoneTick(pbTrack.transform, 50f,  config.heightProgressMax, font);
    SpawnMilestoneTick(pbTrack.transform, 100f, config.heightProgressMax, font);

    // ── Top row ─────────────────────────────────────────────────────────
    var topRow = new GameObject("TopRow").AddComponent<RectTransform>();
    topRow.SetParent(root.transform, false);
    topRow.anchorMin = new Vector2(0, 1); topRow.anchorMax = new Vector2(1, 1);
    topRow.offsetMin = new Vector2(UIStyle.PadH + 12, -50);
    topRow.offsetMax = new Vector2(-UIStyle.PadH, 0);

    // Coin icon + balance
    var coinIcon = UIManager.CreateText(topRow, "CoinIcon", "🪙", 12, FontStyle.Normal, UIStyle.Gold, font);
    coinIcon.rectTransform.anchorMin = new Vector2(0, 0); coinIcon.rectTransform.anchorMax = new Vector2(0, 1);
    coinIcon.rectTransform.offsetMin = new Vector2(0, 0); coinIcon.rectTransform.offsetMax = new Vector2(20, 0);

    _coinText = UIManager.CreateText(topRow, "CoinBalance", "0", 12, FontStyle.Bold, Color.white, font, TextAnchor.MiddleLeft);
    _coinText.rectTransform.anchorMin = new Vector2(0, 0); _coinText.rectTransform.anchorMax = new Vector2(0.5f, 1);
    _coinText.rectTransform.offsetMin = new Vector2(22, 0); _coinText.rectTransform.offsetMax = new Vector2(0, 0);

    // Settings icon (right)
    var settingsBtn = UIManager.CreateIconButton(topRow, "Settings", "⚙", font, UIStyle.SurfaceDark);
    settingsBtn.GetComponent<RectTransform>().anchorMin = new Vector2(1, 0.5f);
    settingsBtn.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0.5f);
    settingsBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-13, 0);
    settingsBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(26, 26);

    // ── Score (center) ──────────────────────────────────────────────────
    var centerPivot = new GameObject("ScoreCenter").AddComponent<RectTransform>();
    centerPivot.SetParent(root.transform, false);
    centerPivot.anchorMin = new Vector2(0, 0.35f); centerPivot.anchorMax = new Vector2(1, 0.75f);
    centerPivot.offsetMin = new Vector2(UIStyle.PadH + 12, 0); centerPivot.offsetMax = new Vector2(-UIStyle.PadH, 0);

    var centerVl = centerPivot.gameObject.AddComponent<VerticalLayoutGroup>();
    centerVl.childAlignment = TextAnchor.MiddleCenter;
    centerVl.spacing = 6; centerVl.childForceExpandWidth = true; centerVl.childForceExpandHeight = false;

    var scoreLabel = UIManager.CreateText(centerPivot, "ScoreLabel", "SCORE",
        9, FontStyle.Bold, UIStyle.TextDim, font, TextAnchor.MiddleCenter);
    scoreLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 14;

    _scoreText = UIManager.CreateText(centerPivot, "ScoreValue", "0m",
        48, FontStyle.Bold, Color.white, font, TextAnchor.MiddleCenter);
    _scoreText.gameObject.AddComponent<LayoutElement>().preferredHeight = 56;

    // Lava distance pill
    var lavaPill = UIManager.CreateImage(centerPivot, "LavaPill", UIStyle.SurfaceDark);
    lavaPill.gameObject.AddComponent<LayoutElement>().preferredHeight = 28;
    _lavaText = UIManager.CreateText(lavaPill.transform, "LavaText", "🌋 +0m",
        9, FontStyle.Bold, UIStyle.Danger, font, TextAnchor.MiddleCenter);
    UIManager.Stretch(_lavaText.rectTransform);

    // ── Pause button (bottom center) ────────────────────────────────────
    var pauseContainer = new GameObject("PauseContainer").AddComponent<RectTransform>();
    pauseContainer.SetParent(root.transform, false);
    pauseContainer.anchorMin = new Vector2(0.5f, 0); pauseContainer.anchorMax = new Vector2(0.5f, 0);
    pauseContainer.pivot = new Vector2(0.5f, 0); pauseContainer.sizeDelta = new Vector2(100, 36);
    pauseContainer.anchoredPosition = new Vector2(0, 14);

    var pauseImg = pauseContainer.gameObject.AddComponent<Image>();
    pauseImg.color = UIStyle.SurfaceDark;
    var pauseBtn = pauseContainer.gameObject.AddComponent<Button>(); pauseBtn.targetGraphic = pauseImg;
    var pauseTxt = UIManager.CreateText(pauseContainer, "PauseTxt", "⏸ PAUSE",
        10, FontStyle.Bold, UIStyle.TextDim, font, TextAnchor.MiddleCenter);
    UIManager.Stretch(pauseTxt.rectTransform);
    UIManager.BindButton(pauseBtn, _onPause);
}

private void SpawnMilestoneTick(Transform parent, float value, float max, Font font)
{
    float normalised = Mathf.Clamp01(value / max);
    var tick = UIManager.CreateImage(parent, $"Tick_{value}m", new Color(1, 1, 1, 0.30f));
    var rt = tick.rectTransform;
    rt.anchorMin = new Vector2(-1f, normalised); // extends left of bar
    rt.anchorMax = new Vector2(1f,  normalised);
    rt.offsetMin = new Vector2(-8, -0.5f);
    rt.offsetMax = new Vector2(0,   0.5f);
}

/// <summary>Call every frame from UIManager.SetScore(). Updates progress bar fill height.</summary>
public void SetProgressBar(float currentScore)
{
    float t = Mathf.Clamp01(currentScore / Mathf.Max(1f, _config.heightProgressMax));
    // Set fill height as fraction of track height via sizeDelta (pivot bottom)
    var trackRt = _progressFill.transform.parent.GetComponent<RectTransform>();
    float trackH = trackRt.rect.height;
    var sd = _progressFill.rectTransform.sizeDelta;
    _progressFill.rectTransform.sizeDelta = new Vector2(sd.x, trackH * t);
}
```

> **Note on coin float:** Existing `QueueRewardToast` handles coin feedback. For HUD coin gain, `UIManager` should call `SpawnCoinFloat(amount)` on `UIHudController`. Add a `SpawnCoinFloat(int amount)` method:

```csharp
public void SpawnCoinFloat(int amount, MonoBehaviour runner)
{
    if (_coinText == null) return;
    var go = new GameObject("CoinFloat");
    go.transform.SetParent(_coinText.transform.parent, false);
    var t = go.AddComponent<Text>();
    t.text = $"+{amount} 🪙";
    t.font = _coinText.font;
    t.fontSize = 11;
    t.fontStyle = FontStyle.Bold;
    t.color = UIStyle.Gold;
    t.alignment = TextAnchor.MiddleCenter;
    t.rectTransform.anchoredPosition = _coinText.rectTransform.anchoredPosition;
    t.rectTransform.sizeDelta = new Vector2(80, 24);
    runner.StartCoroutine(UIStyle.CoinFloat(t));
}
```

- [ ] **Step 4: Call SetProgressBar from UIManager**

First, confirm the update method name and HUD field name in UIManager:

```bash
grep -n "SetScore\|SetHud\|UpdateHud\|UIHudController\|hudController\|_hud" \
  Assets/Scripts/TowerMaze/Runtime/UISystems/UIManager.cs | head -20
```

In the method that updates the HUD score each frame (whatever its name is), add — substituting the actual field name found by the grep above for `<hudField>`:

```csharp
<hudField>.SetProgressBar(score);
```

- [ ] **Step 5: Enter Play Mode → start a run → verify acceptance criteria**

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/UISystems/HudController.cs
git add Assets/Scripts/TowerMaze/Runtime/ConfigData.cs
git commit -m "feat: redesign HUD — dark bg, progress bar with milestones, coin float animation"
```

---

## Chunk 3: Start Screen Redesign

### Task 5: Redesign StartScreenController — Main Menu

**Files:**

- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems/StartScreen.cs`

**Acceptance criteria:**

- Background `#2D1B69` (deep purple)
- "TOWER MAZE" white 28px centered
- START button: orange gradient, full width, pulse animation looping
- "Best: Xm" caption faint below START
- SHOP + MISSIONS buttons at 65% opacity
- 🏆 best score top-left, 🏅 leaderboard + ⚙ settings top-right (icon pills)

- [ ] **Step 1: Rewrite Initialize() visual construction**

Replace the existing `Initialize()` body of `StartScreenController`. Keep all callback parameters intact.

**Required fields** — confirm these exist on `StartScreenController` (carried verbatim from original or add them):

```csharp
// Fields required by the new Initialize() and helpers below
private MonoBehaviour _runner;       // set to UIManager at init
private Font _font;                  // stored for popup row spawning
private System.Action _onStartPressed;
private Coroutine _pulseCoroutine;
private Button _startBtn;
private Button _shopBtn, _missionsBtn; // GetComponent<Button>() from CreateSecondaryButton return value
private Button _leaderboardBtn, _settingsBtn;
private Text _bestScoreText, _captionText;
private Text _lbSubtitle;
private System.Action<System.Action<System.Collections.Generic.List<LeaderboardEntry>>> _onGetLeaderboard;
private System.Func<System.Collections.Generic.List<DailyMissionState>> _getMissions;
private System.Action<string> _onClaimMission; // carried from existing StartScreenController
private bool _isSoundOn, _isVibOn;
private GameObject _settingsPanel;
private GameObject _leaderboardPopup;
private RectTransform _leaderboardSheetRt;
private Transform _leaderboardList;
private GameObject _missionsPopup;
private RectTransform _missionsSheetRt;
private Text _countdownText;
private Transform _missionList;
```

Store `_runner` and `_font` as the first two lines of `Initialize()`:

```csharp
// Also add near top of file — data types for callbacks:
public struct LeaderboardEntry { public int rank; public string name; public int score; }
public class DailyMissionState { public string id; public string title; public int rewardCoins; public int current; public int target; public bool isComplete; }
```

Key structural construction code:

Before rewriting, confirm the existing `Initialize()` signature:

```bash
grep -n "void Initialize" Assets/Scripts/TowerMaze/Runtime/UISystems/StartScreen.cs
```

Add `MonoBehaviour runner` as a parameter if not already present. Then write:

```csharp
public void Initialize(Canvas canvas, Font font, MonoBehaviour runner, /* existing callbacks */)
{
    _runner = runner; _font = font;
    // Root — deep purple
    var root = UIManager.CreateImage(canvas.transform, "StartScreen", UIStyle.MenuBg);
    UIManager.Stretch(root.rectTransform);
    root.gameObject.SetActive(false);
    _root = root.gameObject;

    // ── Top bar (absolute) ───────────────────────────────────────────────
    var topBar = new GameObject("TopBar").AddComponent<RectTransform>();
    topBar.SetParent(root.transform, false);
    topBar.anchorMin = new Vector2(0, 1); topBar.anchorMax = new Vector2(1, 1);
    topBar.offsetMin = new Vector2(UIStyle.PadH, -50);
    topBar.offsetMax = new Vector2(-UIStyle.PadH, 0);

    // Best score (left)
    _bestScoreText = UIManager.CreateText(topBar, "BestScore", "🏆 0m",
        10, FontStyle.Bold, UIStyle.TextFaint, font, TextAnchor.MiddleLeft);
    UIManager.Stretch(_bestScoreText.rectTransform, 0, 0, 0, 0);
    _bestScoreText.rectTransform.anchorMax = new Vector2(0.5f, 1);

    // Right icons row
    var rightIcons = new GameObject("RightIcons").AddComponent<HorizontalLayoutGroup>();
    rightIcons.GetComponent<RectTransform>().SetParent(topBar, false);
    rightIcons.spacing = 7;
    rightIcons.childAlignment = TextAnchor.MiddleRight;
    rightIcons.GetComponent<RectTransform>().anchorMin = new Vector2(1, 0);
    rightIcons.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
    rightIcons.GetComponent<RectTransform>().pivot = new Vector2(1, 0.5f);
    rightIcons.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
    rightIcons.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 30);

    _leaderboardBtn = UIManager.CreateIconButton(rightIcons.transform, "LeaderboardBtn", "🏅", font, new Color(1,1,1,0.10f)).GetComponent<Button>();
    _settingsBtn    = UIManager.CreateIconButton(rightIcons.transform, "SettingsBtn",    "⚙",  font, new Color(1,1,1,0.10f)).GetComponent<Button>();

    UIManager.BindButton(_leaderboardBtn, OnLeaderboardTap);
    UIManager.BindButton(_settingsBtn,    OnSettingsTap);

    // ── Logo ─────────────────────────────────────────────────────────────
    var logo = UIManager.CreateText(root.transform, "Logo", "TOWER\nMAZE",
        28, FontStyle.Bold, Color.white, font, TextAnchor.MiddleCenter);
    logo.lineSpacing = 1.15f;
    var logoRt = logo.rectTransform;
    logoRt.anchorMin = new Vector2(0.5f, 0.5f); logoRt.anchorMax = new Vector2(0.5f, 0.5f);
    logoRt.pivot = new Vector2(0.5f, 0.5f);
    logoRt.anchoredPosition = new Vector2(0, 60);
    logoRt.sizeDelta = new Vector2(240, 80);

    // ── START button ─────────────────────────────────────────────────────
    _startBtn = UIManager.CreateActionButton(root.transform, "StartBtn", "START",
        font, UIStyle.Action, UIStyle.ActionLight, 56, UIStyle.PadH).GetComponent<Button>();
    var startRt = _startBtn.GetComponent<RectTransform>();
    startRt.anchorMin = new Vector2(0, 0.5f); startRt.anchorMax = new Vector2(1, 0.5f);
    startRt.offsetMin = new Vector2(UIStyle.PadH, -28);
    startRt.offsetMax = new Vector2(-UIStyle.PadH, 28);
    startRt.anchoredPosition = new Vector2(0, -10);
    UIManager.BindButton(_startBtn, OnStartTap);
    _pulseCoroutine = _runner.StartCoroutine(UIStyle.Pulse(startRt));

    // ── Best score caption ────────────────────────────────────────────────
    _captionText = UIManager.CreateText(root.transform, "BestCaption", "Best: 0m",
        10, FontStyle.Normal, UIStyle.TextFaint, font, TextAnchor.MiddleCenter);
    // Position: centered, 18px below the START button bottom edge (START center Y = -10, half-height 28 → bottom at -38; caption top at -46)
    var captionRt = _captionText.rectTransform;
    captionRt.anchorMin = new Vector2(0, 0.5f); captionRt.anchorMax = new Vector2(1, 0.5f);
    captionRt.pivot = new Vector2(0.5f, 1f); // top-anchored pivot so we measure from top
    captionRt.anchoredPosition = new Vector2(0, -56); // -10 (START center) - 28 (half height) - 18 (gap) = -56
    captionRt.sizeDelta = new Vector2(0, 20);

    // ── Secondary row (Shop + Missions) at 65% opacity ───────────────────
    var secRow = new GameObject("SecRow").AddComponent<HorizontalLayoutGroup>();
    secRow.spacing = UIStyle.GridGap;
    var secRt = secRow.GetComponent<RectTransform>();
    secRt.SetParent(root.transform, false);
    secRt.anchorMin = new Vector2(0, 0); secRt.anchorMax = new Vector2(1, 0);
    secRt.offsetMin = new Vector2(UIStyle.PadH, 24);
    secRt.offsetMax = new Vector2(-UIStyle.PadH, 68);

    var secGroup = secRow.gameObject.AddComponent<CanvasGroup>();
    secGroup.alpha = 0.65f;

    _shopBtn     = CreateSecondaryButton(secRow.transform, "ShopBtn",     "SHOP",         font).GetComponent<Button>();
    _missionsBtn = CreateSecondaryButton(secRow.transform, "MissionsBtn", "📋 MISSIONS", font).GetComponent<Button>();
    UIManager.BindButton(_shopBtn, OnShopTap);
    UIManager.BindButton(_missionsBtn, OnMissionsTap);
}

// ─────────────────────────────────────────────────────────────────────────────
// THE TWO METHODS BELOW MUST BE PLACED IN UIManager.cs (NOT in StartScreen.cs).
// Add them as public static methods on the UIManager class in
// Assets/Scripts/TowerMaze/Runtime/UISystems/UIManager.cs.
// All callers (HudController, FailScreen, StartScreen) call UIManager.CreateIconButton(...)
// and UIManager.CreateActionButton(...).
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Creates a 26×26px icon pill button (used for ⚙ and 🏅 in top bars).</summary>
public static GameObject CreateIconButton(Transform parent, string name, string icon, Font font, Color bgColor)
{
    var go = new GameObject(name);
    go.transform.SetParent(parent, false);
    var img = go.AddComponent<Image>(); img.color = bgColor; img.raycastTarget = true;
    var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
    var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(26, 26);
    var lbl = UIManager.CreateText(go.transform, "Icon", icon,
        13, FontStyle.Normal, Color.white, font, TextAnchor.MiddleCenter);
    UIManager.Stretch(lbl.rectTransform);
    return go;
}

/// <summary>Creates a gradient action button (START/CONTINUE/CONTINUE/CLAIM style).</summary>
/// NOTE: Defined as public static on UIManager so FailScreenController can also call UIManager.CreateActionButton(...)
public static GameObject CreateActionButton(Transform parent, string name, string label,
    Font font, Color colorA, Color colorB, int height, int padH)
{
    var go = new GameObject(name);
    go.transform.SetParent(parent, false);
    var img = go.AddComponent<Image>();
    img.color = colorA; // gradient approximated with flat action color
    // Note: UGUI Image doesn't support linear gradient natively.
    // Use a custom Shader or a two-color approach:
    //   Option A: Use a 2px wide gradient Texture2D (action→actionLight, horizontal).
    //   Option B: Overlay a semi-transparent white Image on top right half.
    // Simplest path: flat Action color — visual difference from colorA→colorB is subtle.
    img.raycastTarget = true;
    var btn = go.AddComponent<Button>();
    btn.targetGraphic = img;

    var rt = go.GetComponent<RectTransform>();
    rt.sizeDelta = new Vector2(0, height);

    var lbl = UIManager.CreateText(go.transform, "Label", label,
        15, FontStyle.Bold, Color.white, font, TextAnchor.MiddleCenter);
    UIManager.Stretch(lbl.rectTransform);

    // Subtle shadow via second Image behind
    // (optional — skip for initial implementation, add in polish pass)

    UIManager.BindButton(btn, () => { });
    return go;
}

/// <summary>Secondary button: semi-transparent dark bg, dim text.</summary>
/// NOTE: Promoted to public static on UIManager so PopupControllers can also call UIManager.CreateSecondaryButton(...)
public static GameObject CreateSecondaryButton(Transform parent, string name, string label, Font font)
{
    var go = new GameObject(name);
    go.transform.SetParent(parent, false);
    var img = go.AddComponent<Image>();
    img.color = new Color(1, 1, 1, 0.10f);
    img.raycastTarget = true;
    var btn = go.AddComponent<Button>();
    btn.targetGraphic = img;
    go.AddComponent<LayoutElement>().flexibleWidth = 1;

    var lbl = UIManager.CreateText(go.transform, "Label", label,
        10, FontStyle.Bold, new Color(1, 1, 1, 0.70f), font, TextAnchor.MiddleCenter);
    UIManager.Stretch(lbl.rectTransform);

    var rt = go.GetComponent<RectTransform>();
    rt.sizeDelta = new Vector2(0, 44);
    return go;
}
```

- [ ] **Step 2: Add all tap handler bodies**

```csharp
private void OnStartTap()
{
    _runner.StartCoroutine(UIStyle.ButtonPress(_startBtn.GetComponent<RectTransform>()));
    _onStartPressed?.Invoke();
}
private void OnShopTap()
{
    _runner.StartCoroutine(UIStyle.ButtonPress(_shopBtn.GetComponent<RectTransform>()));
    _onShopPressed?.Invoke(); // carried from existing StartScreenController
}
private void OnMissionsTap()
{
    _runner.StartCoroutine(UIStyle.ButtonPress((RectTransform)_missionsBtn.transform));
    OpenMissionsPopup();
}
private void OnLeaderboardTap() => OpenLeaderboardPopup();
private void OnSettingsTap()    => OpenSettings();
private void OnClaimMission(string id) { _onClaimMission?.Invoke(id); } // carried callback
```

- [ ] **Step 3: Enter Play Mode → verify acceptance criteria**

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/UISystems/StartScreen.cs
git commit -m "feat: redesign main menu — deep purple bg, pulsing START, secondary buttons at 65%"
```

---

### Task 6: Add Leaderboard Popup to StartScreen

**Files:**

- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems/StartScreen.cs`

**Acceptance criteria:** Tap 🏅 → bottom sheet slides up with dark bg, top runs list, own rank highlighted in purple.

- [ ] **Step 1: Add `BuildLeaderboardPopup()` method**

// Fields already declared in Task 5 required-fields block — do NOT re-declare.

```csharp
private void BuildLeaderboardPopup(Canvas canvas, Font font)
{
    // Dark overlay
    var overlay = UIManager.CreateImage(canvas.transform, "LBOverlay", new Color(0, 0, 0, 0.50f));
    UIManager.Stretch(overlay.rectTransform);
    overlay.gameObject.SetActive(false);
    _leaderboardPopup = overlay.gameObject;

    var overlayBtn = overlay.gameObject.AddComponent<Button>();
    UIManager.BindButton(overlayBtn, CloseLeaderboardPopup);

    // Bottom sheet (dark)
    var sheet = UIManager.CreateImage(overlay.transform, "LBSheet", UIStyle.ShopBg);
    sheet.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    _leaderboardSheetRt = sheet.rectTransform;
    _leaderboardSheetRt.anchorMin = new Vector2(0, 0); _leaderboardSheetRt.anchorMax = new Vector2(1, 0);
    _leaderboardSheetRt.pivot = new Vector2(0.5f, 0f);
    _leaderboardSheetRt.offsetMin = Vector2.zero; _leaderboardSheetRt.offsetMax = new Vector2(0, 0);

    var vl = sheet.gameObject.AddComponent<VerticalLayoutGroup>();
    vl.padding = new RectOffset(UIStyle.PadH, UIStyle.PadH, 16, 24);
    vl.spacing = 10;
    vl.childForceExpandWidth = true;
    vl.childForceExpandHeight = false;

    // Handle bar
    var handle = UIManager.CreateImage(sheet.transform, "Handle", new Color(1, 1, 1, 0.15f));
    handle.rectTransform.sizeDelta = new Vector2(32, 4);
    var hle = handle.gameObject.AddComponent<LayoutElement>(); hle.preferredHeight = 4; hle.ignoreLayout = false;

    // Title
    UIManager.CreateText(sheet.transform, "LBTitle", "🏅 Top Runs",
        14, FontStyle.Bold, Color.white, font, TextAnchor.MiddleLeft);

    _lbSubtitle = UIManager.CreateText(sheet.transform, "LBSub", "Sen: — · —",
        10, FontStyle.Normal, UIStyle.TextDim, font, TextAnchor.MiddleLeft);

    // List container
    var listGo = new GameObject("LBList");
    listGo.transform.SetParent(sheet.transform, false);
    var listVl = listGo.AddComponent<VerticalLayoutGroup>();
    listVl.spacing = 6; listVl.childForceExpandWidth = true; listVl.childForceExpandHeight = false;
    listGo.AddComponent<LayoutElement>().flexibleWidth = 1;
    _leaderboardList = listGo.transform;

    // Close button
    var closeBtn = CreateSecondaryButton(sheet.transform, "CloseBtn", "Kapat", font);
    UIManager.BindButton(closeBtn.GetComponent<Button>(), CloseLeaderboardPopup);
}

private void OpenLeaderboardPopup()
{
    _leaderboardPopup.SetActive(true);
    _leaderboardSheetRt.anchoredPosition = new Vector2(0, -400);
    RefreshLeaderboard();
    _runner.StartCoroutine(UIStyle.SlideUp(_leaderboardSheetRt, 400));
}

private void CloseLeaderboardPopup()
{
    _runner.StartCoroutine(CloseLeaderboardCoroutine());
}

private IEnumerator CloseLeaderboardCoroutine()
{
    yield return UIStyle.SlideDown(_leaderboardSheetRt, 400);
    _leaderboardPopup.SetActive(false);
}

private void RefreshLeaderboard()
{
    // Clear existing rows
    foreach (Transform child in _leaderboardList) Object.Destroy(child.gameObject);

    _onGetLeaderboard?.Invoke(entries =>
    {
        if (entries == null) return;
        string localName = PlayerPrefs.GetString("PlayerName", "Sen");

        foreach (var entry in entries)
        {
            bool isSelf = entry.name == localName;

            // Row card
            var row = new GameObject($"LBRow_{entry.rank}");
            row.transform.SetParent(_leaderboardList, false);
            var rowImg = row.AddComponent<Image>();
            rowImg.color = isSelf
                ? new Color(UIStyle.Brand.r, UIStyle.Brand.g, UIStyle.Brand.b, 0.15f)
                : UIStyle.SurfaceDark;
            row.AddComponent<LayoutElement>().preferredHeight = 40;

            var hl = row.AddComponent<HorizontalLayoutGroup>();
            hl.padding = new RectOffset(12, 12, 0, 0);
            hl.spacing = 8;
            hl.childAlignment = TextAnchor.MiddleLeft;
            hl.childForceExpandWidth = false;
            hl.childForceExpandHeight = true;

            // Rank "#1"
            var rankText = UIManager.CreateText(row.transform, "Rank", $"#{entry.rank}",
                10, FontStyle.Bold,
                isSelf ? UIStyle.Brand : UIStyle.TextDim,
                _font, TextAnchor.MiddleLeft);
            rankText.gameObject.AddComponent<LayoutElement>().preferredWidth = 28;

            // Name (flex)
            var nameText = UIManager.CreateText(row.transform, "Name", entry.name,
                10, FontStyle.Normal,
                isSelf ? Color.white : UIStyle.TextPrimary,
                _font, TextAnchor.MiddleLeft);
            nameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            // Score
            var scoreText = UIManager.CreateText(row.transform, "Score", $"{entry.score}m",
                10, FontStyle.Bold,
                isSelf ? UIStyle.Brand : Color.white,
                _font, TextAnchor.MiddleRight);
            scoreText.gameObject.AddComponent<LayoutElement>().preferredWidth = 48;
        }

        // Update "Sen: Rank · Score" subtitle
        var self = entries.Find(e => e.name == localName);
        if (_lbSubtitle != null)
            _lbSubtitle.text = self != null ? $"Sen: #{self.rank} · {self.score}m" : "Sen: —";
    });
}
```

- [ ] **Step 2: Call BuildLeaderboardPopup in Initialize(), after main menu construction**

- [ ] **Step 3: Wire OnLeaderboardTap → OpenLeaderboardPopup**

- [ ] **Step 4: Enter Play Mode → tap 🏅 → verify popup slides up → tap overlay → verify closes**

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/UISystems/StartScreen.cs
git commit -m "feat: add leaderboard bottom sheet popup to main menu"
```

---

### Task 7: Add Missions Popup to StartScreen

**Files:**

- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems/StartScreen.cs`

**Acceptance criteria:** Tap 📋 MISSIONS → bottom sheet with max 2 missions, progress bars, countdown timer, CLAIM button for completed missions.

- [ ] **Step 1: Add `BuildMissionsPopup()` method**

// Fields already declared in Task 5 required-fields block — do NOT re-declare.

```csharp
private void BuildMissionsPopup(Canvas canvas, Font font)
{
    // Overlay + sheet — same pattern as leaderboard
    var overlay = UIManager.CreateImage(canvas.transform, "MissionsOverlay", new Color(0, 0, 0, 0.50f));
    UIManager.Stretch(overlay.rectTransform);
    overlay.gameObject.SetActive(false);
    _missionsPopup = overlay.gameObject;
    UIManager.BindButton(overlay.gameObject.AddComponent<Button>(), CloseMissionsPopup);

    var sheet = UIManager.CreateImage(overlay.transform, "MissionsSheet", UIStyle.ShopBg);
    _missionsSheetRt = sheet.rectTransform;
    _missionsSheetRt.anchorMin = new Vector2(0, 0); _missionsSheetRt.anchorMax = new Vector2(1, 0);
    _missionsSheetRt.pivot = new Vector2(0.5f, 0f);
    sheet.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

    var vl = sheet.gameObject.AddComponent<VerticalLayoutGroup>();
    vl.padding = new RectOffset(UIStyle.PadH, UIStyle.PadH, 16, 24);
    vl.spacing = 10; vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;

    // Header row: title + countdown
    var headerRow = new GameObject("Header").AddComponent<HorizontalLayoutGroup>();
    headerRow.transform.SetParent(sheet.transform, false);
    headerRow.childForceExpandWidth = false; headerRow.childForceExpandHeight = true;
    UIManager.CreateText(headerRow.transform, "Title", "📋 Daily Missions",
        14, FontStyle.Bold, Color.white, font, TextAnchor.MiddleLeft)
        .gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
    _countdownText = UIManager.CreateText(headerRow.transform, "Countdown", "23:59:59",
        10, FontStyle.Normal, UIStyle.TextDim, font, TextAnchor.MiddleRight);

    UIManager.CreateText(sheet.transform, "Subtitle", "Her gün sıfırlanır",
        10, FontStyle.Normal, UIStyle.TextDim, font, TextAnchor.MiddleLeft);

    // Mission list (max 2 items shown)
    var listGo = new GameObject("MissionList");
    listGo.transform.SetParent(sheet.transform, false);
    var lvl = listGo.AddComponent<VerticalLayoutGroup>();
    lvl.spacing = 8; lvl.childForceExpandWidth = true; lvl.childForceExpandHeight = false;
    _missionList = listGo.transform;
}

private void SpawnMissionRow(DailyMissionState mission, Font font)
{
    var card = UIManager.CreateImage(_missionList, $"Mission_{mission.id}", UIStyle.SurfaceDark);
    card.rectTransform.sizeDelta = new Vector2(0, 0);
    var cardVl = card.gameObject.AddComponent<VerticalLayoutGroup>();
    cardVl.padding = new RectOffset(12, 12, 10, 10);
    cardVl.spacing = 6; cardVl.childForceExpandWidth = true;
    card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

    // Row: title + reward chip
    var topRow = new GameObject("TopRow").AddComponent<HorizontalLayoutGroup>();
    topRow.transform.SetParent(card.transform, false);
    topRow.childForceExpandWidth = false; topRow.childForceExpandHeight = false;

    UIManager.CreateText(topRow.transform, "MTitle", mission.title,
        11, FontStyle.Bold, Color.white, font, TextAnchor.MiddleLeft)
        .gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

    // Reward chip
    var chip = UIManager.CreateImage(topRow.transform, "RewardChip", new Color(UIStyle.Gold.r, UIStyle.Gold.g, UIStyle.Gold.b, 0.15f));
    chip.gameObject.AddComponent<LayoutElement>().preferredWidth = 60;
    var chipText = UIManager.CreateText(chip.transform, "ChipText", $"+{mission.rewardCoins}🪙",
        9, FontStyle.Bold, UIStyle.Gold, font, TextAnchor.MiddleCenter);
    UIManager.Stretch(chipText.rectTransform);

    // Progress text
    UIManager.CreateText(card.transform, "Progress", $"{mission.current}/{mission.target} tamamlandı",
        9, FontStyle.Normal, UIStyle.TextDim, font, TextAnchor.MiddleLeft);

    // Progress bar
    var barTrack = UIManager.CreateImage(card.transform, "BarTrack", new Color(1, 1, 1, 0.08f));
    barTrack.rectTransform.sizeDelta = new Vector2(0, 4);
    barTrack.gameObject.AddComponent<LayoutElement>().preferredHeight = 4;
    float fill = mission.target > 0 ? Mathf.Clamp01((float)mission.current / mission.target) : 0;
    var barFill = UIManager.CreateImage(barTrack.transform, "BarFill", mission.isComplete ? UIStyle.Owned : UIStyle.Brand);
    // Use normalized anchors (not offsetMax) — rect.width is 0 at construction time.
    var fillRt = barFill.rectTransform;
    fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = new Vector2(fill, 1);
    fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;

    // Claim button (only if complete)
    if (mission.isComplete)
    {
        var claimBtn = UIManager.CreateActionButton(card.transform, "ClaimBtn", "CLAIM ✓", font, UIStyle.Owned, UIStyle.Owned, 36, 0);
        UIManager.BindButton(claimBtn.GetComponent<Button>(), () => OnClaimMission(mission.id));
    }
}

private void OpenMissionsPopup()
{
    _missionsPopup.SetActive(true);
    _missionsSheetRt.anchoredPosition = new Vector2(0, -500);
    // Clear and repopulate mission list (max 2)
    foreach (Transform c in _missionList) Object.Destroy(c.gameObject);
    var missions = _getMissions?.Invoke() ?? new List<DailyMissionState>();
    for (int i = 0; i < Mathf.Min(2, missions.Count); i++)
        SpawnMissionRow(missions[i], _font);
    _runner.StartCoroutine(UIStyle.SlideUp(_missionsSheetRt, 500));
}

private void CloseMissionsPopup()
{
    _runner.StartCoroutine(CloseMissionsCoroutine());
}

private IEnumerator CloseMissionsCoroutine()
{
    yield return UIStyle.SlideDown(_missionsSheetRt, 500);
    _missionsPopup.SetActive(false);
}
```

- [ ] **Step 2: Call `BuildMissionsPopup` from `Initialize()` and wire `OnMissionsTap`**

In `StartScreenController.Initialize()`, after the `BuildLeaderboardPopup(canvas, font)` call, add:

```csharp
BuildMissionsPopup(canvas, font);
```

`OnMissionsTap` (defined in Task 5 Step 2) already calls `OpenMissionsPopup()` — no additional wiring needed.

- [ ] **Step 3: Add `UpdateMissionCountdown` method to `StartScreenController`**

Add this method to `StartScreen.cs`:

```csharp
/// <summary>Called by UIManager each frame/second to keep the reset timer current.</summary>
public void UpdateMissionCountdown(TimeSpan remaining)
{
    if (_countdownText == null) return;
    _countdownText.text = $"{(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
}
```

- [ ] **Step 4: Call UpdateMissionCountdown from UIManager's Update loop**

In `UIManager.cs`, find the update tick. First locate where the mission reset timer is computed:

```bash
grep -n "mission\|Mission\|DailyMission\|resetTime\|SecondsUntil" \
  Assets/Scripts/TowerMaze/Runtime/UISystems/UIManager.cs | head -20
```

Then add (substituting the actual time-source expression for `<remainingSeconds>`):

```csharp
// <remainingSeconds> = e.g. MissionManager.Instance.SecondsUntilReset
//                      or   _missionResetTime - Time.time
//                      (use whatever the existing UIManager already computes)
startScreenController?.UpdateMissionCountdown(TimeSpan.FromSeconds(<remainingSeconds>));
```

- [ ] **Step 5: Enter Play Mode → tap 📋 → verify popup with mission cards, progress bars**

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/UISystems/StartScreen.cs
git commit -m "feat: add daily missions bottom sheet popup to main menu"
```

---

### Task 8: Redesign Settings Panel

**Files:**

- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems/StartScreen.cs`

**Acceptance criteria:** Tap ⚙ → dark panel slides in from right; sound/vibration toggles use dark cards with brand-color active state; language buttons secondary style; ⚙ closes panel.

- [ ] **Step 1: Rewrite `BuildSettingsPanel()`**

The existing settings panel uses a light-colored panel. Update to dark theme:

- Panel background: `UIStyle.ShopBg` (`#1A0A35`)
- Toggle labels: `UIStyle.TextPrimary`
- Active toggle state: `UIStyle.Brand` bg, white text
- Inactive toggle: `UIStyle.SurfaceDark` bg, `UIStyle.TextDim` text
- Language buttons: same toggle pattern (TR / EN / ES)
- Panel slide animation: `anchoredPositionX` from `+screenWidth` → `0` over 0.25s

```csharp
private void BuildSettingsPanel(Canvas canvas, Font font)
{
    var panel = UIManager.CreateImage(canvas.transform, "SettingsPanel", UIStyle.ShopBg);
    var panelRt = panel.rectTransform;
    panelRt.anchorMin = new Vector2(1, 0); panelRt.anchorMax = new Vector2(1, 1);
    panelRt.pivot = new Vector2(1, 0.5f);
    panelRt.sizeDelta = new Vector2(240, 0);
    panelRt.anchoredPosition = new Vector2(240, 0); // pre-positioned off right edge; OpenSettings() animates to (0,0)
    panel.gameObject.SetActive(false);
    _settingsPanel = panel.gameObject;

    var vl = panel.gameObject.AddComponent<VerticalLayoutGroup>();
    vl.padding = new RectOffset(16, 16, 24, 16);
    vl.spacing = 12; vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;

    UIManager.CreateText(panel.transform, "Title", "Ayarlar",
        16, FontStyle.Bold, Color.white, font, TextAnchor.MiddleLeft);

    // Toggle row helper
    SpawnToggleRow(panel.transform, "Ses", font, _isSoundOn, v => SetSound(v));
    SpawnToggleRow(panel.transform, "Titreşim", font, _isVibOn, v => SetVib(v));

    // Language section
    UIManager.CreateText(panel.transform, "LangLabel", "DİL",
        9, FontStyle.Bold, UIStyle.TextFaint, font, TextAnchor.MiddleLeft);
    var langRow = new GameObject("LangRow").AddComponent<HorizontalLayoutGroup>();
    langRow.transform.SetParent(panel.transform, false);
    langRow.spacing = 6; langRow.childForceExpandWidth = true;
    SpawnLangButton(langRow.transform, "TR", font);
    SpawnLangButton(langRow.transform, "EN", font);
    SpawnLangButton(langRow.transform, "ES", font);
}

private void OpenSettings()
{
    _settingsPanel.SetActive(true);
    var rt = _settingsPanel.GetComponent<RectTransform>();
    rt.anchoredPosition = new Vector2(240, 0);
    _runner.StartCoroutine(SlideInFromRight(rt, 240));
}

private IEnumerator SlideInFromRight(RectTransform rt, float width)
{
    float t = 0f;
    while (t < 0.25f) { rt.anchoredPosition = new Vector2(Mathf.Lerp(width, 0, Mathf.SmoothStep(0, 1, t / 0.25f)), 0); t += Time.deltaTime; yield return null; }
    rt.anchoredPosition = Vector2.zero;
}

private void CloseSettings()
{
    _runner.StartCoroutine(CloseSettingsCoroutine());
}

private IEnumerator CloseSettingsCoroutine()
{
    var rt = _settingsPanel.GetComponent<RectTransform>();
    float t = 0f, width = 240f;
    while (t < 0.20f) { rt.anchoredPosition = new Vector2(Mathf.Lerp(0, width, Mathf.SmoothStep(0, 1, t / 0.20f)), 0); t += Time.deltaTime; yield return null; }
    rt.anchoredPosition = new Vector2(width, 0);
    _settingsPanel.SetActive(false);
}

/// <summary>Spawns a labelled ON/OFF toggle row inside the settings panel.</summary>
private void SpawnToggleRow(Transform parent, string label, Font font, bool initialValue, System.Action<bool> onChange)
{
    var row = new GameObject($"Toggle_{label}").AddComponent<HorizontalLayoutGroup>();
    row.transform.SetParent(parent, false);
    row.spacing = 8; row.childForceExpandHeight = true; row.childForceExpandWidth = false;
    row.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 36);
    row.gameObject.AddComponent<LayoutElement>().preferredHeight = 36;

    // Label
    var lbl = UIManager.CreateText(row.transform, "Lbl", label,
        11, FontStyle.Bold, UIStyle.TextPrimary, font, TextAnchor.MiddleLeft);
    lbl.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

    // ON chip
    var onGo = new GameObject("ON");
    onGo.transform.SetParent(row.transform, false);
    var onImg = onGo.AddComponent<Image>();
    onImg.color = initialValue ? UIStyle.Brand : UIStyle.SurfaceDark;
    onGo.AddComponent<LayoutElement>().preferredWidth = 36;
    var onBtn = onGo.AddComponent<Button>(); onBtn.targetGraphic = onImg;
    var onTxt = UIManager.CreateText(onGo.transform, "T", "ON",
        9, FontStyle.Bold, initialValue ? Color.white : UIStyle.TextDim, font, TextAnchor.MiddleCenter);
    UIManager.Stretch(onTxt.rectTransform);

    // OFF chip
    var offGo = new GameObject("OFF");
    offGo.transform.SetParent(row.transform, false);
    var offImg = offGo.AddComponent<Image>();
    offImg.color = !initialValue ? UIStyle.Brand : UIStyle.SurfaceDark;
    offGo.AddComponent<LayoutElement>().preferredWidth = 36;
    var offBtn = offGo.AddComponent<Button>(); offBtn.targetGraphic = offImg;
    var offTxt = UIManager.CreateText(offGo.transform, "T", "OFF",
        9, FontStyle.Bold, !initialValue ? Color.white : UIStyle.TextDim, font, TextAnchor.MiddleCenter);
    UIManager.Stretch(offTxt.rectTransform);

    UIManager.BindButton(onBtn, () =>
    {
        onImg.color = UIStyle.Brand;    onTxt.color = Color.white;
        offImg.color = UIStyle.SurfaceDark; offTxt.color = UIStyle.TextDim;
        onChange?.Invoke(true);
    });
    UIManager.BindButton(offBtn, () =>
    {
        offImg.color = UIStyle.Brand;   offTxt.color = Color.white;
        onImg.color = UIStyle.SurfaceDark;  onTxt.color = UIStyle.TextDim;
        onChange?.Invoke(false);
    });
}

/// <summary>Spawns a language chip button inside the lang row.</summary>
private void SpawnLangButton(Transform parent, string code, Font font)
{
    var go = new GameObject($"Lang_{code}");
    go.transform.SetParent(parent, false);
    bool isCurrent = PlayerPrefs.GetString("Language", "TR") == code;
    var img = go.AddComponent<Image>();
    img.color = isCurrent ? UIStyle.Brand : UIStyle.SurfaceDark;
    go.AddComponent<LayoutElement>().preferredHeight = 32;
    var btn = go.AddComponent<Button>(); btn.targetGraphic = img;

    var txt = UIManager.CreateText(go.transform, "T", code,
        10, FontStyle.Bold, isCurrent ? Color.white : UIStyle.TextDim, font, TextAnchor.MiddleCenter);
    UIManager.Stretch(txt.rectTransform);

    UIManager.BindButton(btn, () =>
    {
        PlayerPrefs.SetString("Language", code);
        PlayerPrefs.Save();
        // Refresh all lang button visuals by rebuilding the panel
        // (simplest: close and reopen; full visual refresh is a polish-pass improvement)
        CloseSettings();
        OpenSettings();
    });
}

// ── Class-level methods below — NOT nested inside BuildSettingsPanel ──────────

private void SetSound(bool on)
{
    _isSoundOn = on;
    // Route to the existing sound-toggle system. Confirm the actual call with:
    // grep -n "AudioListener\|SoundManager\|muteSound\|soundEnabled" UISystems/UIManager.cs
    AudioListener.volume = on ? 1f : 0f; // fallback if no SoundManager exists
}

private void SetVib(bool on)
{
    _isVibOn = on;
    PlayerPrefs.SetInt("VibEnabled", on ? 1 : 0);
    PlayerPrefs.Save();
}
```

- [ ] **Step 2: Wire `OnSettingsTap → OpenSettings` and ⚙ close button inside panel → CloseSettings**

In `BuildSettingsPanel`, after constructing all content, add a close button at the bottom:

```csharp
var closeBtn = CreateSecondaryButton(panel.transform, "CloseSettings", "✕ Kapat", font);
UIManager.BindButton(closeBtn.GetComponent<Button>(), CloseSettings);
```

- [ ] **Step 3: Call `BuildSettingsPanel` from `Initialize()`**

In `StartScreenController.Initialize()`, after the `BuildMissionsPopup(canvas, font)` call, load saved preferences and build the panel:

```csharp
// Load persisted toggle state before building panel so initial UI matches saved values.
_isSoundOn = AudioListener.volume > 0f;      // or SoundManager.IsEnabled if one exists
_isVibOn   = PlayerPrefs.GetInt("VibEnabled", 1) == 1;
BuildSettingsPanel(canvas, font);
```

- [ ] **Step 4: Enter Play Mode → tap ⚙ → verify dark panel, toggles, language buttons; tap ✕ → panel slides out**

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/UISystems/StartScreen.cs
git commit -m "feat: redesign settings panel with dark theme"
```

---

## Chunk 4: Fail Screen Redesign

### Task 9: Redesign FailScreenController

**Files:**

- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems/FailScreen.cs`

**Acceptance criteria:**

- Background `#1A0F2E` (dark purple)
- "YOU MELTED" / "ERİDİN" / "TE DERRETISTE" caption (faint, per current locale)
- Score 56px purple, enters with pop animation (0.4s spring)
- Stats card: dark surface, best score + gold coins
- CONTINUE button orange gradient, 60px, dominant
- "Retry" plain text link with 44px invisible tap area
- On screen entry: one-shot red pulse overlay

- [ ] **Step 1: Add localised title lookup**

```csharp
private static string GetMeltedTitle()
{
    return Application.systemLanguage switch
    {
        SystemLanguage.Turkish => "ERİDİN",
        SystemLanguage.Spanish => "TE DERRETISTE",
        _ => "YOU MELTED"
    };
}
```

> Note: If the game has its own locale system (TR/EN/ES toggle in settings), use that instead of `Application.systemLanguage`.

- [ ] **Step 2: Rewrite Initialize() visual construction**

Add `MonoBehaviour runner` as a parameter if not already present. The first line of `Initialize()` must be `_runner = runner;` — place it before any other code so `_runner.StartCoroutine(...)` calls later in the body work correctly.

```csharp
public void Initialize(Canvas canvas, Font font, MonoBehaviour runner, /* existing callbacks */)
{
    _runner = runner;

    // Root — dark fail bg
    var root = UIManager.CreateImage(canvas.transform, "FailScreen", UIStyle.FailBg);
    UIManager.Stretch(root.rectTransform);
    root.gameObject.SetActive(false);
    _root = root.gameObject;

    // Danger pulse overlay (sits on top, pointer pass-through, starts alpha=0)
    _pulseOverlay = UIManager.CreateImage(root.transform, "PulseOverlay",
        new Color(UIStyle.Danger.r, UIStyle.Danger.g, UIStyle.Danger.b, 0));
    UIManager.Stretch(_pulseOverlay.rectTransform);
    _pulseOverlay.raycastTarget = false;

    // Center container
    var center = new GameObject("Center").AddComponent<VerticalLayoutGroup>();
    center.transform.SetParent(root.transform, false);
    center.childAlignment = TextAnchor.MiddleCenter;
    center.spacing = 10; center.childForceExpandWidth = true; center.childForceExpandHeight = false;
    var centerRt = center.GetComponent<RectTransform>();
    centerRt.anchorMin = new Vector2(0, 0.5f); centerRt.anchorMax = new Vector2(1, 0.5f);
    centerRt.offsetMin = new Vector2(UIStyle.PadH, -200);
    centerRt.offsetMax = new Vector2(-UIStyle.PadH, 200);

    // YOU MELTED title
    UIManager.CreateText(center.transform, "Title", GetMeltedTitle(),
        10, FontStyle.Bold, UIStyle.TextFaint, font, TextAnchor.MiddleCenter);
    // Note: letter-spacing not supported on legacy Text — omit or upgrade to TextMeshPro in a polish pass.

    // Score (56px, pop animation target)
    _scoreText = UIManager.CreateText(center.transform, "Score", "0m",
        56, FontStyle.Bold, UIStyle.Brand, font, TextAnchor.MiddleCenter);
    _scoreText.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

    // Stats card
    var statsCard = UIManager.CreateImage(center.transform, "StatsCard", UIStyle.SurfaceDark);
    statsCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 70;
    var statsVl = statsCard.gameObject.AddComponent<VerticalLayoutGroup>();
    statsVl.padding = new RectOffset(14, 14, 11, 11);
    statsVl.spacing = 6; statsVl.childForceExpandWidth = true;

    // Best row — store value Text so Show() can update it
    _bestRow = SpawnStatRow(statsCard.transform, "Best", "0m", Color.white, font);
    // Divider
    var div = UIManager.CreateImage(statsCard.transform, "Div", UIStyle.BorderDark);
    div.gameObject.AddComponent<LayoutElement>().preferredHeight = 1;
    // Coins row — store value Text so Show() can update it
    _coinsRow = SpawnStatRow(statsCard.transform, "Coins", "+0 🪙", UIStyle.Gold, font);

    // CONTINUE button (orange, 60px)
    _continueBtn = UIManager.CreateActionButton(center.transform, "ContinueBtn", "▶ CONTINUE (Watch Ad)",
        font, UIStyle.Action, UIStyle.ActionLight, 60, 0);
    UIManager.BindButton(_continueBtn.GetComponent<Button>(), OnContinueTap);
    _runner.StartCoroutine(UIStyle.Pulse(_continueBtn.GetComponent<RectTransform>()));

    // Retry — plain text with invisible 44px tap area
    var retryContainer = new GameObject("RetryContainer");
    retryContainer.transform.SetParent(center.transform, false);
    retryContainer.AddComponent<LayoutElement>().preferredHeight = 44;
    var retryImg = retryContainer.AddComponent<Image>();
    retryImg.color = Color.clear;
    retryImg.raycastTarget = true;
    var retryBtn = retryContainer.AddComponent<Button>();
    UIManager.BindButton(retryBtn, OnRetryTap);

    var retryText = UIManager.CreateText(retryContainer.transform, "RetryText", "Retry",
        11, FontStyle.Bold, UIStyle.TextFaint, font, TextAnchor.MiddleCenter);
    UIManager.Stretch(retryText.rectTransform);
}
```

- [ ] **Step 3: Add `SpawnStatRow` helper and required fields; override `Show()`**

`_bestRow` and `_coinsRow` above are the value `Text` returned by `SpawnStatRow`. Add these fields and the helper, then override `Show()`.

Confirm the existing `Initialize()` signature first:

```bash
grep -n "void Initialize" Assets/Scripts/TowerMaze/Runtime/UISystems/FailScreen.cs
```

Add `MonoBehaviour runner` as a parameter if not already present, and store `_runner = runner;` as the first line of `Initialize()`.

Fields required on `FailScreenController`:

```csharp
// Fields on FailScreenController
private Text _scoreText;
private Text _bestRow;    // value Text from SpawnStatRow("Best", ...)
private Text _coinsRow;   // value Text from SpawnStatRow("Coins", ...)
private Image _pulseOverlay;
private GameObject _continueBtn;
private MonoBehaviour _runner;
private System.Action _onContinue;
private System.Action _onRetry;

/// <summary>Creates a label + value row inside the stats card. Returns the value Text.</summary>
private Text SpawnStatRow(Transform parent, string label, string defaultValue, Color valueColor, Font font)
{
    var row = new GameObject($"Row_{label}");
    row.transform.SetParent(parent, false);
    var hl = row.AddComponent<HorizontalLayoutGroup>();
    hl.childForceExpandWidth = false; hl.childForceExpandHeight = true; hl.spacing = 6;
    row.AddComponent<LayoutElement>().preferredHeight = 22;

    var lbl = UIManager.CreateText(row.transform, "Lbl", label,
        10, FontStyle.Normal, UIStyle.TextDim, font, TextAnchor.MiddleLeft);
    lbl.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

    var val = UIManager.CreateText(row.transform, "Val", defaultValue,
        10, FontStyle.Bold, valueColor, font, TextAnchor.MiddleRight);
    val.gameObject.AddComponent<LayoutElement>().preferredWidth = 80;
    return val;
}

public void Show(int score, int bestScore, int coinsEarned)
{
    _root.SetActive(true);
    _scoreText.text    = $"{score}m";
    _bestRow.text      = $"{bestScore}m";
    _coinsRow.text     = $"+{coinsEarned} 🪙";

    _runner.StartCoroutine(UIStyle.ScorePop(_scoreText.rectTransform));
    _runner.StartCoroutine(UIStyle.BackgroundPulse(_pulseOverlay,
        new Color(UIStyle.Danger.r, UIStyle.Danger.g, UIStyle.Danger.b, 0.08f)));
}
```

- [ ] **Step 4: Add button press on CONTINUE tap**

```csharp
private void OnContinueTap()
{
    _runner.StartCoroutine(UIStyle.ButtonPress(_continueBtn.GetComponent<RectTransform>()));
    _onContinue?.Invoke();
}

private void OnRetryTap()
{
    _onRetry?.Invoke();
}
```

- [ ] **Step 5: Enter Play Mode → die → verify acceptance criteria: dark bg, score pop, pulse**

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/UISystems/FailScreen.cs
git commit -m "feat: redesign You Melted screen — dark bg, score pop, dominant CONTINUE"
```

---

## Chunk 5: Shop + Skins Redesign

### Task 10: Redesign ShopScreenController

**Files:**

- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems/ShopScreen.cs`

**Acceptance criteria:**

- Background `#1A0A35` for all tabs
- Coin balance pill: gold border + gold text
- Active tab: brand purple
- BEST VALUE card at top: gradient bg + gold bouncing badge + glow pulse
- Other items at 85% opacity
- Buy buttons: orange gradient
- Owned: green text
- Tab switch: fade animation
- Skins (Balls/Towers): 2-column grid, selected item purple border + glow + 1.05 scale

- [ ] **Step 1: Add `_runner` field + Rewrite Initialize() — header + tabs**

`ShopScreenController` needs `_runner` for coroutine animations and several other fields. Confirm the existing `Initialize()` signature and which fields / methods are already present:

```bash
# Confirm Initialize signature
grep -n "void Initialize" Assets/Scripts/TowerMaze/Runtime/UISystems/ShopScreen.cs

# Confirm carried fields and methods exist
grep -n "class ShopItemData\|class SkinItemData\|void RebuildContent\|void UpdateTabHighlight\|void OnBuyTapped\|void OnSkinTapped\|BuildScrollRect\|_coinPill\|_tabBtns\|_contentGroup\|_contentScroll\|_contentList" \
  Assets/Scripts/TowerMaze/Runtime/UISystems/ShopScreen.cs | head -30
```

The following fields and methods are referenced in the rewritten code below. They must exist on `ShopScreenController` — either carried from the original or added here:

```csharp
// Fields — add any that are missing
private MonoBehaviour _runner;
private Image _coinPill;            // gold coin balance pill image
private Button[] _tabBtns;         // tab highlight array
private CanvasGroup _contentGroup; // tab content fade group
private ScrollRect _contentScroll; // main scrollable area
private Transform _contentList;    // parent for spawned shop/skin rows

// Methods expected to be carried from original ShopScreenController.
// If absent, add stubs (the visual rewrite in later steps fills them in):
// private void RebuildContent(int tabIndex, Font font) { }
// private void UpdateTabHighlight(int tabIndex) { }
// private void OnBuyTapped(ShopItemData item) { }
// private void OnSkinTapped(SkinItemData skin) { }
// private ScrollRect BuildScrollRect(Transform parent) { /* returns a ScrollRect with .content set */ }
//
// Data types expected to exist (confirm with grep above):
// class ShopItemData { string displayName; string subtitle; string priceLabel; ... }
// class SkinItemData { string id; string displayName; bool isSelected; bool isOwned; bool canBuyWithCoins; int coinPrice; string iapPrice; Sprite icon; ... }
```

Add `MonoBehaviour runner` as a parameter if not already present. Start `Initialize()` with `_runner = runner;`:

```csharp
public void Initialize(Canvas canvas, Font font, MonoBehaviour runner, /* existing callbacks */)
{
    _runner = runner;

    var root = UIManager.CreateImage(canvas.transform, "ShopScreen", UIStyle.ShopBg);
    UIManager.Stretch(root.rectTransform);
    root.gameObject.SetActive(false);
    _root = root.gameObject;

    var mainVl = root.gameObject.AddComponent<VerticalLayoutGroup>();
    mainVl.padding = new RectOffset(UIStyle.PadH, UIStyle.PadH, UIStyle.PadV, UIStyle.PadV);
    mainVl.spacing = 8; mainVl.childForceExpandWidth = true; mainVl.childForceExpandHeight = false;

    // Header row: SHOP title + coin balance pill
    var header = new GameObject("Header").AddComponent<HorizontalLayoutGroup>();
    header.transform.SetParent(root.transform, false);
    header.childForceExpandWidth = false; header.childForceExpandHeight = false;

    UIManager.CreateText(header.transform, "Title", "SHOP",
        16, FontStyle.Bold, Color.white, font, TextAnchor.MiddleLeft)
        .gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

    // Coin pill (gold border)
    _coinPill = CreateGoldPill(header.transform, "CoinPill", "🪙 0", font);

    // Tab bar
    var tabBar = new GameObject("TabBar").AddComponent<HorizontalLayoutGroup>();
    tabBar.transform.SetParent(root.transform, false);
    tabBar.spacing = 4; tabBar.childForceExpandWidth = true; tabBar.childForceExpandHeight = false;
    tabBar.gameObject.AddComponent<LayoutElement>().preferredHeight = 32;

    _tabBtns = new Button[3];
    _tabBtns[0] = CreateTabButton(tabBar.transform, "CoinsTab",   "COINS",   font, 0);
    _tabBtns[1] = CreateTabButton(tabBar.transform, "BallsTab",   "BALLS",   font, 1);
    _tabBtns[2] = CreateTabButton(tabBar.transform, "TowersTab",  "TOWERS",  font, 2);

    // Content area (scrollable, with CanvasGroup for tab fade)
    var contentRoot = new GameObject("Content");
    contentRoot.transform.SetParent(root.transform, false);
    contentRoot.AddComponent<LayoutElement>().flexibleHeight = 1;
    _contentGroup = contentRoot.AddComponent<CanvasGroup>();
    _contentScroll = BuildScrollRect(contentRoot.transform);
    _contentList = _contentScroll.content;

    // Initial tab
    SwitchTab(0, font, false);
}

private Image CreateGoldPill(Transform parent, string name, string text, Font font)
{
    var pill = UIManager.CreateImage(parent, name,
        new Color(UIStyle.Gold.r, UIStyle.Gold.g, UIStyle.Gold.b, 0.15f));
    pill.gameObject.AddComponent<Outline>().effectColor = new Color(UIStyle.Gold.r, UIStyle.Gold.g, UIStyle.Gold.b, 0.30f);
    // Note: Unity's Outline component approximates border — acceptable for pill borders.
    var le = pill.gameObject.AddComponent<LayoutElement>();
    le.preferredWidth = 80; le.preferredHeight = 26;
    var txt = UIManager.CreateText(pill.transform, "Text", text,
        9, FontStyle.Bold, UIStyle.Gold, font, TextAnchor.MiddleCenter);
    UIManager.Stretch(txt.rectTransform);
    return pill;
}

/// <summary>Creates a tab button with active/inactive visual state. Stored in _tabBtns[index].</summary>
private Button CreateTabButton(Transform parent, string name, string label, Font font, int index)
{
    var go = new GameObject(name); go.transform.SetParent(parent, false);
    var img = go.AddComponent<Image>(); img.color = UIStyle.SurfaceDark;
    var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
    go.AddComponent<LayoutElement>().flexibleWidth = 1;
    var lbl = UIManager.CreateText(go.transform, "Lbl", label,
        9, FontStyle.Bold, UIStyle.TextDim, font, TextAnchor.MiddleCenter);
    UIManager.Stretch(lbl.rectTransform);
    UIManager.BindButton(btn, () => SwitchTab(index, font));
    return btn;
}
```

- [ ] **Step 2: Build BEST VALUE card**

```csharp
private void SpawnBestValueCard(Transform list, ShopItemData item, Font font, MonoBehaviour runner)
{
    // Glow image behind card (slightly larger, gold color, pulsing alpha)
    var glowGo = new GameObject("BVGlow");
    glowGo.transform.SetParent(list, false);
    var glowImg = glowGo.AddComponent<Image>();
    glowImg.color = new Color(UIStyle.Gold.r, UIStyle.Gold.g, UIStyle.Gold.b, 0.30f);
    glowImg.raycastTarget = false;
    // Size matches card but 8px bigger on each side (via negative padding in layout or manual sizing)
    var glowLe = glowGo.AddComponent<LayoutElement>();
    glowLe.preferredHeight = 76; // card is ~68px

    // Card (slightly larger than regular)
    var card = UIManager.CreateImage(list, "BVCard", new Color(0.18f, 0.06f, 0.38f, 1f)); // #2D1060 approx
    card.gameObject.AddComponent<LayoutElement>().preferredHeight = 68;
    card.gameObject.AddComponent<Outline>().effectColor = new Color(UIStyle.Gold.r, UIStyle.Gold.g, UIStyle.Gold.b, 0.25f);

    var cardHl = card.gameObject.AddComponent<HorizontalLayoutGroup>();
    cardHl.padding = new RectOffset(14, 14, 12, 12);
    cardHl.spacing = 10; cardHl.childForceExpandHeight = true;

    // BEST VALUE badge (absolute, top center, bouncing)
    var badge = UIManager.CreateImage(card.transform, "BVBadge", UIStyle.Gold);
    badge.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
    var badgeRt = badge.rectTransform;
    badgeRt.anchorMin = new Vector2(0.5f, 1); badgeRt.anchorMax = new Vector2(0.5f, 1);
    badgeRt.pivot = new Vector2(0.5f, 0.5f);
    badgeRt.sizeDelta = new Vector2(100, 20);
    badgeRt.anchoredPosition = new Vector2(0, 10);
    var badgeText = UIManager.CreateText(badge.transform, "BVText", "⭐ BEST VALUE",
        9, FontStyle.Bold, new Color(0.03f, 0.016f, 0.1f), font, TextAnchor.MiddleCenter);
    UIManager.Stretch(badgeText.rectTransform);

    // Icon placeholder
    var icon = UIManager.CreateImage(cardHl.transform, "BVIcon", new Color(1, 1, 1, 0.20f));
    icon.gameObject.AddComponent<LayoutElement>().preferredWidth = 44;
    icon.rectTransform.sizeDelta = new Vector2(44, 44);
    // Load actual icon: icon.sprite = item.icon;

    // Title + subtitle
    var textCol = new GameObject("TextCol").AddComponent<VerticalLayoutGroup>();
    textCol.transform.SetParent(cardHl.transform, false);
    textCol.childForceExpandWidth = true; textCol.childForceExpandHeight = false;
    textCol.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
    UIManager.CreateText(textCol.transform, "BVTitle", item.displayName,
        13, FontStyle.Bold, Color.white, font, TextAnchor.MiddleLeft);
    UIManager.CreateText(textCol.transform, "BVSub", item.subtitle,
        9, FontStyle.Normal, UIStyle.TextDim, font, TextAnchor.MiddleLeft);

    // Price button
    var priceBtn = CreateBuyButton(cardHl.transform, "BVPrice", item.priceLabel, font);
    UIManager.BindButton(priceBtn.GetComponent<Button>(), () =>
    {
        runner.StartCoroutine(UIStyle.BuyButtonTap(priceBtn.GetComponent<RectTransform>()));
        OnBuyTapped(item);
    });

    // Start animations
    runner.StartCoroutine(UIStyle.Bounce(badgeRt));
    runner.StartCoroutine(UIStyle.GlowPulse(glowImg, UIStyle.Gold));

    // Other items group (85% opacity applied to CanvasGroup on list container below)
}

private GameObject CreateBuyButton(Transform parent, string name, string label, Font font)
{
    var go = new GameObject(name);
    go.transform.SetParent(parent, false);
    var img = go.AddComponent<Image>(); img.color = UIStyle.Action;
    var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
    var le = go.AddComponent<LayoutElement>(); le.preferredWidth = 58; le.preferredHeight = 34;
    var lbl = UIManager.CreateText(go.transform, "Label", label,
        11, FontStyle.Bold, Color.white, font, TextAnchor.MiddleCenter);
    UIManager.Stretch(lbl.rectTransform);
    return go;
}
```

```csharp
/// <summary>Spawns a standard (non-BEST-VALUE) shop item row at 85% opacity.</summary>
private void SpawnRegularShopItem(Transform list, ShopItemData item, Font font, MonoBehaviour runner, int rank)
{
    var row = UIManager.CreateImage(list, $"ShopItem_{rank}", UIStyle.SurfaceDark);
    row.gameObject.AddComponent<LayoutElement>().preferredHeight = 56;
    var canvasGroup = row.gameObject.AddComponent<CanvasGroup>();
    canvasGroup.alpha = 0.85f;

    var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
    hl.padding = new RectOffset(12, 12, 0, 0);
    hl.spacing = 10; hl.childForceExpandHeight = true; hl.childForceExpandWidth = false;

    // Title + subtitle column
    var textCol = new GameObject("TextCol").AddComponent<VerticalLayoutGroup>();
    textCol.transform.SetParent(row.transform, false);
    textCol.childForceExpandWidth = true; textCol.childForceExpandHeight = false;
    textCol.GetComponent<RectTransform>().SetParent(row.transform, false);
    textCol.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
    UIManager.CreateText(textCol.transform, "Name", item.displayName, 11, FontStyle.Bold, Color.white, font, TextAnchor.MiddleLeft);
    UIManager.CreateText(textCol.transform, "Sub",  item.subtitle,    9,  FontStyle.Normal, UIStyle.TextDim, font, TextAnchor.MiddleLeft);

    // Buy button
    var buyBtn = UIManager.CreateActionButton(row.transform, $"BuyBtn_{rank}", item.priceLabel, font, UIStyle.Action, UIStyle.ActionLight, 36, 0);
    buyBtn.AddComponent<LayoutElement>().preferredWidth = 80;
    UIManager.BindButton(buyBtn.GetComponent<Button>(), () =>
    {
        runner.StartCoroutine(UIStyle.BuyButtonTap(buyBtn.GetComponent<RectTransform>()));
        OnBuyTapped(item);
    });
}
```

- [ ] **Step 3: Build skins 2-column grid**

```csharp
private void BuildSkinsGrid(Transform list, List<SkinItemData> skins, Font font, MonoBehaviour runner)
{
    var grid = new GameObject("SkinsGrid").AddComponent<GridLayoutGroup>();
    grid.transform.SetParent(list, false);
    grid.cellSize = new Vector2(90, 110);
    grid.spacing = new Vector2(UIStyle.GridGap, UIStyle.GridGap);
    grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
    grid.constraintCount = 2;

    var gridCsf = grid.gameObject.AddComponent<ContentSizeFitter>();
    gridCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

    foreach (var skin in skins)
    {
        var card = SpawnSkinCard(grid.transform, skin, font, runner);
    }
}

private GameObject SpawnSkinCard(Transform parent, SkinItemData skin, Font font, MonoBehaviour runner)
{
    var cardGo = new GameObject($"Skin_{skin.id}");
    cardGo.transform.SetParent(parent, false);

    var cardImg = cardGo.AddComponent<Image>();
    bool isSelected = skin.isSelected && skin.isOwned;

    cardImg.color = isSelected
        ? new Color(UIStyle.Brand.r, UIStyle.Brand.g, UIStyle.Brand.b, 0.15f)
        : UIStyle.SurfaceDark;

    var cardRt = cardGo.GetComponent<RectTransform>();
    if (isSelected)
    {
        var outline = cardGo.AddComponent<Outline>();
        outline.effectColor = UIStyle.Brand;
        outline.effectDistance = new Vector2(2, -2);
        cardRt.localScale = new Vector3(1.05f, 1.05f, 1f);
    }

    bool isLocked = !skin.isOwned && !skin.canBuyWithCoins;
    if (isLocked)
    {
        var cg = cardGo.AddComponent<CanvasGroup>();
        cg.alpha = 0.40f;
    }

    var vl = cardGo.AddComponent<VerticalLayoutGroup>();
    vl.padding = new RectOffset(8, 8, 12, 8);
    vl.spacing = 6; vl.childAlignment = TextAnchor.MiddleCenter;
    vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;

    // Icon
    var icon = UIManager.CreateImage(cardGo.transform, "Icon", new Color(UIStyle.Brand.r, UIStyle.Brand.g, UIStyle.Brand.b, 0.30f));
    icon.gameObject.AddComponent<LayoutElement>().preferredHeight = 46;
    // icon.sprite = skin.icon;
    if (isLocked)
    {
        UIManager.CreateText(icon.transform, "Lock", "🔒",
            18, FontStyle.Normal, Color.white, font, TextAnchor.MiddleCenter);
    }

    // Name
    UIManager.CreateText(cardGo.transform, "Name", skin.displayName,
        9, FontStyle.Bold, Color.white, font, TextAnchor.MiddleCenter);

    // Status
    string statusText = skin.isOwned ? "✓ Owned"
        : skin.canBuyWithCoins ? $"🪙 {skin.coinPrice}"
        : skin.iapPrice;
    Color statusColor = skin.isOwned ? UIStyle.Owned
        : skin.canBuyWithCoins ? UIStyle.Gold
        : Color.white;
    UIManager.CreateText(cardGo.transform, "Status", statusText,
        9, FontStyle.Bold, statusColor, font, TextAnchor.MiddleCenter);

    // Tap interaction
    var btn = cardGo.AddComponent<Button>();
    btn.targetGraphic = cardImg;
    UIManager.BindButton(btn, () =>
    {
        runner.StartCoroutine(UIStyle.ButtonPress(cardRt));
        OnSkinTapped(skin);
    });

    return cardGo;
}
```

- [ ] **Step 4: Add `RebuildContent` and `UpdateTabHighlight`**

`SwitchTab` calls `RebuildContent` and `UpdateTabHighlight`. Add their bodies:

```csharp
/// <summary>Clears and repopulates the content list for the given tab index.</summary>
private void RebuildContent(int index, Font font)
{
    foreach (Transform c in _contentList) Object.Destroy(c.gameObject);

    if (index == 0) // COINS tab
    {
        var items = _getShopItems?.Invoke() ?? new System.Collections.Generic.List<ShopItemData>();
        for (int i = 0; i < items.Count; i++)
        {
            if (i == 0)
                SpawnBestValueCard(_contentList, items[i], font, _runner);
            else
                SpawnRegularShopItem(_contentList, items[i], font, _runner, i);
        }
    }
    else // BALLS (1) or TOWERS (2) tab
    {
        var skins = _getSkins?.Invoke(index == 1 ? "ball" : "tower") ?? new System.Collections.Generic.List<SkinItemData>();
        BuildSkinsGrid(_contentList, skins, font, _runner);
    }
}

/// <summary>Updates tab button highlight — active tab brand purple, others dim.</summary>
private void UpdateTabHighlight(int index)
{
    for (int i = 0; i < _tabBtns.Length; i++)
    {
        bool active = i == index;
        _tabBtns[i].GetComponent<Image>().color = active ? UIStyle.Brand : UIStyle.SurfaceDark;
        _tabBtns[i].GetComponentInChildren<Text>().color = active ? Color.white : UIStyle.TextDim;
    }
}
```

**Carried callbacks:** `_getShopItems` (`System.Func<List<ShopItemData>>`) and `_getSkins` (`System.Func<string, List<SkinItemData>>`) must be passed to `Initialize()` and stored as fields. Confirm they exist:

```bash
grep -n "_getShopItems\|_getSkins\|getShopItems\|getSkins" \
  Assets/Scripts/TowerMaze/Runtime/UISystems/ShopScreen.cs
```

Add them to the Step 1 fields block if absent.

- [ ] **Step 5: Wire tab switch with fade + highlight animation**

```csharp
private void SwitchTab(int index, Font font, bool animate = true)
{
    if (animate)
    {
        _runner.StartCoroutine(UIStyle.TabSwitch(_contentGroup, () => RebuildContent(index, font)));
    }
    else
    {
        RebuildContent(index, font);
    }
    UpdateTabHighlight(index);
}
```

- [ ] **Step 6: Enter Play Mode → open Shop → verify all acceptance criteria**

  - Check BEST VALUE badge is bouncing
  - Check other items are dimmer
  - Switch tabs — verify fade animation
  - Tap buy button — verify press animation
  - Open Balls/Towers tab — verify 2-col grid, selected item glow

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/UISystems/ShopScreen.cs
git commit -m "feat: redesign Shop — dark bg, BEST VALUE animated, skins 2-col grid, tab fade"
```

---

## Chunk 6: Minor Popups + Cleanup

### Task 11: Redesign Minor Popups (PopupControllers.cs)

**Files:**

- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems/PopupControllers.cs`

**Acceptance criteria:** Pause, Countdown, IAP Upsell, Toast, Rush Warning all use dark backgrounds. Press animations on all buttons.

- [ ] **Step 1: Add `_runner` field to each controller in PopupControllers.cs**

Every controller in `PopupControllers.cs` that calls coroutines needs a `MonoBehaviour _runner` field. Confirm which controllers exist and add the field + parameter as needed:

```bash
grep -n "^public.*class.*Controller\|void Initialize" \
  Assets/Scripts/TowerMaze/Runtime/UISystems/PopupControllers.cs | head -20
```

For each controller that wires button animations, add `MonoBehaviour runner` to its `Initialize()` and store `_runner = runner;` as the first line.

- [ ] **Step 2: Redesign PauseScreenController.Initialize()**

Replace the existing visual construction. Key call sites:

```csharp
// Root overlay
var root = UIManager.CreateImage(canvas.transform, "PauseOverlay", UIStyle.HudBg);
UIManager.Stretch(root.rectTransform);
root.gameObject.SetActive(false);
_root = root.gameObject;

// Resume button (orange gradient, dominant)
var resumeBtn = UIManager.CreateActionButton(root.transform, "ResumeBtn", "▶ RESUME",
    font, UIStyle.Action, UIStyle.ActionLight, 56, UIStyle.PadH);
UIManager.BindButton(resumeBtn.GetComponent<Button>(), () =>
{
    _runner.StartCoroutine(UIStyle.ButtonPress(resumeBtn.GetComponent<RectTransform>()));
    _onResume?.Invoke(); // carried callback
});

// Return to Menu (secondary)
var menuBtn = UIManager.CreateSecondaryButton(root.transform, "MenuBtn", "Main Menu", font);
UIManager.BindButton(menuBtn.GetComponent<Button>(), () =>
{
    _runner.StartCoroutine(UIStyle.ButtonPress(menuBtn.GetComponent<RectTransform>()));
    _onMenu?.Invoke(); // carried callback
});
```

`_onResume` and `_onMenu` are carried callbacks from the existing `PauseScreenController`. Confirm field names:

```bash
grep -n "_onResume\|_onMenu\|onResume\|onMenu" \
  Assets/Scripts/TowerMaze/Runtime/UISystems/PopupControllers.cs
```

- [ ] **Step 3: Redesign IAPUpsellController.Initialize()**

Apply dark theme to background and reuse `UIManager.CreateActionButton` for buy buttons:

```csharp
// Root background
var root = UIManager.CreateImage(canvas.transform, "IAPScreen", UIStyle.ShopBg);
UIManager.Stretch(root.rectTransform);
root.gameObject.SetActive(false);
_root = root.gameObject;
// IAP pack cards: same UIStyle.SurfaceDark card pattern as ShopScreen regular items.
// BEST VALUE pack (first/largest): use UIStyle.Gold outline + label (no bounce anim needed here).
// Other packs: 85% opacity CanvasGroup, UIManager.CreateActionButton buy buttons.
// Wire existing purchase callbacks — do not change callback logic.
```

- [ ] **Step 4: Redesign RewardToastController**

Update background and coin color only:

```csharp
// Toast card background: UIStyle.SurfaceDark (semi-transparent dark, keeps existing size/position)
// Coin amount text: UIStyle.Gold color
// Animation: keep existing fade-in/out coroutines unchanged — do not modify timing.
```

- [ ] **Step 5: Leave CountdownController, RushWarningController, ControlFlipController visually unchanged** — these are in-game overlays and the dark HUD bg already provides context.

- [ ] **Step 6: Enter Play Mode → trigger pause, toast, IAP upsell → verify dark themes**

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/UISystems/PopupControllers.cs
git commit -m "feat: redesign minor popups with dark theme"
```

---

### Task 12: Ember → Coin Display Rename

**Files:**

- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems/UIManager.cs`
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems/StartScreen.cs`
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems/HudController.cs`
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems/FailScreen.cs`
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems/ShopScreen.cs`
- Possibly: `Assets/Resources/TowerMaze/UITheme/` (icon sprite rename)

**Acceptance criteria:** No "ember" or "Ember" text visible anywhere in UI. Coin icon (🪙 or sprite) used consistently. `EconomyManager.EmberBalance` data field unchanged.

- [ ] **Step 1: Global search for "ember" in UI scripts**

```bash
grep -ri "ember" Assets/Scripts/TowerMaze/Runtime/UISystems/ --include="*.cs"
```

- [ ] **Step 2: Replace UI text labels**

For each occurrence of "ember" or "Ember" in text/label strings:

- `"Ember"` → `"Coin"`
- `"ember"` → `"coin"` (lowercase)
- `ember_icon_hq` sprite reference: either rename the sprite asset to `coin_icon` (update reference) or keep the filename and just change the displayed text

> **Important:** Do NOT rename `EmberBalance`, `emberText` field names, or `PlayerPrefs` keys. Only display strings and icon labels.

- [ ] **Step 3: Verify no "ember" visible in Play Mode across all screens**

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/UISystems/
git commit -m "fix: rename Ember to Coin in all UI display text (data layer unchanged)"
```

---

### Task 13: Final Verification

**Files:** None modified — verification only.

Run through the full verification checklist from the spec (`§8`):

- [ ] **Check 1:** Main menu → `#2D1B69` bg + pulsing orange START ✓
- [ ] **Check 2:** Tap 🏅 → leaderboard bottom sheet slides up ✓
- [ ] **Check 3:** Tap 📋 MISSIONS → missions popup, max 2 items ✓
- [ ] **Check 4:** Start game → HUD left bar visible + 3 milestone ticks ✓
- [ ] **Check 5:** Pick up coins → `+X 🪙` float animation, no persistent objects ✓
- [ ] **Check 6:** Die → "YOU MELTED" score pop ~0.4s, red bg pulse once ✓
- [ ] **Check 7:** Open Shop → BEST VALUE visually largest + bouncing badge + glow ✓
- [ ] **Check 8:** Tap buy button → scale press response + haptic ✓
- [ ] **Check 9:** Open Skins → selected item glow + 1.05 scale, locked items 40% ✓
- [ ] **Check 10:** No white/`#F5F5F7` backgrounds on any screen ✓
- [ ] **Check 11:** Tab switch in Shop → fade transition ✓
- [ ] **Check 12:** Settings panel → dark bg, toggles functional ✓

- [ ] **Final commit**

```bash
git add .
git commit -m "feat: TowerMaze UI redesign complete — dark premium theme, monetization optimized"
```

---

## Notes for Implementer

**Gradient approximation:** UGUI `Image` doesn't support linear gradients natively. For the `action → actionLight` gradient on buttons, use one of:

1. A 2×1 pixel `Texture2D` created at runtime (`new Texture2D(2,1)`, set pixels `[Action, ActionLight]`, apply)
2. A pre-made sprite in Resources
3. Flat `Action` color (visually acceptable, simplest)

**Border/glow in UGUI:** Unity's `Outline` component creates a shadow offset. For the purple card glow effect, use a slightly larger, slightly blurred `Image` behind the card (soft-edge sprite or a glow sprite from Resources). The `GlowPulse` coroutine animates its alpha.

**Haptic feedback:** Use `Handheld.Vibrate()` for medium, or platform-specific haptic APIs (iOS `UIImpactFeedbackGenerator` via DllImport). The existing codebase may already have a haptic wrapper — check `RunSystems.cs` for any vibration calls.

**Coroutine runner:** All `UIStyle` coroutines are static `IEnumerator` — they need a `MonoBehaviour` to call `StartCoroutine`. Pass the `UIManager` MonoBehaviour as `_runner` to each controller constructor, or store it in each controller.

**`characterSpacing` / letter-spacing:** Legacy Unity `Text` component doesn't support character spacing. Either use `TextMeshPro` (if present in project) or simulate with multiple Text objects spaced manually for the "YOU MELTED" label. Alternatively, skip the letter-spacing — the visual difference is minor.
