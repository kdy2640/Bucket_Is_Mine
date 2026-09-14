using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class TerrainChunkStreamer
{
    [SerializeField, Min(0f)] private float loadDistance = 110f;
    [SerializeField, Min(0f)] private float unloadDistance = 120f;
    [SerializeField, Min(1)] private int maxChunkLoadsPerFrame = 4;

    private readonly Queue<Vector3Int> pendingLoads = new Queue<Vector3Int>();
    private readonly List<Vector3Int> coordinates = new List<Vector3Int>();
    private TerrainManager owner;
    private TerrainChunkManager chunkManager;
    private Transform target;
    private Vector3Int targetCoordinate;
    private Vector3Int loadRadius;
    private Vector3Int unloadRadius;
    private Vector3 chunkWorldSize;
    private bool initialized;

    public bool IsInitialLoadComplete { get; private set; }
    public int PendingLoadCount => pendingLoads.Count;

    public void Initialize(TerrainManager terrain, TerrainChunkManager manager, Transform player)
    {
        Reset();
        owner = terrain;
        chunkManager = manager;
        target = player;
        chunkWorldSize = owner.transform.lossyScale * (owner.Data.ChunkSize * owner.Data.Resolution);
        chunkWorldSize = new Vector3(
            Mathf.Abs(chunkWorldSize.x), Mathf.Abs(chunkWorldSize.y), Mathf.Abs(chunkWorldSize.z));
        loadRadius = new Vector3Int(
            Mathf.Max(1, Mathf.CeilToInt(loadDistance / chunkWorldSize.x)),
            Mathf.Max(1, Mathf.CeilToInt(loadDistance / chunkWorldSize.y)),
            Mathf.Max(1, Mathf.CeilToInt(loadDistance / chunkWorldSize.z)));
        float releaseDistance = Mathf.Max(loadDistance, unloadDistance);
        unloadRadius = new Vector3Int(
            Mathf.Max(loadRadius.x + 1, Mathf.CeilToInt(releaseDistance / chunkWorldSize.x)),
            Mathf.Max(loadRadius.y + 1, Mathf.CeilToInt(releaseDistance / chunkWorldSize.y)),
            Mathf.Max(loadRadius.z + 1, Mathf.CeilToInt(releaseDistance / chunkWorldSize.z)));
        initialized = true;
        targetCoordinate = GetTargetCoordinate();
        LoadImmediateNeighbors();
        RefreshLoadingCoordinates();
        IsInitialLoadComplete = pendingLoads.Count == 0;
    }

    public void Tick()
    {
        if (!initialized)
        {
            return;
        }

        UpdateTarget();
        int count = Mathf.Min(Mathf.Max(1, maxChunkLoadsPerFrame), pendingLoads.Count);
        for (int i = 0; i < count; i++)
        {
            chunkManager.LoadChunk(pendingLoads.Dequeue());
        }

        if (pendingLoads.Count == 0)
        {
            IsInitialLoadComplete = true;
        }
    }

    public void UpdateTarget()
    {
        if (!initialized)
        {
            return;
        }

        Vector3Int nextCoordinate = GetTargetCoordinate();
        if (nextCoordinate != targetCoordinate)
        {
            targetCoordinate = nextCoordinate;
            // Also covers teleporting: build support before removing the old neighborhood.
            LoadImmediateNeighbors();
            RefreshLoadingCoordinates();
        }
    }

    public void Reset()
    {
        initialized = false;
        IsInitialLoadComplete = false;
        pendingLoads.Clear();
        coordinates.Clear();
    }

    private Vector3Int GetTargetCoordinate()
    {
        Vector3 position = owner.transform.InverseTransformPoint(target.position);
        float size = owner.Data.ChunkSize * owner.Data.Resolution;
        Vector3Int coordinate = new Vector3Int(
            Mathf.FloorToInt(position.x / size),
            Mathf.FloorToInt(position.y / size),
            Mathf.FloorToInt(position.z / size));
        // A spawn above the field must still prepare the closest ground immediately.
        return Vector3Int.Min(Vector3Int.Max(coordinate, Vector3Int.zero),
            owner.Data.ChunkCounts - Vector3Int.one);
    }

    private void LoadImmediateNeighbors()
    {
        Vector3Int min = Vector3Int.Max(targetCoordinate - Vector3Int.one, Vector3Int.zero);
        Vector3Int max = Vector3Int.Min(
            targetCoordinate + Vector3Int.one, owner.Data.ChunkCounts - Vector3Int.one);
        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                for (int z = min.z; z <= max.z; z++)
                {
                    chunkManager.LoadChunk(new Vector3Int(x, y, z));
                }
            }
        }
    }

    private void RefreshLoadingCoordinates()
    {
        coordinates.Clear();
        foreach (Vector3Int coordinate in chunkManager.LoadedCoordinates)
        {
            Vector3Int offset = coordinate - targetCoordinate;
            if (Mathf.Abs(offset.x) > unloadRadius.x ||
                Mathf.Abs(offset.y) > unloadRadius.y ||
                Mathf.Abs(offset.z) > unloadRadius.z)
            {
                coordinates.Add(coordinate);
            }
        }
        foreach (Vector3Int coordinate in coordinates)
        {
            chunkManager.UnloadChunk(coordinate);
        }

        pendingLoads.Clear();
        coordinates.Clear();
        Vector3Int min = Vector3Int.Max(targetCoordinate - loadRadius, Vector3Int.zero);
        Vector3Int max = Vector3Int.Min(
            targetCoordinate + loadRadius, owner.Data.ChunkCounts - Vector3Int.one);
        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                for (int z = min.z; z <= max.z; z++)
                {
                    Vector3Int coordinate = new Vector3Int(x, y, z);
                    if (!chunkManager.IsChunkLoaded(coordinate))
                    {
                        coordinates.Add(coordinate);
                    }
                }
            }
        }
        coordinates.Sort((a, b) =>
            Vector3.Scale(a - targetCoordinate, chunkWorldSize).sqrMagnitude.CompareTo(
                Vector3.Scale(b - targetCoordinate, chunkWorldSize).sqrMagnitude));
        foreach (Vector3Int coordinate in coordinates)
        {
            pendingLoads.Enqueue(coordinate);
        }
    }
}
