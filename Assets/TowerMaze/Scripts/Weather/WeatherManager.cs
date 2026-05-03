using System.Collections;
using TowerMaze.BackgroundSystem;
using UnityEngine;

namespace TowerMaze.WeatherSystem
{
    /// <summary>
    /// Top-level weather coordinator. Routes zone changes to a theme, swaps the
    /// background via BackgroundManager, toggles the matching particle effect,
    /// drives directional light intensity + RenderSettings.fog, and (optionally)
    /// swaps the tower block material for wet/icy variants.
    /// Use SetManualWeatherSelection(true) so the shop / settings choice locks
    /// the theme regardless of zone progression.
    /// </summary>
    public sealed class WeatherManager : MonoBehaviour
    {
        [Header("Active state")]
        [SerializeField] private WeatherType currentWeather = WeatherType.Sunny;
        [SerializeField] private bool useManualWeatherSelection;

        [Header("References")]
        [SerializeField] private BackgroundManager backgroundManager;
        [SerializeField] private GameObject snowEffect;
        [SerializeField] private GameObject rainEffect;
        [SerializeField] private GameObject fogEffect;
        [SerializeField] private GameObject starsEffect;
        [SerializeField] private Light directionalLight;

        [Header("Block surface override")]
        [SerializeField] private Renderer[] towerBlockRenderers;
        [SerializeField] private Material defaultBlockMaterial;
        [SerializeField] private Material wetBlockMaterial;
        [SerializeField] private Material icyBlockMaterial;

        [Header("Transition speeds (seconds)")]
        [SerializeField] private float lightTransitionDuration = 1.5f;
        [SerializeField] private float fogTransitionDuration = 1.5f;

        public WeatherType CurrentWeather => currentWeather;
        public bool IsManualSelection => useManualWeatherSelection;

        private Coroutine lightLerp;
        private Coroutine fogLerp;
        private bool initialized;

        private void Start()
        {
            ApplyImmediate(currentWeather);
            initialized = true;
        }

        private void Update()
        {
#if UNITY_EDITOR
            // Editor-only debug keys — never compiled into the mobile build.
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetWeather(WeatherType.Sunny);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SetWeather(WeatherType.Snow);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) SetWeather(WeatherType.Rain);
            else if (Input.GetKeyDown(KeyCode.Alpha4)) SetWeather(WeatherType.Fog);
            else if (Input.GetKeyDown(KeyCode.Alpha5)) SetWeather(WeatherType.StarryNight);
#endif
        }

        public void SetWeather(WeatherType weather)
        {
            if (initialized && weather == currentWeather) return;
            currentWeather = weather;

            if (backgroundManager != null)
            {
                if (initialized) backgroundManager.SmoothTransitionBackground(weather);
                else backgroundManager.SetBackground(weather);
            }

            ApplyWeatherEffects(weather);
            ApplyWeatherLighting(weather);
            ApplyBlockMaterial(weather);
        }

        public void UpdateWeatherByZone(int zone)
        {
            if (useManualWeatherSelection) return;
            SetWeather(GetWeatherForZone(zone));
        }

        public void SetManualWeatherSelection(bool value) => useManualWeatherSelection = value;

        public WeatherType GetWeatherForZone(int zone)
        {
            // Zones are 1-indexed by gameplay convention; clamp at the top so
            // very deep climbs stay on the night theme rather than wrapping.
            if (zone <= 2) return WeatherType.Sunny;
            if (zone <= 5) return WeatherType.Snow;
            if (zone <= 8) return WeatherType.Rain;
            if (zone <= 11) return WeatherType.Fog;
            return WeatherType.StarryNight;
        }

        public void ApplyWeatherEffects(WeatherType weather)
        {
            // Single-active rule: only the matching effect is enabled at any
            // moment. ParticleSystems on the others stop and clear so we don't
            // bleed snow into a sunny scene during a transition.
            SetEffectActive(snowEffect, weather == WeatherType.Snow);
            SetEffectActive(rainEffect, weather == WeatherType.Rain);
            SetEffectActive(fogEffect, weather == WeatherType.Fog);
            SetEffectActive(starsEffect, weather == WeatherType.StarryNight);
        }

