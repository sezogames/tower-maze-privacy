# Loading Screen Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Show the TowerMaze key art as a branded splash screen on every app launch, with an animated TOWERMAZE title and spinning loader, before the Start Screen appears.

**Architecture:** A new self-dismissing `SplashScreenController` MonoBehaviour creates its own high-sortOrder Canvas and manages its own lifecycle (fade-in, minimum display time, fade-out). `UIManager.ShowStart()` defers until the splash signals completion via callback. The Unity engine-level Splash Screen (Player Settings) shows the same image before the scene loads.

**Tech Stack:** Unity 6 C#, uGUI (Canvas, RawImage, Image, Text, CanvasGroup), Unity Coroutines, Resources.Load

**Spec:** `docs/superpowers/specs/2026-03-17-loading-screen-design.md`

---

## Chunk 1: Assets

### Task 1: Copy splash background image into the project

**Files:**
- Create: `Assets/Resources/TowerMaze/UITheme/SplashBackground.png`

- [ ] **Step 1: Copy the TowerMaze key art into the Resources folder**

  In Windows Explorer (or any file manager), copy the TowerMaze key art image to:
  ```
  c:\Users\Pc\TowerMaze\Assets\Resources\TowerMaze\UITheme\SplashBackground.png
  ```
  The image is the 1440×2560 portrait render of the tower over lava (same image used in this conversation).

- [ ] **Step 2: Set Unity import settings for SplashBackground.png**

  In Unity Editor, select `Assets/Resources/TowerMaze/UITheme/SplashBackground.png` in the Project window.
  In the Inspector:
  - **Texture Type:** `Default` (NOT Sprite — `Resources.Load<Texture2D>()` returns null on Sprite-typed assets)
  - **Alpha Source:** `None`
  - **sRGB:** checked
  - **Generate Mip Maps:** unchecked (UI texture, no mipmaps needed)
  - **Filter Mode:** `Bilinear`
  - **Compression:** `Normal Quality`
  - Click **Apply**.

### Task 2: Create the spinner ring sprite

**Files:**
- Create: `Assets/Resources/TowerMaze/UITheme/SpinnerRing.png`

- [ ] **Step 1: Create a white circle/ring PNG**

  Using any image editor (Paint.NET, Photoshop, GIMP, etc.), create a 256×256px PNG:
  - Background: fully transparent
  - Draw a white circle ring: outer radius ~120px, inner radius ~90px, centered at 128,128
  - Save as `SpinnerRing.png`

  Copy to:
  ```
  c:\Users\Pc\TowerMaze\Assets\Resources\TowerMaze\UITheme\SpinnerRing.png
  ```

