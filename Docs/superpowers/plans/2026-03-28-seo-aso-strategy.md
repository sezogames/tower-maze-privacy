# TowerMaze SEO & ASO Strategy Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create all store metadata files, build a landing page, and prepare social media content templates for TowerMaze's launch on Google Play and App Store.

**Architecture:** Store metadata as structured JSON files per language. Landing page as a static HTML/CSS/JS site deployable to GitHub Pages or Netlify. Social media templates as markdown reference docs. All artifacts live under `Docs/Marketing/`.

**Tech Stack:** HTML5, CSS3, vanilla JS (landing page). JSON (store metadata). Markdown (social media templates).

**Spec:** `Docs/superpowers/specs/2026-03-28-seo-aso-strategy-design.md`

---

## Chunk 1: Store Metadata Files

### Task 1: Create Store Metadata Directory Structure

**Files:**
- Create: `Docs/Marketing/store/en/metadata.json`
- Create: `Docs/Marketing/store/tr/metadata.json`

- [ ] **Step 1: Create directory structure**

```bash
mkdir -p Docs/Marketing/store/en Docs/Marketing/store/tr
```

- [ ] **Step 2: Create EN metadata.json**

Create `Docs/Marketing/store/en/metadata.json` with this exact content:

```json
{
  "language": "en",
  "google_play": {
    "title": "TowerMaze: Climb & Escape Lava",
    "short_description": "Climb the rotating lava tower! Dodge traps, navigate mazes & escape the heat.",
    "full_description": "🔥 Can you escape the rising lava?\n\nTowerMaze is a thrilling climbing game where you navigate a rotating maze tower while lava rises beneath you. One wrong turn and you're toast!\n\n🏔️ CLIMB THE TOWER\nRace upward through a procedurally generated maze tower that gets harder the higher you climb. Every run is different — no two towers are the same.\n\n🌀 ROTATING MAZE CHALLENGE\nThe tower rotates as you climb, turning simple paths into brain-twisting puzzles. Find the right path before the lava catches up!\n\n🎯 EASY TO PLAY, HARD TO MASTER\nSimple swipe controls, but the maze gets trickier with dead ends, branching paths, and surprise lava rushes. How high can you go?\n\n🎨 UNLOCK EPIC BALL SKINS\nCollect Ember coins and unlock unique ball skins — from Molten Core to Neon Pro to Golden Glory. Each with custom effects and trails.\n\n🏆 FEATURES\n• Endless procedural tower — never the same run twice\n• Rotating maze mechanics with increasing difficulty\n• 13+ unique ball skins with custom VFX\n• Tower skins to change the environment\n• Daily missions and streak rewards\n• Checkpoint system for epic comebacks\n• Offline play — no wifi needed\n• Challenge yourself to climb higher every run\n\nDownload TowerMaze now and see how high you can climb! 🔥",
    "category": "Arcade",
    "tags": ["Arcade", "Casual", "Puzzle", "Single Player", "Offline"],
    "content_rating": "Everyone"
  },
  "app_store": {
    "title": "TowerMaze: Climb & Escape",
    "subtitle": "Lava Tower Ball Climbing Game",
    "keywords": "tower,maze,climb,lava,ball,escape,helix,stack,rotate,endless,fire,run,dodge,puzzle,arcade,3d,casual",
    "promotional_text": "New skins and daily missions added! How high can you climb?",
    "description": "🔥 Can you escape the rising lava?\n\nTowerMaze is a thrilling climbing game where you navigate a rotating maze tower while lava rises beneath you. One wrong turn and you're toast!\n\n🏔️ CLIMB THE TOWER\nRace upward through a procedurally generated maze tower that gets harder the higher you climb. Every run is different — no two towers are the same.\n\n🌀 ROTATING MAZE CHALLENGE\nThe tower rotates as you climb, turning simple paths into brain-twisting puzzles. Find the right path before the lava catches up!\n\n🎯 EASY TO PLAY, HARD TO MASTER\nSimple swipe controls, but the maze gets trickier with dead ends, branching paths, and surprise lava rushes. How high can you go?\n\n🎨 UNLOCK EPIC BALL SKINS\nCollect Ember coins and unlock unique ball skins — from Molten Core to Neon Pro to Golden Glory. Each with custom effects and trails.\n\n🏆 FEATURES\n• Endless procedural tower — never the same run twice\n• Rotating maze mechanics with increasing difficulty\n• 13+ unique ball skins with custom VFX\n• Tower skins to change the environment\n• Daily missions and streak rewards\n• Checkpoint system for epic comebacks\n• Offline play — no wifi needed\n• Challenge yourself to climb higher every run\n\nDownload TowerMaze now and see how high you can climb! 🔥",
    "primary_category": "Games > Arcade",
    "secondary_category_1": "Games > Puzzle",
    "secondary_category_2": "Games > Casual"
  }
}
```

- [ ] **Step 3: Create TR metadata.json**

Create `Docs/Marketing/store/tr/metadata.json` with this exact content:

