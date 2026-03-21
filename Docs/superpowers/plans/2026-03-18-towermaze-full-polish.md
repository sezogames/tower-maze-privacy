# TowerMaze Full Polish — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Redesign all screens to dark-mode, add gameplay-retention features (milestone toasts, fast-retry guard, difficulty review), and fix the outstanding compile error.

**Architecture:** Foundation-first. All 7 controller files are already extracted into `UISystems/`. `UIStyle.cs` needs GradientImage + helpers. Each screen is redesigned in its dedicated file. Gameplay systems are wired in Chunk 7.

**Tech Stack:** Unity 2022+ / C# / UGUI (Canvas, RectTransform, Image, Text, Button, CanvasGroup) — runtime-generated UI, no prefabs, no DOTween, Coroutines + Mathf.Lerp for all animation.

---

## Current State

| File | Status |
| --- | --- |
| `UISystems/UIStyle.cs` | ✅ Exists. Dark tokens + animation helpers (`Pulse`, `ButtonPress`, `BuyButtonTap`, `Bounce`, `GlowPulse`, `FadeIn`, `FadeOut`, `SlideUp`, `SlideDown`, `ScorePop`, `CoinFloat`, `BackgroundPulse`, `TabSwitch`). Missing: `GradientImage`, `CreateGlow`, `SlideX`. |
| `UISystems/UIManager.cs` | ✅ Exists. Contains `UIColors` (old light/dark tokens, still referenced by other files) and `ShopCatalogType` enum at end of file. `UIColors` must stay until each screen is redesigned to use `UIStyle.*`. |
| `UISystems/StartScreen.cs` | ⚠️ Extracted. Still light-mode. **Compile error:** references `LeaderboardPanelController` (L48, L199) which no longer exists. |
| `UISystems/FailScreen.cs` | Extracted. Still light-mode (`UIColors.*`). |
| `UISystems/HudController.cs` | Extracted. Still light-mode (`UIColors.*`). |
| `UISystems/ShopScreen.cs` | Extracted. Still light-mode (`UIColors.*`). |
| `UISystems/PopupControllers.cs` | Extracted. Still references `UIColors.*`. |
| `UISystems.cs` | **Deleted.** |
| `ConfigData.cs` | Missing: `heightMilestones`, `milestoneMax`, `failToRetryDelay`. |
| `RunSystems.cs` `ScoreManager` | Missing: `OnMilestonePassed` event, `config` field, `milestoneFired` array. `Initialize()` takes no params. |
| `TowerMazeBootstrapper.cs` | `scoreManager.Initialize()` with no args (L100). `uiManager.Initialize()` does not pass `gameConfig` or `scoreManager`. |

---

## File Structure (what changes)

| File | Action | Contents summary |
| --- | --- | --- |
| `UISystems/UIStyle.cs` | **Modify** — add `GradientImage`, `CreateGlow`, `SlideX` | All design tokens + animation helpers |
| `UISystems/UIManager.cs` | **Modify** — add `GameConfig`/`ScoreManager` params, pass to child controllers | Canvas, routing; `UIColors` compat class stays until Chunk 6 |
| `UISystems/StartScreen.cs` | **Rewrite** | Dark redesign, bottom sheets, settings panel |
| `UISystems/FailScreen.cs` | **Rewrite** | Dark redesign, retry guard, score pop |
| `UISystems/HudController.cs` | **Rewrite** | Dark redesign, progress bar, milestone ticks |
| `UISystems/ShopScreen.cs` | **Rewrite** | Dark shop offers + skins grid |
| `ConfigData.cs` | **Modify** | Add `heightMilestones`, `milestoneMax`, `failToRetryDelay` |
| `RunSystems.cs` | **Modify** | ScoreManager: event, config, milestone firing |
| `TowerMazeBootstrapper.cs` | **Modify** | Pass `gameConfig` to `scoreManager.Initialize()` and `uiManager.Initialize()` |

---

## Chunk 0: Foundation Completion

**Goal:** Fix compile error. Add `GradientImage`, `CreateGlow`, `SlideX` to UIStyle.cs. Add `OnMilestonePassed` event stub to ScoreManager. Game compiles with zero errors.

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems/UIStyle.cs`
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems/StartScreen.cs` (fix compile error)
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs`

---

### Task 0.1 — Fix compile error in StartScreen.cs

`LeaderboardPanelController` class was deleted but StartScreen.cs still references it.

- [ ] **Step 1: In `StartScreen.cs`, delete the `leaderboardPanel` field declaration (L48):**

```csharp
// DELETE this line:
private LeaderboardPanelController leaderboardPanel;
```

- [ ] **Step 2: Delete the leaderboard card construction block (~L197–L200):**

```csharp
// DELETE these lines:
Image leaderboardCard = UIManager.CreateCard("LeaderboardCard", transform, lightCard, lightOutline);
UIManager.Stretch(leaderboardCard.rectTransform, new Vector2(0.18f, 0.215f), new Vector2(0.82f, 0.365f), Vector2.zero, Vector2.zero);
leaderboardPanel = leaderboardCard.gameObject.AddComponent<LeaderboardPanelController>();
leaderboardPanel.Initialize(font, theme, "TOP RUNS");
```

- [ ] **Step 3: Search StartScreen.cs for any remaining `leaderboardPanel` references and delete them.**

```bash
grep -n "leaderboardPanel" "Assets/Scripts/TowerMaze/Runtime/UISystems/StartScreen.cs"
```

Expected: zero results after cleanup.

- [ ] **Step 4: Open Unity (or run `dotnet build`) — verify 0 compile errors.**

Expected: Game enters play mode. Start screen shows existing light-mode layout (visual change comes in Chunk 1).

- [ ] **Step 5: Commit.**

```bash
git add Assets/Scripts/TowerMaze/Runtime/UISystems/StartScreen.cs
git commit -m "fix: remove deleted LeaderboardPanelController reference from StartScreen.cs"
```

---

### Task 0.2 — Add GradientImage, CreateGlow, SlideX to UIStyle.cs

- [ ] **Step 1: Add the `GradientImage` class at the bottom of `UIStyle.cs` (outside the `UIStyle` static class, after its closing `}`).**

The file already has `using UnityEngine.UI;` at the top — do not duplicate it.

```csharp
/// <summary>
/// Vertical linear gradient for UGUI. Use instead of Image when a top-to-bottom
/// color gradient is needed (orange START button, BEST VALUE card, CONTINUE button).
/// Rounded corners are achieved by placing inside a masked RectTransform.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class GradientImage : MaskableGraphic
{
    public Color colorTop    = Color.white;
    public Color colorBottom = Color.white;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = GetPixelAdjustedRect();
        // BL, BR, TR, TL — bottom color at y=min, top color at y=max
        vh.AddVert(new Vector3(r.xMin, r.yMin), colorBottom, Vector2.zero);
        vh.AddVert(new Vector3(r.xMax, r.yMin), colorBottom, Vector2.zero);
        vh.AddVert(new Vector3(r.xMax, r.yMax), colorTop,    Vector2.zero);
        vh.AddVert(new Vector3(r.xMin, r.yMax), colorTop,    Vector2.zero);
        vh.AddTriangle(0, 2, 1);
        vh.AddTriangle(0, 3, 2);
    }
}
```

- [ ] **Step 2: Add `SlideX` coroutine INSIDE the `UIStyle` static class, after `SlideDown`:**

```csharp
/// <summary>
/// Settings panel slide: anchoredPositionX fromX→toX over duration.
/// </summary>
public static IEnumerator SlideX(RectTransform rt, float fromX, float toX, float duration = 0.25f)
{
    float t = 0f;
    while (t < duration)
    {
        float p = Mathf.SmoothStep(0, 1, t / duration);
        rt.anchoredPosition = new Vector2(Mathf.Lerp(fromX, toX, p), rt.anchoredPosition.y);
        t += Time.deltaTime;
        yield return null;
    }
    rt.anchoredPosition = new Vector2(toX, rt.anchoredPosition.y);
}
```

- [ ] **Step 3: Add `CreateGlow` static method INSIDE the `UIStyle` static class (after `SlideX`):**

```csharp
/// <summary>
/// Creates a "Glow" child Image behind target, stretched by expand px on each side.
/// Returns the Image so caller can animate it with GlowPulse.
/// </summary>
public static Image CreateGlow(RectTransform target, Color glowColor, float expand = 16f)
{
    var go = new GameObject("Glow");
    go.transform.SetParent(target.parent, false);
    go.transform.SetSiblingIndex(target.GetSiblingIndex()); // behind target
    var img = go.AddComponent<Image>();
    img.color = glowColor;
    img.raycastTarget = false;
    var rt = img.rectTransform;
    rt.anchorMin = target.anchorMin;
    rt.anchorMax = target.anchorMax;
    rt.offsetMin = target.offsetMin - new Vector2(expand, expand);
    rt.offsetMax = target.offsetMax + new Vector2(expand, expand);
    return img;
}
```

- [ ] **Step 4: Compile check — open Unity or run build.**

Expected: 0 errors.

- [ ] **Step 5: Commit.**

```bash
git add Assets/Scripts/TowerMaze/Runtime/UISystems/UIStyle.cs
git commit -m "feat: add GradientImage, SlideX, CreateGlow to UIStyle"
```

---

### Task 0.3 — Add OnMilestonePassed event stub to ScoreManager

Allows HudController (Chunk 2) to subscribe before the full milestone system is wired (Chunk 7).

- [ ] **Step 1: Open `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs`. Locate `ScoreManager` class at ~L1823.**

- [ ] **Step 2: Add three fields and the event after `public event Action StateChanged;` (~L1839):**

```csharp
// Add these four lines after: public event Action StateChanged;
private GameConfig config;
private bool[] milestoneFired; // allocated in Initialize when config != null
public event Action<int> OnMilestonePassed;
```

- [ ] **Step 3: Update `ScoreManager.Initialize()` to accept optional `GameConfig` (~L1841).**

Change:
```csharp
public void Initialize()
```
To:
```csharp
public void Initialize(GameConfig gameConfig = null)
```

At the start of the method body, add:
```csharp
config = gameConfig;
if (config != null && config.heightMilestones != null)
    milestoneFired = new bool[config.heightMilestones.Length];
```

The `gameConfig = null` default keeps `scoreManager.Initialize()` in `TowerMazeBootstrapper.cs` (L100) compiling without change. The full wiring (mandatory param, Bootstrapper update) happens in Chunk 7.

- [ ] **Step 4: Compile check.**

Expected: 0 errors.

- [ ] **Step 5: Commit.**

```bash
git add Assets/Scripts/TowerMaze/Runtime/RunSystems.cs
git commit -m "feat: add OnMilestonePassed event stub to ScoreManager"
```

**Chunk 0 checkpoint:** Game enters play mode. Start screen, shop, fail screen all open/close. No visual change. Zero compile errors.

---

## Chunk 1: Start Screen Redesign

**Goal:** Dark `#2D1B69` background. GradientImage orange START with Pulse. Secondary buttons at 65% alpha. Top bar with best score + icon buttons. Settings panel slides in from right.

**Files:**
- Rewrite: `Assets/Scripts/TowerMaze/Runtime/UISystems/StartScreen.cs`

**Checkpoint:** Dark bg. START pulses on appear. `[SHOP]` `[📋 MISSIONS]` at 65% opacity. ⚙ settings panel slides in/out.

