using UnityEngine;

namespace TowerMaze
{
    public sealed class HeroVisualController : MonoBehaviour
    {
        public const float BallDiameter = 0.245f;
        public const float BallRadius = BallDiameter * 0.5f;

        private readonly struct LimbPose
        {
            public readonly Quaternion LeftArm;
            public readonly Quaternion RightArm;
            public readonly Quaternion LeftLeg;
            public readonly Quaternion RightLeg;

            public LimbPose(Quaternion leftArm, Quaternion rightArm, Quaternion leftLeg, Quaternion rightLeg)
            {
                LeftArm = leftArm;
                RightArm = rightArm;
                LeftLeg = leftLeg;
                RightLeg = rightLeg;
            }
        }

        private enum SkinVfxStyle
        {
            EmberDust,
            LavaFlare,
            AshMist,
            NeonShards,
            BronzeSparks,
            GoldSparkle,
            FrostMist,
        }

        private readonly struct SkinVfxProfile
        {
            public readonly SkinVfxStyle Style;
            public readonly Color TrailColor;
            public readonly Color TrailTailColor;
            public readonly Color ParticlePrimaryColor;
            public readonly Color ParticleSecondaryColor;
            public readonly Color LightColor;
            public readonly float TrailTime;
            public readonly float TrailWidth;
            public readonly float ParticleRate;
            public readonly float ParticleLifetime;
            public readonly float ParticleSpeed;
            public readonly float ParticleSize;
            public readonly float VerticalVelocity;
            public readonly float Gravity;
            public readonly float NoiseStrength;
            public readonly float LightBaseIntensity;
            public readonly float LightPulseAmplitude;
            public readonly float LightRange;

            public SkinVfxProfile(
                SkinVfxStyle style,
                Color trailColor,
                Color trailTailColor,
                Color particlePrimaryColor,
                Color particleSecondaryColor,
                Color lightColor,
                float trailTime,
                float trailWidth,
                float particleRate,
                float particleLifetime,
                float particleSpeed,
                float particleSize,
                float verticalVelocity,
                float gravity,
                float noiseStrength,
                float lightBaseIntensity,
                float lightPulseAmplitude,
                float lightRange)
            {
                Style = style;
                TrailColor = trailColor;
                TrailTailColor = trailTailColor;
                ParticlePrimaryColor = particlePrimaryColor;
                ParticleSecondaryColor = particleSecondaryColor;
                LightColor = lightColor;
                TrailTime = trailTime;
                TrailWidth = trailWidth;
                ParticleRate = particleRate;
                ParticleLifetime = particleLifetime;
                ParticleSpeed = particleSpeed;
                ParticleSize = particleSize;
                VerticalVelocity = verticalVelocity;
                Gravity = gravity;
                NoiseStrength = noiseStrength;
                LightBaseIntensity = lightBaseIntensity;
                LightPulseAmplitude = lightPulseAmplitude;
                LightRange = lightRange;
            }
        }

        [SerializeField] private ThemeDefinition theme;

        private Transform leftArmPivot;
        private Transform rightArmPivot;
        private Transform leftLegPivot;
        private Transform rightLegPivot;
        private Transform ballRoot;
        private Transform vfxRoot;
        private Renderer[] renderers = System.Array.Empty<Renderer>();
        private float animTime;
        private float bumpTimer;
        private float bumpDirection;
        private bool bumpVertical;
        private Quaternion rollRotation = Quaternion.identity;
        private Material primaryMaterial;
        private Material trailMaterial;
        private Material particleMaterial;
        private TrailRenderer accentTrail;
        private ParticleSystem accentParticles;
        private Light accentLight;
        private SkinVfxProfile currentVfxProfile;
        private Color baseColor = Color.black;
        private Color hotEmissionColor = new(1f, 0.35f, 0.1f, 1f);
        private float heatIntensity;
        private float motionIntensity;
        private float vfxPulseTime;

        public void Initialize(ThemeDefinition definition)
        {
            theme = definition;
            BuildVisual();
        }