```json
{
  "language": "tr",
  "google_play": {
    "title": "TowerMaze: Lavdan Kaçış Oyunu",
    "short_description": "Dönen lav kulesine tırman! Tuzaklardan kaç, labirenti çöz ve lavdan kurtul!",
    "full_description": "🔥 Yükselen lavdan kaçabilir misin?\n\nTowerMaze, dönen bir labirent kulesinde tırmanırken altından yükselen lavdan kaçtığın heyecan dolu bir tırmanma oyunu. Bir yanlış dönüş ve her şey biter!\n\n🏔️ KULEYİ TIRMAN\nTırmandıkça zorlaşan prosedürel labirent kulesinde yukarı doğru yarış. Her koşu farklı — hiçbir kule birbirinin aynısı değil.\n\n🌀 DÖNEN LABİRENT MEYDAN OKUMASI\nKule sen tırmanırken dönüyor, basit yolları beyin yakan bulmacalara dönüştürüyor. Lav seni yakalamadan doğru yolu bul!\n\n🎯 OYNAMASI KOLAY, USTALAŞMASI ZOR\nBasit kaydırma kontrolleri, ama labirent çıkmaz sokaklar, ayrılan yollar ve sürpriz lav dalgalarıyla giderek zorlaşıyor. Ne kadar yükseğe çıkabilirsin?\n\n🎨 EPİK TOP GÖRÜNÜMLERİ AÇ\nEmber coin topla ve benzersiz top görünümleri aç — Molten Core'dan Neon Pro'ya, Golden Glory'ye kadar. Her birinin kendine özel efektleri ve izleri var.\n\n🏆 ÖZELLİKLER\n• Sonsuz prosedürel kule — asla aynı koşu yok\n• Artan zorlukla dönen labirent mekanikleri\n• 13+ benzersiz top görünümü ve özel efektler\n• Çevreyi değiştiren kule görünümleri\n• Günlük görevler ve seri ödülleri\n• Epik geri dönüşler için kontrol noktası sistemi\n• Çevrimdışı oyna — wifi gerekmez\n• Her koşuda daha yükseğe tırmanmaya çalış\n\nTowerMaze'i şimdi indir ve ne kadar yükseğe tırmanabileceğini gör! 🔥",
    "category": "Arcade",
    "tags": ["Arcade", "Casual", "Puzzle", "Single Player", "Offline"],
    "content_rating": "Everyone"
  },
  "app_store": {
    "title": "TowerMaze: Lavdan Kaçış",
    "subtitle": "Lav Kulesi Top Tırmanma Oyunu",
    "keywords": "kule,labirent,tirmanma,lav,top,kacis,donen,ates,kosu,bulmaca,arcade,oyun,engel,zeka,tuzak,3d",
    "promotional_text": "Yeni görünümler ve günlük görevler eklendi! Ne kadar yükseğe çıkabilirsin?",
    "description": "🔥 Yükselen lavdan kaçabilir misin?\n\nTowerMaze, dönen bir labirent kulesinde tırmanırken altından yükselen lavdan kaçtığın heyecan dolu bir tırmanma oyunu. Bir yanlış dönüş ve her şey biter!\n\n🏔️ KULEYİ TIRMAN\nTırmandıkça zorlaşan prosedürel labirent kulesinde yukarı doğru yarış. Her koşu farklı — hiçbir kule birbirinin aynısı değil.\n\n🌀 DÖNEN LABİRENT MEYDAN OKUMASI\nKule sen tırmanırken dönüyor, basit yolları beyin yakan bulmacalara dönüştürüyor. Lav seni yakalamadan doğru yolu bul!\n\n🎯 OYNAMASI KOLAY, USTALAŞMASI ZOR\nBasit kaydırma kontrolleri, ama labirent çıkmaz sokaklar, ayrılan yollar ve sürpriz lav dalgalarıyla giderek zorlaşıyor. Ne kadar yükseğe çıkabilirsin?\n\n🎨 EPİK TOP GÖRÜNÜMLERİ AÇ\nEmber coin topla ve benzersiz top görünümleri aç — Molten Core'dan Neon Pro'ya, Golden Glory'ye kadar. Her birinin kendine özel efektleri ve izleri var.\n\n🏆 ÖZELLİKLER\n• Sonsuz prosedürel kule — asla aynı koşu yok\n• Artan zorlukla dönen labirent mekanikleri\n• 13+ benzersiz top görünümü ve özel efektler\n• Çevreyi değiştiren kule görünümleri\n• Günlük görevler ve seri ödülleri\n• Epik geri dönüşler için kontrol noktası sistemi\n• Çevrimdışı oyna — wifi gerekmez\n• Her koşuda daha yükseğe tırmanmaya çalış\n\nTowerMaze'i şimdi indir ve ne kadar yükseğe tırmanabileceğini gör! 🔥",
    "primary_category": "Games > Arcade",
    "secondary_category_1": "Games > Puzzle",
    "secondary_category_2": "Games > Casual"
  }
}
```

- [ ] **Step 4: Validate character limits**

Run a validation script to confirm all fields are within store limits:

