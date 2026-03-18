using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TowerMaze
{
    [DefaultExecutionOrder(150)]
    public sealed class EnvironmentBackdropController : MonoBehaviour
    {
        private const string SkyboxResourcePath = "TowerMaze/Skybox/aristea_wreck_puresky";

        private struct FloatingRuinMotion
        {
            public Transform transform;
            public Vector3 baseLocalPosition;
            public Vector3 baseEulerAngles;
            public float bobAmplitude;
            public float bobSpeed;
            public float rotationSpeed;
            public float phase;
        }

        [SerializeField] private float verticalFollowFactor = 0.28f;
        [SerializeField] private float backdropDistance = 110f;
        [SerializeField] private Vector2 backdropSize = new(220f, 170f);
        [SerializeField] private float glowDistance = 92f;
        [SerializeField] private Vector2 glowSize = new(132f, 84f);
        [SerializeField] private float ruinRingRadius = 44f;
        [SerializeField] private int ruinCount = 18;
        [SerializeField] private int floatingRuinCount = 7;

        private readonly List<FloatingRuinMotion> floatingRuins = new();

        private Camera sceneCamera;
        private Transform followTarget;
        private ThemeDefinition theme;
        private Transform backdropQuad;
        private Transform glowQuad;
        private Transform ruinsRoot;
        private Transform floatingRuinsRoot;
        private ParticleSystem emberParticles;
        private Material backdropMaterial;
        private Material glowMaterial;
        private Material ruinMaterial;
        private Material ruinAccentMaterial;
        private Material emberMaterial;
        private float smoothedFollowY;
        private bool built;

        private static Texture2D backdropTexture;
        private static Texture2D glowTexture;
        private static Texture2D emberTexture;
        private static Texture2D panoramicSkyTexture;
        private static Material panoramicSkyMaterial;

        public void Initialize(ThemeDefinition definition, Camera targetCamera, Transform heightTarget)
        {
            theme = definition;
            sceneCamera = targetCamera;
            followTarget = heightTarget;
            smoothedFollowY = followTarget != null ? followTarget.position.y * verticalFollowFactor : 0f;

            if (TryApplyDownloadedSkybox())
            {
                built = true;
                DestroyExistingChildren();
                SnapToCurrentView();
                return;
            }

            if (!built)
            {
                BuildBackdrop();
            }

            SnapToCurrentView();
        }


        public void SetStaticMode(bool active)
        {
            if (ruinsRoot != null) ruinsRoot.gameObject.SetActive(!active);
            if (floatingRuinsRoot != null) floatingRuinsRoot.gameObject.SetActive(!active);
            if (emberParticles != null)
            {
                if (active) emberParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                else emberParticles.Play();
                emberParticles.gameObject.SetActive(!active);
            }
            if (glowQuad != null) glowQuad.gameObject.SetActive(!active);
        }


        private bool TryApplyDownloadedSkybox()
        {
            panoramicSkyTexture ??= Resources.Load<Texture2D>(SkyboxResourcePath);
            if (panoramicSkyTexture == null)
            {
                return false;
            }

            if (panoramicSkyMaterial == null)
            {
                Shader shader = Shader.Find("Skybox/Panoramic");
                if (shader == null)
                {
                    return false;
                }

                panoramicSkyMaterial = new Material(shader)
                {
                    name = "TowerMaze_DownloadedSkybox",
                };

                if (panoramicSkyMaterial.HasProperty("_MainTex"))
                {
                    panoramicSkyMaterial.SetTexture("_MainTex", panoramicSkyTexture);
                }

                if (panoramicSkyMaterial.HasProperty("_Mapping"))
                {
                    panoramicSkyMaterial.SetFloat("_Mapping", 1f);
                }

                if (panoramicSkyMaterial.HasProperty("_ImageType"))
                {
                    panoramicSkyMaterial.SetFloat("_ImageType", 0f);
                }

                if (panoramicSkyMaterial.HasProperty("_Exposure"))
                {
                    panoramicSkyMaterial.SetFloat("_Exposure", 1.18f);
                }

                if (panoramicSkyMaterial.HasProperty("_Rotation"))
                {
                    panoramicSkyMaterial.SetFloat("_Rotation", 0f);
                }

                if (panoramicSkyMaterial.HasProperty("_Tint"))
                {
                    panoramicSkyMaterial.SetColor("_Tint", Color.white);
                }
            }

            RenderSettings.skybox = panoramicSkyMaterial;
            DynamicGI.UpdateEnvironment();

            if (sceneCamera != null)
            {
                sceneCamera.clearFlags = CameraClearFlags.Skybox;
            }

            return true;
        }

        public static Color GetSkyColor(ThemeDefinition definition)
        {
            Color source = definition != null ? definition.skyColor : new Color(0.56f, 0.78f, 0.99f, 1f);
            Color baseSky = new Color(0.56f, 0.78f, 0.99f, 1f);
            Color softTint = new(
                Mathf.Lerp(0.56f, 0.74f, source.r * 0.35f),
                Mathf.Lerp(0.78f, 0.9f, source.g * 0.3f),
                0.99f,
                1f);
            return Color.Lerp(baseSky, softTint, 0.35f);
        }

        public static Color GetFogColor(ThemeDefinition definition)
        {
            Color source = definition != null ? definition.fogColor : new Color(0.8f, 0.88f, 0.98f, 1f);
            Color baseFog = new Color(0.8f, 0.88f, 0.98f, 1f);
            Color softTint = new(
                Mathf.Lerp(0.8f, 0.9f, source.r * 0.2f),
                Mathf.Lerp(0.88f, 0.95f, source.g * 0.2f),
                0.98f,
                1f);
            return Color.Lerp(baseFog, softTint, 0.3f);
        }

        private void LateUpdate()
        {
            if (sceneCamera == null)
            {
                return;
            }

            float targetFollowY = followTarget != null ? followTarget.position.y * verticalFollowFactor : 0f;
            smoothedFollowY = Mathf.Lerp(smoothedFollowY, targetFollowY, 1f - Mathf.Exp(-Time.deltaTime * 2.25f));

            if (ruinsRoot != null)
            {
                ruinsRoot.position = new Vector3(0f, smoothedFollowY - 10f, 0f);
            }

            if (floatingRuinsRoot != null)
            {
                floatingRuinsRoot.position = new Vector3(0f, smoothedFollowY + 6f, 0f);
            }

            if (emberParticles != null)
            {
                emberParticles.transform.position = new Vector3(0f, smoothedFollowY + 8f, 0f);
            }

            PositionScreenQuad(backdropQuad, backdropDistance, backdropSize, new Vector3(0f, -4f, 0f));
            PositionScreenQuad(glowQuad, glowDistance, glowSize, new Vector3(0f, -18f, 0f));
            UpdateFloatingRuins();
        }

        private void BuildBackdrop()
        {
            built = true;
            DestroyExistingChildren();

            Color skyColor = GetSkyColor(theme);
            Color fogColor = GetFogColor(theme);
            Color lavaColor = theme != null ? theme.lavaColor : new Color(1f, 0.39f, 0.08f, 1f);
            Color accentColor = theme != null ? theme.accentColor : new Color(1f, 0.69f, 0.19f, 1f);

            backdropMaterial = CreateTransparentMaterial(GetBackdropTexture(skyColor, fogColor, lavaColor), Color.white);
            glowMaterial = CreateAdditiveMaterial(GetGlowTexture(), new Color(1f, 0.9f, 0.72f, 0.42f));
            ruinMaterial = CreateSolidMaterial(new Color(0.47f, 0.56f, 0.68f, 1f));
            ruinAccentMaterial = CreateSolidMaterial(new Color(0.67f, 0.75f, 0.85f, 1f));
            emberMaterial = CreateAdditiveMaterial(GetEmberTexture(), Color.white);

            backdropQuad = CreateQuad("SkyGradient", backdropMaterial, transform, backdropSize);
            glowQuad = CreateQuad("HorizonGlow", glowMaterial, transform, glowSize);
            ruinsRoot = CreateContainer("DistantRuins");
            floatingRuinsRoot = CreateContainer("FloatingRuins");
            emberParticles = CreateEmberParticles();

            BuildRuinRing();
            BuildFloatingRuins();
            SnapToCurrentView();
        }

        private void DestroyExistingChildren()
        {
            floatingRuins.Clear();

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private Transform CreateContainer(string name)
        {
            GameObject container = new(name);
            container.transform.SetParent(transform, false);
            return container.transform;
        }

        private void SnapToCurrentView()
        {
            if (followTarget != null)
            {
                smoothedFollowY = followTarget.position.y * verticalFollowFactor;
            }

            if (ruinsRoot != null)
            {
                ruinsRoot.position = new Vector3(0f, smoothedFollowY - 10f, 0f);
            }

            if (floatingRuinsRoot != null)
            {
                floatingRuinsRoot.position = new Vector3(0f, smoothedFollowY + 6f, 0f);
            }

            if (emberParticles != null)
            {
                emberParticles.transform.position = new Vector3(0f, smoothedFollowY + 8f, 0f);
            }

            PositionScreenQuad(backdropQuad, backdropDistance, backdropSize, new Vector3(0f, -4f, 0f));
            PositionScreenQuad(glowQuad, glowDistance, glowSize, new Vector3(0f, -18f, 0f));
        }

        private void PositionScreenQuad(Transform quad, float distance, Vector2 size, Vector3 localOffset)
        {
            if (quad == null || sceneCamera == null)
            {
                return;
            }

            Transform cameraTransform = sceneCamera.transform;
            quad.position = cameraTransform.position + (cameraTransform.forward * distance) +
                (cameraTransform.right * localOffset.x) +
                (cameraTransform.up * localOffset.y);
            quad.rotation = cameraTransform.rotation;
            quad.localScale = new Vector3(size.x, size.y, 1f);
        }

        private void BuildRuinRing()
        {
            UnityEngine.Random.State previousState = UnityEngine.Random.state;
            UnityEngine.Random.InitState((theme != null ? theme.themeId.GetHashCode() : 17) ^ 19463);

            for (int i = 0; i < ruinCount; i++)
            {
                float angle = (i / Mathf.Max(1f, ruinCount)) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.14f, 0.14f);
                float radius = ruinRingRadius + UnityEngine.Random.Range(-7f, 8f);
                float width = UnityEngine.Random.Range(2.4f, 5.4f);
                float depth = UnityEngine.Random.Range(1.6f, 3.2f);
                float height = UnityEngine.Random.Range(16f, 40f);
                float baseY = UnityEngine.Random.Range(-4f, 6f);
                Vector3 localPosition = new(Mathf.Cos(angle) * radius, baseY + (height * 0.5f), Mathf.Sin(angle) * radius);
                Vector3 scale = new(width, height, depth);
                CreateRuinBlock($"Ruin_{i}", ruinsRoot, localPosition, scale, i % 4 == 0 ? ruinAccentMaterial : ruinMaterial, i % 3 == 0);
            }

            UnityEngine.Random.state = previousState;
        }

        private void BuildFloatingRuins()
        {
            UnityEngine.Random.State previousState = UnityEngine.Random.state;
            UnityEngine.Random.InitState((theme != null ? theme.themeId.GetHashCode() : 29) ^ 27691);

            for (int i = 0; i < floatingRuinCount; i++)
            {
                float angle = (i / Mathf.Max(1f, floatingRuinCount)) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.18f, 0.18f);
                float radius = ruinRingRadius * UnityEngine.Random.Range(0.55f, 0.82f);
                float width = UnityEngine.Random.Range(3.2f, 8.4f);
                float depth = UnityEngine.Random.Range(1.2f, 2.8f);
                float height = UnityEngine.Random.Range(0.8f, 2.4f);
                float localY = UnityEngine.Random.Range(10f, 28f);

                GameObject ruinObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ruinObject.name = $"FloatingRuin_{i}";
                ruinObject.transform.SetParent(floatingRuinsRoot, false);
                ruinObject.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, localY, Mathf.Sin(angle) * radius);
                ruinObject.transform.localRotation = Quaternion.Euler(UnityEngine.Random.Range(-18f, 18f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-12f, 12f));
                ruinObject.transform.localScale = new Vector3(width, height, depth);

                Renderer renderer = ruinObject.GetComponent<Renderer>();
                renderer.sharedMaterial = i % 2 == 0 ? ruinAccentMaterial : ruinMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                RemoveCollider(ruinObject);

                FloatingRuinMotion motion = new()
                {
                    transform = ruinObject.transform,
                    baseLocalPosition = ruinObject.transform.localPosition,
                    baseEulerAngles = ruinObject.transform.localEulerAngles,
                    bobAmplitude = UnityEngine.Random.Range(0.35f, 1.2f),
                    bobSpeed = UnityEngine.Random.Range(0.18f, 0.52f),
                    rotationSpeed = UnityEngine.Random.Range(-8f, 8f),
                    phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                };
                floatingRuins.Add(motion);
            }

            UnityEngine.Random.state = previousState;
        }

        private void UpdateFloatingRuins()
        {
            if (floatingRuins.Count == 0)
            {
                return;
            }

            float time = Time.time;
            for (int i = 0; i < floatingRuins.Count; i++)
            {
                FloatingRuinMotion motion = floatingRuins[i];
                if (motion.transform == null)
                {
                    continue;
                }

                Vector3 localPosition = motion.baseLocalPosition;
                localPosition.y += Mathf.Sin((time * motion.bobSpeed) + motion.phase) * motion.bobAmplitude;
                motion.transform.localPosition = localPosition;

                Vector3 euler = motion.baseEulerAngles;
                euler.y += time * motion.rotationSpeed;
                motion.transform.localRotation = Quaternion.Euler(euler);
            }
        }

        private void CreateRuinBlock(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material, bool addCap)
        {
            GameObject ruinObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ruinObject.name = name;
            ruinObject.transform.SetParent(parent, false);
            ruinObject.transform.localPosition = localPosition;
            ruinObject.transform.localRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            ruinObject.transform.localScale = scale;

            Renderer renderer = ruinObject.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            RemoveCollider(ruinObject);

            if (!addCap)
            {
                return;
            }

            GameObject capObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            capObject.name = "Cap";
            capObject.transform.SetParent(ruinObject.transform, false);
            capObject.transform.localPosition = new Vector3(0f, 0.48f, 0f);
            capObject.transform.localScale = new Vector3(1.3f, 0.12f, 1.2f);

            Renderer capRenderer = capObject.GetComponent<Renderer>();
            capRenderer.sharedMaterial = ruinAccentMaterial;
            capRenderer.shadowCastingMode = ShadowCastingMode.Off;
            capRenderer.receiveShadows = false;
            RemoveCollider(capObject);
        }

        private ParticleSystem CreateEmberParticles()
        {
            GameObject particlesObject = new("BackdropEmbers");
            particlesObject.transform.SetParent(transform, false);
            ParticleSystem particles = particlesObject.AddComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.material = emberMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 12f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.42f);
            main.maxParticles = 96;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 1f, 1f, 0.28f),
                new Color(0.78f, 0.88f, 1f, 0.14f));

            var emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 10f;

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(34f, 18f, 34f);
            shape.position = new Vector3(0f, -6f, 0f);

            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.y = new ParticleSystem.MinMaxCurve(0.04f, 0.22f);
            velocity.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);

            var noise = particles.noise;
            noise.enabled = true;
            noise.strength = 0.08f;
            noise.frequency = 0.16f;
            noise.scrollSpeed = 0.1f;

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 1f, 1f), 0f),
                    new GradientColorKey(new Color(0.84f, 0.92f, 1f), 0.52f),
                    new GradientColorKey(new Color(0.72f, 0.84f, 0.98f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.22f, 0.18f),
                    new GradientAlphaKey(0.14f, 0.72f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = gradient;

            particles.Play();
            return particles;
        }

        private static Transform CreateQuad(string name, Material material, Transform parent, Vector2 size)
        {
            GameObject quadObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadObject.name = name;
            quadObject.transform.SetParent(parent, false);
            quadObject.transform.localScale = new Vector3(size.x, size.y, 1f);

            Renderer renderer = quadObject.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            RemoveCollider(quadObject);
            return quadObject.transform;
        }

        private static Material CreateTransparentMaterial(Texture texture, Color color)
        {
            Shader shader = Shader.Find("Unlit/Transparent")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit");
            Material material = new(shader);
            ApplyMaterialColor(material, color);
            ApplyTexture(material, texture);
            material.renderQueue = 2950;
            return material;
        }

        private static Material CreateAdditiveMaterial(Texture texture, Color color)
        {
            Shader shader = Shader.Find("Legacy Shaders/Particles/Additive")
                ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Sprites/Default");
            Material material = new(shader);
            ApplyMaterialColor(material, color);
            ApplyTexture(material, texture);
            material.renderQueue = 3000;
            return material;
        }

        private static Material CreateSolidMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard");
            Material material = new(shader);
            ApplyMaterialColor(material, color);
            return material;
        }

        private static void ApplyMaterialColor(Material material, Color color)
        {
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        private static void ApplyTexture(Material material, Texture texture)
        {
            if (texture == null)
            {
                return;
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }
        }

        private static Texture2D GetBackdropTexture(Color skyColor, Color fogColor, Color lavaColor)
        {
            if (backdropTexture != null)
            {
                return backdropTexture;
            }

            backdropTexture = new Texture2D(32, 256, TextureFormat.RGBA32, false, true)
            {
                name = "TowerMaze_BackdropGradient",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            Color bottom = Color.Lerp(fogColor, new Color(1f, 0.92f, 0.78f, 1f), 0.45f);
            bottom.a = 1f;
            Color middle = Color.Lerp(fogColor, skyColor, 0.55f);
            middle.a = 1f;
            Color top = Color.Lerp(skyColor, Color.white, 0.18f);
            top.a = 1f;

            for (int y = 0; y < backdropTexture.height; y++)
            {
                float t = y / (backdropTexture.height - 1f);
                Color rowColor = t < 0.38f
                    ? Color.Lerp(bottom, middle, t / 0.38f)
                    : Color.Lerp(middle, top, (t - 0.38f) / 0.62f);

                for (int x = 0; x < backdropTexture.width; x++)
                {
                    float cloudBand = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.18f) / 0.6f)) * (1f - Mathf.SmoothStep(0.72f, 1f, t));
                    float cloudNoise = Mathf.PerlinNoise((x * 0.22f) + 11.2f, (y * 0.028f) + 4.6f);
                    float wispyNoise = Mathf.PerlinNoise((x * 0.1f) + 3.8f, (y * 0.015f) + 8.9f);
                    float cloudMask = Mathf.Clamp01(((cloudNoise * 0.72f) + (wispyNoise * 0.4f)) - 0.67f) * cloudBand;
                    Color finalColor = Color.Lerp(rowColor, Color.white, cloudMask * 0.38f);
                    finalColor = Color.Lerp(finalColor, new Color(1f, 0.94f, 0.82f, 1f), Mathf.Clamp01((0.22f - t) * 2.4f) * 0.18f);
                    backdropTexture.SetPixel(x, y, finalColor);
                }
            }

            backdropTexture.Apply(false, true);
            return backdropTexture;
        }

        private static Texture2D GetGlowTexture()
        {
            if (glowTexture != null)
            {
                return glowTexture;
            }

            glowTexture = BuildRadialTexture("TowerMaze_BackdropGlow", 192, 0.2f, 1.75f);
            return glowTexture;
        }

        private static Texture2D GetEmberTexture()
        {
            if (emberTexture != null)
            {
                return emberTexture;
            }

            emberTexture = BuildRadialTexture("TowerMaze_EmberDot", 64, 0f, 3.2f);
            return emberTexture;
        }

        private static Texture2D BuildRadialTexture(string name, int size, float innerRadius, float falloff)
        {
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false, true)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            Vector2 center = new(size * 0.5f, size * 0.5f);
            float radius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float normalized = Mathf.InverseLerp(1f, innerRadius, distance);
                    float alpha = Mathf.Pow(Mathf.Clamp01(normalized), falloff);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, true);
            return texture;
        }

        private static void RemoveCollider(GameObject target)
        {
            Collider collider = target.GetComponent<Collider>();
            if (collider == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(collider);
            }
            else
            {
                DestroyImmediate(collider);
            }
        }
    }
}