---

### Task 1.1 — Rewrite StartScreenController

The existing `Initialize()` builds a light-mode layout using `UIColors.*`. Replace the entire class body while keeping the public `Initialize()` and `Show()` signatures.

- [ ] **Step 1: Replace all private field declarations with the new set:**

```csharp
private EconomyManager economyManager;
private Action buttonClickSound;
private Font runtimeFont;
private Text bestScoreText;
private Text emberText;
private RectTransform startButtonRt;
private GameObject secondaryRow;
private GameObject settingsPanel;
private RectTransform settingsPanelRt;
private Image settingsSoundBg;
private Image settingsVibBg;
private bool cachedSoundEnabled;
private bool cachedVibrationEnabled;
private Coroutine pulseCoroutine;
private RectTransform leaderboardSheet;
private RectTransform missionsSheet;
private Text missionCountdownText;
```

- [ ] **Step 2: Replace the body of `Initialize()`. Keep the existing signature:**

```csharp
public void Initialize(Font font, ThemeDefinition theme, EconomyManager economy,
    Action onPlay, Action onPlayDailyChallenge, Action onOpenShop, Action onClaimChest,
    Action onToggleSound, Action onToggleVibration, Action onRerollMissions,
    Action onButtonClick = null)
{
    economyManager = economy;
    buttonClickSound = onButtonClick;
    runtimeFont = font;

    // ── Full-screen background ─────────────────────────────────────────────
    Image bg = UIManager.CreateImage("StartBg", transform, UIStyle.MenuBg);
    UIManager.Stretch(bg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

    // ── Top bar ───────────────────────────────────────────────────────────
    bestScoreText = UIManager.CreateText("BestScore", transform, font, 10,
        TextAnchor.MiddleLeft, UIStyle.TextFaint);
    bestScoreText.rectTransform.anchorMin = new Vector2(0.04f, 0.93f);
    bestScoreText.rectTransform.anchorMax = new Vector2(0.60f, 0.97f);
    bestScoreText.rectTransform.offsetMin = bestScoreText.rectTransform.offsetMax = Vector2.zero;

    // Leaderboard icon button (🏅)
    var lbBtnGo = CreateIconCircle("LeaderboardBtn", transform, font, "\U0001f3c5",
        ShowLeaderboardSheet, buttonClickSound);
    var lbRt = lbBtnGo.GetComponent<RectTransform>();
    lbRt.anchorMin = new Vector2(0.81f, 0.93f);
    lbRt.anchorMax = new Vector2(0.90f, 0.97f);
    lbRt.offsetMin = lbRt.offsetMax = Vector2.zero;

    // Settings icon button (⚙)
    var settingsBtnGo = CreateIconCircle("SettingsBtn", transform, font, "\u2699",
        ShowSettingsPanel, buttonClickSound);
    var settingsRt = settingsBtnGo.GetComponent<RectTransform>();
    settingsRt.anchorMin = new Vector2(0.91f, 0.93f);
    settingsRt.anchorMax = new Vector2(1.00f, 0.97f);
    settingsRt.offsetMin = settingsRt.offsetMax = Vector2.zero;

    // ── Logo ──────────────────────────────────────────────────────────────
    Text logo = UIManager.CreateText("Logo", transform, font, 28,
        TextAnchor.MiddleCenter, UIStyle.TextPrimary);
    logo.text = "TOWER MAZE";
    logo.fontStyle = FontStyle.Bold;
    logo.rectTransform.anchorMin = new Vector2(0.05f, 0.82f);
    logo.rectTransform.anchorMax = new Vector2(0.95f, 0.91f);
    logo.rectTransform.offsetMin = logo.rectTransform.offsetMax = Vector2.zero;

    // ── START button (GradientImage orange, AnimatePulse on enable) ───────
    var startGo = new GameObject("StartButton");
    startGo.transform.SetParent(transform, false);
    startButtonRt = startGo.AddComponent<RectTransform>();
    startButtonRt.anchorMin = new Vector2(0.04f, 0.72f);
    startButtonRt.anchorMax = new Vector2(0.96f, 0.80f);
    startButtonRt.offsetMin = startButtonRt.offsetMax = Vector2.zero;
    var startGrad = startGo.AddComponent<GradientImage>();
    startGrad.colorBottom = UIStyle.Action;
    startGrad.colorTop    = UIStyle.ActionLight;
    var startBtn = startGo.AddComponent<Button>();
    UIManager.BindButton(startBtn,
        () => { StartCoroutine(UIStyle.ButtonPress(startButtonRt)); onPlay?.Invoke(); }, null);
    var startLabel = UIManager.CreateText("Label", startGo.transform, font, 15,
        TextAnchor.MiddleCenter, Color.white);
    startLabel.text = "START";
    startLabel.fontStyle = FontStyle.Bold;
    UIManager.Stretch(startLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

    // ── Secondary row: [SHOP] [📋 MISSIONS] at 65% alpha ─────────────────
    secondaryRow = new GameObject("SecondaryRow");
    secondaryRow.transform.SetParent(transform, false);
    var rowCg = secondaryRow.AddComponent<CanvasGroup>();
    rowCg.alpha = 0.65f;
    var rowRt = secondaryRow.AddComponent<RectTransform>();
    rowRt.anchorMin = new Vector2(0.04f, 0.645f);
    rowRt.anchorMax = new Vector2(0.96f, 0.715f);
    rowRt.offsetMin = rowRt.offsetMax = Vector2.zero;

    var shopBtnGo = CreateSecondaryButton("ShopBtn", secondaryRow.transform, font, "SHOP",
        () => onOpenShop?.Invoke(), buttonClickSound);
    var missionsBtnGo = CreateSecondaryButton("MissionsBtn", secondaryRow.transform, font,
        "\U0001f4cb MISSIONS", ShowMissionsSheet, buttonClickSound);
    LayoutTwoChildren(rowRt, shopBtnGo.GetComponent<RectTransform>(),
        missionsBtnGo.GetComponent<RectTransform>(), 8f);

    // ── Settings panel (built once, starts offscreen) ─────────────────────
    BuildSettingsPanel(font, onToggleSound, onToggleVibration);

    // ── Bottom sheets (leaderboard + missions) ────────────────────────────
    BuildLeaderboardSheet(font);
    BuildMissionsSheet(font);
}
```

- [ ] **Step 3: Add `OnEnable` / `OnDisable` to run START pulse immediately on screen appear:**

```csharp
private void OnEnable()
{
    if (startButtonRt == null) return;
    if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
    pulseCoroutine = StartCoroutine(UIStyle.Pulse(startButtonRt, 1f, 1.05f, 1.4f));
}

private void OnDisable()
{
    if (pulseCoroutine != null) { StopCoroutine(pulseCoroutine); pulseCoroutine = null; }
}
```

- [ ] **Step 4: Add private helper methods:**

