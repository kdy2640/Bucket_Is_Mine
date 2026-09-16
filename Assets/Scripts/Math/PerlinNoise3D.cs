
using UnityEngine; 

public class PerlinNoise3D
{
    private int seed = 0;
    public int Seed { get { return seed; } }

    private const int X_HASH = 73856093;
    private const int Y_HASH = 19349663;
    private const int Z_HASH = 83492791;

    private const int MAX = 100;

    public PerlinNoise3D(int seed)
    {
        this.seed = seed;
    }

    public PerlinNoise3D()
    {
        seed = Random.Range(0, MAX);
    }

    public float GetRandomValue(float x, float y, float z)
    {
        return GetRandomValue(x, y, z, seed);
    }
    public float GetRandomValue(Vector3 position)
    {
        return GetRandomValue(position.x, position.y, position.z, seed);
    }

    // 공유 임시 배열 없이 같은 시드와 좌표의 노이즈를 계산한다.
    public static float GetRandomValue(float x, float y, float z, int seed)
    {
        Vector3 position = new Vector3(x, y, z);
        int _x = Mathf.FloorToInt(position.x);
        int _y = Mathf.FloorToInt(position.y);
        int _z = Mathf.FloorToInt(position.z);

        float dX = position.x - _x;
        float dY = position.y - _y;
        float dZ = position.z - _z;

        float dFX = Fade(dX);
        float dFY = Fade(dY);
        float dFZ = Fade(dZ);

        float g0 = Vector3.Dot(position - new Vector3Int(_x, _y, _z), GetRandomVec3(_x, _y, _z, seed));
        float g1 = Vector3.Dot(position - new Vector3Int(_x + 1, _y, _z), GetRandomVec3(_x + 1, _y, _z, seed));
        float g2 = Vector3.Dot(position - new Vector3Int(_x, _y + 1, _z), GetRandomVec3(_x, _y + 1, _z, seed));
        float g3 = Vector3.Dot(position - new Vector3Int(_x + 1, _y + 1, _z), GetRandomVec3(_x + 1, _y + 1, _z, seed));
        float g4 = Vector3.Dot(position - new Vector3Int(_x, _y, _z + 1), GetRandomVec3(_x, _y, _z + 1, seed));
        float g5 = Vector3.Dot(position - new Vector3Int(_x + 1, _y, _z + 1), GetRandomVec3(_x + 1, _y, _z + 1, seed));
        float g6 = Vector3.Dot(position - new Vector3Int(_x, _y + 1, _z + 1), GetRandomVec3(_x, _y + 1, _z + 1, seed));
        float g7 = Vector3.Dot(position - new Vector3Int(_x + 1, _y + 1, _z + 1), GetRandomVec3(_x + 1, _y + 1, _z + 1, seed));

        //tri-linear interpolation
        float x1 = (1 - dFX) * g0 + dFX * g1;
        float x2 = (1 - dFX) * g2 + dFX * g3;
        float y1 = (1 - dFY) * x1 + dFY * x2;

        float x3 = (1 - dFX) * g4 + dFX * g5;
        float x4 = (1 - dFX) * g6 + dFX * g7;
        float y2 = (1 - dFY) * x3 + dFY * x4;

        float z1 = (1 - dFZ) * y1 + dFZ * y2;

        float normalized = Mathf.Clamp01(z1 * 0.5f + 0.5f);
        return normalized;
    }

    private const float INV_SQRT2 = 0.70710678f;

    private static readonly Vector3[] Gradients =
    {
    new Vector3( 1,  1,  0) * INV_SQRT2,
    new Vector3(-1,  1,  0) * INV_SQRT2,
    new Vector3( 1, -1,  0) * INV_SQRT2,
    new Vector3(-1, -1,  0) * INV_SQRT2,

    new Vector3( 1,  0,  1) * INV_SQRT2,
    new Vector3(-1,  0,  1) * INV_SQRT2,
    new Vector3( 1,  0, -1) * INV_SQRT2,
    new Vector3(-1,  0, -1) * INV_SQRT2,

    new Vector3( 0,  1,  1) * INV_SQRT2,
    new Vector3( 0, -1,  1) * INV_SQRT2,
    new Vector3( 0,  1, -1) * INV_SQRT2,
    new Vector3( 0, -1, -1) * INV_SQRT2,
};

    private static Vector3 GetRandomVec3(int x, int y, int z, int seed)
    {
        unchecked
        {
            uint hash = (uint)seed;

            hash ^= (uint)x * 0x9E3779B9u;
            hash ^= (uint)y * 0x85EBCA6Bu;
            hash ^= (uint)z * 0xC2B2AE35u;

            hash ^= hash >> 16;
            hash *= 0x7FEB352Du;
            hash ^= hash >> 15;
            hash *= 0x846CA68Bu;
            hash ^= hash >> 16;

            return Gradients[(int)(hash % Gradients.Length)];
        }
    }

    private static float Fade(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }
}