```bash
# Quick validation with node or python
python3 -c "
import json
for lang in ['en', 'tr']:
    with open(f'Docs/Marketing/store/{lang}/metadata.json') as f:
        d = json.load(f)
    gp = d['google_play']
    ap = d['app_store']
    checks = [
        ('GP title', gp['title'], 50),
        ('GP short_desc', gp['short_description'], 80),
        ('GP full_desc', gp['full_description'], 4000),
        ('AS title', ap['title'], 30),
        ('AS subtitle', ap['subtitle'], 30),
        ('AS keywords', ap['keywords'], 100),
    ]
    print(f'--- {lang.upper()} ---')
    for name, val, limit in checks:
        ok = '✅' if len(val) <= limit else '❌'
        print(f'{ok} {name}: {len(val)}/{limit}')
"
```

Expected: All fields show ✅.

- [ ] **Step 5: Commit**

```bash
git add Docs/Marketing/store/
git commit -m "feat(marketing): add store metadata files for EN and TR"
```

---

### Task 2: Create Screenshot Brief Document

**Files:**
- Create: `Docs/Marketing/screenshots/screenshot-brief.md`

- [ ] **Step 1: Create screenshot brief**

Create `Docs/Marketing/screenshots/screenshot-brief.md` — a ready-to-hand-off brief for screenshot creation (Figma, Canva, or any design tool):

```markdown
# TowerMaze Screenshot Brief

## General Rules
- Resolution: 1290x2796 (iPhone 15 Pro Max) / 1080x1920 (Android)
- Orientation: Portrait
- No device frames (frameless modern style)
- Background: orange-to-red gradient matching lava theme
- Caption font: Bold sans-serif (Montserrat Black or similar), white with subtle drop shadow
- Caption placement: top 20% of image
- Gameplay area: center 60% of image

## Screenshots (in order)

### 1. Hero Shot — Gameplay
- **EN Caption:** "Climb or Burn!"
- **TR Caption:** "Tırman ya da Yan!"
- **Visual:** Ball climbing tower, lava visible below, tower rotating
- **Focus:** Show the core action — upward movement with visible danger

### 2. Rotating Tower
- **EN Caption:** "Rotating Maze Tower"
- **TR Caption:** "Dönen Labirent Kulesi"
- **Visual:** Close-up of tower showing maze paths, slight motion blur on rotation
- **Focus:** Maze complexity and the rotating mechanic

### 3. Lava Rush
- **EN Caption:** "Survive the Lava Rush!"
- **TR Caption:** "Lav Dalgasından Kurtul!"
- **Visual:** Lava surging upward, ball barely escaping, screen tinted red
- **Focus:** Tension and danger moment

### 4. Skin Collection
- **EN Caption:** "Collect Epic Skins"
- **TR Caption:** "Epik Skinleri Topla"
- **Visual:** Shop/collection screen showing multiple ball skins
- **Focus:** Variety and visual appeal of cosmetics

### 5. Height Challenge
- **EN Caption:** "How High Can You Go?"
- **TR Caption:** "Ne Kadar Yükseğe Çıkarsın?"
- **Visual:** Bird's eye view looking down the tower, showing height achieved
- **Focus:** Scale and achievement feeling

### 6. Checkpoint
- **EN Caption:** "Checkpoint Saves!"
- **TR Caption:** "Kontrol Noktası Kurtarır!"
- **Visual:** Ball at a checkpoint marker, continue UI visible
- **Focus:** Second chance mechanic

### 7. Daily Missions
- **EN Caption:** "Daily Missions & Rewards"
- **TR Caption:** "Günlük Görevler ve Ödüller"
- **Visual:** Mission list UI with progress bars and reward icons
- **Focus:** Retention features

### 8. Customization
- **EN Caption:** "Customize Your Tower"
- **TR Caption:** "Kuleni Özelleştir"
- **Visual:** Tower with alternate skin/theme applied
- **Focus:** Visual variety and personalization

## Feature Graphic (Google Play, 1024x500)
- Center: Ball character (Molten Core skin) on rotating lava tower
- Background: Dark volcanic with lava glow
- Text: "TowerMaze" (large) + "Climb. Escape. Survive." (tagline)
- Lava particle effects around edges

## App Preview Video (iOS, 15-30s, 1080x1920)
- 0-5s: Dramatic lava rush near-escape (hook)
- 5-15s: Normal gameplay showing climb + maze navigation + tower rotation
- 15-25s: Skin unlock celebration moment
- 25-30s: TowerMaze logo + "Download Free" CTA + store badge
- Audio: Gameplay SFX + subtle background music, no voiceover
```

- [ ] **Step 2: Commit**

```bash
git add Docs/Marketing/screenshots/
git commit -m "docs(marketing): add screenshot and video brief for store assets"
```

---

## Chunk 2: Landing Page

### Task 3: Create Landing Page Structure

**Files:**
- Create: `Docs/Marketing/landing-page/index.html`
- Create: `Docs/Marketing/landing-page/style.css`
- Create: `Docs/Marketing/landing-page/tr/index.html`

- [ ] **Step 1: Create EN landing page HTML**

