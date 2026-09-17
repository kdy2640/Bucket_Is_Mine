using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;

// 청크별 밀도 배열을 소유하고 격자 좌표 변환과 밀도 수정을 담당한다.
public class TerrainData : IDisposable
{
    public const byte ArtificialTypeId = 0;

    // 밀도 수정 단계별 성능 측정
    private static readonly ProfilerMarker ModifyMarker = new ProfilerMarker("TerrainDensity.Modify");
    private static readonly ProfilerMarker ScheduleMarker = new ProfilerMarker("TerrainDensity.Schedule");
    private static readonly ProfilerMarker CompleteMarker = new ProfilerMarker("TerrainDensity.Complete");
    // 이 데이터가 소유하는 청크별 밀도 배열
    private readonly Dictionary<Vector3Int, ChunkDensityData> chunks =
        new Dictionary<Vector3Int, ChunkDensityData>();

    // 전체 격자 크기와 청크 분할 정보
    public int Width { get; }
    public int DensityFieldHeight { get; }
    public float Resolution { get; }
    public int ChunkSize { get; }
    public Vector3Int ChunkCounts { get; }
    internal NativeArray<TerrainLayer> Layers { get; }
    internal Color ArtificialColor { get; }

    // 지형 크기에 맞춰 청크를 나누고 각 청크의 밀도 배열을 할당한다.
    public TerrainData(TerrainGridGeometry grid, TerrainLayer[] sourceLayers, Color artificialColor)
    {
        // SO를 수정하지 않고 생성 시점의 설정과 렌더링 색 공간을 네이티브 데이터에 복사한다.
        NativeArray<TerrainLayer> layers = new NativeArray<TerrainLayer>(sourceLayers.Length, Allocator.Persistent);
        bool linearColorSpace = QualitySettings.activeColorSpace == ColorSpace.Linear;
        for (int i = 0; i < layers.Length; i++)
        {
            TerrainLayer layer = sourceLayers[i];
            if (linearColorSpace) layer.Color = layer.Color.linear;
            layers[i] = layer;
        }
        Layers = layers;
        ArtificialColor = linearColorSpace ? artificialColor.linear : artificialColor;

        Width = grid.Width;
        DensityFieldHeight = grid.DensityFieldHeight;
        Resolution = grid.Resolution;
        ChunkSize = grid.ChunkSize;
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
                    chunks.Add(chunkCoord, new ChunkDensityData(origin, cubeCount, sampleCount));
                }
            }
        }

        grid.Initialize(this);
    }

    // 모든 청크의 밀도를 0으로 초기화한다.
    public void ResetDensities()
    {
        foreach (ChunkDensityData chunk in chunks.Values)
        {
            var densities = chunk.Densities;
            for (int i = 0; i < densities.Length; i++)
            {
                densities[i] = 0f;
            }
        }
    }

    // 좌표에 해당하는 청크 데이터를 반환한다.
    // 반환된 배열은 빌려 쓰는 참조이며 해제는 TerrainData가 담당한다.
    public ChunkDensityData GetChunkData(Vector3Int chunkCoord)
    {
        return chunks[chunkCoord];
    }

    // 소유한 모든 청크의 네이티브 밀도 배열을 해제한다.
    public void Dispose()
    {
        foreach (ChunkDensityData chunk in chunks.Values)
        {
            chunk.Densities.Dispose();
            chunk.TypeIds.Dispose();
        }

        chunks.Clear();
        Layers.Dispose();
    }

    // 유효한 격자 좌표의 종류를 읽는다. 고체 여부는 밀도로 판정한다.
    public byte GetTerrainType(Vector3Int index)
    {
        Vector3Int chunkCoord = new Vector3Int(
            Mathf.Min(index.x / ChunkSize, ChunkCounts.x - 1),
            Mathf.Min(index.y / ChunkSize, ChunkCounts.y - 1),
            Mathf.Min(index.z / ChunkSize, ChunkCounts.z - 1));
        ChunkDensityData chunk = chunks[chunkCoord];
        Vector3Int localIndex = index - chunk.Origin;
        int flatIndex = (localIndex.x * chunk.SampleCount.y + localIndex.y) * chunk.SampleCount.z + localIndex.z;
        return chunk.TypeIds[flatIndex];
    }

    // 전체 격자 좌표에 해당하는 밀도를 읽고 범위 밖이면 0을 반환한다.
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
        ChunkDensityData chunk = chunks[chunkCoord];
        return chunk.GetDensity(index - chunk.Origin);
    }

    // 유효한 격자 좌표의 밀도를 0~1 범위로 제한해 저장한다.
    public void SetDensity(Vector3Int index, float density)
    {
        if (IsValidIndex(index))
        {
            Vector3Int chunkCoord = new Vector3Int(
                Mathf.Min(index.x / ChunkSize, ChunkCounts.x - 1),
                Mathf.Min(index.y / ChunkSize, ChunkCounts.y - 1),
                Mathf.Min(index.z / ChunkSize, ChunkCounts.z - 1));
            ChunkDensityData chunk = chunks[chunkCoord];
            chunk.SetDensity(index - chunk.Origin, Mathf.Clamp01(density));
        }
    }

    // 지형 로컬 위치를 가장 가까운 밀도 샘플의 격자 좌표로 변환한다.
    public Vector3Int PositionToIndex(Vector3 localPosition)
    {
        return new Vector3Int(
            Mathf.RoundToInt(localPosition.x / Resolution),
            Mathf.RoundToInt(localPosition.y / Resolution),
            Mathf.RoundToInt(localPosition.z / Resolution));
    }

    // 밀도 샘플의 격자 좌표를 지형 로컬 위치로 변환한다.
    public Vector3 IndexToPosition(Vector3Int index)
    {
        return new Vector3(index.x, index.y, index.z) * Resolution;
    }

    // 구 영역의 밀도 수정을 청크별 Job으로 실행하고 수정 영역의 최소·최대 좌표를 반환한다.
    public bool ModifyDensitySphere(
        Vector3 localPosition,
        float radius,
        float power,
        float densityThreshold,
        Vector3 worldErosionDirection,
        Matrix4x4 densityNormalToWorld,
        float erosionSideStrength,
        bool useErosionDistanceFalloff,
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
        // 표면 교차점은 구 안에 있어도 edge의 endpoint는 한 칸 밖에 있을 수 있다.
        if (power < 0f)
        {
            indexRadius++;
        }
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
        Vector3Int erosionSampleCount = maxIndex - minIndex + Vector3Int.one;
        NativeArray<float> erosionWeights = new NativeArray<float>(
            power < 0f ? erosionSampleCount.x * erosionSampleCount.y * erosionSampleCount.z : 0,
            Allocator.TempJob);

        NativeArray<float> densitySnapshot = default;
        CalculateErosionWeightsJob weightsJob = default;
        if (power < 0f)
        {
            // endpoint 범위에 노멀 계산용 이웃 한 칸을 더해 시작 밀도를 복사한다.
            Vector3Int snapshotMin = Vector3Int.Max(minIndex - Vector3Int.one, Vector3Int.zero);
            Vector3Int snapshotMax = Vector3Int.Min(maxIndex + Vector3Int.one,
                new Vector3Int(Width, DensityFieldHeight, Width));
            Vector3Int snapshotCount = snapshotMax - snapshotMin + Vector3Int.one;
            densitySnapshot = new NativeArray<float>(snapshotCount.x * snapshotCount.y * snapshotCount.z,
                Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            Vector3Int snapshotMinChunk = new Vector3Int(
                Mathf.Min(snapshotMin.x / ChunkSize, ChunkCounts.x - 1),
                Mathf.Min(snapshotMin.y / ChunkSize, ChunkCounts.y - 1),
                Mathf.Min(snapshotMin.z / ChunkSize, ChunkCounts.z - 1));
            Vector3Int snapshotMaxChunk = new Vector3Int(
                Mathf.Min(snapshotMax.x / ChunkSize, ChunkCounts.x - 1),
                Mathf.Min(snapshotMax.y / ChunkSize, ChunkCounts.y - 1),
                Mathf.Min(snapshotMax.z / ChunkSize, ChunkCounts.z - 1));

            for (int x = snapshotMinChunk.x; x <= snapshotMaxChunk.x; x++)
            {
                for (int y = snapshotMinChunk.y; y <= snapshotMaxChunk.y; y++)
                {
                    for (int z = snapshotMinChunk.z; z <= snapshotMaxChunk.z; z++)
                    {
                        ChunkDensityData chunk = chunks[new Vector3Int(x, y, z)];
                        Vector3Int copyMin = Vector3Int.Max(snapshotMin, chunk.Origin);
                        Vector3Int copyMax = Vector3Int.Min(snapshotMax,
                            chunk.Origin + chunk.SampleCount - Vector3Int.one);
                        int copyLength = copyMax.z - copyMin.z + 1;
                        // 청크의 연속된 Z 구간을 복사해 샘플마다 Dictionary를 조회하지 않는다.
                        for (int sampleX = copyMin.x; sampleX <= copyMax.x; sampleX++)
                        {
                            for (int sampleY = copyMin.y; sampleY <= copyMax.y; sampleY++)
                            {
                                int sourceIndex = ((sampleX - chunk.Origin.x) * chunk.SampleCount.y +
                                    sampleY - chunk.Origin.y) * chunk.SampleCount.z + copyMin.z - chunk.Origin.z;
                                int targetIndex = ((sampleX - snapshotMin.x) * snapshotCount.y +
                                    sampleY - snapshotMin.y) * snapshotCount.z + copyMin.z - snapshotMin.z;
                                NativeArray<float>.Copy(chunk.Densities, sourceIndex, densitySnapshot, targetIndex, copyLength);
                            }
                        }
                    }
                }
            }

            weightsJob = new CalculateErosionWeightsJob
            {
                Densities = densitySnapshot,
                DensityOrigin = snapshotMin,
                DensitySampleCount = snapshotCount,
                MaxTerrainIndex = new Vector3Int(Width, DensityFieldHeight, Width),
                WeightOrigin = minIndex,
                WeightSampleCount = erosionSampleCount,
                LocalPosition = localPosition,
                Resolution = Resolution,
                Radius = radius,
                DensityThreshold = densityThreshold,
                WorldErosionDirection = worldErosionDirection,
                DensityNormalToWorld = densityNormalToWorld,
                ErosionSideStrength = erosionSideStrength,
                UseDistanceFalloff = useErosionDistanceFalloff,
                Weights = erosionWeights
            };
        }

        NativeArray<JobHandle> handles = new NativeArray<JobHandle>(jobCount, Allocator.Temp);
        NativeArray<Vector3Int>[] changedBounds = new NativeArray<Vector3Int>[jobCount];

        using (ScheduleMarker.Auto())
        {
            JobHandle weightsHandle = power < 0f ? weightsJob.Schedule(erosionWeights.Length, 64) : default;
            int i = 0;
            for (int x = minChunk.x; x <= maxChunk.x; x++)
            {
                for (int y = minChunk.y; y <= maxChunk.y; y++)
                {
                    for (int z = minChunk.z; z <= maxChunk.z; z++)
                    {
                        ChunkDensityData chunk = chunks[new Vector3Int(x, y, z)];
                        // Each Job owns a separate density array and result buffer.
                        changedBounds[i] = new NativeArray<Vector3Int>(2, Allocator.TempJob);
                        ModifyDensitySphereJob job = new ModifyDensitySphereJob
                        {
                            Densities = chunk.Densities,
                            TypeIds = chunk.TypeIds,
                            DensityThreshold = densityThreshold,
                            Origin = chunk.Origin,
                            SampleCount = chunk.SampleCount,
                            MinIndex = Vector3Int.Max(minIndex, chunk.Origin),
                            MaxIndex = Vector3Int.Min(maxIndex, chunk.Origin + chunk.SampleCount - Vector3Int.one),
                            LocalPosition = localPosition,
                            Resolution = Resolution,
                            Radius = radius,
                            Power = power,
                            ErosionWeights = erosionWeights,
                            ErosionOrigin = minIndex,
                            ErosionSampleCount = erosionSampleCount,
                            ChangedBounds = changedBounds[i]
                        };
                        handles[i] = job.Schedule(weightsHandle);
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
        erosionWeights.Dispose();
        if (power < 0f)
        {
            densitySnapshot.Dispose();
        }

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

    // 좌표가 지형 끝점 샘플을 포함한 밀도 격자 범위 안인지 확인한다.
    public bool IsValidIndex(Vector3Int index)
    {
        return index.x >= 0 && index.x <= Width &&
               index.y >= 0 && index.y <= DensityFieldHeight &&
               index.z >= 0 && index.z <= Width;
    }
}
