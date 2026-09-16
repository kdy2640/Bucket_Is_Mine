using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

// 청크의 정점·노멀·삼각형을 모으고 Unity 메시 버퍼로 옮기는 작업용 구조체다.
internal struct MeshBuilder : IDisposable
{
    // 정점 중복 제거와 면 노멀 계산용 작업 데이터
    private NativeHashMap<Vector3, int> vertexDict;
    private NativeList<Vector3> faceNormals;
    private readonly bool isSmoothShading;

    // 완성할 메시의 정점·노멀·삼각형 버퍼
    public NativeList<Vector3> Vertices;
    public NativeList<Vector3> Normals;
    public NativeList<Color> Colors;
    public NativeList<int> Triangles;
    // 밀도 기울기가 0인 정점의 면 노멀 보완 여부
    public bool NeedsFaceNormals;

    // 셰이딩 방식을 저장하고 메시 조립에 필요한 임시 네이티브 버퍼를 할당한다.
    public MeshBuilder(bool isSmoothShading)
    {
        this.isSmoothShading = isSmoothShading;
        vertexDict = new NativeHashMap<Vector3, int>(1, Allocator.TempJob);
        faceNormals = new NativeList<Vector3>(Allocator.TempJob);
        Vertices = new NativeList<Vector3>(Allocator.TempJob);
        Normals = new NativeList<Vector3>(Allocator.TempJob);
        Colors = new NativeList<Color>(Allocator.TempJob);
        Triangles = new NativeList<int>(Allocator.TempJob);
        NeedsFaceNormals = false;
    }

    // 세 정점을 등록하고 지형 표면 방향에 맞는 순서로 삼각형 인덱스를 추가한다.
    public void AddTriangle(
        Vector3 vertex0, Vector3 vertex1, Vector3 vertex2,
        Vector3 normal0, Vector3 normal1, Vector3 normal2,
        Color color0, Color color1, Color color2)
    {
        // 임계값 교차점이 같은 위치나 직선으로 모이면 충돌면을 만들 수 없다.
        float3 faceNormal = math.cross((float3)(vertex1 - vertex0), (float3)(vertex2 - vertex0));
        if (math.lengthsq(faceNormal) == 0f)
        {
            return;
        }

        int index0 = AddVertex(vertex0, normal0, color0);
        int index1 = AddVertex(vertex1, normal1, color1);
        int index2 = AddVertex(vertex2, normal2, color2);

        Triangles.Add(index0);
        Triangles.Add(index2);
        Triangles.Add(index1);
    }

    // 정점 조회용 맵과 정점·노멀·삼각형 버퍼를 해제한다.
    public void Dispose()
    {
        vertexDict.Dispose();
        faceNormals.Dispose();
        Vertices.Dispose();
        Normals.Dispose();
        Colors.Dispose();
        Triangles.Dispose();
    }

    // 평면 셰이딩의 노멀을 계산하거나 밀도 기울기로 구하지 못한 노멀을 보완한다.
    public void CompleteNormals()
    {
        if (isSmoothShading && !NeedsFaceNormals)
        {
            return;
        }

        faceNormals.Resize(Vertices.Length, NativeArrayOptions.ClearMemory);
        for (int i = 0; i < Triangles.Length; i += 3)
        {
            int index0 = Triangles[i];
            int index1 = Triangles[i + 1];
            int index2 = Triangles[i + 2];
            // Match the float products used by Unity's mesh normal recalculation.
            Vector3 normal = math.cross(
                (float3)(Vertices[index1] - Vertices[index0]),
                (float3)(Vertices[index2] - Vertices[index0]));
            faceNormals[index0] += normal;
            faceNormals[index1] += normal;
            faceNormals[index2] += normal;
        }

        if (!isSmoothShading)
        {
            Normals.ResizeUninitialized(Vertices.Length);
        }

        for (int i = 0; i < Vertices.Length; i++)
        {
            if (!isSmoothShading || Normals[i].sqrMagnitude == 0f)
            {
                Vector3 normal = faceNormals[i];
                float lengthSquared = normal.sqrMagnitude;
                Normals[i] = lengthSquared > 0f ? normal / Mathf.Sqrt(lengthSquared) : Vector3.zero;
            }
        }
    }

    // 정점·노멀·인덱스와 서브메시 정보를 기록하고 정점들을 감싸는 경계 상자를 반환한다.
    public Bounds WriteMeshData(
        Mesh.MeshData meshData,
        NativeArray<VertexAttributeDescriptor> vertexAttributes)
    {
        Bounds bounds = default;
        if (Vertices.Length > 0)
        {
            Vector3 min = Vertices[0];
            Vector3 max = min;
            for (int i = 1; i < Vertices.Length; i++)
            {
                min = Vector3.Min(min, Vertices[i]);
                max = Vector3.Max(max, Vertices[i]);
            }

            bounds.SetMinMax(min, max);
        }

        meshData.SetVertexBufferParams(Vertices.Length, vertexAttributes);
        meshData.SetIndexBufferParams(Triangles.Length, IndexFormat.UInt32);
        meshData.GetVertexData<Vector3>(0).CopyFrom(Vertices.AsArray());
        meshData.GetVertexData<Vector3>(1).CopyFrom(Normals.AsArray());
        meshData.GetVertexData<Color>(2).CopyFrom(Colors.AsArray());
        meshData.GetIndexData<int>().CopyFrom(Triangles.AsArray());
        meshData.subMeshCount = 1;
        meshData.SetSubMesh(0, new SubMeshDescriptor(0, Triangles.Length, MeshTopology.Triangles)
        {
            firstVertex = 0,
            vertexCount = Vertices.Length,
            bounds = bounds
        }, MeshUpdateFlags.DontRecalculateBounds);
        return bounds;
    }

    // 부드러운 셰이딩에서는 같은 위치의 정점을 재사용하고 평면 셰이딩에서는 새로 추가한다.
    private int AddVertex(Vector3 vertex, Vector3 normal, Color color)
    {
        if (isSmoothShading)
        {
            if (vertexDict.TryGetValue(vertex, out int existingIndex))
            {
                return existingIndex;
            }

            int index = Vertices.Length;
            vertexDict.Add(vertex, index);
            Vertices.Add(vertex);
            Normals.Add(normal);
            Colors.Add(color);
            NeedsFaceNormals |= normal.sqrMagnitude == 0f;
            return index;
        }

        Vertices.Add(vertex);
        Colors.Add(color);
        return Vertices.Length - 1;
    }
}
