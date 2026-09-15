using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

// 지형 밀도 생성, 청크 메시 갱신과 대상 주변 스트리밍을 관리한다.
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class TerrainManager : MonoBehaviour
{
    [Header("밀도 격자 크기")]
    [SerializeField] private int width = 30;
    [SerializeField, Min(1)] private int densityFieldHeight = 20;

    [Header("표면 높이와 변화 폭")]
    [SerializeField, Min(0f)] private float baseSurfaceHeight = 5f;
    [SerializeField, Min(0f)] private float terrainAmplitude = 5f;

    [Header("밀도 샘플 간격")]
    [SerializeField] private float resolution = 1f;

    [Header("노이즈와 표면 판정")]
    [SerializeField] private float noiseScale = 1f;
    [FormerlySerializedAs("heightTresshold")]
    [SerializeField] private float densityThreshold = 0.5f;
    [SerializeField] private bool use3DNoise;

    [Header("지형 재질")]
    [SerializeField] private Material mat;

    [Header("청크 크기")]
    [SerializeField] private int chunkSize = 16;

    [Header("메시 셰이딩")]
    [SerializeField] private bool isSmoothShading;

    [Header("청크 스트리밍")]
    [SerializeField] private Transform streamingTarget;
    [SerializeField] private TerrainChunkStreamer streamer = new TerrainChunkStreamer();

    // 실행 중인 지형 데이터와 생성 작업
    private TerrainData data;
    private TerrainGenerator generator;
    private TerrainChunkManager chunkManager;
    private Coroutine generationRoutine;
#if UNITY_EDITOR
    // Unity preserves private serializable fields during script hot reload.
    private bool regenerateAfterReload;
#endif

    // 청크 생성과 외부 조회에 사용하는 지형 상태
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

    // 부모 오브젝트의 기존 메시를 비우고 초기 지형을 생성한다.
    protected virtual void Start()
    {
        ClearParentMesh();
        GenerateTerrain();
    }

    // 대상 위치에 따라 청크 활성화 대기열을 프레임 예산만큼 처리한다.
    private void Update()
    {
        streamer.Tick();
    }

    // 물리 갱신에 맞춰 대상 주변 청크와 충돌체를 먼저 활성화한다.
    private void FixedUpdate()
    {
        // Run before player physics so a newly entered neighborhood has colliders.
        // The normal activation budget is processed only by Update.
        streamer.UpdateTarget();
    }

    // 월드 좌표의 구 영역에 밀도를 더하고 영향을 받은 청크 메시만 갱신한다.
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

    // 현재 지형을 정리하고 설정값으로 밀도와 청크 메시를 다시 생성한다.
    [ContextMenu("Regenerate Terrain")]
    public void GenerateTerrain()
    {
        if (generationRoutine != null)
        {
            StopCoroutine(generationRoutine);
            generationRoutine = null;
        }
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
            // Replace edit-mode previews with the complete runtime terrain.
            chunkManager.ClearChunks();
            generationRoutine = StartCoroutine(GenerateTerrainRoutine());
        }
        else
        {
            chunkManager.RegenerateAllChunks();
        }
    }

    // 초기 청크를 여러 프레임에 나눠 생성한 뒤 스트리밍을 시작한다.
    private IEnumerator GenerateTerrainRoutine()
    {
        yield return chunkManager.GenerateInitialChunks();
        streamer.Initialize(this, chunkManager, streamingTarget);
        generationRoutine = null;
    }

    // 현재 밀도 데이터를 유지한 채 모든 청크 메시를 다시 만든다.
    public void RegenerateAllChunks()
    {
        EnsureInitialized();
        chunkManager.RegenerateAllChunks();
    }

    // 밀도 데이터와 청크 관리자가 아직 없으면 생성한다.
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

    // 청크 메시만 표시되도록 부모의 렌더링·충돌 메시를 비운다.
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

    // 현재 격자 설정으로 밀도 데이터를 만들고 에디터 정리 이벤트를 연결한다.
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

    // 오브젝트가 파괴될 때 지형이 소유한 자원을 해제한다.
    private void OnDestroy()
    {
        ReleaseResources();
    }

    // 생성 코루틴과 스트리밍을 중단하고 메시·밀도 버퍼·이벤트 연결을 정리한다.
    private void ReleaseResources()
    {
        if (generationRoutine != null)
        {
            StopCoroutine(generationRoutine);
            generationRoutine = null;
        }
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
    // 스크립트 리로드 전에 지형이 있던 매니저만 다음 에디터 갱신에서 복원한다.
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

    // 지형 복원 필요 여부를 기록하고 스크립트 리로드 전에 자원을 해제한다.
    private void OnBeforeAssemblyReload()
    {
        regenerateAfterReload = data != null;
        ReleaseResources();
    }

    // 에디트 모드나 플레이 모드에서 나갈 때 기존 지형 자원을 정리한다.
    private void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
    {
        if (state == UnityEditor.PlayModeStateChange.ExitingEditMode ||
            state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
        {
            ReleaseResources();
        }
    }

    // 이 매니저가 속한 씬이 닫힐 때 지형 자원을 해제한다.
    private void OnSceneClosing(UnityEngine.SceneManagement.Scene scene, bool removingScene)
    {
        if (gameObject.scene == scene)
        {
            ReleaseResources();
        }
    }
#endif

    // 기준 높이와 높이 변화 폭이 밀도 격자의 세로 범위를 넘지 않게 제한한다.
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
