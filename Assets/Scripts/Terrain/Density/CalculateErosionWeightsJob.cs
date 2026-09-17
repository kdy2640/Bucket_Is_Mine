using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

// 시작 밀도의 복사본에서 각 endpoint의 침식 weight를 독립적으로 계산한다.
[BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.High)]
internal struct CalculateErosionWeightsJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> Densities;
    public Vector3Int DensityOrigin;
    public Vector3Int DensitySampleCount;
    public Vector3Int MaxTerrainIndex;
    public Vector3Int WeightOrigin;
    public Vector3Int WeightSampleCount;
    public Vector3 LocalPosition;
    public float Resolution;
    public float Radius;
    public float DensityThreshold;
    public Vector3 WorldErosionDirection;
    public Matrix4x4 DensityNormalToWorld;
    public float ErosionSideStrength;
    public bool UseDistanceFalloff;
    [WriteOnly] public NativeArray<float> Weights;

    public void Execute(int flatIndex)
    {
        Vector3Int index = WeightOrigin + new Vector3Int(
            flatIndex / (WeightSampleCount.y * WeightSampleCount.z),
            flatIndex / WeightSampleCount.z % WeightSampleCount.y,
            flatIndex % WeightSampleCount.z);
        Vector3Int localIndex = index - DensityOrigin;
        int densityIndex = (localIndex.x * DensitySampleCount.y + localIndex.y)
            * DensitySampleCount.z + localIndex.z;
        float density = Densities[densityIndex];
        float maxWeight = 0f;
        Vector3 ownGradient = Vector3.zero;
        bool hasOwnGradient = false;

        for (int axis = 0; axis < 3; axis++)
        {
            int stride = axis == 0 ? DensitySampleCount.y * DensitySampleCount.z :
                (axis == 1 ? DensitySampleCount.z : 1);
            for (int direction = -1; direction <= 1; direction += 2)
            {
                Vector3Int neighbor = index;
                neighbor[axis] += direction;
                if (neighbor[axis] < WeightOrigin[axis] ||
                    neighbor[axis] >= WeightOrigin[axis] + WeightSampleCount[axis])
                {
                    continue;
                }

                int neighborDensityIndex = densityIndex + direction * stride;
                float neighborDensity = Densities[neighborDensityIndex];
                if ((density > DensityThreshold) == (neighborDensity > DensityThreshold))
                {
                    continue;
                }

                // 어느 endpoint에서 검사해도 기존 양의 축 방향과 같은 순서로 보간한다.
                Vector3Int startIndex = direction > 0 ? index : neighbor;
                Vector3Int endIndex = direction > 0 ? neighbor : index;
                float startDensity = direction > 0 ? density : neighborDensity;
                float endDensity = direction > 0 ? neighborDensity : density;
                float edgeT = (DensityThreshold - startDensity) / (endDensity - startDensity);
                Vector3 surfacePosition = Vector3.Lerp(
                    (Vector3)startIndex * Resolution, (Vector3)endIndex * Resolution, edgeT);
                float distance = Vector3.Distance(surfacePosition, LocalPosition);
                if (distance >= Radius)
                {
                    continue;
                }

                float weight = 1f;
                if (UseDistanceFalloff)
                {
                    float t = 1f - distance / Radius;
                    weight = t * t * (3f - 2f * t);
                }
                if (ErosionSideStrength < 1f)
                {
                    Vector3 neighborGradient = Vector3.zero;
                    for (int gradientAxis = 0; gradientAxis < 3; gradientAxis++)
                    {
                        int gradientStride = gradientAxis == 0 ? DensitySampleCount.y * DensitySampleCount.z :
                            (gradientAxis == 1 ? DensitySampleCount.z : 1);
                        int limit = MaxTerrainIndex[gradientAxis];
                        if (!hasOwnGradient)
                        {
                            int lower = Mathf.Max(index[gradientAxis] - 1, 0);
                            int upper = Mathf.Min(index[gradientAxis] + 1, limit);
                            ownGradient[gradientAxis] =
                                (Densities[densityIndex + (upper - index[gradientAxis]) * gradientStride] -
                                 Densities[densityIndex + (lower - index[gradientAxis]) * gradientStride]) /
                                ((upper - lower) * Resolution);
                        }

                        int neighborLower = Mathf.Max(neighbor[gradientAxis] - 1, 0);
                        int neighborUpper = Mathf.Min(neighbor[gradientAxis] + 1, limit);
                        neighborGradient[gradientAxis] =
                            (Densities[neighborDensityIndex + (neighborUpper - neighbor[gradientAxis]) * gradientStride] -
                             Densities[neighborDensityIndex + (neighborLower - neighbor[gradientAxis]) * gradientStride]) /
                            ((neighborUpper - neighborLower) * Resolution);
                    }
                    hasOwnGradient = true;

                    Vector3 startGradient = direction > 0 ? ownGradient : neighborGradient;
                    Vector3 endGradient = direction > 0 ? neighborGradient : ownGradient;
                    Vector3 gradient = Vector3.Lerp(startGradient, endGradient, edgeT);
                    Vector3 inwardNormal = DensityNormalToWorld.MultiplyVector(gradient).normalized;
                    float alignment = Mathf.Clamp01(Vector3.Dot(inwardNormal, WorldErosionDirection));
                    weight *= ErosionSideStrength + (1f - ErosionSideStrength) * alignment * alignment;
                }
                maxWeight = Mathf.Max(maxWeight, weight);
            }
        }

        // 각 작업은 자신의 vertex만 써서 공유 endpoint의 동시 쓰기를 피한다.
        Weights[flatIndex] = maxWeight;
    }
}