Create `Docs/Marketing/landing-page/index.html`:

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>TowerMaze — Climb the Rotating Lava Tower | Free Mobile Game</title>
  <meta name="description" content="Escape rising lava in TowerMaze! Navigate a rotating maze tower, unlock epic ball skins, and see how high you can climb. Free on iOS & Android.">
  <meta name="keywords" content="tower maze game, lava climbing game, rotating maze, ball climbing game, free mobile game, endless tower, maze puzzle game">
  <link rel="alternate" hreflang="en" href="https://towermaze.game/">
  <link rel="alternate" hreflang="tr" href="https://towermaze.game/tr/">
  <link rel="canonical" href="https://towermaze.game/">

  <!-- Open Graph -->
  <meta property="og:type" content="website">
  <meta property="og:title" content="TowerMaze — Climb the Rotating Lava Tower">
  <meta property="og:description" content="Escape rising lava in TowerMaze! Navigate a rotating maze tower, unlock epic ball skins, and see how high you can climb.">
  <meta property="og:image" content="https://towermaze.game/images/og-image.png">
  <meta property="og:url" content="https://towermaze.game/">
  <meta property="og:site_name" content="TowerMaze">

  <!-- Twitter Card -->
  <meta name="twitter:card" content="summary_large_image">
  <meta name="twitter:title" content="TowerMaze — Climb the Rotating Lava Tower">
  <meta name="twitter:description" content="Escape rising lava in TowerMaze! Free on iOS & Android.">
  <meta name="twitter:image" content="https://towermaze.game/images/og-image.png">

  <!-- Structured Data -->
  <script type="application/ld+json">
  {
    "@context": "https://schema.org",
    "@type": "MobileApplication",
    "name": "TowerMaze",
    "description": "Climb a rotating lava tower, navigate maze paths, and escape rising lava. Free on iOS & Android.",
    "operatingSystem": "Android, iOS",
    "applicationCategory": "GameApplication",
    "genre": ["Arcade", "Puzzle"],
    "inLanguage": ["en", "tr"],
    "author": {
      "@type": "Organization",
      "name": "SezoGames"
    },
    "offers": {
      "@type": "Offer",
      "price": "0",
      "priceCurrency": "USD"
    }
  }
  </script>

  <link rel="stylesheet" href="style.css">
</head>
<body>
  <!-- Hero -->
  <section id="hero">
    <div class="hero-content">
      <h1>TowerMaze</h1>
      <p class="tagline">Climb. Escape. Survive.</p>
      <p class="hero-desc">Navigate a rotating maze tower while lava rises beneath you. How high can you climb?</p>
      <div class="store-badges">
        <a href="#" class="badge" aria-label="Download on the App Store">
          <img src="images/app-store-badge.svg" alt="Download on the App Store" width="180" height="53">
        </a>
        <a href="#" class="badge" aria-label="Get it on Google Play">
          <img src="images/google-play-badge.png" alt="Get it on Google Play" width="180" height="53">
        </a>
      </div>
    </div>
    <div class="hero-visual">
      <!-- Replace with gameplay GIF or video -->
      <div class="placeholder-visual">Gameplay Preview</div>
    </div>
  </section>

  <!-- Features -->
  <section id="features">
    <h2>Why You'll Love TowerMaze</h2>
    <div class="feature-grid">
      <div class="feature-card">
        <div class="feature-icon">🌀</div>
        <h3>Rotating Maze Tower</h3>
        <p>The tower rotates as you climb, turning every path into a brain-twisting puzzle.</p>
      </div>
      <div class="feature-card">
        <div class="feature-icon">🔥</div>
        <h3>Lava Rush Events</h3>
        <p>Survive sudden lava surges that test your reflexes and route planning.</p>
      </div>
      <div class="feature-card">
        <div class="feature-icon">🎨</div>
        <h3>Epic Ball Skins</h3>
        <p>Unlock 13+ unique skins with custom effects, trails, and particle systems.</p>
      </div>
      <div class="feature-card">
        <div class="feature-icon">🎯</div>
        <h3>Daily Missions</h3>
        <p>Complete daily challenges, earn streaks, and collect exclusive rewards.</p>
      </div>
    </div>
  </section>

  <!-- How to Play -->
  <section id="how-to-play">
    <h2>How to Play</h2>
    <div class="steps">
      <div class="step">
        <div class="step-number">1</div>
        <h3>Swipe</h3>
        <p>Swipe to move your ball through the maze paths on the tower surface.</p>
      </div>
      <div class="step">
        <div class="step-number">2</div>
        <h3>Climb</h3>
        <p>Navigate upward through branching paths and avoid dead ends.</p>
      </div>
      <div class="step">
        <div class="step-number">3</div>
        <h3>Escape</h3>
        <p>Stay ahead of the rising lava and survive as long as you can!</p>
      </div>
    </div>
  </section>

  <!-- Gallery -->
  <section id="gallery">
    <h2>Screenshots</h2>
    <div class="screenshot-scroll">
      <!-- Replace src with actual screenshots -->
      <img src="images/screenshot-1.png" alt="TowerMaze gameplay - climbing the lava tower" loading="lazy" width="300" height="650">
      <img src="images/screenshot-2.png" alt="TowerMaze rotating maze tower" loading="lazy" width="300" height="650">
      <img src="images/screenshot-3.png" alt="TowerMaze lava rush event" loading="lazy" width="300" height="650">
      <img src="images/screenshot-4.png" alt="TowerMaze ball skin collection" loading="lazy" width="300" height="650">
      <img src="images/screenshot-5.png" alt="TowerMaze height challenge" loading="lazy" width="300" height="650">
      <img src="images/screenshot-6.png" alt="TowerMaze checkpoint system" loading="lazy" width="300" height="650">
      <img src="images/screenshot-7.png" alt="TowerMaze daily missions and rewards" loading="lazy" width="300" height="650">
      <img src="images/screenshot-8.png" alt="TowerMaze tower customization" loading="lazy" width="300" height="650">
    </div>
  </section>

  <!-- Reviews (placeholder for post-launch) -->
  <section id="reviews">
    <h2>What Players Say</h2>
    <p class="reviews-placeholder">Reviews coming soon after launch!</p>
  </section>

  <!-- Download CTA -->
  <section id="download">
    <h2>Download Free</h2>
    <p>Available on iOS and Android. No wifi needed!</p>
    <div class="store-badges">
      <a href="#" class="badge" aria-label="Download on the App Store">
        <img src="images/app-store-badge.svg" alt="Download on the App Store" width="180" height="53">
      </a>
      <a href="#" class="badge" aria-label="Get it on Google Play">
        <img src="images/google-play-badge.png" alt="Get it on Google Play" width="180" height="53">
      </a>
    </div>
  </section>

  <!-- Footer -->
  <footer>
    <nav>
      <a href="privacy.html">Privacy Policy</a>
      <a href="terms.html">Terms of Service</a>
      <a href="mailto:contact@sezogames.com">Contact</a>
    </nav>
    <p>&copy; 2026 SezoGames. All rights reserved.</p>
  </footer>
