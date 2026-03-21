# Start Screen UI Polish Design

**Date:** 2026-03-16
**Status:** Approved

## Problem

1. The Missions ribbon on the start screen shows only 2 missions as a static text block, with overflow hidden as "+N more". Players cannot see all their missions without navigating away.
2. The gap between the TOP RUNS leaderboard and the Missions ribbon below it is visually tight (~5% of screen height), making the screen feel cluttered.
3. All UI text uses the built-in `LegacyRuntime.ttf` (Arial), which is plain and doesn't match the game's playful tone.

## Goals

- The Missions ribbon is scrollable — all missions visible by scrolling.
- Visual spacing between the leaderboard and the mission ribbon is increased.
- All UI text uses the "Kenney Future" font for a more informal, game-appropriate look.

---

## Changes

**File:** `Assets/Scripts/TowerMaze/Runtime/UISystems.cs`

---

### Change 1: Font — Kenney Future everywhere

**Location:** `UIManager.Initialize()` L70

```csharp
// Before
runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

// After
runtimeFont = Resources.Load<Font>("TowerMaze/UITheme/Kenney Future")
              ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
```

`runtimeFont` is passed to all controllers (`hudController`, `startScreenController`, `failScreenController`, `countdownController`, `rushWarningController`, `controlFlipController`, `shopScreenController`, `rewardToastController`), so this single change propagates everywhere.

The fallback ensures the game still runs if the font asset is missing.

---

### Change 2: Spacing — shift mission ribbon down

**Location:** `StartScreenController.Initialize()` L2106

```csharp
// Before
UIManager.Stretch(missionRibbon.rectTransform, new Vector2(0.17f, 0.075f), new Vector2(0.83f, 0.165f), Vector2.zero, Vector2.zero);

// After
UIManager.Stretch(missionRibbon.rectTransform, new Vector2(0.17f, 0.04f), new Vector2(0.83f, 0.14f), Vector2.zero, Vector2.zero);
```

Shifts the ribbon down by 0.035 normalized units. Gap between leaderboard bottom (y=0.215) and ribbon top increases from 0.05 → 0.075 (~50% more breathing room).

---

### Change 3: Scrollable missions

**Locations:**
- `StartScreenController.Initialize()` L2113–2115 — replace `missionText` creation with scroll setup
- `StartScreenController.BuildMissionText()` L2249 — remove `visibleCount = 2` cap and "+N more" text

#### Initialize: replace static Text with ScrollRect

Current code (L2113–2115):
```csharp
missionText = UIManager.CreateText("MissionText", missionRibbon.transform, font, 16, TextAnchor.UpperCenter, lightText);
UIManager.Stretch(missionText.rectTransform, new Vector2(0f, 0f), new Vector2(0.72f, 0.64f), new Vector2(18f, 8f), new Vector2(-10f, -4f));
UIManager.StyleToyText(missionText, subtleHighlight, new Vector2(0f, 1f), subtleShadow, new Vector2(0f, -1f));
```

Replace with:
```csharp
// Viewport (clips the content)
GameObject missionViewportObj = new("MissionViewport");
missionViewportObj.transform.SetParent(missionRibbon.transform, false);
RectTransform missionViewportRect = missionViewportObj.AddComponent<RectTransform>();
UIManager.Stretch(missionViewportRect, new Vector2(0f, 0f), new Vector2(0.72f, 0.64f), new Vector2(18f, 4f), new Vector2(-10f, -4f));
Image missionViewportImage = missionViewportObj.AddComponent<Image>();
missionViewportImage.color = Color.clear;
Mask missionMask = missionViewportObj.AddComponent<Mask>();
missionMask.showMaskGraphic = false;

// ScrollRect
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

// Content Text (grows with content, ScrollRect scrolls it)
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

#### BuildMissionText: show all missions

```csharp
// Before
int visibleCount = Mathf.Min(2, dailyMissions.Count);
// ...
if (dailyMissions.Count > visibleCount)
{
    builder.AppendLine()
        .Append('+')
        .Append(dailyMissions.Count - visibleCount)
        .Append(" more");
}

// After
int visibleCount = dailyMissions.Count;
// (remove the "+N more" block entirely)
```

---

## Scope

- 1 line changed in `UIManager.Initialize()` (font)
- 1 line changed in `StartScreenController.Initialize()` (ribbon position)
- 3 lines replaced with ~25 lines in `StartScreenController.Initialize()` (scroll setup)
- `BuildMissionText()`: remove 1 line + remove 5-line "+N more" block
- No new fields, no new abstractions
- All changes in `UISystems.cs`