```csharp
private GameObject CreateIconCircle(string name, Transform parent, Font font,
    string icon, Action onClick, Action sound)
{
    var go = new GameObject(name);
    go.transform.SetParent(parent, false);
    var img = go.AddComponent<Image>();
    img.color = new Color(1f, 1f, 1f, 0.10f);
    go.AddComponent<RectTransform>(); // anchor set by caller
    var btn = go.AddComponent<Button>();
    UIManager.BindButton(btn, () => {
        StartCoroutine(UIStyle.ButtonPress(go.GetComponent<RectTransform>())); onClick?.Invoke();
    }, sound);
    var lbl = UIManager.CreateText("Icon", go.transform, font, 13,
        TextAnchor.MiddleCenter, UIStyle.TextPrimary);
    lbl.text = icon;
    UIManager.Stretch(lbl.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    return go;
}

private GameObject CreateSecondaryButton(string name, Transform parent, Font font,
    string label, Action onClick, Action sound)
{
    var go = new GameObject(name);
    go.transform.SetParent(parent, false);
    go.AddComponent<RectTransform>();
    var img = go.AddComponent<Image>();
    img.color = new Color(1f, 1f, 1f, 0.10f);
    var btn = go.AddComponent<Button>();
    UIManager.BindButton(btn, () => {
        StartCoroutine(UIStyle.ButtonPress(go.GetComponent<RectTransform>())); onClick?.Invoke();
    }, sound);
    var lbl = UIManager.CreateText("Label", go.transform, font, 10,
        TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.70f));
    lbl.text = label;
    UIManager.Stretch(lbl.rectTransform, Vector2.zero, Vector2.one,
        new Vector2(8f, 0f), new Vector2(-8f, 0f));
    return go;
}

private static void LayoutTwoChildren(RectTransform parent, RectTransform a,
    RectTransform b, float gapPx)
{
    // Splits parent into two equal halves with a pixel gap
    // Uses anchorMin/Max since we don't know pixel width at build time
    float gapFrac = gapPx / 360f; // approximate; actual gap applied via offsetMax/offsetMin
    a.anchorMin = new Vector2(0f, 0f);
    a.anchorMax = new Vector2(0.5f - gapFrac / 2f, 1f);
    a.offsetMin = a.offsetMax = Vector2.zero;
    b.anchorMin = new Vector2(0.5f + gapFrac / 2f, 0f);
    b.anchorMax = new Vector2(1f, 1f);
    b.offsetMin = b.offsetMax = Vector2.zero;
}

private void BuildSettingsPanel(Font font, Action onToggleSound, Action onToggleVibration)
{
    settingsPanel = new GameObject("SettingsPanel");
    settingsPanel.transform.SetParent(transform, false);
    settingsPanelRt = settingsPanel.AddComponent<RectTransform>();
    settingsPanelRt.anchorMin = Vector2.zero;
    settingsPanelRt.anchorMax = Vector2.one;
    settingsPanelRt.offsetMin = settingsPanelRt.offsetMax = Vector2.zero;
    var bg = settingsPanel.AddComponent<Image>();
    bg.color = UIStyle.ShopBg;

    // X close button — top right
    var closeGo = CreateIconCircle("CloseBtn", settingsPanel.transform, font, "\u00d7",
        HideSettingsPanel, buttonClickSound);
    var closeRt = closeGo.GetComponent<RectTransform>();
    closeRt.anchorMin = new Vector2(0.86f, 0.91f);
    closeRt.anchorMax = new Vector2(0.96f, 0.97f);
    closeRt.offsetMin = closeRt.offsetMax = Vector2.zero;

    // Sound toggle row
    BuildSettingsToggleRow("Sound", font, settingsPanel.transform,
        ref settingsSoundBg, 0.72f, () => { onToggleSound?.Invoke(); });

    // Vibration toggle row
    BuildSettingsToggleRow("Vibration", font, settingsPanel.transform,
        ref settingsVibBg, 0.62f, () => { onToggleVibration?.Invoke(); });

    // Start offscreen to the right
    settingsPanelRt.anchoredPosition = new Vector2(1080f, 0f);
    settingsPanel.SetActive(false);
}

private void BuildSettingsToggleRow(string labelStr, Font font, Transform parent,
    ref Image toggleBg, float anchorYMid, Action onToggle)
{
    float h = 0.07f;
    var lbl = UIManager.CreateText("Lbl_" + labelStr, parent, font, 12,
        TextAnchor.MiddleLeft, UIStyle.TextPrimary);
    lbl.text = labelStr.ToUpper();
    lbl.rectTransform.anchorMin = new Vector2(0.06f, anchorYMid - h * 0.5f);
    lbl.rectTransform.anchorMax = new Vector2(0.60f, anchorYMid + h * 0.5f);
    lbl.rectTransform.offsetMin = lbl.rectTransform.offsetMax = Vector2.zero;

    var btnGo = new GameObject("Toggle_" + labelStr);
    btnGo.transform.SetParent(parent, false);
    toggleBg = btnGo.AddComponent<Image>();
    toggleBg.color = new Color(1f, 1f, 1f, 0.10f);
    var btnRt = toggleBg.rectTransform;
    btnRt.anchorMin = new Vector2(0.65f, anchorYMid - 0.04f);
    btnRt.anchorMax = new Vector2(0.94f, anchorYMid + 0.04f);
    btnRt.offsetMin = btnRt.offsetMax = Vector2.zero;
    var btn = btnGo.AddComponent<Button>();
    UIManager.BindButton(btn, onToggle, null);
    var toggleLbl = UIManager.CreateText("Lbl", btnGo.transform, font, 11,
        TextAnchor.MiddleCenter, Color.white);
    toggleLbl.text = "ON";
    UIManager.Stretch(toggleLbl.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
}

private void ShowSettingsPanel()
{
    settingsPanel.SetActive(true);
    float w = settingsPanelRt.rect.width > 1f ? settingsPanelRt.rect.width : 1080f;
    settingsPanelRt.anchoredPosition = new Vector2(w, 0f);
    StartCoroutine(UIStyle.SlideX(settingsPanelRt, w, 0f, 0.25f));
}

private void HideSettingsPanel()
{
    float w = settingsPanelRt.rect.width > 1f ? settingsPanelRt.rect.width : 1080f;
    StartCoroutine(CloseSettingsPanel(w));
}

private IEnumerator CloseSettingsPanel(float w)
{
    yield return StartCoroutine(UIStyle.SlideX(settingsPanelRt, 0f, w, 0.20f));
    settingsPanel.SetActive(false);
}

private void ShowLeaderboardSheet()
{
    if (leaderboardSheet == null) return;
    leaderboardSheet.gameObject.SetActive(true);
    float h = leaderboardSheet.rect.height > 1f ? leaderboardSheet.rect.height : 800f;
    StartCoroutine(UIStyle.SlideUp(leaderboardSheet, h, 0.25f));
}

private void ShowMissionsSheet()
{
    if (missionsSheet == null) return;
    missionsSheet.gameObject.SetActive(true);
    float h = missionsSheet.rect.height > 1f ? missionsSheet.rect.height : 900f;
    StartCoroutine(UIStyle.SlideUp(missionsSheet, h, 0.25f));
}

private IEnumerator CloseSheet(RectTransform sheet)
{
    float h = sheet.rect.height > 1f ? sheet.rect.height : 800f;
    yield return StartCoroutine(UIStyle.SlideDown(sheet, h, 0.20f));
    sheet.gameObject.SetActive(false);
}

private void BuildLeaderboardSheet(Font font)
{
    var go = new GameObject("LeaderboardSheet");
    go.transform.SetParent(transform, false);
    leaderboardSheet = go.AddComponent<RectTransform>();
    leaderboardSheet.anchorMin = new Vector2(0f, 0f);
    leaderboardSheet.anchorMax = new Vector2(1f, 0.65f);
    leaderboardSheet.offsetMin = leaderboardSheet.offsetMax = Vector2.zero;
    go.AddComponent<CanvasGroup>().alpha = 0f;
    go.AddComponent<Image>().color = UIStyle.ShopBg;

    // Handle bar
    var handle = UIManager.CreateImage("Handle", go.transform, new Color(1f, 1f, 1f, 0.15f));
    handle.rectTransform.anchorMin = new Vector2(0.35f, 0.965f);
    handle.rectTransform.anchorMax = new Vector2(0.65f, 0.985f);
    handle.rectTransform.offsetMin = handle.rectTransform.offsetMax = Vector2.zero;
    handle.raycastTarget = false;

    // Title
    var title = UIManager.CreateText("Title", go.transform, font, 14,
        TextAnchor.MiddleLeft, UIStyle.TextPrimary);
    title.text = "\U0001f3c5 Top Runs";
    title.fontStyle = FontStyle.Bold;
    title.rectTransform.anchorMin = new Vector2(0.04f, 0.87f);
    title.rectTransform.anchorMax = new Vector2(0.80f, 0.96f);
    title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;

    // 5 entry rows
    for (int i = 0; i < 5; i++)
    {
        float yTop = 0.86f - i * 0.155f;
        Image row = UIManager.CreateCard($"Row{i}", go.transform, UIStyle.SurfaceDark, UIStyle.BorderDark);
        row.rectTransform.anchorMin = new Vector2(0.04f, yTop - 0.14f);
        row.rectTransform.anchorMax = new Vector2(0.96f, yTop);
        row.rectTransform.offsetMin = row.rectTransform.offsetMax = Vector2.zero;

        var rank = UIManager.CreateText("Rank", row.transform, font, 11, TextAnchor.MiddleLeft,
            i == 0 ? UIStyle.Gold : UIStyle.TextDim);
        rank.text = $"#{i + 1}";
        rank.rectTransform.anchorMin = new Vector2(0.03f, 0f);
        rank.rectTransform.anchorMax = new Vector2(0.18f, 1f);
        rank.rectTransform.offsetMin = rank.rectTransform.offsetMax = Vector2.zero;

        var name = UIManager.CreateText("Name", row.transform, font, 10,
            TextAnchor.MiddleLeft, UIStyle.TextPrimary);
        name.text = "---";
        name.rectTransform.anchorMin = new Vector2(0.20f, 0f);
        name.rectTransform.anchorMax = new Vector2(0.72f, 1f);
        name.rectTransform.offsetMin = name.rectTransform.offsetMax = Vector2.zero;

        var score = UIManager.CreateText("Score", row.transform, font, 10,
            TextAnchor.MiddleRight, UIStyle.TextPrimary);
        score.fontStyle = FontStyle.Bold;
        score.text = "0m";
        score.rectTransform.anchorMin = new Vector2(0.73f, 0f);
        score.rectTransform.anchorMax = new Vector2(0.97f, 1f);
        score.rectTransform.offsetMin = score.rectTransform.offsetMax = Vector2.zero;
    }

    // Close button
    var closeBtn = UIManager.CreateButton("CloseBtn", go.transform, font, "Close",
        UIStyle.SurfaceDark, UIStyle.TextPrimary);
    UIManager.Stretch((RectTransform)closeBtn.transform,
        new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.11f), Vector2.zero, Vector2.zero);
    UIManager.StyleButtonLabel(closeBtn, 11, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
    UIManager.BindButton(closeBtn, () => StartCoroutine(CloseSheet(leaderboardSheet)), null);

    go.SetActive(false);
}

private void BuildMissionsSheet(Font font)
{
    var go = new GameObject("MissionsSheet");
    go.transform.SetParent(transform, false);
    missionsSheet = go.AddComponent<RectTransform>();
    missionsSheet.anchorMin = new Vector2(0f, 0f);
    missionsSheet.anchorMax = new Vector2(1f, 0.75f);
    missionsSheet.offsetMin = missionsSheet.offsetMax = Vector2.zero;
    go.AddComponent<CanvasGroup>().alpha = 0f;
    go.AddComponent<Image>().color = UIStyle.ShopBg;

    // Handle
    var handle = UIManager.CreateImage("Handle", go.transform, new Color(1f, 1f, 1f, 0.15f));
    handle.rectTransform.anchorMin = new Vector2(0.35f, 0.967f);
    handle.rectTransform.anchorMax = new Vector2(0.65f, 0.984f);
    handle.rectTransform.offsetMin = handle.rectTransform.offsetMax = Vector2.zero;
    handle.raycastTarget = false;

    // Title
    var title = UIManager.CreateText("Title", go.transform, font, 14,
        TextAnchor.MiddleLeft, UIStyle.TextPrimary);
    title.text = "\U0001f4cb Daily Missions";
    title.fontStyle = FontStyle.Bold;
    title.rectTransform.anchorMin = new Vector2(0.04f, 0.89f);
    title.rectTransform.anchorMax = new Vector2(0.65f, 0.96f);
    title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;

    missionCountdownText = UIManager.CreateText("Countdown", go.transform, font, 10,
        TextAnchor.MiddleRight, UIStyle.TextDim);
    missionCountdownText.text = "23:59:59";
    missionCountdownText.rectTransform.anchorMin = new Vector2(0.65f, 0.89f);
    missionCountdownText.rectTransform.anchorMax = new Vector2(0.96f, 0.96f);
    missionCountdownText.rectTransform.offsetMin = missionCountdownText.rectTransform.offsetMax = Vector2.zero;

    // 2 mission cards
    for (int i = 0; i < 2; i++)
    {
        float yTop = 0.88f - i * 0.39f;
        Image card = UIManager.CreateCard($"Mission{i}", go.transform, UIStyle.SurfaceDark, UIStyle.BorderDark);
        card.rectTransform.anchorMin = new Vector2(0.04f, yTop - 0.37f);
        card.rectTransform.anchorMax = new Vector2(0.96f, yTop);
        card.rectTransform.offsetMin = card.rectTransform.offsetMax = Vector2.zero;

        var mTitle = UIManager.CreateText("Title", card.transform, font, 11,
            TextAnchor.UpperLeft, UIStyle.TextPrimary);
        mTitle.text = "Complete 3 runs";
        mTitle.fontStyle = FontStyle.Bold;
        mTitle.rectTransform.anchorMin = new Vector2(0.04f, 0.58f);
        mTitle.rectTransform.anchorMax = new Vector2(0.75f, 0.93f);
        mTitle.rectTransform.offsetMin = mTitle.rectTransform.offsetMax = Vector2.zero;

        var progress = UIManager.CreateText("Progress", card.transform, font, 9,
            TextAnchor.UpperRight, UIStyle.TextDim);
        progress.text = "0/3";
        progress.rectTransform.anchorMin = new Vector2(0.76f, 0.60f);
        progress.rectTransform.anchorMax = new Vector2(0.96f, 0.93f);
        progress.rectTransform.offsetMin = progress.rectTransform.offsetMax = Vector2.zero;

        // Progress bar track + fill
        var barTrack = UIManager.CreateImage("BarTrack", card.transform, new Color(1f, 1f, 1f, 0.08f));
        barTrack.rectTransform.anchorMin = new Vector2(0.04f, 0.34f);
        barTrack.rectTransform.anchorMax = new Vector2(0.96f, 0.42f);
        barTrack.rectTransform.offsetMin = barTrack.rectTransform.offsetMax = Vector2.zero;
        barTrack.raycastTarget = false;
        var barFill = UIManager.CreateImage("BarFill", barTrack.transform, UIStyle.Brand);
        barFill.rectTransform.anchorMin = new Vector2(0f, 0f);
        barFill.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        barFill.rectTransform.offsetMin = barFill.rectTransform.offsetMax = Vector2.zero;
        barFill.raycastTarget = false;

        // Reward chip
        var rewardBg = UIManager.CreateImage("RewardBg", card.transform,
            new Color(UIStyle.Gold.r, UIStyle.Gold.g, UIStyle.Gold.b, 0.15f));
        rewardBg.rectTransform.anchorMin = new Vector2(0.04f, 0.06f);
        rewardBg.rectTransform.anchorMax = new Vector2(0.38f, 0.31f);
        rewardBg.rectTransform.offsetMin = rewardBg.rectTransform.offsetMax = Vector2.zero;
        rewardBg.raycastTarget = false;
        var rewardText = UIManager.CreateText("Reward", rewardBg.transform, font, 9,
            TextAnchor.MiddleCenter, UIStyle.Gold);
        rewardText.text = "+50 \U0001fa99";
        UIManager.Stretch(rewardText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    // Close button
    var closeBtn = UIManager.CreateButton("CloseBtn", go.transform, font, "Close",
        UIStyle.SurfaceDark, UIStyle.TextPrimary);
    UIManager.Stretch((RectTransform)closeBtn.transform,
        new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.09f), Vector2.zero, Vector2.zero);
    UIManager.StyleButtonLabel(closeBtn, 11, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
    UIManager.BindButton(closeBtn, () => StartCoroutine(CloseSheet(missionsSheet)), null);

    go.SetActive(false);
}
```

