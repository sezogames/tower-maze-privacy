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
            currentSinkSpeed = (baseSpeed + ((elapsedRunTime / 60f) * perMinuteAcceleration)) * speedMultiplier;
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
        private readonly ThemeDefinition theme;

        public readonly Material PathMaterial;
        public readonly Material MainPathMaterial;
        public readonly Material WallMaterial;
        public readonly Material LavaMaterial;

        public TowerMaterials(ThemeDefinition theme)
        {
            this.theme = theme;
            Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? lit;

            if (lit == null)
            {
                Debug.LogWarning("[TowerMaterials] Could not find Lit shader! Falling back to internal fallback.");
                lit = Shader.Find("Hidden/InternalErrorShader");
            }
            if (unlit == null) unlit = lit;

            PathMaterial = new Material(lit);
            MainPathMaterial = new Material(lit);
            WallMaterial = new Material(lit);
            LavaMaterial = new Material(unlit);

            ApplyTreasureTier(0);

            if (LavaMaterial != null)
            {
                LavaMaterial.SetColor("_BaseColor", theme.lavaColor);
                LavaMaterial.SetColor("_Color", theme.lavaColor);
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
                Color.Lerp(Color.black, wallColor * 0.08f, richness * 0.85f),
                Mathf.Lerp(0.7f, 0.92f, richness),
                Mathf.Lerp(0.46f, 0.62f, richness),
                0.75f);
            ConfigureSurface(
                PathMaterial,
                pathBaseMap,
                pathNormalMap,
                pathScale,
                pathColor,
                Color.Lerp(pathColor * 0.025f, pathColor * 0.13f, richness),
                Mathf.Lerp(0.82f, 0.98f, richness),
                Mathf.Lerp(0.62f, 0.86f, richness),
                0.65f);
            ConfigureSurface(
                MainPathMaterial,
                mainPathBaseMap,
                mainPathNormalMap,
                mainPathScale,
                mainPathColor,
                Color.Lerp(mainPathColor * 0.06f, mainPathColor * 0.22f, richness),
                Mathf.Lerp(0.88f, 1f, richness),
                Mathf.Lerp(0.72f, 0.94f, richness),
                0.9f);
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
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, value);
            }
        }

        private static void SetFloat(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
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

        private static Texture2D ResolveTexture(string resourcePath, Texture2D fallback)
        {
            Texture2D loadedTexture = BallSkinTextureLibrary.LoadTexture(resourcePath);
            return loadedTexture != null ? loadedTexture : fallback;
        }
    }

    public sealed class TowerSegment : MonoBehaviour
    {
        private struct CellRendererBinding
        {
            public Renderer renderer;
            public MazeCellKind cellKind;
        }

        private Transform contentRoot;
        private readonly List<CellRendererBinding> cellRenderers = new();

        public SegmentData Data { get; private set; }
        public int SegmentIndex => Data != null ? Data.segmentIndex : -1;

        public void Build(SegmentData data, GameConfig config, TowerMaterials materials)
        {
            Data = data;
            name = $"TowerSegment_{data.segmentIndex:000}";
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            contentRoot ??= transform;
            ClearChildren();
            cellRenderers.Clear();

            float anglePerCell = config.AnglePerCell;
            float cellHeight = config.CellHeight;
            float arcWidth = config.CellArcWidth;

            for (int row = 0; row < data.height; row++)
            {
                for (int column = 0; column < data.width; column++)
                {
                    MazeCellKind cellKind = data.GetCell(row, column);
                    bool isPath = cellKind != MazeCellKind.Wall;
                    float thickness = isPath ? config.pathInsetThickness : config.wallThickness;
                    float padding = isPath ? config.pathCellPadding : config.wallCellPadding;
                    float angle = (column + 0.5f) * anglePerCell;
                    float outerRadius = isPath ? config.PathOuterRadius : config.towerRadius;
                    Quaternion rotation = Quaternion.LookRotation(DirectionForAngle(angle), Vector3.up);
                    Vector3 position = DirectionForAngle(angle) * (outerRadius - (thickness * 0.5f));
                    position.y = (data.segmentIndex * config.segmentHeight) + ((row + 0.5f) * cellHeight);

                    GameObject cellObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cellObject.name = $"Cell_{row:00}_{column:00}";
                    cellObject.transform.SetParent(contentRoot, false);
                    cellObject.transform.localPosition = position;
                    cellObject.transform.localRotation = rotation;
                    cellObject.transform.localScale = new Vector3(
                        (arcWidth * padding) + config.cellWidthOverlap,
                        (cellHeight * padding) + config.cellHeightOverlap,
                        thickness + config.cellDepthOverlap);

                    Collider collider = cellObject.GetComponent<Collider>();
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

                    if (!isPath)
                    {
                        MeshFilter meshFilter = cellObject.GetComponent<MeshFilter>();
                        if (meshFilter != null)
                        {
                            meshFilter.sharedMesh = RoundedBoxMeshCache.GetWallMesh(config);
                        }
                    }

                    Renderer renderer = cellObject.GetComponent<Renderer>();
                    renderer.sharedMaterial = cellKind switch
                    {
                        MazeCellKind.MainPath => materials.MainPathMaterial,
                        MazeCellKind.Path => materials.PathMaterial,
                        _ => materials.WallMaterial,
                    };
                    renderer.shadowCastingMode = isPath ? ShadowCastingMode.Off : ShadowCastingMode.On;
                    renderer.receiveShadows = !isPath;
                    cellRenderers.Add(new CellRendererBinding
                    {
                        renderer = renderer,
                        cellKind = cellKind,
                    });
                }
            }
        }

        public void RefreshMaterials(TowerMaterials materials)
        {
            for (int i = 0; i < cellRenderers.Count; i++)
            {
                Renderer renderer = cellRenderers[i].renderer;
                if (renderer == null)
                {
                    continue;
                }

                renderer.sharedMaterial = cellRenderers[i].cellKind switch
                {
                    MazeCellKind.MainPath => materials.MainPathMaterial,
                    MazeCellKind.Path => materials.PathMaterial,
                    _ => materials.WallMaterial,
                };
            }
        }

        private void ClearChildren()
        {
            List<GameObject> toDestroy = new();
            foreach (Transform child in transform)
            {
                toDestroy.Add(child.gameObject);
            }

            foreach (GameObject child in toDestroy)
            {
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }

            cellRenderers.Clear();
        }

        private static Vector3 DirectionForAngle(float angleDegrees)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians)).normalized;
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

        public Transform TowerSpace => transform;
        public TowerRotationController RotationController => rotationController;
        public TowerSinkController SinkController => sinkController;
        public int DebugDifficultyTier => debugDifficultyTier;

        public void Initialize(GameConfig gameConfig, DifficultyProfile profile, ThemeDefinition definition)
        {
            config = gameConfig;
            difficultyProfile = profile;
            theme = definition;
            materials = new TowerMaterials(theme);
            
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
            DifficultySettings settings = difficultyProfile.Evaluate(playerHeight);
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

        private int AngleToColumn(float angleDegrees)
        {
            float wrapped = Mathf.Repeat(angleDegrees, 360f);
            return Mathf.FloorToInt(wrapped / config.AnglePerCell) % config.mazeWidthCells;
        }

        private void SpawnSegment(int segmentIndex)
        {
            int zoneIndex = GetZoneIndexForSegment(segmentIndex);
            SegmentData data = segmentIndex == 0
                ? mazeGenerator.CreateTutorialSegment(config, theme, segmentIndex, lastExitColumn)
                : mazeGenerator.Generate(
                    config,
                    difficultyProfile,
                    theme,
                    segmentIndex,
                    zoneIndex,
                    lastExitColumn,
                    GetSegmentSeed(segmentIndex, zoneIndex));

            lastExitColumn = data.exitColumn;
            highestSpawnedSegment = Mathf.Max(highestSpawnedSegment, segmentIndex);
            segmentDataByIndex[segmentIndex] = data;

            TowerSegment segment = segmentPool.Get(pooledSegmentsRoot);
            segment.transform.SetParent(activeSegmentsRoot, false);
            segment.Build(data, config, materials);
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
            return Mathf.Max(0, segmentIndex / segmentsPerZone);
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