- [ ] **Step 2: Set Unity import settings for SpinnerRing.png**

  In Unity Editor, select `Assets/Resources/TowerMaze/UITheme/SpinnerRing.png`.
  In the Inspector:
  - **Texture Type:** `Sprite (2D and UI)`
  - **Sprite Mode:** `Single`
  - **Alpha Source:** `Input Texture Alpha` (the ring's transparency depends on the PNG alpha — do NOT set this to None)
  - **Pixels Per Unit:** `100`
  - **Filter Mode:** `Bilinear`
  - **Compression:** `Normal Quality`
  - Click **Apply**.

- [ ] **Step 3: Commit assets**

  ```bash
  git add Assets/Resources/TowerMaze/UITheme/SplashBackground.png
  git add Assets/Resources/TowerMaze/UITheme/SplashBackground.png.meta
  git add Assets/Resources/TowerMaze/UITheme/SpinnerRing.png
  git add Assets/Resources/TowerMaze/UITheme/SpinnerRing.png.meta
  git commit -m "feat: add loading screen assets (SplashBackground + SpinnerRing)"
  ```

---

## Chunk 2: SplashScreenController

### Task 3: Add SplashScreenController class to UISystems.cs

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs` (append new class at end of namespace)

- [ ] **Step 1: Locate the insertion point**

  Open `Assets/Scripts/TowerMaze/Runtime/UISystems.cs`. Scroll to the very bottom of the file. Find the closing `}` of the last class (currently `IAPUpsellController`). The new class goes before the final `}` that closes the `namespace TowerMaze` block.

- [ ] **Step 2: Add the SplashScreenController class**

  Insert the following code immediately before the closing `}` of `namespace TowerMaze` (i.e., after `IAPUpsellController`'s closing `}`):

  ```csharp
      public sealed class SplashScreenController : MonoBehaviour
      {
          private bool isVisible;
          public bool IsVisible => isVisible;

          public void Initialize(Font font, Texture2D backgroundTexture, Action onComplete)
          {
              isVisible = true;

              // Create standalone canvas that renders above all other UI
              Canvas splashCanvas = gameObject.AddComponent<Canvas>();
              splashCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
              splashCanvas.sortingOrder = 100;

              CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
              scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
              scaler.referenceResolution = new Vector2(1080f, 1920f);
              scaler.matchWidthOrHeight = 0.5f;
              scaler.referencePixelsPerUnit = 100f;

              gameObject.AddComponent<GraphicRaycaster>();

              // Root CanvasGroup for fade in/out
              CanvasGroup rootGroup = gameObject.AddComponent<CanvasGroup>();
              rootGroup.alpha = 0f;
              rootGroup.blocksRaycasts = false;

              // Full-screen background
              GameObject bgObj = new("SplashBg");
              bgObj.transform.SetParent(transform, false);
              RectTransform bgRect = bgObj.AddComponent<RectTransform>();
              bgRect.anchorMin = Vector2.zero;
              bgRect.anchorMax = Vector2.one;
              bgRect.offsetMin = Vector2.zero;
              bgRect.offsetMax = Vector2.zero;
              RawImage bg = bgObj.AddComponent<RawImage>();
              bg.color = backgroundTexture != null ? Color.white : Color.black;
              bg.texture = backgroundTexture;
              bg.raycastTarget = false;

              // TOWERMAZE title text
              GameObject titleRoot = new("TitleRoot");
              titleRoot.transform.SetParent(transform, false);
              RectTransform titleRect = titleRoot.AddComponent<RectTransform>();
              titleRect.anchorMin = new Vector2(0f, 0.78f);
              titleRect.anchorMax = new Vector2(1f, 0.93f);
              titleRect.offsetMin = Vector2.zero;
              titleRect.offsetMax = Vector2.zero;
              CanvasGroup titleGroup = titleRoot.AddComponent<CanvasGroup>();
              titleGroup.alpha = 0f;

              GameObject titleObj = new("TitleText");
              titleObj.transform.SetParent(titleRoot.transform, false);
              RectTransform textRect = titleObj.AddComponent<RectTransform>();
              textRect.anchorMin = Vector2.zero;
              textRect.anchorMax = Vector2.one;
              textRect.offsetMin = Vector2.zero;
              textRect.offsetMax = Vector2.zero;
              Text titleText = titleObj.AddComponent<Text>();
              titleText.text = "TOWERMAZE";
              titleText.font = font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
              titleText.fontSize = 96;
              titleText.fontStyle = FontStyle.Bold;
              titleText.alignment = TextAnchor.MiddleCenter;
              titleText.color = new Color(1f, 0.48f, 0f, 1f); // orange #FF7A00
              titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
              titleText.verticalOverflow = VerticalWrapMode.Overflow;
              titleText.raycastTarget = false;

              // Spinner ring
              Sprite spinnerSprite = Resources.Load<Sprite>("TowerMaze/UITheme/SpinnerRing");
              GameObject spinnerObj = null;
              Image spinnerImage = null;
              if (spinnerSprite != null)
              {
                  spinnerObj = new("Spinner");
                  spinnerObj.transform.SetParent(transform, false);
                  RectTransform spinRect = spinnerObj.AddComponent<RectTransform>();
                  spinRect.anchorMin = new Vector2(0.5f, 0f);
                  spinRect.anchorMax = new Vector2(0.5f, 0f);
                  spinRect.pivot = new Vector2(0.5f, 0.5f);
                  spinRect.anchoredPosition = new Vector2(0f, 120f);
                  spinRect.sizeDelta = new Vector2(80f, 80f);
                  spinnerImage = spinnerObj.AddComponent<Image>();
                  spinnerImage.sprite = spinnerSprite;
                  spinnerImage.type = Image.Type.Filled;
                  spinnerImage.fillMethod = Image.FillMethod.Radial360;
                  spinnerImage.fillAmount = 0.75f;
                  spinnerImage.color = new Color(1f, 0.48f, 0f, 0.9f);
                  spinnerImage.raycastTarget = false;
              }

              StartCoroutine(SplashRoutine(rootGroup, titleRoot.transform, titleGroup, spinnerObj, onComplete));
          }

          private System.Collections.IEnumerator SplashRoutine(
              CanvasGroup rootGroup,
              Transform titleTransform,
              CanvasGroup titleGroup,
              GameObject spinnerObj,
              Action onComplete)
          {
              float startTime = Time.realtimeSinceStartup;
              const float minDisplayTime = 2.5f;
              const float fadeInDuration = 0.3f;
              const float textAnimDuration = 0.5f;
              const float fadeOutDuration = 0.5f;

              // Fade in root
              float t = 0f;
              while (t < fadeInDuration)
              {
                  rootGroup.alpha = t / fadeInDuration;
                  t += Time.unscaledDeltaTime;
                  yield return null;
              }
              rootGroup.alpha = 1f;

              // Animate title: scale + alpha
              titleTransform.localScale = new Vector3(0.8f, 0.8f, 1f);
              t = 0f;
              while (t < textAnimDuration)
              {
                  float progress = t / textAnimDuration;
                  float eased = 1f - (1f - progress) * (1f - progress); // ease out quad
                  titleTransform.localScale = Vector3.Lerp(new Vector3(0.8f, 0.8f, 1f), Vector3.one, eased);
                  titleGroup.alpha = eased;
                  t += Time.unscaledDeltaTime;
                  yield return null;
              }
              titleTransform.localScale = Vector3.one;
              titleGroup.alpha = 1f;

              // Spin and wait for minimum display time
              float spinnerAngle = 0f;
              while (Time.realtimeSinceStartup - startTime < minDisplayTime)
              {
                  if (spinnerObj != null)
                  {
                      spinnerAngle -= 360f * Time.unscaledDeltaTime;
                      spinnerObj.transform.localRotation = Quaternion.Euler(0f, 0f, spinnerAngle);
                  }
                  yield return null;
              }

              // Fade out root
              t = 0f;
              float startAlpha = rootGroup.alpha;
              while (t < fadeOutDuration)
              {
                  rootGroup.alpha = Mathf.Lerp(startAlpha, 0f, t / fadeOutDuration);
                  if (spinnerObj != null)
                  {
                      spinnerAngle -= 360f * Time.unscaledDeltaTime;
                      spinnerObj.transform.localRotation = Quaternion.Euler(0f, 0f, spinnerAngle);
                  }
                  t += Time.unscaledDeltaTime;
                  yield return null;
              }

              isVisible = false;
              onComplete?.Invoke();
              Destroy(gameObject);
          }
      }
  ```

- [ ] **Step 3: Verify the file compiles in Unity**

  Switch to the Unity Editor. Wait for script compilation. The Console should show **0 errors**. If there are errors, fix them before continuing.

- [ ] **Step 4: Commit**

  ```bash
  git add Assets/Scripts/TowerMaze/Runtime/UISystems.cs
  git commit -m "feat: add SplashScreenController to UISystems"
  ```

---

## Chunk 3: UIManager modifications

### Task 4: Add splash deferral to UIManager

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs` (UIManager class, lines ~20–170)

