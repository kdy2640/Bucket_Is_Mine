using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;

public class TerrainData : IDisposable
{
    private static readonly ProfilerMarker ModifyMarker = new ProfilerMarker("TerrainDensity.Modify");
    private static readonly ProfilerMarker ScheduleMarker = new ProfilerMarker("TerrainDensity.Schedule");
    private static readonly ProfilerMarker CompleteMarker = new ProfilerMarker("TerrainDensity.Complete");
    private readonly Dictionary<Vector3Int, ChunkTerrainData> chunks =
        new Dictionary<Vector3Int, ChunkTerrainData>();

    public int Width { get; }
    public int DensityFieldHeight { get; }
    public float Resolution { get; }
    public int ChunkSize { get; }
    public Vector3Int ChunkCounts { get; }

    public TerrainData(int width, int densityFieldHeight, float resolution, int chunkSize)
    {
        Width = Mathf.Max(1, width);
        DensityFieldHeight = Mathf.Max(1, densityFieldHeight);
        Resolution = Mathf.Max(0.001f, resolution);
        ChunkSize = Mathf.Max(1, chunkSize);
        ChunkCounts = new Vector3Int(
            Mathf.CeilToInt((float)Width / ChunkSize),
            Mathf.CeilToInt((float)DensityFieldHeight / ChunkSize),
            Mathf.CeilToInt((float)Width / ChunkSize));

        for (int x = 0; x < ChunkCounts.x; x++)
        {
            for (int y = 0; y < ChunkCounts.y; y++)
            {
                for (int z = 0; z < ChunkCounts.z; z++)
                {
                    Vector3Int chunkCoord = new Vector3Int(x, y, z);
                    Vector3Int origin = chunkCoord * ChunkSize;
                    Vector3Int cubeCount = new Vector3Int(
                        Mathf.Min(ChunkSize, Width - origin.x),
                        Mathf.Min(ChunkSize, DensityFieldHeight - origin.y),
                        Mathf.Min(ChunkSize, Width - origin.z));

                    // Only the last chunk on each axis owns the terrain's endpoint sample.
                    Vector3Int sampleCount = cubeCount + new Vector3Int(
                        x == ChunkCounts.x - 1 ? 1 : 0,
                        y == ChunkCounts.y - 1 ? 1 : 0,
                        z == ChunkCounts.z - 1 ? 1 : 0);
                    chunks.Add(chunkCoord, new ChunkTerrainData(origin, cubeCount, sampleCount));
                }
            }
        }
    }

    public void ResetDensities()
    {
        foreach (ChunkTerrainData chunk in chunks.Values)
        {
            var densities = chunk.Densities;
            for (int i = 0; i < densities.Length; i++)
            {
                densities[i] = 0f;
            }
        }
    }

    // The returned struct borrows its array; TerrainData alone disposes it.
    public ChunkTerrainData GetChunkData(Vector3Int chunkCoord)
    {
        return chunks[chunkCoord];
    }

    public void Dispose()
    {
        foreach (ChunkTerrainData chunk in chunks.Values)
        {
            chunk.Densities.Dispose();
        }

        chunks.Clear();
    }

    public float GetDensity(Vector3Int index)
    {
        if (!IsValidIndex(index))
        {
            return 0f;
        }

        Vector3Int chunkCoord = new Vector3Int(
            Mathf.Min(index.x / ChunkSize, ChunkCounts.x - 1),
            Mathf.Min(index.y / ChunkSize, ChunkCounts.y - 1),
            Mathf.Min(index.z / ChunkSize, ChunkCounts.z - 1));
        ChunkTerrainData chunk = chunks[chunkCoord];
        return chunk.GetDensity(index - chunk.Origin);
    }

    public void SetDensity(Vector3Int index, float density)
    {
        if (IsValidIndex(index))
        {
            Vector3Int chunkCoord = new Vector3Int(
                Mathf.Min(index.x / ChunkSize, ChunkCounts.x - 1),
                Mathf.Min(index.y / ChunkSize, ChunkCounts.y - 1),
                Mathf.Min(index.z / ChunkSize, ChunkCounts.z - 1));
            ChunkTerrainData chunk = chunks[chunkCoord];
            chunk.SetDensity(index - chunk.Origin, Mathf.Clamp01(density));
        }
    }

