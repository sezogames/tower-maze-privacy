using System.Collections;
using System.Collections.Generic;
using TowerMaze.WeatherSystem;
using UnityEngine;

namespace TowerMaze.BackgroundSystem
{
    /// <summary>
    /// Owns the BackgroundRoot hierarchy and cross-fades the mid-atmosphere /
    /// overlay layers when the active weather changes. The renderer fields are
    /// assigned by the Editor setup tool but can be overridden in the Inspector
    /// at any time. Designed so a single coroutine drives all layer alphas — if
    /// SmoothTransitionBackground is called twice in quick succession, the older
    /// coroutine is cancelled so we never end up cross-fading three layers at
    /// once.
    /// </summary>
    public sealed class BackgroundManager : MonoBehaviour
    {
        [Header("Active state")]
        [SerializeField] private WeatherType currentWeather = WeatherType.Sunny;

        [Header("Camera + root")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform backgroundRoot;

        [Header("Layer renderers")]
        [SerializeField] private Renderer backGradientRenderer;
        [SerializeField] private Renderer midAtmosphereA;
        [SerializeField] private Renderer midAtmosphereB;
        [SerializeField] private Renderer farCloudsRenderer;
        [SerializeField] private Renderer weatherOverlayA;
        [SerializeField] private Renderer weatherOverlayB;

        [Header("Material library (resolved by enum index)")]
        [Tooltip("Background gradient materials, one per WeatherType in enum order.")]
        [SerializeField] private Material[] backgroundMaterials = new Material[5];
        [Tooltip("Optional overlay materials per weather (Sunny entry can be null).")]
        [SerializeField] private Material[] overlayMaterials = new Material[5];

        [Header("Transitions")]
        [SerializeField] private float transitionDuration = 1.5f;
        [SerializeField] private bool enableParallax = true;
        [SerializeField] private float farCloudParallaxSpeed = 0.015f;

        public WeatherType CurrentWeather => currentWeather;

        private Coroutine activeTransition;
        private bool useALayer = true; // tracks which mid/overlay slot currently holds the live texture

        private void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
        }

        private void Start()
        {
            FitBackgroundToCamera();
            ApplyImmediate(currentWeather);
        }

        private void LateUpdate()
        {
            // Keep the backdrop locked behind the camera. LateUpdate so the
            // camera follow controllers have finished moving for this frame.
            if (targetCamera != null && backgroundRoot != null)
            {
                Vector3 cam = targetCamera.transform.position;
                Vector3 forward = targetCamera.transform.forward;
                float distance = targetCamera.orthographic ? 5f : 10f;
                backgroundRoot.position = cam + forward * distance;
                backgroundRoot.rotation = targetCamera.transform.rotation;
            }
            UpdateParallax();
        }

        public void SetBackground(WeatherType weather)
        {
            currentWeather = weather;
            ApplyImmediate(weather);
        }

        public void SmoothTransitionBackground(WeatherType weather)
        {
            currentWeather = weather;
            if (activeTransition != null) StopCoroutine(activeTransition);
            activeTransition = StartCoroutine(TransitionRoutine(weather));
        }

        public static void SetLayerAlpha(Renderer renderer, float alpha)
        {
            if (renderer == null) return;
            Material mat = renderer.material;
            if (mat == null) return;
            if (mat.HasProperty("_BaseColor"))
            {
                Color c = mat.GetColor("_BaseColor");
                c.a = Mathf.Clamp01(alpha);
                mat.SetColor("_BaseColor", c);
            }
            else if (mat.HasProperty("_Color"))
            {
                Color c = mat.color;
                c.a = Mathf.Clamp01(alpha);
                mat.color = c;
            }
        }

        public void ApplyThemeColors(WeatherType weather)
        {
            // Renderers reuse the same material slot but the colors are
            // managed by the material itself — overrides happen in SetMaterial.
            // Subclasses or designers can override per-theme tint here later.
        }

        public void FitBackgroundToCamera()
        {
            if (targetCamera == null || backgroundRoot == null) return;
            float aspect = targetCamera.aspect;
            float height;
            float width;
            float distance;

            if (targetCamera.orthographic)
            {
                distance = 5f;
                height = targetCamera.orthographicSize * 2f;
                width = height * aspect;
            }
            else
            {
                distance = 10f;
                height = 2f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                width = height * aspect;
            }

            // Cover (not stretch) — pick the larger axis so the texture wraps the screen.
            const float TexAspect = 1080f / 1920f; // portrait source
            float coverWidth = width;
            float coverHeight = height;
            if (width / height > TexAspect)
            {
                coverHeight = width / TexAspect;
            }
            else
            {
                coverWidth = height * TexAspect;
            }

            Vector3 scale = new Vector3(coverWidth, coverHeight, 1f);
            ApplyScaleRecursive(backgroundRoot, scale);
            // Distance is anchored in LateUpdate by repositioning the root in
            // front of the camera; nothing to do here for Z.
        }