- [ ] **Step 1: Add new fields to UIManager**

  In `UIManager`, after the existing field declarations (after line 46, `private bool cachedVibrationEnabled;`), add:

  ```csharp
          private bool splashComplete;
          private Action pendingShowStart;
  ```

- [ ] **Step 2: Modify UIManager.Initialize() signature**

  Change the first line of `Initialize()` from:
  ```csharp
          public void Initialize(
              ThemeDefinition definition,
  ```
  To:
  ```csharp
          public void Initialize(
              bool splashActive,
              ThemeDefinition definition,
  ```

- [ ] **Step 3: Set splashComplete at start of Initialize()**

  At the very beginning of `Initialize()` body (before `theme = definition;` on line 67), add:
  ```csharp
              splashComplete = !splashActive;
  ```

- [ ] **Step 4: Modify ShowStart() to defer when splash is active**

  The current `ShowStart()` starts (line 117):
  ```csharp
          public void ShowStart(float bestScore, int emberBalance, IReadOnlyList<LeaderboardEntry> leaderboardEntries, IReadOnlyList<DailyMissionState> dailyMissions, DailyChestStatus chestStatus, DailyChallengeStatus challengeStatus, int missionRerollCost, bool soundEnabled, bool vibrationEnabled)
          {
              cachedBestScore = bestScore;
  ```

  Change it to add a deferral guard at the top:
  ```csharp
          public void ShowStart(float bestScore, int emberBalance, IReadOnlyList<LeaderboardEntry> leaderboardEntries, IReadOnlyList<DailyMissionState> dailyMissions, DailyChestStatus chestStatus, DailyChallengeStatus challengeStatus, int missionRerollCost, bool soundEnabled, bool vibrationEnabled)
          {
              if (!splashComplete)
              {
                  pendingShowStart = () => ShowStart(bestScore, emberBalance, leaderboardEntries, dailyMissions, chestStatus, challengeStatus, missionRerollCost, soundEnabled, vibrationEnabled);
                  return;
              }
              cachedBestScore = bestScore;
  ```

