# Pause Menu Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a pause button to the gameplay HUD (bottom-left) that freezes the game and shows a minimal overlay with Resume and Return to Main Menu.

**Architecture:** Add `PauseScreenController` to `UISystems.cs` following existing screen controller patterns; update `UIHudController` to render a pause button; extend `UIManager.Initialize()` with `onPause`/`onResume` callbacks; add `isPaused`, `PauseRun()`, and `ResumeRun()` to `RunManager` using `Time.timeScale` + `AudioListener.pause`; wire everything in `TowerMazeBootstrapper`.

**Tech Stack:** Unity C#, `Time.timeScale`, `AudioListener.pause`, `Input.GetKeyDown(KeyCode.Escape)`, existing `UIManager.CreateButton` / `CreateCard` / `BindButton` helpers.

---

## Chunk 1: UI Layer (UISystems.cs)

### Task 1: Add PauseScreenController class

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs` (insert new class after `UIHudController` ends at ~line 1356)

- [ ] **Step 1: Insert PauseScreenController after UIHudController (after line ~1356)**

Insert the following class between the closing `}` of `UIHudController` and the start of `LeaderboardPanelController`:

```csharp
public sealed class PauseScreenController : MonoBehaviour
{
    private Action buttonClickSound;

    public void Initialize(Font font, ThemeDefinition theme, Action onResume, Action onReturnToMenu, Action onButtonClick = null)
    {
        buttonClickSound = onButtonClick;

        // Full-screen dim overlay
        Image dim = UIManager.CreateImage("PauseDim", transform, new Color(0f, 0f, 0f, 0.72f));
        UIManager.Stretch(dim.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        dim.raycastTarget = true;

        // Center panel
        Image panel = UIManager.CreateCard("PausePanel", transform, UIColors.HudBg, UIColors.HudBorder);
        UIManager.Stretch(panel.rectTransform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-200f, -180f), new Vector2(200f, 180f));

        // Title
        Text title = UIManager.CreateText("PauseTitle", panel.transform, font, 36, TextAnchor.MiddleCenter, UIColors.PrimaryLight);
        title.text = "PAUSED";
        title.fontStyle = FontStyle.Bold;
        UIManager.Stretch(title.rectTransform,
            new Vector2(0f, 0.72f), new Vector2(1f, 1f),
            new Vector2(24f, 0f), new Vector2(-24f, 0f));

        // Resume button
        Button resumeBtn = UIManager.CreateButton("ResumeBtn", panel.transform, font, "DEVAM ET", UIColors.Primary, Color.white);
        UIManager.Stretch(resumeBtn.GetComponent<RectTransform>(),
            new Vector2(0.1f, 0.42f), new Vector2(0.9f, 0.68f),
            Vector2.zero, Vector2.zero);
        UIManager.BindButton(resumeBtn, onResume, buttonClickSound);

        // Main menu button
        Button menuBtn = UIManager.CreateButton("MainMenuBtn", panel.transform, font, "ANA MENÜ", UIColors.HudCard, UIColors.PrimaryLight);
        UIManager.Stretch(menuBtn.GetComponent<RectTransform>(),
            new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.36f),
            Vector2.zero, Vector2.zero);
        UIManager.BindButton(menuBtn, onReturnToMenu, buttonClickSound);
    }
}
```

- [ ] **Step 2: Verify compiles in Unity Editor**

Switch to Unity. Confirm no compile errors in Console.

---

### Task 2: Add pause button to UIHudController

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs` (`UIHudController` class, ~line 1230)

`UIHudController.Initialize` currently has signature:
```csharp
public void Initialize(Font font, ThemeDefinition theme)
```

- [ ] **Step 1: Add `pauseButton` field to UIHudController**

After `private Text controlsHintText;` (~line 1241), add:
```csharp
private Button pauseButton;
```

- [ ] **Step 2: Update UIHudController.Initialize signature**

Change:
```csharp
public void Initialize(Font font, ThemeDefinition theme)
```
To:
```csharp
public void Initialize(Font font, ThemeDefinition theme, Action onPause, Action soundCallback = null)
```

