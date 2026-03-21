# TowerMaze Design System Spec

## Goal
Define a complete Minimalist Modern design system for TowerMaze to be built in Figma, covering color tokens, typography, spacing, components, and all game screens. This replaces the current procedurally-generated Kenney Future UI.

## Design Decisions
- **Style:** Minimalist Modern
- **Accent:** Deep Purple
- **Font:** Outfit (Google Fonts)
- **Background:** Light
- **Corner radius:** Soft (18px cards, pill buttons)

---

## 1. Color Tokens

### Primary
| Token | Hex | Usage |
|---|---|---|
| `primary` | `#7C3AED` | Buttons, accents, active states |
| `primary-light` | `#A78BFA` | Secondary accents, HUD tint |
| `primary-bg` | `#EDE9FE` | Progress bar background, soft badge bg |

### Surface (Light Mode — all menus)
| Token | Hex | Usage |
|---|---|---|
| `surface` | `#F8F7FF` | Screen/page background |
| `card` | `#FFFFFF` | Card backgrounds |
| `divider` | `#F0EEFF` | Subtle separators |

### Text
| Token | Hex | Usage |
|---|---|---|
| `text-dark` | `#111111` | Primary labels, scores |
| `text-mid` | `#555555` | Body text, descriptions |
| `text-dim` | `#AAAAAA` | Captions, secondary labels |

### Semantic (base colors)
| Token | Hex | Usage |
|---|---|---|
| `success` | `#10B981` | DEVAM ET button, completion states |
| `success-bg` | `#D1FAE5` | Success badge background |
| `success-text` | `#059669` | Success badge text |
| `danger` | `#EF4444` | TRY AGAIN, destructive actions |
| `danger-bg` | `#FEE2E2` | Danger badge background |
| `danger-text` | `#DC2626` | Danger badge text |
| `warning` | `#F59E0B` | Rush warnings, daily challenge strip |
| `warning-bg` | `#FEF3C7` | Warning badge background |
| `warning-text` | `#D97706` | Warning badge text |

### HUD (Dark — in-game overlay only)
| Token | Hex | Usage |
|---|---|---|
| `hud-bg` | `#0F0A1E` | HUD screen background, Countdown background |
| `hud-card` | `#1C1433` | HUD cards/panels |
| `hud-border` | `#2D2050` | HUD card borders |
| `hud-text-dim` | `#7C6FA0` | HUD secondary labels |

---

## 2. Typography

**Font family:** Outfit (weights: 400, 500, 700, 900)
**Fallback:** system-ui, sans-serif

| Style | Size | Weight | Line Height | Usage |
| --- | --- | --- | --- | --- |
| Display | 48px | 900 | 1.0 | Score numbers, big stats |
| Heading | 28px | 900 | 1.1 | Screen titles (TOWERMAZE, TRY AGAIN) |
| Title | 18px | 700 | 1.2 | Card headers, section titles |
| Body | 14px | 500 | 1.4 | Mission descriptions, item names |
| Caption | 11px | 400 | 1.4 | Secondary info, timestamps |
| Label | 10px | 700 | 1.0 | Uppercase category labels (letter-spacing: 2px, text-transform: uppercase) |

---

## 3. Spacing & Radius

### Spacing scale (4px base)
`4 · 8 · 12 · 16 · 20 · 24 · 32 · 48`

### Border radius
| Token | Value | Usage |
|---|---|---|
| `radius-sm` | 10px | Badges, small chips |
| `radius-md` | 18px | Cards, panels, modals |
| `radius-lg` | 24px | Screen containers |
| `radius-pill` | 999px | All buttons |

