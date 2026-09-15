using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// 대상 주변 청크를 거리순으로 활성화하고 멀어진 청크를 비활성화한다.
[Serializable]
public sealed class TerrainChunkStreamer
{
    [Header("청크 활성화·해제 거리")]
    [SerializeField, Min(0f)] private float loadDistance = 110f;
    [SerializeField, Min(0f)] private float unloadDistance = 120f;

    [Header("프레임당 활성화 제한")]
    [FormerlySerializedAs("maxChunkLoadsPerFrame")]
    [SerializeField, Min(1)] private int maxChunkActivationsPerFrame = 64;

    // 거리순 활성화 대기열과 좌표 작업 목록
    private readonly Queue<Vector3Int> pendingActivations = new Queue<Vector3Int>();
    private readonly List<Vector3Int> coordinates = new List<Vector3Int>();
    // 스트리밍 대상과 지형 관리 참조
    private TerrainManager owner;
    private TerrainChunkRegistry registry;
    private TerrainGridGeometry grid;
    private Transform target;
    // 대상 청크, 축별 활성 범위와 월드 기준 청크 크기
    private Vector3Int targetCoordinate;
    private Vector3Int loadRadius;
    private Vector3Int unloadRadius;
    private Vector3 chunkWorldSize;
    // 초기화 및 첫 활성화 작업의 진행 상태
    private bool initialized;

    public bool IsInitialLoadComplete { get; private set; }
    public int PendingActivationCount => pendingActivations.Count;

    // 대상과 청크 크기로 활성 범위를 계산하고 주변 청크와 초기 대기열을 준비한다.
    public void Initialize(TerrainManager terrain, TerrainChunkRegistry registry, TerrainGridGeometry grid, Transform player)
    {
        Reset();
        owner = terrain;
        this.registry = registry;
        this.grid = grid;
        target = player;
        chunkWorldSize = grid.GetChunkWorldSize(owner.transform.lossyScale);
        grid.GetStreamingRadii(
            loadDistance, unloadDistance, chunkWorldSize, out loadRadius, out unloadRadius);
        initialized = true;
        targetCoordinate = GetTargetCoordinate();
        ActivateImmediateNeighbors();
        RefreshActivationCoordinates();
        IsInitialLoadComplete = pendingActivations.Count == 0;
    }

    // 대상 이동을 반영하고 프레임당 제한 개수만큼 대기 중인 청크를 활성화한다.
    public void Tick()
    {
        if (!initialized)
        {
            return;
        }

        UpdateTarget();
        int count = Mathf.Min(Mathf.Max(1, maxChunkActivationsPerFrame), pendingActivations.Count);
        for (int i = 0; i < count; i++)
        {
            registry.SetChunkActive(pendingActivations.Dequeue(), true);
        }

        if (pendingActivations.Count == 0)
        {
            IsInitialLoadComplete = true;
        }
    }

    // 대상이 다른 청크로 이동하면 바로 주변을 활성화하고 거리별 대기열을 갱신한다.
    public void UpdateTarget()
    {
        if (!initialized)
        {
            return;
        }

        Vector3Int nextCoordinate = GetTargetCoordinate();
        if (nextCoordinate != targetCoordinate)
        {
            targetCoordinate = nextCoordinate;
            // Also covers teleporting: enable support before hiding the old neighborhood.
            ActivateImmediateNeighbors();
            RefreshActivationCoordinates();
        }
    }

    // 스트리밍 진행 상태와 대기 목록을 초기화한다.
    public void Reset()
    {
        initialized = false;
        IsInitialLoadComplete = false;
        pendingActivations.Clear();
        coordinates.Clear();
    }

    // 대상의 월드 위치를 지형 내부 청크 좌표로 바꾸고 지형 범위 안으로 제한한다.
    private Vector3Int GetTargetCoordinate()
    {
        Vector3 position = owner.transform.InverseTransformPoint(target.position);
        return grid.LocalPositionToChunkCoord(position);
    }

    // 대상 청크와 각 축으로 한 칸 이내인 이웃 청크를 즉시 활성화한다.
    private void ActivateImmediateNeighbors()
    {
        grid.GetClampedChunkBounds(
            targetCoordinate, Vector3Int.one,
            out Vector3Int min, out Vector3Int max);
        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                for (int z = min.z; z <= max.z; z++)
                {
                    registry.SetChunkActive(new Vector3Int(x, y, z), true);
                }
            }
        }
    }

    // 해제 거리 밖 청크를 끄고 활성 범위의 비활성 청크를 가까운 순서로 대기열에 넣는다.
    private void RefreshActivationCoordinates()
    {
        coordinates.Clear();
        foreach (Vector3Int coordinate in registry.ChunkCoordinates)
        {
            if (registry.IsChunkActive(coordinate) &&
                grid.IsOutsideRadius(coordinate, targetCoordinate, unloadRadius))
            {
                coordinates.Add(coordinate);
            }
        }
        foreach (Vector3Int coordinate in coordinates)
        {
            registry.SetChunkActive(coordinate, false);
        }

        pendingActivations.Clear();
        coordinates.Clear();
        grid.GetClampedChunkBounds(
            targetCoordinate, loadRadius,
            out Vector3Int min, out Vector3Int max);
        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                for (int z = min.z; z <= max.z; z++)
                {
                    Vector3Int coordinate = new Vector3Int(x, y, z);
                    if (!registry.IsChunkActive(coordinate))
                    {
                        coordinates.Add(coordinate);
                    }
                }
            }
        }
        coordinates.Sort((a, b) =>
            grid.GetSquaredWorldDistance(a, targetCoordinate, chunkWorldSize).CompareTo(
                grid.GetSquaredWorldDistance(b, targetCoordinate, chunkWorldSize)));
        foreach (Vector3Int coordinate in coordinates)
        {
            pendingActivations.Enqueue(coordinate);
        }
    }
}