Note: The spec mentions a separate `SetPauseCallback()` method, but passing via `Initialize()` is the approach used in this plan — it avoids a two-step construction sequence and matches how every other controller in the codebase receives its callbacks. No `SetPauseCallback` method is needed.

- [ ] **Step 3: Add pause button creation at end of Initialize body**

After the `controlsHintCard` block (after `UIManager.StyleToyText(controlsHintText, ...)` at ~line 1298), add:

```csharp
// Pause button — bottom-left, same row as controls hint
Image pauseCard = UIManager.CreateCard("PauseBtn", transform, UIColors.HudCard, UIColors.HudBorder);
UIManager.Stretch(pauseCard.rectTransform,
    new Vector2(0.03f, 0.03f), new Vector2(0.13f, 0.072f),
    Vector2.zero, Vector2.zero);
Text pauseLabel = UIManager.CreateText("PauseBtnLabel", pauseCard.transform, font, 22, TextAnchor.MiddleCenter, UIColors.HudTextDim);
pauseLabel.text = "II";
pauseLabel.fontStyle = FontStyle.Bold;
UIManager.Stretch(pauseLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
pauseButton = pauseCard.gameObject.AddComponent<Button>();
UIManager.BindButton(pauseButton, onPause, soundCallback);
```

- [ ] **Step 4: Verify compiles in Unity Editor**

Confirm no compile errors. UIHudController now won't compile until UIManager passes the new arguments — fix that in Task 3.

---

### Task 3: Update UIManager fields, Initialize(), ShowPause(), HidePause()

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs` (`UIManager` class)

- [ ] **Step 1: Add `pauseScreenController` field to UIManager**

After `private RewardToastController rewardToastController;` (~line 65), add:
```csharp
private PauseScreenController pauseScreenController;
```

- [ ] **Step 2: Update UIManager.Initialize() signature**

Current signature ends with:
```csharp
        Action onToggleVibration,
        Action onButtonClick = null,
        Sprite staticBackground = null)
```

Change to:
```csharp
        Action onToggleVibration,
        Action onPause,
        Action onResume,
        Action onButtonClick = null,
        Sprite staticBackground = null)
```

- [ ] **Step 3: Update hudController.Initialize() call inside UIManager.Initialize()**

Find (~line 139):
```csharp
hudController.Initialize(runtimeFont, theme);
```

Replace with:
```csharp
hudController.Initialize(runtimeFont, theme, onPause, buttonClickSound);
```

Note: `buttonClickSound` is already assigned earlier in `Initialize()` (line ~112).

- [ ] **Step 4: Create pauseScreenController in UIManager.Initialize()**

After the `iapUpsellController` block (after ~line 166), add:

```csharp
pauseScreenController = CreatePanel<PauseScreenController>("PauseScreen", canvas.transform);
pauseScreenController.Initialize(runtimeFont, theme, onResume, onReturnToMenu, buttonClickSound);
pauseScreenController.gameObject.SetActive(false);
```

- [ ] **Step 5: Add ShowPause() and HidePause() methods to UIManager**

After `public void HidePause()` doesn't exist yet — add both after the existing `SetHeat()` method (~line 305):

```csharp
public void ShowPause()
{
    pauseScreenController.gameObject.SetActive(true);
}

public void HidePause()
{
    pauseScreenController.gameObject.SetActive(false);
}
```

- [ ] **Step 6: Verify compiles in Unity Editor**

Switch to Unity. There will be **one expected compile error** in `TowerMazeBootstrapper.cs` because the `UIManager.Initialize()` call is now missing the two new `onPause` / `onResume` arguments — this is fixed in Task 5 (Chunk 2). Confirm no other errors. UIManager itself and UISystems.cs should be error-free at this point.

---

## Chunk 2: Logic Layer (RunSystems.cs + Bootstrapper)

### Task 4: Add pause logic to RunManager

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs` (`RunManager` class)

`RunManager` is in the same file as `EconomyManager`, `AudioManager`, etc. The `RunManager` class starts around line 2530.

- [ ] **Step 1: Add `isPaused` field to RunManager**

After `private RunState state = RunState.Boot;` (~line 2542), add:
```csharp
private bool isPaused;
```

- [ ] **Step 2: Add PauseRun() and ResumeRun() methods**

