using UnityEngine;

// 청크 오브젝트와 렌더링·충돌 처리에 사용하는 컴포넌트 참조를 보관한다.
public class ChunkObject
{
    // 청크 오브젝트와 메시·렌더링·충돌 참조
    public GameObject gameObject;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public MeshCollider meshCollider;
    public StoneSpawnData[] stoneSpawns;
    public bool stonesSpawned;
}
