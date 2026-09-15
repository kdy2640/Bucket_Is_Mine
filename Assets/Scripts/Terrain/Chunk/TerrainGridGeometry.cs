using UnityEngine;

// 지형 격자 설정을 보관하고 현재 생성된 데이터의 좌표·범위·거리를 계산한다.
[System.Serializable]
public class TerrainGridGeometry
{
    #region 격자 설정 및 생성된 데이터

    [Header("밀도 격자 크기")]
    [SerializeField, Min(1)] private int width = 30;
    [SerializeField, Min(1)] private int densityFieldHeight = 20;

    [Header("밀도 샘플 간격")]
    [SerializeField, Min(0.001f)] private float resolution = 1f;

    [Header("청크 크기")]
    [SerializeField, Min(1)] private int chunkSize = 16;

    // 계산에 사용할 현재 밀도 데이터 참조. 생성과 해제는 TerrainManager가 맡는다.
    private TerrainData data;

    // 다음 지형 생성에 적용할 설정값
    public int Width => Mathf.Max(1, width);
    public int DensityFieldHeight => Mathf.Max(1, densityFieldHeight);
    public float Resolution => Mathf.Max(0.001f, resolution);
    public int ChunkSize => Mathf.Max(1, chunkSize);

    // 현재 생성된 지형을 기준으로 하는 청크 분할 정보
    public Vector3Int ChunkCounts => data.ChunkCounts;
    public float ChunkLocalSize => data.ChunkSize * data.Resolution;

    // 새 밀도 데이터가 생성되면 계산 기준을 함께 교체한다.
    internal void Initialize(TerrainData data)
    {
        this.data = data;
    }

    #endregion

    #region 밀도 변경 범위 및 좌표 변환

    // 밀도 변경과 노멀 보정에 영향을 받는 청크의 최소·최대 좌표를 구한다.
    public void GetAffectedChunkBounds(
        Vector3Int minIndex, Vector3Int maxIndex, bool isSmoothShading,
        out Vector3Int minChunk, out Vector3Int maxChunk)
    {
        // 부드러운 노멀은 큐브 모서리보다 한 칸 바깥의 밀도까지 참조한다.
        int normalPadding = isSmoothShading ? 1 : 0;
        Vector3Int minCube = new Vector3Int(
            Mathf.Clamp(minIndex.x - 1 - normalPadding, 0, data.Width - 1),
            Mathf.Clamp(minIndex.y - 1 - normalPadding, 0, data.DensityFieldHeight - 1),
            Mathf.Clamp(minIndex.z - 1 - normalPadding, 0, data.Width - 1));

        Vector3Int maxCube = new Vector3Int(
            Mathf.Clamp(maxIndex.x + normalPadding, 0, data.Width - 1),
            Mathf.Clamp(maxIndex.y + normalPadding, 0, data.DensityFieldHeight - 1),
            Mathf.Clamp(maxIndex.z + normalPadding, 0, data.Width - 1));

        minChunk = CubeIndexToChunkCoord(minCube);
        maxChunk = CubeIndexToChunkCoord(maxCube);
    }

    // 전체 격자의 큐브 좌표를 해당 큐브가 속한 청크 좌표로 변환한다.
    public Vector3Int CubeIndexToChunkCoord(Vector3Int cubeIndex)
    {
        return new Vector3Int(
            cubeIndex.x / data.ChunkSize,
            cubeIndex.y / data.ChunkSize,
            cubeIndex.z / data.ChunkSize);
    }

    // 지형 내부 위치를 청크 좌표로 바꾸고 지형 밖 위치는 가장 가까운 청크로 제한한다.
    public Vector3Int LocalPositionToChunkCoord(Vector3 position)
    {
        float size = ChunkLocalSize;
        Vector3Int coordinate = new Vector3Int(
            Mathf.FloorToInt(position.x / size),
            Mathf.FloorToInt(position.y / size),
            Mathf.FloorToInt(position.z / size));
        return Vector3Int.Min(Vector3Int.Max(coordinate, Vector3Int.zero),
            ChunkCounts - Vector3Int.one);
    }

    #endregion

    #region 스트리밍 범위 및 거리

    // 지형의 월드 스케일을 반영한 축별 청크 크기를 구한다.
    public Vector3 GetChunkWorldSize(Vector3 lossyScale)
    {
        Vector3 size = lossyScale * ChunkLocalSize;
        return new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
    }

    // 거리를 청크 반경으로 바꾸고 해제 반경은 활성 반경보다 최소 한 칸 크게 잡는다.
    public void GetStreamingRadii(
        float loadDistance, float unloadDistance, Vector3 chunkWorldSize,
        out Vector3Int loadRadius, out Vector3Int unloadRadius)
    {
        loadRadius = new Vector3Int(
            Mathf.Max(1, Mathf.CeilToInt(loadDistance / chunkWorldSize.x)),
            Mathf.Max(1, Mathf.CeilToInt(loadDistance / chunkWorldSize.y)),
            Mathf.Max(1, Mathf.CeilToInt(loadDistance / chunkWorldSize.z)));
        float releaseDistance = Mathf.Max(loadDistance, unloadDistance);
        unloadRadius = new Vector3Int(
            Mathf.Max(loadRadius.x + 1, Mathf.CeilToInt(releaseDistance / chunkWorldSize.x)),
            Mathf.Max(loadRadius.y + 1, Mathf.CeilToInt(releaseDistance / chunkWorldSize.y)),
            Mathf.Max(loadRadius.z + 1, Mathf.CeilToInt(releaseDistance / chunkWorldSize.z)));
    }

    // 중심 청크와 축별 반경으로 최소·최대 좌표를 구하고 지형 범위 안으로 제한한다.
    public void GetClampedChunkBounds(
        Vector3Int center, Vector3Int radius,
        out Vector3Int min, out Vector3Int max)
    {
        min = Vector3Int.Max(center - radius, Vector3Int.zero);
        max = Vector3Int.Min(center + radius, ChunkCounts - Vector3Int.one);
    }

    // 청크가 중심으로부터 어느 한 축이라도 지정 반경을 벗어났는지 확인한다.
    public bool IsOutsideRadius(Vector3Int coordinate, Vector3Int center, Vector3Int radius)
    {
        Vector3Int offset = coordinate - center;
        return Mathf.Abs(offset.x) > radius.x ||
            Mathf.Abs(offset.y) > radius.y ||
            Mathf.Abs(offset.z) > radius.z;
    }

    // 청크 사이의 월드 거리 제곱을 구해 가까운 청크부터 정렬할 때 사용한다.
    public float GetSquaredWorldDistance(Vector3Int coordinate, Vector3Int center, Vector3 chunkWorldSize)
    {
        return Vector3.Scale(coordinate - center, chunkWorldSize).sqrMagnitude;
    }

    #endregion
}