- [ ] **Step 5: Add OnSplashComplete() method**

  Add the following method to UIManager, after `ShowStart()` and before `UpdateCachedLeaderboard()` (around line 138):

  ```csharp
          internal void OnSplashComplete()
          {
              splashComplete = true;
              Action pending = pendingShowStart;
              pendingShowStart = null; // clear before invoke to prevent re-entrancy issues
              pending?.Invoke();
          }
  ```
  Note: The spec clears `pendingShowStart` after invoking; this implementation clears it first (safer). This is an intentional improvement over the spec.

- [ ] **Step 6: Verify compilation**

  Switch to Unity Editor. Wait for recompile. Console should show **0 errors**.

- [ ] **Step 7: Verify compilation — IMPORTANT**

  Switch to Unity Editor. Wait for recompile.

  > **Expected:** 1 compile error — `TowerMazeBootstrapper.cs` line ~93 still calls `uiManager.Initialize(themeDefinition, ...)` with the old signature. This is expected and will be fixed in the very next step (Task 5 Step 2). Do NOT commit until after applying Task 5 Step 2.

- [ ] **Step 8: Commit (do after Task 5 Step 2 is also applied)**

  After applying both Chunk 3 and Chunk 4 changes, Unity should compile with 0 errors. Then commit both files together:

  ```bash
  git add Assets/Scripts/TowerMaze/Runtime/UISystems.cs
  git add Assets/Scripts/TowerMaze/Runtime/TowerMazeBootstrapper.cs
  git commit -m "feat: add SplashScreenController wiring (UIManager deferral + Bootstrapper)"
  ```

---

## Chunk 4: Bootstrapper wiring

