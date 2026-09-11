using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

public class TerrainMeshGenerator : IDisposable
{
    private NativeArray<Vector3Int> corners;
    private NativeArray<int> edgeCornerIndexes;
    private NativeArray<int> triangleTable;
    private NativeArray<VertexAttributeDescriptor> vertexAttributes;

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
        vertexAttributes = new NativeArray<VertexAttributeDescriptor>(new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 1)
        }, Allocator.Persistent);
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
            Mesh.MeshDataArray meshData = Mesh.AllocateWritableMeshData(1);
            NativeArray<Bounds> boundsResult = new NativeArray<Bounds>(1, Allocator.TempJob);
            JobHandle handle = default;
            bool meshDataApplied = false;
            try
            {
                BuildChunkMeshJob job = new BuildChunkMeshJob
                {
                    Mesher = new MarchingCubesMesher(
                        input, corners, edgeCornerIndexes, triangleTable, threshold, isSmoothShading),
                    Builder = builder,
                    MeshData = meshData[0],
                    VertexAttributes = vertexAttributes,
                    BoundsResult = boundsResult
                };
                handle = job.Schedule();
                handle.Complete();

                Mesh mesh = new Mesh();
                Mesh.ApplyAndDisposeWritableMeshData(meshData, mesh, MeshUpdateFlags.DontRecalculateBounds);
                meshDataApplied = true;
                mesh.bounds = boundsResult[0];
                meshes[i] = mesh;
            }
            finally
            {
                handle.Complete();
                if (!meshDataApplied)
                {
                    meshData.Dispose();
                }

                boundsResult.Dispose();
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
        vertexAttributes.Dispose();
    }
}
