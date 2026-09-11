using UnityEngine;

public class MarchingCubesMesher
{
    private readonly float[,,] densities;
    private readonly int width;
    private readonly int densityFieldHeight;
    private readonly float resolution;
    private readonly float threshold;
    private readonly bool isSmoothShading;

    private static readonly int[,] EdgeCornerIndexes = new int[12, 2]
    {
        { 0, 1 },
        { 1, 2 },
        { 3, 2 },
        { 0, 3 },
        { 4, 5 },
        { 5, 6 },
        { 7, 6 },
        { 4, 7 },
        { 0, 4 },
        { 1, 5 },
        { 2, 6 },
        { 3, 7 },
    };

    public MarchingCubesMesher(
        float[,,] densities,
        int width,
        int densityFieldHeight,
        float resolution,
        float threshold,
        bool isSmoothShading)
    {
        this.densities = densities;
        this.width = width;
        this.densityFieldHeight = densityFieldHeight;
        this.resolution = resolution;
        this.threshold = threshold;
        this.isSmoothShading = isSmoothShading;
    }

    public Mesh BuildChunkMesh(Vector3Int chunkCoord, int chunkSize)
    {
        MeshBuilder builder = new MeshBuilder(isSmoothShading);

        int startX = chunkCoord.x * chunkSize;
        int startY = chunkCoord.y * chunkSize;
        int startZ = chunkCoord.z * chunkSize;
        int endX = Mathf.Min(startX + chunkSize, width);
        int endY = Mathf.Min(startY + chunkSize, densityFieldHeight);
        int endZ = Mathf.Min(startZ + chunkSize, width);

        for (int x = startX; x < endX; x++)
        {
            for (int y = startY; y < endY; y++)
            {
                for (int z = startZ; z < endZ; z++)
                {
                    float[] cubeCorners = GetCubeCorners(x, y, z);
                    MarchCube(builder, new Vector3(x, y, z) * resolution, cubeCorners);
                }
            }
        }

        return builder.ToMesh();
    }

    private float[] GetCubeCorners(int x, int y, int z)
    {
        float[] cubeCorners = new float[8];

        for (int i = 0; i < 8; i++)
        {
            Vector3Int corner = new Vector3Int(x, y, z) + MarchingTable.Corners[i];
            cubeCorners[i] = densities[corner.x, corner.y, corner.z];
        }

        return cubeCorners;
    }

    private void MarchCube(MeshBuilder builder, Vector3 position, float[] cubeCorners)
    {
        int configIndex = GetConfigIndex(cubeCorners);

        if (configIndex == 0 || configIndex == 255)
        {
            return;
        }

        for (int edgeIndex = 0; edgeIndex < 15; edgeIndex += 3)
        {
            if (MarchingTable.Triangles[configIndex, edgeIndex] == -1)
            {
                return;
            }

            Vector3 vertex0 = GetEdgeVertex(position, cubeCorners, MarchingTable.Triangles[configIndex, edgeIndex]);
            Vector3 vertex1 = GetEdgeVertex(position, cubeCorners, MarchingTable.Triangles[configIndex, edgeIndex + 1]);
            Vector3 vertex2 = GetEdgeVertex(position, cubeCorners, MarchingTable.Triangles[configIndex, edgeIndex + 2]);

            builder.AddTriangle(vertex0, vertex1, vertex2);
        }
    }

    private int GetConfigIndex(float[] cubeCorners)
    {
        int configIndex = 0;

        for (int i = 0; i < 8; i++)
        {
            if (cubeCorners[i] > threshold)
            {
                configIndex |= 1 << i;
            }
        }

        return configIndex;
    }

    private Vector3 GetEdgeVertex(Vector3 position, float[] cubeCorners, int edgeIndex)
    {
        int startCornerIndex = EdgeCornerIndexes[edgeIndex, 0];
        int endCornerIndex = EdgeCornerIndexes[edgeIndex, 1];

        Vector3 edgeStart = position + (Vector3)MarchingTable.Corners[startCornerIndex] * resolution;
        Vector3 edgeEnd = position + (Vector3)MarchingTable.Corners[endCornerIndex] * resolution;

        float startDensity = cubeCorners[startCornerIndex];
        float endDensity = cubeCorners[endCornerIndex];
        float densityDelta = endDensity - startDensity;

        if (Mathf.Abs(densityDelta) < Mathf.Epsilon)
        {
            return (edgeStart + edgeEnd) * 0.5f;
        }

        float t = Mathf.Clamp01((threshold - startDensity) / densityDelta);
        return Vector3.Lerp(edgeStart, edgeEnd, t);
    }
}
