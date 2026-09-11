using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MeshBuilder
{
    private readonly Dictionary<Vector3, int> vertexDict = new Dictionary<Vector3, int>();
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<int> triangles = new List<int>();
    private readonly bool isSmoothShading;

    public MeshBuilder(bool isSmoothShading)
    {
        this.isSmoothShading = isSmoothShading;
    }

    public void AddTriangle(Vector3 vertex0, Vector3 vertex1, Vector3 vertex2)
    {
        int index0 = AddVertex(vertex0);
        int index1 = AddVertex(vertex1);
        int index2 = AddVertex(vertex2);

        triangles.Add(index0);
        triangles.Add(index2);
        triangles.Add(index1);
    }

    public Mesh ToMesh()
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private int AddVertex(Vector3 vertex)
    {
        if (isSmoothShading)
        {
            if (!vertexDict.ContainsKey(vertex))
            {
                vertexDict[vertex] = vertices.Count;
                vertices.Add(vertex);
            }

            return vertexDict[vertex];
        }

        vertices.Add(vertex);
        return vertices.Count - 1;
    }
}
