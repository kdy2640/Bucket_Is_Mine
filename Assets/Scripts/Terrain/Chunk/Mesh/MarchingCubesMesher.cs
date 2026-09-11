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
                    MarchCube(builder, new Vector3Int(x, y, z), cubeCorners);
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

    private void MarchCube(MeshBuilder builder, Vector3Int cubeIndex, float[] cubeCorners)
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

            Vector3 vertex0 = GetEdgeVertex(
                cubeIndex, cubeCorners, MarchingTable.Triangles[configIndex, edgeIndex], out Vector3 normal0);
            Vector3 vertex1 = GetEdgeVertex(
                cubeIndex, cubeCorners, MarchingTable.Triangles[configIndex, edgeIndex + 1], out Vector3 normal1);
            Vector3 vertex2 = GetEdgeVertex(
                cubeIndex, cubeCorners, MarchingTable.Triangles[configIndex, edgeIndex + 2], out Vector3 normal2);

            builder.AddTriangle(vertex0, vertex1, vertex2, normal0, normal1, normal2);
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

    private Vector3 GetEdgeVertex(
        Vector3Int cubeIndex, float[] cubeCorners, int edgeIndex, out Vector3 normal)
    {
        int startCornerIndex = EdgeCornerIndexes[edgeIndex, 0];
        int endCornerIndex = EdgeCornerIndexes[edgeIndex, 1];

        Vector3 position = (Vector3)cubeIndex * resolution;
        Vector3 edgeStart = position + (Vector3)MarchingTable.Corners[startCornerIndex] * resolution;
        Vector3 edgeEnd = position + (Vector3)MarchingTable.Corners[endCornerIndex] * resolution;

        float startDensity = cubeCorners[startCornerIndex];
        float endDensity = cubeCorners[endCornerIndex];
        float densityDelta = endDensity - startDensity;
        bool useMidpoint = Mathf.Abs(densityDelta) < Mathf.Epsilon;
        float t = useMidpoint ? 0.5f : Mathf.Clamp01((threshold - startDensity) / densityDelta);

        normal = Vector3.zero;
        if (isSmoothShading)
        {
            Vector3 startGradient = GetDensityGradient(cubeIndex + MarchingTable.Corners[startCornerIndex]);
            Vector3 endGradient = GetDensityGradient(cubeIndex + MarchingTable.Corners[endCornerIndex]);
            // Higher density is inside the terrain, so the outward normal opposes the gradient.
            normal = -Vector3.Lerp(startGradient, endGradient, t).normalized;
        }

        return useMidpoint ? (edgeStart + edgeEnd) * 0.5f : Vector3.Lerp(edgeStart, edgeEnd, t);
    }

    private Vector3 GetDensityGradient(Vector3Int index)
    {
        int minX = Mathf.Max(index.x - 1, 0);
        int maxX = Mathf.Min(index.x + 1, width);
        int minY = Mathf.Max(index.y - 1, 0);
        int maxY = Mathf.Min(index.y + 1, densityFieldHeight);
        int minZ = Mathf.Max(index.z - 1, 0);
        int maxZ = Mathf.Min(index.z + 1, width);

        // At the field boundary the sample span is one cell, giving a one-sided difference.
        return new Vector3(
            (densities[maxX, index.y, index.z] - densities[minX, index.y, index.z]) /
                ((maxX - minX) * resolution),
            (densities[index.x, maxY, index.z] - densities[index.x, minY, index.z]) /
                ((maxY - minY) * resolution),
            (densities[index.x, index.y, maxZ] - densities[index.x, index.y, minZ]) /
                ((maxZ - minZ) * resolution));
    }
}
