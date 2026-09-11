using System.Collections.Generic;
using UnityEngine;

public class TerrainChunkManager
{
    private readonly TerrainManager owner;
    private readonly Dictionary<Vector3Int, ChunkData> chunks =
        new Dictionary<Vector3Int, ChunkData>();

    public TerrainChunkManager(TerrainManager owner)
    {
        this.owner = owner;
        RegisterExistingChunks();
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

    private void RegenerateChunk(Vector3Int chunkCoord)
    {
        TerrainData data = owner.Data;
        ChunkData chunk = GetOrCreateChunk(chunkCoord);
        MarchingCubesMesher mesher = new MarchingCubesMesher(
            data,
            owner.DensityThreshold,
            owner.IsSmoothShading);

        SetChunkMesh(chunk, mesher.BuildChunkMesh(chunkCoord));
    }

    private Vector3Int GetChunkCounts()
    {
        return owner.Data.ChunkCounts;
    }

    private Vector3Int CubeIndexToChunkCoord(Vector3Int cubeIndex)
    {
        int chunkSize = owner.Data.ChunkSize;
        return new Vector3Int(
            cubeIndex.x / chunkSize,
            cubeIndex.y / chunkSize,
            cubeIndex.z / chunkSize);
    }

    private ChunkData GetOrCreateChunk(Vector3Int chunkCoord)
    {
        if (chunks.TryGetValue(chunkCoord, out ChunkData chunk))
        {
            return chunk;
        }

        GameObject chunkObject =
            new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}_{chunkCoord.z}");
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

    private Material ResolveMaterial()
    {
        if (owner.Material != null)
        {
            return owner.Material;
        }

        MeshRenderer parentRenderer = owner.GetComponent<MeshRenderer>();
        return parentRenderer != null ? parentRenderer.sharedMaterial : null;
    }

    private static void SetChunkMesh(ChunkData chunk, Mesh mesh)
    {
        Mesh oldMesh = chunk.meshFilter.sharedMesh;
        chunk.meshFilter.sharedMesh = mesh;
        chunk.meshCollider.sharedMesh = null;
        chunk.meshCollider.sharedMesh = mesh;

        if (oldMesh == null)
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
