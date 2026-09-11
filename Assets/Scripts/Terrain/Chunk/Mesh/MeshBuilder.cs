using System;
using Unity.Collections;
using UnityEngine;

internal struct MeshBuilder : IDisposable
{
    private NativeHashMap<Vector3, int> vertexDict;
    private readonly bool isSmoothShading;

    public NativeList<Vector3> Vertices;
    public NativeList<Vector3> Normals;
    public NativeList<int> Triangles;
    public bool NeedsFaceNormals;

    public MeshBuilder(bool isSmoothShading)
    {
        this.isSmoothShading = isSmoothShading;
        vertexDict = new NativeHashMap<Vector3, int>(1, Allocator.TempJob);
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
        Vertices.Dispose();
        Normals.Dispose();
        Triangles.Dispose();
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