After the `Initialize()` method (after ~line 2636), add:

```csharp
public void PauseRun()
{
    if (state != RunState.Running) return;
    isPaused = true;
    Time.timeScale = 0f;
    AudioListener.pause = true;
    uiManager.ShowPause();
}

public void ResumeRun()
{
    if (!isPaused) return;
    uiManager.HidePause();
    AudioListener.pause = false;
    Time.timeScale = 1f;
    isPaused = false;
}
```

- [ ] **Step 3: Add Escape key handling and pause guard in Update()**

In `RunManager.Update()`, find the block that guards against non-Running state (~lines 2661–2665):

```csharp
            if (state != RunState.Running)
            {
                playerController?.Tick(false);
                return;
            }
```

Insert two lines immediately **after** this block (before `runElapsedTime += Time.deltaTime;`):

```csharp
            if (state == RunState.Running && Input.GetKeyDown(KeyCode.Escape))
            {
                if (isPaused) ResumeRun(); else PauseRun();
                return;
            }

            if (isPaused) return;
```

After the insert, the code should read:
```csharp
            if (state != RunState.Running)
            {
                playerController?.Tick(false);
                return;
            }

            if (state == RunState.Running && Input.GetKeyDown(KeyCode.Escape))
            {
                if (isPaused) ResumeRun(); else PauseRun();
                return;
            }

            if (isPaused) return;

            runElapsedTime += Time.deltaTime;
            // ... rest of Running tick
```

- [ ] **Step 4: Reset pause state in ReturnToMainMenu()**

`ReturnToMainMenu()` starts at ~line 2829. Find the very beginning of the method body (after the opening `{`), and add:

```csharp
        if (isPaused)
        {
            isPaused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            uiManager.HidePause();
        }
```

This guard ensures time scale and audio are restored even if the player returns to menu while paused.

- [ ] **Step 5: Verify compiles in Unity Editor**

Confirm no compile errors. The methods `PauseRun` and `ResumeRun` are now public and ready to be passed as callbacks.

---

### Task 5: Update TowerMazeBootstrapper

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/TowerMazeBootstrapper.cs` (~line 108)

Current call:
```csharp
uiManager.Initialize(splashActive: true, themeDefinition, economyManager, rewardedAdManager, coinStoreManager, playerController, runManager.StartRun, runManager.StartDailyChallenge, runManager.RetryRun, runManager.ContinueRun, runManager.ReturnToMainMenu, runManager.ClaimDoubleReward, runManager.WatchAdForLifeRefill, runManager.BuyLifeRefillWithCoins, runManager.ToggleSound, runManager.ToggleVibration, audioManager.PlayButtonClick, staticBgSprite);
```

- [ ] **Step 1: Add runManager.PauseRun and runManager.ResumeRun arguments**

Replace with:
```csharp
uiManager.Initialize(splashActive: true, themeDefinition, economyManager, rewardedAdManager, coinStoreManager, playerController, runManager.StartRun, runManager.StartDailyChallenge, runManager.RetryRun, runManager.ContinueRun, runManager.ReturnToMainMenu, runManager.ClaimDoubleReward, runManager.WatchAdForLifeRefill, runManager.BuyLifeRefillWithCoins, runManager.ToggleSound, runManager.ToggleVibration, runManager.PauseRun, runManager.ResumeRun, audioManager.PlayButtonClick, staticBgSprite);
```

The only change is inserting `runManager.PauseRun, runManager.ResumeRun,` after `runManager.ToggleVibration,`.

- [ ] **Step 2: Verify compiles in Unity Editor**

Confirm no compile errors in Console. All systems are now wired.

- [ ] **Step 3: Play-test in Unity Editor**

Press Play in Unity Editor. Verify:
1. HUD shows a small `II` button at bottom-left during gameplay
2. Tapping `II` (or pressing ESC) while running opens the pause overlay
3. Pressing `II` or ESC again (or tapping DEVAM ET) resumes — game and audio continue normally
4. Tapping ANA MENÜ from the pause overlay returns to main menu cleanly (no frozen menu)
5. Pause button does nothing during Countdown or at the main menu
6. Sound is muted while paused, restored on resume
