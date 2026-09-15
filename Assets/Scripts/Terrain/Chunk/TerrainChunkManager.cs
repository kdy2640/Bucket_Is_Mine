using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

// 청크 오브젝트의 생성·활성화와 메시·충돌체 교체 및 해제를 관리한다.
public class TerrainChunkManager : System.IDisposable
{
    // 한 번에 생성할 청크 수와 충돌 메시 적용 시간 측정
    private const int ChunkGenerationBatchSize = 64;
    private static readonly ProfilerMarker ColliderMarker = new ProfilerMarker("TerrainMesh.Collider");
    // 지형 소유자와 메시 생성기
    private readonly TerrainManager owner;
    private readonly TerrainMeshGenerator meshGenerator;
    // 등록된 청크와 직접 생성해 해제해야 하는 메시
    private readonly Dictionary<Vector3Int, ChunkData> chunks =
        new Dictionary<Vector3Int, ChunkData>();
    private readonly HashSet<Mesh> generatedMeshes = new HashSet<Mesh>();

    public ICollection<Vector3Int> ChunkCoordinates => chunks.Keys;

    // 해당 좌표의 청크 오브젝트가 활성화되어 있는지 확인한다.
    public bool IsChunkActive(Vector3Int coordinate) => chunks[coordinate].gameObject.activeSelf;

    // 청크 오브젝트의 활성 상태가 요청과 다를 때만 변경한다.
    public void SetChunkActive(Vector3Int coordinate, bool active)
    {
        GameObject chunkObject = chunks[coordinate].gameObject;
        if (chunkObject.activeSelf != active)
        {
            chunkObject.SetActive(active);
        }
    }

    // 청크의 메시와 오브젝트를 제거하고 관리 목록에서 제외한다.
    private void DestroyChunk(Vector3Int coordinate)
    {
        ChunkData chunk = chunks[coordinate];
        SetChunkMesh(chunk, null);
        chunk.gameObject.SetActive(false);
        if (Application.isPlaying) Object.Destroy(chunk.gameObject);
        else Object.DestroyImmediate(chunk.gameObject);
        chunks.Remove(coordinate);
    }

    // 등록된 모든 청크 오브젝트와 메시를 제거한다.
    public void ClearChunks()
    {
        foreach (Vector3Int coordinate in new List<Vector3Int>(chunks.Keys))
        {
            DestroyChunk(coordinate);
        }
    }

    // 지형 소유자와 메시 생성기를 연결하고 기존 자식 청크를 등록한다.
    public TerrainChunkManager(TerrainManager owner)
    {
        this.owner = owner;
        meshGenerator = new TerrainMeshGenerator();
        RegisterExistingChunks();
    }