- [ ] **Step 5: Update `Show()` method to set best score text.** Find the existing `Show(float bestScore, ...)` method. At the top of its body add:

```csharp
if (bestScoreText != null)
    bestScoreText.text = $"\U0001f3c6 {bestScore:0}m";
cachedSoundEnabled = soundEnabled;
cachedVibrationEnabled = vibrationEnabled;
```

- [ ] **Step 6: Update `UpdateSettings()` or `UpdateLanguage()` methods if they exist.** The toggle highlight should use `UIStyle.Brand` for active and `rgba(255,255,255,0.10)` for inactive:

```csharp
if (settingsSoundBg != null)
    settingsSoundBg.color = cachedSoundEnabled
        ? UIStyle.Brand
        : new Color(1f, 1f, 1f, 0.10f);
if (settingsVibBg != null)
    settingsVibBg.color = cachedVibrationEnabled
        ? UIStyle.Brand
        : new Color(1f, 1f, 1f, 0.10f);
```

- [ ] **Step 7: Delete all remaining methods from the old light-mode implementation** that are no longer needed (e.g., `UpdateLeaderboard()`, `UpdateMissions()`, chest-related methods). Keep: `Initialize()`, `Show()`, `Hide()`, `OnEnable()`, `OnDisable()`, and the new private helpers above. Any method that references the old light-mode UI elements that no longer exist should be deleted.

- [ ] **Step 8: Compile check.**

Expected: 0 errors.

- [ ] **Step 9: Play-mode checkpoint — Start screen.**

- Dark `#2D1B69` background fills screen
- "TOWER MAZE" logo visible
- Orange gradient START button — pulse begins within first frame of appearance
- [SHOP] and [📋 MISSIONS] buttons at ~65% opacity below START
- 🏅 and ⚙ icon buttons in top-right area
- Tap ⚙ → settings panel slides in from right
- Tap 🏅 → leaderboard sheet slides up from bottom
- Tap 📋 MISSIONS → missions sheet slides up from bottom

- [ ] **Step 10: Commit.**

```bash
git add Assets/Scripts/TowerMaze/Runtime/UISystems/StartScreen.cs
git commit -m "feat: Start screen dark redesign — orange START pulse, bottom sheets, settings slide"
```

---

## Chunk 2: HUD Redesign

**Goal:** Dark `#0F0A1E` HUD. Left edge 4px progress bar with milestone ticks driven by `GameConfig.heightMilestones`. Score + lava pill in center. Subscribe to `ScoreManager.OnMilestonePassed`.

**Files:**
- Rewrite: `Assets/Scripts/TowerMaze/Runtime/UISystems/HudController.cs`
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems/UIManager.cs`
- Modify: `Assets/Scripts/TowerMaze/Runtime/TowerMazeBootstrapper.cs`

**Checkpoint:** Progress bar fills as player climbs. Milestone ticks visible. Lava pill shows `🌋 Xm`. Score 48px center-left.

---

### Task 2.1 — Add GameConfig + ScoreManager to UIManager

- [ ] **Step 1: In `UIManager.cs`, add private fields:**

```csharp
private GameConfig gameConfig;
private ScoreManager scoreManager;
```

- [ ] **Step 2: Add two optional parameters at the END of `UIManager.Initialize()`:**

```csharp
// Add at the end, after "Sprite staticBackground = null":
GameConfig config = null,
ScoreManager scoreMgr = null
```

At the start of the method body:
```csharp
gameConfig = config;
scoreManager = scoreMgr;
```

- [ ] **Step 3: Update `hudController.Initialize()` call** in UIManager.Initialize() to pass the new params:

```csharp
// Change:
hudController.Initialize(runtimeFont, theme, onPause, buttonClickSound);
// To:
hudController.Initialize(runtimeFont, theme, onPause, buttonClickSound, gameConfig, scoreManager);
```

- [ ] **Step 4: In `FailScreenController.Initialize()` call** in UIManager.Initialize(), add retryDelay:

```csharp
// Change (end of the arg list):
failScreenController.Initialize(runtimeFont, theme, economyManager, onRetry, onContinue,
    onReturnToMenu, onClaimDoubleReward, onWatchLifeRefillAd, onBuyLifeRefillWithCoins,
    buttonClickSound);
// To:
failScreenController.Initialize(runtimeFont, theme, economyManager, onRetry, onContinue,
    onReturnToMenu, onClaimDoubleReward, onWatchLifeRefillAd, onBuyLifeRefillWithCoins,
    buttonClickSound, gameConfig?.failToRetryDelay ?? 0.3f);
```

**Note:** `failToRetryDelay` is added to `GameConfig` in Chunk 3 Task 3.1. The `?.` null-safe access handles the case before that field exists at runtime (GameConfig loaded from asset).

- [ ] **Step 5: After creating `rewardToastController`, add dependencies call:**

```csharp
hudController.SetDependencies(economyManager, rewardToastController);
```

- [ ] **Step 6: In `TowerMazeBootstrapper.cs`, update the `uiManager.Initialize()` call (line 110) to add `gameConfig, scoreManager` at the end.**

Find the call to `uiManager.Initialize(splashActive: true, ...)`. The call ends with `audioManager.PlayButtonClick, staticBgSprite)`. Change to:

```csharp
audioManager.PlayButtonClick, staticBgSprite, gameConfig, scoreManager);
```

- [ ] **Step 7: Compile check.**

Expected: 0 errors. (The `gameConfig?.failToRetryDelay` won't cause errors — `GameConfig` doesn't have the field yet, so add a `?.` guard OR wait until Task 3.1 adds the field first. If compile error here, proceed to Task 3.1 first then return.)

---

### Task 2.2 — Rewrite UIHudController

- [ ] **Step 1: Update `UIHudController` field declarations** — replace all existing UI-element fields with:

```csharp
private Text scoreText;        // big score (48px)
private Text lavaText;         // lava pill text
private Text coinText;         // top-left coin balance
private Image progressFill;    // progress bar fill (height driven by score/milestoneMax)
private List<Image> milestoneTicks = new();
private GameConfig gameConfig;
private ScoreManager scoreManager;
private EconomyManager economyManager;
private RewardToastController rewardToastController;
private Button pauseButton;
```

- [ ] **Step 2: Update `Initialize()` signature:**

```csharp
public void Initialize(Font font, ThemeDefinition theme, Action onPause,
    Action soundCallback = null, GameConfig config = null, ScoreManager scoreMgr = null)
