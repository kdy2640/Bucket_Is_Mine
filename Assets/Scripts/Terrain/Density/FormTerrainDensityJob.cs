using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

// 각 실행이 청크의 밀도 샘플 하나를 계산하고 자신의 배열 위치에 기록한다.
[BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.High)]
internal struct FormTerrainDensityJob : IJobParallelFor
{
    [WriteOnly] public NativeArray<float> Densities;
    [WriteOnly] public NativeArray<byte> TypeIds;
    [ReadOnly] public NativeArray<TerrainLayer> Layers;
    public float Resolution;
    public Vector3Int Origin;
    public Vector3Int SampleCount;
    [ReadOnly] public NativeArray<float> SurfaceHeights;
    public int SurfaceWidth;
    public int NoiseSeed;
    public float NoiseScale;
    public float DensityThreshold;
    public bool Use3DNoise;

    public void Execute(int index)
    {
        int z = index % SampleCount.z + Origin.z;
        int xy = index / SampleCount.z;
        int y = xy % SampleCount.y + Origin.y;
        int x = xy / SampleCount.y + Origin.x;

        float density;
        if (Use3DNoise)
        {
            density = PerlinNoise3D.GetRandomValue(
                x * NoiseScale, y * NoiseScale, z * NoiseScale, NoiseSeed);
        }
        else
        {
            float surfaceY = SurfaceHeights[x * SurfaceWidth + z];
            density = (surfaceY - y) * 0.1f + DensityThreshold;
        }

        Densities[index] = Mathf.Clamp01(density);

        float localY = y * Resolution;
        byte typeId = Layers[0].TypeId;
        for (int i = 1; i < Layers.Length; i++)
        {
            if (localY < Layers[i].YStart) break;
            typeId = Layers[i].TypeId;
        }
        TypeIds[index] = typeId;
    }
}
