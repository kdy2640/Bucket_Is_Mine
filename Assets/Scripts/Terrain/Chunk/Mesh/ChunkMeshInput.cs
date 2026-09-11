using System;
using Unity.Collections;
using UnityEngine;

// Borrows original density arrays. Only the main-thread constructor accesses TerrainData.
internal struct ChunkMeshInput
{
    public Vector3Int Origin;
    public Vector3Int CubeCount;
    public int Width;
    public int Height;
    public float Resolution;

    private Vector3Int chunkCoord;
    private Vector3Int chunkCounts;
    private int chunkSize;

    // Eight corner-owner combinations and single-axis gradient neighbors.
    // A one-cell chunk can read two chunks ahead for the gradient at its upper corner.
    [ReadOnly] private NativeArray<float> density000;
    [ReadOnly] private NativeArray<float> density001;
    [ReadOnly] private NativeArray<float> density010;
    [ReadOnly] private NativeArray<float> density011;
    [ReadOnly] private NativeArray<float> density100;
    [ReadOnly] private NativeArray<float> density101;
    [ReadOnly] private NativeArray<float> density110;
    [ReadOnly] private NativeArray<float> density111;
    [ReadOnly] private NativeArray<float> densityN00;
    [ReadOnly] private NativeArray<float> densityN01;
    [ReadOnly] private NativeArray<float> densityN10;
    [ReadOnly] private NativeArray<float> densityN11;
    [ReadOnly] private NativeArray<float> density200;
    [ReadOnly] private NativeArray<float> density201;
    [ReadOnly] private NativeArray<float> density210;
    [ReadOnly] private NativeArray<float> density211;
    [ReadOnly] private NativeArray<float> density0N0;
    [ReadOnly] private NativeArray<float> density1N0;
    [ReadOnly] private NativeArray<float> density0N1;
    [ReadOnly] private NativeArray<float> density1N1;
    [ReadOnly] private NativeArray<float> density020;
    [ReadOnly] private NativeArray<float> density120;
    [ReadOnly] private NativeArray<float> density021;
    [ReadOnly] private NativeArray<float> density121;
    [ReadOnly] private NativeArray<float> density00N;
    [ReadOnly] private NativeArray<float> density01N;
    [ReadOnly] private NativeArray<float> density10N;
    [ReadOnly] private NativeArray<float> density11N;
    [ReadOnly] private NativeArray<float> density002;
    [ReadOnly] private NativeArray<float> density012;
    [ReadOnly] private NativeArray<float> density102;
    [ReadOnly] private NativeArray<float> density112;

