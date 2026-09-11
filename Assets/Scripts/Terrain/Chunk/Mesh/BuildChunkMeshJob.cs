using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

[BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.High)]
internal struct BuildChunkMeshJob : IJob
{
    public MarchingCubesMesher Mesher;
    public MeshBuilder Builder;
    public Mesh.MeshData MeshData;
    [ReadOnly] public NativeArray<VertexAttributeDescriptor> VertexAttributes;
    [WriteOnly] public NativeArray<Bounds> BoundsResult;

    public void Execute()
    {
        Mesher.Build(ref Builder);
        Builder.CompleteNormals();
        BoundsResult[0] = Builder.WriteMeshData(MeshData, VertexAttributes);
    }
}
