using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

public class TerrainMeshGenerator : IDisposable
{
    private static readonly ProfilerMarker PrepareMarker = new ProfilerMarker("TerrainMesh.Prepare");
    private static readonly ProfilerMarker ScheduleMarker = new ProfilerMarker("TerrainMesh.Schedule");
    private static readonly ProfilerMarker CompleteMarker = new ProfilerMarker("TerrainMesh.Complete");
    private static readonly ProfilerMarker ApplyMarker = new ProfilerMarker("TerrainMesh.Apply");
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
        if (meshes.Length == 0)
        {
            return meshes;
        }

        // Managed arrays own the per-chunk containers; Jobs receive only their own buffers.
        MeshBuilder[] builders = new MeshBuilder[meshes.Length];
        Mesh.MeshDataArray[] meshData = new Mesh.MeshDataArray[meshes.Length];
        NativeArray<Bounds>[] boundsResults = new NativeArray<Bounds>[meshes.Length];
        NativeArray<JobHandle> handles = new NativeArray<JobHandle>(meshes.Length, Allocator.Temp);
        int preparedBuilders = 0;
        int preparedMeshData = 0;
        int preparedBounds = 0;
        int appliedMeshes = 0;
        bool succeeded = false;
        try
        {
            using (PrepareMarker.Auto())
            {
                for (int i = 0; i < meshes.Length; i++)
                {
                    builders[i] = new MeshBuilder(isSmoothShading);
                    preparedBuilders++;
                    meshData[i] = Mesh.AllocateWritableMeshData(1);
                    preparedMeshData++;
                    boundsResults[i] = new NativeArray<Bounds>(1, Allocator.TempJob);
                    preparedBounds++;
                }
            }

            using (ScheduleMarker.Auto())
            {
                for (int i = 0; i < meshes.Length; i++)
                {
                    ChunkMeshInput input = new ChunkMeshInput(data, chunkCoords[i]);
                    BuildChunkMeshJob job = new BuildChunkMeshJob
                    {
                        Mesher = new MarchingCubesMesher(
                            input, corners, edgeCornerIndexes, triangleTable, threshold, isSmoothShading),
                        Builder = builders[i],
                        MeshData = meshData[i][0],
                        VertexAttributes = vertexAttributes,
                        BoundsResult = boundsResults[i]
                    };
                    handles[i] = job.Schedule();
                }
            }

            using (CompleteMarker.Auto())
            {
                JobHandle.CompleteAll(handles);
            }
            using (ApplyMarker.Auto())
            {
                for (int i = 0; i < meshes.Length; i++)
                {
                    meshes[i] = new Mesh();
                    Mesh.ApplyAndDisposeWritableMeshData(meshData[i], meshes[i], MeshUpdateFlags.DontRecalculateBounds);
                    appliedMeshes++;
                    meshes[i].bounds = boundsResults[i][0];
                }
            }

            succeeded = true;
            return meshes;
        }
        finally
        {
            // Also finish already scheduled Jobs when preparation/scheduling/application fails.
            JobHandle.CompleteAll(handles);
            handles.Dispose();
            for (int i = appliedMeshes; i < preparedMeshData; i++)
            {
                meshData[i].Dispose();
            }
            for (int i = 0; i < preparedBounds; i++)
            {
                boundsResults[i].Dispose();
            }
            for (int i = 0; i < preparedBuilders; i++)
            {
                builders[i].Dispose();
            }
            if (!succeeded)
            {
                foreach (Mesh mesh in meshes)
                {
                    if (mesh == null) continue;
                    if (Application.isPlaying) UnityEngine.Object.Destroy(mesh);
                    else UnityEngine.Object.DestroyImmediate(mesh);
                }
            }
        }
    }

    public void Dispose()
    {
        corners.Dispose();
        edgeCornerIndexes.Dispose();
        triangleTable.Dispose();
        vertexAttributes.Dispose();
    }
}
