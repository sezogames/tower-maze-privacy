# Button Click Sound Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Play `click-a.ogg` whenever any UI button is pressed, respecting the existing `SoundEnabled` toggle.

**Architecture:** Add `PlayButtonClick()` to `AudioManager`, add a static `BindButton` helper to `UIManager`, thread an `Action onButtonClick` callback through `UIManager.Initialize` and all four sub-controller `Initialize` methods, replace every `onClick.AddListener` call with `BindButton`. Wire up from `TowerMazeBootstrapper`.

**Tech Stack:** Unity C#, `UnityEngine.UI.Button`, `AudioSource.PlayOneShot`, `Resources.Load<AudioClip>`.

---

## Chunk 1: Audio Asset + AudioManager Method

### Task 1: Copy audio clip to Resources

**Files:**
- Create: `Assets/Resources/TowerMaze/Sounds/click-a.ogg` (copy from ThirdParty)

- [ ] **Step 1: Create the Sounds folder and copy the file**

In Windows Explorer or via file system, copy:
```
Assets/ThirdParty/UI/Kenney/Sounds/click-a.ogg
→ Assets/Resources/TowerMaze/Sounds/click-a.ogg
```

Unity will auto-generate the `.meta` file when the Editor re-imports.

- [ ] **Step 2: Verify in Unity Editor**

Open Unity. In the Project window, navigate to `Assets/Resources/TowerMaze/Sounds/` and confirm `click-a.ogg` appears as an AudioClip asset.

---

### Task 2: Add PlayButtonClick to AudioManager

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs` (AudioManager class, ~lines 1992–2098)

- [ ] **Step 0: Verify `using System;` is present in RunSystems.cs**

Open `RunSystems.cs` and confirm `using System;` exists near the top. `AudioManager` uses `Action` indirectly via other parts of the file, so it should already be there — but confirm before proceeding.

- [ ] **Step 1: Add `buttonClickClip` field**

In `AudioManager`, after the `wallBumpClip` field declaration (line ~1999):

```csharp
private AudioClip wallBumpClip;
private AudioClip buttonClickClip;  // ADD THIS LINE
private AudioClip rushAlarmClip;    // already present — shown for context
```

- [ ] **Step 2: Load the clip in Awake**

In `AudioManager.Awake()`, after the line `wallBumpClip = CreateToneClip(...)` (line ~2040) and before `rushAlarmClip = CreateSirenClip(...)`:

```csharp
wallBumpClip    = CreateToneClip("WallBump", 310f, 0.045f, 0.12f);
buttonClickClip = Resources.Load<AudioClip>("TowerMaze/Sounds/click-a"); // ADD THIS LINE
rushAlarmClip   = CreateSirenClip("RushAlarm", 1.25f, 520f, 860f, 0.14f); // already present — shown for context
```

- [ ] **Step 3: Add PlayButtonClick method**

After `PlayWallBump()` (line ~2098), add:

```csharp
public void PlayButtonClick()
{
    PlayClip(buttonClickClip, 0.75f);
}
```

- [ ] **Step 4: Verify compiles in Unity**

Switch to Unity Editor, wait for compilation. Check Console: no errors.

---

## Chunk 2: UIManager Wiring

### Task 3: Add BindButton static helper to UIManager

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs` (UIManager class, ~line 710)

- [ ] **Step 1: Add BindButton after CreateButton**

In `UIManager`, after the `CreateButton` static method (~line 750), add:

```csharp
internal static void BindButton(Button btn, Action action, Action soundCallback = null)
{
    btn.onClick.AddListener(() =>
    {
        soundCallback?.Invoke();
        action?.Invoke();
    });
}
```

---

