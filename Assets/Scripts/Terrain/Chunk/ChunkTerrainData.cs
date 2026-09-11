using Unity.Collections;
using UnityEngine;

public struct ChunkTerrainData
{
    public Vector3Int Origin;
    public Vector3Int CubeCount;
    public Vector3Int SampleCount;
    public NativeArray<float> Densities;

    public ChunkTerrainData(Vector3Int origin, Vector3Int cubeCount, Vector3Int sampleCount)
    {
        Origin = origin;
        CubeCount = cubeCount;
        SampleCount = sampleCount;
        Densities = new NativeArray<float>(
            sampleCount.x * sampleCount.y * sampleCount.z, Allocator.Persistent);
    }

    public float GetDensity(Vector3Int localIndex)
    {
        int index = (localIndex.x * SampleCount.y + localIndex.y) * SampleCount.z + localIndex.z;
        return Densities[index];
    }

    public void SetDensity(Vector3Int localIndex, float density)
    {
        int index = (localIndex.x * SampleCount.y + localIndex.y) * SampleCount.z + localIndex.z;
        Densities[index] = density;
    }
}