```

- [ ] **Step 3: Replace the entire `Initialize()` body with the dark HUD layout:**

```csharp
{
    gameConfig = config;
    scoreManager = scoreMgr;
    if (scoreManager != null)
        scoreManager.OnMilestonePassed += HandleMilestonePassed;

    // ── Full-screen dark background ──────────────────────────────────────
    var bg = UIManager.CreateImage("HudBg", transform, UIStyle.HudBg);
    UIManager.Stretch(bg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    bg.raycastTarget = false;

    // ── Left edge progress bar: 4px wide, x=7 to x=11 ───────────────────
    var barRoot = new GameObject("ProgressBar");
    barRoot.transform.SetParent(transform, false);
    var barRt = barRoot.AddComponent<RectTransform>();
    barRt.anchorMin = new Vector2(0f, 0f);
    barRt.anchorMax = new Vector2(0f, 1f);
    barRt.offsetMin = new Vector2(7f,  0f);
    barRt.offsetMax = new Vector2(11f, 0f);
    var barTrack = barRoot.AddComponent<Image>();
    barTrack.color = new Color(1f, 1f, 1f, 0.06f);
    barTrack.raycastTarget = false;

    // Fill (anchored bottom, top driven by score / milestoneMax)
    var fillGo = new GameObject("Fill");
    fillGo.transform.SetParent(barRoot.transform, false);
    progressFill = fillGo.AddComponent<Image>();
    progressFill.color = UIStyle.Brand;
    progressFill.raycastTarget = false;
    var fillRt = progressFill.rectTransform;
    fillRt.anchorMin = new Vector2(0f, 0f);
    fillRt.anchorMax = new Vector2(1f, 0f); // anchorMax.y updated each frame in SetValues
    fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
    fillRt.pivot = new Vector2(0.5f, 0f);

    // Milestone ticks (8px wide × 1px, faint white lines across the bar)
    if (config != null && config.heightMilestones != null)
    {
        foreach (int mh in config.heightMilestones)
        {
            float yFrac = config.milestoneMax > 0f
                ? Mathf.Clamp01((float)mh / config.milestoneMax) : 0f;
            var tickGo = new GameObject($"Tick_{mh}m");
            tickGo.transform.SetParent(barRoot.transform, false);
            var tick = tickGo.AddComponent<Image>();
            tick.color = new Color(1f, 1f, 1f, 0.30f);
            tick.raycastTarget = false;
            var tickRt = tick.rectTransform;
            tickRt.anchorMin = new Vector2(0f, yFrac);
            tickRt.anchorMax = new Vector2(2f, yFrac); // 8px wide: offsetMax.x=8 sets width
            tickRt.offsetMin = new Vector2(0f, -0.5f);
            tickRt.offsetMax = new Vector2(8f,  0.5f);
            milestoneTicks.Add(tick);
        }
    }

    // ── Top row ──────────────────────────────────────────────────────────
    coinText = UIManager.CreateText("CoinText", transform, font, 9,
        TextAnchor.MiddleLeft, Color.white);
    coinText.rectTransform.anchorMin = new Vector2(0.05f, 0.935f);
    coinText.rectTransform.anchorMax = new Vector2(0.55f, 0.975f);
    coinText.rectTransform.offsetMin = coinText.rectTransform.offsetMax = Vector2.zero;

    // Pause button — small circle, top-right
    var pauseGo = new GameObject("PauseBtn");
    pauseGo.transform.SetParent(transform, false);
    var pauseImg = pauseGo.AddComponent<Image>();
    pauseImg.color = new Color(1f, 1f, 1f, 0.07f);
    var pauseRt = pauseImg.rectTransform;
    pauseRt.anchorMin = new Vector2(0.89f, 0.935f);
    pauseRt.anchorMax = new Vector2(0.98f, 0.975f);
    pauseRt.offsetMin = pauseRt.offsetMax = Vector2.zero;
    pauseButton = pauseGo.AddComponent<Button>();
    UIManager.BindButton(pauseButton, onPause, soundCallback);
    var pauseLbl = UIManager.CreateText("PauseIcon", pauseGo.transform, font, 12,
        TextAnchor.MiddleCenter, UIStyle.TextDim);
    pauseLbl.text = "||";
    UIManager.Stretch(pauseLbl.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

    // ── Center: "SCORE" label + big score ────────────────────────────────
    var scoreLabel = UIManager.CreateText("ScoreLabel", transform, font, 9,
        TextAnchor.MiddleLeft, new Color(0.486f, 0.435f, 0.627f, 1f));
    scoreLabel.text = "SCORE";
    scoreLabel.rectTransform.anchorMin = new Vector2(0.045f, 0.80f);
    scoreLabel.rectTransform.anchorMax = new Vector2(0.55f,  0.835f);
    scoreLabel.rectTransform.offsetMin = scoreLabel.rectTransform.offsetMax = Vector2.zero;

    scoreText = UIManager.CreateText("Score", transform, font, 48,
        TextAnchor.MiddleLeft, Color.white);
    scoreText.fontStyle = FontStyle.Bold;
    scoreText.rectTransform.anchorMin = new Vector2(0.04f, 0.70f);
    scoreText.rectTransform.anchorMax = new Vector2(0.90f, 0.80f);
    scoreText.rectTransform.offsetMin = scoreText.rectTransform.offsetMax = Vector2.zero;

    // ── Lava pill: rgba(255,255,255,0.07) bg, Danger text ─────────────────
    var lavaGo = new GameObject("LavaPill");
    lavaGo.transform.SetParent(transform, false);
    var lavaBg = lavaGo.AddComponent<Image>();
    lavaBg.color = new Color(1f, 1f, 1f, 0.07f);
    var lavaRt = lavaBg.rectTransform;
    lavaRt.anchorMin = new Vector2(0.04f, 0.635f);
    lavaRt.anchorMax = new Vector2(0.58f, 0.695f);
    lavaRt.offsetMin = lavaRt.offsetMax = Vector2.zero;
    lavaText = UIManager.CreateText("LavaText", lavaGo.transform, font, 11,
        TextAnchor.MiddleLeft, UIStyle.Danger);
    UIManager.Stretch(lavaText.rectTransform, Vector2.zero, Vector2.one,
        new Vector2(8f, 0f), new Vector2(-8f, 0f));
}
```

- [ ] **Step 4: Add `SetDependencies()` method:**

```csharp
public void SetDependencies(EconomyManager eco, RewardToastController toast)
{
    economyManager = eco;
    rewardToastController = toast;
}
```

- [ ] **Step 5: Replace `SetValues()` with the new implementation:**

```csharp
public void SetValues(float score, float bestScore, float runTime, int zoneIndex,
    float lavaGap, float gapDangerNormalized, bool isNewBest, bool showControlsHint)
{
    if (scoreText != null)
        scoreText.text = $"{score:0}m";

    if (lavaText != null)
        lavaText.text = $"\U0001f30b {lavaGap:0.0}m";

    // Drive progress bar fill height
    if (progressFill != null && gameConfig != null && gameConfig.milestoneMax > 0f)
    {
        float frac = Mathf.Clamp01(score / gameConfig.milestoneMax);
        progressFill.rectTransform.anchorMax = new Vector2(1f, frac);
    }

    if (coinText != null && economyManager != null)
        coinText.text = $"\U0001fa99 {economyManager.EmberBalance}";
}
```

- [ ] **Step 6: Add `HandleMilestonePassed` and `OnDestroy`:**

```csharp
private void HandleMilestonePassed(int heightMeters)
{
    rewardToastController?.ShowToast($"{heightMeters}m!");

    if (gameConfig != null && gameConfig.heightMilestones != null)
    {
        int idx = System.Array.IndexOf(gameConfig.heightMilestones, heightMeters);
        if (idx >= 0 && idx < milestoneTicks.Count)
            StartCoroutine(FlashTick(milestoneTicks[idx]));
    }
}

private IEnumerator FlashTick(Image tick)
{
    // Fade out from 0.30 to 0 over 0.4s, then reset
    float t = 0f;
    Color baseColor = new Color(1f, 1f, 1f, 0.30f);
    while (t < 0.4f)
    {
        tick.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.30f, 0f, t / 0.4f));
        t += Time.deltaTime;
        yield return null;
    }
    tick.color = baseColor;
}

private void OnDestroy()
{
    if (scoreManager != null)
        scoreManager.OnMilestonePassed -= HandleMilestonePassed;
}
```

- [ ] **Step 7: Compile check. Play-mode checkpoint — HUD.**

- Dark HUD background
- Progress bar on left edge, Brand fill rises as you climb
- Milestone tick marks visible as faint white lines
- Score 48px, large, center-left
- Lava pill shows "🌋 Xm"

- [ ] **Step 8: Commit.**

```bash
git add Assets/Scripts/TowerMaze/Runtime/UISystems/HudController.cs
git add Assets/Scripts/TowerMaze/Runtime/UISystems/UIManager.cs
git add Assets/Scripts/TowerMaze/Runtime/TowerMazeBootstrapper.cs
git commit -m "feat: HUD dark redesign — progress bar, milestone ticks, lava pill, score 48px"
```

---

## Chunk 3: Game Over Redesign

**Goal:** Dark `#1A0F2E`. "TOO SLOW" title. Score pops in. CONTINUE dominant orange. Retry wrapper hidden for `failToRetryDelay`. Red bg pulse on entry. Coin float on reveal.

**Files:**
- Rewrite: `Assets/Scripts/TowerMaze/Runtime/UISystems/FailScreen.cs`
- Modify: `Assets/Scripts/TowerMaze/Runtime/ConfigData.cs`

**Checkpoint:** "TOO SLOW"; score pops in ~0.4s; CONTINUE orange dominant; Retry hidden ~0.3s; red bg flashes once.

---

### Task 3.1 — Add failToRetryDelay to ConfigData.cs

- [ ] **Step 1: Open `ConfigData.cs`. Find `[Header("Run Flow")]` (~L62). Add one line after `countdownGoSeconds`:**

```csharp
[Header("Run Flow")]
[Min(0f)] public float startCountdownSeconds = 3f;
[Min(0f)] public float countdownGoSeconds = 0.6f;
[Min(0f)] public float failToRetryDelay = 0.3f;   // seconds retry is blocked after fail
```

- [ ] **Step 2: Compile check. Verify `failToRetryDelay` appears in GameConfig Inspector.**

- [ ] **Step 3: Commit.**

```bash
git add Assets/Scripts/TowerMaze/Runtime/ConfigData.cs
git commit -m "feat: add failToRetryDelay to GameConfig"
```

---

### Task 3.2 — Rewrite FailScreenController

- [ ] **Step 1: Replace all private field declarations with:**

```csharp
private EconomyManager economyManager;
private Action buttonClickSound;
private Action retryRunAction;
private Action continueRunAction;
private Button continueButton;
private RectTransform retryWrapper;   // replaces retryButton — hidden for failToRetryDelay
private Text scoreValueText;
private RectTransform scoreRt;
private Text bestScoreValue;
private Text coinValueText;
private Image bgPulseOverlay;
private bool bgPulsed;
private float failToRetryDelay;
private Font runtimeFont;
```

- [ ] **Step 2: Update `Initialize()` signature — add `float retryDelay = 0.3f` at the end:**

```csharp
public void Initialize(Font font, ThemeDefinition theme, EconomyManager economy,
    Action onRetry, Action onContinue, Action onReturnToMenu,
    Action onClaimDoubleReward, Action onWatchLifeRefillAd, Action onBuyLifeRefillWithCoins,
    Action onButtonClick = null, float retryDelay = 0.3f)
```

- [ ] **Step 3: Replace entire `Initialize()` body:**