### Task 4: Add onButtonClick to UIManager

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs` (UIManager class)

- [ ] **Step 1: Add buttonClickSound field**

In `UIManager`, after `cachedVibrationEnabled` field (~line 78), add:

```csharp
private Action buttonClickSound;
```

- [ ] **Step 2: Add onButtonClick parameter to UIManager.Initialize**

Modify the `Initialize` signature to add `Action onButtonClick = null` at the end (before the optional `Sprite staticBackground` parameter, or after it — put it just before `staticBackground`):

```csharp
public void Initialize(
    bool splashActive,
    ThemeDefinition definition,
    EconomyManager economy,
    RewardedAdManager rewardedAds,
    CoinStoreManager coinStore,
    PlayerController player,
    Action onPlay,
    Action onPlayDailyChallenge,
    Action onRetry,
    Action onContinue,
    Action onReturnToMenu,
    Action onClaimDoubleReward,
    Action onWatchLifeRefillAd,
    Action onBuyLifeRefillWithCoins,
    Action onToggleSound,
    Action onToggleVibration,
    Action onButtonClick = null,        // ADD THIS
    Sprite staticBackground = null)
```

- [ ] **Step 3: Store the callback**

At the top of the `Initialize` method body, after `playerController = player;`, add:

```csharp
buttonClickSound = onButtonClick;
```

- [ ] **Step 4: Pass buttonClickSound to sub-controller Initializes**

Update each sub-controller Initialize call to include `buttonClickSound` as the last argument (details in Task 5–8 below).

---

### Task 5: Thread through ShopScreenController

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs` (ShopScreenController, ~line 1554)

- [ ] **Step 1: Add onButtonClick param to ShopScreenController.Initialize**

Change the signature at ~line 1591:
```csharp
public void Initialize(Font font, ThemeDefinition themeDefinition, Action onClose, Action onClaimCoinBoost, Action onRestorePurchases, Action<ShopCatalogType, string> onSelectItem, Action onButtonClick = null)
```

- [ ] **Step 2: Store it**

Add field in ShopScreenController:
```csharp
private Action buttonClickSound;
```

At start of Initialize body:
```csharp
buttonClickSound = onButtonClick;
```

- [ ] **Step 3: Replace onClick.AddListener calls (lines ~1622–1989)**

Replace:
```csharp
adRewardButton.onClick.AddListener(() => onClaimCoinBoost?.Invoke());
restoreButton.onClick.AddListener(() => onRestorePurchases?.Invoke());
exitButton.onClick.AddListener(() => onClose());
coinsTabButton.onClick.AddListener(() => SwitchCategory(ShopCatalogType.Coin));
ballsTabButton.onClick.AddListener(() => SwitchCategory(ShopCatalogType.Ball));
towersTabButton.onClick.AddListener(() => SwitchCategory(ShopCatalogType.Tower));
```

With:
```csharp
UIManager.BindButton(adRewardButton, () => onClaimCoinBoost?.Invoke(), buttonClickSound);
UIManager.BindButton(restoreButton, () => onRestorePurchases?.Invoke(), buttonClickSound);
UIManager.BindButton(exitButton, () => onClose(), buttonClickSound);
UIManager.BindButton(coinsTabButton, () => SwitchCategory(ShopCatalogType.Coin), buttonClickSound);
UIManager.BindButton(ballsTabButton, () => SwitchCategory(ShopCatalogType.Ball), buttonClickSound);
UIManager.BindButton(towersTabButton, () => SwitchCategory(ShopCatalogType.Tower), buttonClickSound);
```

Also the item buttons line (~line 1989):
```csharp
// Before:
button.onClick.AddListener(() => onItemSelected?.Invoke(currentCategory, itemIds[capturedIndex]));
// After:
UIManager.BindButton(button, () => onItemSelected?.Invoke(currentCategory, itemIds[capturedIndex]), buttonClickSound);
```

- [ ] **Step 4: Update UIManager call site (~line 154)**

```csharp
shopScreenController.Initialize(runtimeFont, theme, HideShop, HandleShopCoinBoost, HandleCoinStoreRestore, HandleShopAction, buttonClickSound);
```

---

