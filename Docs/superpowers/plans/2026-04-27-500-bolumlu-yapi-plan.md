# 500 Bölümlü Yapı — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** TowerMaze chapter sistemini 50'den 500 bölüme çıkar; tower rotation ve top hızını endless modla aynı tutarken sinkSpeed/maze karmaşıklığı/targetHeight'ı per-chapter değişkenle. Optimal oynanışla bitirilebilir olduğunu solver'la garantile.

**Architecture:** ChapterDefinition'a yeni alanlar (TierIndex, Complexity, SinkSpeed, MazeSettings); MazeGenerator'a `GenerateWithSettings` overload; yeni ChapterValidator (A* + re-roll + PlayerPrefs cache) Bootstrapper'da first-launch'ta çalışır; tier 50/100/.../500'de TierCelebrationScreen tetiklenir; ChapterSelectScreen 10 tier section'lı vertical scroll'a refactor edilir.

**Tech Stack:** Unity 2022 LTS, C# 9, Unity UI (UGUI), MonoBehaviour-based managers, ScriptableObject (DifficultyProfile, GameConfig), PlayerPrefs persistence, procedural sprite generation.

**Spec:** `Docs/superpowers/specs/2026-04-27-500-bolumlu-yapi-design.md`

**Verification model:** Bu codebase'de unit test framework yok (Tests/ dizini yok). Verification yöntemi: (1) Unity Editor Play mode'da oyna, davranışı gözlemle; (2) Debug.Log assert benzeri kontroller; (3) Editor menu item'larıyla doğrulama; (4) PlayerPrefs içeriğini PlayerPrefs Editor extension veya log'la kontrol.

---

## File Structure

### Yeni dosyalar

| Dosya | Sorumluluk |
|-------|------------|
| `Assets/Scripts/TowerMaze/Runtime/ChapterValidator.cs` | A* solver, maze adapter, re-roll loop, PlayerPrefs cache. Coroutine API. |
| `Assets/Scripts/TowerMaze/Runtime/MazeSettings.cs` | `readonly struct MazeSettings` — per-chapter maze parametre seti. |
| `Assets/Scripts/TowerMaze/Runtime/UISystems/TierCelebrationScreen.cs` | Tier 1–10 tamamlama kutlama ekranı. Tap-to-dismiss. |
| `Assets/Scripts/TowerMaze/Editor/PreValidateChaptersTool.cs` | Editor menu item: tüm 500 bölümü pre-validate eder, ChapterSeedTable asset'ine yazar. |
| `Assets/Resources/TowerMaze/ChapterSeedTable.asset` | 500 attempt int dizisini tutan ScriptableObject. Editor'da pre-bake yapılır, runtime'da fallback olarak okunur. |
| `Assets/Scripts/TowerMaze/Runtime/ChapterSeedTable.cs` | ChapterSeedTable ScriptableObject tanımı. |

### Değiştirilecek dosyalar

| Dosya | Değişiklik özeti |
|-------|------------------|
| `Assets/Scripts/TowerMaze/Runtime/ChapterManager.cs` | TotalChapters 500, formüller revize, struct genişler, validation entegrasyonu. |
| `Assets/Scripts/TowerMaze/Runtime/TowerSystems.cs` | `SetChapterMazeSettings(MazeSettings)` ve `SetChapterSinkSpeed(float)` eklenir; `SetChapterDifficulty` kaldırılır. SpawnSegment chapter branch'ini günceller. MazeGenerator.GenerateWithSettings overload'ı eklenir. |
| `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs` | PrepareFreshRun yeni API'ya geçer; CompleteChapterRun tier routing yapar. |
| `Assets/Scripts/TowerMaze/Runtime/UISystems/UIManager.cs` | `ShowTierCelebration(tier, bonus, onContinue)` eklenir. ShowChapterSelect 500-bölüm refactor'a uyumlu olur. |
| `Assets/Scripts/TowerMaze/Runtime/UISystems/ChapterSelectScreen.cs` | 5×10 grid → 10 tier section'lı vertical scroll. |
| `Assets/Scripts/TowerMaze/Runtime/TowerMazeBootstrapper.cs` | First-launch validation overlay, asset preload. |

---

## Chunk 1: Veri Katmanı (ChapterDefinition + Formüller)

### Task 1.1: MazeSettings struct

**Files:**
- Create: `Assets/Scripts/TowerMaze/Runtime/MazeSettings.cs`

- [ ] **Step 1: Dosyayı oluştur**

```csharp
using UnityEngine;

namespace TowerMaze
{
    public readonly struct MazeSettings
    {
        public readonly float pathTwistiness;
        public readonly float branchDensity;
        public readonly float deadEndDensity;
        public readonly float decisionDensity;
        public readonly int minDecisionPoints;
        public readonly int minDeadEnds;

        public MazeSettings(
            float pathTwistiness,
            float branchDensity,
            float deadEndDensity,
            float decisionDensity,
            int minDecisionPoints,
            int minDeadEnds)
        {
            this.pathTwistiness = Mathf.Clamp01(pathTwistiness);
            this.branchDensity = Mathf.Clamp01(branchDensity);
            this.deadEndDensity = Mathf.Clamp01(deadEndDensity);
            this.decisionDensity = Mathf.Clamp01(decisionDensity);
            this.minDecisionPoints = Mathf.Max(0, minDecisionPoints);
            this.minDeadEnds = Mathf.Max(0, minDeadEnds);
        }
    }
}
```

- [ ] **Step 2: Unity'de derlemenin geçtiğini doğrula**

Unity Editor'a geç, otomatik derleme tamamlanır. Console'da `MazeSettings` ile ilgili hata olmamalı.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/MazeSettings.cs Assets/Scripts/TowerMaze/Runtime/MazeSettings.cs.meta
git commit -m "feat(chapter): add MazeSettings struct for per-chapter maze params"
```

---

### Task 1.2: ChapterManager — 500 bölüm + yeni formüller

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/ChapterManager.cs` (full rewrite of struct + formulas)

- [ ] **Step 1: ChapterDefinition struct'ını revize et**

`ChapterDefinition` struct'ını değiştir — eski `DifficultyOffset`/`ZoneOffset`'i kaldır, yeni alanları ekle:

```csharp
public readonly struct ChapterDefinition
{
    public readonly int Index;
    public readonly int TierIndex;          // 1..10
    public readonly float Complexity;       // 0..1, c(n)
    public readonly float TargetHeight;     // m
    public readonly float SinkSpeed;        // m/s
    public readonly MazeSettings MazeSettings;
    public readonly string DisplayName;
    public readonly int Seed;

    public ChapterDefinition(
        int index,
        int tierIndex,
        float complexity,
        float targetHeight,
        float sinkSpeed,
        MazeSettings mazeSettings,
        int seed)
    {
        Index = index;
        TierIndex = tierIndex;
        Complexity = complexity;
        TargetHeight = targetHeight;
        SinkSpeed = sinkSpeed;
        MazeSettings = mazeSettings;
        Seed = seed;
        DisplayName = $"LEVEL {index}";
    }
}
```

