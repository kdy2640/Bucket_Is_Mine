using UnityEngine;

// 종류별 원본 설정은 SO 안의 레이어 하나이며 실행 시 그 값을 복사해 사용한다.
[CreateAssetMenu(menuName = "Terrain/Terrain Type")]
public class TerrainTypeSO : ScriptableObject
{
    [SerializeField] private TerrainLayer layer;

    public TerrainLayer Layer => layer;
}
