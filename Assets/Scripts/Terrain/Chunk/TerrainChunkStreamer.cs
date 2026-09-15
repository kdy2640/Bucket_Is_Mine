using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public sealed class TerrainChunkStreamer
{
    [SerializeField, Min(0f)] private float loadDistance = 110f;
    [SerializeField, Min(0f)] private float unloadDistance = 120f;
    [FormerlySerializedAs("maxChunkLoadsPerFrame")]
    [SerializeField, Min(1)] private int maxChunkActivationsPerFrame = 64;

    private readonly Queue<Vector3Int> pendingActivations = new Queue<Vector3Int>();
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
    public int PendingActivationCount => pendingActivations.Count;

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
        ActivateImmediateNeighbors();
        RefreshActivationCoordinates();
        IsInitialLoadComplete = pendingActivations.Count == 0;
    }

    public void Tick()
    {
        if (!initialized)
        {
            return;
        }

        UpdateTarget();
        int count = Mathf.Min(Mathf.Max(1, maxChunkActivationsPerFrame), pendingActivations.Count);
        for (int i = 0; i < count; i++)
        {
            chunkManager.SetChunkActive(pendingActivations.Dequeue(), true);
        }

        if (pendingActivations.Count == 0)
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
            // Also covers teleporting: enable support before hiding the old neighborhood.
            ActivateImmediateNeighbors();
            RefreshActivationCoordinates();
        }
    }

    public void Reset()
    {
        initialized = false;
        IsInitialLoadComplete = false;
        pendingActivations.Clear();
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

    private void ActivateImmediateNeighbors()
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
                    chunkManager.SetChunkActive(new Vector3Int(x, y, z), true);
                }
            }
        }
    }

    private void RefreshActivationCoordinates()
    {
        coordinates.Clear();
        foreach (Vector3Int coordinate in chunkManager.ChunkCoordinates)
        {
            Vector3Int offset = coordinate - targetCoordinate;
            if (chunkManager.IsChunkActive(coordinate) &&
                (Mathf.Abs(offset.x) > unloadRadius.x ||
                Mathf.Abs(offset.y) > unloadRadius.y ||
                Mathf.Abs(offset.z) > unloadRadius.z))
            {
                coordinates.Add(coordinate);
            }
        }
        foreach (Vector3Int coordinate in coordinates)
        {
            chunkManager.SetChunkActive(coordinate, false);
        }

        pendingActivations.Clear();
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
                    if (!chunkManager.IsChunkActive(coordinate))
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
            pendingActivations.Enqueue(coordinate);
        }
    }
}
