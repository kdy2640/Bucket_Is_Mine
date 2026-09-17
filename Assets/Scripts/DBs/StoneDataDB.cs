using System.Collections.Generic;
using UnityEngine;

public static class StoneDataDB
{
    private static readonly Dictionary<int, StoneDataSO> stoneDataMap = new();

    static StoneDataDB()
    {
        foreach (StoneDataSO data in Resources.LoadAll<StoneDataSO>("SOs/StoneDatas"))
            stoneDataMap.Add(data.Id, data);
    }

    public static StoneDataSO GetData(int stoneID)
    {
        return stoneDataMap[stoneID];
    }
}