</body>
</html>
```

- [ ] **Step 2: Create CSS**

Create `Docs/Marketing/landing-page/style.css`:

```css
/* TowerMaze Landing Page */
:root {
  --lava-orange: #ff6414;
  --lava-dark: #cc3300;
  --bg-dark: #1a0a04;
  --bg-section: #120802;
  --text-primary: #ffffff;
  --text-secondary: #ffccaa;
  --accent: #ff8c42;
}

* { margin: 0; padding: 0; box-sizing: border-box; }

body {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  background: var(--bg-dark);
  color: var(--text-primary);
  line-height: 1.6;
}

/* Hero */
#hero {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-wrap: wrap;
  gap: 2rem;
  padding: 4rem 2rem;
  background: linear-gradient(135deg, var(--bg-dark) 0%, #2d0e00 50%, var(--bg-dark) 100%);
  text-align: center;
}

h1 {
  font-size: clamp(3rem, 8vw, 5rem);
  font-weight: 900;
  background: linear-gradient(180deg, var(--text-primary), var(--lava-orange));
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.tagline {
  font-size: 1.5rem;
  color: var(--accent);
  margin: 0.5rem 0 1rem;
  font-weight: 600;
}

.hero-desc {
  font-size: 1.1rem;
  color: var(--text-secondary);
  max-width: 500px;
  margin: 0 auto 2rem;
}

.store-badges {
  display: flex;
  gap: 1rem;
  justify-content: center;
  flex-wrap: wrap;
}

.badge img { border-radius: 8px; }

.placeholder-visual {
  width: 300px;
  height: 550px;
  background: rgba(255, 100, 20, 0.1);
  border: 2px dashed var(--lava-orange);
  border-radius: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--text-secondary);
}

/* Features */
#features {
  padding: 5rem 2rem;
  text-align: center;
  background: var(--bg-section);
}

#features h2,
#how-to-play h2,
#gallery h2,
#download h2 {
  font-size: 2rem;
  margin-bottom: 3rem;
  color: var(--text-primary);
}

.feature-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
  gap: 2rem;
  max-width: 1000px;
  margin: 0 auto;
}

.feature-card {
  background: rgba(255, 100, 20, 0.05);
  border: 1px solid rgba(255, 100, 20, 0.15);
  border-radius: 16px;
  padding: 2rem;
}

.feature-icon { font-size: 2.5rem; margin-bottom: 1rem; }
.feature-card h3 { font-size: 1.2rem; margin-bottom: 0.5rem; color: var(--accent); }
.feature-card p { color: var(--text-secondary); font-size: 0.95rem; }

/* How to Play */
#how-to-play {
  padding: 5rem 2rem;
  text-align: center;
}

.steps {
  display: flex;
  gap: 2rem;
  justify-content: center;
  flex-wrap: wrap;
  max-width: 800px;
  margin: 0 auto;
}

.step {
  flex: 1;
  min-width: 200px;
  max-width: 240px;
}

.step-number {
  width: 50px;
  height: 50px;
  border-radius: 50%;
  background: var(--lava-orange);
  color: white;
  font-size: 1.5rem;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 0 auto 1rem;
}

.step h3 { font-size: 1.3rem; margin-bottom: 0.5rem; }
.step p { color: var(--text-secondary); font-size: 0.95rem; }

/* Gallery */
#gallery {
  padding: 5rem 2rem;
  background: var(--bg-section);
  text-align: center;
}

.screenshot-scroll {
  display: flex;
  gap: 1.5rem;
  overflow-x: auto;
  padding: 1rem;
  justify-content: center;
  flex-wrap: wrap;
}

.screenshot-scroll img {
  border-radius: 16px;
  box-shadow: 0 4px 20px rgba(255, 100, 20, 0.2);
}

/* Download CTA */
#download {
  padding: 5rem 2rem;
  text-align: center;
}