        public void UpdateParallax()
        {
            if (!enableParallax || farCloudsRenderer == null) return;
            // Soft horizontal scroll on the far cloud material — tied to enum
            // so each theme can dial its own intensity through the multiplier.
            Material mat = farCloudsRenderer.sharedMaterial;
            if (mat == null) return;
            float multiplier = currentWeather switch
            {
                WeatherType.Sunny => 0.4f,
                WeatherType.Snow => 1.0f,
                WeatherType.Rain => 1.2f,
                WeatherType.Fog => 0.8f,
                WeatherType.StarryNight => 0.2f,
                _ => 0.5f,
            };
            float u = Time.time * farCloudParallaxSpeed * multiplier;
            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTextureOffset("_BaseMap", new Vector2(u, 0f));
            }
            else if (mat.HasProperty("_MainTex"))
            {
                mat.SetTextureOffset("_MainTex", new Vector2(u, 0f));
            }
        }

        private void ApplyImmediate(WeatherType weather)
        {
            int index = (int)weather;
            Material bg = SafeAt(backgroundMaterials, index);
            Material overlay = SafeAt(overlayMaterials, index);

            if (backGradientRenderer != null && bg != null) backGradientRenderer.sharedMaterial = bg;
            if (midAtmosphereA != null && bg != null) midAtmosphereA.sharedMaterial = bg;
            if (midAtmosphereB != null) midAtmosphereB.sharedMaterial = bg;
            if (weatherOverlayA != null) weatherOverlayA.sharedMaterial = overlay;
            if (weatherOverlayB != null) weatherOverlayB.sharedMaterial = overlay;

            SetLayerAlpha(midAtmosphereA, 1f);
            SetLayerAlpha(midAtmosphereB, 0f);
            SetLayerAlpha(weatherOverlayA, overlay != null ? 1f : 0f);
            SetLayerAlpha(weatherOverlayB, 0f);
            useALayer = true;
            ApplyThemeColors(weather);
        }

        private IEnumerator TransitionRoutine(WeatherType weather)
        {
            int index = (int)weather;
            Material bg = SafeAt(backgroundMaterials, index);
            Material overlay = SafeAt(overlayMaterials, index);

            // Pick the off-screen slot to hold the new material so we can fade in.
            Renderer incomingMid = useALayer ? midAtmosphereB : midAtmosphereA;
            Renderer outgoingMid = useALayer ? midAtmosphereA : midAtmosphereB;
            Renderer incomingOverlay = useALayer ? weatherOverlayB : weatherOverlayA;
            Renderer outgoingOverlay = useALayer ? weatherOverlayA : weatherOverlayB;

            if (incomingMid != null && bg != null) incomingMid.sharedMaterial = new Material(bg);
            if (incomingOverlay != null) incomingOverlay.sharedMaterial = overlay != null ? new Material(overlay) : null;
            SetLayerAlpha(incomingMid, 0f);
            SetLayerAlpha(incomingOverlay, 0f);

            float t = 0f;
            while (t < transitionDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / transitionDuration);
                SetLayerAlpha(incomingMid, k);
                SetLayerAlpha(outgoingMid, 1f - k);
                SetLayerAlpha(incomingOverlay, overlay != null ? k : 0f);
                SetLayerAlpha(outgoingOverlay, 1f - k);
                yield return null;
            }

            // Swap roles so the next transition uses the now-vacant slot.
            useALayer = !useALayer;
            // Snap the gradient renderer to the latest material for solid backdrops.
            if (backGradientRenderer != null && bg != null) backGradientRenderer.sharedMaterial = bg;
            ApplyThemeColors(weather);
            activeTransition = null;
        }

        private static Material SafeAt(IReadOnlyList<Material> list, int index)
        {
            if (list == null || index < 0 || index >= list.Count) return null;
            return list[index];
        }

        private static void ApplyScaleRecursive(Transform root, Vector3 scale)
        {
            int count = root.childCount;
            for (int i = 0; i < count; i++)
            {
                Transform child = root.GetChild(i);
                child.localScale = scale;
            }
        }
    }
}