    // 지형 범위 밖 청크를 제거한 뒤 전체 청크 메시를 다시 생성한다.
    public int RegenerateAllChunks()
    {
        Vector3Int chunkCounts = GetChunkCounts();
        RemoveUnusedChunks(chunkCounts);
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

    // 초기 청크를 일정 개수씩 생성해 비활성 상태로 준비하고 프레임을 나눠 처리한다.
    public IEnumerator GenerateInitialChunks()
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
                            SetChunkActive(coordinate, false);
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
                SetChunkActive(coordinate, false);
            }
            yield return null;
        }
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

    // 메시 생성기의 버퍼와 직접 생성한 메시를 해제하고 관리 목록을 비운다.
    public void Dispose()
    {
        meshGenerator.Dispose();
        // Child components may already be destroyed when the owner's OnDestroy runs.
        foreach (Mesh mesh in generatedMeshes)
        {
            if (Application.isPlaying) Object.Destroy(mesh);
            else Object.DestroyImmediate(mesh);
        }
        generatedMeshes.Clear();
        chunks.Clear();
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
                SetChunkMesh(GetOrCreateChunk(batch[i]), meshes[i]);
            }
        }

        return chunkCoords.Count;
    }

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

    // 등록된 청크를 반환하거나 지형의 레이어·태그·재질을 사용하는 청크를 생성한다.
    private ChunkData GetOrCreateChunk(Vector3Int chunkCoord)
    {
        if (chunks.TryGetValue(chunkCoord, out ChunkData chunk))
        {
            return chunk;
        }

        GameObject chunkObject =
            new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}_{chunkCoord.z}");
        if (Application.isPlaying)
        {
            chunkObject.SetActive(false);
        }
        chunkObject.transform.SetParent(owner.transform, false);
        chunkObject.layer = owner.gameObject.layer;
        chunkObject.tag = owner.gameObject.tag;

        chunk = new ChunkData
        {
            gameObject = chunkObject,
            meshFilter = chunkObject.AddComponent<MeshFilter>(),
            meshRenderer = chunkObject.AddComponent<MeshRenderer>(),
            meshCollider = chunkObject.AddComponent<MeshCollider>()
        };

        chunk.meshRenderer.sharedMaterial = ResolveMaterial();
        chunks[chunkCoord] = chunk;
        return chunk;
    }

    // 자식 오브젝트 중 청크 이름과 필수 컴포넌트가 있는 항목을 관리 목록에 등록한다.
    private void RegisterExistingChunks()
    {
        foreach (Transform child in owner.transform)
        {
            if (!TryParseChunkCoord(child.name, out Vector3Int chunkCoord))
            {
                continue;
            }

            MeshFilter meshFilter = child.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = child.GetComponent<MeshRenderer>();
            MeshCollider meshCollider = child.GetComponent<MeshCollider>();

            if (meshFilter == null || meshRenderer == null || meshCollider == null)
            {
                continue;
            }

            chunks[chunkCoord] = new ChunkData
            {
                gameObject = child.gameObject,
                meshFilter = meshFilter,
                meshRenderer = meshRenderer,
                meshCollider = meshCollider
            };
        }
    }

    // Chunk_x_y_z 형식의 오브젝트 이름에서 청크 좌표를 읽는다.
    private static bool TryParseChunkCoord(string objectName, out Vector3Int chunkCoord)
    {
        chunkCoord = Vector3Int.zero;
        string[] parts = objectName.Split('_');

        if (parts.Length != 4 || parts[0] != "Chunk" ||
            !int.TryParse(parts[1], out int x) ||
            !int.TryParse(parts[2], out int y) ||
            !int.TryParse(parts[3], out int z))
        {
            return false;
        }

        chunkCoord = new Vector3Int(x, y, z);
        return true;
    }

    // 지형에 지정된 재질을 우선 사용하고 없으면 부모 렌더러의 재질을 가져온다.
    private Material ResolveMaterial()
    {
        if (owner.Material != null)
        {
            return owner.Material;
        }

        MeshRenderer parentRenderer = owner.GetComponent<MeshRenderer>();
        return parentRenderer != null ? parentRenderer.sharedMaterial : null;
    }

    // 렌더링 메시와 충돌 메시를 함께 교체하고 이전에 생성한 메시를 해제한다.
    private void SetChunkMesh(ChunkData chunk, Mesh mesh)
    {
        Mesh oldMesh = chunk.meshFilter.sharedMesh;
        chunk.meshFilter.sharedMesh = mesh;
        using (ColliderMarker.Auto())
        {
            chunk.meshCollider.sharedMesh = null;
            chunk.meshCollider.sharedMesh = mesh;
        }
        if (mesh != null)
        {
            generatedMeshes.Add(mesh);
        }

        if (oldMesh == null || !generatedMeshes.Remove(oldMesh))
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(oldMesh);
        }
        else
        {
            Object.DestroyImmediate(oldMesh);
        }
    }

    // 현재 지형의 축별 청크 개수를 벗어난 청크를 제거한다.
    private void RemoveUnusedChunks(Vector3Int chunkCounts)
    {
        List<Vector3Int> unusedChunkCoords = new List<Vector3Int>();

        foreach (Vector3Int chunkCoord in chunks.Keys)
        {
            if (chunkCoord.x >= chunkCounts.x ||
                chunkCoord.y >= chunkCounts.y ||
                chunkCoord.z >= chunkCounts.z)
            {
                unusedChunkCoords.Add(chunkCoord);
            }
        }

        foreach (Vector3Int chunkCoord in unusedChunkCoords)
        {
            DestroyChunk(chunkCoord);
        }
    }
}