        public void Tick(bool isClimbing, float horizontalInput, float angleDeltaDegrees, float heightDelta, float laneRadius)
        {
            animTime += Time.deltaTime * (isClimbing ? 6f : 2f);
            float swing = Mathf.Sin(animTime) * (isClimbing ? 28f : 6f);
            float counterSwing = Mathf.Sin(animTime + Mathf.PI) * (isClimbing ? 24f : 4f);
            float lean = horizontalInput * 10f;

            ApplyPose(new LimbPose(
                Quaternion.Euler(-40f + swing, 0f, 15f + lean),
                Quaternion.Euler(-40f + counterSwing, 0f, -15f + lean),
                Quaternion.Euler(20f + counterSwing, 0f, 6f),
                Quaternion.Euler(20f + swing, 0f, -6f)));

            ApplyRoll(angleDeltaDegrees, heightDelta, laneRadius);
            ApplyBumpOffset();
            motionIntensity = Mathf.Clamp01((isClimbing ? 0.35f : 0f) + Mathf.Abs(horizontalInput) * 0.4f + Mathf.Abs(heightDelta) * 18f);
            UpdateSkinVfx();
        }

        public void TriggerWallBump(float horizontalDirection)
        {
            bumpTimer = 0.12f;
            bumpDirection = Mathf.Approximately(horizontalDirection, 0f) ? 0f : Mathf.Sign(horizontalDirection);
            bumpVertical = Mathf.Approximately(horizontalDirection, 0f);
        }

        private void BuildVisual()
        {
            foreach (Transform child in transform)
            {
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            Material primary = CreateMaterial(baseColor);
            primaryMaterial = primary;
            trailMaterial = null;
            particleMaterial = null;
            accentTrail = null;
            accentParticles = null;
            accentLight = null;
            vfxRoot = null;
            Transform root = CreatePart("HeroBall", PrimitiveType.Sphere, transform, Vector3.zero, new Vector3(BallDiameter, BallDiameter, BallDiameter), primary);
            ballRoot = root;
            rollRotation = Quaternion.identity;
            motionIntensity = 0f;
            vfxPulseTime = 0f;

            leftArmPivot = null;
            rightArmPivot = null;
            leftLegPivot = null;
            rightLegPivot = null;

            EnsureVfxObjects();
            renderers = new Renderer[] { root.GetComponent<Renderer>() };
            root.localRotation = Quaternion.identity;
        }

        public void SetHeat(float intensity)
        {
            heatIntensity = intensity;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.sharedMaterial.HasProperty("_EmissionColor"))
                {
                    continue;
                }

                renderer.sharedMaterial.SetColor("_BaseColor", baseColor);
                renderer.sharedMaterial.SetColor("_Color", baseColor);
                Color emission = Color.Lerp(Color.black, hotEmissionColor * 0.55f, intensity);
                renderer.sharedMaterial.EnableKeyword("_EMISSION");
                renderer.sharedMaterial.SetColor("_EmissionColor", emission);
            }

            UpdateSkinVfx();
        }

        public void ApplySkin(BallSkinDefinition skin)
        {
            if (string.IsNullOrWhiteSpace(skin.id))
            {
                return;
            }

            baseColor = skin.baseColor;
            hotEmissionColor = skin.emissionColor * Mathf.Max(0.01f, skin.emissionIntensity);
            if (primaryMaterial != null)
            {
                ConfigureSkinMaterial(primaryMaterial, skin);
            }

            ConfigureSkinVfx(skin);
            SetHeat(heatIntensity);
        }

