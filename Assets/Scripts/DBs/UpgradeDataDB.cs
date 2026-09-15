using System.Collections.Generic;
using UnityEngine;

public static class UpgradeDataDB
{
    private static Dictionary<string, UpgradeDataSO> upgradeDataMap;

    // 저장된 ID로 Resources/SOs/UpgradeDatas 안의 업그레이드 데이터를 찾는다.
    public static UpgradeDataSO GetData(string id)
    {
        if (upgradeDataMap == null)
        {
            upgradeDataMap = new Dictionary<string, UpgradeDataSO>();
            foreach (UpgradeDataSO data in Resources.LoadAll<UpgradeDataSO>("SOs/UpgradeDatas"))
                upgradeDataMap.Add(data.Id, data);
        }

        if (upgradeDataMap.TryGetValue(id, out UpgradeDataSO result))
            return result;

        Debug.LogWarning($"There is no UpgradeDataSO. id : {id}");
        return null;
    }
}
