using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;

// 고정 시드의 3D 노이즈 또는 2D 높이 노이즈로 지형의 초기 밀도를 채운다.
public class TerrainDensityFormer
{
    // 초기 밀도 생성 전체와 준비·예약·완료 대기 시간을 구분한다.
    private static readonly ProfilerMarker FormMarker = new ProfilerMarker("TerrainDensity.Form");
    private static readonly ProfilerMarker PrepareMarker = new ProfilerMarker("TerrainDensity.Form.Prepare");
    private static readonly ProfilerMarker ScheduleMarker = new ProfilerMarker("TerrainDensity.Form.Schedule");
    private static readonly ProfilerMarker CompleteMarker = new ProfilerMarker("TerrainDensity.Form.Complete");

    // 동일한 입력에서 지형을 재현하기 위한 고정 시드
    private const int NoiseSeed = 15;

    // 청크별 밀도 채우기를 예약하고 모든 Job이 완료된 뒤 반환한다.
    public void Generate(
        TerrainData data,
        float baseSurfaceHeight,
        float terrainAmplitude,
        float noiseScale,
        float densityThreshold,
        bool use3DNoise)
    {
        using var formScope = FormMarker.Auto();
        int surfaceWidth = data.Width + 1;
        NativeArray<float> surfaceHeights;
        Vector3Int counts = data.ChunkCounts;
        NativeArray<JobHandle> handles;
        using (PrepareMarker.Auto())
        {
            surfaceHeights = new NativeArray<float>(
                use3DNoise ? 0 : surfaceWidth * surfaceWidth,
                Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            handles = new NativeArray<JobHandle>(
                counts.x * counts.y * counts.z, Allocator.Temp);
            if (!use3DNoise)
            {
                for (int x = 0; x < surfaceWidth; x++)
                {
                    for (int z = 0; z < surfaceWidth; z++)
                    {
                        float heightNoise = terrainAmplitude == 0f
                            ? 0f
                            : Mathf.PerlinNoise(x * noiseScale, z * noiseScale) * 2f - 1f;
                        surfaceHeights[x * surfaceWidth + z] = baseSurfaceHeight + heightNoise * terrainAmplitude;
                    }
                }
            }
        }

        using (ScheduleMarker.Auto())
        {
            int jobIndex = 0;
            for (int x = 0; x < counts.x; x++)
            {
                for (int y = 0; y < counts.y; y++)
                {
                    for (int z = 0; z < counts.z; z++)
                    {
                        ChunkDensityData chunk = data.GetChunkData(new Vector3Int(x, y, z));
                        FormTerrainDensityJob job = new FormTerrainDensityJob
                        {
                            Densities = chunk.Densities,
                            TypeIds = chunk.TypeIds,
                            Layers = data.Layers,
                            Resolution = data.Resolution,
                            Origin = chunk.Origin,
                            SampleCount = chunk.SampleCount,
                            SurfaceHeights = surfaceHeights,
                            SurfaceWidth = surfaceWidth,
                            NoiseSeed = NoiseSeed,
                            NoiseScale = noiseScale,
                            DensityThreshold = densityThreshold,
                            Use3DNoise = use3DNoise
                        };
                        handles[jobIndex++] = job.Schedule(chunk.Densities.Length, 64);
                    }
                }
            }
        }

        using (CompleteMarker.Auto())
        {
            JobHandle.CompleteAll(handles);
        }
        handles.Dispose();
        surfaceHeights.Dispose();
    }
}