### Elevation (box-shadow)
| Token | Value | Usage |
|---|---|---|
| `shadow-sm` | `0 2px 8px rgba(0,0,0,0.06)` | Cards at rest |
| `shadow-md` | `0 4px 16px rgba(0,0,0,0.10)` | Modals, overlays |
| `shadow-primary` | `0 4px 16px rgba(124,58,237,0.30)` | Primary action buttons |
| `shadow-success` | `0 4px 16px rgba(16,185,129,0.30)` | Success buttons |
| `shadow-danger` | `0 4px 16px rgba(239,68,68,0.30)` | Danger buttons |

---

## 4. Components

### Buttons
| Variant | Background | Text | Shadow |
|---|---|---|---|
| Primary | `primary` | white | `shadow-primary` |
| Secondary | `card`, border `primary` 2px | `primary` | `shadow-sm` |
| Success | `success` | white | `shadow-success` |
| Danger | `danger` | white | `shadow-danger` |
| Ghost | transparent | `text-dim` | none |

All buttons: `radius-pill`, `font: Outfit 700 15px`, `padding: 12px 28px`.
Small variant: `padding: 7px 16px`, `font-size: 12px`.

### Cards
- Background: `card`
- Border radius: `radius-md` (18px)
- Shadow: `shadow-sm`
- Padding: 16px
- Optional accent: 3px left border in `primary`

### Pill (currency / stat display)
- Background: `card`
- Border radius: `radius-pill`
- Shadow: `shadow-sm`
- Padding: `8px 14px`
- Gap: 6px (icon + text)
- Accent variant: background `primary`, text white

### Progress Bar
- Height: 8px
- Track: `primary-bg`
- Fill: gradient `primary` → `primary-light`
- Border radius: `radius-pill`

### Badge

| Variant | Background | Text color |
|---|---|---|
| Default | `primary-bg` | `primary` |
| Warning | `warning-bg` | `warning-text` |
| Success | `success-bg` | `success-text` |
| Danger | `danger-bg` | `danger-text` |

All badges: `radius-pill`, `font: Outfit 700 11px`, `padding: 4px 10px`.

---

## 5. Screens

### 5.1 Ana Menü (Start Screen)

**Background:** `surface`

Layout (top → bottom):

1. **Header** — "TOWER**MAZE**" (Heading/900, `primary` accent on MAZE) + tagline caption
2. **Best Score Card** — Card with Label + Display score + `primary` accent
3. **Daily Mission Card** — Card with `primary` left border, mission text, progress bar
4. **Daily Challenge Card** — Card with `warning` left border, target height, DAILY RUN button inside
5. **Action buttons** — Primary "OYNA" full-width + Secondary row [SHOP | 🏆 TOP RUNS]
6. **Bottom pills** — Lives pill (left), Ember balance pill (right)

### 5.2 Fail Ekranı (Fail Screen)

**Background:** `surface` with semi-transparent dark overlay

Layout:

1. **Title** — "TRY AGAIN" (Heading/900)
2. **Score Card** — Large display score, "New Best!" badge (Success variant) if applicable
3. **Stats Row** — 2 cards side by side: TIME | ZONE
4. **Reward Card** — Coins earned (with coin icon)
5. **Mission progress** — Updated progress bar if mission active
6. **Buttons (bottom)** — YENİDEN DENE (Primary) + DEVAM ET (Success) + Ana Menü (Ghost)

### 5.3 Oyun HUD

**Background:** `hud-bg`, transparent overlay over 3D gameplay

Elements:

- Top-left: Score card (`hud-card` style, `text-dark` → white)
- Top-right: Best score card (`hud-card` style, `primary-light` text)
- Top-center: Zone badge (Default badge variant, `hud-card` style)
- Bottom: Lava proximity bar (progress bar + Warning badge when close)
- New Best badge: floating center-top, `success-bg`/`success-text`, fades in/out

### 5.4 Shop Ekranı (modal)

**Background:** semi-transparent black overlay (`rgba(0,0,0,0.7)`) + `card` modal

Layout:

1. **Header bar** — "SHOP" (Title/700) + close (✕) Ghost button
2. **Tab bar** — COINS | BALLS | TOWERS; active tab: `primary` background pill, inactive: Ghost
3. **Item grid** — 2-column grid of item cards; Owned: `surface` background + `success` badge; Unowned: `primary-bg` background + price button
4. **Restore purchases** — Ghost button at bottom

### 5.5 Countdown

**Background:** `hud-bg` (full screen, same as HUD dark theme)

- Large Display text (80px), color white, centered
- Number: 3 → 2 → 1
- "GO!" in `primary`

### 5.6 Splash Screen

**Background:** `hud-bg`

- Logo centered: "TOWER**MAZE**" (Display/900, white + `primary-light` on MAZE)
- Spinner: thin ring (2px stroke) in `primary-light`, rotating
- No tagline text

### 5.7 IAP Upsell (modal)

**Background:** semi-transparent black overlay + `card` modal (`radius-lg`)

Layout:

- Close (✕) Ghost button top-right
- Product image frame: `radius-md`, `primary-bg` background, 1:1 ratio
- Title (Title/700) + description (Body/`text-mid`)
- Price button (Primary, full-width)

### 5.8 Reward Toast

- Floating pill at top-center, `radius-md`, `card` background, `shadow-md`
- Icon (24px) + Title (Title/700) + optional subtitle (Caption/`text-dim`)
- Auto-dismiss: 2.5s

### 5.9 Leaderboard Paneli

**Background:** `surface` within Start Screen or modal overlay

Layout:

1. **Header** — "TOP RUNS" (Title/700) + optional close button
2. **Entry rows** (up to 10 visible, scrollable): rank number (Label/`text-dim`), player name (Body/700/`text-dark`), score (Body/700/`primary`); ranks 1–3 get gold/silver/bronze left border
3. **Current player row** — highlighted with `primary-bg` background

### 5.10 Rush Warning

**Background:** full-screen semi-transparent overlay (`warning-bg` tint, ~60% opacity)

Center card (`card`, `radius-md`): warning icon in `warning` color, "⚡ RUSH MODE" (Heading/900/`warning`), subtitle "Speed increases!" (Body/`text-mid`). Auto-dismiss after 1.5s.

### 5.11 Control Flip Warning

**Background:** full-screen semi-transparent overlay (`primary-bg` tint, ~60% opacity)

Center card (`card`, `radius-md`): flip arrows icon in `primary`, "CONTROLS FLIPPED" (Title/700/`primary`), subtitle "Left and right are now reversed" (Caption/`text-dim`). Dismiss button: Primary Small "GOT IT".

---

## 6. Figma File Structure

```text
TowerMaze Design System
├── 🎨 Foundations
│   ├── Colors
│   │   ├── Primary group (primary, primary-light, primary-bg)
│   │   ├── Surface group (surface, card, divider)
│   │   ├── Text group (text-dark, text-mid, text-dim)
│   │   ├── Semantic group (success/*, danger/*, warning/*)
│   │   └── HUD group (hud-bg, hud-card, hud-border, hud-text-dim)
│   ├── Typography (6 text styles: Display, Heading, Title, Body, Caption, Label)
│   ├── Spacing (frame documenting 4px scale: 4–48)
│   └── Shadows (5 effect styles: shadow-sm, shadow-md, shadow-primary, shadow-success, shadow-danger)
├── 🧩 Components
│   ├── Buttons (Primary, Secondary, Success, Danger, Ghost, Small)
│   ├── Cards (Default, Accented-left-border)
│   ├── Pills (Default, Accent)
│   ├── Badges (Default, Warning, Success, Danger)
│   ├── Progress Bar
│   └── Icons (settings, coin, life, trophy, close, clock/timer, zone, ember)
├── 📱 Screens
│   ├── Ana Menü
│   ├── Fail Ekranı
│   ├── Oyun HUD
│   ├── Shop — Coins tab
│   ├── Shop — Balls tab
│   ├── Shop — Towers tab
│   ├── Countdown
│   ├── Splash Screen
│   ├── IAP Upsell
│   ├── Reward Toast
│   ├── Leaderboard Paneli
│   ├── Rush Warning
│   └── Control Flip Warning
└── 📤 Export
    ├── design-tokens.json (Style Dictionary v3 format)
    └── Sprite inventory (see Section 7)
```