```csharp
{
    economyManager = economy;
    buttonClickSound = onButtonClick;
    retryRunAction  = onRetry;
    continueRunAction = onContinue;
    failToRetryDelay = retryDelay;
    runtimeFont = font;

    // ── Full-screen background ─────────────────────────────────────────────
    var bg = UIManager.CreateImage("FailBg", transform, UIStyle.FailBg);
    UIManager.Stretch(bg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

    // Red pulse overlay (one-shot on each Show, starts alpha=0)
    bgPulseOverlay = UIManager.CreateImage("BgPulse", transform,
        new Color(UIStyle.Danger.r, UIStyle.Danger.g, UIStyle.Danger.b, 0f));
    UIManager.Stretch(bgPulseOverlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    bgPulseOverlay.raycastTarget = false;

    // ── Title: "TOO SLOW" ──────────────────────────────────────────────────
    var title = UIManager.CreateText("Title", transform, font, 28,
        TextAnchor.MiddleCenter, UIStyle.TextPrimary);
    title.text = "TOO SLOW";
    title.fontStyle = FontStyle.Bold;
    title.rectTransform.anchorMin = new Vector2(0.05f, 0.77f);
    title.rectTransform.anchorMax = new Vector2(0.95f, 0.85f);
    title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;

    // ── Score: 56px Brand, Shadow glow, AnimateScorePop on Show ──────────
    scoreValueText = UIManager.CreateText("Score", transform, font, 56,
        TextAnchor.MiddleCenter, UIStyle.Brand);
    scoreValueText.fontStyle = FontStyle.Bold;
    scoreValueText.rectTransform.anchorMin = new Vector2(0.05f, 0.645f);
    scoreValueText.rectTransform.anchorMax = new Vector2(0.95f, 0.77f);
    scoreValueText.rectTransform.offsetMin = scoreValueText.rectTransform.offsetMax = Vector2.zero;
    scoreRt = scoreValueText.rectTransform;
    var scoreShadow = scoreValueText.gameObject.AddComponent<UnityEngine.UI.Shadow>();
    scoreShadow.effectDistance = Vector2.zero;
    scoreShadow.effectColor = new Color(UIStyle.Brand.r, UIStyle.Brand.g, UIStyle.Brand.b, 0.40f);

    // ── Stats card ────────────────────────────────────────────────────────
    var statsCard = UIManager.CreateCard("StatsCard", transform, UIStyle.SurfaceDark, UIStyle.BorderDark);
    statsCard.rectTransform.anchorMin = new Vector2(0.05f, 0.52f);
    statsCard.rectTransform.anchorMax = new Vector2(0.95f, 0.645f);
    statsCard.rectTransform.offsetMin = statsCard.rectTransform.offsetMax = Vector2.zero;

    // Row 1: Best
    var bestLbl = UIManager.CreateText("BestLbl", statsCard.transform, font, 10,
        TextAnchor.MiddleLeft, UIStyle.TextDim);
    bestLbl.text = "Best";
    bestLbl.rectTransform.anchorMin = new Vector2(0.04f, 0.54f);
    bestLbl.rectTransform.anchorMax = new Vector2(0.50f, 0.96f);
    bestLbl.rectTransform.offsetMin = bestLbl.rectTransform.offsetMax = Vector2.zero;
    bestScoreValue = UIManager.CreateText("BestVal", statsCard.transform, font, 11,
        TextAnchor.MiddleRight, UIStyle.TextPrimary);
    bestScoreValue.fontStyle = FontStyle.Bold;
    bestScoreValue.rectTransform.anchorMin = new Vector2(0.50f, 0.54f);
    bestScoreValue.rectTransform.anchorMax = new Vector2(0.96f, 0.96f);
    bestScoreValue.rectTransform.offsetMin = bestScoreValue.rectTransform.offsetMax = Vector2.zero;

    // Divider
    var div = UIManager.CreateImage("Div", statsCard.transform, new Color(1f, 1f, 1f, 0.06f));
    div.rectTransform.anchorMin = new Vector2(0.04f, 0.48f);
    div.rectTransform.anchorMax = new Vector2(0.96f, 0.52f);
    div.rectTransform.offsetMin = div.rectTransform.offsetMax = Vector2.zero;
    div.raycastTarget = false;

    // Row 2: Coins
    var coinsLbl = UIManager.CreateText("CoinsLbl", statsCard.transform, font, 10,
        TextAnchor.MiddleLeft, UIStyle.TextDim);
    coinsLbl.text = "Coins";
    coinsLbl.rectTransform.anchorMin = new Vector2(0.04f, 0.04f);
    coinsLbl.rectTransform.anchorMax = new Vector2(0.50f, 0.46f);
    coinsLbl.rectTransform.offsetMin = coinsLbl.rectTransform.offsetMax = Vector2.zero;
    coinValueText = UIManager.CreateText("CoinsVal", statsCard.transform, font, 11,
        TextAnchor.MiddleRight, UIStyle.Gold);
    coinValueText.fontStyle = FontStyle.Bold;
    coinValueText.rectTransform.anchorMin = new Vector2(0.50f, 0.04f);
    coinValueText.rectTransform.anchorMax = new Vector2(0.96f, 0.46f);
    coinValueText.rectTransform.offsetMin = coinValueText.rectTransform.offsetMax = Vector2.zero;

    // ── CONTINUE button: GradientImage orange, 60px height ────────────────
    var contGo = new GameObject("ContinueBtn");
    contGo.transform.SetParent(transform, false);
    var contRt = contGo.AddComponent<RectTransform>();
    contRt.anchorMin = new Vector2(0.04f, 0.395f);
    contRt.anchorMax = new Vector2(0.96f, 0.515f);
    contRt.offsetMin = contRt.offsetMax = Vector2.zero;
    var contGrad = contGo.AddComponent<GradientImage>();
    contGrad.colorBottom = UIStyle.Action;
    contGrad.colorTop    = UIStyle.ActionLight;
    continueButton = contGo.AddComponent<Button>();
    UIManager.BindButton(continueButton, HandleContinuePressed, buttonClickSound);
    var contLbl = UIManager.CreateText("Label", contGo.transform, font, 15,
        TextAnchor.MiddleCenter, Color.white);
    contLbl.text = "CONTINUE";
    contLbl.fontStyle = FontStyle.Bold;
    UIManager.Stretch(contLbl.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

    // ── Retry wrapper (44px hit area, starts hidden) ───────────────────────
    var retryGo = new GameObject("RetryWrapper");
    retryGo.transform.SetParent(transform, false);
    retryWrapper = retryGo.AddComponent<RectTransform>();
    retryWrapper.anchorMin = new Vector2(0.10f, 0.325f);
    retryWrapper.anchorMax = new Vector2(0.90f, 0.39f);
    retryWrapper.offsetMin = retryWrapper.offsetMax = Vector2.zero;
    var retryBtn = retryGo.AddComponent<Button>();
    UIManager.BindButton(retryBtn, HandleRetryPressed, buttonClickSound);
    var retryLbl = UIManager.CreateText("RetryLabel", retryGo.transform, font, 11,
        TextAnchor.MiddleCenter, UIStyle.TextFaint);
    retryLbl.text = "Retry";
    UIManager.Stretch(retryLbl.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    retryWrapper.gameObject.SetActive(false);  // enabled after failToRetryDelay
}
```

- [ ] **Step 4: Add `HandleContinuePressed()` and `HandleRetryPressed()`:**

```csharp
private void HandleContinuePressed()
{
    StartCoroutine(UIStyle.ButtonPress(continueButton.GetComponent<RectTransform>()));
    AudioManager.TriggerVibration();  // or however the project calls vibrate
    continueRunAction?.Invoke();
}

private void HandleRetryPressed()
{
    retryRunAction?.Invoke();
}
```

**Note:** Check the project's vibration call pattern. It may be `audioManager.TriggerVibration()` where audioManager is passed in. If the existing `HandleContinuePressed` has this, keep the pattern. If AudioManager is a singleton, use that.

- [ ] **Step 5: Find the existing `Show()` method and update its body.** The existing `Show()` signature takes `float currentScore, float bestScore, int emberBalance, int pendingRunReward, bool outOfLives, bool canWatchLifeRefillAd, int lifeRefillCoinCost, float nextLifeRefreshTime, bool continueAvailable, int continueCount, bool canClaimDoubleReward`. Use those same params — just update the body:

```csharp
// In Show() body (replace the visual-building code while keeping param extraction):
if (scoreValueText != null)
{
    scoreValueText.text = $"{currentScore:0}m";
    StartCoroutine(UIStyle.ScorePop(scoreRt));
}
if (bestScoreValue != null)
    bestScoreValue.text = $"{bestScore:0}m";
if (coinValueText != null)
{
    int reward = pendingRunReward;
    coinValueText.text = $"+{reward}";
    if (reward > 0)
        StartCoroutine(UIStyle.CoinFloat(coinValueText));
}

// CONTINUE visibility (keep existing continue logic but style with the new button)
continueButton.gameObject.SetActive(continueAvailable && continueCount > 0);

// Retry guard
retryWrapper.SetActive(false);
StartCoroutine(EnableRetryAfterDelay());

// Background pulse — once per Show()
StartCoroutine(UIStyle.BackgroundPulse(bgPulseOverlay,
    new Color(UIStyle.Danger.r, UIStyle.Danger.g, UIStyle.Danger.b, 0.08f)));
```

- [ ] **Step 6: Add `EnableRetryAfterDelay` and `OnDisable`:**

```csharp
private IEnumerator EnableRetryAfterDelay()
{
    yield return new WaitForSeconds(failToRetryDelay);
    if (retryWrapper != null)
        retryWrapper.SetActive(true);
}

private void OnDisable()
{
    // Allow bg pulse on next Show()
    if (bgPulseOverlay != null)
        bgPulseOverlay.color = new Color(UIStyle.Danger.r, UIStyle.Danger.g, UIStyle.Danger.b, 0f);
}
```

- [ ] **Step 7: Delete the old `retryButton` and `retryButton.interactable` line.** Search FailScreen.cs for `retryButton` and remove all references (field declaration, construction code, and any `retryButton.interactable = ...` line). The new `retryWrapper` replaces it.

```bash
grep -n "retryButton" Assets/Scripts/TowerMaze/Runtime/UISystems/FailScreen.cs
```

Expected: zero results after cleanup.

- [ ] **Step 8: Delete unused old methods** — any method that built or updated the old light-mode UI (scoreText references using old names, life timer, etc.). Keep `Initialize()`, `Show()`, `Hide()` (if exists), `HandleContinuePressed()`, `HandleRetryPressed()`, and the new private helpers.

- [ ] **Step 9: Compile check. Play-mode checkpoint — Game Over.**

- Dark `#1A0F2E` background
- "TOO SLOW" title (no subtitle)
- Score pops in with scale animation (~0.4s)
- Red background flashes once on entry
- CONTINUE button dominant orange
- Retry text appears after ~0.3s

- [ ] **Step 10: Commit.**

```bash
git add Assets/Scripts/TowerMaze/Runtime/UISystems/FailScreen.cs
git add Assets/Scripts/TowerMaze/Runtime/ConfigData.cs
git commit -m "feat: Game Over dark redesign — TOO SLOW, score pop, retry guard, bg pulse"
```

---

## Chunk 4: Shop Offers Redesign

**Goal:** Dark `#1A0A35` background. BEST VALUE card: first in list, ~15% taller, gradient bg, bouncing badge, glow pulse. Others at 85% opacity. Tab bar with Brand active state. Tab switch fades content.

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems/ShopScreen.cs`

**Checkpoint:** Shop dark. BEST VALUE card first, taller, gold-bordered, glow pulsing. Other items at 85% opacity. Tab fade-switch works.

---

### Task 4.1 — Rewrite ShopScreenController visual layer

The ShopScreenController has complex offer-rendering logic. The approach: update color tokens, replace card backgrounds, add BEST VALUE treatment, update tab bar style.

- [ ] **Step 1: Replace the shop background.** Find `UIManager.CreateImage("ShopBackdrop", ...)` in `Initialize()`. Change its color from `UIColors.Surface` (or similar) to `UIStyle.ShopBg`.

- [ ] **Step 2: Update tab bar buttons.** Find the three tab button creation lines. Apply this helper when a tab is activated/deactivated:

```csharp
private void SetTabStyle(Button tab, bool active)
{
    var img = tab.GetComponent<Image>();
    if (img) img.color = active ? UIStyle.Brand : new Color(1f, 1f, 1f, 0.12f);
    var txt = tab.GetComponentInChildren<Text>();
    if (txt) txt.color = active ? Color.white : new Color(1f, 1f, 1f, 0.55f);
}
```

Call `SetTabStyle(coinsTabButton, true)` for the initially-active tab in Initialize(), and update on tab switch.

- [ ] **Step 3: Add tab switch fade animation.** Find `SwitchCategory()` method. Wrap the content-swap logic:

```csharp
// Find the content CanvasGroup (or add one to the offers scroll/container):
// Store as: private CanvasGroup contentCg;
// In Initialize(), add: contentCg = offersContainer.AddComponent<CanvasGroup>();
// In SwitchCategory():
StartCoroutine(UIStyle.TabSwitch(contentCg, () => {
    // existing content rebuild code goes here
}));
```

- [ ] **Step 4: Update regular item card colors.** Find all `UIColors.Card` / `UIColors.Surface` in card creation for non-featured items. Replace with `UIStyle.SurfaceDark`. Replace `UIColors.Divider` with `UIStyle.BorderDark`.

- [ ] **Step 5: Apply 85% opacity to non-featured cards.** After creating each non-featured card's root GameObject:

```csharp
var cg = cardGo.AddComponent<CanvasGroup>();
cg.alpha = 0.85f;
```

- [ ] **Step 6: Build BEST VALUE card treatment.** Find where the featured offer card is built (search for `offer.featured == true` or `StoreOfferKind.WelcomePack`). Apply:

```csharp
// Replace standard Image bg with GradientImage:
var gradBg = cardGo.AddComponent<GradientImage>();
gradBg.colorBottom = UIStyle.ShopBg;
gradBg.colorTop    = new Color(0.176f, 0.063f, 0.376f, 1f); // #2D1060

// Gold border (1px child behind content):
var border = UIManager.CreateImage("Border", cardGo.transform,
    new Color(UIStyle.Gold.r, UIStyle.Gold.g, UIStyle.Gold.b, 0.25f));
