using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

// 청크 데이터와 오브젝트를 보관하고 활성 상태 및 생성된 메시의 수명을 관리한다.
public class TerrainChunkRegistry : System.IDisposable
{
    #region 필드 및 속성

    // 청크의 부모 지형과 충돌 메시 적용 시간 측정
    private readonly TerrainManager owner;
    private static readonly ProfilerMarker ColliderMarker = new ProfilerMarker("TerrainMesh.Collider");
    // 등록된 청크와 직접 생성하여 해제해야 하는 메시
    private readonly Dictionary<Vector3Int, ChunkData> chunks =
        new Dictionary<Vector3Int, ChunkData>();
    private readonly HashSet<Mesh> generatedMeshes = new HashSet<Mesh>();

    public ICollection<Vector3Int> ChunkCoordinates => chunks.Keys;

    #endregion

    #region 초기화 및 기존 청크 등록

    // 부모 지형을 연결하고 기존 자식 청크를 등록한다.
    public TerrainChunkRegistry(TerrainManager owner)
    {
        this.owner = owner;
        RegisterExistingChunks();
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

    #endregion

    #region 청크 활성화

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

    #endregion

    #region 청크 생성 및 메시 교체

    // 등록된 청크를 반환하거나 지형의 레이어·태그·재질을 사용하는 청크를 생성한다.
    public ChunkData GetOrCreateChunk(Vector3Int chunkCoord)
    {
        if (chunks.TryGetValue(chunkCoord, out ChunkData chunk))
        {
            return chunk;
        }

        GameObject chunkObject =
            new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}_{chunkCoord.z}");
        chunkObject.SetActive(false);
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

        chunk.meshRenderer.sharedMaterial = owner.Material;
        chunks[chunkCoord] = chunk;
        return chunk;
    }

    // 렌더링 메시와 충돌 메시를 함께 교체하고 이전에 생성한 메시를 해제한다.
    public void SetChunkMesh(ChunkData chunk, Mesh mesh)
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

    #endregion

    #region 청크 제거 및 자원 해제

    // 등록된 모든 청크 오브젝트와 메시를 제거한다.
    public void ClearChunks()
    {
        foreach (Vector3Int coordinate in new List<Vector3Int>(chunks.Keys))
        {
            DestroyChunk(coordinate);
        }
    }

    // 현재 지형의 축별 청크 개수를 벗어난 청크를 제거한다.
    public void RemoveUnusedChunks(Vector3Int chunkCounts)
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

    // 직접 생성한 메시를 해제하고 청크 관리 목록을 비운다.
    public void Dispose()
    {
        // Child components may already be destroyed when the owner's OnDestroy runs.
        foreach (Mesh mesh in generatedMeshes)
        {
            if (Application.isPlaying) Object.Destroy(mesh);
            else Object.DestroyImmediate(mesh);
        }
        generatedMeshes.Clear();
        chunks.Clear();
    }

    #endregion
}
