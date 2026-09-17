using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

// 한 청크에서 표면 침식 또는 구 영역의 밀도 추가를 적용하는 Burst Job이다.
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
    // 굴착 시작 상태의 표면에서 계산한 endpoint별 최대 weight
    [ReadOnly] public NativeArray<float> ErosionWeights;
    public Vector3Int ErosionOrigin;
    public Vector3Int ErosionSampleCount;
    // 처리한 샘플의 최소·최대 격자 좌표
    [WriteOnly] public NativeArray<Vector3Int> ChangedBounds;

    // 굴착은 확정된 표면 weight를, 쌓기는 샘플 위치의 falloff를 적용한다.
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
                    float falloff;
                    if (Power < 0f)
                    {
                        Vector3Int weightIndex = index - ErosionOrigin;
                        int weightFlatIndex = (weightIndex.x * ErosionSampleCount.y + weightIndex.y)
                            * ErosionSampleCount.z + weightIndex.z;
                        falloff = ErosionWeights[weightFlatIndex];
                        if (falloff <= 0f)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        float distance = Vector3.Distance(new Vector3(x, y, z) * Resolution, LocalPosition);
                        if (distance > Radius)
                        {
                            continue;
                        }

                        float t = 1f - distance / Radius;
                        falloff = t * t * (3f - 2f * t);
                    }

                    Vector3Int localIndex = index - Origin;
                    int flatIndex = (localIndex.x * SampleCount.y + localIndex.y) * SampleCount.z + localIndex.z;
                    float before = Densities[flatIndex];
                    float after = Mathf.Clamp01(before + Power * falloff);
                    if (Power < 0f && after == before)
                    {
                        continue;
                    }
                    Densities[flatIndex] = after;
                    // 기존 고체는 유지하고, 빈 공간에 누적되는 밀도에만 인공 지형을 기록한다.
                    if (after > before && before <= DensityThreshold)
                    {
                        TypeIds[flatIndex] = TerrainData.ArtificialTypeId;
                    }
                    // 쌓기의 기존 bounds 처리는 유지하고, 굴착은 실제 변경만 기록한다.
                    minChanged = Vector3Int.Min(minChanged, index);
                    maxChanged = Vector3Int.Max(maxChanged, index);
                }
            }
        }

        ChangedBounds[0] = minChanged;
        ChangedBounds[1] = maxChanged;
    }
}