### Task 6: Thread through StartScreenController

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs` (StartScreenController, ~line 2235)

- [ ] **Step 1: Add onButtonClick param to StartScreenController.Initialize**

Change signature at ~line 2272:
```csharp
public void Initialize(Font font, ThemeDefinition theme, EconomyManager economy, Action onPlay, Action onPlayDailyChallenge, Action onOpenShop, Action onClaimChest, Action onToggleSound, Action onToggleVibration, Action onRerollMissions, Action onButtonClick = null)
```

- [ ] **Step 2: Store it**

Add field:
```csharp
private Action buttonClickSound;
```

At start of Initialize body:
```csharp
buttonClickSound = onButtonClick;
```

- [ ] **Step 3: Replace onClick.AddListener calls (~lines 2290–2589)**

```csharp
// Before:
shopButton.onClick.AddListener(() => onOpenShop());
rerollButton.onClick.AddListener(() => onRerollMissions?.Invoke());
chestButton.onClick.AddListener(() => onClaimChest());
challengeButton.onClick.AddListener(() => onPlayDailyChallenge?.Invoke());
playButton.onClick.AddListener(() => onPlay());
soundToggleBtn.onClick.AddListener(() => onToggleSound());
vibeToggleBtn.onClick.AddListener(() => onToggleVibration());
settingsLangTRBtn.onClick.AddListener(() => { ... });
settingsLangENBtn.onClick.AddListener(() => { ... });
settingsLangESBtn.onClick.AddListener(() => { ... });
closePanelBtn.onClick.AddListener(() => settingsPanel.SetActive(false));
gearButton.onClick.AddListener(() => settingsPanel.SetActive(true));

// After (same pattern for all):
UIManager.BindButton(shopButton, () => onOpenShop(), buttonClickSound);
UIManager.BindButton(rerollButton, () => onRerollMissions?.Invoke(), buttonClickSound);
UIManager.BindButton(chestButton, () => onClaimChest(), buttonClickSound);
UIManager.BindButton(challengeButton, () => onPlayDailyChallenge?.Invoke(), buttonClickSound);
UIManager.BindButton(playButton, () => onPlay(), buttonClickSound);
UIManager.BindButton(soundToggleBtn, () => onToggleSound(), buttonClickSound);
UIManager.BindButton(vibeToggleBtn, () => onToggleVibration(), buttonClickSound);
UIManager.BindButton(settingsLangTRBtn, () => { ... }, buttonClickSound);
UIManager.BindButton(settingsLangENBtn, () => { ... }, buttonClickSound);
UIManager.BindButton(settingsLangESBtn, () => { ... }, buttonClickSound);
UIManager.BindButton(closePanelBtn, () => settingsPanel.SetActive(false), buttonClickSound);
UIManager.BindButton(gearButton, () => settingsPanel.SetActive(true), buttonClickSound);
```

Note: for `settingsLangTR/EN/ES` buttons, the lambda bodies may contain multi-line code — wrap the entire existing lambda body inside `UIManager.BindButton(..., () => { <existing body> }, buttonClickSound)`.

- [ ] **Step 4: Update UIManager call site (~line 139)**

```csharp
startScreenController.Initialize(runtimeFont, theme, economyManager, onPlay, onPlayDailyChallenge, ShowShop, HandleChestClaim, onToggleSound, onToggleVibration, HandleMissionReroll, buttonClickSound);
```

---

### Task 7: Thread through FailScreenController

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs` (FailScreenController, ~line 2761)

- [ ] **Step 1: Add onButtonClick param to FailScreenController.Initialize**

Change signature at ~line 2786:
```csharp
public void Initialize(Font font, ThemeDefinition theme, EconomyManager economy, Action onRetry, Action onContinue, Action onReturnToMenu, Action onClaimDoubleReward, Action onWatchLifeRefillAd, Action onBuyLifeRefillWithCoins, Action onButtonClick = null)
```

- [ ] **Step 2: Store it**

Add field:
```csharp
private Action buttonClickSound;
```

At start of Initialize body:
```csharp
buttonClickSound = onButtonClick;
```

- [ ] **Step 3: Replace onClick.AddListener calls (~lines 2850–2867)**

