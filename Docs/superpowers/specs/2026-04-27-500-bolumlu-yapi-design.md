# 500 Bölümlü Yapı — Design

**Tarih:** 2026-04-27
**Durum:** Brainstorm tamamlandı, implementation plan bekliyor

---

## 1. Bağlam

TowerMaze şu an 50 bölümlü episodik bir sistemle çalışıyor. Mevcut yapı:
- `ChapterManager.TotalChapters = 50`
- Per-chapter `DifficultyOffset` (DifficultyProfile'a height shift) + `ZoneOffset` (maze segment shift) eksenleri.
- Bölüm tamamlama: `ChapterCompleteScreen`, ölüm: `ChapterCompleteScreen.SetFailState` (tap-to-dismiss).
- Endless mod ayrı, `DifficultyProfile.Evaluate(playerHeight)` ile dinamik zorluk.

Kullanıcı 500 bölüme genişletme istiyor:
- Top hızı sabit (oyuncu input'u — zaten sabit).
- Tower rotation hızı sabit, **endless modla birebir aynı** (chapter mode rotation'ı için endless DifficultyProfile akışı korunur, kayma yok).
- Lava yükselme hızı per-chapter değişken.
- Maze karmaşıklığı per-chapter değişken (twistiness, branch density, dead ends, decision points).
- Hedef yükseklik per-chapter değişken — **chapter 500 = 500m kilit nokta**.
- Optimal hamlelerle bitirilebilir olmalı (impossible level olmamalı).

Ek istek:
- Bölüm bitiş/ölüm ekranı 3 saniyelik auto-return yerine **tap-to-dismiss** olmalı (zaten implement edildi).

---

## 2. Mimari Genel Bakış

### Sabitler (endless ile aynı, chapter mode dokunmaz)
- **Tower rotation**: `DifficultyProfile.Evaluate(playerHeight).rotationSpeed` (0–20m: 8°/s, 20–60m: 10°/s, 60m+: 12.5°/s). Chapter index'e göre kayma yok.
- **Top hızı**: Oyuncu input'undan üretilir, chapter mode değiştirmez.

### Chapter-only override
- `sinkSpeed` — `ChapterDefinition.SinkSpeed`'ten okunur.
- Maze parametreleri — `ChapterDefinition.MazeSettings`'ten okunur.
- `targetHeight` — bölüm hedefi.

### Veri yapıları
```csharp
public readonly struct ChapterDefinition {
    public readonly int Index;
    public readonly int TierIndex;          // 1..10
    public readonly float Complexity;       // 0..1, c(n)
    public readonly float TargetHeight;     // m
    public readonly float SinkSpeed;        // m/s
    public readonly MazeSettings MazeSettings;
    public readonly string DisplayName;     // "LEVEL N" (lokalize edilirken kullanılır)
}

public readonly struct MazeSettings {
    public readonly float pathTwistiness;
    public readonly float branchDensity;
    public readonly float deadEndDensity;
    public readonly float decisionDensity;
    public readonly int minDecisionPoints;
    public readonly int minDeadEnds;
}
```

Eski alanlar (`DifficultyOffset`, `ZoneOffset`) ve helper metodlar (`ComputeDifficultyOffset`, `ComputeZoneOffset`) kaldırılır.

---

## 3. Zorluk Eğrisi Formülleri

### Tuning sabitleri (playtest sırasında kalibre edilecek)
- `B_PLAYER` ≈ 4 m/s — top'un düşey hızı × max maze efficiency. Endless modda ölçülüp sabit yazılır.
- `H_MIN = 50m`, `H_MAX = 500m`.
- `LAVA_HEAD_START = 8m`.

### Normalize ilerleme + complexity skaleri
```
n = 1..500
T(n) = floor((n - 1) / 50) + 1            // tier 1..10
k(n) = ((n - 1) % 50) + 1                  // tier içi 1..50
t(n) = (n - 1) / 499                       // 0..1
s(n) = 6t⁵ - 15t⁴ + 10t³                  // smoothstep (Hermite)
c(n) = s(n)                                // complexity scalar 0..1
```

### TargetHeight (birincil eğri, monotonic)
```
targetHeight(n) = lerp(H_MIN, H_MAX, s(n))
// ch 1: 50m, ch 250: 275m, ch 500: 500m (ç hedef)
```

### Maze parametreleri (c'den lerp)
```
mazeEfficiency      = lerp(0.95, 0.50, c)
pathTwistiness      = lerp(0.18, 0.65, c)
branchDensity       = lerp(0.30, 0.78, c)
deadEndDensity      = lerp(0.18, 0.72, c)
decisionDensity     = lerp(0.24, 0.66, c)
minDecisionPoints   = round(lerp(2, 6, c))
minDeadEnds         = round(lerp(1, 7, c))
```

### Beklenen oyun süresi (türev, bilgi amaçlı)
```
playerEffSpeed(n)  = B_PLAYER × mazeEfficiency(c(n))
expectedTime(n)    = targetHeight(n) / playerEffSpeed(n)
```

### Lava sinkSpeed (safety margin'den türev)
```
safetyMargin(n)    = lerp(1.30, 1.05, c(n))    // ch1 %30 buffer → ch500 %5
sinkSpeed(n)       = (targetHeight(n) + LAVA_HEAD_START)
                     / (expectedTime(n) × safetyMargin(n))
```

### Örnek tablo (B_PLAYER = 4 m/s varsayımı)

| n | tier | c | targetHeight | mazeEff | beklenenSüre | sinkSpeed | safety |
|---|------|------|--------------|---------|--------------|-----------|--------|
| 1   | 1  | 0.000 | 50m  | 0.950 | 13.2s   | 2.92 m/s | 1.300 |
| 50  | 1  | 0.028 | 63m  | 0.937 | 16.7s   | 2.88 m/s | 1.292 |
| 100 | 2  | 0.105 | 97m  | 0.903 | 26.9s   | 2.85 m/s | 1.270 |
| 200 | 4  | 0.317 | 193m | 0.807 | 59.7s   | 2.65 m/s | 1.221 |
| 250 | 5  | 0.500 | 275m | 0.725 | 94.8s   | 2.45 m/s | 1.175 |
| 300 | 6  | 0.683 | 357m | 0.643 | 138.8s  | 2.27 m/s | 1.129 |
| 400 | 8  | 0.937 | 472m | 0.529 | 223.1s  | 2.07 m/s | 1.063 |
| 500 | 10 | 1.000 | 500m | 0.500 | 250.0s  | 1.99 m/s | 1.050 |

### Eğri özellikleri
- TargetHeight monotonic ↗ (her bölüm öncekinden uzun veya eşit).
- Maze karmaşıklığı monotonic ↗.
- Lava buffer'ı monotonic ↘ (rahat → tight).
- Erken bölümler 15–30 sn (warmup), geç bölümler 3–4 dk (challenge).

---

## 4. Solver Tasarımı

### Amaç
Her bölümün **optimal oynanışla bitirilebilir** olduğunu, gerçek oyun başlamadan kanıtlamak. Üretilen maze + sinkSpeed + targetHeight kombinasyonu çözülemiyorsa seed'i değiştirip yeniden dene.

### Solver girdisi
- TowerGenerator'ın segment-bazlı maze data yapısını düz cell grid'e indirgeyen adapter.
- Her cell'in 4 yöne (N/S/E/W) duvarı var/yok bilgisi.
- Tower silindirsel: x ekseni `% gridWidth` ile döngüsel komşuluk.
- Edge weight: `cellHeight / B_PLAYER` (mesafe-bazlı, time-bazlı; rotation'ın pozitif katkısı ihmal edilir — admissible).

### Algoritma: A*
- Start: tower tabanındaki tüm cells (oyuncu en alttan başlar, x serbest).
- Goal: `y >= targetHeight` olan herhangi bir cell.
- Heuristic: `(targetHeight - currentY) / B_PLAYER` (admissible).
- Open set: priority queue (binary min-heap).
- Tipik node sayısı: 5k–25k arası.

### Çözülebilirlik kriteri
```csharp
optimalPathTime = AStar(maze, start, goal).cost;
lavaBudget      = (targetHeight + LAVA_HEAD_START) / sinkSpeed(n);
isSolvable      = optimalPathTime * safetyMargin(n) <= lavaBudget;
```
`safetyMargin` zaten formülde olduğu için ortalama olarak garantili. Solver kötü seed sapma kanunlarını yakalar.

### Re-roll mekaniği
```csharp
const int MAX_ATTEMPTS = 16;
for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++) {
    seed = baseSeed ^ (n * 7919) ^ (attempt * 12911);
    maze = TowerGenerator.BuildPreview(seed, mazeSettings(n), targetHeight(n));
    if (Solver.IsSolvable(maze, sinkSpeed(n), targetHeight(n), safetyMargin(n))) {
        cachedAttempt[n] = attempt;
        break;
    }
}
if (attempt == MAX_ATTEMPTS) {
    cachedAttempt[n] = MAX_ATTEMPTS - 1;  // fallback, formül agresif demek
    Debug.LogError($"Chapter {n} failed validation after {MAX_ATTEMPTS} attempts");
}
```

### Cache
```
PlayerPrefs (versiyonlu):
  TowerMaze.ChaptersValidated.v1     bool   // tüm 500 bölüm doğrulanmış mı?
  TowerMaze.ChapterSeedAttempt.{n}   int    // 0..15, geçerli attempt
```
Sadece attempt numarası saklanır. Runtime'da gerçek seed `compute(baseSeed, n, attempt)` ile üretilir. Formül değişirse versiyon `v2`'ye çıkar, cache invalidate.

### İlk açılış UX
- `Bootstrapper`: `TowerMaze.ChaptersValidated.v1` flag yoksa splash overlay'i progress bar'la açar, `ChapterValidator.ValidateAll(progress)` coroutine'i her bölümden sonra `yield return null` ile frame'e döner.
- Tahmini maliyet: 500 × ~12ms = **3–9 saniye**.

### Editor pre-bake (önerilen optimizasyon)
- `Tools/TowerMaze/Pre-Validate Chapters` editor menü item'ı: doğrulamayı editor'da yapıp sonuçları `Resources/TowerMaze/ChapterSeedTable.asset`'e yazar.
- Build'de bu asset varsa runtime validation tamamen atlanır → first-launch hızı 0.
- Formül değişikliklerinden sonra geliştirici yeniden çalıştırır, asset commit eder.

---

## 5. UI Değişiklikleri

### Tier Celebration Screen (yeni)
- **Tetikleme**: `RunSystems.CompleteChapterRun(idx)` içinde `idx % 50 == 0` ise `ChapterCompleteScreen` yerine `TierCelebrationScreen` açılır.
- **İçerik**:
  - `"TIER {tier} USTASI!"` başlığı (tr: TIER N USTASI / en: TIER N MASTER / es: ¡MAESTRO DE NIVEL N!)
  - Büyük tier rozeti (`Resources/TowerMaze/UITheme/tier_badges/tier_{n}` sprite — sonradan eklenir, fallback prosedürel renkli madalya).
  - "+{tier × 500} EMBER" bonus ödülü (tier 1 = 500, tier 5 = 2500, tier 10 = 5000).
  - "Devam etmek için ekrana dokun" — `ChapterCompleteScreen` ile aynı tap-to-dismiss pattern (350ms cooldown).
- **Render**: `ChapterCompleteScreen` ile aynı layered yapı (MenuBg → dimmer → candy panel → glow → texts). Reward panel daha büyük ve soft pulse animasyonlu.
- **Tier 10 sonu** (ch 500 sonrası): aynı kutlama + ekstra "TÜM BÖLÜMLER TAMAMLANDI" tag'i bir kez.

### ChapterSelectScreen (refactor)
- Eski: 5×10 grid (50 bölüm).
- Yeni: 10 tier section'lı vertical scroll.
- Her tier section:
  - Header: tier name + tamamlanan/toplam (`★★★ 50/50` formatı).
  - 5 column × 10 row = 50 cell grid.
- Tier name'leri lokalize: 10 element'lik dizi (Acemi, Öğrenci, Yetenekli, Usta, Şampiyon, Sergeden, Lord, Efsane, Tanrı, Ölümsüz) — final isimler çeviride netleşecek.
- Toplam scroll height: ~10 tier × 600px ≈ 6000px.
- Açılışta `ScrollToChapter(unlockedUpTo)` ile en yüksek açık bölüme otomatik konumlanır.
- Cell state'ler korunur: kilitli / açık / tamamlandı (mevcut renderer mantığı).

### Bootstrapper splash overlay
- İlk açılışta validation çalışıyorsa, mevcut splash sistemine bir progress bar overlay eklenir.
- Mesaj: tr "Bölümler hazırlanıyor..." / en "Setting up chapters..." / es "Preparando niveles...".

---

## 6. Implementation Order

### Aşama 1 — Veri katmanı
1. `ChapterDefinition` struct yenilenir (yukarıdaki alanlar).
2. `MazeSettings` struct (yeni dosya veya `ChapterManager` içinde).
3. `ChapterManager.TotalChapters = 500`. Yeni `ComputeTargetHeight`, `ComputeMazeSettings`, `ComputeSinkSpeed` formülleri (Bölüm 3'teki).
4. Eski `DifficultyOffset` ve `ZoneOffset` ile ilgili tüm kod kaldırılır.

### Aşama 2 — Solver
5. Yeni `Runtime/ChapterValidator.cs`: A* + re-roll + cache. Coroutine API'lı.
6. `ChapterManager.Initialize` async-friendly hale gelir.
7. `TowerGenerator.BuildPreview(seed, mazeSettings, height)` yardımcı API (sadece solver için, oyun runtime'ı etkilemez).

### Aşama 3 — Run akışı
8. `RunSystems.PrepareFreshRun`: chapter modunda `towerGenerator.SetChapterMazeSettings(maze)` ve `towerGenerator.SetChapterSinkSpeed(sink)` çağrıları.
9. `TowerSystems.cs`: yeni `SetChapterMazeSettings` ve `SetChapterSinkSpeed` metodları. `UpdateDifficulty` chapter modunda DifficultyProfile yerine bunları kullanır — **rotation hariç**: rotation halen `DifficultyProfile.Evaluate(playerHeight).rotationSpeed`.
10. `RunSystems.CompleteChapterRun`: tier kutlaması routing (`idx % 50 == 0` → TierCelebration).

### Aşama 4 — UI
11. Yeni `Runtime/UISystems/TierCelebrationScreen.cs`. `UIManager.ShowTierCelebration(tier, bonus, onContinue)` eklenir.
12. `ChapterSelectScreen` 500 bölüm + tier section'lı vertical scroll'a refactor.
13. Bootstrapper'a first-launch validation overlay (mevcut splash sistemiyle entegre).

### Aşama 5 — Editor pre-bake (opsiyonel)
14. `Editor/PreValidateChaptersTool.cs`: menu item, sonuçları `Resources/TowerMaze/ChapterSeedTable.asset`'e yazar.
15. `ChapterManager.Initialize`: asset varsa runtime validation atlanır.

---

## 7. Edge Case'ler ve Karar Notları

- **Bölüm 500 sonrası**: `UnlockedUpTo` 500'de kapanır. Ana menüde START butonu son tamamlanan bölümü tekrar oynatır. Tier 10 kutlamasından sonra "TÜM BÖLÜMLER TAMAMLANDI" paragrafı bir kez gösterilir.
- **Endless mode korunur**: Tüm chapter-only path'ler `if (activeRunMode == RunMode.Chapter)` guard'larıyla kapatılır. DifficultyProfile endless'da aynen çalışır.
- **Memory**: 500 ChapterDefinition struct ≈ 32KB. PlayerPrefs cache ≈ 2KB. İhmal edilebilir.
- **Versiyonlama**: `B_PLAYER` / `H_MAX` / curve değişirse `ChaptersValidated.v1` → `v2` bump, otomatik re-validate.
- **Tap-to-dismiss cooldown**: 350ms. Ekran açıldığı anda son in-game tap'in kazara dismiss yapmasını engeller.

---

## 8. Doğrulama Kriterleri

Implementation tamamlandığında:
1. Chapter 1, 50, 100, 250, 500 oynanabilir ve tablo'daki süreler ±%20 toleransla doğrulanır.
2. Solver edit-time'da çalıştırılır, 500 bölümün hepsi `MAX_ATTEMPTS` içinde valid çıkar.
3. Endless mode'da hiçbir regression yok — eski rotation/lava davranışı bire bir korunur.
4. Tier 1, 5, 10 bitişinde tier celebration screen tetiklenir, EMBER bonus ekonomi sistemine ulaşır.
5. ChapterSelectScreen 500 cell'i scroll-friendly render eder, en yüksek açık bölüme auto-scroll yapar.
6. PlayerPrefs migration: eski `TowerMaze.ChapterBest.{n}` (1–50) korunur, yeni indeks'ler (51–500) ilk kez 0 başlar.
7. İlk açılışta validation 10 saniye altında biter (veya editor pre-bake ile 0 sn).
