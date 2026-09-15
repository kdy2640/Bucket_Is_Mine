using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

// 한 청크의 삼각형과 노멀을 만들고 Unity 메시 버퍼에 기록하는 Burst Job이다.
[BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.High)]
internal struct BuildChunkMeshJob : IJob
{
    // 표면 추출과 메시 조립을 수행할 작업 데이터
    public MarchingCubesMesher Mesher;
    public MeshBuilder Builder;
    // Unity 메시 출력 버퍼와 정점 형식
    public Mesh.MeshData MeshData;
    [ReadOnly] public NativeArray<VertexAttributeDescriptor> VertexAttributes;
    // 생성된 메시의 경계 상자 출력
    [WriteOnly] public NativeArray<Bounds> BoundsResult;

    // 청크 표면을 추출하고 노멀을 완성한 뒤 메시 데이터와 경계 상자를 기록한다.
    public void Execute()
    {
        Mesher.Build(ref Builder);
        Builder.CompleteNormals();
        BoundsResult[0] = Builder.WriteMeshData(MeshData, VertexAttributes);
    }
}
