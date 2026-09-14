using UnityEngine;

public class TerrainGenerator
{
    private const int NoiseSeed = 15;

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
