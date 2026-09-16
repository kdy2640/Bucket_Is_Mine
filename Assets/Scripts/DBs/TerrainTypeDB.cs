using System.Collections.Generic;
using UnityEngine;

public static class TerrainTypeDB
{
    private static readonly Dictionary<byte, TerrainTypeSO> terrainTypeMap =
        new Dictionary<byte, TerrainTypeSO>();

    static TerrainTypeDB()
    {
        foreach (TerrainTypeSO data in Resources.LoadAll<TerrainTypeSO>("SOs/TerrainDatas"))
            terrainTypeMap.Add(data.Layer.TypeId, data);
    }

    public static TerrainTypeSO GetData(byte typeId)
    {
        return terrainTypeMap[typeId];
    }

    // 원본 SO를 유지한 채 자연 지층의 값만 복사하고 시작 높이순으로 정렬한다.
    public static TerrainLayer[] GetLayers()
    {
        List<TerrainLayer> layers = new List<TerrainLayer>();
        foreach (TerrainTypeSO data in terrainTypeMap.Values)
        {
            TerrainLayer layer = data.Layer;
            if (layer.TypeId != TerrainData.ArtificialTypeId)
                layers.Add(layer);
        }
        layers.Sort((a, b) => a.YStart.CompareTo(b.YStart));
        return layers.ToArray();
    }
}
