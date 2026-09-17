using System.Collections.Generic;
using UnityEngine;

public static class OreDataDB
{
    private static readonly Dictionary<int, OreDataSO> oreDataMap = new();

    static OreDataDB()
    {
        foreach (OreDataSO data in Resources.LoadAll<OreDataSO>("SOs/OreDatas"))
            oreDataMap.Add(data.Id, data);
    }

    public static OreDataSO GetData(int id)
    {
        return oreDataMap[id];
    }
}
