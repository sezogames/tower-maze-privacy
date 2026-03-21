# Start Screen UI Polish Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Change all UI text to the Kenney Future font, increase spacing between TOP RUNS and the Missions ribbon, and make the Missions ribbon scrollable to show all missions.

**Architecture:** All changes are in `UISystems.cs` — one file. No new abstractions; surgical edits to existing methods. No automated test runner exists; verification is manual play-test in Unity Editor.

**Tech Stack:** Unity C#, `UISystems.cs`

**Spec:** `docs/superpowers/specs/2026-03-16-start-screen-ui-polish-design.md`

---

## Chunk 1: Font + Spacing (quick wins)

### Task 1: Change font to Kenney Future

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs:70`

- [ ] **Step 1: Make the change**

At L70, replace:
```csharp
// Before
runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

// After
runtimeFont = Resources.Load<Font>("TowerMaze/UITheme/Kenney Future")
              ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
```

- [ ] **Step 2: Play-test**

  1. Open Unity Editor, press Play
  2. **Expected:** All UI text (start screen labels, HUD, fail screen, shop, toasts) uses the Kenney Future font — a pixel/arcade-style font instead of Arial
  3. Check for any text overflow — if a label is clipped, note the element for later tuning

---

### Task 2: Shift mission ribbon down

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs:2106`

- [ ] **Step 3: Make the change**

At L2106, replace:
```csharp
// Before
UIManager.Stretch(missionRibbon.rectTransform, new Vector2(0.17f, 0.075f), new Vector2(0.83f, 0.165f), Vector2.zero, Vector2.zero);

// After
UIManager.Stretch(missionRibbon.rectTransform, new Vector2(0.17f, 0.04f), new Vector2(0.83f, 0.14f), Vector2.zero, Vector2.zero);
```

- [ ] **Step 4: Play-test**

  1. Press Play in Unity Editor
  2. Go to start screen
  3. **Expected:** More visible gap between the TOP RUNS leaderboard card and the MISSIONS ribbon below it

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/UISystems.cs
git commit -m "feat: Kenney Future font for all UI; shift mission ribbon down for spacing"
```

---

## Chunk 2: Scrollable missions

### Task 3: Replace static missionText with ScrollRect

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs:2113-2115` (mission text creation)
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs:2249` (BuildMissionText — remove cap)

- [ ] **Step 1: Replace missionText creation with scroll setup**

At L2113–2115, replace these 3 lines:
```csharp
missionText = UIManager.CreateText("MissionText", missionRibbon.transform, font, 16, TextAnchor.UpperCenter, lightText);
UIManager.Stretch(missionText.rectTransform, new Vector2(0f, 0f), new Vector2(0.72f, 0.64f), new Vector2(18f, 8f), new Vector2(-10f, -4f));
UIManager.StyleToyText(missionText, subtleHighlight, new Vector2(0f, 1f), subtleShadow, new Vector2(0f, -1f));
```

With this block:
```csharp
// Viewport — clips the scrollable content to the ribbon body
GameObject missionViewportObj = new("MissionViewport");
missionViewportObj.transform.SetParent(missionRibbon.transform, false);
RectTransform missionViewportRect = missionViewportObj.AddComponent<RectTransform>();
UIManager.Stretch(missionViewportRect, new Vector2(0f, 0f), new Vector2(0.72f, 0.64f), new Vector2(18f, 4f), new Vector2(-10f, -4f)); // Note: offsetMin.y is 4f (was 8f on the old Text) — intentional, gives scroll area slightly more vertical room
Image missionViewportImage = missionViewportObj.AddComponent<Image>();
missionViewportImage.color = Color.clear;
Mask missionMask = missionViewportObj.AddComponent<Mask>();
missionMask.showMaskGraphic = false;

// ScrollRect — handles vertical scrolling
GameObject missionScrollObj = new("MissionScroll");
missionScrollObj.transform.SetParent(missionViewportObj.transform, false);
RectTransform missionScrollRectTransform = missionScrollObj.AddComponent<RectTransform>();
UIManager.Stretch(missionScrollRectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
ScrollRect missionScroll = missionScrollObj.AddComponent<ScrollRect>();
missionScroll.horizontal = false;
missionScroll.vertical = true;
missionScroll.movementType = ScrollRect.MovementType.Clamped;
missionScroll.scrollSensitivity = 32f;
missionScroll.viewport = missionViewportRect;

// Content Text — grows vertically with content; ScrollRect scrolls it
missionText = UIManager.CreateText("MissionText", missionScrollObj.transform, font, 16, TextAnchor.UpperCenter, lightText);
missionText.rectTransform.anchorMin = new Vector2(0f, 1f);
missionText.rectTransform.anchorMax = new Vector2(1f, 1f);
missionText.rectTransform.pivot = new Vector2(0.5f, 1f);
missionText.rectTransform.offsetMin = Vector2.zero;
missionText.rectTransform.offsetMax = Vector2.zero;
missionText.verticalOverflow = VerticalWrapMode.Overflow;
ContentSizeFitter missionFitter = missionText.gameObject.AddComponent<ContentSizeFitter>();
missionFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
missionFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
UIManager.StyleToyText(missionText, subtleHighlight, new Vector2(0f, 1f), subtleShadow, new Vector2(0f, -1f));
missionScroll.content = missionText.rectTransform;
```

⚠️ `missionText` is still the same field name — `SetState()` continues to call `missionText.text = BuildMissionText(dailyMissions)` unchanged.

- [ ] **Step 2: Remove the visibleCount cap in BuildMissionText**

At L2249, replace:
```csharp
int visibleCount = Mathf.Min(2, dailyMissions.Count);
```
With:
```csharp
int visibleCount = dailyMissions.Count;
```

Then remove the "+N more" block (L2267–2273):
```csharp
// Remove this entire block:
if (dailyMissions.Count > visibleCount)
{
    builder.AppendLine()
        .Append('+')
        .Append(dailyMissions.Count - visibleCount)
        .Append(" more");
}
```

Also update the newline logic at L2261–2264 — the condition `if (index < visibleCount - 1)` now correctly handles all missions since `visibleCount == dailyMissions.Count`, so no change needed there.

- [ ] **Step 3: Play-test**

  1. Press Play in Unity Editor
  2. Go to start screen
  3. **Expected:** Missions ribbon shows all missions (not just 2)
  4. If you have more than 2 missions: drag/swipe the missions area — **Expected:** it scrolls smoothly
  5. If you have ≤2 missions: content fits without scrolling — **Expected:** no scroll indicator, missions shown normally
  6. **Expected:** No "+N more" text appears

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/UISystems.cs
git commit -m "feat: scrollable missions ribbon — shows all missions with vertical scroll"
```
