using System;
using System.Collections.Generic;
using UnityEngine;

public enum UpgradeAvailability
{
    Available,
    InvalidData,
    MaxLevel,
    InsufficientCurrency
}

public class UpgradeManager : MonoBehaviour
{
    [SerializeField] private List<UpgradeState> upgradeStates = new();

    private readonly Dictionary<string, UpgradeState> upgradeStateMap = new();
    private Action onUpgradeChanged;

    private void Awake()
    {
        foreach (UpgradeState state in upgradeStates)
            upgradeStateMap.Add(state.data.Id, state);
    }

    public UpgradeState GetState(UpgradeDataSO data)
    {
        if (data == null || string.IsNullOrEmpty(data.Id))
            return null;

        if (upgradeStateMap.TryGetValue(data.Id, out UpgradeState state))
        {
            if (state.data != data)
            {
                Debug.LogError($"중복된 UpgradeData id가 있습니다: {data.Id}");
                return null;
            }

            return state;
        }

        state = new UpgradeState { data = data, level = 0 };
        upgradeStates.Add(state);
        upgradeStateMap.Add(data.Id, state);
        return state;
    }

    public bool HasState(UpgradeDataSO data)
    {
        return data != null && !string.IsNullOrEmpty(data.Id)
            && upgradeStateMap.ContainsKey(data.Id);
    }

    public bool IsMaxLevel(UpgradeState state)
    {
        return state != null && state.data != null && state.level >= state.data.MaxLevel;
    }

    public UpgradeAvailability GetUpgradeAvailability(UpgradeDataSO data)
    {
        if (data == null || string.IsNullOrEmpty(data.Id))
            return UpgradeAvailability.InvalidData;

        int level = 0;
        if (upgradeStateMap.TryGetValue(data.Id, out UpgradeState state))
        {
            if (state.data != data)
                return UpgradeAvailability.InvalidData;

            level = state.level;
        }

        if (level >= data.MaxLevel)
            return UpgradeAvailability.MaxLevel;

        if (!data.TryGetRequiredCost(level + 1, out int cost))
            return UpgradeAvailability.InvalidData;

        return GameManager.Instance.StockManager.CanConsumeCurrency(cost)
            ? UpgradeAvailability.Available
            : UpgradeAvailability.InsufficientCurrency;
    }

    public bool CanUpgrade(UpgradeDataSO data)
    {
        return GetUpgradeAvailability(data) == UpgradeAvailability.Available;
    }

    public bool TryUpgrade(UpgradeDataSO data)
    {
        if (!CanUpgrade(data))
            return false;

        UpgradeState state = GetState(data);
        if (!state.TryGetCurrentCost(out int cost)
            || !GameManager.Instance.StockManager.TryConsumeCurrency(cost))
            return false;

        state.level++;
        GameManager.Instance.Utility.Audio.PlaySFX(SFXType.Hub_Upgrade);
        onUpgradeChanged?.Invoke();
        return true;
    }

    public void SubscribeUpgradeChanged(Action callback)
    {
        onUpgradeChanged += callback;
    }

    public void UnsubscribeUpgradeChanged(Action callback)
    {
        onUpgradeChanged -= callback;
    }

    public List<UpgradeSaveData> CreateUpgradeSaveData()
    {
        List<UpgradeSaveData> saveData = new();
        foreach (UpgradeState state in upgradeStates)
            saveData.Add(new UpgradeSaveData(state.data.Id, state.level));

        return saveData;
    }

    public void LoadUpgradeSaveData(List<UpgradeSaveData> saveData)
    {
        upgradeStates.Clear();
        upgradeStateMap.Clear();

        if (saveData != null)
        {
            foreach (UpgradeSaveData savedState in saveData)
            {
                if (savedState == null || string.IsNullOrEmpty(savedState.id))
                    continue;

                UpgradeDataSO data = UpgradeDataDB.GetData(savedState.id);
                if (data == null)
                    continue;

                GetState(data).level = Mathf.Clamp(savedState.level, 0, data.MaxLevel);
            }
        }

        onUpgradeChanged?.Invoke();
    }

    public void ResetUpgradeSaveData()
    {
        upgradeStates.Clear();
        upgradeStateMap.Clear();
        onUpgradeChanged?.Invoke();
    }
}
