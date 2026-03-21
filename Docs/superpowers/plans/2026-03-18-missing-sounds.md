# Missing Sounds Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add sound feedback for zone transitions, mission completion, new best score, and coin rewards using existing Kenney .ogg files.

**Architecture:** Copy 4 audio files to Resources, add 4 clip fields + methods to `AudioManager` in `RunSystems.cs`, then call them at 5 event points in `RunManager` (same file). All sounds use the existing `PlayClip()` infrastructure which enforces `SoundEnabled`.

**Tech Stack:** Unity C#, `AudioSource.PlayOneShot`, `Resources.Load<AudioClip>`.

---

## Chunk 1: Audio Assets + AudioManager Methods

### Task 1: Copy audio clips to Resources

**Files:**
- Create: `Assets/Resources/TowerMaze/Sounds/switch-a.ogg`
- Create: `Assets/Resources/TowerMaze/Sounds/switch-b.ogg`
- Create: `Assets/Resources/TowerMaze/Sounds/tap-a.ogg`
- Create: `Assets/Resources/TowerMaze/Sounds/tap-b.ogg`

- [ ] **Step 1: Copy the 4 files**

```
Assets/ThirdParty/UI/Kenney/Sounds/switch-a.ogg → Assets/Resources/TowerMaze/Sounds/switch-a.ogg
Assets/ThirdParty/UI/Kenney/Sounds/switch-b.ogg → Assets/Resources/TowerMaze/Sounds/switch-b.ogg
Assets/ThirdParty/UI/Kenney/Sounds/tap-a.ogg    → Assets/Resources/TowerMaze/Sounds/tap-a.ogg
Assets/ThirdParty/UI/Kenney/Sounds/tap-b.ogg    → Assets/Resources/TowerMaze/Sounds/tap-b.ogg
```

Unity will auto-generate `.meta` files on re-import.

- [ ] **Step 2: Verify in Unity Editor**

Confirm all 4 appear as AudioClip assets under `Assets/Resources/TowerMaze/Sounds/`.

---

### Task 2: Add 4 clip fields + methods to AudioManager

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs` (AudioManager class, ~lines 1992–2110)

AudioManager already has this pattern — follow it exactly:
- Field: `private AudioClip xyzClip;`
- Awake load: `xyzClip = Resources.Load<AudioClip>("TowerMaze/Sounds/xyz");`
- Method: `public void PlayXyz() { PlayClip(xyzClip, volumeScale); }`

Existing reference: `buttonClickClip` (line ~2000), loaded at `Awake` (line ~2042), method `PlayButtonClick()` (line ~2102).

- [ ] **Step 1: Add 4 clip fields**

After `private AudioClip buttonClickClip;` (line ~2000), add:

```csharp
private AudioClip zoneReachedClip;
private AudioClip missionCompleteClip;
private AudioClip newBestClip;
private AudioClip rewardClip;
```

- [ ] **Step 2: Load clips in Awake**

After `buttonClickClip = Resources.Load<AudioClip>("TowerMaze/Sounds/click-a");` (line ~2042), add:

```csharp
zoneReachedClip    = Resources.Load<AudioClip>("TowerMaze/Sounds/switch-a");
missionCompleteClip = Resources.Load<AudioClip>("TowerMaze/Sounds/tap-a");
newBestClip        = Resources.Load<AudioClip>("TowerMaze/Sounds/switch-b");
rewardClip         = Resources.Load<AudioClip>("TowerMaze/Sounds/tap-b");
```

- [ ] **Step 3: Add 4 public methods**

After `PlayButtonClick()` (line ~2104), add:

```csharp
public void PlayZoneReached()
{
    PlayClip(zoneReachedClip, 0.7f);
}

public void PlayMissionComplete()
{
    PlayClip(missionCompleteClip, 0.85f);
}

public void PlayNewBest()
{
    PlayClip(newBestClip, 0.9f);
}

public void PlayReward()
{
    PlayClip(rewardClip, 0.75f);
}
```

- [ ] **Step 4: Verify compiles in Unity Editor**

Switch to Unity. Confirm no compile errors in Console.

---

## Chunk 2: RunManager Call Sites

### Task 3: Add 5 audioManager calls in RunManager

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs` (RunManager class — 5 locations)

`audioManager` is a `private AudioManager` field in `RunManager`, already used for `PlayFailCue()`, `PlayCountdownTick()` etc.

- [ ] **Step 1: Zone transition sound (~line 2647)**

Find:
```csharp
uiManager.QueueRewardToast($"ZONE {currentZone + 1}", "NEW ZONE", new Color(0.2f, 0.85f, 0.9f));
```

Add immediately after:
```csharp
audioManager.PlayZoneReached();
```

- [ ] **Step 2: New best sound — in FailRun() (~line 2905)**

Find in `FailRun()`:
```csharp
if (activeRunMode == RunMode.Normal) { scoreManager.CommitBest(); }
```

Replace with:
```csharp
if (activeRunMode == RunMode.Normal)
{
    if (scoreManager.IsNewBestThisRun) { audioManager.PlayNewBest(); }
    scoreManager.CommitBest();
}
```

**Critical:** `IsNewBestThisRun` becomes false after `CommitBest()` — the check must be before the commit.

- [ ] **Step 3: Mission complete sound (~line 3052)**

Find:
```csharp
uiManager.QueueRewardToast(title, $"+{missionReward.rewardEmber} COIN", new Color(1f, 0.82f, 0.32f, 1f));
```

Add immediately after:
```csharp
audioManager.PlayMissionComplete();
```

- [ ] **Step 4: Rewarded ad sound (~line 2891)**

Find:
```csharp
uiManager.QueueRewardToast("REWARDED", $"+{claimedRewardAmountThisFail} COIN", new Color(1f, 0.72f, 0.28f, 1f));
```

Add immediately after:
```csharp
audioManager.PlayReward();
```

- [ ] **Step 5: Daily challenge reward sound (~line 3057)**

Find:
```csharp
uiManager.QueueRewardToast("DAILY CHALLENGE", $"+{challengeReward.RewardCoins} COIN", new Color(0.32f, 0.82f, 1f, 1f));
```

Add immediately after:
```csharp
audioManager.PlayReward();
```

- [ ] **Step 6: Verify compiles in Unity Editor**

Confirm no compile errors.

- [ ] **Step 7: Play-test in Editor**

Press Play. Verify:
- Yeni bölgeye geçişte ses çalıyor
- Run bitişinde yeni rekor varsa ses çalıyor (rekor yoksa çalmıyor)
- Görev tamamlanırken ses çalıyor
- Rewarded ad sonrası ses çalıyor
- Sound OFF iken hiçbiri çalmıyor
