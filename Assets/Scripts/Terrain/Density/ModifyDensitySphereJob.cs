using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

// 한 청크에서 구 영역에 포함된 샘플의 밀도를 수정하는 Burst Job이다.
[BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.High)]
internal struct ModifyDensitySphereJob : IJob
{
    // 수정 대상 청크의 밀도 배열과 격자 정보
    public NativeArray<float> Densities;
    public NativeArray<byte> TypeIds;
    public float DensityThreshold;
    public Vector3Int Origin;
    public Vector3Int SampleCount;
    // 이번 Job이 처리할 전체 격자 좌표 범위
    public Vector3Int MinIndex;
    public Vector3Int MaxIndex;
    // 지형 로컬 좌표 기준 구 영역과 밀도 변화량
    public Vector3 LocalPosition;
    public float Resolution;
    public float Radius;
    public float Power;
    // 처리한 샘플의 최소·최대 격자 좌표
    [WriteOnly] public NativeArray<Vector3Int> ChangedBounds;

    // 구 중심에서 멀어질수록 밀도 변화량을 줄이고 처리한 샘플의 경계를 기록한다.
    public void Execute()
    {
        Vector3Int minChanged = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
        Vector3Int maxChanged = new Vector3Int(-1, -1, -1);
        for (int x = MinIndex.x; x <= MaxIndex.x; x++)
        {
            for (int y = MinIndex.y; y <= MaxIndex.y; y++)
            {
                for (int z = MinIndex.z; z <= MaxIndex.z; z++)
                {
                    Vector3Int index = new Vector3Int(x, y, z);
                    float distance = Vector3.Distance(new Vector3(x, y, z) * Resolution, LocalPosition);
                    if (distance > Radius)
                    {
                        continue;
                    }

                    float t = 1f - distance / Radius;
                    float falloff = t * t * (3f - 2f * t);
                    Vector3Int localIndex = index - Origin;
                    int flatIndex = (localIndex.x * SampleCount.y + localIndex.y) * SampleCount.z + localIndex.z;
                    float before = Densities[flatIndex];
                    float after = Mathf.Clamp01(before + Power * falloff);
                    Densities[flatIndex] = after;
                    // 기존 고체는 유지하고, 빈 공간에 누적되는 밀도에만 인공 지형을 기록한다.
                    if (after > before && before <= DensityThreshold)
                    {
                        TypeIds[flatIndex] = TerrainData.ArtificialTypeId;
                    }
                    // Preserve the existing bounds even when clamping leaves density unchanged.
                    minChanged = Vector3Int.Min(minChanged, index);
                    maxChanged = Vector3Int.Max(maxChanged, index);
                }
            }
        }

        ChangedBounds[0] = minChanged;
        ChangedBounds[1] = maxChanged;
    }
}
