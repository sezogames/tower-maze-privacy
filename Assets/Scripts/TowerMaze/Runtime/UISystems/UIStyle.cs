// Assets/Scripts/TowerMaze/Runtime/UISystems/UIStyle.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Centralized design token system and animation helpers for TowerMaze UI.
/// All hex values and alpha values are static readonly — no inline hex strings elsewhere.
/// </summary>
public static class UIStyle
{
    // ─── Color Tokens ───────────────────────────────────────────────────────
    public static readonly Color Brand        = Hex("#7C4DFF"); // identity / tabs / selected
    public static readonly Color MenuBg       = Hex("#2D1B69"); // main menu background
    public static readonly Color ShopBg       = Hex("#1A0A35"); // shop + skins background
    public static readonly Color HudBg        = Hex("#0F0A1E"); // in-game HUD background
    public static readonly Color FailBg       = Hex("#1A0F2E"); // game over background
    public static readonly Color Action       = Hex("#FF9F0A"); // START / CONTINUE / buy
    public static readonly Color ActionLight  = Hex("#FFB340"); // buy button gradient end
    public static readonly Color Gold         = Hex("#FFD60A"); // coins / BEST VALUE badge
    public static readonly Color Owned        = Hex("#10B981"); // owned badge / claim button
    public static readonly Color Danger       = Hex("#EF4444"); // lava distance indicator

    // Transparent variants (cannot be readonly because Color struct with alpha)
    public static Color SurfaceDark  => new Color(1f, 1f, 1f, 0.07f); // cards on dark bg
    public static Color SurfaceDark2 => new Color(1f, 1f, 1f, 0.04f); // locked / secondary
    public static Color BorderDark   => new Color(1f, 1f, 1f, 0.06f); // card borders
    public static Color TextPrimary  => Color.white;
    public static Color TextDim      => new Color(1f, 1f, 1f, 0.35f); // secondary text
    public static Color TextFaint    => new Color(1f, 1f, 1f, 0.30f); // captions / labels

    // ─── Metrics (pixels, Unity units match 1:1 at 1080p reference) ─────────
    public const int RadiusLg   = 16;
    public const int RadiusMd   = 14;
    public const int RadiusSm   = 10;
    public const int RadiusPill = 999;
    public const int PadH       = 14;   // horizontal screen padding
    public const int PadV       = 18;   // vertical screen padding
    public const int GridGap    = 8;    // skins grid gap
    public const int BtnHeightPrimary  = 48;
    public const int BtnHeightStart    = 56;
    public const int BtnHeightContinue = 60;

