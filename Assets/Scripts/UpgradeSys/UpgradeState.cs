/// <summary>
/// 플레이어가 보유한 업그레이드와 현재 레벨을 나타냅니다.
/// </summary>
[System.Serializable]
public class UpgradeState
{
    public UpgradeDataSO data;
    public int level;

    public bool TryGetCurrentCost(out int requiredCost)
    {
        if (data == null)
        {
            requiredCost = 0;
            return false;
        }

        return data.TryGetRequiredCost(level + 1, out requiredCost);
    }
}