```csharp
// Before:
retryButton.onClick.AddListener(HandleRetryPressed);
menuActionButton.onClick.AddListener(() => onReturnToMenu());
continueButton.onClick.AddListener(HandleContinuePressed);
rewardButton.onClick.AddListener(() => onClaimDoubleReward());

// After:
UIManager.BindButton(retryButton, HandleRetryPressed, buttonClickSound);
UIManager.BindButton(menuActionButton, () => onReturnToMenu(), buttonClickSound);
UIManager.BindButton(continueButton, HandleContinuePressed, buttonClickSound);
UIManager.BindButton(rewardButton, () => onClaimDoubleReward(), buttonClickSound);
```

- [ ] **Step 4: Update UIManager call site (~line 142)**

```csharp
failScreenController.Initialize(runtimeFont, theme, economyManager, onRetry, onContinue, onReturnToMenu, onClaimDoubleReward, onWatchLifeRefillAd, onBuyLifeRefillWithCoins, buttonClickSound);
```

---

### Task 8: Thread through IAPUpsellController

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems.cs` (IAPUpsellController, ~line 2969)

- [ ] **Step 1: Add onButtonClick param to IAPUpsellController.Initialize**

Change signature at ~line 2982:
```csharp
public void Initialize(Font font, ThemeDefinition themeDefinition, Action<string> purchaseCallback, Action onButtonClick = null)
```

- [ ] **Step 2: Store it**

Add field:
```csharp
private Action buttonClickSound;
```

In `Initialize`, assign the field as the **very first line**, before `BuildUI()` is called — `BuildUI()` creates the buttons and registers listeners, so `buttonClickSound` must be set before it runs:
```csharp
public void Initialize(Font font, ThemeDefinition themeDefinition, Action<string> purchaseCallback, Action onButtonClick = null)
{
    buttonClickSound = onButtonClick;  // MUST be first — BuildUI() registers listeners immediately after
    // ... rest of existing Initialize body (including BuildUI() call)
}
```

- [ ] **Step 3: Replace onClick.AddListener calls (~lines 3024, 3066)**

```csharp
// Before:
closeBtn.onClick.AddListener(OnCloseClicked);
buyButton.onClick.AddListener(OnBuyClicked);

// After:
UIManager.BindButton(closeBtn, OnCloseClicked, buttonClickSound);
UIManager.BindButton(buyButton, OnBuyClicked, buttonClickSound);
```

- [ ] **Step 4: Update UIManager call site (~line 162)**

```csharp
iapUpsellController.Initialize(runtimeFont, theme, id => TriggerUpsellPurchase(id), buttonClickSound);
```

---

## Chunk 3: Bootstrapper Wiring + Final Verification

### Task 9: Wire up in TowerMazeBootstrapper

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/TowerMazeBootstrapper.cs` (~line 108)

- [ ] **Step 1: Add onButtonClick to uiManager.Initialize call**

Find the `uiManager.Initialize(...)` call (~line 108). Add `audioManager.PlayButtonClick` just before `staticBgSprite`:

```csharp
uiManager.Initialize(
    splashActive: true,
    themeDefinition,
    economyManager,
    rewardedAdManager,
    coinStoreManager,
    playerController,
    runManager.StartRun,
    runManager.StartDailyChallenge,
    runManager.RetryRun,
    runManager.ContinueRun,
    runManager.ReturnToMainMenu,
    runManager.ClaimDoubleReward,
    runManager.WatchAdForLifeRefill,
    runManager.BuyLifeRefillWithCoins,
    runManager.ToggleSound,
    runManager.ToggleVibration,
    audioManager.PlayButtonClick,   // ADD THIS
    staticBgSprite);
```

- [ ] **Step 2: Verify compiles in Unity Editor**

Switch to Unity. Confirm no compile errors in Console.

- [ ] **Step 3: Play-test in Editor**

Press Play. Open the main menu and tap several buttons (Play, Shop, Settings gear, tabs in Shop). Confirm click sound plays on each. Toggle Sound OFF in settings — confirm clicks go silent.

---
