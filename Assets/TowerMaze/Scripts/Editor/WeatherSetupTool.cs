#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using TowerMaze.BackgroundSystem;
using TowerMaze.WeatherSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TowerMaze.EditorTools
{
    /// <summary>
    /// One-shot bootstrap for the Weather + Background system. Run from
    /// Tools → TowerMaze → Setup Weather System. Idempotent — re-running
    /// upgrades existing assets in place rather than duplicating.
    /// Creates: placeholder textures, URP materials, particle prefabs, UI
    /// selection panel prefab, scene hierarchy with auto-bound references.
    /// Replace the placeholder textures with the production art when ready;
    /// the materials stay wired by GUID.
    /// </summary>
    public static class WeatherSetupTool
    {
        private const string BgTexFolder = "Assets/TowerMaze/Textures/Backgrounds";
        private const string WeatherTexFolder = "Assets/TowerMaze/Textures/Weather";
        private const string BgMatFolder = "Assets/TowerMaze/Materials/Backgrounds";
        private const string WeatherMatFolder = "Assets/TowerMaze/Materials/Weather";
        private const string BgPrefabFolder = "Assets/TowerMaze/Prefabs/Backgrounds";
        private const string WeatherPrefabFolder = "Assets/TowerMaze/Prefabs/Weather";

        private static readonly WeatherType[] AllWeather =
        {
            WeatherType.Sunny, WeatherType.Snow, WeatherType.Rain, WeatherType.Fog, WeatherType.StarryNight,
        };

        [MenuItem("Tools/TowerMaze/Setup Weather System")]
        public static void Run()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Weather Setup", "Ensuring folders...", 0.05f);
                EnsureFolders();

                EditorUtility.DisplayProgressBar("Weather Setup", "Generating placeholder textures...", 0.20f);
                Dictionary<WeatherType, Texture2D> bgTextures = GenerateBackgroundPlaceholders();
                Dictionary<WeatherType, Texture2D> overlayTextures = GenerateOverlayPlaceholders();
                Texture2D wetBlock = GenerateBlockPlaceholder("TX_Block_Wet_1024", new Color(0.10f, 0.12f, 0.16f), 0.85f);
                Texture2D icyBlock = GenerateBlockPlaceholder("TX_Block_Icy_1024", new Color(0.62f, 0.78f, 0.92f), 0.40f);

                EditorUtility.DisplayProgressBar("Weather Setup", "Building materials...", 0.40f);
                Dictionary<WeatherType, Material> bgMaterials = CreateBackgroundMaterials(bgTextures);
                Dictionary<WeatherType, Material> overlayMaterials = CreateOverlayMaterials(overlayTextures);
                Material wetMat = CreateLitMaterial("MAT_Block_Wet", wetBlock, glossiness: 0.85f, metallic: 0.05f);
                Material icyMat = CreateLitMaterial("MAT_Block_Icy", icyBlock, glossiness: 0.55f, metallic: 0.10f, tint: new Color(0.85f, 0.92f, 1f));

                EditorUtility.DisplayProgressBar("Weather Setup", "Building particle prefabs...", 0.60f);
                GameObject snowPrefab = CreateParticlePrefab("SnowEffect", BuildSnowSystem);
                GameObject rainPrefab = CreateParticlePrefab("RainEffect", BuildRainSystem);
                GameObject fogPrefab = CreateParticlePrefab("FogEffect", BuildFogSystem);
                GameObject starsPrefab = CreateParticlePrefab("StarsEffect", BuildStarsSystem);

                EditorUtility.DisplayProgressBar("Weather Setup", "Wiring scene hierarchy...", 0.80f);
                BuildSceneHierarchy(bgMaterials, overlayMaterials, snowPrefab, rainPrefab, fogPrefab, starsPrefab, wetMat, icyMat);

                EditorUtility.DisplayProgressBar("Weather Setup", "Building selection panel...", 0.92f);
                CreateWeatherSelectionPanelPrefab();

                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log("[WeatherSetupTool] Done. Replace placeholder textures with production art when ready.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WeatherSetupTool] Setup failed: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void EnsureFolders()
        {
            CreateFolderRecursive(BgTexFolder);
            CreateFolderRecursive(WeatherTexFolder);
            CreateFolderRecursive(BgMatFolder);
            CreateFolderRecursive(WeatherMatFolder);
            CreateFolderRecursive(BgPrefabFolder);
            CreateFolderRecursive(WeatherPrefabFolder);
        }

        private static void CreateFolderRecursive(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;
            string parent = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent)) CreateFolderRecursive(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(assetPath));
        }

        // ─── Texture placeholders ───────────────────────────────────────────

        private static Dictionary<WeatherType, Texture2D> GenerateBackgroundPlaceholders()
        {
            Dictionary<WeatherType, Texture2D> map = new();
            // Vertical gradient palette per theme — top color → bottom color.
            (Color top, Color bottom)[] palettes =
            {
                (new Color(0.45f, 0.78f, 1.00f), new Color(0.85f, 0.93f, 1.00f)), // Sunny
                (new Color(0.62f, 0.74f, 0.92f), new Color(0.92f, 0.96f, 1.00f)), // Snow
                (new Color(0.30f, 0.36f, 0.46f), new Color(0.55f, 0.62f, 0.72f)), // Rain
                (new Color(0.46f, 0.50f, 0.56f), new Color(0.78f, 0.82f, 0.86f)), // Fog
                (new Color(0.04f, 0.05f, 0.18f), new Color(0.10f, 0.14f, 0.30f)), // StarryNight
            };
            string[] names = { "TX_BG_Sunny", "TX_BG_Snow", "TX_BG_Rain", "TX_BG_Fog", "TX_BG_Night" };

            for (int i = 0; i < AllWeather.Length; i++)
            {
                Texture2D tex = CreateGradientTexture(256, 456, palettes[i].top, palettes[i].bottom);
                string path = $"{BgTexFolder}/{names[i]}_1080x1920.png";
                SaveAndImport(tex, path, alphaIsTransparency: false, mipmaps: false, maxSize: 2048);
                map[AllWeather[i]] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            return map;
        }

        private static Dictionary<WeatherType, Texture2D> GenerateOverlayPlaceholders()
        {
            Dictionary<WeatherType, Texture2D> map = new();
            // Overlay placeholders are very subtle alpha noise — designer
            // replaces with the proper particle / fog / sparkle art later.
            string[] names = { null, "TX_Overlay_Snow", "TX_Overlay_Rain", "TX_Overlay_Fog", "TX_Overlay_Night" };
            float[] alphas = { 0f, 0.18f, 0.20f, 0.32f, 0.16f };
            Color[] tints =
            {
                Color.clear,
                new Color(1f, 1f, 1f, 1f),
                new Color(0.62f, 0.72f, 0.85f, 1f),
                new Color(0.78f, 0.82f, 0.86f, 1f),
                new Color(0.95f, 0.92f, 0.65f, 1f),
            };

            for (int i = 0; i < AllWeather.Length; i++)
            {
                if (names[i] == null) { map[AllWeather[i]] = null; continue; }
                Texture2D tex = CreateAlphaNoiseTexture(256, 456, tints[i], alphas[i]);
                string path = $"{BgTexFolder}/{names[i]}_1080x1920.png";
                SaveAndImport(tex, path, alphaIsTransparency: true, mipmaps: false, maxSize: 2048);
                map[AllWeather[i]] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            return map;
        }

        private static Texture2D GenerateBlockPlaceholder(string name, Color baseColor, float wetness)
        {
            Texture2D tex = new Texture2D(256, 256, TextureFormat.RGB24, false);
            for (int y = 0; y < 256; y++)
            for (int x = 0; x < 256; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.04f, y * 0.04f);
                Color c = baseColor * (0.85f + 0.30f * n);
                c = Color.Lerp(c, Color.white, wetness * 0.15f * n);
                c.a = 1f;
                tex.SetPixel(x, y, c);
            }
            tex.Apply(false, false);
            string path = $"{WeatherTexFolder}/{name}.png";
            SaveAndImport(tex, path, alphaIsTransparency: false, mipmaps: true, maxSize: 1024);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Texture2D CreateGradientTexture(int w, int h, Color top, Color bottom)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1);
                Color row = Color.Lerp(bottom, top, t);
                // Add a soft horizon glow at the lower-third for atmosphere.
                if (t > 0.20f && t < 0.45f) row = Color.Lerp(row, Color.white * 0.8f, 0.10f * (1f - Mathf.Abs(t - 0.32f) * 5f));
                for (int x = 0; x < w; x++) tex.SetPixel(x, y, row);
            }
            tex.Apply(false, false);
            return tex;
        }

        private static Texture2D CreateAlphaNoiseTexture(int w, int h, Color tint, float maxAlpha)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.025f, y * 0.05f);
                Color c = tint;
                c.a = Mathf.Clamp01(n * maxAlpha);
                tex.SetPixel(x, y, c);
            }
            tex.Apply(false, false);
            return tex;
        }

        private static void SaveAndImport(Texture2D tex, string assetPath, bool alphaIsTransparency, bool mipmaps, int maxSize)
        {
            File.WriteAllBytes(assetPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;
            importer.alphaIsTransparency = alphaIsTransparency;
            importer.mipmapEnabled = mipmaps;
            importer.maxTextureSize = maxSize;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        // ─── Materials ──────────────────────────────────────────────────────

        private static Dictionary<WeatherType, Material> CreateBackgroundMaterials(Dictionary<WeatherType, Texture2D> textures)
        {
            string[] names = { "MAT_BG_Sunny", "MAT_BG_Snow", "MAT_BG_Rain", "MAT_BG_Fog", "MAT_BG_Night" };
            Dictionary<WeatherType, Material> map = new();
            for (int i = 0; i < AllWeather.Length; i++)
            {
                Material mat = CreateUnlitMaterial(names[i], textures[AllWeather[i]], transparent: false, BgMatFolder);
                map[AllWeather[i]] = mat;
            }
            return map;
        }

        private static Dictionary<WeatherType, Material> CreateOverlayMaterials(Dictionary<WeatherType, Texture2D> textures)
        {
            string[] names = { null, "MAT_Overlay_Snow", "MAT_Overlay_Rain", "MAT_Overlay_Fog", "MAT_Overlay_Night" };
            Dictionary<WeatherType, Material> map = new();
            for (int i = 0; i < AllWeather.Length; i++)
            {
                if (names[i] == null) { map[AllWeather[i]] = null; continue; }
                Material mat = CreateUnlitMaterial(names[i], textures[AllWeather[i]], transparent: true, BgMatFolder);
                map[AllWeather[i]] = mat;
            }
            return map;
        }

        private static Material CreateUnlitMaterial(string name, Texture tex, bool transparent, string folder)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
            Material mat = new Material(shader);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            Color baseColor = Color.white;
            if (transparent) baseColor.a = 1f;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Color")) mat.color = baseColor;

            if (transparent && mat.HasProperty("_Surface"))
            {
                // URP Unlit Surface options: 0 Opaque, 1 Transparent.
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f); // Alpha
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            string path = $"{folder}/{name}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.shader = mat.shader;
                existing.CopyPropertiesFromMaterial(mat);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static Material CreateLitMaterial(string name, Texture tex, float glossiness, float metallic, Color? tint = null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(shader);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint ?? Color.white);
            if (mat.HasProperty("_Color")) mat.color = tint ?? Color.white;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", glossiness);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", glossiness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            string path = $"{WeatherMatFolder}/{name}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.shader = mat.shader;
                existing.CopyPropertiesFromMaterial(mat);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        // ─── Particle prefabs ───────────────────────────────────────────────

        private static GameObject CreateParticlePrefab(string name, Action<ParticleSystem> configure)
        {
            string path = $"{WeatherPrefabFolder}/{name}.prefab";
            GameObject root = new GameObject(name);
            ParticleSystem ps = root.AddComponent<ParticleSystem>();
            configure(ps);
            ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.sharedMaterial = CreateDefaultParticleMaterial(name);
            }
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static Material CreateDefaultParticleMaterial(string suffix)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Particles/Alpha Blended");
            Material mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color")) mat.color = Color.white;
            string path = $"{WeatherMatFolder}/MAT_Particle_{suffix}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.shader = mat.shader;
                existing.CopyPropertiesFromMaterial(mat);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void BuildSnowSystem(ParticleSystem ps)
        {
            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = 4.5f;
            main.startSpeed = 0.6f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.36f);
            main.startColor = new Color(1f, 1f, 1f, 0.85f);
            main.maxParticles = 240;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission; emission.rateOverTime = 35f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(20f, 0.2f, 8f); shape.position = new Vector3(0f, 8f, 0f);
            var velocity = ps.velocityOverLifetime; velocity.enabled = true; velocity.x = new ParticleSystem.MinMaxCurve(-0.20f, 0.20f); velocity.y = new ParticleSystem.MinMaxCurve(-0.6f);
        }

        private static void BuildRainSystem(ParticleSystem ps)
        {
            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = 1.4f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(8f, 12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
            main.startColor = new Color(0.78f, 0.86f, 0.96f, 0.55f);
            main.maxParticles = 480;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.5f;
            var emission = ps.emission; emission.rateOverTime = 220f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(20f, 0.2f, 8f); shape.position = new Vector3(0f, 12f, 0f);
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null) { renderer.renderMode = ParticleSystemRenderMode.Stretch; renderer.lengthScale = 4f; renderer.velocityScale = 0.05f; }
        }

        private static void BuildFogSystem(ParticleSystem ps)
        {
            var main = ps.main;
            main.duration = 6f;
            main.loop = true;
            main.startLifetime = 6f;
            main.startSpeed = 0.2f;
            main.startSize = new ParticleSystem.MinMaxCurve(4f, 8f);
            main.startColor = new Color(0.78f, 0.82f, 0.86f, 0.18f);
            main.maxParticles = 24;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission; emission.rateOverTime = 3f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(18f, 4f, 6f);
        }

        private static void BuildStarsSystem(ParticleSystem ps)
        {
            var main = ps.main;
            main.duration = 8f;
            main.loop = true;
            main.startLifetime = 4f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startColor = new Color(1f, 0.96f, 0.78f, 0.95f);
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission; emission.rateOverTime = 6f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(18f, 14f, 6f); shape.position = new Vector3(0f, 6f, 0f);
            var coa = ps.colorOverLifetime; coa.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) });
            coa.color = new ParticleSystem.MinMaxGradient(g);
        }

        // ─── Scene hierarchy ────────────────────────────────────────────────

        private static void BuildSceneHierarchy(
            Dictionary<WeatherType, Material> bgMats,
            Dictionary<WeatherType, Material> overlayMats,
            GameObject snowPrefab, GameObject rainPrefab, GameObject fogPrefab, GameObject starsPrefab,
            Material wetMat, Material icyMat)
        {
            // BackgroundRoot — anchored under the active scene root, repositioned each LateUpdate.
            GameObject bgRoot = GameObject.Find("BackgroundRoot") ?? new GameObject("BackgroundRoot");

            Renderer backGradient = EnsureLayer(bgRoot.transform, "BG_BackGradient", -8f);
            Renderer midA = EnsureLayer(bgRoot.transform, "BG_MidAtmosphere_A", -7f);
            Renderer midB = EnsureLayer(bgRoot.transform, "BG_MidAtmosphere_B", -6.95f);
            Renderer farClouds = EnsureLayer(bgRoot.transform, "BG_FarClouds", -6.9f);
            Renderer overlayA = EnsureLayer(bgRoot.transform, "BG_WeatherOverlay_A", -6.85f);
            Renderer overlayB = EnsureLayer(bgRoot.transform, "BG_WeatherOverlay_B", -6.80f);
            EnsureLayer(bgRoot.transform, "BG_Particles", -6.75f);

            BackgroundManager bgMgr = bgRoot.GetComponent<BackgroundManager>() ?? bgRoot.AddComponent<BackgroundManager>();
            SerializedObject bgSO = new SerializedObject(bgMgr);
            bgSO.FindProperty("targetCamera").objectReferenceValue = Camera.main;
            bgSO.FindProperty("backgroundRoot").objectReferenceValue = bgRoot.transform;
            bgSO.FindProperty("backGradientRenderer").objectReferenceValue = backGradient;
            bgSO.FindProperty("midAtmosphereA").objectReferenceValue = midA;
            bgSO.FindProperty("midAtmosphereB").objectReferenceValue = midB;
            bgSO.FindProperty("farCloudsRenderer").objectReferenceValue = farClouds;
            bgSO.FindProperty("weatherOverlayA").objectReferenceValue = overlayA;
            bgSO.FindProperty("weatherOverlayB").objectReferenceValue = overlayB;
            FillMaterialArray(bgSO.FindProperty("backgroundMaterials"), bgMats);
            FillMaterialArray(bgSO.FindProperty("overlayMaterials"), overlayMats);
            bgSO.ApplyModifiedPropertiesWithoutUndo();

            // WeatherEffects — instantiate the four particle prefabs as scene
            // children so the WeatherManager can toggle them by reference.
            GameObject effectsRoot = GameObject.Find("WeatherEffects") ?? new GameObject("WeatherEffects");
            GameObject snowGo = EnsureChildPrefab(effectsRoot.transform, "SnowEffect", snowPrefab);
            GameObject rainGo = EnsureChildPrefab(effectsRoot.transform, "RainEffect", rainPrefab);
            GameObject fogGo = EnsureChildPrefab(effectsRoot.transform, "FogEffect", fogPrefab);
            GameObject starsGo = EnsureChildPrefab(effectsRoot.transform, "StarsEffect", starsPrefab);
            snowGo.SetActive(false); rainGo.SetActive(false); fogGo.SetActive(false); starsGo.SetActive(false);

            // WeatherManager
            GameObject mgrGo = GameObject.Find("WeatherManager") ?? new GameObject("WeatherManager");
            WeatherManager mgr = mgrGo.GetComponent<WeatherManager>() ?? mgrGo.AddComponent<WeatherManager>();
            SerializedObject mgrSO = new SerializedObject(mgr);
            mgrSO.FindProperty("backgroundManager").objectReferenceValue = bgMgr;
            mgrSO.FindProperty("snowEffect").objectReferenceValue = snowGo;
            mgrSO.FindProperty("rainEffect").objectReferenceValue = rainGo;
            mgrSO.FindProperty("fogEffect").objectReferenceValue = fogGo;
            mgrSO.FindProperty("starsEffect").objectReferenceValue = starsGo;
            mgrSO.FindProperty("directionalLight").objectReferenceValue = UnityEngine.Object.FindAnyObjectByType<Light>();
            mgrSO.FindProperty("wetBlockMaterial").objectReferenceValue = wetMat;
            mgrSO.FindProperty("icyBlockMaterial").objectReferenceValue = icyMat;
            mgrSO.ApplyModifiedPropertiesWithoutUndo();

            // Bridge — polls PlayerController + GameConfig for zone changes.
            WeatherZoneBridge bridge = mgrGo.GetComponent<WeatherZoneBridge>() ?? mgrGo.AddComponent<WeatherZoneBridge>();
            SerializedObject brSO = new SerializedObject(bridge);
            brSO.FindProperty("weatherManager").objectReferenceValue = mgr;
            brSO.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Renderer EnsureLayer(Transform parent, string layerName, float zOffset)
        {
            Transform existing = parent.Find(layerName);
            GameObject go;
            if (existing == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                UnityEngine.Object.DestroyImmediate(go.GetComponent<MeshCollider>());
                go.name = layerName;
                go.transform.SetParent(parent, false);
            }
            else
            {
                go = existing.gameObject;
            }
            go.transform.localPosition = new Vector3(0f, 0f, zOffset);
            go.transform.localRotation = Quaternion.identity;
            return go.GetComponent<Renderer>();
        }

        private static GameObject EnsureChildPrefab(Transform parent, string name, GameObject prefab)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing.gameObject;
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            return instance;
        }

        private static void FillMaterialArray(SerializedProperty arrayProp, Dictionary<WeatherType, Material> map)
        {
            arrayProp.arraySize = AllWeather.Length;
            for (int i = 0; i < AllWeather.Length; i++)
            {
                arrayProp.GetArrayElementAtIndex(i).objectReferenceValue = map[AllWeather[i]];
            }
        }

        // ─── Selection panel prefab ─────────────────────────────────────────

        private static void CreateWeatherSelectionPanelPrefab()
        {
            string path = $"{WeatherPrefabFolder}/WeatherSelectionPanel.prefab";

            GameObject panel = new GameObject("WeatherSelectionPanel", typeof(RectTransform));
            RectTransform panelRT = panel.GetComponent<RectTransform>();
            panelRT.sizeDelta = new Vector2(720f, 720f);
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.04f, 0.14f, 0.95f);

            VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(40, 40, 40, 40);
            vlg.spacing = 18f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childAlignment = TextAnchor.UpperCenter;

            for (int i = 0; i < AllWeather.Length; i++)
            {
                CreateSelectionRow(panel.transform, AllWeather[i]);
            }

            PrefabUtility.SaveAsPrefabAsset(panel, path);
            UnityEngine.Object.DestroyImmediate(panel);
        }

        private static void CreateSelectionRow(Transform parent, WeatherType weather)
        {
            GameObject row = new GameObject($"Row_{weather}", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 96f;
            Image rowBg = row.AddComponent<Image>();
            rowBg.color = new Color(0.10f, 0.08f, 0.20f, 0.95f);
            Outline rowOutline = row.AddComponent<Outline>();
            rowOutline.effectColor = new Color(1f, 0.78f, 0.30f, 0.45f);
            rowOutline.effectDistance = new Vector2(2f, -2f);

            // Label
            GameObject labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(row.transform, false);
            RectTransform labelRT = labelGo.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0.05f, 0f); labelRT.anchorMax = new Vector2(0.55f, 1f);
            labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
            TextMeshProUGUI label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text = weather.ToString().ToUpperInvariant();
            label.fontSize = 32f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = new Color(1f, 0.95f, 0.78f, 1f);

            // Status text
            GameObject statusGo = new GameObject("Status", typeof(RectTransform));
            statusGo.transform.SetParent(row.transform, false);
            RectTransform statusRT = statusGo.GetComponent<RectTransform>();
            statusRT.anchorMin = new Vector2(0.55f, 0f); statusRT.anchorMax = new Vector2(0.85f, 1f);
            statusRT.offsetMin = statusRT.offsetMax = Vector2.zero;
            TextMeshProUGUI status = statusGo.AddComponent<TextMeshProUGUI>();
            status.text = "SELECT";
            status.fontSize = 28f;
            status.fontStyle = FontStyles.Bold;
            status.alignment = TextAlignmentOptions.MidlineRight;
            status.color = new Color(1f, 0.96f, 0.78f, 0.95f);

            // Lock icon (text fallback because we don't have a sprite asset yet)
            GameObject lockGo = new GameObject("LockIcon", typeof(RectTransform));
            lockGo.transform.SetParent(row.transform, false);
            RectTransform lockRT = lockGo.GetComponent<RectTransform>();
            lockRT.anchorMin = new Vector2(0.85f, 0f); lockRT.anchorMax = new Vector2(0.95f, 1f);
            lockRT.offsetMin = lockRT.offsetMax = Vector2.zero;
            TextMeshProUGUI lockText = lockGo.AddComponent<TextMeshProUGUI>();
            lockText.text = "X";
            lockText.fontSize = 32f;
            lockText.fontStyle = FontStyles.Bold;
            lockText.alignment = TextAlignmentOptions.Center;
            lockText.color = new Color(1f, 0.42f, 0.42f, 0.95f);

            // Button covers the whole row
            Button btn = row.AddComponent<Button>();
            btn.targetGraphic = rowBg;

            WeatherSelectButton ctrl = row.AddComponent<WeatherSelectButton>();
            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("weatherType").enumValueIndex = (int)weather;
            so.FindProperty("selectButton").objectReferenceValue = btn;
            so.FindProperty("lockIcon").objectReferenceValue = lockGo;
            so.FindProperty("statusText").objectReferenceValue = status;
            so.FindProperty("labelText").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
