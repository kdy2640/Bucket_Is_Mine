using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class TerrainManager : MonoBehaviour
{
    [SerializeField] private int width = 30;
    [SerializeField, Min(1)] private int densityFieldHeight = 20;
    [SerializeField, Min(0f)] private float baseSurfaceHeight = 5f;
    [SerializeField, Min(0f)] private float terrainAmplitude = 5f;
    [SerializeField] private float resolution = 1f;
    [SerializeField] private float noiseScale = 1f;
    [FormerlySerializedAs("heightTresshold")]
    [SerializeField] private float densityThreshold = 0.5f;
    [SerializeField] private bool use3DNoise;
    [SerializeField] private Material mat;
    [SerializeField] private int chunkSize = 16;
    [SerializeField] private bool isSmoothShading;

    private TerrainData data;
    private TerrainGenerator generator;
    private TerrainChunkManager chunkManager;

    public TerrainData Data => data;
    public int ChunkSize => Mathf.Max(1, chunkSize);
    public float DensityThreshold => densityThreshold;
    public Material Material => mat;
    public bool IsSmoothShading
    {
        get => isSmoothShading;
        set => isSmoothShading = value;
    }

    protected virtual void Start()
    {
        ClearParentMesh();
        GenerateTerrain();
    }

    public void AddDensitySphere(Vector3 worldPosition, float radius, float power)
    {
        EnsureInitialized();
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);

        if (data.ModifyDensitySphere(
            localPosition,
            radius,
            power,
            out Vector3Int minChangedIndex,
            out Vector3Int maxChangedIndex))
        {
            chunkManager.RegenerateChunksInBounds(minChangedIndex, maxChangedIndex);
        }
    }

    [ContextMenu("Regenerate Terrain")]
    public void GenerateTerrain()
    {
        data = CreateTerrainData();
        generator = generator ?? new TerrainGenerator();
        generator.Generate(
            data,
            baseSurfaceHeight,
            terrainAmplitude,
            noiseScale,
            densityThreshold,
            use3DNoise);
        chunkManager = chunkManager ?? new TerrainChunkManager(this);
        chunkManager.RegenerateAllChunks();
    }

    public void RegenerateAllChunks()
    {
        EnsureInitialized();
        chunkManager.RegenerateAllChunks();
    }

    private void EnsureInitialized()
    {
        if (data == null)
        {
            data = CreateTerrainData();
            generator = generator ?? new TerrainGenerator();
            generator.Generate(
                data,
                baseSurfaceHeight,
                terrainAmplitude,
                noiseScale,
                densityThreshold,
                use3DNoise);
        }

        if (chunkManager == null)
        {
            chunkManager = new TerrainChunkManager(this);
        }
    }

    private void ClearParentMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        MeshCollider meshCollider = GetComponent<MeshCollider>();

        if (meshFilter != null)
        {
            meshFilter.sharedMesh = null;
        }

        if (meshCollider != null)
        {
            meshCollider.sharedMesh = null;
        }
    }

    private TerrainData CreateTerrainData()
    {
        return new TerrainData(width, densityFieldHeight, resolution);
    }

    private void OnValidate()
    {
        densityFieldHeight = Mathf.Max(1, densityFieldHeight);
        baseSurfaceHeight = Mathf.Clamp(baseSurfaceHeight, 0f, densityFieldHeight);

        float maximumAmplitude = Mathf.Min(
            baseSurfaceHeight,
            densityFieldHeight - baseSurfaceHeight);

        terrainAmplitude = Mathf.Clamp(terrainAmplitude, 0f, maximumAmplitude);
    }
}