    public Vector3Int PositionToIndex(Vector3 localPosition)
    {
        return new Vector3Int(
            Mathf.RoundToInt(localPosition.x / Resolution),
            Mathf.RoundToInt(localPosition.y / Resolution),
            Mathf.RoundToInt(localPosition.z / Resolution));
    }

    public Vector3 IndexToPosition(Vector3Int index)
    {
        return new Vector3(index.x, index.y, index.z) * Resolution;
    }

    public bool ModifyDensitySphere(
        Vector3 localPosition,
        float radius,
        float power,
        out Vector3Int minChangedIndex,
        out Vector3Int maxChangedIndex)
    {
        using var modifyScope = ModifyMarker.Auto();
        minChangedIndex = new Vector3Int(Width, DensityFieldHeight, Width);
        maxChangedIndex = Vector3Int.zero;

        if (radius <= 0f || Mathf.Approximately(power, 0f))
        {
            return false;
        }

        Vector3Int center = PositionToIndex(localPosition);
        int indexRadius = Mathf.CeilToInt(radius / Resolution);
        Vector3Int extent = Vector3Int.one * indexRadius;
        Vector3Int minIndex = Vector3Int.Max(center - extent, Vector3Int.zero);
        Vector3Int maxIndex = Vector3Int.Min(center + extent, minChangedIndex);
        if (minIndex.x > maxIndex.x || minIndex.y > maxIndex.y || minIndex.z > maxIndex.z)
        {
            return false;
        }

        Vector3Int minChunk = new Vector3Int(
            Mathf.Min(minIndex.x / ChunkSize, ChunkCounts.x - 1),
            Mathf.Min(minIndex.y / ChunkSize, ChunkCounts.y - 1),
            Mathf.Min(minIndex.z / ChunkSize, ChunkCounts.z - 1));
        Vector3Int maxChunk = new Vector3Int(
            Mathf.Min(maxIndex.x / ChunkSize, ChunkCounts.x - 1),
            Mathf.Min(maxIndex.y / ChunkSize, ChunkCounts.y - 1),
            Mathf.Min(maxIndex.z / ChunkSize, ChunkCounts.z - 1));
        Vector3Int count = maxChunk - minChunk + Vector3Int.one;
        int jobCount = count.x * count.y * count.z;
        NativeArray<JobHandle> handles = new NativeArray<JobHandle>(jobCount, Allocator.Temp);
        NativeArray<Vector3Int>[] changedBounds = new NativeArray<Vector3Int>[jobCount];

        using (ScheduleMarker.Auto())
        {
            int i = 0;
            for (int x = minChunk.x; x <= maxChunk.x; x++)
            {
                for (int y = minChunk.y; y <= maxChunk.y; y++)
                {
                    for (int z = minChunk.z; z <= maxChunk.z; z++)
                    {
                        ChunkTerrainData chunk = chunks[new Vector3Int(x, y, z)];
                        // Each Job owns a separate density array and result buffer.
                        changedBounds[i] = new NativeArray<Vector3Int>(2, Allocator.TempJob);
                        ModifyDensitySphereJob job = new ModifyDensitySphereJob
                        {
                            Densities = chunk.Densities,
                            Origin = chunk.Origin,
                            SampleCount = chunk.SampleCount,
                            MinIndex = Vector3Int.Max(minIndex, chunk.Origin),
                            MaxIndex = Vector3Int.Min(maxIndex, chunk.Origin + chunk.SampleCount - Vector3Int.one),
                            LocalPosition = localPosition,
                            Resolution = Resolution,
                            Radius = radius,
                            Power = power,
                            ChangedBounds = changedBounds[i]
                        };
                        handles[i] = job.Schedule();
                        i++;
                    }
                }
            }
        }

        using (CompleteMarker.Auto())
        {
            JobHandle.CompleteAll(handles);
        }
        handles.Dispose();

        bool changed = false;
        for (int i = 0; i < jobCount; i++)
        {
            Vector3Int min = changedBounds[i][0];
            Vector3Int max = changedBounds[i][1];
            if (min.x <= max.x)
            {
                minChangedIndex = Vector3Int.Min(minChangedIndex, min);
                maxChangedIndex = Vector3Int.Max(maxChangedIndex, max);
                changed = true;
            }
            changedBounds[i].Dispose();
        }

        return changed;
    }

    public bool IsValidIndex(Vector3Int index)
    {
        return index.x >= 0 && index.x <= Width &&
               index.y >= 0 && index.y <= DensityFieldHeight &&
               index.z >= 0 && index.z <= Width;
    }
}
