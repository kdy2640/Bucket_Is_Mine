using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

// 지형 밀도 생성, 청크 메시 갱신과 대상 주변 스트리밍을 관리한다.
[DisallowMultipleComponent]
public class TerrainManager : MonoBehaviour
{
    [Header("표면 높이와 변화 폭")]
    [SerializeField, Min(0f)] private float baseSurfaceHeight = 5f;
    [SerializeField, Min(0f)] private float terrainAmplitude = 5f;

    [Header("노이즈와 표면 판정")]
    [SerializeField] private float noiseScale = 1f;
    [FormerlySerializedAs("heightTresshold")]
    [SerializeField] private float densityThreshold = 0.5f;
    [SerializeField] private bool use3DNoise;

    [Header("지형 재질")]
    [SerializeField] private Material mat;

    [Header("메시 셰이딩")]
    [SerializeField] private bool isSmoothShading;

    [Header("청크 스트리밍")]
    [SerializeField] private Transform streamingTarget;
    [SerializeField] private TerrainChunkManager chunkManager = new TerrainChunkManager();

    // 실행 중인 지형 데이터와 생성 작업
    private TerrainData data;
    private TerrainDensityFormer generator;
    private Coroutine generationRoutine;

    // 청크 생성과 외부 조회에 사용하는 지형 상태
    public TerrainData Data => data;
    public int ChunkSize => chunkManager.Grid.ChunkSize;
    public float DensityThreshold => densityThreshold;
    public Material Material => mat;
    public bool IsInitialLoadComplete => data != null && chunkManager.Streamer.IsInitialLoadComplete;
    public bool IsSmoothShading
    {
        get => isSmoothShading;
        set => isSmoothShading = value;
    }

    // 초기 지형을 생성한다.
    protected virtual void Start()
    {
        GenerateTerrain();
    }

    // 대상 위치에 따라 청크 활성화 대기열을 프레임 예산만큼 처리한다.
    private void Update()
    {
        chunkManager.Streamer.Tick();
    }

    // 물리 갱신에 맞춰 대상 주변 청크와 충돌체를 먼저 활성화한다.
    private void FixedUpdate()
    {
        // Run before player physics so a newly entered neighborhood has colliders.
        // The normal activation budget is processed only by Update.
        chunkManager.Streamer.UpdateTarget();
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
    public void GenerateTerrain()
    {
        if (generationRoutine != null)
        {
            StopCoroutine(generationRoutine);
            generationRoutine = null;
        }
        chunkManager.Streamer.Reset();
        if (chunkManager.Registry != null)
        {
            chunkManager.Registry.ClearChunks();
        }
        if (data != null)
        {
            data.Dispose();
        }

        data = CreateTerrainData();
        generator = generator ?? new TerrainDensityFormer();
        generator.Generate(
            data,
            baseSurfaceHeight,
            terrainAmplitude,
            noiseScale,
            densityThreshold,
            use3DNoise);
        if (chunkManager.Registry == null)
        {
            chunkManager.Initialize(this);
        }
        chunkManager.Registry.ClearChunks();
        generationRoutine = StartCoroutine(GenerateTerrainRoutine());
    }

    // 초기 청크를 여러 프레임에 나눠 생성한 뒤 스트리밍을 시작한다.
    private IEnumerator GenerateTerrainRoutine()
    {
        yield return chunkManager.GenerateInitialChunks(streamingTarget);
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
            generator = generator ?? new TerrainDensityFormer();
            generator.Generate(
                data,
                baseSurfaceHeight,
                terrainAmplitude,
                noiseScale,
                densityThreshold,
                use3DNoise);
        }

        if (chunkManager.Registry == null)
        {
            chunkManager.Initialize(this);
        }
    }

    // 현재 격자 설정으로 밀도 데이터를 생성한다.
    private TerrainData CreateTerrainData()
    {
        return new TerrainData(chunkManager.Grid);
    }

    // 오브젝트가 파괴될 때 지형이 소유한 자원을 해제한다.
    private void OnDestroy()
    {
        ReleaseResources();
    }

    // 생성 코루틴과 스트리밍을 중단하고 메시와 밀도 버퍼를 해제한다.
    private void ReleaseResources()
    {
        if (generationRoutine != null)
        {
            StopCoroutine(generationRoutine);
            generationRoutine = null;
        }
        chunkManager.Streamer.Reset();
        if (chunkManager.Registry != null)
        {
            chunkManager.Dispose();
        }

        if (data != null)
        {
            data.Dispose();
            data = null;
        }
    }

}
