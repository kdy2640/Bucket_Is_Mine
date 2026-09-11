using System;
using System.Collections.Generic;
using UnityEngine;

public class TerrainData : IDisposable
{
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
        minChangedIndex = new Vector3Int(Width, DensityFieldHeight, Width);
        maxChangedIndex = Vector3Int.zero;

        if (radius <= 0f || Mathf.Approximately(power, 0f))
        {
            return false;
        }

        Vector3Int center = PositionToIndex(localPosition);
        int indexRadius = Mathf.CeilToInt(radius / Resolution);
        bool changed = false;

        for (int x = -indexRadius; x <= indexRadius; x++)
        {
            for (int y = -indexRadius; y <= indexRadius; y++)
            {
                for (int z = -indexRadius; z <= indexRadius; z++)
                {
                    Vector3Int index = center + new Vector3Int(x, y, z);
                    if (!IsValidIndex(index))
                    {
                        continue;
                    }

                    float distance = Vector3.Distance(IndexToPosition(index), localPosition);
                    if (distance > radius)
                    {
                        continue;
                    }

                    float t = 1f - distance / radius;
                    float falloff = t * t * (3f - 2f * t);
                    SetDensity(index, GetDensity(index) + power * falloff);
                    minChangedIndex = Vector3Int.Min(minChangedIndex, index);
                    maxChangedIndex = Vector3Int.Max(maxChangedIndex, index);
                    changed = true;
                }
            }
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