---

## 7. Design Tokens Export (Figma → Unity)

Export format: **Style Dictionary v3** (`tokens.json`). Save to `Assets/Resources/TowerMaze/UITheme/design-tokens.json`.

```json
{
  "color": {
    "primary":          { "value": "#7C3AED" },
    "primary-light":    { "value": "#A78BFA" },
    "primary-bg":       { "value": "#EDE9FE" },
    "surface":          { "value": "#F8F7FF" },
    "card":             { "value": "#FFFFFF" },
    "divider":          { "value": "#F0EEFF" },
    "text-dark":        { "value": "#111111" },
    "text-mid":         { "value": "#555555" },
    "text-dim":         { "value": "#AAAAAA" },
    "success":          { "value": "#10B981" },
    "success-bg":       { "value": "#D1FAE5" },
    "success-text":     { "value": "#059669" },
    "danger":           { "value": "#EF4444" },
    "danger-bg":        { "value": "#FEE2E2" },
    "danger-text":      { "value": "#DC2626" },
    "warning":          { "value": "#F59E0B" },
    "warning-bg":       { "value": "#FEF3C7" },
    "warning-text":     { "value": "#D97706" },
    "hud-bg":           { "value": "#0F0A1E" },
    "hud-card":         { "value": "#1C1433" },
    "hud-border":       { "value": "#2D2050" },
    "hud-text-dim":     { "value": "#7C6FA0" }
  },
  "radius": {
    "sm":   { "value": "10" },
    "md":   { "value": "18" },
    "lg":   { "value": "24" },
    "pill": { "value": "999" }
  },
  "shadow": {
    "sm":      { "value": "0 2px 8px rgba(0,0,0,0.06)" },
    "md":      { "value": "0 4px 16px rgba(0,0,0,0.10)" },
    "primary": { "value": "0 4px 16px rgba(124,58,237,0.30)" },
    "success": { "value": "0 4px 16px rgba(16,185,129,0.30)" },
    "danger":  { "value": "0 4px 16px rgba(239,68,68,0.30)" }
  }
}
```

---

## 8. Sprite Inventory (Figma → Unity Export)

All sprites exported as **9-slice PNG @3x** to `Assets/Resources/TowerMaze/UITheme/`.
9-slice margins: **corner radius px × 3** (e.g. radius-md 18px → 54px margin at @3x).

| Filename | Replaces | Radius | States |
| --- | --- | --- | --- |
| `panel_light.png` | `panel_light_hq.png` | `radius-md` | 1 (static) |
| `panel_dark.png` | `panel_dark_hq.png` | `radius-md` | 1 (static) |
| `panel_primary_bg.png` | — | `radius-md` | 1 (static) |
| `panel_hud.png` | — | `radius-md` | 1 (static, dark) |
| `button_primary.png` | `button_blue.png` | `radius-pill` | 1 (Unity tint handles press) |
| `button_success.png` | `button_green.png` | `radius-pill` | 1 |
| `button_danger.png` | `button_grey.png` | `radius-pill` | 1 |
| `button_outline.png` | — | `radius-pill` | 1 (border only, transparent fill) |
| `badge_pill.png` | — | `radius-pill` | 1 (Unity tint for color variants) |
| `progress_track.png` | — | `radius-pill` | 1 |
| `progress_fill.png` | — | `radius-pill` | 1 |

Icon exports (SVG → PNG @3x, 72×72px):
`icon_settings.png`, `icon_coin.png`, `icon_life.png`, `icon_trophy.png`, `icon_close.png`, `icon_clock.png`, `icon_zone.png`, `icon_ember.png`