### Task 5: Wire SplashScreenController in TowerMazeBootstrapper

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/TowerMazeBootstrapper.cs` (Awake method)

- [ ] **Step 1: Add splash creation block after uiManager assignment**

  In `TowerMazeBootstrapper.Awake()`, find line 79:
  ```csharp
              UIManager uiManager = EnsureComponent<UIManager>(uiRoot);
  ```

  Immediately after this line (before `towerGenerator.Initialize(...)`), add:
  ```csharp
              // --- Splash Screen ---
              // uiManager is now assigned — safe to capture in the callback lambda.
              // Font loaded early; UIManager.Initialize will also load it (intentional duplication).
              // Timing note: StartCoroutine defers to the next frame, so Awake() completes fully
              // (including uiManager.Initialize below) before the coroutine's first yield.
              // The callback therefore always fires after uiManager is fully initialized.
              Texture2D splashTex = Resources.Load<Texture2D>("TowerMaze/UITheme/SplashBackground");
              Font splashFont = Resources.Load<Font>("TowerMaze/UITheme/Kenney Future");
              SplashScreenController splashController = new GameObject("SplashScreen")
                  .AddComponent<SplashScreenController>();
              splashController.Initialize(splashFont, splashTex, onComplete: () => uiManager.OnSplashComplete());
              // --- End Splash Screen ---
  ```

- [ ] **Step 2: Update uiManager.Initialize() call to pass splashActive: true**

  Find the existing `uiManager.Initialize(...)` call (around line 93). It currently starts:
  ```csharp
              uiManager.Initialize(themeDefinition, economyManager,
  ```

  Change it to:
  ```csharp
              uiManager.Initialize(splashActive: true, themeDefinition, economyManager,
  ```

  The rest of the arguments remain unchanged.

- [ ] **Step 3: Verify compilation**

  Switch to Unity Editor. Wait for recompile. Console should show **0 errors**.

- [ ] **Step 4: First Play Mode test**

  Enter Play Mode in Unity Editor (**standard mode** — ensure Fast Enter Play Mode is disabled in Editor → Project Settings → Editor → Enter Play Mode Settings).

  Expected behaviour:
  - The splash screen covers the entire Game view immediately
  - Background shows the TowerMaze tower image (or black if asset not yet present)
  - "TOWERMAZE" text fades and scales in from smaller size
  - Spinner ring rotates continuously
  - After approximately 2.5 seconds, splash fades out
  - Start Screen appears normally

  Check Console for errors. If any NullReferenceExceptions appear, check:
  - `SplashBackground.png` import type is `Default` (not Sprite)
  - `SpinnerRing.png` exists and is imported as Sprite

- [ ] **Step 5: Verify return-to-menu flow is unaffected**

  In Play Mode:
  - Start a run (tap/click Play)
  - Die or fail
  - Return to main menu
  - Start Screen should appear instantly without any delay (splash guard is one-shot)

- [ ] **Step 6: Commit**

  ```bash
  git add Assets/Scripts/TowerMaze/Runtime/TowerMazeBootstrapper.cs
  git commit -m "feat: wire SplashScreenController into TowerMazeBootstrapper"
  ```

---

## Chunk 5: Unity Player Settings splash (manual)

### Task 6: Configure Unity's built-in Splash Screen

**Files:**
- Modify: Project Settings → Player → Splash Screen (no code file)

- [ ] **Step 1: Import SplashBackground as Sprite for Player Settings use**

  In Unity, duplicate the import of the same image for Sprite use:
  - In Project window, select `Assets/Resources/TowerMaze/UITheme/SplashBackground.png`
  - Press Ctrl+D to duplicate it
  - Rename the duplicate to `SplashBackground_Splash`
  - Move it to `Assets/TowerMaze/` (outside Resources — it's only needed at build time, not runtime)
  - In its Inspector: set **Texture Type** to `Sprite (2D and UI)`, click Apply

- [ ] **Step 2: Open Player Settings**

  Menu: **Edit → Project Settings → Player**

- [ ] **Step 3: Configure Splash Screen**

  Click the **Splash Screen** section in Player Settings.

  Set the following:

  **Logos section:**
  - **Show Unity Logo:** uncheck (disable the Unity logo overlay)
  - If there are any entries in the **Logos list**, remove them with the `-` button

  **Background section** (this is where the full-screen image goes in Unity 6 — NOT the Logos list):
  - **Background Image:** assign `SplashBackground_Splash`
  - **Overlay Opacity:** `0` (removes any tint)
  - **Background Color:** `#000000` (black — shown if image fails to load)

  **Splash Style:**
  - **Animation:** `Static`
  - **Duration:** `2` seconds

- [ ] **Step 4: Verify in a development build (optional)**

  Make a Development Build (File → Build Settings → check "Development Build" → Build And Run on device or PC).
  Expected: Unity splash shows the TowerMaze key art for ~2 seconds before the scene loads, then the custom in-game SplashScreenController shows.

- [ ] **Step 5: Commit ProjectSettings changes**

  ```bash
  git add ProjectSettings/ProjectSettings.asset
  git commit -m "feat: configure Unity Splash Screen with TowerMaze key art"
  ```

---

## Final Verification Checklist

- [ ] Play Mode in Unity Editor: splash covers screen, text animates, spinner spins, fades after 2.5s, Start Screen appears
- [ ] Console: 0 errors during splash lifecycle
- [ ] Null-safe: temporarily remove `SplashBackground.png` → black background, no crash; restore it
- [ ] Return-to-menu: `ShowStart()` after splash has completed executes immediately, no delay
- [ ] Retry run: same — no regression in existing game flow
- [ ] Device build: Unity engine splash (key art) precedes the custom in-game overlay