    public ChunkMeshInput(TerrainData data, Vector3Int chunkCoord)
    {
        ChunkTerrainData chunk = data.GetChunkData(chunkCoord);
        Origin = chunk.Origin;
        CubeCount = chunk.CubeCount;
        Width = data.Width;
        Height = data.DensityFieldHeight;
        Resolution = data.Resolution;
        this.chunkCoord = chunkCoord;
        chunkCounts = data.ChunkCounts;
        chunkSize = data.ChunkSize;

        int xN = Mathf.Max(chunkCoord.x - 1, 0);
        int x0 = chunkCoord.x;
        int x1 = Mathf.Min(chunkCoord.x + 1, chunkCounts.x - 1);
        int x2 = Mathf.Min(chunkCoord.x + (chunkSize == 1 ? 2 : 1), chunkCounts.x - 1);
        int yN = Mathf.Max(chunkCoord.y - 1, 0);
        int y0 = chunkCoord.y;
        int y1 = Mathf.Min(chunkCoord.y + 1, chunkCounts.y - 1);
        int y2 = Mathf.Min(chunkCoord.y + (chunkSize == 1 ? 2 : 1), chunkCounts.y - 1);
        int zN = Mathf.Max(chunkCoord.z - 1, 0);
        int z0 = chunkCoord.z;
        int z1 = Mathf.Min(chunkCoord.z + 1, chunkCounts.z - 1);
        int z2 = Mathf.Min(chunkCoord.z + (chunkSize == 1 ? 2 : 1), chunkCounts.z - 1);
        density000 = data.GetChunkData(new Vector3Int(x0, y0, z0)).Densities;
        density001 = data.GetChunkData(new Vector3Int(x0, y0, z1)).Densities;
        density010 = data.GetChunkData(new Vector3Int(x0, y1, z0)).Densities;
        density011 = data.GetChunkData(new Vector3Int(x0, y1, z1)).Densities;
        density100 = data.GetChunkData(new Vector3Int(x1, y0, z0)).Densities;
        density101 = data.GetChunkData(new Vector3Int(x1, y0, z1)).Densities;
        density110 = data.GetChunkData(new Vector3Int(x1, y1, z0)).Densities;
        density111 = data.GetChunkData(new Vector3Int(x1, y1, z1)).Densities;
        densityN00 = data.GetChunkData(new Vector3Int(xN, y0, z0)).Densities;
        densityN01 = data.GetChunkData(new Vector3Int(xN, y0, z1)).Densities;
        densityN10 = data.GetChunkData(new Vector3Int(xN, y1, z0)).Densities;
        densityN11 = data.GetChunkData(new Vector3Int(xN, y1, z1)).Densities;
        density200 = data.GetChunkData(new Vector3Int(x2, y0, z0)).Densities;
        density201 = data.GetChunkData(new Vector3Int(x2, y0, z1)).Densities;
        density210 = data.GetChunkData(new Vector3Int(x2, y1, z0)).Densities;
        density211 = data.GetChunkData(new Vector3Int(x2, y1, z1)).Densities;
        density0N0 = data.GetChunkData(new Vector3Int(x0, yN, z0)).Densities;
        density1N0 = data.GetChunkData(new Vector3Int(x1, yN, z0)).Densities;
        density0N1 = data.GetChunkData(new Vector3Int(x0, yN, z1)).Densities;
        density1N1 = data.GetChunkData(new Vector3Int(x1, yN, z1)).Densities;
        density020 = data.GetChunkData(new Vector3Int(x0, y2, z0)).Densities;
        density120 = data.GetChunkData(new Vector3Int(x1, y2, z0)).Densities;
        density021 = data.GetChunkData(new Vector3Int(x0, y2, z1)).Densities;
        density121 = data.GetChunkData(new Vector3Int(x1, y2, z1)).Densities;
        density00N = data.GetChunkData(new Vector3Int(x0, y0, zN)).Densities;
        density01N = data.GetChunkData(new Vector3Int(x0, y1, zN)).Densities;
        density10N = data.GetChunkData(new Vector3Int(x1, y0, zN)).Densities;
        density11N = data.GetChunkData(new Vector3Int(x1, y1, zN)).Densities;
        density002 = data.GetChunkData(new Vector3Int(x0, y0, z2)).Densities;
        density012 = data.GetChunkData(new Vector3Int(x0, y1, z2)).Densities;
        density102 = data.GetChunkData(new Vector3Int(x1, y0, z2)).Densities;
        density112 = data.GetChunkData(new Vector3Int(x1, y1, z2)).Densities;
    }

    public readonly float GetDensity(Vector3Int index)
    {
        Vector3Int owner = new Vector3Int(
            Mathf.Min(index.x / chunkSize, chunkCounts.x - 1),
            Mathf.Min(index.y / chunkSize, chunkCounts.y - 1),
            Mathf.Min(index.z / chunkSize, chunkCounts.z - 1));
        Vector3Int origin = owner * chunkSize;
        Vector3Int localIndex = index - origin;
        int sampleCountY = Mathf.Min(chunkSize, Height - origin.y) +
            (owner.y == chunkCounts.y - 1 ? 1 : 0);
        int sampleCountZ = Mathf.Min(chunkSize, Width - origin.z) +
            (owner.z == chunkCounts.z - 1 ? 1 : 0);
        int flatIndex = (localIndex.x * sampleCountY + localIndex.y) * sampleCountZ + localIndex.z;
        Vector3Int offset = owner - chunkCoord;
        int sourceIndex = (offset.x + 1) * 16 + (offset.y + 1) * 4 + offset.z + 1;

        switch (sourceIndex)
        {
            case 21: return density000[flatIndex];
            case 22: return density001[flatIndex];
            case 25: return density010[flatIndex];
            case 26: return density011[flatIndex];
            case 37: return density100[flatIndex];
            case 38: return density101[flatIndex];
            case 41: return density110[flatIndex];
            case 42: return density111[flatIndex];
            case 5: return densityN00[flatIndex];
            case 6: return densityN01[flatIndex];
            case 9: return densityN10[flatIndex];
            case 10: return densityN11[flatIndex];
            case 53: return density200[flatIndex];
            case 54: return density201[flatIndex];
            case 57: return density210[flatIndex];
            case 58: return density211[flatIndex];
            case 17: return density0N0[flatIndex];
            case 33: return density1N0[flatIndex];
            case 18: return density0N1[flatIndex];
            case 34: return density1N1[flatIndex];
            case 29: return density020[flatIndex];
            case 45: return density120[flatIndex];
            case 30: return density021[flatIndex];
            case 46: return density121[flatIndex];
            case 20: return density00N[flatIndex];
            case 24: return density01N[flatIndex];
            case 36: return density10N[flatIndex];
            case 40: return density11N[flatIndex];
            case 23: return density002[flatIndex];
            case 27: return density012[flatIndex];
            case 39: return density102[flatIndex];
            case 43: return density112[flatIndex];
            default: throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
