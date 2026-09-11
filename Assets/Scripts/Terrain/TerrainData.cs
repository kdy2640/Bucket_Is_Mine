using UnityEngine;

public class TerrainData
{
    public float[,,] Densities { get; private set; }
    public int Width { get; }
    public int DensityFieldHeight { get; }
    public float Resolution { get; }

    public TerrainData(int width, int densityFieldHeight, float resolution)
    {
        Width = Mathf.Max(1, width);
        DensityFieldHeight = Mathf.Max(1, densityFieldHeight);
        Resolution = Mathf.Max(0.001f, resolution);
        ResetDensities();
    }

    public void ResetDensities()
    {
        Densities = new float[Width + 1, DensityFieldHeight + 1, Width + 1];
    }

    public float GetDensity(Vector3Int index)
    {
        return IsValidIndex(index) ? Densities[index.x, index.y, index.z] : 0f;
    }

    public void SetDensity(Vector3Int index, float density)
    {
        if (IsValidIndex(index))
        {
            Densities[index.x, index.y, index.z] = Mathf.Clamp01(density);
        }
    }

    public Vector3Int PositionToIndex(Vector3 localPosition)
    {
        return new Vector3Int(
            Mathf.RoundToInt(localPosition.x / Resolution),
            Mathf.RoundToInt(localPosition.y / Resolution),
            Mathf.RoundToInt(localPosition.z / Resolution));
    }

    public Vector3 IndexToPosition(Vector3Int index)
    {
        return new Vector3(index.x, index.y, index.z) * Resolution;
    }

    public bool ModifyDensitySphere(
        Vector3 localPosition,
        float radius,
        float power,
        out Vector3Int minChangedIndex,
        out Vector3Int maxChangedIndex)
    {
        minChangedIndex = new Vector3Int(Width, DensityFieldHeight, Width);
        maxChangedIndex = Vector3Int.zero;

        if (radius <= 0f || Mathf.Approximately(power, 0f))
        {
            return false;
        }

        Vector3Int center = PositionToIndex(localPosition);
        int indexRadius = Mathf.CeilToInt(radius / Resolution);
        bool changed = false;

        for (int x = -indexRadius; x <= indexRadius; x++)
        {
            for (int y = -indexRadius; y <= indexRadius; y++)
            {
                for (int z = -indexRadius; z <= indexRadius; z++)
                {
                    Vector3Int index = center + new Vector3Int(x, y, z);
                    if (!IsValidIndex(index))
                    {
                        continue;
                    }

                    float distance = Vector3.Distance(IndexToPosition(index), localPosition);
                    if (distance > radius)
                    {
                        continue;
                    }

                    float t = 1f - distance / radius;
                    float falloff = t * t * (3f - 2f * t);
                    SetDensity(index, GetDensity(index) + power * falloff);
                    minChangedIndex = Vector3Int.Min(minChangedIndex, index);
                    maxChangedIndex = Vector3Int.Max(maxChangedIndex, index);
                    changed = true;
                }
            }
        }

        return changed;
    }

    public bool IsValidIndex(Vector3Int index)
    {
        return index.x >= 0 && index.x <= Width &&
               index.y >= 0 && index.y <= DensityFieldHeight &&
               index.z >= 0 && index.z <= Width;
    }
}
