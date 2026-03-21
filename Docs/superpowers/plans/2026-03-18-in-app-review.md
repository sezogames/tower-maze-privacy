# In-App Review (Google Play Native Dialog) Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Android'de `market://` URL yönlendirmesini kaldırıp Google Play In-App Review API ile native dialog göster; iOS mevcut `Device.RequestStoreReview()` kalır.

**Architecture:** `MonetizationSystems.cs` içine `InAppReviewManager` sınıfı eklenir. `RunManager.ReturnToMainMenu()` içindeki `#if` bloğu bu sınıfın `RequestReview()` metodunu çağıracak şekilde güncellenir. `TowerMazeBootstrapper` manager'ı oluşturup `RunManager`'a enjekte eder.

**Tech Stack:** Unity 2022+, Google Play Core Unity Plugin (`com.google.play.review` 1.8.1), `#if UNITY_ANDROID / UNITY_IOS` preprocessor

---

## Chunk 1: Paket Kurulumu

### Task 1: `com.google.play.review` paketini ekle

**Files:**
- Modify: `Packages/manifest.json`

- [ ] **Adım 1: `manifest.json`'a scoped registry ve paketi ekle**

`com.google.play.review` Unity'nin default registry'sinde değil, Google'ın kendi registry'sinde. Önce `scopedRegistries` bloğunu, ardından `dependencies` girişini ekle.

`manifest.json`'un en üstüne (veya `"dependencies"` bloğundan önce) şunu ekle:

```json
"scopedRegistries": [
  {
    "name": "Google",
    "url": "https://unitypackage.google.com",
    "scopes": ["com.google"]
  }
],
```

Ardından `"dependencies"` bloğuna — alfabetik olarak `com.unity.*` paketlerinden önce:

```json
"com.google.play.review": "1.8.1",
```

Sonuç şu şekilde görünmeli:

```json
{
  "scopedRegistries": [
    {
      "name": "Google",
      "url": "https://unitypackage.google.com",
      "scopes": ["com.google"]
    }
  ],
  "dependencies": {
    "com.google.play.review": "1.8.1",
    "com.unity.2d.sprite": "1.0.0",
    ...
  }
}
```

- [ ] **Adım 2: Unity Editor'ı aç, paketin indirilip derlendiğini doğrula**

Unity Editor'da `Window → Package Manager` → sol üstte `In Project` seç → `Google Play Review` görünmeli.

Hata alınırsa: `com.google.play.core` bağımlılığı eksik olabilir — `dependencies`'e şunu da ekle:
```json
"com.google.play.core": "1.8.1",
```

---

## Chunk 2: InAppReviewManager Sınıfı

### Task 2: `InAppReviewManager` sınıfını yaz

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/MonetizationSystems.cs` (dosyanın sonuna, son `}` kapanışından önce)

- [ ] **Adım 1: Dosyanın sonunu bul**

`MonetizationSystems.cs` dosyasını aç. Son satırlarda `namespace TowerMaze` bloğunu kapatan `}` var. Yeni sınıf bu kapanıştan hemen önce eklenir.

- [ ] **Adım 2: Sınıfı ekle**

```csharp
public sealed class InAppReviewManager : MonoBehaviour
{
#if UNITY_ANDROID
    private Google.Play.Review.ReviewManager reviewManager;
#endif

    public void Initialize()
    {
#if UNITY_ANDROID
        reviewManager = new Google.Play.Review.ReviewManager();
#endif
    }

    /// <summary>
    /// iOS: native RequestStoreReview.
    /// Android: Google Play In-App Review API (native dialog, oyundan çıkılmaz).
    /// Editor: sadece log.
    /// Google kotayı aştığında diyalogu kendisi bastırır — hata fırlatmaz.
    /// </summary>
    public void RequestReview()
    {
#if UNITY_IOS
        UnityEngine.iOS.Device.RequestStoreReview();
#elif UNITY_ANDROID
        StartCoroutine(RequestReviewCoroutine());
#else
        UnityEngine.Debug.Log("[InAppReviewManager] RequestReview called (editor — no-op)");
#endif
    }

#if UNITY_ANDROID
    private System.Collections.IEnumerator RequestReviewCoroutine()
    {
        var requestFlowOperation = reviewManager.RequestReviewFlow();
        yield return requestFlowOperation;

        if (requestFlowOperation.Error != Google.Play.Review.ReviewErrorCode.NoError)
        {
            UnityEngine.Debug.LogWarning($"[InAppReviewManager] RequestReviewFlow failed: {requestFlowOperation.Error}");
            yield break;
        }

        var reviewInfo = requestFlowOperation.GetResult();
        var launchFlowOperation = reviewManager.LaunchReviewFlow(reviewInfo);
        yield return launchFlowOperation;

        if (launchFlowOperation.Error != Google.Play.Review.ReviewErrorCode.NoError)
        {
            UnityEngine.Debug.LogWarning($"[InAppReviewManager] LaunchReviewFlow failed: {launchFlowOperation.Error}");
        }
        // Başarı durumunda log yok — Google API dialog gösterip göstermemeyi kendisi karar verir.
    }
#endif
}
```

- [ ] **Adım 3: Derleme hatası yok mu kontrol et**

Unity Console'da hata yoksa devam et. Hata varsa:
- `Google.Play.Review` namespace bulunamıyorsa → Chunk 1'e dön, paket kurulumunu tekrarla
- `ReviewManager` bulunamıyorsa → Package Manager'da paketin `1.8.1` versiyonu yüklü mü kontrol et

---

## Chunk 3: Bootstrapper + RunManager Entegrasyonu

### Task 3: Bootstrapper'da `InAppReviewManager` oluştur

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/TowerMazeBootstrapper.cs` (Awake metodu)