- [ ] **Step 2: Sabitleri ve sayaçları güncelle**

```csharp
public const int TotalChapters = 500;
public const int ChaptersPerTier = 50;
public const int TotalTiers = TotalChapters / ChaptersPerTier;
private const float HMin = 50f;
private const float HMax = 500f;
private const float LavaHeadStart = 8f;
private const float SafetyMarginCh1 = 1.30f;
private const float SafetyMarginCh500 = 1.05f;
private const float MazeEffMax = 0.95f;
private const float MazeEffMin = 0.50f;
// B_PLAYER will be configured via GameConfig — added in later step
private const string KeyUnlocked = "TowerMaze.UnlockedChapters";
private const string KeyBestPrefix = "TowerMaze.ChapterBest.";
```

- [ ] **Step 3: Eski formül helper'larını sil ve yenilerini ekle**

`ComputeDifficultyOffset` ve `ComputeZoneOffset` metodlarını **tamamen sil**.

Yeni formül helper'ları ekle:

```csharp
private static float Smoothstep(float t)
{
    t = Mathf.Clamp01(t);
    return t * t * t * (t * (t * 6f - 15f) + 10f);
}

private static int ComputeTierIndex(int n) => ((n - 1) / ChaptersPerTier) + 1;

private static float ComputeNormalizedT(int n) => (n - 1) / 499f;

private static float ComputeComplexity(int n) => Smoothstep(ComputeNormalizedT(n));

private static float ComputeTargetHeight(int n)
{
    float s = Smoothstep(ComputeNormalizedT(n));
    return Mathf.Lerp(HMin, HMax, s);
}

private static float ComputeMazeEfficiency(float c) => Mathf.Lerp(MazeEffMax, MazeEffMin, c);

private static float ComputeSafetyMargin(float c) => Mathf.Lerp(SafetyMarginCh1, SafetyMarginCh500, c);

private static float ComputeSinkSpeed(int n, float ballPlayerSpeed)
{
    float c = ComputeComplexity(n);
    float h = ComputeTargetHeight(n);
    float playerEff = ballPlayerSpeed * ComputeMazeEfficiency(c);
    float expectedTime = h / Mathf.Max(0.01f, playerEff);
    float safety = ComputeSafetyMargin(c);
    return (h + LavaHeadStart) / Mathf.Max(0.01f, expectedTime * safety);
}

private static MazeSettings ComputeMazeSettings(int n)
{
    float c = ComputeComplexity(n);
    return new MazeSettings(
        pathTwistiness:    Mathf.Lerp(0.18f, 0.65f, c),
        branchDensity:     Mathf.Lerp(0.30f, 0.78f, c),
        deadEndDensity:    Mathf.Lerp(0.18f, 0.72f, c),
        decisionDensity:   Mathf.Lerp(0.24f, 0.66f, c),
        minDecisionPoints: Mathf.RoundToInt(Mathf.Lerp(2f, 6f, c)),
        minDeadEnds:       Mathf.RoundToInt(Mathf.Lerp(1f, 7f, c)));
}

private static int ComputeSeed(int baseSeed, int n, int attempt)
{
    return (baseSeed * 31) ^ (n * 7919) ^ (attempt * 12911);
}
```

- [ ] **Step 4: Initialize'ı yeni formüllerle revize et**

```csharp
public void Initialize(int baseSeed, float ballPlayerSpeed)
{
    UnlockedUpTo = PlayerPrefs.GetInt(KeyUnlocked, 1);
    _chapters = new ChapterDefinition[TotalChapters];
    for (int i = 1; i <= TotalChapters; i++)
    {
        int tier = ComputeTierIndex(i);
        float complexity = ComputeComplexity(i);
        float targetHeight = ComputeTargetHeight(i);
        float sinkSpeed = ComputeSinkSpeed(i, ballPlayerSpeed);
        MazeSettings mazeSettings = ComputeMazeSettings(i);
        int attempt = PlayerPrefs.GetInt(KeySeedAttemptPrefix + i, 0);
        int seed = ComputeSeed(baseSeed, i, attempt);
        _chapters[i - 1] = new ChapterDefinition(i, tier, complexity, targetHeight, sinkSpeed, mazeSettings, seed);
    }
}

private const string KeySeedAttemptPrefix = "TowerMaze.ChapterSeedAttempt.";
```