        private void EnsureVfxObjects()
        {
            if (ballRoot == null)
            {
                return;
            }

            if (vfxRoot == null)
            {
                GameObject vfxObject = new("HeroVfx");
                vfxObject.transform.SetParent(ballRoot, false);
                vfxRoot = vfxObject.transform;
            }

            if (accentTrail == null)
            {
                trailMaterial = CreateEffectMaterial();
                accentTrail = vfxRoot.gameObject.AddComponent<TrailRenderer>();
                accentTrail.material = trailMaterial;
                accentTrail.alignment = LineAlignment.View;
                accentTrail.textureMode = LineTextureMode.Stretch;
                accentTrail.numCapVertices = 10;
                accentTrail.numCornerVertices = 4;
                accentTrail.minVertexDistance = 0.01f;
                accentTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                accentTrail.receiveShadows = false;
                accentTrail.autodestruct = false;
            }

            if (accentParticles == null)
            {
                GameObject particlesObject = new("SkinParticles");
                particlesObject.transform.SetParent(vfxRoot, false);
                accentParticles = particlesObject.AddComponent<ParticleSystem>();
                ParticleSystemRenderer particleRenderer = particlesObject.GetComponent<ParticleSystemRenderer>();
                particleMaterial = CreateEffectMaterial();
                particleRenderer.material = particleMaterial;
                particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
                particleRenderer.alignment = ParticleSystemRenderSpace.View;
                particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                particleRenderer.receiveShadows = false;

                var main = accentParticles.main;
                main.playOnAwake = false;
                main.loop = true;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = 96;
                main.startColor = Color.white;

                var emission = accentParticles.emission;
                emission.enabled = true;

                var shape = accentParticles.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = BallRadius * 0.5f;
                shape.radiusThickness = 0.85f;
            }

            if (accentLight == null)
            {
                GameObject lightObject = new("SkinLight");
                lightObject.transform.SetParent(vfxRoot, false);
                accentLight = lightObject.AddComponent<Light>();
                accentLight.type = LightType.Point;
                accentLight.shadows = LightShadows.None;
                accentLight.renderMode = LightRenderMode.ForceVertex;
                accentLight.enabled = false;
            }
        }

        private void ConfigureSkinVfx(BallSkinDefinition skin)
        {
            EnsureVfxObjects();
            currentVfxProfile = CreateVfxProfile(skin.id, skin.emissionColor);

            ConfigureTrail(currentVfxProfile);
            ConfigureParticles(currentVfxProfile);

            if (accentLight != null)
            {
                accentLight.color = currentVfxProfile.LightColor;
                accentLight.range = currentVfxProfile.LightRange;
                accentLight.intensity = currentVfxProfile.LightBaseIntensity;
                accentLight.enabled = currentVfxProfile.LightBaseIntensity > 0.01f;
            }

            accentTrail?.Clear();
            if (accentParticles != null)
            {
                accentParticles.Clear(true);
                accentParticles.Play(true);
            }
        }

        private void ConfigureTrail(SkinVfxProfile profile)
        {
            if (accentTrail == null)
            {
                return;
            }

            accentTrail.time = profile.TrailTime;
            accentTrail.widthMultiplier = profile.TrailWidth;
            accentTrail.startColor = profile.TrailColor;
            accentTrail.endColor = profile.TrailTailColor;
            accentTrail.emitting = true;
            SetMaterialColor(trailMaterial, profile.TrailColor);
        }

        private void ConfigureParticles(SkinVfxProfile profile)
        {
            if (accentParticles == null)
            {
                return;
            }

            var main = accentParticles.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(profile.ParticleLifetime * 0.8f, profile.ParticleLifetime * 1.15f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(profile.ParticleSpeed * 0.75f, profile.ParticleSpeed * 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(profile.ParticleSize * 0.8f, profile.ParticleSize * 1.2f);
            main.gravityModifier = profile.Gravity;

            var emission = accentParticles.emission;
            emission.rateOverTime = profile.ParticleRate;

            var velocityOverLifetime = accentParticles.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(profile.VerticalVelocity);

            var limitVelocity = accentParticles.limitVelocityOverLifetime;
            limitVelocity.enabled = true;
            limitVelocity.dampen = 0.35f;
            limitVelocity.limit = new ParticleSystem.MinMaxCurve(Mathf.Max(0.05f, profile.ParticleSpeed * 1.2f));

            var colorOverLifetime = accentParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(CreateGradient(
                profile.ParticlePrimaryColor,
                profile.ParticleSecondaryColor));

            var sizeOverLifetime = accentParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1.1f, 1f, 0f));

            var noise = accentParticles.noise;
            noise.enabled = profile.NoiseStrength > 0.01f;
            noise.strength = profile.NoiseStrength;
            noise.frequency = 0.65f;
            noise.scrollSpeed = 0.45f;

            SetMaterialColor(particleMaterial, profile.ParticlePrimaryColor);
        }