    // ─── Hex Parser ──────────────────────────────────────────────────────────
    private static Color Hex(string hex)
    {
        if (!ColorUtility.TryParseHtmlString(hex, out var c))
            Debug.LogError($"[UIStyle] Failed to parse hex color: '{hex}'");
        return c;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // COROUTINE ANIMATIONS
    // All coroutines are static — callers must StartCoroutine on a MonoBehaviour.
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Universal button press: scale 1→0.95 (0.08s) then →1 (0.12s).
    /// </summary>
    public static IEnumerator ButtonPress(RectTransform rt)
    {
        var original = rt.localScale;
        var pressed  = original * 0.95f;
        float t = 0f;
        while (t < 0.08f) { rt.localScale = Vector3.Lerp(original, pressed, t / 0.08f); t += Time.deltaTime; yield return null; }
        rt.localScale = pressed;
        t = 0f;
        while (t < 0.12f) { rt.localScale = Vector3.Lerp(pressed, original, t / 0.12f); t += Time.deltaTime; yield return null; }
        rt.localScale = original;
    }

    /// <summary>
    /// Buy button tap expand: scale 1→1.05 (0.08s) then press 1.05→0.95 (0.06s) then →1 (0.10s).
    /// </summary>
    public static IEnumerator BuyButtonTap(RectTransform rt)
    {
        var orig = rt.localScale;
        var big  = orig * 1.05f;
        var sm   = orig * 0.95f;
        float t = 0f;
        while (t < 0.08f) { rt.localScale = Vector3.Lerp(orig, big, t / 0.08f); t += Time.deltaTime; yield return null; }
        t = 0f;
        while (t < 0.06f) { rt.localScale = Vector3.Lerp(big, sm, t / 0.06f);  t += Time.deltaTime; yield return null; }
        t = 0f;
        while (t < 0.10f) { rt.localScale = Vector3.Lerp(sm, orig, t / 0.10f); t += Time.deltaTime; yield return null; }
        rt.localScale = orig;
    }

    /// <summary>
    /// Idle pulse loop for START/CONTINUE buttons (scale 1↔1.05, period 1.4s).
    /// Runs forever — stop by destroying the coroutine or the GameObject.
    /// </summary>
    public static IEnumerator Pulse(RectTransform rt, float min = 1f, float max = 1.05f, float period = 1.4f)
    {
        float half = period / 2f;
        while (true)
        {
            float t = 0f;
            while (t < half) { float s = Mathf.Lerp(min, max, Mathf.SmoothStep(0, 1, t / half)); rt.localScale = new Vector3(s, s, 1); t += Time.deltaTime; yield return null; }
            t = 0f;
            while (t < half) { float s = Mathf.Lerp(max, min, Mathf.SmoothStep(0, 1, t / half)); rt.localScale = new Vector3(s, s, 1); t += Time.deltaTime; yield return null; }
        }
    }

    /// <summary>
    /// Looping vertical bounce (BEST VALUE badge, amplitude 5px, period 1.3s).
    /// </summary>
    public static IEnumerator Bounce(RectTransform rt, float amplitude = 5f, float period = 1.3f)
    {
        var origin = rt.anchoredPosition;
        while (true)
        {
            if (rt == null) yield break;
            float t = 0f, phase = period * 0.45f;
            while (t < phase) 
            { 
                if (rt == null) yield break;
                rt.anchoredPosition = new Vector2(origin.x, origin.y - amplitude * Mathf.SmoothStep(0, 1, t / phase)); 
                t += Time.deltaTime; 
                yield return null; 
            }
            t = 0f; phase = period * 0.55f;
            while (t < phase) 
            { 
                if (rt == null) yield break;
                rt.anchoredPosition = new Vector2(origin.x, (origin.y - amplitude) + amplitude * Mathf.SmoothStep(0, 1, t / phase)); 
                t += Time.deltaTime; 
                yield return null; 
            }
        }
    }

    /// <summary>
    /// Looping alpha glow pulse (BEST VALUE card glow image, 1.8s period).
    /// </summary>
    public static IEnumerator GlowPulse(Image glowImg, Color baseColor, float minA = 0.3f, float maxA = 0.7f, float period = 1.8f)
    {
        float half = period / 2f;
        while (true)
        {
            if (glowImg == null) yield break;
            float t = 0f;
            while (t < half) 
            { 
                if (glowImg == null) yield break;
                glowImg.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Lerp(minA, maxA, Mathf.SmoothStep(0, 1, t / half))); 
                t += Time.deltaTime; yield return null; 
            }
            t = 0f;
            while (t < half) 
            { 
                if (glowImg == null) yield break;
                glowImg.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Lerp(maxA, minA, Mathf.SmoothStep(0, 1, t / half))); 
                t += Time.deltaTime; yield return null; 
            }
        }
    }

    /// <summary>
    /// Screen / panel fade in (CanvasGroup alpha 0→1, default 0.25s).
    /// </summary>
    public static IEnumerator FadeIn(CanvasGroup cg, float duration = 0.25f)
    {
        if (cg == null) yield break;
        cg.alpha = 0f;
        float t = 0f;
        while (t < duration)
        {
            if (cg == null) yield break;
            t += Time.deltaTime;
            cg.alpha = Mathf.SmoothStep(0, 1, t / duration);
            yield return null;
        }
        if (cg != null) cg.alpha = 1f;
    }

    /// <summary>
    /// Screen / panel fade out (CanvasGroup alpha →0, default 0.20s).
    /// </summary>
    public static IEnumerator FadeOut(CanvasGroup cg, float duration = 0.20f)
    {
        float start = cg.alpha, t = 0f;
        while (t < duration) { cg.alpha = Mathf.Lerp(start, 0, t / duration); t += Time.deltaTime; yield return null; }
        cg.alpha = 0f;
    }

    /// <summary>
    /// Bottom sheet open: slide up from below AND fade in simultaneously (spec §5 "Popup Open").
    /// Requires a CanvasGroup on the sheet's root GameObject.
    /// </summary>
    public static IEnumerator SlideUp(RectTransform rt, float sheetHeight, float duration = 0.25f)
    {
        var cg = rt.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>();
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -sheetHeight);
        cg.alpha = 0f;
        float t = 0f;
        while (t < duration)
        {
            float p = Mathf.SmoothStep(0, 1, t / duration);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, Mathf.Lerp(-sheetHeight, 0, p));
            cg.alpha = p;
            t += Time.deltaTime;
            yield return null;
        }
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 0);
        cg.alpha = 1f;
    }

    /// <summary>
    /// Bottom sheet close: slide down AND fade out simultaneously (spec §5 "Popup Close").
    /// </summary>
    public static IEnumerator SlideDown(RectTransform rt, float sheetHeight, float duration = 0.20f)
    {
        var cg = rt.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>();
        float t = 0f;
        while (t < duration)
        {
            float p = t / duration;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, Mathf.Lerp(0, -sheetHeight, p));
            cg.alpha = 1f - p;
            t += Time.deltaTime;
            yield return null;
        }
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -sheetHeight);
        cg.alpha = 0f;
    }

    /// <summary>
    /// Score pop entry: scale 0→1.15 (0.28s ease-out) then 1.15→1 (0.12s ease-in).
    /// </summary>
    public static IEnumerator ScorePop(RectTransform rt)
    {
        rt.localScale = Vector3.zero;
        float t = 0f;
        while (t < 0.28f) { float s = Mathf.Lerp(0f, 1.15f, 1f - Mathf.Pow(1f - t / 0.28f, 3f)); rt.localScale = new Vector3(s, s, 1); t += Time.deltaTime; yield return null; }
        rt.localScale = new Vector3(1.15f, 1.15f, 1);
        t = 0f;
        while (t < 0.12f) { float s = Mathf.Lerp(1.15f, 1f, Mathf.Pow(t / 0.12f, 2f)); rt.localScale = new Vector3(s, s, 1); t += Time.deltaTime; yield return null; }
        rt.localScale = Vector3.one;
    }

    /// <summary>
    /// Coin gain float: spawns "+X 🪙" text, floats up 44px, scale 1→1.2→1, fades out. Auto-destroys.
    /// </summary>
    public static IEnumerator CoinFloat(Text floatText)
    {
        var rt = floatText.rectTransform;
        var startPos = rt.anchoredPosition;
        var baseColor = floatText.color;
        float elapsed = 0f, dur = 1.2f;
        while (elapsed < dur)
        {
            float p = elapsed / dur;
            float yOff = Mathf.Lerp(0, -44f, p);
            float scale = p < 0.25f ? Mathf.Lerp(1f, 1.2f, p / 0.25f) : Mathf.Lerp(1.2f, 1f, (p - 0.25f) / 0.75f);
            float alpha = p < 0.30f ? 1f : Mathf.Lerp(1f, 0f, (p - 0.30f) / 0.70f);
            rt.anchoredPosition = new Vector2(startPos.x, startPos.y - yOff);
            rt.localScale = new Vector3(scale, scale, 1);
            floatText.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Object.Destroy(floatText.gameObject);
    }

    /// <summary>
    /// One-shot background danger pulse (fade in 0.3s, hold 0.5s, fade out 0.4s).
    /// overlay.color.a should be 0 before calling.
    /// </summary>
    public static IEnumerator BackgroundPulse(Image overlay, Color pulseColor)
    {
        float t = 0f;
        while (t < 0.3f) { overlay.color = new Color(pulseColor.r, pulseColor.g, pulseColor.b, Mathf.Lerp(0, pulseColor.a, t / 0.3f)); t += Time.deltaTime; yield return null; }
        overlay.color = pulseColor;
        yield return new WaitForSeconds(0.5f);
        t = 0f;
        while (t < 0.4f) { overlay.color = new Color(pulseColor.r, pulseColor.g, pulseColor.b, Mathf.Lerp(pulseColor.a, 0, t / 0.4f)); t += Time.deltaTime; yield return null; }
        overlay.color = new Color(pulseColor.r, pulseColor.g, pulseColor.b, 0);
    }

    /// <summary>
    /// Tab content swap with fade: fade out 0.1s, call swap(), fade in 0.15s.
    /// </summary>
    public static IEnumerator TabSwitch(CanvasGroup cg, System.Action swap)
    {
        float t = 0f;
        float startA = cg.alpha;
        while (t < 0.10f) { cg.alpha = Mathf.Lerp(startA, 0, t / 0.10f); t += Time.deltaTime; yield return null; }
        cg.alpha = 0f;
        swap();
        t = 0f;
        while (t < 0.15f) { cg.alpha = Mathf.Lerp(0, 1, t / 0.15f); t += Time.deltaTime; yield return null; }
        cg.alpha = 1f;
    }

    /// <summary>
    /// Settings panel slide: anchoredPositionX fromX→toX over duration.
    /// </summary>
    public static IEnumerator SlideX(RectTransform rt, float fromX, float toX, float duration = 0.25f)
    {
        float t = 0f;
        while (t < duration)
        {
            float p = Mathf.SmoothStep(0, 1, t / duration);
            rt.anchoredPosition = new Vector2(Mathf.Lerp(fromX, toX, p), rt.anchoredPosition.y);
            t += Time.deltaTime;
            yield return null;
        }
        rt.anchoredPosition = new Vector2(toX, rt.anchoredPosition.y);
    }

    /// <summary>
    /// Creates a "Glow" child Image behind target, stretched by expand px on each side.
    /// Returns the Image so caller can animate it with GlowPulse.
    /// </summary>
    public static Image CreateGlow(RectTransform target, Color glowColor, float expand = 16f)
    {
        var go = new GameObject("Glow");
        go.transform.SetParent(target.parent, false);
        go.transform.SetSiblingIndex(target.GetSiblingIndex()); // behind target
        var img = go.AddComponent<Image>();
        img.color = glowColor;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = target.anchorMin;
        rt.anchorMax = target.anchorMax;
        rt.offsetMin = target.offsetMin - new Vector2(expand, expand);
        rt.offsetMax = target.offsetMax + new Vector2(expand, expand);
        return img;
    }
}

/// <summary>
/// Vertical linear gradient for UGUI. Use instead of Image when a top-to-bottom
/// color gradient is needed (orange START button, BEST VALUE card, CONTINUE button).
/// Rounded corners are achieved by placing inside a masked RectTransform.
/// </summary>
[UnityEngine.RequireComponent(typeof(CanvasRenderer))]
public sealed class GradientImage : UnityEngine.UI.MaskableGraphic
{
    public Color colorTop    = Color.white;
    public Color colorBottom = Color.white;

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
    {
        vh.Clear();
        var r = GetPixelAdjustedRect();
        // BL, BR, TR, TL — bottom color at y=min, top color at y=max
        vh.AddVert(new Vector3(r.xMin, r.yMin), colorBottom, Vector2.zero);
        vh.AddVert(new Vector3(r.xMax, r.yMin), colorBottom, Vector2.zero);
        vh.AddVert(new Vector3(r.xMax, r.yMax), colorTop,    Vector2.zero);
        vh.AddVert(new Vector3(r.xMin, r.yMax), colorTop,    Vector2.zero);
        vh.AddTriangle(0, 2, 1);
        vh.AddTriangle(0, 3, 2);
    }
}