#download p {
  color: var(--text-secondary);
  margin-bottom: 2rem;
  font-size: 1.1rem;
}

/* Footer */
footer {
  padding: 2rem;
  text-align: center;
  border-top: 1px solid rgba(255, 100, 20, 0.15);
}

footer nav {
  display: flex;
  gap: 2rem;
  justify-content: center;
  margin-bottom: 1rem;
}

footer a {
  color: var(--text-secondary);
  text-decoration: none;
  font-size: 0.9rem;
}

footer a:hover { color: var(--accent); }
footer p { color: rgba(255, 255, 255, 0.4); font-size: 0.85rem; }

/* Responsive */
@media (max-width: 600px) {
  #hero { padding: 2rem 1rem; }
  .feature-grid { grid-template-columns: 1fr; }
  .steps { flex-direction: column; align-items: center; }
}
```

- [ ] **Step 3: Create TR landing page**

Create `Docs/Marketing/landing-page/tr/index.html` — same structure as EN but with Turkish text. Key differences:

- `<html lang="tr">`
- `<title>TowerMaze — Dönen Lav Kulesine Tırman | Ücretsiz Mobil Oyun</title>`
- `<meta name="description" content="TowerMaze'de yükselen lavdan kaç! Dönen labirent kulesinde tırman, epik top skinleri aç ve ne kadar yükseğe çıkabileceğini gör. iOS ve Android'de ücretsiz.">`
- `<link rel="canonical" href="https://towermaze.game/tr/">`
- hreflang tags same as EN
- Hero: `<p class="tagline">Tırman. Kaç. Hayatta Kal.</p>`
- Hero desc: "Altından lav yükselirken dönen labirent kulesinde yol bul. Ne kadar yükseğe çıkabilirsin?"
- Features section heading: "Neden TowerMaze'i Seveceksin"
  - "Dönen Labirent Kulesi" / "Lav Dalgası" / "Epik Top Görünümleri" / "Günlük Görevler"
- How to Play heading: "Nasıl Oynanır" — steps: "Kaydır" / "Tırman" / "Kaç"
- Gallery heading: "Ekran Görüntüleri"
- Download heading: "Ücretsiz İndir" — sub: "iOS ve Android'de mevcut. WiFi gerekmez!"
- Footer: "Gizlilik Politikası" / "Kullanım Koşulları" / "İletişim"
- CSS: `../style.css` (shared)

- [ ] **Step 4: Create robots.txt and sitemap.xml**

Create `Docs/Marketing/landing-page/robots.txt`:

```
User-agent: *
Allow: /
Sitemap: https://towermaze.game/sitemap.xml
```

Create `Docs/Marketing/landing-page/sitemap.xml`:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9"
        xmlns:xhtml="http://www.w3.org/1999/xhtml">
  <url>
    <loc>https://towermaze.game/</loc>
    <xhtml:link rel="alternate" hreflang="en" href="https://towermaze.game/"/>
    <xhtml:link rel="alternate" hreflang="tr" href="https://towermaze.game/tr/"/>
    <lastmod>2026-03-28</lastmod>
    <priority>1.0</priority>
  </url>
  <url>
    <loc>https://towermaze.game/tr/</loc>
    <xhtml:link rel="alternate" hreflang="en" href="https://towermaze.game/"/>
    <xhtml:link rel="alternate" hreflang="tr" href="https://towermaze.game/tr/"/>
    <lastmod>2026-03-28</lastmod>
    <priority>0.9</priority>
  </url>
</urlset>
```

- [ ] **Step 5: Commit**

```bash
git add Docs/Marketing/landing-page/
git commit -m "feat(marketing): add landing page with EN/TR, SEO meta, structured data"
```

---

## Chunk 3: Social Media Templates & Privacy Policy

### Task 4: Create Social Media Content Templates

**Files:**
- Create: `Docs/Marketing/social/profiles.md`
- Create: `Docs/Marketing/social/hashtags.md`
- Create: `Docs/Marketing/social/launch-week-plan.md`

- [ ] **Step 1: Create profiles.md**

Create `Docs/Marketing/social/profiles.md` — ready-to-paste bio text for each platform:

```markdown
# Social Media Profile Setup

## Account Handles

| Platform | Handle | Priority |
|----------|--------|----------|
| TikTok | @towermaze | High |
| Instagram | @towermaze | High |
| YouTube | @TowerMazeGame | Medium |
| X (Twitter) | @TowerMazeGame | Medium |
| Facebook | TowerMaze Game | Low |

## Bio Text

### English (TikTok, Instagram, X)

```
🔥 Climb the rotating lava tower
🎮 Free on iOS & Android
🏔️ How high can you go?
🔗 towermaze.game
```

### Turkish (Facebook, secondary TikTok)

```
🔥 Dönen lav kulesine tırman
🎮 iOS & Android'de ücretsiz
🏔️ Ne kadar yükseğe çıkarsın?
🔗 towermaze.game
```

### YouTube About

**EN:**
TowerMaze is a free mobile climbing game where you navigate a rotating lava tower.
Swipe to climb, dodge traps, and escape the rising lava!

Download free: [App Store link] | [Google Play link]

Follow us: @towermaze on TikTok & Instagram

**TR:**
TowerMaze, dönen lav kulesinde tırmandığın ücretsiz bir mobil oyun.
Kaydırarak tırman, tuzaklardan kaç ve yükselen lavdan kurtul!

Ücretsiz indir: [App Store link] | [Google Play link]

Bizi takip et: TikTok & Instagram'da @towermaze
```