        private void UpdateSkinVfx()
        {
            if (accentTrail == null || accentParticles == null)
            {
                return;
            }

            vfxPulseTime += Time.deltaTime * (1.2f + heatIntensity * 1.5f + motionIntensity);
            float pulse = 0.5f + (Mathf.Sin(vfxPulseTime) * 0.5f);
            float intensity = Mathf.Clamp01(0.25f + motionIntensity + (heatIntensity * 0.85f));

            accentTrail.time = Mathf.Lerp(currentVfxProfile.TrailTime * 0.7f, currentVfxProfile.TrailTime * 1.18f, intensity);
            accentTrail.widthMultiplier = Mathf.Lerp(currentVfxProfile.TrailWidth * 0.78f, currentVfxProfile.TrailWidth * 1.2f, intensity);
            accentTrail.startColor = TintColor(currentVfxProfile.TrailColor, 0.6f + (intensity * 0.8f) + (pulse * 0.2f));
            accentTrail.endColor = TintColor(currentVfxProfile.TrailTailColor, 0.45f + (intensity * 0.4f));

            var emission = accentParticles.emission;
            emission.rateOverTime = currentVfxProfile.ParticleRate * Mathf.Lerp(0.3f, 1.35f, intensity);

            if (!accentParticles.isPlaying)
            {
                accentParticles.Play(true);
            }

            if (accentLight != null)
            {
                accentLight.enabled = currentVfxProfile.LightBaseIntensity > 0.01f;
                accentLight.color = Color.Lerp(currentVfxProfile.LightColor, hotEmissionColor, heatIntensity * 0.35f);
                accentLight.intensity = currentVfxProfile.LightBaseIntensity + (currentVfxProfile.LightPulseAmplitude * pulse) + (intensity * 0.15f);
                accentLight.range = currentVfxProfile.LightRange + (intensity * 0.12f);
            }
        }

        private void ApplyPose(LimbPose pose)
        {
            if (leftArmPivot == null)
            {
                return;
            }

            leftArmPivot.localRotation = pose.LeftArm;
            rightArmPivot.localRotation = pose.RightArm;
            leftLegPivot.localRotation = pose.LeftLeg;
            rightLegPivot.localRotation = pose.RightLeg;
        }

        private void ApplyBumpOffset()
        {
            if (ballRoot == null)
            {
                return;
            }

            if (bumpTimer <= 0f)
            {
                ballRoot.localPosition = Vector3.zero;
                ballRoot.localRotation = rollRotation;
                return;
            }

            bumpTimer = Mathf.Max(0f, bumpTimer - Time.deltaTime);
            float normalized = bumpTimer / 0.12f;
            float pulse = Mathf.Sin((1f - normalized) * Mathf.PI);
            float sideways = bumpVertical
                ? Mathf.Sin((1f - normalized) * Mathf.PI * 8f) * 0.015f
                : -bumpDirection * 0.05f * pulse;
            float vertical = bumpVertical
                ? Mathf.Sin((1f - normalized) * Mathf.PI * 12f) * 0.012f
                : 0f;
            float backward = -0.03f * pulse;
            float tilt = bumpVertical
                ? Mathf.Sin((1f - normalized) * Mathf.PI * 6f) * 4f
                : bumpDirection * 6f * pulse;

            ballRoot.localPosition = new Vector3(sideways, vertical, backward);
            ballRoot.localRotation = Quaternion.Euler(0f, 0f, tilt) * rollRotation;
        }

        private void ApplyRoll(float angleDeltaDegrees, float heightDelta, float laneRadius)
        {
            if (ballRoot == null)
            {
                return;
            }

            float ballRadius = BallRadius;
            float horizontalDistance = Mathf.Deg2Rad * angleDeltaDegrees * Mathf.Max(0.01f, laneRadius);
            float verticalDistance = heightDelta;
            if (Mathf.Approximately(horizontalDistance, 0f) && Mathf.Approximately(verticalDistance, 0f))
            {
                return;
            }

            Vector3 localMove = new(horizontalDistance, verticalDistance, 0f);
            Vector3 moveDirection = localMove.normalized;
            Vector3 surfaceNormal = -Vector3.forward;
            Vector3 rollAxis = Vector3.Cross(surfaceNormal, moveDirection).normalized;
            float travelDistance = localMove.magnitude;
            float rollDegrees = Mathf.Rad2Deg * (travelDistance / Mathf.Max(0.001f, ballRadius));
            rollRotation = Quaternion.AngleAxis(rollDegrees, rollAxis) * rollRotation;
        }