UIManager.Stretch(border.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
border.transform.SetAsFirstSibling();
border.raycastTarget = false;

// Glow behind card:
var glowImg = UIStyle.CreateGlow(cardGo.GetComponent<RectTransform>(), UIStyle.Gold, 14f);
glowImg.color = new Color(UIStyle.Gold.r, UIStyle.Gold.g, UIStyle.Gold.b, 0.20f);
StartCoroutine(UIStyle.GlowPulse(glowImg, UIStyle.Gold, 0.20f, 0.55f, 1.8f));

// "⭐ BEST VALUE" bouncing badge above card:
var badgeGo = new GameObject("BestValueBadge");
badgeGo.transform.SetParent(cardGo.transform, false);
var badgeBg = badgeGo.AddComponent<Image>();
badgeBg.color = UIStyle.Gold;
var badgeRt = badgeBg.rectTransform;
badgeRt.anchorMin = new Vector2(0.5f, 1f);
badgeRt.anchorMax = new Vector2(0.5f, 1f);
badgeRt.pivot = new Vector2(0.5f, 0f);
badgeRt.anchoredPosition = new Vector2(0f, 11f);
badgeRt.sizeDelta = new Vector2(110f, 22f);
var badgeTxt = UIManager.CreateText("BadgeTxt", badgeGo.transform, font, 9,
    TextAnchor.MiddleCenter, new Color(0.031f, 0.016f, 0.102f, 1f));
badgeTxt.text = "\u2b50 BEST VALUE";
badgeTxt.fontStyle = FontStyle.Bold;
UIManager.Stretch(badgeTxt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
StartCoroutine(UIStyle.Bounce(badgeRt, 5f, 1.3f));
```

- [ ] **Step 7: Ensure BEST VALUE card renders first** in the scroll list. In the code that builds offer cards, move featured offer to index 0:

```csharp
// Sort offers: featured first
var sorted = offers.OrderByDescending(o => o.featured).ToList();
```

- [ ] **Step 8: Buy button styling.** Replace standard `UIColors.Primary` buy buttons with GradientImage:

```csharp
// For buy buttons in shop offers, find the button creation code.
// Replace the Image background approach with:
var buyGo = new GameObject("BuyBtn");
buyGo.transform.SetParent(cardContent, false);
// ... set RectTransform ...
var buyGrad = buyGo.AddComponent<GradientImage>();
buyGrad.colorBottom = UIStyle.Action;
buyGrad.colorTop    = UIStyle.ActionLight;
var buyBtn = buyGo.AddComponent<Button>();
UIManager.BindButton(buyBtn, () => {
    StartCoroutine(UIStyle.BuyButtonTap(buyGo.GetComponent<RectTransform>()));
    AudioManager.TriggerVibration();
    onSelectItem?.Invoke(ShopCatalogType.Coin, offer.id);
}, buttonClickSound);
```

- [ ] **Step 9: Compile check. Play-mode checkpoint — Shop.**

Open shop, verify:
- Dark `#1A0A35` background
- BEST VALUE card first, taller (add 15% extra height to the card RectTransform), gold-bordered, glow pulsing, badge bouncing
- Other cards at 85% opacity
- Tab buttons: active = Brand bg, inactive = 12% white bg; inactive text at 55% opacity
- Tab switching fades content

- [ ] **Step 10: Commit.**

```bash
git add Assets/Scripts/TowerMaze/Runtime/UISystems/ShopScreen.cs
git commit -m "feat: Shop offers dark redesign — BEST VALUE card, tab bar, buy buttons"
```

---

## Chunk 5: Skins Grid Redesign

**Goal:** 4 skin card states. Locked at 55% opacity with "🔒 Premium" italic. Select/deselect animates. Unlock particle burst.

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems/ShopScreen.cs`

**Checkpoint:** All 4 states render. Locked at 55% with "🔒 Premium". Tap selected skin triggers scale+border animation. Unlock triggers 5-particle burst.

---

### Task 5.1 — Rewrite skins grid rendering

- [ ] **Step 1: Find the method that builds individual skin cards in `ShopScreen.cs`** (search for `SkinEntry`, `GetEquippedSkin`, or the block with ball/tower skin icons).

- [ ] **Step 2: Replace card appearance based on state:**

```csharp
private GameObject BuildSkinCard(Transform parent, Font font, /* skin params */,
    bool isEquipped, bool isOwned, bool isCoinBuy, bool isIAP, int coinPrice)
{
    var go = new GameObject("SkinCard");
    go.transform.SetParent(parent, false);
    var rt = go.AddComponent<RectTransform>();
    // RectTransform size/position set by grid layout caller

    // Background
    var cardBg = go.AddComponent<Image>();
    cardBg.color = isEquipped
        ? new Color(UIStyle.Brand.r, UIStyle.Brand.g, UIStyle.Brand.b, 0.15f)
        : (isIAP && !isOwned ? UIStyle.SurfaceDark2 : UIStyle.SurfaceDark);

    // Border (1px child Image)
    var border = UIManager.CreateImage("Border", go.transform,
        isEquipped ? UIStyle.Brand
        : (isIAP && !isOwned ? new Color(1f, 1f, 1f, 0.05f) : UIStyle.BorderDark));
    UIManager.Stretch(border.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    border.transform.SetAsFirstSibling();
    border.raycastTarget = false;

    // Glow for selected
    if (isEquipped)
    {
        var glow = UIStyle.CreateGlow(rt, UIStyle.Brand, 14f);
        glow.color = new Color(UIStyle.Brand.r, UIStyle.Brand.g, UIStyle.Brand.b, 0.35f);
    }
    if (isEquipped) go.transform.localScale = new Vector3(1.05f, 1.05f, 1f);

    // Opacity for locked IAP
    if (isIAP && !isOwned)
    {
        var cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 0.55f;
    }

    // State label
    string labelText = isEquipped ? "\u2713 Selected"
        : isOwned    ? "Owned"
        : isCoinBuy  ? $"\U0001fa99 {coinPrice}"
        : "\U0001f512 Premium";
    Color labelColor = isEquipped ? new Color(0.655f, 0.545f, 0.980f, 1f)  // #A78BFA
        : isOwned    ? UIStyle.TextDim
        : isCoinBuy  ? UIStyle.Gold
        : new Color(1f, 1f, 1f, 0.45f);
    var lbl = UIManager.CreateText("Label", go.transform, font, isIAP && !isOwned ? 8 : 9,
        TextAnchor.LowerCenter, labelColor);
    lbl.text = labelText;
    if (isIAP && !isOwned) lbl.fontStyle = FontStyle.Italic;
    lbl.rectTransform.anchorMin = new Vector2(0.04f, 0f);
    lbl.rectTransform.anchorMax = new Vector2(0.96f, 0.28f);
    lbl.rectTransform.offsetMin = lbl.rectTransform.offsetMax = Vector2.zero;

    // Tap handler
    if (!isIAP || isOwned || isCoinBuy)
    {
        var btn = go.AddComponent<Button>();
        UIManager.BindButton(btn, () => {
            StartCoroutine(UIStyle.ButtonPress(rt));
            HandleSkinTapped(go, /* skin id */);
        }, null);
    }
    return go;
}
```

- [ ] **Step 3: Add `HandleSkinTapped()` which animates previous card out and new card in:**

```csharp
private GameObject selectedSkinCard;

private void HandleSkinTapped(GameObject card, string skinId)
{
    if (selectedSkinCard != null && selectedSkinCard != card)
    {
        // Deselect old card
        StartCoroutine(AnimateSkinCardDeselect(selectedSkinCard));
    }
    selectedSkinCard = card;
    StartCoroutine(AnimateSkinCardSelect(card));
    // ... call economy to equip/buy
}

private IEnumerator AnimateSkinCardSelect(GameObject card)
{
    // Scale to 1.05 over 0.15s
    float t = 0f;
    var rt = card.GetComponent<RectTransform>();
    var origScale = rt.localScale;
    var target = new Vector3(1.05f, 1.05f, 1f);
    while (t < 0.15f) { rt.localScale = Vector3.Lerp(origScale, target, t / 0.15f); t += Time.deltaTime; yield return null; }
    rt.localScale = target;
    // Fade border to Brand (0→1 over 0.2s)
    var border = card.transform.Find("Border")?.GetComponent<Image>();
    if (border != null) { t = 0f; while (t < 0.2f) { border.color = Color.Lerp(UIStyle.BorderDark, UIStyle.Brand, t / 0.2f); t += Time.deltaTime; yield return null; } border.color = UIStyle.Brand; }
}

private IEnumerator AnimateSkinCardDeselect(GameObject card)
{
    float t = 0f;
    var rt = card.GetComponent<RectTransform>();
    var origScale = rt.localScale;
    var target = Vector3.one;
    while (t < 0.15f) { rt.localScale = Vector3.Lerp(origScale, target, t / 0.15f); t += Time.deltaTime; yield return null; }
    rt.localScale = target;
    var border = card.transform.Find("Border")?.GetComponent<Image>();
    if (border != null) { t = 0f; while (t < 0.2f) { border.color = Color.Lerp(UIStyle.Brand, UIStyle.BorderDark, t / 0.2f); t += Time.deltaTime; yield return null; } border.color = UIStyle.BorderDark; }
}
```

- [ ] **Step 4: Add `UnlockBurst()` coroutine triggered after a successful coin purchase:**

```csharp
private IEnumerator UnlockBurst(Transform cardTransform)
{
    for (int i = 0; i < 5; i++)
    {
        var p = new GameObject("Particle");
        p.transform.SetParent(cardTransform.parent, false);
        p.transform.position = cardTransform.position;
        var img = p.AddComponent<Image>();
        img.color = UIStyle.Gold;
        var pRt = img.rectTransform;
        pRt.sizeDelta = new Vector2(4f, 4f);
        float delay = UnityEngine.Random.Range(0f, 0.05f);
        float tx = UnityEngine.Random.Range(-15f, 15f);
        float ty = UnityEngine.Random.Range(-35f, -20f);
        StartCoroutine(MoveParticle(p, pRt, img, delay, tx, ty));
    }
    yield return null;
}

private IEnumerator MoveParticle(GameObject p, RectTransform rt, Image img,
    float delay, float tx, float ty)
{
    yield return new WaitForSeconds(delay);
    var start = rt.anchoredPosition;
    float t = 0f;
    while (t < 0.1f) { rt.localScale = new Vector3(t / 0.1f, t / 0.1f, 1); t += Time.deltaTime; yield return null; }
    t = 0f;
    while (t < 0.4f)
    {
        float p2 = t / 0.4f;
        rt.anchoredPosition = new Vector2(start.x + tx * p2, start.y + ty * p2);
        img.color = new Color(UIStyle.Gold.r, UIStyle.Gold.g, UIStyle.Gold.b, 1f - p2);
        t += Time.deltaTime;
        yield return null;
    }
    Destroy(p);
}
```

Call `StartCoroutine(UnlockBurst(newlyUnlockedCard.transform))` after a successful purchase.

- [ ] **Step 5: Compile check. Play-mode checkpoint — Skins.**

- All 4 states render with correct colors/opacity
- Locked skins at 55% opacity, "🔒 Premium" italic
- Selected skin at 1.05 scale with Brand border
- Tapping a card triggers ButtonPress + select animation

- [ ] **Step 6: Commit.**

```bash
git add Assets/Scripts/TowerMaze/Runtime/UISystems/ShopScreen.cs
git commit -m "feat: Skins grid dark redesign — 4 states, select animate, unlock burst, locked 55%"
```

---

## Chunk 6: Leaderboard + Missions Popups (Cleanup)

**Goal:** Remove the old missions ribbon code from `StartScreen.cs`. The leaderboard sheet and missions sheet were already built in Chunk 1 as part of the Start screen redesign. This chunk cleans up any remaining old-style mission/chest/challenge UI that Chunk 1 didn't touch.

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems/StartScreen.cs`

**Checkpoint:** Old missions scroll/ribbon gone. Old chest/challenge buttons gone. Both bottom sheets work correctly.

---

### Task 6.1 — Remove legacy missions / chest / challenge elements

After Chunk 1 rewrote `Initialize()`, old UI elements from the original StartScreen are gone (they were in the old `Initialize()` body). If Chunk 1 was done as a full rewrite, this chunk verifies cleanup only.

- [ ] **Step 1: Search `StartScreen.cs` for any remaining references to old UI elements:**

```bash
grep -n "missionScroll\|chestButton\|challengeButton\|rerollButton\|chestInfo\|lifeIcon\|lifePill\|missionText\|challengeInfo" \
  Assets/Scripts/TowerMaze/Runtime/UISystems/StartScreen.cs
```

Expected: zero results (all old elements removed by Chunk 1 rewrite).

- [ ] **Step 2: Verify `UIManager.ShowStart()` call in UIManager.cs still compiles.** The ShowStart signature includes daily missions, chest status, etc. After the StartScreen redesign, StartScreen.cs `Show()` method must still accept and use these params (even if simplified). Ensure no missing Show() params cause compile errors.

- [ ] **Step 3: Wire `UpdateLeaderboard()` data into the leaderboard sheet.** Find UIManager's `ShowStart()` which receives `IReadOnlyList<LeaderboardEntry> leaderboardEntries`. Pass this to StartScreen's `Show()` so the leaderboard rows can be populated:

```csharp
// In StartScreen.cs Show() or a new UpdateLeaderboard() method:
public void UpdateLeaderboard(IReadOnlyList<LeaderboardEntry> entries)
{
    // Find the Row0..Row4 Text children in leaderboardSheet and update them
    // entries[i].height → score text; entries[i].name (or index+1) → name text
}
```

- [ ] **Step 4: Wire `UpdateMissions()` data into the missions sheet.** Similarly update the mission card titles, progress bars, and reward text from `IReadOnlyList<DailyMissionState>`.

- [ ] **Step 5: Compile check. Play-mode checkpoint — Popups.**

- Tap 🏅 → leaderboard sheet slides up with entry data
- Tap 📋 MISSIONS → missions sheet slides up with mission data
- Close buttons slide both sheets back down
- No old leaderboard panel exists anywhere in the scene

- [ ] **Step 6: Commit.**

```bash
git add Assets/Scripts/TowerMaze/Runtime/UISystems/StartScreen.cs
git commit -m "feat: leaderboard + missions sheets data wired, legacy UI confirmed removed"
```

---

## Chunk 7: Gameplay Retention

**Goal:** Complete milestone system (ConfigData fields, ScoreManager Tick logic, Bootstrapper update). Difficulty band review. All 16 spec criteria verified.

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/ConfigData.cs`
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs`
- Modify: `Assets/Scripts/TowerMaze/Runtime/TowerMazeBootstrapper.cs`

---

### Task 7.1 — Add milestone config fields to ConfigData.cs

- [ ] **Step 1: Open `ConfigData.cs`. After the `[Header("Maze Progression")]` section (~L94), add a new header section:**

```csharp
[Header("Milestones")]
public int[]  heightMilestones = new[] { 25, 50, 100 };
[Min(1f)] public float milestoneMax = 200f;
```

- [ ] **Step 2: Verify `failToRetryDelay` was added in Chunk 3 Task 3.1.** If missing, add it now.

- [ ] **Step 3: Compile check. New fields appear in GameConfig Inspector.**

- [ ] **Step 4: Commit.**

```bash
git add Assets/Scripts/TowerMaze/Runtime/ConfigData.cs
git commit -m "feat: add heightMilestones and milestoneMax to GameConfig"
```

---

### Task 7.2 — Complete ScoreManager milestone firing

- [ ] **Step 1: Verify Task 0.3 was done** — `config`, `milestoneFired`, `OnMilestonePassed`, and the updated `Initialize(GameConfig gameConfig = null)` are in ScoreManager.

- [ ] **Step 2: Update `Tick()` in ScoreManager (~L1859) to fire milestone events:**

Change from:
```csharp
public void Tick(float towerHeight, float elapsedTime)
{
    CurrentScore = Mathf.Max(CurrentScore, towerHeight);
    CurrentRunTime = Mathf.Max(0f, elapsedTime);
    BestScore = Mathf.Max(BestScore, CurrentScore);
}
```

To:
```csharp
public void Tick(float towerHeight, float elapsedTime)
{
    CurrentScore = Mathf.Max(CurrentScore, towerHeight);
    CurrentRunTime = Mathf.Max(0f, elapsedTime);
    BestScore = Mathf.Max(BestScore, CurrentScore);

    if (config != null && milestoneFired != null)
    {
        for (int i = 0; i < config.heightMilestones.Length; i++)
        {
            if (!milestoneFired[i] && towerHeight >= config.heightMilestones[i])
            {
                milestoneFired[i] = true;
                OnMilestonePassed?.Invoke(config.heightMilestones[i]);
            }
        }
    }
}
```

- [ ] **Step 3: Update `ResetRun()` to clear the fired flags:**

Add at the end of `ResetRun()`:
```csharp
if (milestoneFired != null)
    System.Array.Clear(milestoneFired, 0, milestoneFired.Length);
```

- [ ] **Step 4: Compile check.**

---

### Task 7.3 — Update TowerMazeBootstrapper.cs

- [ ] **Step 1: Update `scoreManager.Initialize()` call (line 100) to pass `gameConfig`:**

Change:
```csharp
scoreManager.Initialize();
```
To:
```csharp
scoreManager.Initialize(gameConfig);
```

- [ ] **Step 2: Verify `uiManager.Initialize()` already receives `gameConfig, scoreManager`** at the end (added in Chunk 2 Task 2.1). If not, add them now.

- [ ] **Step 3: Full compile check. Play-mode test — milestone system.**

- Reach 25m → toast fires "25m!" and HUD tick flashes
- Start new run → reaching 25m fires the toast again (milestoneFired was cleared by ResetRun)
- HUD progress bar tick positions match `GameConfig.heightMilestones`

- [ ] **Step 4: Commit.**

```bash
git add Assets/Scripts/TowerMaze/Runtime/RunSystems.cs
git add Assets/Scripts/TowerMaze/Runtime/TowerMazeBootstrapper.cs
git commit -m "feat: complete milestone system — ScoreManager Tick fires OnMilestonePassed, Bootstrapper wired"
```

---

### Task 7.4 — Difficulty Band Review

- [ ] **Step 1: Open `DifficultyProfile.asset` in Unity Inspector.**

- [ ] **Step 2: Review Band 0 settings** (`minHeight=0`, `maxHeight=20`). Check:

- `rotationSpeed` ≤ 8f (gentle start)
- `sinkSpeed` ≤ 0.35f
- No abrupt jump between Band 0 → Band 1 (values should not more than double at the boundary)

- [ ] **Step 3: Adjust any bands that are too aggressive. Save the asset.**

- [ ] **Step 4: Play-test: first 0–20m should feel gentle; subsequent bands should ramp gradually.**

---

### Task 7.5 — Final Verification

- [ ] **Criterion 1:** All chunks produced 0 compile errors.
- [ ] **Criterion 2:** No white/light (`#F5F5F7`+) backgrounds visible on any screen.
- [ ] **Criterion 3:** START button pulse starts within first rendered frame of `SetActive(true)`.
- [ ] **Criterion 4:** HUD progress bar and milestone ticks visible; ticks read from `GameConfig.heightMilestones`.
- [ ] **Criterion 5:** Game Over title reads "TOO SLOW" — no subtitle line.
- [ ] **Criterion 6:** Score pop animation completes in ~0.4s.
- [ ] **Criterion 7:** Red background pulse fires once per fail entry.
- [ ] **Criterion 8:** Shop BEST VALUE card is taller than regular cards, first in list, others at 85% opacity.
- [ ] **Criterion 9:** Inactive shop tab text readable (alpha ≥ 0.55).
- [ ] **Criterion 10:** Skins locked state at 55% opacity with "🔒 Premium" label.
- [ ] **Criterion 11:** Leaderboard bottom sheet slides up and down smoothly.
- [ ] **Criterion 12:** Missions bottom sheet renders up to 2 cards; countdown visible.
- [ ] **Criterion 13:** Milestone toasts fire at correct heights.
- [ ] **Criterion 14:** Retry tap area inactive for `GameConfig.failToRetryDelay` seconds after fail.
- [ ] **Criterion 15:** Coin float animation fires on fail screen when coin reward is shown — NOT during active HUD.
- [ ] **Criterion 16:** `UISystems.cs` already deleted — no duplicate class definitions anywhere.

- [ ] **Final commit:**

```bash
git add -A
git commit -m "feat: TowerMaze full polish complete — dark UI, milestone system, retry guard"
```

---

## Reference: Color Values Used Throughout

| Token | Hex | Used on |
| --- | --- | --- |
| `UIStyle.Brand` | `#7C4DFF` | Progress bar, selected skin, score glow, active tab |
| `UIStyle.MenuBg` | `#2D1B69` | Start screen bg |
| `UIStyle.ShopBg` | `#1A0A35` | Shop, skins, settings panel, bottom sheets bg |
| `UIStyle.HudBg` | `#0F0A1E` | HUD bg |
| `UIStyle.FailBg` | `#1A0F2E` | Game Over bg |
| `UIStyle.Action` | `#FF9F0A` | START, CONTINUE, buy buttons (gradient bottom) |
| `UIStyle.ActionLight` | `#FFB340` | Gradient top |
| `UIStyle.Gold` | `#FFD60A` | Coins, BEST VALUE badge, reward chips |
| `UIStyle.Owned` | `#10B981` | Owned badge, claim button |
| `UIStyle.Danger` | `#EF4444` | Lava distance text, bg pulse |

## Reference: UIStyle Method Names (actual names in UIStyle.cs)

| What you need | Call |
| --- | --- |
| Button press | `UIStyle.ButtonPress(rt)` |
| Buy button tap | `UIStyle.BuyButtonTap(rt)` |
| Idle pulse loop | `UIStyle.Pulse(rt, min, max, period)` |
| Bounce loop | `UIStyle.Bounce(rt, amplitude, period)` |
| Glow pulse loop | `UIStyle.GlowPulse(img, baseColor, minA, maxA, period)` |
| Fade in (CanvasGroup) | `UIStyle.FadeIn(cg, duration)` |
| Fade out (CanvasGroup) | `UIStyle.FadeOut(cg, duration)` |
| Bottom sheet open | `UIStyle.SlideUp(rt, sheetHeight, duration)` — combines slide + fade |
| Bottom sheet close | `UIStyle.SlideDown(rt, sheetHeight, duration)` — combines slide + fade |
| Settings panel slide | `UIStyle.SlideX(rt, fromX, toX, duration)` |
| Score pop | `UIStyle.ScorePop(rt)` |
| Coin float | `UIStyle.CoinFloat(text)` |
| Background danger pulse | `UIStyle.BackgroundPulse(overlayImage, pulseColor)` |
| Tab content swap | `UIStyle.TabSwitch(contentCg, swapAction)` |
| Create glow child | `UIStyle.CreateGlow(targetRt, glowColor, expand)` |
