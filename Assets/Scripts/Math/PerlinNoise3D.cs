
using UnityEngine; 

public class PerlinNoise3D
{
    private int seed = 0;
    public int Seed { get { return seed; } }

    private const int X_HASH = 73856093;
    private const int Y_HASH = 19349663;
    private const int Z_HASH = 83492791;

    private const int MAX = 100;
    private const int CORENER_COUNT = 8;

    Vector3Int[] diffArr = new Vector3Int[CORENER_COUNT]; 
    Vector3[] VecArr = new Vector3[CORENER_COUNT];
    Vector3Int[] CubeArr = new Vector3Int[CORENER_COUNT];
    float[] GradientArr = new float[CORENER_COUNT];

    public PerlinNoise3D(int seed)
    {
        this.seed = seed;
        Init();
    }

    public PerlinNoise3D()
    {
        seed = Random.Range(0, MAX);
        Init();
    }

    private void Init()
    {
        diffArr = new Vector3Int[CORENER_COUNT];
        diffArr[0] = Vector3Int.zero;
        diffArr[1] = new Vector3Int(1, 0, 0);
        diffArr[2] = new Vector3Int(0, 1, 0);
        diffArr[3] = new Vector3Int(1, 1, 0);
        diffArr[4] = new Vector3Int(0, 0, 1);
        diffArr[5] = new Vector3Int(1, 0, 1);
        diffArr[6] = new Vector3Int(0, 1, 1);
        diffArr[7] = new Vector3Int(1, 1, 1);
         
    }
    public float GetRandomValue(float x, float y, float z)
    {
        return GetRandomValue(new Vector3(x, y, z));
    }
    public float GetRandomValue(Vector3 position)
    {
        int _x = Mathf.FloorToInt(position.x);
        int _y = Mathf.FloorToInt(position.y);
        int _z = Mathf.FloorToInt(position.z);

        float dX = position.x - _x;
        float dY = position.y - _y;
        float dZ = position.z - _z;

        float dFX = Fade(dX);
        float dFY = Fade(dY);
        float dFZ = Fade(dZ);

        Vector3Int anchor = new Vector3Int(_x, _y, _z); 


        for (int i = 0; i < CORENER_COUNT; i++)
        {
            CubeArr[i]= anchor + diffArr[i];
            VecArr[i] = GetRandomVec3(CubeArr[i].x, CubeArr[i].y, CubeArr[i].z); 
            GradientArr[i] = Vector3.Dot(position - CubeArr[i], VecArr[i]);
        }

        //tri-linear interpolation
        float x1 = (1 - dFX) * GradientArr[0] + dFX * GradientArr[1];
        float x2 = (1 - dFX) * GradientArr[2] + dFX * GradientArr[3];
        float y1 = (1 - dFY) * x1 + dFY * x2;

        float x3 = (1 - dFX) * GradientArr[4] + dFX * GradientArr[5];
        float x4 = (1 - dFX) * GradientArr[6] + dFX * GradientArr[7];
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

    private Vector3 GetRandomVec3(int x, int y, int z)
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

    private float Fade(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }
}
