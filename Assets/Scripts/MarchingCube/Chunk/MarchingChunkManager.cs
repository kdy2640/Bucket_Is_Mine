using System.Collections.Generic;
using UnityEngine;

public class MarchingChunkManager
{
    private readonly MarchingCubes owner;
    private readonly Dictionary<Vector3Int, ChunkData> chunks = new Dictionary<Vector3Int, ChunkData>();

    public MarchingChunkManager(MarchingCubes owner)
    {
        this.owner = owner;
    }

    public int RegenerateAllChunks()
    {
        Vector3Int chunkCounts = GetChunkCounts();
        RemoveUnusedChunks(chunkCounts);
        int regeneratedCount = 0;

        for (int x = 0; x < chunkCounts.x; x++)
        {
            for (int y = 0; y < chunkCounts.y; y++)
            {
                for (int z = 0; z < chunkCounts.z; z++)
                {
                    RegenerateChunk(new Vector3Int(x, y, z));
                    regeneratedCount++;
                }
            }
        }

        return regeneratedCount;
    }

    public int RegenerateChunksInBounds(Vector3Int minIndex, Vector3Int maxIndex)
    {
        if (owner.Heights == null)
        {
            return 0;
        }

        Vector3Int minCube = new Vector3Int(
            Mathf.Clamp(minIndex.x - 1, 0, owner.Width - 1),
            Mathf.Clamp(minIndex.y - 1, 0, 2 * owner.Height - 1),
            Mathf.Clamp(minIndex.z - 1, 0, owner.Width - 1));

        Vector3Int maxCube = new Vector3Int(
            Mathf.Clamp(maxIndex.x, 0, owner.Width - 1),
            Mathf.Clamp(maxIndex.y, 0, 2 * owner.Height - 1),
            Mathf.Clamp(maxIndex.z, 0, owner.Width - 1));

        Vector3Int minChunk = CubeIndexToChunkCoord(minCube);
        Vector3Int maxChunk = CubeIndexToChunkCoord(maxCube);
        int regeneratedCount = 0;

        for (int x = minChunk.x; x <= maxChunk.x; x++)
        {
            for (int y = minChunk.y; y <= maxChunk.y; y++)
            {
                for (int z = minChunk.z; z <= maxChunk.z; z++)
                {
                    RegenerateChunk(new Vector3Int(x, y, z));
                    regeneratedCount++;
                }
            }
        }

        return regeneratedCount;
    }

    public void RegenerateChunk(Vector3Int chunkCoord)
    {
        ChunkData chunk = GetOrCreateChunk(chunkCoord);
        MarchingCubesMesher mesher = new MarchingCubesMesher(
            owner.Heights,
            owner.Width,
            owner.Height,
            owner.Resolution,
            owner.HeightThreshold,
            owner.isSmoothShading);

        Mesh mesh = mesher.BuildChunkMesh(chunkCoord, owner.ChunkSize);
        SetChunkMesh(chunk, mesh);
    }

    private void SetChunkMesh(ChunkData chunk, Mesh mesh)
    {
        Mesh oldMesh = chunk.meshFilter.sharedMesh;

        chunk.meshFilter.mesh = mesh;
        chunk.meshCollider.sharedMesh = null;
        chunk.meshCollider.sharedMesh = mesh;

        if (oldMesh != null)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(oldMesh);
            }
            else
            {
                Object.DestroyImmediate(oldMesh);
            }
        }
    }

    private Vector3Int GetChunkCounts()
    {
        int size = owner.ChunkSize;
        return new Vector3Int(
            Mathf.CeilToInt((float)owner.Width / size),
            Mathf.CeilToInt((float)(2 * owner.Height) / size),
            Mathf.CeilToInt((float)owner.Width / size));
    }

    private Vector3Int CubeIndexToChunkCoord(Vector3Int cubeIndex)
    {
        int size = owner.ChunkSize;
        return new Vector3Int(
            cubeIndex.x / size,
            cubeIndex.y / size,
            cubeIndex.z / size);
    }

    private ChunkData GetOrCreateChunk(Vector3Int chunkCoord)
    {
        if (chunks.TryGetValue(chunkCoord, out ChunkData chunk))
        {
            return chunk;
        }

        GameObject ownerObject = owner.gameObject;
        GameObject chunkObject = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}_{chunkCoord.z}");
        chunkObject.transform.SetParent(owner.transform, false);
        chunkObject.layer = ownerObject.layer;
        chunkObject.tag = ownerObject.tag;

        chunk = new ChunkData
        {
            gameObject = chunkObject,
            meshFilter = chunkObject.AddComponent<MeshFilter>(),
            meshRenderer = chunkObject.AddComponent<MeshRenderer>(),
            meshCollider = chunkObject.AddComponent<MeshCollider>()
        };

        MeshRenderer parentRenderer = owner.GetComponent<MeshRenderer>();
        if (owner.Material != null)
        {
            chunk.meshRenderer.sharedMaterial = owner.Material;
        }
        else if (parentRenderer != null)
        {
            chunk.meshRenderer.sharedMaterial = parentRenderer.sharedMaterial;
        }

        chunks[chunkCoord] = chunk;
        return chunk;
    }

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
            GameObject chunkObject = chunks[chunkCoord].gameObject;
            chunks.Remove(chunkCoord);

            if (Application.isPlaying)
            {
                Object.Destroy(chunkObject);
            }
            else
            {
                Object.DestroyImmediate(chunkObject);
            }
        }
    }
}
