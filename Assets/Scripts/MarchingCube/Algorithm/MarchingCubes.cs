using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class MarchingCubes : MonoBehaviour
{
    [SerializeField] private int width = 30;
    [SerializeField] private int height = 10;
    [SerializeField] private float resolution = 1;
    [SerializeField] private float noiseScale = 1;
    [SerializeField] private float heightTresshold = 0.5f;
    [SerializeField] private bool visualizeNoise;
    [SerializeField] private bool use3DNoise;
    [SerializeField] public Material mat;
    [SerializeField] private int chunkSize = 16;
    [SerializeField] private int benchmarkIterations = 20;
    [SerializeField] private float benchmarkRadius = 3f;
    [SerializeField] private bool benchmarkAtTerrainCenter = true;
    [SerializeField] private Vector3 benchmarkPosition;

    public bool isSmoothShading = false;

    private float[,,] heights;
    private MarchingChunkManager chunkManager;

    public int Width { get { return width; } }
    public int Height { get { return height; } }
    public int ChunkSize { get { return Mathf.Max(1, chunkSize); } }
    public float Resolution { get { return resolution; } }
    public float HeightThreshold { get { return heightTresshold; } }
    public Material Material { get { return mat; } }
    public float[,,] Heights { get { return heights; } }

    private void Start()
    {
        ClearParentMesh();
        EnsureChunkManager();
        UpdateMesh();
    }

    public void AddHeightSphere(Vector3 pos, float radius, float power)
    {
        Vector3Int center = PositionToIndex(pos);
        int r = Mathf.CeilToInt(radius / resolution);
        bool changed = false;
        Vector3Int minIndex = new Vector3Int(width, 2 * height, width);
        Vector3Int maxIndex = Vector3Int.zero;

        for (int x = -r; x <= r; x++)
        {
            for (int y = -r; y <= r; y++)
            {
                for (int z = -r; z <= r; z++)
                {
                    Vector3Int index = center + new Vector3Int(x, y, z);

                    if (index.x < 0 || index.x >= width + 1 ||
                        index.y < 0 || index.y >= 2 * height + 1 ||
                        index.z < 0 || index.z >= width + 1)
                        continue;

                    Vector3 worldPos = IndexToPosition(index);
                    float dist = Vector3.Distance(worldPos, pos);

                    if (dist > radius) continue;

                    float falloff = 1 - (dist / radius);
                    heights[index.x, index.y, index.z] = Mathf.Clamp01(heights[index.x, index.y, index.z] + power * falloff);
                    minIndex = Vector3Int.Min(minIndex, index);
                    maxIndex = Vector3Int.Max(maxIndex, index);
                    changed = true;
                }
            }
        }

        if (changed)
        {
            EnsureChunkManager();
            chunkManager.RegenerateChunksInBounds(minIndex, maxIndex);
        }
    }

    [ContextMenu("Execute UpdateMesh")]
    private void UpdateMesh()
    {
        SetHeights();
        MarchCubes();
    }

    public void MarchCubes()
    {
        EnsureChunkManager();
        chunkManager.RegenerateAllChunks();
    }

    [ContextMenu("Benchmark Chunk Regeneration")]
    private void BenchmarkChunkRegeneration()
    {
        EnsureChunkManager();

        if (heights == null)
        {
            SetHeights();
            MarchCubes();
        }

        Vector3 benchmarkPoint = benchmarkAtTerrainCenter
            ? new Vector3(width * resolution * 0.5f, height * resolution, width * resolution * 0.5f)
            : benchmarkPosition;

        if (!TryGetSphereIndexBounds(benchmarkPoint, benchmarkRadius, out Vector3Int minIndex, out Vector3Int maxIndex))
        {
            Debug.LogWarning("Benchmark skipped: the benchmark sphere does not overlap the density field.");
            return;
        }

        int iterations = Mathf.Max(1, benchmarkIterations);

        int partialChunkCount = chunkManager.RegenerateChunksInBounds(minIndex, maxIndex);
        int fullChunkCount = chunkManager.RegenerateAllChunks();

        Stopwatch stopwatch = new Stopwatch();

        stopwatch.Start();
        for (int i = 0; i < iterations; i++)
        {
            chunkManager.RegenerateChunksInBounds(minIndex, maxIndex);
        }
        stopwatch.Stop();
        double partialTotalMs = stopwatch.Elapsed.TotalMilliseconds;

        stopwatch.Reset();
        stopwatch.Start();
        for (int i = 0; i < iterations; i++)
        {
            chunkManager.RegenerateAllChunks();
        }
        stopwatch.Stop();
        double fullTotalMs = stopwatch.Elapsed.TotalMilliseconds;

        double partialAverageMs = partialTotalMs / iterations;
        double fullAverageMs = fullTotalMs / iterations;
        double speedup = partialAverageMs > 0 ? fullAverageMs / partialAverageMs : 0;

        Debug.Log(
            $"Chunk benchmark ({iterations} iterations)\n" +
            $"Point: {benchmarkPoint}, radius: {benchmarkRadius}, chunk size: {ChunkSize}\n" +
            $"Partial regeneration: {partialChunkCount} chunks, total {partialTotalMs:F3} ms, avg {partialAverageMs:F3} ms\n" +
            $"Full regeneration: {fullChunkCount} chunks, total {fullTotalMs:F3} ms, avg {fullAverageMs:F3} ms\n" +
            $"Speedup: {speedup:F2}x");
    }

    private void SetHeights()
    {
        heights = new float[width + 1, 2 * height + 1, width + 1];
        PerlinNoise3D noise = new PerlinNoise3D(15);

        for (int x = 0; x < width + 1; x++)
        {
            for (int y = 0; y < 2 * height + 1; y++)
            {
                for (int z = 0; z < width + 1; z++)
                {
                    if (use3DNoise)
                    {
                        heights[x, y, z] = noise.GetRandomValue(x * noiseScale, y * noiseScale, z * noiseScale);
                    }
                    else
                    {
                        float currentHeight = height * Mathf.PerlinNoise(x * noiseScale, z * noiseScale);
                        heights[x, y, z] = Mathf.Clamp01(currentHeight - y + heightTresshold);
                    }
                }
            }
        }
    }

    private Vector3Int PositionToIndex(Vector3 position)
    {
        int x = Mathf.RoundToInt(position.x / resolution);
        int y = Mathf.RoundToInt(position.y / resolution);
        int z = Mathf.RoundToInt(position.z / resolution);

        return new Vector3Int(x, y, z);
    }

    private bool TryGetSphereIndexBounds(Vector3 pos, float radius, out Vector3Int minIndex, out Vector3Int maxIndex)
    {
        Vector3Int center = PositionToIndex(pos);
        int r = Mathf.CeilToInt(radius / resolution);

        if (center.x + r < 0 || center.x - r > width ||
            center.y + r < 0 || center.y - r > 2 * height ||
            center.z + r < 0 || center.z - r > width)
        {
            minIndex = Vector3Int.zero;
            maxIndex = Vector3Int.zero;
            return false;
        }

        minIndex = new Vector3Int(
            Mathf.Clamp(center.x - r, 0, width),
            Mathf.Clamp(center.y - r, 0, 2 * height),
            Mathf.Clamp(center.z - r, 0, width));

        maxIndex = new Vector3Int(
            Mathf.Clamp(center.x + r, 0, width),
            Mathf.Clamp(center.y + r, 0, 2 * height),
            Mathf.Clamp(center.z + r, 0, width));

        return minIndex.x <= maxIndex.x &&
               minIndex.y <= maxIndex.y &&
               minIndex.z <= maxIndex.z;
    }

    private Vector3 IndexToPosition(Vector3Int index)
    {
        float x = index.x * resolution;
        float y = index.y * resolution;
        float z = index.z * resolution;

        return new Vector3(x, y, z);
    }

    private void EnsureChunkManager()
    {
        if (chunkManager == null)
        {
            chunkManager = new MarchingChunkManager(this);
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

    private void OnDrawGizmosSelected()
    {
        if (!visualizeNoise || !Application.isPlaying || heights == null)
        {
            return;
        }

        for (int x = 0; x < width + 1; x++)
        {
            for (int y = 0; y < height + 1; y++)
            {
                for (int z = 0; z < width + 1; z++)
                {
                    Gizmos.color = new Color(heights[x, y, z], heights[x, y, z], heights[x, y, z], 1);
                    Gizmos.DrawSphere(new Vector3(x * resolution, y * resolution, z * resolution), 0.2f * resolution);
                }
            }
        }
    }
}