- [ ] **Step 2: Create hashtags.md**

Create `Docs/Marketing/social/hashtags.md`:

```markdown
# Hashtag Reference

## Always Use (every post)
#TowerMaze #MobileGame #IndieGame

## Gameplay Content (EN)
#LavaEscape #MazeGame #BallGame #HowHighCanYouGo
#GamingChallenge #SatisfyingGameplay #AddictiveGame
#HypercasualGame #EndlessGame #TowerClimb

## Gameplay Content (TR)
#MobilOyun #OyunÖnerisi #BağımlılıkYapanOyun
#LabirentOyunu #TürkçeOyun #IndieOyun
#OyunTavsiyesi #MobilOyunÖnerisi

## Trend/Explore
#gaming #games #mobilegaming #newgame #freegame
#indiegamedev #gamedev #satisfying #oddlysatisfying

## Video Titles (YouTube Shorts)
- "This lava tower game is INSANE 🔥 #TowerMaze"
- "How high can YOU climb? 🏔️ #MobileGame"
- "IMPOSSIBLE lava escape in TowerMaze 😱"

## TikTok Caption Formula
[Hook] + [Gameplay] + [CTA]
Example: "Lav tam arkamda 🔥 Sen ne kadar yükseğe çıkabilirsin? Link bio'da 👆 #TowerMaze"
```

- [ ] **Step 3: Create launch-week-plan.md**

Create `Docs/Marketing/social/launch-week-plan.md`:

```markdown
# Launch Week Content Plan

## Pre-Launch

| Day | Content | Platform | Assets Needed |
|-----|---------|----------|---------------|
| D-3 | Teaser: lava tower silhouette + "Coming Soon" | All | 1 image (1080x1920) |
| D-1 | Trailer (30s gameplay) | YouTube + All | 1 video (1080x1920, 30s) |

## Launch Day & After

| Day | Content | Platform | Assets Needed |
|-----|---------|----------|---------------|
| D+0 | "OUT NOW" post + store links | All | 1 image + store URLs |
| D+1 | "How high?" challenge | TikTok + Reels | 1 gameplay clip (15s) |
| D+3 | Fail compilation | TikTok | 3-5 fail clips edited together (30s) |
| D+5 | Skin showcase | Instagram | 1 loop video (10s) |
| D+7 | First week stats share | X + Stories | 1 infographic |

## Content Production Checklist

- [ ] Record 10+ gameplay clips (various moments: close calls, fails, high scores)
- [ ] Record skin unlock and showcase clips
- [ ] Create "Coming Soon" teaser image
- [ ] Edit 30s trailer
- [ ] Edit fail compilation
- [ ] Create skin showcase loop
- [ ] Prepare first week stats template
```

- [ ] **Step 4: Commit**

```bash
git add Docs/Marketing/social/
git commit -m "docs(marketing): add social media profiles, hashtags, and launch week plan"
```

---

### Task 5: Create Privacy Policy Page

**Files:**
- Create: `Docs/Marketing/landing-page/privacy.html`

- [ ] **Step 1: Create privacy.html**

Create a basic privacy policy page at `Docs/Marketing/landing-page/privacy.html`. This is required by both Google Play and App Store. Include:

- Data collection: analytics (Unity Analytics), advertising (AdMob with personalized ads)
- Third-party SDKs: Google AdMob, Unity Analytics
- Data storage: local device only (no cloud sync at launch)
- Children's privacy: not directed at children under 13
- Contact: contact@sezogames.com
- Use same `style.css` as landing page
- Link back to main page

> **Note:** This is a template. Legal review recommended before publishing.

- [ ] **Step 2: Create terms.html**

Create a basic terms of service page at `Docs/Marketing/landing-page/terms.html` with standard game ToS boilerplate.

- [ ] **Step 3: Commit**

```bash
git add Docs/Marketing/landing-page/privacy.html Docs/Marketing/landing-page/terms.html
git commit -m "docs(marketing): add privacy policy and terms of service pages"
```

---

### Task 6: Create App Links Configuration Files

**Files:**
- Create: `Docs/Marketing/landing-page/.well-known/assetlinks.json`
- Create: `Docs/Marketing/landing-page/.well-known/apple-app-site-association`

- [ ] **Step 1: Create Android asset links**

Create `Docs/Marketing/landing-page/.well-known/assetlinks.json`:

```json
[
  {
    "relation": ["delegate_permission/common.handle_all_urls"],
    "target": {
      "namespace": "android_app",
      "package_name": "com.sezogames.towermaze",
      "sha256_cert_fingerprints": ["TODO:ADD_KEYSTORE_FINGERPRINT"]
    }
  }
]
```

> Replace `TODO:ADD_KEYSTORE_FINGERPRINT` with actual keystore SHA-256 before deployment.

- [ ] **Step 2: Create iOS app site association**

Create `Docs/Marketing/landing-page/.well-known/apple-app-site-association`:

