using Unity.Collections;
using UnityEngine;

// 밀도 격자에서 마칭 큐브 방식으로 청크 표면의 삼각형과 노멀을 추출한다.
internal struct MarchingCubesMesher
{
    // 밀도 입력과 표면 추출 설정
    private readonly ChunkMeshInput input;
    private readonly int width;
    private readonly int densityFieldHeight;
    private readonly float resolution;
    private readonly float threshold;
    private readonly bool isSmoothShading;

    // 마칭 큐브 꼭짓점·모서리·삼각형 조회 테이블
    [ReadOnly] private NativeArray<Vector3Int> corners;
    [ReadOnly] private NativeArray<int> edgeCornerIndexes;
    [ReadOnly] private NativeArray<int> triangleTable;

    // 메시 입력과 조회 테이블, 표면 임계값 및 셰이딩 방식을 저장한다.
    public MarchingCubesMesher(
        ChunkMeshInput input,
        NativeArray<Vector3Int> corners,
        NativeArray<int> edgeCornerIndexes,
        NativeArray<int> triangleTable,
        float threshold,
        bool isSmoothShading)
    {
        this.input = input;
        this.corners = corners;
        this.edgeCornerIndexes = edgeCornerIndexes;
        this.triangleTable = triangleTable;
        width = input.Width;
        densityFieldHeight = input.Height;
        resolution = input.Resolution;
        this.threshold = threshold;
        this.isSmoothShading = isSmoothShading;
    }

    // 청크에 속한 모든 큐브를 순회하며 표면 삼각형을 빌더에 추가한다.
    public void Build(ref MeshBuilder builder)
    {
        int startX = input.Origin.x;
        int startY = input.Origin.y;
        int startZ = input.Origin.z;
        int endX = startX + input.CubeCount.x;
        int endY = startY + input.CubeCount.y;
        int endZ = startZ + input.CubeCount.z;

        for (int x = startX; x < endX; x++)
        {
            for (int y = startY; y < endY; y++)
            {
                for (int z = startZ; z < endZ; z++)
                {
                    FixedList64Bytes<float> cubeCorners = GetCubeCorners(x, y, z);
                    MarchCube(ref builder, new Vector3Int(x, y, z), cubeCorners);
                }
            }
        }
    }

    // 큐브의 여덟 꼭짓점에서 밀도 값을 읽어 반환한다.
    private FixedList64Bytes<float> GetCubeCorners(int x, int y, int z)
    {
        FixedList64Bytes<float> cubeCorners = default;

        for (int i = 0; i < 8; i++)
        {
            Vector3Int corner = new Vector3Int(x, y, z) + corners[i];
            cubeCorners.Add(input.GetDensity(corner));
        }

        return cubeCorners;
    }

    // 꼭짓점 밀도에 맞는 테이블 항목을 찾아 한 큐브의 표면 삼각형을 생성한다.
    private void MarchCube(ref MeshBuilder builder, Vector3Int cubeIndex, FixedList64Bytes<float> cubeCorners)
    {
        int configIndex = GetConfigIndex(cubeCorners);

        if (configIndex == 0 || configIndex == 255)
        {
            return;
        }

        for (int edgeIndex = 0; edgeIndex < 15; edgeIndex += 3)
        {
            if (triangleTable[configIndex * 16 + edgeIndex] == -1)
            {
                return;
            }

            Vector3 vertex0 = GetEdgeVertex(
                cubeIndex, cubeCorners, triangleTable[configIndex * 16 + edgeIndex], out Vector3 normal0);
            Vector3 vertex1 = GetEdgeVertex(
                cubeIndex, cubeCorners, triangleTable[configIndex * 16 + edgeIndex + 1], out Vector3 normal1);
            Vector3 vertex2 = GetEdgeVertex(
                cubeIndex, cubeCorners, triangleTable[configIndex * 16 + edgeIndex + 2], out Vector3 normal2);

            builder.AddTriangle(vertex0, vertex1, vertex2, normal0, normal1, normal2);
        }
    }

    // 표면 임계값보다 높은 꼭짓점을 비트로 표시해 조회 테이블의 인덱스를 만든다.
    private int GetConfigIndex(FixedList64Bytes<float> cubeCorners)
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

    // 밀도 임계값을 지나는 모서리 위치를 보간하고 부드러운 셰이딩용 노멀을 계산한다.
    private Vector3 GetEdgeVertex(
        Vector3Int cubeIndex, FixedList64Bytes<float> cubeCorners, int edgeIndex, out Vector3 normal)
    {
        int startCornerIndex = edgeCornerIndexes[edgeIndex * 2];
        int endCornerIndex = edgeCornerIndexes[edgeIndex * 2 + 1];

        Vector3 position = (Vector3)cubeIndex * resolution;
        Vector3 edgeStart = position + (Vector3)corners[startCornerIndex] * resolution;
        Vector3 edgeEnd = position + (Vector3)corners[endCornerIndex] * resolution;

        float startDensity = cubeCorners[startCornerIndex];
        float endDensity = cubeCorners[endCornerIndex];
        float densityDelta = endDensity - startDensity;
        bool useMidpoint = Mathf.Abs(densityDelta) < Mathf.Epsilon;
        float t = useMidpoint ? 0.5f : Mathf.Clamp01((threshold - startDensity) / densityDelta);

        normal = Vector3.zero;
        if (isSmoothShading)
        {
            Vector3 startGradient = GetDensityGradient(cubeIndex + corners[startCornerIndex]);
            Vector3 endGradient = GetDensityGradient(cubeIndex + corners[endCornerIndex]);
            // Higher density is inside the terrain, so the outward normal opposes the gradient.
            normal = -Vector3.Lerp(startGradient, endGradient, t).normalized;
        }

        return useMidpoint ? (edgeStart + edgeEnd) * 0.5f : Vector3.Lerp(edgeStart, edgeEnd, t);
    }

    // 주변 샘플의 밀도 차이로 기울기를 구하며 격자 끝에서는 한쪽 방향의 차이를 사용한다.
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
            (input.GetDensity(new Vector3Int(maxX, index.y, index.z)) -
             input.GetDensity(new Vector3Int(minX, index.y, index.z))) /
                ((maxX - minX) * resolution),
            (input.GetDensity(new Vector3Int(index.x, maxY, index.z)) -
             input.GetDensity(new Vector3Int(index.x, minY, index.z))) /
                ((maxY - minY) * resolution),
            (input.GetDensity(new Vector3Int(index.x, index.y, maxZ)) -
             input.GetDensity(new Vector3Int(index.x, index.y, minZ))) /
                ((maxZ - minZ) * resolution));
    }
}
