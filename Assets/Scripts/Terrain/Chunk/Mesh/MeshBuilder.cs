using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

internal struct MeshBuilder : IDisposable
{
    private NativeHashMap<Vector3, int> vertexDict;
    private NativeList<Vector3> faceNormals;
    private readonly bool isSmoothShading;

    public NativeList<Vector3> Vertices;
    public NativeList<Vector3> Normals;
    public NativeList<int> Triangles;
    public bool NeedsFaceNormals;

    public MeshBuilder(bool isSmoothShading)
    {
        this.isSmoothShading = isSmoothShading;
        vertexDict = new NativeHashMap<Vector3, int>(1, Allocator.TempJob);
        faceNormals = new NativeList<Vector3>(Allocator.TempJob);
        Vertices = new NativeList<Vector3>(Allocator.TempJob);
        Normals = new NativeList<Vector3>(Allocator.TempJob);
        Triangles = new NativeList<int>(Allocator.TempJob);
        NeedsFaceNormals = false;
    }

    public void AddTriangle(
        Vector3 vertex0, Vector3 vertex1, Vector3 vertex2,
        Vector3 normal0, Vector3 normal1, Vector3 normal2)
    {
        int index0 = AddVertex(vertex0, normal0);
        int index1 = AddVertex(vertex1, normal1);
        int index2 = AddVertex(vertex2, normal2);

        Triangles.Add(index0);
        Triangles.Add(index2);
        Triangles.Add(index1);
    }

    public void Dispose()
    {
        vertexDict.Dispose();
        faceNormals.Dispose();
        Vertices.Dispose();
        Normals.Dispose();
        Triangles.Dispose();
    }

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

    private int AddVertex(Vector3 vertex, Vector3 normal)
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
            NeedsFaceNormals |= normal.sqrMagnitude == 0f;
            return index;
        }

        Vertices.Add(vertex);
        return Vertices.Length - 1;
    }
}