        private static Transform CreatePivot(string name, Transform parent, Vector3 localPosition)
        {
            GameObject pivot = new(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = localPosition;
            pivot.transform.localRotation = Quaternion.identity;
            return pivot.transform;
        }

        private static Transform CreatePart(string name, PrimitiveType primitiveType, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(primitiveType);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = localScale;

            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(collider);
                }
                else
                {
                    DestroyImmediate(collider);
                }
            }

            Renderer renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            return part.transform;
        }

        private static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new(shader)
            {
                color = color
            };
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            return material;
        }

        private void ConfigureSkinMaterial(Material material, BallSkinDefinition skin)
        {
            material.color = skin.baseColor;
            material.SetColor("_BaseColor", skin.baseColor);
            material.SetColor("_Color", skin.baseColor);

            Texture2D baseMap = BallSkinTextureLibrary.LoadTexture(skin.baseMapResourcePath);
            Texture2D normalMap = BallSkinTextureLibrary.LoadTexture(skin.normalMapResourcePath);
            Texture2D emissionMap = BallSkinTextureLibrary.LoadTexture(skin.emissionMapResourcePath);

            SetTexture(material, "_BaseMap", baseMap);
            SetTexture(material, "_MainTex", baseMap);
            SetTextureScale(material, "_BaseMap", skin.textureScale);
            SetTextureScale(material, "_MainTex", skin.textureScale);

            SetTexture(material, "_EmissionMap", emissionMap);

            if (normalMap != null)
            {
                SetTexture(material, "_BumpMap", normalMap);
                SetTextureScale(material, "_BumpMap", skin.textureScale);
                material.EnableKeyword("_NORMALMAP");
            }
            else
            {
                SetTexture(material, "_BumpMap", null);
                material.DisableKeyword("_NORMALMAP");
            }

            SetFloat(material, "_BumpScale", skin.normalStrength);
            SetFloat(material, "_Metallic", skin.metallic);
            SetFloat(material, "_Smoothness", skin.smoothness);
            SetFloat(material, "_Glossiness", skin.smoothness);
        }

        private static SkinVfxProfile CreateVfxProfile(string skinId, Color emissionColor)
        {
            switch (skinId)
            {
                case "molten_core":
                    return new SkinVfxProfile(
                        SkinVfxStyle.LavaFlare,
                        new Color(1f, 0.48f, 0.16f, 0.88f),
                        new Color(0.88f, 0.16f, 0.06f, 0f),
                        new Color(1f, 0.74f, 0.42f, 0.95f),
                        new Color(1f, 0.16f, 0.08f, 0f),
                        emissionColor,
                        0.2f,
                        0.08f,
                        22f,
                        0.65f,
                        0.32f,
                        0.03f,
                        0.18f,
                        0.05f,
                        0.28f,
                        1.1f,
                        0.55f,
                        1.7f);

                case "ash_marble":
                    return new SkinVfxProfile(
                        SkinVfxStyle.AshMist,
                        new Color(0.86f, 0.78f, 0.74f, 0.42f),
                        new Color(0.32f, 0.32f, 0.34f, 0f),
                        new Color(0.92f, 0.86f, 0.82f, 0.45f),
                        new Color(0.34f, 0.34f, 0.38f, 0f),
                        new Color(1f, 0.72f, 0.4f, 1f),
                        0.24f,
                        0.09f,
                        8f,
                        0.9f,
                        0.12f,
                        0.04f,
                        0.18f,
                        -0.01f,
                        0.42f,
                        0.22f,
                        0.12f,
                        1.2f);

                case "hazard_neon":
                    return new SkinVfxProfile(
                        SkinVfxStyle.NeonShards,
                        new Color(1f, 0.95f, 0.22f, 0.9f),
                        new Color(0.92f, 0.42f, 0.08f, 0f),
                        new Color(1f, 0.96f, 0.42f, 0.9f),
                        new Color(0.98f, 0.64f, 0.08f, 0f),
                        new Color(1f, 0.92f, 0.22f, 1f),
                        0.14f,
                        0.06f,
                        16f,
                        0.38f,
                        0.55f,
                        0.022f,
                        0.04f,
                        0.12f,
                        0.18f,
                        0.58f,
                        0.28f,
                        1.35f);

                case "forge_bronze":
                    return new SkinVfxProfile(
                        SkinVfxStyle.BronzeSparks,
                        new Color(0.98f, 0.72f, 0.38f, 0.74f),
                        new Color(0.72f, 0.28f, 0.08f, 0f),
                        new Color(1f, 0.82f, 0.54f, 0.85f),
                        new Color(0.76f, 0.3f, 0.08f, 0f),
                        new Color(1f, 0.66f, 0.28f, 1f),
                        0.18f,
                        0.07f,
                        13f,
                        0.52f,
                        0.44f,
                        0.024f,
                        0.06f,
                        0.18f,
                        0.14f,
                        0.52f,
                        0.2f,
                        1.45f);

                case "relic_gold":
                    return new SkinVfxProfile(
                        SkinVfxStyle.GoldSparkle,
                        new Color(1f, 0.88f, 0.46f, 0.86f),
                        new Color(1f, 0.72f, 0.12f, 0f),
                        new Color(1f, 0.94f, 0.72f, 0.92f),
                        new Color(1f, 0.7f, 0.2f, 0f),
                        new Color(1f, 0.86f, 0.34f, 1f),
                        0.2f,
                        0.08f,
                        10f,
                        0.62f,
                        0.24f,
                        0.028f,
                        0.08f,
                        0f,
                        0.08f,
                        0.82f,
                        0.28f,
                        1.75f);

                case "void_ice":
                    return new SkinVfxProfile(
                        SkinVfxStyle.FrostMist,
                        new Color(0.62f, 0.92f, 1f, 0.74f),
                        new Color(0.18f, 0.42f, 0.68f, 0f),
                        new Color(0.84f, 0.98f, 1f, 0.84f),
                        new Color(0.42f, 0.8f, 1f, 0f),
                        new Color(0.5f, 0.9f, 1f, 1f),
                        0.26f,
                        0.09f,
                        11f,
                        0.72f,
                        0.18f,
                        0.032f,
                        0.12f,
                        -0.03f,
                        0.36f,
                        0.68f,
                        0.24f,
                        1.6f);

                default:
                    return new SkinVfxProfile(
                        SkinVfxStyle.EmberDust,
                        new Color(1f, 0.44f, 0.18f, 0.56f),
                        new Color(0.45f, 0.16f, 0.08f, 0f),
                        new Color(1f, 0.74f, 0.52f, 0.76f),
                        new Color(0.72f, 0.22f, 0.08f, 0f),
                        new Color(1f, 0.42f, 0.14f, 1f),
                        0.16f,
                        0.06f,
                        6f,
                        0.46f,
                        0.18f,
                        0.02f,
                        0.08f,
                        0.04f,
                        0.16f,
                        0.28f,
                        0.1f,
                        1.1f);
            }
        }

        private static Gradient CreateGradient(Color startColor, Color endColor)
        {
            Gradient gradient = new();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(startColor, 0f),
                    new GradientColorKey(Color.Lerp(startColor, endColor, 0.45f), 0.55f),
                    new GradientColorKey(endColor, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(startColor.a, 0f),
                    new GradientAlphaKey(Mathf.Lerp(startColor.a, endColor.a, 0.4f), 0.55f),
                    new GradientAlphaKey(endColor.a, 1f),
                });
            return gradient;
        }

        private static Color TintColor(Color color, float brightness)
        {
            return new Color(
                Mathf.Clamp01(color.r * brightness),
                Mathf.Clamp01(color.g * brightness),
                Mathf.Clamp01(color.b * brightness),
                color.a);
        }

        private static Material CreateEffectMaterial()
        {
            Shader shader = Shader.Find("Legacy Shaders/Particles/Additive")
                ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Sprites/Default");
            Material material = new(shader);
            SetMaterialColor(material, Color.white);
            return material;
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        private static void SetTexture(Material material, string propertyName, Texture texture)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, texture);
            }
        }

        private static void SetTextureScale(Material material, string propertyName, Vector2 scale)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetTextureScale(propertyName, scale);
            }
        }

        private static void SetFloat(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }
    }
}
