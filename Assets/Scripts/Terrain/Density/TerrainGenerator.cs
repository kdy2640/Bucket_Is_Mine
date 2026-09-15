using UnityEngine;

// 고정 시드의 3D 노이즈 또는 2D 높이 노이즈로 지형의 초기 밀도를 채운다.
public class TerrainGenerator
{
    // 동일한 입력에서 지형을 재현하기 위한 고정 시드
    private const int NoiseSeed = 15;

    // 밀도를 초기화한 뒤 선택한 노이즈 방식과 표면 설정으로 전체 격자를 채운다.
    public void Generate(
        TerrainData data,
        float baseSurfaceHeight,
        float terrainAmplitude,
        float noiseScale,
        float densityThreshold,
        bool use3DNoise)
    {
        data.ResetDensities();
        PerlinNoise3D noise = new PerlinNoise3D(NoiseSeed);

        for (int x = 0; x <= data.Width; x++)
        {
            for (int y = 0; y <= data.DensityFieldHeight; y++)
            {
                for (int z = 0; z <= data.Width; z++)
                {
                    float density;

                    if (use3DNoise)
                    {
                        density = noise.GetRandomValue(
                            x * noiseScale,
                            y * noiseScale,
                            z * noiseScale);
                    }
                    else
                    {
                        float heightNoise = terrainAmplitude == 0f
                            ? 0f
                            : Mathf.PerlinNoise(x * noiseScale, z * noiseScale) * 2f - 1f;
                        float surfaceY = baseSurfaceHeight + heightNoise * terrainAmplitude;
                        density = Mathf.Clamp01((surfaceY - y) * 0.1f + densityThreshold);
                    }

                    data.SetDensity(new Vector3Int(x, y, z), density);
                }
            }
        }
    }
}