```json
{
  "applinks": {
    "apps": [],
    "details": [
      {
        "appID": "TEAM_ID.com.sezogames.towermaze",
        "paths": ["*"]
      }
    ]
  }
}
```

> Replace `TEAM_ID` with actual Apple Team ID before deployment.

- [ ] **Step 3: Commit**

```bash
git add Docs/Marketing/landing-page/.well-known/
git commit -m "feat(marketing): add app links config for Android and iOS deep linking"
```

---

## Chunk 4: Reference Documents

### Task 7: Create Competitor Analysis Reference

**Files:**
- Create: `Docs/Marketing/competitor-analysis.md`

- [ ] **Step 1: Create competitor-analysis.md**

Create `Docs/Marketing/competitor-analysis.md` preserving the competitive intelligence from the spec:

```markdown
# TowerMaze Competitor Analysis

## Tier 1 — Dominant (500M+ downloads)

| Game | Title Strategy | Category | Key Takeaway |
|------|---------------|----------|--------------|
| Helix Jump | Brand-only title | Arcade | Clean branding, relies on brand recognition |
| Stack Ball | Brand - Action Keywords | Arcade | Keywords in title for discoverability |

## Tier 2 — Direct Niche Competitors

| Game | Overlap | Size | Opportunity |
|------|---------|------|-------------|
| Lava Tower | Closest mechanic match (procedural tower, rising lava) | Small | Direct competition possible |
| Tower Climbing | Tower climbing niche | Medium | Keyword overlap |
| Tower Escape: ball adventure | Ball + tower | Medium | Keyword overlap |
| Blocky Castle: Tower Climb | Tower climbing | Medium | Different art style |

## Our Differentiator

**Climbing UP a rotating maze** vs competitors who have falling DOWN through platforms.
This must be emphasized in all marketing copy.

## Keyword Gap Opportunities (Low Competition)

These keyword combinations have low direct competition from established titles:

- "rotating tower" / "rotating maze game"
- "tower maze" / "maze tower"
- "lava maze" / "climbing maze"
- "ball maze tower"

## Monitoring

Review competitor keyword positions monthly using AppFollow or Sensor Tower.
Update this document when new competitors enter the niche.
```

- [ ] **Step 2: Commit**

```bash
git add Docs/Marketing/competitor-analysis.md
git commit -m "docs(marketing): add competitor analysis reference document"
```

---

### Task 8: Create Measurement & Tracking Plan

**Files:**
- Create: `Docs/Marketing/measurement/tracking-plan.md`

- [ ] **Step 1: Create tracking-plan.md**

Create `Docs/Marketing/measurement/tracking-plan.md`:

```markdown
# TowerMaze ASO Measurement & Tracking Plan

## Tools

| Tool | Purpose | Setup |
|------|---------|-------|
| Google Play Console | Store listing experiments, acquisition reports, search terms | Auto with dev account |
| App Store Connect | Product page optimization, impression sources | Auto with dev account |
| Google Search Console | Landing page search performance | Register towermaze.game |
| Bing Webmaster Tools | Landing page search (Bing/Yandex) | Register towermaze.game |
| AppFollow (free tier) | Keyword ranking tracking | Sign up, add app |

## Key Metrics & Targets

| Metric | Target | Source |
|--------|--------|--------|
| Store conversion rate (impressions → installs) | >30% organic | Play Console / ASC |
| Keyword ranking position (top 20 keywords) | Track weekly | AppFollow |
| Landing page CTR | Track baseline | Search Console |
| Social media referral installs | Track via UTM | UTM-tagged store links |

## Post-Launch Optimization Schedule

| Timeframe | Action |
|-----------|--------|
| Week 1-2 | Collect baseline: impressions, conversion rate, top search terms |
| Week 3 | First Google Play store listing experiment (icon or screenshot A/B test) |
| Week 4 | Revise keywords based on actual search term data from both stores |
| Month 2 | Screenshot A/B test round 2, description optimization |
| Monthly | Review keyword rankings, update long-tail keywords, refresh social content |

## UTM Link Template

Use these UTM parameters for all social media store links:

- Google Play: `https://play.google.com/store/apps/details?id=com.sezogames.towermaze&utm_source={platform}&utm_medium=social&utm_campaign=launch`
- App Store: Use App Store campaign tokens via App Store Connect

Replace `{platform}` with: `tiktok`, `instagram`, `youtube`, `twitter`, `facebook`

## Registration Checklist

- [ ] Register towermaze.game in Google Search Console
- [ ] Register towermaze.game in Bing Webmaster Tools
- [ ] Create AppFollow account and add TowerMaze
- [ ] Set up UTM-tagged links for each social platform
- [ ] Verify landing page is indexed by Google (after deployment)
```

- [ ] **Step 2: Commit**

```bash
git add Docs/Marketing/measurement/
git commit -m "docs(marketing): add measurement and tracking plan for ASO iteration"
```

---

## Final Checklist

After all tasks are complete, verify:

- [ ] All store metadata character limits pass validation
- [ ] Landing page renders correctly in browser (open `index.html` locally)
- [ ] TR landing page links back to EN via hreflang and vice versa
- [ ] Privacy policy and terms pages load correctly
- [ ] Competitor analysis and tracking plan are complete
- [ ] All files committed to git