- [ ] **Adım 1: Manager nesnesini oluştur ve initialize et**

`TowerMazeBootstrapper.cs` → `Awake()` içinde, `rewardedAdManager` satırının hemen altına ekle:

```csharp
// Mevcut:
RewardedAdManager rewardedAdManager = EnsureComponent<RewardedAdManager>(EnsureChild(managersRoot, "RewardedAdManager"));

// Altına ekle:
InAppReviewManager inAppReviewManager = EnsureComponent<InAppReviewManager>(EnsureChild(managersRoot, "InAppReviewManager"));
```

- [ ] **Adım 2: `Initialize()` çağrısını ekle**

Aynı dosyada `rewardedAdManager.Initialize(gameConfig);` satırının hemen altına:

```csharp
inAppReviewManager.Initialize();
```

- [ ] **Adım 3: `RunManager.Initialize()` çağrısına `inAppReviewManager` parametresini ekle**

`runManager.Initialize(...)` çağrısını bul (L112). Parametre listesinin sonuna ekle:

```csharp
runManager.Initialize(gameConfig, difficultyProfile, themeDefinition, towerGenerator,
    playerController, lavaController, scoreManager, economyManager, rewardedAdManager,
    audioManager, uiManager, backdropController, cameraFollow, inAppReviewManager);
```

### Task 4: `RunManager`'ı güncelle

**Files:**
- Modify: `Assets/Scripts/TowerMaze/Runtime/RunSystems.cs`

- [ ] **Adım 1: `RunManager` alanını ekle**

`RunManager` sınıfında diğer manager alanlarının yanına:

```csharp
private InAppReviewManager inAppReviewManager;
```

- [ ] **Adım 2: `Initialize()` imzasına parametre ekle**

`RunManager.Initialize(...)` metodunun **en sonundaki iki optional parametreden sonra** yeni optional parametre ekle. Mevcut imzanın sonu şöyle görünüyor:

```csharp
EnvironmentBackdropController backdrop = null,
CameraFollowController cameraFollow = null)
```

Bunu şu şekilde değiştir:

```csharp
EnvironmentBackdropController backdrop = null,
CameraFollowController cameraFollow = null,
InAppReviewManager inAppReviewManager = null)
```

> **Önemli:** C# kuralı gereği optional parametreden sonra required parametre gelemez. Bu yüzden `inAppReviewManager = null` olarak tanımlanmalı.

Ve metod gövdesinde ata:

```csharp
this.inAppReviewManager = inAppReviewManager;
```

- [ ] **Adım 3: `ReturnToMainMenu()` içindeki eski bloğu değiştir**

Mevcut kod (L2876-2884):
```csharp
if (economyManager.ShouldRequestReview())
{
    economyManager.MarkReviewRequested();
#if UNITY_IOS
    UnityEngine.iOS.Device.RequestStoreReview();
#elif UNITY_ANDROID
    Application.OpenURL("market://details?id=" + Application.identifier);
#endif
}
```

Yeni kod:
```csharp
if (economyManager.ShouldRequestReview())
{
    economyManager.MarkReviewRequested();
    inAppReviewManager?.RequestReview();
}
```

> `?.` null-guard: `inAppReviewManager` initialize edilmemişse hata atmaz.

- [ ] **Adım 4: Derleme hatası yok mu kontrol et**

Unity Console temiz olmalı.

---

## Chunk 4: Doğrulama

### Task 5: Editor'da çalıştır

- [ ] **Adım 1: Play mode'a gir**

Unity Editor → Play. Console'da hata yok mu?

- [ ] **Adım 2: Review trigger'ı simüle et**

`EconomyManager.ShouldRequestReview()` koşulu: `TotalRuns >= 5 && ReviewRequested flag = 0` (`RunSystems.cs` L1749-1751).

`PlayerPrefs`'teki `TowerMaze.ReviewRequested` key'ini sıfırla:
- Unity Editor → `Edit → Clear All PlayerPrefs`
- Ardından 5 run bitir ya da `EconomyManager.TotalRuns` değerini inspector'dan geçici olarak `5`'e ayarla

- [ ] **Adım 3: Editor log'unu kontrol et**

`[InAppReviewManager] RequestReview called (editor — no-op)` logu Console'da görünmeli.

### Task 6: Android build testi

- [ ] **Adım 1: Android build al**

`File → Build Settings → Android → Build And Run`

- [ ] **Adım 2: 5 run tamamla**

Cihazda 5 run bitir → `ReturnToMainMenu` tetiklenir → Google Play native dialog (veya sessiz geçme, kota aşıldıysa) beklenir.

> **Not:** Google Play In-App Review API'si kota kontrolü yapar. Test cihazında dialog görünmeyebilir. Google Play Console'dan `Internal Testing` kanalına upload edip test etmek gerekir.

- [ ] **Adım 3: Logcat'i kontrol et**

`adb logcat | grep InAppReviewManager` — hata yoksa başarılı.

---

## Notlar

- **iOS değişmedi:** `Device.RequestStoreReview()` zaten native, dokunulmadı.
- **Kota:** Google, kullanıcı başına yılda 1-2 defa dialog gösterir. Bu beklenen davranış, bug değil.
- **`market://` tamamen kaldırıldı:** Kullanıcıyı oyundan çıkaran eski yöntem artık yok.
- **Cihazda test için Play Console gerekli:** Google Play In-App Review API, uygulamanın Play Console'da en az `Internal Testing` kanalında yayınlanmış olmasını gerektirir. Debug APK ile cihazda dialog görünmez; logcat'te hata da çıkmaz (API sessizce geçer).
