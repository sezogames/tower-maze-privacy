using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TowerMaze
{
    public sealed class TowerRotationController : MonoBehaviour
    {
        [SerializeField] private float currentSpeed;
        [SerializeField] private bool simulationActive;
        [SerializeField] private float speedMultiplier = 1f;

        private Quaternion initialRotation;

        public float CurrentSpeed => currentSpeed;

        private void Awake()
        {
            initialRotation = transform.localRotation;
        }

        private void Update()
        {
            if (!simulationActive)
            {
                return;
            }

            transform.Rotate(Vector3.up, currentSpeed * speedMultiplier * Time.deltaTime, Space.Self);
        }

        public void ResetRotation()
        {
            transform.localRotation = initialRotation;
            speedMultiplier = 1f;
        }

        public void SetSimulationActive(bool isActive)
        {
            simulationActive = isActive;
        }

        public void SetSpeed(float speed)
        {
            currentSpeed = speed;
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            speedMultiplier = Mathf.Max(0.01f, multiplier);
        }
    }

    public sealed class TowerSinkController : MonoBehaviour
    {
        private const float StartupSlowdownDuration = 10f;
        private const float StartupSlowdownMultiplier = 0.75f;

        [SerializeField] private bool simulationActive;
        [SerializeField] private float currentSinkSpeed;
        [SerializeField] private float speedMultiplier = 1f;

        private Vector3 initialLocalPosition;
        private float elapsedRunTime;

        public float CurrentSinkSpeed => currentSinkSpeed;

        private void Awake()
        {
            initialLocalPosition = transform.localPosition;
        }

        private void Update()
        {
            if (!simulationActive)
            {
                return;
            }

            elapsedRunTime += Time.deltaTime;
            transform.localPosition += Vector3.down * currentSinkSpeed * Time.deltaTime;
        }

        public void ResetPosition()
        {
            transform.localPosition = initialLocalPosition;
            elapsedRunTime = 0f;
            currentSinkSpeed = 0f;
            speedMultiplier = 1f;
        }

        public void SetSimulationActive(bool isActive)
        {
            simulationActive = isActive;
        }

        public void SetSpeed(float baseSpeed, float perMinuteAcceleration)
        {
            float startupMultiplier = elapsedRunTime < StartupSlowdownDuration
                ? StartupSlowdownMultiplier
                : 1f;
            currentSinkSpeed = (baseSpeed + ((elapsedRunTime / 60f) * perMinuteAcceleration)) * speedMultiplier * startupMultiplier;
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            speedMultiplier = Mathf.Max(0.01f, multiplier);
        }
    }

    internal sealed class SegmentPool
    {
        private readonly Queue<TowerSegment> pooledSegments = new();

        public void Return(TowerSegment segment, Transform poolRoot)
        {
            segment.gameObject.SetActive(false);
            segment.transform.SetParent(poolRoot, false);
            pooledSegments.Enqueue(segment);
        }

        public TowerSegment Get(Transform poolRoot)
        {
            if (pooledSegments.Count == 0)
            {
                GameObject segmentObject = new("TowerSegment");
                segmentObject.transform.SetParent(poolRoot, false);
                return segmentObject.AddComponent<TowerSegment>();
            }

            TowerSegment segment = pooledSegments.Dequeue();
            segment.gameObject.SetActive(true);
            return segment;
        }
    }

    public sealed class TowerMaterials
    {
        private static readonly Color LockedPathColor = new(0.30f, 0.30f, 0.32f, 1f);

        private readonly ThemeDefinition theme;

        public readonly Material PathMaterial;
        public readonly Material MainPathMaterial;
        public readonly Material WallMaterial;
        public readonly Material LavaMaterial;
        public ThemeDefinition Theme => theme;

        public TowerMaterials(ThemeDefinition theme)
        {
            this.theme = theme ?? ScriptableObject.CreateInstance<ThemeDefinition>();
            if (theme == null)
            {
                Debug.LogWarning("[TowerMaterials] ThemeDefinition was null. Using in-memory defaults.");
            }

            PathMaterial = RuntimeMaterialFactory.CreateLit(this.theme, "TowerMaze_PathMaterial");
            MainPathMaterial = RuntimeMaterialFactory.CreateLit(this.theme, "TowerMaze_MainPathMaterial");
            WallMaterial = RuntimeMaterialFactory.CreateLit(this.theme, "TowerMaze_WallMaterial");
            LavaMaterial = RuntimeMaterialFactory.CreateUnlit(this.theme, "TowerMaze_TowerLavaMaterial");

            ApplyTreasureTier(0);

            if (LavaMaterial != null)
            {
                LavaMaterial.SetColor("_BaseColor", this.theme.lavaColor);
                LavaMaterial.SetColor("_Color", this.theme.lavaColor);
            }
        }

        public void ApplyTreasureTier(int emberBalance)
        {
            ApplyTowerSkin(default, emberBalance);
        }

        public void ApplyTowerSkin(TowerSkinDefinition towerSkin, int emberBalance)
        {
            float richness = Mathf.Clamp01(emberBalance / 1200f);
            float wallBlend = Mathf.SmoothStep(0f, 0.9f, richness);
            float pathBlend = Mathf.SmoothStep(0.18f, 1f, richness);
            float mainPathBlend = Mathf.SmoothStep(0.3f, 1f, richness);
            bool useUnifiedTextureSet = string.IsNullOrWhiteSpace(towerSkin.id) || towerSkin.useUnifiedTextureSet;

            Texture2D wallBaseMap = ResolveTexture(towerSkin.wallBaseMapResourcePath, theme.towerWallBaseMap);
            Texture2D wallNormalMap = ResolveTexture(towerSkin.wallNormalMapResourcePath, theme.towerWallNormalMap);
            Vector2 wallScale = towerSkin.wallTextureScale == default ? theme.towerWallTextureScale : towerSkin.wallTextureScale;
            Texture2D pathBaseMap = useUnifiedTextureSet
                ? wallBaseMap
                : ResolveTexture(towerSkin.pathBaseMapResourcePath, wallBaseMap != null ? wallBaseMap : theme.towerPathBaseMap);
            Texture2D pathNormalMap = useUnifiedTextureSet
                ? wallNormalMap
                : ResolveTexture(towerSkin.pathNormalMapResourcePath, wallNormalMap != null ? wallNormalMap : theme.towerPathNormalMap);
            Vector2 pathScale = useUnifiedTextureSet
                ? wallScale
                : (towerSkin.pathTextureScale == default ? theme.towerPathTextureScale : towerSkin.pathTextureScale);
            Texture2D mainPathBaseMap = useUnifiedTextureSet
                ? wallBaseMap
                : ResolveTexture(
                    towerSkin.mainPathBaseMapResourcePath,
                    pathBaseMap != null ? pathBaseMap : (theme.towerMainPathBaseMap != null ? theme.towerMainPathBaseMap : theme.towerPathBaseMap));
            Texture2D mainPathNormalMap = useUnifiedTextureSet
                ? wallNormalMap
                : ResolveTexture(
                    towerSkin.mainPathNormalMapResourcePath,
                    pathNormalMap != null ? pathNormalMap : (theme.towerMainPathNormalMap != null ? theme.towerMainPathNormalMap : theme.towerPathNormalMap));
            Vector2 mainPathScale = useUnifiedTextureSet
                ? wallScale
                : (towerSkin.mainPathTextureScale == default ? theme.towerMainPathTextureScale : towerSkin.mainPathTextureScale);

            Color towerWallTint = string.IsNullOrWhiteSpace(towerSkin.id) ? new Color(0.54f, 0.37f, 0.16f, 1f) : towerSkin.wallTint;
            Color towerPathTint = string.IsNullOrWhiteSpace(towerSkin.id) ? new Color(0.86f, 0.68f, 0.2f, 1f) : towerSkin.pathTint;
            Color towerMainPathTint = string.IsNullOrWhiteSpace(towerSkin.id) ? new Color(1f, 0.86f, 0.34f, 1f) : towerSkin.mainPathTint;

            Color wallColor = Color.Lerp(
                theme.towerWallColor,
                towerWallTint,
                wallBlend);
            Color pathColor = Color.Lerp(
                theme.towerPathColor,
                towerPathTint,
                pathBlend);
            Color mainPathColor = Color.Lerp(
                theme.towerMainPathColor,
                towerMainPathTint,
                mainPathBlend);

            ConfigureSurface(
                WallMaterial,
                wallBaseMap,
                wallNormalMap,
                wallScale,
                wallColor,
                Color.Lerp(Color.black, wallColor * 0.02f, richness * 0.85f),
                Mathf.Lerp(0.7f, 0.92f, richness),
                Mathf.Lerp(0.46f, 0.62f, richness),
                0.75f);

            // Corridor (path) appearance is locked so players always read the rolling
            // surface the same, regardless of which tower skin is equipped.
            pathBaseMap = theme.towerPathBaseMap;
            pathNormalMap = theme.towerPathNormalMap;
            pathScale = theme.towerPathTextureScale == default ? Vector2.one : theme.towerPathTextureScale;
            
            // We force the Main Path to visually match the normal Path perfectly
            // so we don't reveal the puzzle solution to the player (fixing visual borders).
            mainPathBaseMap = pathBaseMap;
            mainPathNormalMap = pathNormalMap;
            mainPathScale = pathScale;
            pathColor = LockedPathColor;
            mainPathColor = LockedPathColor;

            ConfigureSurface(
                PathMaterial,
                pathBaseMap,
                pathNormalMap,
                pathScale,
                pathColor,
                Color.black,
                Mathf.Lerp(0.05f, 0.15f, richness),
                Mathf.Lerp(0.18f, 0.32f, richness),
                0.65f);

            ConfigureSurface(
                MainPathMaterial,
                mainPathBaseMap,
                mainPathNormalMap,
                mainPathScale,
                mainPathColor,
                Color.black,
                Mathf.Lerp(0.05f, 0.15f, richness),
                Mathf.Lerp(0.18f, 0.32f, richness),
                0.65f);
        }

        private static void ConfigureSurface(
            Material material,
            Texture2D baseMap,
            Texture2D normalMap,
            Vector2 textureScale,
            Color baseColor,
            Color emissionColor,
            float metallic,
            float smoothness,
            float normalStrength)
        {
            if (material == null)
            {
                return;
            }

            SetColor(material, "_BaseColor", baseColor);
            SetColor(material, "_Color", baseColor);
            SetFloat(material, "_Metallic", metallic);
            SetFloat(material, "_Smoothness", smoothness);
            SetFloat(material, "_Glossiness", smoothness);

            if (baseMap != null)
            {
                SetTexture(material, "_BaseMap", baseMap);
                SetTexture(material, "_MainTex", baseMap);
                SetTextureScale(material, "_BaseMap", textureScale);
                SetTextureScale(material, "_MainTex", textureScale);
            }
            else
            {
                SetTexture(material, "_BaseMap", null);
                SetTexture(material, "_MainTex", null);
            }

            if (normalMap != null)
            {
                SetTexture(material, "_BumpMap", normalMap);
                SetTextureScale(material, "_BumpMap", textureScale);
                SetFloat(material, "_BumpScale", normalStrength);
                material.EnableKeyword("_NORMALMAP");
            }
            else
            {
                SetTexture(material, "_BumpMap", null);
                material.DisableKeyword("_NORMALMAP");
            }

            SetColor(material, "_EmissionColor", emissionColor);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            if (emissionColor.maxColorComponent > 0.0001f)
            {
                material.EnableKeyword("_EMISSION");
            }
            else
            {
                material.DisableKeyword("_EMISSION");
            }
        }

        private static void SetColor(Material material, string propertyName, Color value)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, value);
            }
        }

        private static void SetFloat(Material material, string propertyName, float value)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static void SetTexture(Material material, string propertyName, Texture texture)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, texture);
            }
        }

        private static void SetTextureScale(Material material, string propertyName, Vector2 scale)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetTextureScale(propertyName, scale);
            }
        }

        private static Texture2D ResolveTexture(string resourcePath, Texture2D fallback)
        {
            Texture2D loadedTexture = BallSkinTextureLibrary.LoadTexture(resourcePath);
            return loadedTexture != null ? loadedTexture : fallback;
        }
    }

    public sealed class TowerSegment : MonoBehaviour
    {
        private sealed class MeshBucket
        {
            public Transform Root;
            public MeshFilter Filter;
            public MeshRenderer Renderer;
            public Mesh Mesh;
        }

        private const float CoinSpinSpeed = 180f;

        private Transform contentRoot;
        private Transform meshesRoot;
        private Transform coinsRoot;
        private readonly Dictionary<int, MazeCoinCollectible> mazeCoins = new();
        private readonly List<MazeCoinCollectible> activeCoinViews = new();
        private readonly List<MazeCoinCollectible> pooledCoins = new();
        private readonly List<CombineInstance> wallCombines = new();
        private readonly List<CombineInstance> pathCombines = new();
        private readonly List<CombineInstance> mainPathCombines = new();
        private MeshBucket wallBucket;
        private MeshBucket pathBucket;
        private MeshBucket mainPathBucket;
        private ThemeDefinition themeDefinition;
        private float coinSpinAngle;

        public SegmentData Data { get; private set; }
        public int SegmentIndex => Data != null ? Data.segmentIndex : -1;

        public void Build(SegmentData data, GameConfig config, TowerMaterials materials, int segmentSeed)
        {
            Data = data;
            name = $"TowerSegment_{data.segmentIndex:000}";
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            themeDefinition = materials != null ? materials.Theme : null;

            EnsureRoots();
            RecycleActiveCoins();

            wallCombines.Clear();
            pathCombines.Clear();
            mainPathCombines.Clear();
            coinSpinAngle = 0f;

            float anglePerCell = config.AnglePerCell;
            float cellHeight = config.CellHeight;
            float arcWidth = config.CellArcWidth;
            float coinSpawnChance = GetZoneAdjustedCoinSpawnChance(config, data.segmentIndex);
            Mesh wallMesh = RoundedBoxMeshCache.GetWallMesh(config);
            Mesh cubeMesh = PrimitiveMeshCache.CubeMesh;
            System.Random coinRandom = new(segmentSeed ^ 0x4B9F31);

            for (int row = 0; row < data.height; row++)
            {
                for (int column = 0; column < data.width; column++)
                {
                    MazeCellKind cellKind = data.GetCell(row, column);
                    bool isPath = cellKind != MazeCellKind.Wall;
                    float thickness = isPath ? config.pathInsetThickness : config.wallThickness;
                    float padding = isPath ? config.pathCellPadding : config.wallCellPadding;
                    float angle = (column + 0.5f) * anglePerCell;
                    Vector3 outward = DirectionForAngle(angle);
                    float outerRadius = isPath ? config.PathOuterRadius : config.towerRadius;
                    Quaternion rotation = Quaternion.LookRotation(outward, Vector3.up);
                    Vector3 position = outward * (outerRadius - (thickness * 0.5f));
                    position.y = (data.segmentIndex * config.segmentHeight) + ((row + 0.5f) * cellHeight);
                    Vector3 scale = new(
                        (arcWidth * padding) + config.cellWidthOverlap,
                        (cellHeight * padding) + config.cellHeightOverlap,
                        thickness + config.cellDepthOverlap);
                    CombineInstance combine = new()
                    {
                        mesh = isPath ? cubeMesh : wallMesh,
                        transform = Matrix4x4.TRS(position, rotation, scale),
                    };

                    switch (cellKind)
                    {
                        case MazeCellKind.MainPath:
                            mainPathCombines.Add(combine);
                            break;
                        case MazeCellKind.Path:
                            pathCombines.Add(combine);
                            break;
                        default:
                            wallCombines.Add(combine);
                            break;
                    }

                    if (isPath)
                    {
                        TryCreateMazeCoin(data, config, row, column, outward, position.y, coinSpawnChance, coinRandom);
                    }
                }
            }

            ApplyBucket(wallBucket, wallCombines, materials != null ? materials.WallMaterial : null, ShadowCastingMode.On, true);
            ApplyBucket(pathBucket, pathCombines, materials != null ? materials.PathMaterial : null, ShadowCastingMode.Off, false);
            // MainPath uses the same material as Path so the solved route cannot be
            // visually distinguished from branches.
            ApplyBucket(mainPathBucket, mainPathCombines, materials != null ? materials.PathMaterial : null, ShadowCastingMode.Off, false);
        }

        private void Update()
        {
            if (activeCoinViews.Count == 0)
            {
                return;
            }

            coinSpinAngle = Mathf.Repeat(coinSpinAngle + (CoinSpinSpeed * Time.deltaTime), 360f);
            for (int index = 0; index < activeCoinViews.Count; index++)
            {
                MazeCoinCollectible coin = activeCoinViews[index];
                if (coin != null && coin.gameObject.activeSelf)
                {
                    coin.SetSpinAngle(coinSpinAngle);
                }
            }
        }

        public void RefreshMaterials(TowerMaterials materials)
        {
            if (materials == null)
            {
                return;
            }

            if (wallBucket?.Renderer != null)
            {
                wallBucket.Renderer.sharedMaterial = materials.WallMaterial;
            }

            if (pathBucket?.Renderer != null)
            {
                pathBucket.Renderer.sharedMaterial = materials.PathMaterial;
            }

            if (mainPathBucket?.Renderer != null)
            {
                mainPathBucket.Renderer.sharedMaterial = materials.PathMaterial;
            }

            themeDefinition = materials.Theme;
            for (int index = 0; index < activeCoinViews.Count; index++)
            {
                MazeCoinCollectible coin = activeCoinViews[index];
                if (coin != null)
                {
                    coin.Initialize(coin.LocalRow, coin.Column, coin.RewardAmount, themeDefinition);
                    coin.SetSpinAngle(coinSpinAngle);
                }
            }
        }

        public bool TryCollectCoin(int localRow, int column, out int rewardAmount, out Vector3 worldPosition)
        {
            rewardAmount = 0;
            worldPosition = Vector3.zero;

            if (Data == null || localRow < 0 || localRow >= Data.height)
            {
                return false;
            }

            int wrappedColumn = Data.WrapColumn(column);
            int key = GetCoinKey(localRow, wrappedColumn, Data.width);
            if (!mazeCoins.TryGetValue(key, out MazeCoinCollectible coin) || coin == null)
            {
                return false;
            }

            worldPosition = coin.transform.position;
            if (!coin.TryCollect(out rewardAmount))
            {
                return false;
            }

            mazeCoins.Remove(key);
            activeCoinViews.Remove(coin);
            ReturnCoinToPool(coin);
            return rewardAmount > 0;
        }

        private void EnsureRoots()
        {
            contentRoot ??= transform;
            meshesRoot ??= EnsureChild(contentRoot, "Meshes");
            coinsRoot ??= EnsureChild(contentRoot, "Coins");
            wallBucket ??= CreateBucket("Walls");
            pathBucket ??= CreateBucket("Paths");
            mainPathBucket ??= CreateBucket("MainPaths");
        }

        private MeshBucket CreateBucket(string bucketName)
        {
            Transform bucketRoot = EnsureChild(meshesRoot, bucketName);
            MeshFilter filter = bucketRoot.GetComponent<MeshFilter>();
            if (filter == null)
            {
                filter = bucketRoot.gameObject.AddComponent<MeshFilter>();
            }

            MeshRenderer renderer = bucketRoot.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = bucketRoot.gameObject.AddComponent<MeshRenderer>();
            }

            Mesh mesh = filter.sharedMesh;
            if (mesh == null)
            {
                mesh = new Mesh
                {
                    name = $"{name}_{bucketName}_Mesh",
                    indexFormat = IndexFormat.UInt32,
                };
                mesh.MarkDynamic();
                filter.sharedMesh = mesh;
            }

            return new MeshBucket
            {
                Root = bucketRoot,
                Filter = filter,
                Renderer = renderer,
                Mesh = mesh,
            };
        }

        private void ApplyBucket(MeshBucket bucket, List<CombineInstance> combines, Material material, ShadowCastingMode shadowCastingMode, bool receiveShadows)
        {
            if (bucket == null)
            {
                return;
            }

            if (combines.Count == 0)
            {
                if (bucket.Filter != null)
                {
                    bucket.Filter.sharedMesh = null;
                }

                if (bucket.Root != null)
                {
                    bucket.Root.gameObject.SetActive(false);
                }

                return;
            }

            bucket.Mesh.Clear();
            bucket.Mesh.indexFormat = IndexFormat.UInt32;
            bucket.Mesh.CombineMeshes(combines.ToArray(), true, true, false);
            bucket.Filter.sharedMesh = bucket.Mesh;
            bucket.Renderer.sharedMaterial = material;
            bucket.Renderer.shadowCastingMode = shadowCastingMode;
            bucket.Renderer.receiveShadows = receiveShadows;
            bucket.Root.gameObject.SetActive(true);
        }

        private void RecycleActiveCoins()
        {
            for (int index = 0; index < activeCoinViews.Count; index++)
            {
                ReturnCoinToPool(activeCoinViews[index]);
            }

            activeCoinViews.Clear();
            mazeCoins.Clear();
        }

        private MazeCoinCollectible GetPooledCoin()
        {
            int lastIndex = pooledCoins.Count - 1;
            if (lastIndex >= 0)
            {
                MazeCoinCollectible pooledCoin = pooledCoins[lastIndex];
                pooledCoins.RemoveAt(lastIndex);
                return pooledCoin;
            }

            GameObject coinObject = new("MazeCoin");
            coinObject.transform.SetParent(coinsRoot, false);
            return coinObject.AddComponent<MazeCoinCollectible>();
        }

        private void ReturnCoinToPool(MazeCoinCollectible coin)
        {
            if (coin == null)
            {
                return;
            }

            coin.gameObject.SetActive(false);
            coin.transform.SetParent(coinsRoot, false);
            pooledCoins.Add(coin);
        }

        private void TryCreateMazeCoin(
            SegmentData data,
            GameConfig config,
            int row,
            int column,
            Vector3 outward,
            float height,
            float spawnChance,
            System.Random coinRandom)
        {
            if (spawnChance <= 0f)
            {
                return;
            }

            if (data.segmentIndex == 0 && row < 2)
            {
                return;
            }

            if (coinRandom.NextDouble() > spawnChance)
            {
                return;
            }

            Vector3 position = outward * (config.HeroLaneRadius + (HeroVisualController.BallRadius * 0.35f));
            position.y = height;

            MazeCoinCollectible coin = GetPooledCoin();
            coin.transform.SetParent(coinsRoot, false);
            coin.transform.localPosition = position;
            coin.transform.localRotation = Quaternion.LookRotation(outward, Vector3.up);
            coin.Initialize(row, column, Mathf.Max(1, config.mazeCoinReward), themeDefinition);
            coin.SetSpinAngle(coinSpinAngle);
            mazeCoins[GetCoinKey(row, column, data.width)] = coin;
            activeCoinViews.Add(coin);
        }

        private static int GetCoinKey(int row, int column, int width)
        {
            return (row * width) + column;
        }

        private static float GetZoneAdjustedCoinSpawnChance(GameConfig config, int segmentIndex)
        {
            float baseChance = Mathf.Clamp01(config.mazeCoinSpawnChance);
            if (baseChance <= 0f)
            {
                return 0f;
            }

            int zoneIndex = Mathf.Max(0, segmentIndex / Mathf.Max(1, config.segmentsPerZone));
            float normalizedZone = Mathf.Clamp01(zoneIndex / 6f);
            float earlyChance = baseChance * 0.3f;
            float lateChance = Mathf.Min(0.2f, baseChance * 1.35f);
            return Mathf.Lerp(earlyChance, lateChance, normalizedZone);
        }

        private static Transform EnsureChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                return child;
            }

            GameObject childObject = new(childName);
            childObject.transform.SetParent(parent, false);
            return childObject.transform;
        }

        private static Vector3 DirectionForAngle(float angleDegrees)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians)).normalized;
        }
    }

    internal static class PrimitiveMeshCache
    {
        private static Mesh cubeMesh;

        public static Mesh CubeMesh => cubeMesh ??= CreateCubeMesh();

        private static Mesh CreateCubeMesh()
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            MeshFilter meshFilter = cube.GetComponent<MeshFilter>();
            Mesh mesh = meshFilter != null
                ? Object.Instantiate(meshFilter.sharedMesh)
                : new Mesh();
            mesh.name = "TowerMaze_CachedCube";

            if (Application.isPlaying)
            {
                Object.Destroy(cube);
            }
            else
            {
                Object.DestroyImmediate(cube);
            }

            return mesh;
        }
    }

    internal static class RoundedBoxMeshCache
    {
        private static Mesh wallMesh;
        private static float cachedRoundness = -1f;
        private static int cachedSegments = -1;

        public static Mesh GetWallMesh(GameConfig config)
        {
            float roundness = Mathf.Clamp(config.wallCornerRoundness, 0f, 0.45f);
            int segments = Mathf.Clamp(config.wallCornerSegments, 1, 8);

            if (wallMesh == null || !Mathf.Approximately(cachedRoundness, roundness) || cachedSegments != segments)
            {
                cachedRoundness = roundness;
                cachedSegments = segments;
                wallMesh = BuildRoundedBoxMesh(roundness, segments);
            }

            return wallMesh;
        }

        private static Mesh BuildRoundedBoxMesh(float roundness, int segments)
        {
            List<Vector3> vertices = new();
            List<Vector3> normals = new();
            List<Vector2> uvs = new();
            List<int> triangles = new();

            FaceDefinition[] faces =
            {
                new(Vector3.forward, Vector3.right, Vector3.up),
                new(Vector3.back, Vector3.left, Vector3.up),
                new(Vector3.right, Vector3.back, Vector3.up),
                new(Vector3.left, Vector3.forward, Vector3.up),
                new(Vector3.up, Vector3.right, Vector3.back),
                new(Vector3.down, Vector3.right, Vector3.forward)
            };

            foreach (FaceDefinition face in faces)
            {
                AppendFace(vertices, normals, uvs, triangles, face, roundness, segments);
            }

            Mesh mesh = new()
            {
                name = $"RoundedWallBox_{roundness:0.00}_{segments}"
            };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AppendFace(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<int> triangles,
            FaceDefinition face,
            float roundness,
            int segments)
        {
            int baseVertex = vertices.Count;
            int rowSize = segments + 1;

            for (int y = 0; y <= segments; y++)
            {
                float v = (float)y / segments;
                float localV = Mathf.Lerp(-0.5f, 0.5f, v);

                for (int x = 0; x <= segments; x++)
                {
                    float u = (float)x / segments;
                    float localU = Mathf.Lerp(-0.5f, 0.5f, u);
                    Vector3 cubePoint = (face.normal * 0.5f) + (face.axisA * localU) + (face.axisB * localV);
                    GetRoundedPoint(cubePoint, face.normal, roundness, out Vector3 vertex, out Vector3 normal);
                    vertices.Add(vertex);
                    normals.Add(normal);
                    uvs.Add(new Vector2(u, v));
                }
            }

            bool standardWindingIsOutward = Vector3.Dot(Vector3.Cross(face.axisA, face.axisB), face.normal) > 0f;
            for (int y = 0; y < segments; y++)
            {
                for (int x = 0; x < segments; x++)
                {
                    int a = baseVertex + x + (y * rowSize);
                    int b = a + 1;
                    int c = a + rowSize;
                    int d = c + 1;

                    if (standardWindingIsOutward)
                    {
                        triangles.Add(a);
                        triangles.Add(b);
                        triangles.Add(c);
                        triangles.Add(b);
                        triangles.Add(d);
                        triangles.Add(c);
                    }
                    else
                    {
                        triangles.Add(a);
                        triangles.Add(c);
                        triangles.Add(b);
                        triangles.Add(b);
                        triangles.Add(c);
                        triangles.Add(d);
                    }
                }
            }
        }

        private static void GetRoundedPoint(Vector3 cubePoint, Vector3 fallbackNormal, float roundness, out Vector3 vertex, out Vector3 normal)
        {
            if (roundness <= 0.0001f)
            {
                vertex = cubePoint;
                normal = fallbackNormal;
                return;
            }

            float min = -0.5f + roundness;
            float max = 0.5f - roundness;
            Vector3 inner = new(
                Mathf.Clamp(cubePoint.x, min, max),
                Mathf.Clamp(cubePoint.y, min, max),
                Mathf.Clamp(cubePoint.z, min, max));

            Vector3 delta = cubePoint - inner;
            if (delta.sqrMagnitude <= 0.000001f)
            {
                vertex = cubePoint;
                normal = fallbackNormal;
                return;
            }

            normal = delta.normalized;
            vertex = inner + (normal * roundness);
        }

        private readonly struct FaceDefinition
        {
            public readonly Vector3 normal;
            public readonly Vector3 axisA;
            public readonly Vector3 axisB;

            public FaceDefinition(Vector3 normal, Vector3 axisA, Vector3 axisB)
            {
                this.normal = normal;
                this.axisA = axisA;
                this.axisB = axisB;
            }
        }
    }

    public sealed class TowerGenerator : MonoBehaviour
    {
        [SerializeField] private Transform activeSegmentsRoot;
        [SerializeField] private Transform pooledSegmentsRoot;
        [SerializeField] private TowerRotationController rotationController;
        [SerializeField] private TowerSinkController sinkController;
        [SerializeField] private int debugDifficultyTier;

        private readonly Dictionary<int, SegmentData> segmentDataByIndex = new();
        private readonly Dictionary<int, TowerSegment> activeSegments = new();
        private readonly SegmentPool segmentPool = new();
        private readonly MazeGenerator mazeGenerator = new();

        private GameConfig config;
        private DifficultyProfile difficultyProfile;
        private ThemeDefinition theme;
        private TowerMaterials materials;
        private int highestSpawnedSegment = -1;
        private int lastExitColumn;
        private int baseSeed;
        private float difficultyOffset;
        private int zoneOffset;

        public void SetChapterDifficulty(float heightOffset, int zoneOff)
        {
            difficultyOffset = heightOffset;
            zoneOffset = zoneOff;
        }

        public Transform TowerSpace => transform;
        public TowerRotationController RotationController => rotationController;
        public TowerSinkController SinkController => sinkController;
        public int DebugDifficultyTier => debugDifficultyTier;

        public void Initialize(GameConfig gameConfig, DifficultyProfile profile, ThemeDefinition definition)
        {
            config = gameConfig != null ? gameConfig : Resources.Load<GameConfig>("TowerMaze/GameConfig");
            difficultyProfile = profile != null
                ? profile
                : (Resources.Load<DifficultyProfile>("TowerMaze/DifficultyProfile")
                    ?? Resources.Load<DifficultyProfile>("TowerMaze/StandardDifficultyProfile"));
            theme = definition != null ? definition : ScriptableObject.CreateInstance<ThemeDefinition>();
            materials = new TowerMaterials(theme);

            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GameConfig>();
                Debug.LogWarning("[TowerGenerator] GameConfig was null. Using in-memory defaults.");
            }

            if (difficultyProfile == null)
            {
                difficultyProfile = ScriptableObject.CreateInstance<DifficultyProfile>();
                Debug.LogWarning("[TowerGenerator] DifficultyProfile was null. Using in-memory defaults.");
            }
            
            if (activeSegmentsRoot == null) activeSegmentsRoot = CreateChild("ActiveSegments");
            if (pooledSegmentsRoot == null) pooledSegmentsRoot = CreateChild("PooledSegments");
            if (rotationController == null) rotationController = GetComponent<TowerRotationController>();
            if (sinkController == null) sinkController = GetComponentInParent<TowerSinkController>();
        }

        public void ApplyTreasureVisuals(int emberBalance)
        {
            materials?.ApplyTreasureTier(emberBalance);
            RefreshActiveSegmentMaterials();
        }

        public void ApplyTowerSkin(TowerSkinDefinition towerSkin, int emberBalance)
        {
            materials?.ApplyTowerSkin(towerSkin, emberBalance);
            RefreshActiveSegmentMaterials();
        }

        public void ResetRun(int seed)
        {
            baseSeed = seed;
            lastExitColumn = config.mazeWidthCells / 2;
            highestSpawnedSegment = -1;
            debugDifficultyTier = 0;

            foreach (KeyValuePair<int, TowerSegment> kvp in activeSegments)
            {
                segmentPool.Return(kvp.Value, pooledSegmentsRoot);
            }

            activeSegments.Clear();
            segmentDataByIndex.Clear();

            rotationController.ResetRotation();
            sinkController.ResetPosition();

            for (int segmentIndex = 0; segmentIndex < config.initialSegments; segmentIndex++)
            {
                SpawnSegment(segmentIndex);
            }
        }

        public void UpdateStreaming(float playerHeight)
        {
            int playerSegment = Mathf.Max(0, Mathf.FloorToInt(playerHeight / config.segmentHeight));
            int targetHighest = playerSegment + config.spawnAheadSegments;

            while (highestSpawnedSegment < targetHighest)
            {
                SpawnSegment(highestSpawnedSegment + 1);
            }

            int lowestKept = Mathf.Max(0, playerSegment - config.keepBehindSegments);
            List<int> toRecycle = new();
            foreach (int segmentIndex in activeSegments.Keys)
            {
                if (segmentIndex < lowestKept)
                {
                    toRecycle.Add(segmentIndex);
                }
            }

            foreach (int segmentIndex in toRecycle)
            {
                TowerSegment segment = activeSegments[segmentIndex];
                activeSegments.Remove(segmentIndex);
                segmentDataByIndex.Remove(segmentIndex);
                segmentPool.Return(segment, pooledSegmentsRoot);
            }
        }

        public void UpdateDifficulty(float playerHeight)
        {
            if (difficultyProfile == null || rotationController == null || sinkController == null || config == null)
            {
                return;
            }

            DifficultySettings settings = difficultyProfile.Evaluate(playerHeight + difficultyOffset);
            debugDifficultyTier = difficultyProfile.GetBandIndex(playerHeight);
            rotationController.SetSpeed(settings.rotationSpeed);
            sinkController.SetSpeed(settings.sinkSpeed, config.sinkAccelerationPerMinute);
        }

        public bool IsPathOpen(float angleDegrees, float towerHeight, float angleClearanceDegrees = 0f, float heightClearance = 0f)
        {
            float[] sampleAngles = angleClearanceDegrees > 0f
                ? new[] { angleDegrees, angleDegrees - angleClearanceDegrees, angleDegrees + angleClearanceDegrees }
                : new[] { angleDegrees };
            float[] sampleHeights = heightClearance > 0f
                ? new[] { towerHeight, towerHeight - heightClearance, towerHeight + heightClearance }
                : new[] { towerHeight };

            foreach (float sampleHeight in sampleHeights)
            {
                foreach (float sampleAngle in sampleAngles)
                {
                    if (!IsPathOpenAtSample(sampleAngle, sampleHeight))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool IsPathOpenAtSample(float angleDegrees, float towerHeight)
        {
            if (towerHeight < 0f)
            {
                return false;
            }

            int globalRow = Mathf.Max(0, Mathf.FloorToInt(towerHeight / config.CellHeight));
            int segmentIndex = globalRow / config.mazeHeightCells;
            int localRow = globalRow % config.mazeHeightCells;
            if (!segmentDataByIndex.TryGetValue(segmentIndex, out SegmentData data))
            {
                return false;
            }

            int column = AngleToColumn(angleDegrees);
            return data.IsOpen(localRow, column);
        }

        public bool TryFindOpenPosition(float preferredAngle, float minHeight, out float safeAngle, out float safeHeight)
        {
            int preferredColumn = AngleToColumn(preferredAngle);
            int startRow = Mathf.Max(0, Mathf.FloorToInt(minHeight / config.CellHeight));
            int endRow = startRow + (config.mazeHeightCells * 2);

            for (int globalRow = startRow; globalRow <= endRow; globalRow++)
            {
                int segmentIndex = globalRow / config.mazeHeightCells;
                int localRow = globalRow % config.mazeHeightCells;
                if (!segmentDataByIndex.TryGetValue(segmentIndex, out SegmentData data))
                {
                    continue;
                }

                for (int offset = 0; offset <= config.mazeWidthCells / 2; offset++)
                {
                    int[] columns = offset == 0
                        ? new[] { preferredColumn }
                        : new[] { preferredColumn + offset, preferredColumn - offset };

                    foreach (int column in columns)
                    {
                        int wrappedColumn = data.WrapColumn(column);
                        if (!data.IsOpen(localRow, wrappedColumn))
                        {
                            continue;
                        }

                        safeAngle = ColumnToAngleCenter(wrappedColumn);
                        safeHeight = ((globalRow + 0.5f) * config.CellHeight);
                        return true;
                    }
                }
            }

            safeAngle = Mathf.Repeat(preferredAngle, 360f);
            safeHeight = minHeight;
            return false;
        }

        public float ColumnToAngleCenter(int column)
        {
            return (config.AnglePerCell * (Mathf.Repeat(column, config.mazeWidthCells) + 0.5f));
        }

        public float HeightForRowCenter(int globalRow)
        {
            return (globalRow + 0.5f) * config.CellHeight;
        }

        public bool TryGetOpenCellCenter(float angleDegrees, float towerHeight, out float centerAngle, out float centerHeight)
        {
            centerAngle = Mathf.Repeat(angleDegrees, 360f);
            centerHeight = Mathf.Max(0f, towerHeight);

            if (towerHeight < 0f)
            {
                return false;
            }

            int globalRow = Mathf.Max(0, Mathf.FloorToInt(towerHeight / config.CellHeight));
            int segmentIndex = globalRow / config.mazeHeightCells;
            int localRow = globalRow % config.mazeHeightCells;
            if (!segmentDataByIndex.TryGetValue(segmentIndex, out SegmentData data))
            {
                return false;
            }

            int column = AngleToColumn(angleDegrees);
            if (!data.IsOpen(localRow, column))
            {
                return false;
            }

            centerAngle = ColumnToAngleCenter(column);
            centerHeight = HeightForRowCenter(globalRow);
            return true;
        }

        public bool TryFindHorizontalTurnTarget(
            float currentAngle,
            float currentHeight,
            float horizontalInput,
            float verticalInput,
            int maxRowOffset,
            int maxColumnOffset,
            out float resolvedAngle,
            out float resolvedHeight)
        {
            resolvedAngle = Mathf.Repeat(currentAngle, 360f);
            resolvedHeight = Mathf.Max(0f, currentHeight);

            if (config == null || currentHeight < 0f || Mathf.Abs(horizontalInput) < 0.01f)
            {
                return false;
            }

            int currentRow = Mathf.Max(0, Mathf.FloorToInt(currentHeight / config.CellHeight));
            int currentColumn = AngleToColumn(currentAngle);
            int signedDirection = horizontalInput > 0f ? 1 : -1;
            int rowLimit = Mathf.Max(0, maxRowOffset);
            int columnLimit = Mathf.Clamp(maxColumnOffset, 1, 1);
            float bestScore = float.MaxValue;
            bool found = false;

            for (int rowOffset = -rowLimit; rowOffset <= rowLimit; rowOffset++)
            {
                int globalRow = currentRow + rowOffset;
                if (globalRow < 0)
                {
                    continue;
                }

                int segmentIndex = globalRow / config.mazeHeightCells;
                int localRow = globalRow % config.mazeHeightCells;
                if (!segmentDataByIndex.TryGetValue(segmentIndex, out SegmentData data))
                {
                    continue;
                }

                if (!data.IsOpen(localRow, currentColumn))
                {
                    continue;
                }

                int adjacentColumn = data.WrapColumn(currentColumn + signedDirection);
                for (int step = 1; step <= columnLimit; step++)
                {
                    int candidateColumn = adjacentColumn;
                    if (!data.IsOpen(localRow, candidateColumn))
                    {
                        continue;
                    }

                    float rowDistanceCells = Mathf.Abs(currentHeight - HeightForRowCenter(globalRow)) / config.CellHeight;
                    float directionalBias = 0f;
                    if (Mathf.Abs(verticalInput) > 0.01f && rowOffset != 0 && Mathf.Sign(rowOffset) == Mathf.Sign(verticalInput))
                    {
                        directionalBias = -0.2f;
                    }

                    float score = (step - 1) * 0.55f + (rowDistanceCells * 1.35f) + directionalBias;
                    if (score >= bestScore)
                    {
                        continue;
                    }

                    bestScore = score;
                    resolvedAngle = ColumnToAngleCenter(candidateColumn);
                    resolvedHeight = HeightForRowCenter(globalRow);
                    found = true;
                }
            }

            return found;
        }

        public bool TryFindVerticalTurnTarget(
            float currentAngle,
            float currentHeight,
            float verticalInput,
            float horizontalInput,
            int maxColumnOffset,
            out float resolvedAngle,
            out float resolvedHeight)
        {
            resolvedAngle = Mathf.Repeat(currentAngle, 360f);
            resolvedHeight = Mathf.Max(0f, currentHeight);

            if (config == null || currentHeight < 0f || Mathf.Abs(verticalInput) < 0.01f)
            {
                return false;
            }

            int currentRow = Mathf.Max(0, Mathf.FloorToInt(currentHeight / config.CellHeight));
            int currentColumn = AngleToColumn(currentAngle);
            int targetRow = currentRow + (verticalInput > 0f ? 1 : -1);
            if (targetRow < 0)
            {
                return false;
            }

            int currentSegmentIndex = currentRow / config.mazeHeightCells;
            int currentLocalRow = currentRow % config.mazeHeightCells;
            if (!segmentDataByIndex.TryGetValue(currentSegmentIndex, out SegmentData currentData) ||
                !currentData.IsOpen(currentLocalRow, currentColumn))
            {
                return false;
            }

            int targetSegmentIndex = targetRow / config.mazeHeightCells;
            int targetLocalRow = targetRow % config.mazeHeightCells;
            if (!segmentDataByIndex.TryGetValue(targetSegmentIndex, out SegmentData targetData) ||
                targetData == null)
            {
                return false;
            }

            float bestScore = float.MaxValue;
            bool found = false;
            int columnLimit = Mathf.Max(0, maxColumnOffset);

            for (int columnOffset = -columnLimit; columnOffset <= columnLimit; columnOffset++)
            {
                int candidateColumn = currentData.WrapColumn(currentColumn + columnOffset);
                if (!targetData.IsOpen(targetLocalRow, candidateColumn) ||
                    !IsHorizontalCorridorOpen(currentData, currentLocalRow, currentColumn, columnOffset))
                {
                    continue;
                }

                float candidateAngle = ColumnToAngleCenter(candidateColumn);
                float angleDistanceCells = Mathf.Abs(Mathf.DeltaAngle(currentAngle, candidateAngle)) / config.AnglePerCell;
                float horizontalBias = 0f;
                if (Mathf.Abs(horizontalInput) > 0.01f &&
                    columnOffset != 0 &&
                    Mathf.Sign(columnOffset) == Mathf.Sign(horizontalInput))
                {
                    horizontalBias = -0.15f;
                }

                float score = angleDistanceCells + (Mathf.Abs(columnOffset) * 0.1f) + horizontalBias;
                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                resolvedAngle = candidateAngle;
                resolvedHeight = HeightForRowCenter(targetRow);
                found = true;
            }

            return found;
        }

        private static bool IsHorizontalCorridorOpen(SegmentData data, int row, int startColumn, int columnOffset)
        {
            if (data == null)
            {
                return false;
            }

            int stepDirection = columnOffset >= 0 ? 1 : -1;
            for (int step = 0; step <= Mathf.Abs(columnOffset); step++)
            {
                int wrappedColumn = data.WrapColumn(startColumn + (step * stepDirection));
                if (!data.IsOpen(row, wrappedColumn))
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryFindNearestOpenCellCenter(
            float targetAngle,
            float targetHeight,
            int maxColumnOffset,
            int maxRowOffset,
            out float resolvedAngle,
            out float resolvedHeight)
        {
            resolvedAngle = Mathf.Repeat(targetAngle, 360f);
            resolvedHeight = Mathf.Max(0f, targetHeight);

            if (config == null || targetHeight < 0f)
            {
                return false;
            }

            int targetRow = Mathf.Max(0, Mathf.FloorToInt(targetHeight / config.CellHeight));
            int targetColumn = AngleToColumn(targetAngle);
            float bestScore = float.MaxValue;
            bool found = false;
            int rowLimit = Mathf.Max(0, maxRowOffset);
            int columnLimit = Mathf.Max(0, maxColumnOffset);

            for (int rowOffset = -rowLimit; rowOffset <= rowLimit; rowOffset++)
            {
                int globalRow = targetRow + rowOffset;
                if (globalRow < 0)
                {
                    continue;
                }

                int segmentIndex = globalRow / config.mazeHeightCells;
                int localRow = globalRow % config.mazeHeightCells;
                if (!segmentDataByIndex.TryGetValue(segmentIndex, out SegmentData data))
                {
                    continue;
                }

                for (int columnOffset = -columnLimit; columnOffset <= columnLimit; columnOffset++)
                {
                    int wrappedColumn = data.WrapColumn(targetColumn + columnOffset);
                    if (!data.IsOpen(localRow, wrappedColumn))
                    {
                        continue;
                    }

                    float candidateAngle = ColumnToAngleCenter(wrappedColumn);
                    float candidateHeight = HeightForRowCenter(globalRow);
                    float angleDistanceCells = Mathf.Abs(Mathf.DeltaAngle(targetAngle, candidateAngle)) / config.AnglePerCell;
                    float heightDistanceCells = Mathf.Abs(targetHeight - candidateHeight) / config.CellHeight;
                    float score = (angleDistanceCells * angleDistanceCells) + (heightDistanceCells * heightDistanceCells * 1.15f);

                    if (score >= bestScore)
                    {
                        continue;
                    }

                    bestScore = score;
                    resolvedAngle = candidateAngle;
                    resolvedHeight = candidateHeight;
                    found = true;
                }
            }

            return found;
        }

        public bool TryCollectCoin(float angleDegrees, float towerHeight, out int rewardAmount, out Vector3 worldPosition)
        {
            rewardAmount = 0;
            worldPosition = Vector3.zero;

            if (config == null || towerHeight < 0f)
            {
                return false;
            }

            int column = AngleToColumn(angleDegrees);
            int globalRow = Mathf.Max(0, Mathf.FloorToInt(towerHeight / config.CellHeight));

            for (int rowOffset = -1; rowOffset <= 1; rowOffset++)
            {
                int sampledGlobalRow = globalRow + rowOffset;
                if (sampledGlobalRow < 0)
                {
                    continue;
                }

                int segmentIndex = sampledGlobalRow / config.mazeHeightCells;
                int localRow = sampledGlobalRow % config.mazeHeightCells;
                if (!activeSegments.TryGetValue(segmentIndex, out TowerSegment segment))
                {
                    continue;
                }

                if (segment.TryCollectCoin(localRow, column, out rewardAmount, out worldPosition))
                {
                    return true;
                }
            }

            return false;
        }

        private int AngleToColumn(float angleDegrees)
        {
            float wrapped = Mathf.Repeat(angleDegrees, 360f);
            return Mathf.FloorToInt(wrapped / config.AnglePerCell) % config.mazeWidthCells;
        }

        private void SpawnSegment(int segmentIndex)
        {
            if (config == null || materials == null)
            {
                Debug.LogError("[TowerGenerator] SpawnSegment skipped because generator dependencies are missing.");
                return;
            }

            int zoneIndex = GetZoneIndexForSegment(segmentIndex);
            int segmentSeed = GetSegmentSeed(segmentIndex, zoneIndex);
            SegmentData data = segmentIndex == 0
                ? mazeGenerator.CreateTutorialSegment(config, theme, segmentIndex, lastExitColumn)
                : mazeGenerator.Generate(
                    config,
                    difficultyProfile,
                    theme,
                    segmentIndex,
                    zoneIndex,
                    lastExitColumn,
                    segmentSeed);

            lastExitColumn = data.exitColumn;
            highestSpawnedSegment = Mathf.Max(highestSpawnedSegment, segmentIndex);
            segmentDataByIndex[segmentIndex] = data;

            TowerSegment segment = segmentPool.Get(pooledSegmentsRoot);
            segment.transform.SetParent(activeSegmentsRoot, false);
            segment.Build(data, config, materials, segmentSeed);
            activeSegments[segmentIndex] = segment;
        }

        private Transform CreateChild(string childName)
        {
            GameObject child = new(childName);
            child.transform.SetParent(transform, false);
            return child.transform;
        }

        private void RefreshActiveSegmentMaterials()
        {
            if (materials == null)
            {
                return;
            }

            foreach (TowerSegment segment in activeSegments.Values)
            {
                segment.RefreshMaterials(materials);
            }
        }

        private int GetZoneIndexForSegment(int segmentIndex)
        {
            int segmentsPerZone = Mathf.Max(1, config.segmentsPerZone);
            return Mathf.Max(0, segmentIndex / segmentsPerZone) + zoneOffset;
        }

        private int GetSegmentSeed(int segmentIndex, int zoneIndex)
        {
            int zoneSeed = HashSeed(baseSeed, zoneIndex + 1);
            return HashSeed(zoneSeed, segmentIndex + 1);
        }

        private static int HashSeed(int a, int b)
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + a;
                hash = (hash * 31) + b;
                hash ^= (hash << 13);
                hash ^= (hash >> 17);
                hash ^= (hash << 5);
                return Mathf.Abs(hash == int.MinValue ? int.MaxValue : hash);
            }
        }
    }
}