(Solver bu dosyayı sonraki chunk'ta dolduracak; şimdilik attempt 0 default.)

- [ ] **Step 5: ChapterCompleteScreen format kullanımını güncelle**

Aynı dosyada veya UseSites'te değişen tek API noktası `Initialize(int baseSeed)` → `Initialize(int baseSeed, float ballPlayerSpeed)` oldu. **Bu Bootstrapper'ı bozar** — Bootstrapper Chunk 6'da güncellenecek. Şimdilik Initialize'ın eski overload'ını da geçici olarak ekle:

```csharp
[System.Obsolete("Use Initialize(baseSeed, ballPlayerSpeed). Kept for compile compatibility.")]
public void Initialize(int baseSeed) => Initialize(baseSeed, 4f);
```

- [ ] **Step 6: Unity'de derlemenin geçtiğini doğrula**

Unity'ye dön, derleme bitsin. Console'da hata olmamalı (warning yalnız obsolete overload için olabilir).

- [ ] **Step 7: Quick smoke check — Debug.Log ile doğrula**

`ChapterManager.Initialize` sonuna geçici olarak şunu ekle:

```csharp
Debug.Log($"[ChapterManager] ch1: target={_chapters[0].TargetHeight:F1}m, sink={_chapters[0].SinkSpeed:F2}m/s, c={_chapters[0].Complexity:F3}");
Debug.Log($"[ChapterManager] ch250: target={_chapters[249].TargetHeight:F1}m, sink={_chapters[249].SinkSpeed:F2}m/s, c={_chapters[249].Complexity:F3}");
Debug.Log($"[ChapterManager] ch500: target={_chapters[499].TargetHeight:F1}m, sink={_chapters[499].SinkSpeed:F2}m/s, c={_chapters[499].Complexity:F3}");
```

Editor'da Play, console'a bak. Beklenen değerler (B_PLAYER=4 default):
- ch1: target≈50m, sink≈2.92m/s, c=0.000
- ch250: target≈275m, sink≈2.45m/s, c=0.500
- ch500: target≈500m, sink≈1.99m/s, c=1.000

Onaydan sonra Debug.Log'ları çıkar.

- [ ] **Step 8: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/ChapterManager.cs
git commit -m "feat(chapter): expand to 500 chapters with smoothstep difficulty curve"
```

---

## Chunk 2: Tower Integration (MazeGenerator + TowerGenerator)

### Task 2.1: MazeGenerator — GenerateWithSettings overload

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/TowerSystems.cs` (MazeGenerator class)

**Background:** Mevcut `MazeGenerator.Generate(config, difficultyProfile, theme, segmentIndex, zoneIndex, lastExitColumn, seed)` DifficultyProfile'dan `Evaluate(zoneHeight)` çağırarak parametreleri okuyor. Chapter modunda DifficultyProfile yerine direkt MazeSettings vermek istiyoruz.

- [ ] **Step 1: Mevcut Generate'in implementasyonunu oku**

`TowerSystems.cs` içinde `class MazeGenerator` ve `Generate` metodunu bul, hangi DifficultyProfile field'larını okuduğunu çıkar (pathTwistiness, branchDensity vs.).

- [ ] **Step 2: Internal helper'ı çıkar**

`Generate` içindeki **DifficultyProfile.Evaluate(...)**'dan sonra DifficultySettings'i kullanan tüm kodu yeni bir private helper'a taşı:

```csharp
private SegmentData GenerateInternal(
    GameConfig config,
    DifficultySettings settings,
    int minDecisionPoints,
    int minDeadEnds,
    ThemeDefinition theme,
    int segmentIndex,
    int zoneIndex,
    int lastExitColumn,
    int seed)
{
    // mevcut implementation gövdesi, settings'ten field okuyarak
}
```

`DifficultySettings` zaten struct olarak var (DifficultyProfile içinde). Eğer `minimumDecisionPoints`/`minimumDeadEnds` settings'in içindeyse onu da bu yolla geç.

- [ ] **Step 3: Eski public Generate'i yeni helper'a yönlendir**

```csharp
public SegmentData Generate(
    GameConfig config,
    DifficultyProfile profile,
    ThemeDefinition theme,
    int segmentIndex,
    int zoneIndex,
    int lastExitColumn,
    int seed)
{
    DifficultySettings settings = profile.Evaluate(zoneIndex * config.ZoneHeight);
    return GenerateInternal(config, settings, settings.minimumDecisionPoints, settings.minimumDeadEnds, theme, segmentIndex, zoneIndex, lastExitColumn, seed);
}
```

- [ ] **Step 4: Yeni public GenerateWithSettings overload'ı ekle**

```csharp
public SegmentData GenerateWithSettings(
    GameConfig config,
    MazeSettings mazeSettings,
    ThemeDefinition theme,
    int segmentIndex,
    int zoneIndex,
    int lastExitColumn,
    int seed)
{
    DifficultySettings settings = new DifficultySettings
    {
        pathTwistiness = mazeSettings.pathTwistiness,
        branchDensity = mazeSettings.branchDensity,
        deadEndDensity = mazeSettings.deadEndDensity,
        decisionDensity = mazeSettings.decisionDensity,
        // rotationSpeed, sinkSpeed are NOT used by maze layout — left at zero/default
        rotationSpeed = 0f,
        sinkSpeed = 0f,
        minimumDecisionPoints = mazeSettings.minDecisionPoints,
        minimumDeadEnds = mazeSettings.minDeadEnds,
    };
    return GenerateInternal(config, settings, mazeSettings.minDecisionPoints, mazeSettings.minDeadEnds, theme, segmentIndex, zoneIndex, lastExitColumn, seed);
}
```

- [ ] **Step 5: Derleme ve smoke test**

Unity'ye geç, derleme geçmeli. Endless mode oynamayı dene, hiçbir regression olmamalı (eski Generate path aynı sonuç döndürüyor).

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/TowerSystems.cs
git commit -m "feat(tower): add MazeGenerator.GenerateWithSettings overload"
```

---

### Task 2.2: TowerGenerator — yeni chapter API

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/TowerSystems.cs` (TowerGenerator class)

- [ ] **Step 1: Eski state'i yeni state'le değiştir**

TowerGenerator içinde:

```csharp
// Eski (silinecek):
private float difficultyOffset;
private int zoneOffset;

// Yeni:
private bool useChapterMazeSettings;
private MazeSettings chapterMazeSettings;
private bool useChapterSinkSpeed;
private float chapterSinkSpeedValue;
```

- [ ] **Step 2: Eski API'yı sil ve yenilerini ekle**

```csharp
// Sil:
public void SetChapterDifficulty(float heightOffset, int zoneOff)

// Yeni:
public void SetChapterMazeSettings(MazeSettings settings)
{
    useChapterMazeSettings = true;
    chapterMazeSettings = settings;
}

public void ClearChapterMazeSettings()
{
    useChapterMazeSettings = false;
}

public void SetChapterSinkSpeed(float sinkSpeed)
{
    useChapterSinkSpeed = true;
    chapterSinkSpeedValue = Mathf.Max(0.01f, sinkSpeed);
}

public void ClearChapterSinkSpeed()
{
    useChapterSinkSpeed = false;
}
```

- [ ] **Step 3: ResetRun'da yeni state'i sıfırla**

`ResetRun` içinde mevcut `difficultyOffset = 0; zoneOffset = 0;` satırlarını sil. Yenileri **sıfırlama** — chapter run akışında `SetChapter*` çağrılarından sonra `ResetRun` gelmemeli (ya da sıfırlanmamalı, sıraya bakılır). Kontrol için: RunSystems chapter modu önce SetChapter*, sonra ResetRun çağırıyorsa yeni state'i ResetRun'da sıfırlama.

- [ ] **Step 4: SpawnSegment'i güncelle**

`SpawnSegment` metodunda:

```csharp
SegmentData data;
if (segmentIndex == 0)
{
    data = mazeGenerator.CreateTutorialSegment(config, theme, segmentIndex, lastExitColumn);
}
else if (useChapterMazeSettings)
{
    data = mazeGenerator.GenerateWithSettings(
        config,
        chapterMazeSettings,
        theme,
        segmentIndex,
        zoneIndex,
        lastExitColumn,
        segmentSeed);
}
else
{
    data = mazeGenerator.Generate(
        config,
        difficultyProfile,
        theme,
        segmentIndex,
        zoneIndex,
        lastExitColumn,
        segmentSeed);
}
```

- [ ] **Step 5: GetZoneIndexForSegment'i temizle**

Eski `+ zoneOffset` ifadesini sil:

```csharp
private int GetZoneIndexForSegment(int segmentIndex)
{
    int segmentsPerZone = Mathf.Max(1, config.segmentsPerZone);
    return Mathf.Max(0, segmentIndex / segmentsPerZone);
}
```

- [ ] **Step 6: TowerSinkController'a chapter sinkSpeed'i ilet**

`TowerSinkController` (aynı dosyada veya ayrı) sinkSpeed'i DifficultyProfile.Evaluate'tan okuyor. `useChapterSinkSpeed` true ise `chapterSinkSpeedValue` kullanılmalı.

`TowerGenerator`'da SinkController'ın güncellendiği yere şunu ekle (genelde `Update` veya difficulty refresh metodunda):

```csharp
float effectiveSinkSpeed = useChapterSinkSpeed
    ? chapterSinkSpeedValue
    : difficultyProfile.Evaluate(playerHeight).sinkSpeed;
sinkController.SetSpeed(effectiveSinkSpeed);
```

(`SetSpeed` zaten varsa kullan, yoksa `TowerSinkController`'a ekle: `public void SetSpeed(float s) { speed = s; }`)

**Önemli:** Rotation tarafına dokunma. RotationController halen DifficultyProfile.Evaluate(playerHeight).rotationSpeed kullanmaya devam etsin (= endless ile aynı, kullanıcının kararı).

- [ ] **Step 7: Derleme**

Unity'ye geç, console'da `SetChapterDifficulty` referansı kalan dosya hatası gelirse bir sonraki task'ta (RunSystems update) düzelir.

- [ ] **Step 8: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/TowerSystems.cs
git commit -m "feat(tower): replace SetChapterDifficulty with MazeSettings/SinkSpeed API"
```

---

### Task 2.3: RunSystems — yeni chapter API'ya geçiş

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs` (PrepareFreshRun chapter branch)

- [ ] **Step 1: Eski çağrıları yenisiyle değiştir**

`RunSystems.cs` içinde `SetChapterDifficulty` çağrılarını bul (line 3515 ve 3519 civarı) ve değiştir:

```csharp
// Chapter mode:
if (activeRunMode == RunMode.Chapter && chapterManager != null)
{
    var ch = chapterManager.GetChapter(chapterManager.ActiveChapterIndex);
    towerGenerator.SetChapterMazeSettings(ch.MazeSettings);
    towerGenerator.SetChapterSinkSpeed(ch.SinkSpeed);
}
else
{
    towerGenerator.ClearChapterMazeSettings();
    towerGenerator.ClearChapterSinkSpeed();
}
```

- [ ] **Step 2: Derleme & endless smoke test**

Unity'de Play, endless modda oyna. Hiçbir regression olmamalı (Clear* çağrıları default DifficultyProfile akışını koruyor).

- [ ] **Step 3: Chapter 1 smoke test (henüz solver yok, attempt=0 seed)**

Ana menüden chapter 1'i başlat. Oyna. Lava hızının makul olduğunu, maze'in basit olduğunu gözlemle. Hedef 50m'ye varmaya çalış. (Henüz tier celebration yok, varsa 3 sn auto-return ile menüye dönmeli.)

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/RunSystems.cs
git commit -m "feat(chapter): wire RunSystems to per-chapter MazeSettings/SinkSpeed"
```

---

## Chunk 3: Solver (ChapterValidator)

### Task 3.1: Maze adapter — SegmentData → cell graph

**Files:**
- Create: `Assets/Scripts/TowerMaze/Runtime/ChapterValidator.cs` (partial — adapter section)

- [ ] **Step 1: SegmentData'nın yapısını oku**

`TowerSystems.cs` içinde `SegmentData` class'ını bul. Cell grid yapısını, wall bilgisi nereden okunuyor (cell.walls? cell.NorthWall vb.) çıkar. Bu bilgi ChapterValidator için kritik.

- [ ] **Step 2: ChapterValidator dosyasını oluştur (skeleton)**

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerMaze
{
    public sealed class ChapterValidator
    {
        private readonly GameConfig config;
        private readonly ThemeDefinition theme;
        private readonly MazeGenerator mazeGenerator;
        private readonly float ballPlayerSpeed;
        private const int MaxAttempts = 16;
        private const float CellHeight = 1f; // ileride config'ten

        public ChapterValidator(GameConfig config, ThemeDefinition theme, float ballPlayerSpeed)
        {
            this.config = config;
            this.theme = theme;
            this.ballPlayerSpeed = ballPlayerSpeed;
            this.mazeGenerator = new MazeGenerator();
        }

        // adapter, A*, validate metodları sıradaki step'lerde
    }
}
```

- [ ] **Step 3: BuildPreviewCells metodunu ekle**

```csharp
private struct Cell
{
    public bool wallN, wallS, wallE, wallW; // true = path geçilmez
}

private Cell[,] BuildPreviewMaze(int seed, MazeSettings settings, float targetHeight)
{
    int segmentsPerZone = Mathf.Max(1, config.segmentsPerZone);
    int rowsPerSegment = config.rowsPerSegment; // GameConfig'te varsa
    int width = config.mazeWidthCells;
    int targetSegments = Mathf.CeilToInt(targetHeight / config.SegmentHeight) + 2;
    int totalRows = targetSegments * rowsPerSegment;

    Cell[,] grid = new Cell[totalRows, width];
    int lastExitColumn = width / 2;

    for (int seg = 0; seg < targetSegments; seg++)
    {
        int zoneIndex = seg / segmentsPerZone;
        SegmentData data = (seg == 0)
            ? mazeGenerator.CreateTutorialSegment(config, theme, seg, lastExitColumn)
            : mazeGenerator.GenerateWithSettings(config, settings, theme, seg, zoneIndex, lastExitColumn, seed ^ (seg * 31));

        // SegmentData'dan grid'e kopyala — exact API SegmentData yapısına göre netleşir
        for (int row = 0; row < rowsPerSegment; row++)
        for (int col = 0; col < width; col++)
        {
            int globalRow = seg * rowsPerSegment + row;
            // Örnek field okuma — gerçek alan adı SegmentData incelemesi sonrası netleşir
            grid[globalRow, col].wallN = data.HasNorthWall(row, col);
            grid[globalRow, col].wallS = data.HasSouthWall(row, col);
            grid[globalRow, col].wallE = data.HasEastWall(row, col);
            grid[globalRow, col].wallW = data.HasWestWall(row, col);
        }
        lastExitColumn = data.exitColumn;
    }
    return grid;
}
```

**Not:** `data.HasNorthWall(...)` yer tutucu API. Gerçek API'yı SegmentData'yı okuduktan sonra burayı düzelt. Eğer SegmentData walls'u 2D array olarak tutuyorsa direkt index'le.

- [ ] **Step 4: Derleme ve placeholder kontrol**

Eğer `HasNorthWall` API mevcut değilse, derleme hatası verir. SegmentData'yı dönüp gerçek API'ya uydur. Bu task SegmentData public surface'ine bağımlı.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/ChapterValidator.cs Assets/Scripts/TowerMaze/Runtime/ChapterValidator.cs.meta
git commit -m "feat(validator): add SegmentData → cell-grid adapter skeleton"
```

---

### Task 3.2: A* solver

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/ChapterValidator.cs`

- [ ] **Step 1: Min-heap priority queue ekle (ya da SortedSet kullan)**

```csharp
private sealed class MinHeap<T>
{
    private readonly List<(float priority, T item)> heap = new();
    public int Count => heap.Count;

    public void Push(float priority, T item) { /* standard binary heap up-sift */ }
    public T Pop() { /* standard binary heap down-sift, return min */ }
}
```

(Tam implementasyon engineer judgment, basit `List<T>` + insertion sort 25k node için yeterli olur.)

- [ ] **Step 2: AStarMinTime metodunu ekle**

```csharp
private float AStarMinTime(Cell[,] grid, float targetHeight)
{
    int rows = grid.GetLength(0);
    int cols = grid.GetLength(1);
    int targetRow = Mathf.CeilToInt(targetHeight / CellHeight);
    targetRow = Mathf.Min(targetRow, rows - 1);

    float bestCost = float.PositiveInfinity;
    var open = new MinHeap<(int row, int col)>();
    var costSoFar = new float[rows, cols];
    for (int r = 0; r < rows; r++)
    for (int c = 0; c < cols; c++)
        costSoFar[r, c] = float.PositiveInfinity;

    // Start: tüm bottom row hücreleri (oyuncu x-serbest)
    for (int c = 0; c < cols; c++)
    {
        costSoFar[0, c] = 0f;
        open.Push(targetHeight / ballPlayerSpeed, (0, c));
    }

    float edgeCost = CellHeight / ballPlayerSpeed;

    while (open.Count > 0)
    {
        var (row, col) = open.Pop();
        if (row >= targetRow)
        {
            bestCost = Mathf.Min(bestCost, costSoFar[row, col]);
            return bestCost;
        }

        float baseCost = costSoFar[row, col];

        // 4 yön: N (row+1), S (row-1), E (col+1 wraparound), W (col-1 wraparound)
        TryRelax(grid, ref open, costSoFar, row, col, row + 1, col, !grid[row, col].wallN, edgeCost, baseCost, targetRow);
        if (row > 0)
            TryRelax(grid, ref open, costSoFar, row, col, row - 1, col, !grid[row, col].wallS, edgeCost, baseCost, targetRow);
        TryRelax(grid, ref open, costSoFar, row, col, row, (col + 1) % cols, !grid[row, col].wallE, edgeCost, baseCost, targetRow);
        TryRelax(grid, ref open, costSoFar, row, col, row, (col - 1 + cols) % cols, !grid[row, col].wallW, edgeCost, baseCost, targetRow);
    }
    return bestCost;
}

private void TryRelax(Cell[,] grid, ref MinHeap<(int,int)> open, float[,] cost,
    int fromRow, int fromCol, int toRow, int toCol, bool canPass, float edge, float baseCost, int targetRow)
{
    if (!canPass) return;
    if (toRow < 0 || toRow >= grid.GetLength(0)) return;
    float newCost = baseCost + edge;
    if (newCost < cost[toRow, toCol])
    {
        cost[toRow, toCol] = newCost;
        float h = Mathf.Max(0, targetRow - toRow) * edge;
        open.Push(newCost + h, (toRow, toCol));
    }
}
```

**Önemli:** Heuristic `(targetRow - currentRow) * edgeCost` — admissible (kalan minimum süreyi underestimate ediyor).

- [ ] **Step 3: Validate metodunu ekle**

```csharp
public bool TryValidateChapter(int chapterIndex, int baseSeed,
    float targetHeight, MazeSettings settings, float sinkSpeed, float safetyMargin,
    out int validatedAttempt)
{
    for (int attempt = 0; attempt < MaxAttempts; attempt++)
    {
        int seed = (baseSeed * 31) ^ (chapterIndex * 7919) ^ (attempt * 12911);
        Cell[,] grid = BuildPreviewMaze(seed, settings, targetHeight);
        float optimalTime = AStarMinTime(grid, targetHeight);
        if (float.IsInfinity(optimalTime)) continue; // unreachable, re-roll

        float lavaBudget = (targetHeight + 8f) / sinkSpeed; // LAVA_HEAD_START = 8
        if (optimalTime * safetyMargin <= lavaBudget)
        {
            validatedAttempt = attempt;
            return true;
        }
    }
    validatedAttempt = MaxAttempts - 1;
    Debug.LogError($"[ChapterValidator] Chapter {chapterIndex} failed validation after {MaxAttempts} attempts");
    return false;
}
```

- [ ] **Step 4: Derleme & smoke test**

Unity'ye geç, derleme geçmeli (heap impl tam ise).

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/ChapterValidator.cs
git commit -m "feat(validator): add A* solver with safety-margin check"
```

---

### Task 3.3: ChapterValidator coroutine + ChapterManager entegrasyonu

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/ChapterValidator.cs` (ValidateAll)
- Modify: `Assets/Scripts/TowerMaze/Runtime/ChapterManager.cs` (Initialize coroutine)

- [ ] **Step 1: ValidateAll coroutine'i ekle**

```csharp
public IEnumerator ValidateAll(int baseSeed, System.Action<float> progressCallback)
{
    const string ValidatedFlag = "TowerMaze.ChaptersValidated.v1";
    if (PlayerPrefs.GetInt(ValidatedFlag, 0) == 1)
    {
        progressCallback?.Invoke(1f);
        yield break;
    }

    for (int n = 1; n <= ChapterManager.TotalChapters; n++)
    {
        // formülleri ChapterManager'dan al — aynı formüller burada da kullanılır
        float c = ChapterManagerFormulas.Complexity(n);
        float targetHeight = ChapterManagerFormulas.TargetHeight(n);
        float sinkSpeed = ChapterManagerFormulas.SinkSpeed(n, ballPlayerSpeed);
        float safetyMargin = ChapterManagerFormulas.SafetyMargin(c);
        MazeSettings settings = ChapterManagerFormulas.MazeSettings(n);

        TryValidateChapter(n, baseSeed, targetHeight, settings, sinkSpeed, safetyMargin, out int attempt);
        PlayerPrefs.SetInt("TowerMaze.ChapterSeedAttempt." + n, attempt);

        if (n % 10 == 0)
        {
            PlayerPrefs.Save();
            progressCallback?.Invoke(n / (float)ChapterManager.TotalChapters);
            yield return null;
        }
    }
    PlayerPrefs.SetInt(ValidatedFlag, 1);
    PlayerPrefs.Save();
    progressCallback?.Invoke(1f);
}
```

- [ ] **Step 2: ChapterManager formüllerini public static helper'a çıkar**

`ChapterManager` içindeki `ComputeComplexity`, `ComputeTargetHeight`, `ComputeMazeSettings`, `ComputeSinkSpeed`, `ComputeSafetyMargin` metodlarını private'tan internal static yap:

```csharp
internal static float ComputeComplexity(int n) => Smoothstep(ComputeNormalizedT(n));
internal static float ComputeTargetHeight(int n) { /* ... */ }
internal static float ComputeMazeEfficiency(float c) { /* ... */ }
internal static float ComputeSafetyMargin(float c) { /* ... */ }
internal static float ComputeSinkSpeed(int n, float ballPlayerSpeed) { /* ... */ }
internal static MazeSettings ComputeMazeSettings(int n) { /* ... */ }
```

ChapterValidator'da `ChapterManagerFormulas.X` yerine `ChapterManager.ComputeX` kullan.

- [ ] **Step 3: ChapterManager.Initialize coroutine versiyonu**

```csharp
public IEnumerator InitializeAsync(int baseSeed, float ballPlayerSpeed,
    GameConfig config, ThemeDefinition theme, System.Action<float> progressCallback)
{
    var validator = new ChapterValidator(config, theme, ballPlayerSpeed);
    yield return validator.ValidateAll(baseSeed, progressCallback);
    Initialize(baseSeed, ballPlayerSpeed); // formüller + cache attempt'leri okur
}
```

- [ ] **Step 4: Smoke test — Editor'da çalıştır**

Geçici olarak Bootstrapper'ı `InitializeAsync` çağıracak şekilde elden ayarla, Play, Console'a bak. Beklenen log: 500 bölüm doğrulandı, çoğu attempt 0, bazıları 1–3 olabilir (geç tier'larda). Hata yoksa devam.

Sonra geçici Bootstrapper değişikliğini geri al — bu Chunk 6'da düzgünce yapılacak.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/ChapterValidator.cs Assets/Scripts/TowerMaze/Runtime/ChapterManager.cs
git commit -m "feat(validator): coroutine ValidateAll with PlayerPrefs cache"
```

---

## Chunk 4: Run Flow Integration (Tier Routing)

### Task 4.1: RunSystems — tier celebration routing

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs` (CompleteChapterRun)

- [ ] **Step 1: CompleteChapterRun'da tier kontrolü**

```csharp
private void CompleteChapterRun()
{
    // ... mevcut prep kodu, height/idx/emberReward hesaplaması ...

    bool isTierMilestone = (idx % ChapterManager.ChaptersPerTier) == 0;
    bool nextUnlocked = chapterManager.IsUnlocked(idx + 1);
    bool isLastChapter = idx >= ChapterManager.TotalChapters;

    if (isTierMilestone)
    {
        int tierIndex = idx / ChapterManager.ChaptersPerTier;
        int tierBonus = tierIndex * 500;
        economyManager.GrantEmber(tierBonus); // chapter ödülüne ek
        uiManager.ShowTierCelebration(
            tierIndex,
            tierBonus,
            isLastChapter,
            ReturnToMainMenu);
    }
    else
    {
        uiManager.ShowChapterComplete(
            idx,
            height,
            chapterManager.GetChapter(idx).TargetHeight,
            emberReward,
            nextUnlocked,
            isLastChapter,
            ReturnToMainMenu,
            () => { if (!isLastChapter) StartChapterRun(idx + 1); else ReturnToMainMenu(); },
            () => uiManager.ShowChapterSelect(chapterManager, StartChapterRun));
    }
}
```

- [ ] **Step 2: Derleme bekle, ShowTierCelebration UIManager'da yok henüz**

Compile error: `UIManager.ShowTierCelebration` undefined. Bu beklenen — Chunk 5 Task 5.1'de eklenecek. Şimdilik geçici stub:

```csharp
// UIManager.cs içinde geçici:
public void ShowTierCelebration(int tier, int bonus, bool isLast, System.Action onContinue)
{
    Debug.Log($"[UIManager] TIER {tier} cleared, bonus={bonus}, last={isLast}");
    onContinue?.Invoke();
}
```

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/RunSystems.cs Assets/Scripts/TowerMaze/Runtime/UISystems/UIManager.cs
git commit -m "feat(chapter): route tier-milestone completions to TierCelebration"
```

---

## Chunk 5: UI (TierCelebration + ChapterSelect refactor)

### Task 5.1: TierCelebrationScreenController

**Files:**
- Create: `Assets/Scripts/TowerMaze/Runtime/UISystems/TierCelebrationScreen.cs`
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems/UIManager.cs`

- [ ] **Step 1: TierCelebrationScreenController dosyasını oluştur**

ChapterCompleteScreen.cs pattern'ini taban al (aynı klasörde). Aynı tap-to-dismiss yaklaşımı, IPointerClickHandler, 350ms cooldown:

```csharp
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TowerMaze
{
    public class TierCelebrationScreenController : MonoBehaviour, IPointerClickHandler
    {
        private const float TapCooldownSeconds = 0.35f;
        private Text titleText;
        private Text subtitleText;
        private Text rewardText;
        private Text tapHintText;
        private Image badgeImage;
        private Action pendingOnContinue;
        private float armedAtRealtime;

        public void Initialize(Font font, ThemeDefinition theme)
        {
            // ChapterCompleteScreen ile aynı katmanlı yapı:
            // bg + dimmer + jelly panel + glow + texts + badge
            // (detayları ChapterCompleteScreen'den kopya/uyarla)
        }

        public void SetState(int tierIndex, int bonusEmber, bool isLastChapter, Action onContinue)
        {
            pendingOnContinue = onContinue;
            string template = UILanguage.Translate(
                "TIER {0} USTASI!",
                "TIER {0} MASTER!",
                "¡MAESTRO DE NIVEL {0}!");
            titleText.text = string.Format(template, tierIndex);

            subtitleText.text = isLastChapter
                ? UILanguage.Translate("Tum bolumler tamamlandi!", "All chapters completed!", "¡Todos los niveles completados!")
                : UILanguage.Translate("Bir sonraki tier acildi.", "Next tier unlocked.", "Siguiente nivel desbloqueado.");

            rewardText.text = $"+{bonusEmber} EMBER";

            // tier badge sprite resolver
            Sprite badge = Resources.Load<Sprite>($"TowerMaze/UITheme/tier_badges/tier_{tierIndex}");
            if (badge != null) badgeImage.sprite = badge;
            // fallback: prosedürel renk (HSV(tier/10, 0.8, 0.95))

            tapHintText.text = UILanguage.Translate(
                "Devam etmek icin ekrana dokun",
                "Tap anywhere to continue",
                "Toca la pantalla para continuar");

            armedAtRealtime = Time.realtimeSinceStartup + TapCooldownSeconds;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Time.realtimeSinceStartup < armedAtRealtime) return;
            Action cb = pendingOnContinue;
            pendingOnContinue = null;
            cb?.Invoke();
        }
    }
}
```

- [ ] **Step 2: UIManager.Initialize'da TierCelebrationScreen instance'ı oluştur**

ChapterCompleteScreen pattern'ine uy:

```csharp
private TierCelebrationScreenController tierCelebrationController;

// Initialize sonunda:
tierCelebrationController = CreatePanel<TierCelebrationScreenController>("TierCelebrationScreen", canvas.transform);
tierCelebrationController.Initialize(runtimeFont, theme);
tierCelebrationController.gameObject.SetActive(false);
```

- [ ] **Step 3: ShowTierCelebration metodunu yaz**

```csharp
public void ShowTierCelebration(int tierIndex, int bonusEmber, bool isLastChapter, System.Action onContinue)
{
    startScreenController.gameObject.SetActive(false);
    failScreenController.gameObject.SetActive(false);
    hudController.gameObject.SetActive(false);
    countdownController.gameObject.SetActive(false);
    chapterSelectController?.gameObject.SetActive(false);
    chapterCompleteController?.gameObject.SetActive(false);
    tierCelebrationController.gameObject.SetActive(true);
    tierCelebrationController.SetState(tierIndex, bonusEmber, isLastChapter, onContinue);
    SetHeat(0f);
    if (staticMenuBackground != null) staticMenuBackground.gameObject.SetActive(true);
    if (bannerAdManager != null && (coinStoreManager == null || !coinStoreManager.HasNoAds))
        bannerAdManager.ShowBanner();
}
```

ShowStart, ShowChapterSelect vb. metodlarda `tierCelebrationController?.gameObject.SetActive(false)` ekle.

- [ ] **Step 4: Smoke test — chapter 50 simülasyonu**

PlayerPrefs üzerinden `TowerMaze.UnlockedChapters = 50` set et, chapter 50'yi başlat, hedefe ulaş. TierCelebrationScreen açılmalı, tap edince ana menüye dönmeli, EMBER bakiyesi +500 artmalı.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/UISystems/TierCelebrationScreen.cs Assets/Scripts/TowerMaze/Runtime/UISystems/TierCelebrationScreen.cs.meta Assets/Scripts/TowerMaze/Runtime/UISystems/UIManager.cs
git commit -m "feat(ui): add TierCelebrationScreen with tap-to-dismiss"
```

---

### Task 5.2: ChapterSelectScreen — 500 bölüm vertical scroll

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/UISystems/ChapterSelectScreen.cs`

- [ ] **Step 1: Mevcut grid yapısını oku**

ChapterSelectScreen.cs'i baştan oku, grid layout ve cell creation kodu çıkar.

- [ ] **Step 2: Tier section render mantığı**

Mevcut `Refresh(ChapterManager)` metodunda 1×50 cell loop yerine 10×(header+50cell) iç içe loop:

```csharp
public void Refresh(ChapterManager chapterManager)
{
    foreach (Transform child in scrollContent) Destroy(child.gameObject);

    for (int tier = 1; tier <= ChapterManager.TotalTiers; tier++)
    {
        // Tier header
        var header = CreateTierHeader(tier, chapterManager);
        header.transform.SetParent(scrollContent, false);

        // Tier grid (5 col × 10 row)
        var tierGrid = CreateTierGrid(scrollContent);
        for (int k = 1; k <= ChapterManager.ChaptersPerTier; k++)
        {
            int chapterIndex = (tier - 1) * ChapterManager.ChaptersPerTier + k;
            var cell = CreateChapterCell(tierGrid, chapterIndex, chapterManager, onChapterSelected);
        }
    }

    // En yüksek açık bölüme auto-scroll
    ScrollToChapter(chapterManager.UnlockedUpTo);
}
```

- [ ] **Step 3: Tier name lokalizasyonu**

```csharp
private static string GetTierName(int tier)
{
    string[] tr = { "ACEMI", "OGRENCI", "YETENEKLI", "USTA", "SAMPIYON",
                    "DEHA", "LORD", "EFSANE", "TANRI", "OLUMSUZ" };
    string[] en = { "ROOKIE", "STUDENT", "ADEPT", "MASTER", "CHAMPION",
                    "GENIUS", "LORD", "LEGEND", "GOD", "IMMORTAL" };
    string[] es = { "NOVATO", "ESTUDIANTE", "EXPERTO", "MAESTRO", "CAMPEON",
                    "GENIO", "SENOR", "LEYENDA", "DIOS", "INMORTAL" };
    int i = Mathf.Clamp(tier - 1, 0, 9);
    return UILanguage.Translate(tr[i], en[i], es[i]);
}
```

- [ ] **Step 4: ScrollRect content size ayarla**

10 tier × ~600px = ~6000px. ContentSizeFitter veya manuel sizeDelta ile.

- [ ] **Step 5: Auto-scroll**

```csharp
private void ScrollToChapter(int chapterIndex)
{
    int tier = (chapterIndex - 1) / ChapterManager.ChaptersPerTier;
    float normalizedY = 1f - (tier / (float)ChapterManager.TotalTiers);
    scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedY);
}
```

- [ ] **Step 6: Smoke test**

ChapterSelectScreen'i ana menüden aç. 10 tier section, her birinde 50 cell, smooth scroll, en yüksek açık bölüme otomatik konumlanma. Tap'lar doğru chapter index'i başlatmalı.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/UISystems/ChapterSelectScreen.cs
git commit -m "feat(ui): refactor ChapterSelectScreen for 500 chapters in 10 tiers"
```

---

## Chunk 6: Bootstrapper + Editor Pre-bake

### Task 6.1: ChapterSeedTable ScriptableObject

**Files:**
- Create: `Assets/Scripts/TowerMaze/Runtime/ChapterSeedTable.cs`

- [ ] **Step 1: ScriptableObject tanımı**

```csharp
using UnityEngine;

namespace TowerMaze
{
    [CreateAssetMenu(menuName = "TowerMaze/Chapter Seed Table", fileName = "ChapterSeedTable")]
    public sealed class ChapterSeedTable : ScriptableObject
    {
        [SerializeField] private int[] attempts = new int[ChapterManager.TotalChapters];

        public int GetAttempt(int chapterIndex)
        {
            int i = chapterIndex - 1;
            if (i < 0 || i >= attempts.Length) return 0;
            return attempts[i];
        }

        public void SetAttempt(int chapterIndex, int attempt)
        {
            int i = chapterIndex - 1;
            if (i < 0 || i >= attempts.Length) return;
            attempts[i] = attempt;
        }

        public void EnsureSize()
        {
            if (attempts == null || attempts.Length != ChapterManager.TotalChapters)
            {
                System.Array.Resize(ref attempts, ChapterManager.TotalChapters);
            }
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/ChapterSeedTable.cs Assets/Scripts/TowerMaze/Runtime/ChapterSeedTable.cs.meta
git commit -m "feat(validator): add ChapterSeedTable ScriptableObject"
```

---

### Task 6.2: PreValidateChaptersTool — editor menu

**Files:**
- Create: `Assets/Scripts/TowerMaze/Editor/PreValidateChaptersTool.cs`

- [ ] **Step 1: Editor tool dosyası**

```csharp
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TowerMaze.EditorTools
{
    public static class PreValidateChaptersTool
    {
        [MenuItem("Tools/TowerMaze/Pre-Validate Chapters")]
        public static void Run()
        {
            var config = Resources.Load<GameConfig>("TowerMaze/GameConfig");
            var theme = Resources.Load<ThemeDefinition>("TowerMaze/ThemeDefinition")
                ?? ScriptableObject.CreateInstance<ThemeDefinition>();
            float ballPlayerSpeed = config.ballVerticalSpeed; // GameConfig'te varsa, yoksa 4f sabit
            int baseSeed = config.seed;

            var validator = new ChapterValidator(config, theme, ballPlayerSpeed);
            string assetPath = "Assets/Resources/TowerMaze/ChapterSeedTable.asset";
            var table = AssetDatabase.LoadAssetAtPath<ChapterSeedTable>(assetPath)
                ?? CreateAssetAt<ChapterSeedTable>(assetPath);
            table.EnsureSize();

            for (int n = 1; n <= ChapterManager.TotalChapters; n++)
            {
                EditorUtility.DisplayProgressBar("Pre-Validating", $"Chapter {n}/500", n / 500f);
                float c = ChapterManager.ComputeComplexity(n);
                float h = ChapterManager.ComputeTargetHeight(n);
                float s = ChapterManager.ComputeSinkSpeed(n, ballPlayerSpeed);
                float sm = ChapterManager.ComputeSafetyMargin(c);
                MazeSettings ms = ChapterManager.ComputeMazeSettings(n);
                validator.TryValidateChapter(n, baseSeed, h, ms, s, sm, out int attempt);
                table.SetAttempt(n, attempt);
            }

            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
            Debug.Log("[PreValidateChaptersTool] Done. Asset: " + assetPath);
        }

        private static T CreateAssetAt<T>(string path) where T : ScriptableObject
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
#endif
```

- [ ] **Step 2: Editor menü ile çalıştır**

Unity'de `Tools → TowerMaze → Pre-Validate Chapters`. Progress bar, sonunda console'da "Done. Asset: ..." log'u, `Assets/Resources/TowerMaze/ChapterSeedTable.asset` oluşmalı.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/TowerMaze/Editor/PreValidateChaptersTool.cs Assets/Scripts/TowerMaze/Editor/PreValidateChaptersTool.cs.meta Assets/Resources/TowerMaze/ChapterSeedTable.asset Assets/Resources/TowerMaze/ChapterSeedTable.asset.meta
git commit -m "feat(validator): add editor pre-bake tool for ChapterSeedTable"
```

---

### Task 6.3: Bootstrapper — pre-bake öncelikli, runtime fallback

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/TowerMazeBootstrapper.cs`
- Modify: `Assets/Scripts/TowerMaze/Runtime/ChapterManager.cs` (LoadFromTable + Initialize hibrit)

- [ ] **Step 1: ChapterManager'a tablo yükleme**

```csharp
public void Initialize(int baseSeed, float ballPlayerSpeed, ChapterSeedTable preValidatedTable = null)
{
    UnlockedUpTo = PlayerPrefs.GetInt(KeyUnlocked, 1);
    _chapters = new ChapterDefinition[TotalChapters];
    for (int i = 1; i <= TotalChapters; i++)
    {
        int attempt = preValidatedTable != null
            ? preValidatedTable.GetAttempt(i)
            : PlayerPrefs.GetInt(KeySeedAttemptPrefix + i, 0);
        // ... aynı struct construction ...
    }
}
```

- [ ] **Step 2: Bootstrapper akışı**

```csharp
private IEnumerator BootstrapChapters()
{
    var preBaked = Resources.Load<ChapterSeedTable>("TowerMaze/ChapterSeedTable");
    if (preBaked != null)
    {
        chapterManager.Initialize(gameConfig.seed, ballPlayerSpeed, preBaked);
        yield break;
    }

    // Pre-bake yok, runtime validation
    if (PlayerPrefs.GetInt("TowerMaze.ChaptersValidated.v1", 0) == 0)
    {
        uiManager.ShowSplashOverlay(); // basit overlay metodu
        var validator = new ChapterValidator(gameConfig, theme, ballPlayerSpeed);
        yield return validator.ValidateAll(gameConfig.seed,
            progress => uiManager.SetSplashProgress(progress));
        uiManager.HideSplashOverlay();
    }
    chapterManager.Initialize(gameConfig.seed, ballPlayerSpeed, null);
}
```

- [ ] **Step 3: ballPlayerSpeed'i GameConfig'ten oku**

GameConfig'e `[SerializeField] private float ballVerticalSpeed = 4f;` ekle, public property üret. Bootstrapper bu değeri okur.

- [ ] **Step 4: ChapterManager.Initialize obsolete overload'ı sil**

Chunk 1 Task 1.2 Step 5'te eklenen `[Obsolete] Initialize(int baseSeed)` overload'ını kaldır. Tüm call site'lar artık ballPlayerSpeed geçiyor.

- [ ] **Step 5: Smoke test**

1. PlayerPrefs'i temizle (Unity → Edit → Clear All PlayerPrefs).
2. Resources/TowerMaze/ChapterSeedTable.asset'i geçici olarak sil.
3. Play et → splash overlay + progress bar görünmeli, 5–10 sn'de bitmeli.
4. Asset'i geri ekle → Play → instant boot, validation atlanır.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/TowerMaze/Runtime/TowerMazeBootstrapper.cs Assets/Scripts/TowerMaze/Runtime/ChapterManager.cs
git commit -m "feat(bootstrap): wire pre-baked seed table with runtime fallback"
```

---

## Final Verification

### Acceptance test checklist

- [ ] Chapter 1 oynanabilir, 50m hedefi ~15s civarı uygun lava buffer ile bitirilebiliyor.
- [ ] Chapter 50 bitince TierCelebrationScreen açılıyor, +500 EMBER, tap-to-dismiss çalışıyor.
- [ ] Chapter 250 oynanabilir, hedef ~275m, lava daha agresif.
- [ ] Chapter 500 oynanabilir, hedef tam **500m**, lava buffer ~%5.
- [ ] Endless mod regression yok (rotation, lava, maze davranışı eski).
- [ ] ChapterSelectScreen 10 tier × 50 cell, smooth scroll, en yüksek açık bölüme auto-scroll.
- [ ] İlk açılış: ChapterSeedTable asset varsa instant; yoksa progress bar 5–10 sn.
- [ ] PreValidateChaptersTool editor menüsünden çalışınca asset üretiyor.
- [ ] Tap-to-dismiss tüm chapter ekranlarında 350ms cooldown ile çalışıyor.
- [ ] Build ekran üzerinde Android cihazda smoke (manual).

### Final commit

```bash
git add -A
git commit --allow-empty -m "feat(chapter): 500-chapter system fully wired and verified"
```

---

## Plan Notes

- **TDD yapılmadı**: Codebase'de Tests/ dizini ve NUnit yok, mevcut pattern Editor playtest. Plan integration verification odaklı.
- **B_PLAYER kalibrasyonu**: GameConfig.ballVerticalSpeed default 4 m/s. Implementation sonrası birkaç bölüm oynayıp süre eğrisini doğrula. Eğer chapter 1 < 10s veya chapter 500 > 5min ise, B_PLAYER veya H_MIN ayarla.
- **MAX_ATTEMPTS = 16**: Solver 16 denemede hâlâ valid çözüm bulamazsa Debug.LogError. Bu olursa formül parametreleri (özellikle safetyMargin) çok agresif demektir, gevşetmek gerekir.
- **DifficultyProfile dokunulmaz**: Mevcut asset endless ve chapter rotation için ortak. Chapter mode sadece sinkSpeed ve maze parametrelerini override eder.
- **PlayerPrefs migration**: Mevcut `TowerMaze.ChapterBest.{1..50}` korunur. 51–500 ilk kez 0. UnlockedChapters minimum 1 (mevcut davranış).
