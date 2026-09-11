using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class TerrainMeshGenerator : IDisposable
{
    private NativeArray<Vector3Int> corners;
    private NativeArray<int> edgeCornerIndexes;
    private NativeArray<int> triangleTable;

    public TerrainMeshGenerator()
    {
        corners = new NativeArray<Vector3Int>(MarchingTable.Corners, Allocator.Persistent);
        edgeCornerIndexes = new NativeArray<int>(new[]
        {
            0, 1, 1, 2, 3, 2, 0, 3,
            4, 5, 5, 6, 7, 6, 4, 7,
            0, 4, 1, 5, 2, 6, 3, 7
        }, Allocator.Persistent);
        triangleTable = new NativeArray<int>(MarchingTable.Triangles.Length, Allocator.Persistent);
        for (int config = 0; config < MarchingTable.Triangles.GetLength(0); config++)
        {
            for (int edge = 0; edge < MarchingTable.Triangles.GetLength(1); edge++)
            {
                triangleTable[config * 16 + edge] = MarchingTable.Triangles[config, edge];
            }
        }
    }

    // Returned meshes belong to the caller; original density arrays remain owned by TerrainData.
    public Mesh[] Generate(
        TerrainData data,
        IReadOnlyList<Vector3Int> chunkCoords,
        float threshold,
        bool isSmoothShading)
    {
        Mesh[] meshes = new Mesh[chunkCoords.Count];
        for (int i = 0; i < chunkCoords.Count; i++)
        {
            ChunkMeshInput input = new ChunkMeshInput(data, chunkCoords[i]);
            MeshBuilder builder = new MeshBuilder(isSmoothShading);
            try
            {
                MarchingCubesMesher mesher = new MarchingCubesMesher(
                    input, corners, edgeCornerIndexes, triangleTable, threshold, isSmoothShading);
                mesher.Build(ref builder);
                meshes[i] = CreateMesh(ref builder, isSmoothShading);
            }
            finally
            {
                builder.Dispose();
            }
        }

        return meshes;
    }

    public void Dispose()
    {
        corners.Dispose();
        edgeCornerIndexes.Dispose();
        triangleTable.Dispose();
    }

    private static Mesh CreateMesh(ref MeshBuilder builder, bool isSmoothShading)
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(builder.Vertices.AsArray());
        mesh.SetIndices(builder.Triangles.AsArray(), MeshTopology.Triangles, 0, false, 0);
        if (!isSmoothShading || builder.NeedsFaceNormals)
        {
            mesh.RecalculateNormals();
        }

        if (isSmoothShading)
        {
            if (builder.NeedsFaceNormals)
            {
                Vector3[] faceNormals = mesh.normals;
                for (int i = 0; i < builder.Normals.Length; i++)
                {
                    if (builder.Normals[i].sqrMagnitude == 0f)
                    {
                        builder.Normals[i] = faceNormals[i];
                    }
                }
            }

            mesh.SetNormals(builder.Normals.AsArray());
        }

        mesh.RecalculateBounds();
        return mesh;
    }
}
