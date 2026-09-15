using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 업그레이드 노드가 공통으로 사용하는 데이터입니다.
/// </summary>
[CreateAssetMenu(menuName = "Upgrade/Upgrade Data")]
public class UpgradeDataSO : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite displayIcon;

    [SerializeField] private List<int> requiredCosts = new() { 0, 0, 0, 0, 0 };
    [SerializeField] private int maxLevel = 1;

    public string Id => id;
    public string DisplayName => displayName;
    public Sprite DisplayIcon => displayIcon;
    public IReadOnlyList<int> RequiredCosts => requiredCosts;
    public int MaxLevel => maxLevel;

    /// <summary>
    /// 목표 업그레이드 레벨에 필요한 재화량을 조회합니다.
    /// </summary>
    public bool TryGetRequiredCost(int targetUpgradeLevel, out int requiredCost)
    {
        int index = targetUpgradeLevel - 1;

        if (index < 0 || index >= requiredCosts.Count)
        {
            requiredCost = 0;
            return false;
        }

        requiredCost = Mathf.Max(0, requiredCosts[index]);
        return true;
    }
}