        public void ApplyWeatherLighting(WeatherType weather)
        {
            float targetIntensity = weather switch
            {
                WeatherType.Sunny => 1.10f,
                WeatherType.Snow => 0.90f,
                WeatherType.Rain => 0.55f,
                WeatherType.Fog => 0.65f,
                WeatherType.StarryNight => 0.25f,
                _ => 1f,
            };

            // Fog config per theme. Sunny + StarryNight skip fog entirely.
            bool fogEnabled = weather is WeatherType.Snow or WeatherType.Rain or WeatherType.Fog;
            Color fogColor = weather switch
            {
                WeatherType.Snow => new Color(0.78f, 0.86f, 0.95f, 1f),
                WeatherType.Rain => new Color(0.42f, 0.48f, 0.58f, 1f),
                WeatherType.Fog => new Color(0.62f, 0.66f, 0.72f, 1f),
                _ => RenderSettings.fogColor,
            };
            float targetDensity = weather switch
            {
                WeatherType.Snow => 0.020f,
                WeatherType.Rain => 0.015f,
                WeatherType.Fog => 0.045f,
                _ => 0.0f,
            };

            if (lightLerp != null) StopCoroutine(lightLerp);
            if (fogLerp != null) StopCoroutine(fogLerp);

            if (directionalLight != null && initialized)
            {
                lightLerp = StartCoroutine(LerpLightIntensity(directionalLight, targetIntensity, lightTransitionDuration));
            }
            else if (directionalLight != null)
            {
                directionalLight.intensity = targetIntensity;
            }

            RenderSettings.fog = fogEnabled;
            if (fogEnabled) RenderSettings.fogColor = fogColor;
            if (initialized)
            {
                fogLerp = StartCoroutine(LerpFogDensity(targetDensity, fogTransitionDuration));
            }
            else
            {
                RenderSettings.fogDensity = targetDensity;
            }
        }

        public void ApplyBlockMaterial(WeatherType weather)
        {
            if (towerBlockRenderers == null || towerBlockRenderers.Length == 0) return;
            Material target = weather switch
            {
                WeatherType.Rain when wetBlockMaterial != null => wetBlockMaterial,
                WeatherType.Snow when icyBlockMaterial != null => icyBlockMaterial,
                _ => defaultBlockMaterial,
            };
            if (target == null) return;
            for (int i = 0; i < towerBlockRenderers.Length; i++)
            {
                Renderer r = towerBlockRenderers[i];
                if (r != null) r.sharedMaterial = target;
            }
        }

        private void ApplyImmediate(WeatherType weather)
        {
            currentWeather = weather;
            if (backgroundManager != null) backgroundManager.SetBackground(weather);
            ApplyWeatherEffects(weather);
            ApplyWeatherLighting(weather);
            ApplyBlockMaterial(weather);
        }

        private static void SetEffectActive(GameObject go, bool active)
        {
            if (go == null) return;
            if (go.activeSelf == active) return;
            go.SetActive(active);
            if (active)
            {
                ParticleSystem ps = go.GetComponentInChildren<ParticleSystem>();
                if (ps != null)
                {
                    ps.Clear(true);
                    ps.Play(true);
                }
            }
        }

        private static IEnumerator LerpLightIntensity(Light light, float target, float duration)
        {
            float start = light.intensity;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                light.intensity = Mathf.Lerp(start, target, k);
                yield return null;
            }
            light.intensity = target;
        }

        private static IEnumerator LerpFogDensity(float target, float duration)
        {
            float start = RenderSettings.fogDensity;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                RenderSettings.fogDensity = Mathf.Lerp(start, target, k);
                yield return null;
            }
            RenderSettings.fogDensity = target;
        }
    }
}
