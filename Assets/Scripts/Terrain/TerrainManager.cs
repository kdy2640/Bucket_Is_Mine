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
    [SerializeField] private Transform streamingTarget;
    [SerializeField] private TerrainChunkStreamer streamer = new TerrainChunkStreamer();

    private TerrainData data;
    private TerrainGenerator generator;
    private TerrainChunkManager chunkManager;
#if UNITY_EDITOR
    // Unity preserves private serializable fields during script hot reload.
    private bool regenerateAfterReload;
#endif

    public TerrainData Data => data;
    public int ChunkSize => Mathf.Max(1, chunkSize);
    public float DensityThreshold => densityThreshold;
    public Material Material => mat;
    public bool IsInitialLoadComplete => data != null && streamer.IsInitialLoadComplete;
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

    private void Update()
    {
        streamer.Tick();
    }

    private void FixedUpdate()
    {
        // Run before player physics so a newly entered neighborhood has colliders.
        // The normal load budget is processed only by Update.
        streamer.UpdateTarget();
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
        streamer.Reset();
        if (chunkManager != null)
        {
            chunkManager.ClearChunks();
        }
        if (data != null)
        {
            data.Dispose();
        }

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
        if (Application.isPlaying)
        {
            // Discard edit-mode preview chunks before starting the runtime queue.
            chunkManager.ClearChunks();
            streamer.Initialize(this, chunkManager, streamingTarget);
        }
        else
        {
            chunkManager.RegenerateAllChunks();
        }
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
#if UNITY_EDITOR
        // Resources can also be created by the edit-mode context menu, without OnEnable.
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        UnityEditor.SceneManagement.EditorSceneManager.sceneClosing -= OnSceneClosing;
        UnityEditor.SceneManagement.EditorSceneManager.sceneClosing += OnSceneClosing;
#endif
        return new TerrainData(width, densityFieldHeight, resolution, ChunkSize);
    }

    private void OnDestroy()
    {
        ReleaseResources();
    }

    private void ReleaseResources()
    {
        streamer.Reset();
#if UNITY_EDITOR
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
        UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        UnityEditor.SceneManagement.EditorSceneManager.sceneClosing -= OnSceneClosing;
#endif
        if (chunkManager != null)
        {
            chunkManager.Dispose();
            chunkManager = null;
        }

        if (data != null)
        {
            data.Dispose();
            data = null;
        }
    }

#if UNITY_EDITOR
    [UnityEditor.Callbacks.DidReloadScripts]
    private static void RestoreTerrainAfterReload()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            foreach (TerrainManager terrain in FindObjectsByType<TerrainManager>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (terrain.regenerateAfterReload)
                {
                    terrain.regenerateAfterReload = false;
                    terrain.GenerateTerrain();
                }
            }
        };
    }

    private void OnBeforeAssemblyReload()
    {
        regenerateAfterReload = data != null;
        ReleaseResources();
    }

    private void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
    {
        if (state == UnityEditor.PlayModeStateChange.ExitingEditMode ||
            state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
        {
            ReleaseResources();
        }
    }

    private void OnSceneClosing(UnityEngine.SceneManagement.Scene scene, bool removingScene)
    {
        if (gameObject.scene == scene)
        {
            ReleaseResources();
        }
    }
#endif

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
