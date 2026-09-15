using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 청크 초기 생성·재생성을 진행하고 청크 저장소와 스트리머를 제공한다.
[System.Serializable]
public class TerrainChunkManager : System.IDisposable
{
    #region 필드 및 속성

    [Header("청크 스트리밍")]
    [SerializeField] private TerrainChunkStreamer streamer = new TerrainChunkStreamer();

    // 한 번에 생성할 청크 수와 실행 중인 지형·메시 생성기
    private const int ChunkGenerationBatchSize = 64;
    private TerrainManager owner;
    private TerrainMeshGenerator meshGenerator;

    public TerrainChunkRegistry Registry { get; private set; }
    public TerrainChunkStreamer Streamer => streamer;

    #endregion

    #region 초기화

    // 지형을 연결하고 청크 저장소와 메시 생성기의 실행 자원을 준비한다.
    public void Initialize(TerrainManager owner)
    {
        this.owner = owner;
        Registry = new TerrainChunkRegistry(owner);
        meshGenerator = new TerrainMeshGenerator();
    }

    #endregion

    #region 청크 생성 및 메시 갱신

    // 초기 청크를 프레임에 나눠 생성하고 전체 준비가 끝나면 스트리밍을 시작한다.
    public IEnumerator GenerateInitialChunks(Transform streamingTarget)
    {
        Vector3Int chunkCounts = GetChunkCounts();
        List<Vector3Int> batch = new List<Vector3Int>(ChunkGenerationBatchSize);
        for (int x = 0; x < chunkCounts.x; x++)
        {
            for (int y = 0; y < chunkCounts.y; y++)
            {
                for (int z = 0; z < chunkCounts.z; z++)
                {
                    batch.Add(new Vector3Int(x, y, z));
                    if (batch.Count == ChunkGenerationBatchSize)
                    {
                        RegenerateChunks(batch);
                        foreach (Vector3Int coordinate in batch)
                        {
                            Registry.SetChunkActive(coordinate, false);
                        }
                        batch.Clear();
                        yield return null;
                    }
                }
            }
        }

        if (batch.Count > 0)
        {
            RegenerateChunks(batch);
            foreach (Vector3Int coordinate in batch)
            {
                Registry.SetChunkActive(coordinate, false);
            }
            yield return null;
        }

        streamer.Initialize(owner, Registry, streamingTarget);
    }

    // 지형 범위 밖 청크를 제거한 뒤 전체 청크 메시를 다시 생성한다.
    public int RegenerateAllChunks()
    {
        Vector3Int chunkCounts = GetChunkCounts();
        Registry.RemoveUnusedChunks(chunkCounts);
        List<Vector3Int> chunkCoords = new List<Vector3Int>();

        for (int x = 0; x < chunkCounts.x; x++)
        {
            for (int y = 0; y < chunkCounts.y; y++)
            {
                for (int z = 0; z < chunkCounts.z; z++)
                {
                    chunkCoords.Add(new Vector3Int(x, y, z));
                }
            }
        }

        return RegenerateChunks(chunkCoords);
    }

    // 수정된 밀도와 경계 노멀에 영향을 받는 청크만 골라 메시를 갱신한다.
    public int RegenerateChunksInBounds(Vector3Int minIndex, Vector3Int maxIndex)
    {
        TerrainData data = owner.Data;
        if (data == null)
        {
            return 0;
        }

        // Gradient normals also depend on density samples one cell beyond each corner.
        int normalPadding = owner.IsSmoothShading ? 1 : 0;
        Vector3Int minCube = new Vector3Int(
            Mathf.Clamp(minIndex.x - 1 - normalPadding, 0, data.Width - 1),
            Mathf.Clamp(minIndex.y - 1 - normalPadding, 0, data.DensityFieldHeight - 1),
            Mathf.Clamp(minIndex.z - 1 - normalPadding, 0, data.Width - 1));

        Vector3Int maxCube = new Vector3Int(
            Mathf.Clamp(maxIndex.x + normalPadding, 0, data.Width - 1),
            Mathf.Clamp(maxIndex.y + normalPadding, 0, data.DensityFieldHeight - 1),
            Mathf.Clamp(maxIndex.z + normalPadding, 0, data.Width - 1));

        Vector3Int minChunk = CubeIndexToChunkCoord(minCube);
        Vector3Int maxChunk = CubeIndexToChunkCoord(maxCube);
        List<Vector3Int> chunkCoords = new List<Vector3Int>();

        for (int x = minChunk.x; x <= maxChunk.x; x++)
        {
            for (int y = minChunk.y; y <= maxChunk.y; y++)
            {
                for (int z = minChunk.z; z <= maxChunk.z; z++)
                {
                    chunkCoords.Add(new Vector3Int(x, y, z));
                }
            }
        }

        return RegenerateChunks(chunkCoords);
    }

    // 요청된 청크들을 배치 크기로 나눠 생성하고 메시와 충돌체에 적용한다.
    private int RegenerateChunks(List<Vector3Int> chunkCoords)
    {
        // Bound temporary mesh/job buffers even when regenerating the entire terrain.
        for (int start = 0; start < chunkCoords.Count; start += ChunkGenerationBatchSize)
        {
            int count = Mathf.Min(ChunkGenerationBatchSize, chunkCoords.Count - start);
            List<Vector3Int> batch = chunkCoords.GetRange(start, count);
            Mesh[] meshes = meshGenerator.Generate(
                owner.Data, batch, owner.DensityThreshold, owner.IsSmoothShading);
            for (int i = 0; i < count; i++)
            {
                Registry.SetChunkMesh(Registry.GetOrCreateChunk(batch[i]), meshes[i]);
            }
        }

        return chunkCoords.Count;
    }

    #endregion

    #region 청크 좌표

    // 현재 밀도 데이터의 축별 청크 개수를 반환한다.
    private Vector3Int GetChunkCounts()
    {
        return owner.Data.ChunkCounts;
    }

    // 전체 격자의 큐브 좌표를 해당 큐브가 속한 청크 좌표로 변환한다.
    private Vector3Int CubeIndexToChunkCoord(Vector3Int cubeIndex)
    {
        int chunkSize = owner.Data.ChunkSize;
        return new Vector3Int(
            cubeIndex.x / chunkSize,
            cubeIndex.y / chunkSize,
            cubeIndex.z / chunkSize);
    }

    #endregion

    #region 자원 해제

    // 스트리밍을 중단하고 메시 생성기와 청크 저장소의 자원을 해제한다.
    public void Dispose()
    {
        streamer.Reset();
        meshGenerator.Dispose();
        Registry.Dispose();
        Registry = null;
    }

    #endregion
}
