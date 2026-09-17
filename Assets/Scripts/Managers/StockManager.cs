using System;
using System.Collections.Generic;
using UnityEngine;

public class StockManager : MonoBehaviour
{
    [SerializeField] private StockData stockData = new();

    private Action onStockDataChanged;

    // UI 등에서 현재 재고를 읽을 때 사용.
    public IReadableStockData StockData => stockData;

    #region Currency

    // 재화를 획득했을 때 사용.
    public void AddCurrency(int amount)
    {
        if (!IsValidCurrencyAmount(amount))
        {
            Debug.LogWarning("StockManager.AddCurrency에는 0 이상의 유한한 값만 전달할 수 있습니다.");
            return;
        }

        int addedCurrency = (int)Math.Min((long)stockData.currency + amount, int.MaxValue);
        SetCurrency(addedCurrency);
    }

    // 비용을 지불할 수 있는지 확인할 때 사용.
    public bool CanConsumeCurrency(int amount)
    {
        return IsValidCurrencyAmount(amount) && stockData.currency >= amount;
    }

    // 비용을 확인하고 실제로 지불할 때 사용.
    public bool TryConsumeCurrency(int amount)
    {
        if (!CanConsumeCurrency(amount))
            return false;

        SetCurrency(stockData.currency - amount);
        return true;
    }

    private void SetCurrency(int amount, bool forceNotify = false)
    {
        int clampedAmount = Mathf.Max(0, amount);

        if (!forceNotify && stockData.currency == clampedAmount)
            return;

        stockData.currency = clampedAmount;
        NotifyStockDataChanged();
    }

    private static bool IsValidCurrencyAmount(int amount)
    {
        return amount >= 0;
    }

    #endregion

    #region Ore

    // 광석 하나를 획득했을 때 사용.
    public void AddOre(OreAmount oreAmount)
    {
        AddOre(new List<OreAmount> { oreAmount });
    }

    // 광석 여러 개를 한 번에 획득했을 때 사용.
    public void AddOre(List<OreAmount> oreAmounts)
    {
        if (oreAmounts == null)
        {
            Debug.LogWarning("StockManager.AddOre에는 null이 아닌 0 이상의 광석 수량만 전달할 수 있습니다.");
            return;
        }

        foreach (OreAmount oreAmount in oreAmounts)
        {
            if (oreAmount == null || oreAmount.amount < 0)
            {
                Debug.LogWarning("StockManager.AddOre에는 null이 아닌 0 이상의 광석 수량만 전달할 수 있습니다.");
                return;
            }
        }

        bool hasChanged = false;

        foreach (OreAmount oreAmount in oreAmounts)
        {
            if (oreAmount.amount == 0)
                continue;

            OreAmount target = stockData.ores.Find(
                entry => entry != null && entry.oreId == oreAmount.oreId);

            if (target == null)
            {
                stockData.ores.Add(new OreAmount(
                    oreAmount.oreId,
                    oreAmount.amount));
                hasChanged = true;
                continue;
            }

            int addedAmount = (int)Math.Min(
                (long)Mathf.Max(0, target.amount) + oreAmount.amount,
                int.MaxValue);

            if (target.amount == addedAmount)
                continue;

            target.amount = addedAmount;
            hasChanged = true;
        }

        if (hasChanged)
            NotifyStockDataChanged();
    }

    // 광석 하나를 사용할 수 있는지 확인할 때 사용.
    public bool CanConsumeOre(OreAmount oreAmount)
    {
        return CanConsumeOre(new List<OreAmount> { oreAmount });
    }

    // 여러 광석을 모두 사용할 수 있는지 확인할 때 사용.
    public bool CanConsumeOre(List<OreAmount> oreAmounts)
    {
        if (oreAmounts == null)
            return false;

        foreach (OreAmount oreAmount in oreAmounts)
        {
            if (oreAmount == null || oreAmount.amount < 0)
                return false;

            long requiredAmount = 0;

            foreach (OreAmount requestedAmount in oreAmounts)
            {
                if (requestedAmount != null
                    && requestedAmount.oreId == oreAmount.oreId)
                {
                    requiredAmount += requestedAmount.amount;
                }
            }

            if (GetOreAmount(oreAmount.oreId) < requiredAmount)
                return false;
        }

        return true;
    }

    // 광석 하나를 확인하고 실제로 사용할 때 사용.
    public bool TryConsumeOre(OreAmount oreAmount)
    {
        return TryConsumeOre(new List<OreAmount> { oreAmount });
    }

    // 여러 광석을 확인하고 한 번에 사용할 때 사용.
    public bool TryConsumeOre(List<OreAmount> oreAmounts)
    {
        if (!CanConsumeOre(oreAmounts))
            return false;

        bool hasChanged = false;

        foreach (OreAmount oreAmount in oreAmounts)
        {
            int remainingAmount = oreAmount.amount;

            if (remainingAmount == 0)
                continue;

            hasChanged = true;

            foreach (OreAmount stockOreAmount in stockData.ores)
            {
                if (remainingAmount == 0)
                    break;

                if (stockOreAmount == null
                    || stockOreAmount.oreId != oreAmount.oreId)
                    continue;

                int consumableAmount = Mathf.Min(
                    Mathf.Max(0, stockOreAmount.amount),
                    remainingAmount);
                stockOreAmount.amount -= consumableAmount;
                remainingAmount -= consumableAmount;
            }
        }

        if (hasChanged)
            NotifyStockDataChanged();

        return true;
    }

    public int GetOreAmount(int oreId)
    {
        long total = 0;

        foreach (OreAmount oreAmount in stockData.ores)
        {
            if (oreAmount != null && oreAmount.oreId == oreId)
                total += Mathf.Max(0, oreAmount.amount);
        }

        return (int)Math.Min(total, int.MaxValue);
    }

    #endregion

    #region Save Data

    public StockSaveData CreateStockSaveData()
    {
        StockSaveData saveData = new()
        {
            currency = stockData.currency
        };

        foreach (OreAmount oreAmount in stockData.ores)
        {
            if (oreAmount == null)
                continue;

            saveData.ores.Add(new OreAmount(
                oreAmount.oreId,
                oreAmount.amount));
        }

        return saveData;
    }

    public void LoadStockSaveData(StockSaveData saveData)
    {
        stockData = new StockData();

        if (saveData != null)
        {
            if (IsValidCurrencyAmount(saveData.currency))
                stockData.currency = saveData.currency;

            if (saveData.ores != null)
            {
                foreach (OreAmount oreAmount in saveData.ores)
                {
                    if (oreAmount == null)
                        continue;

                    stockData.ores.Add(new OreAmount(
                        oreAmount.oreId,
                        Mathf.Max(0, oreAmount.amount)));
                }
            }


        }

        NotifyStockDataChanged();
    }

    public void ResetStockSaveData()
    {
        stockData = new StockData();
        NotifyStockDataChanged();
    }

    #endregion

    #region Stock Data Change

    // 재고가 바뀔 때 갱신이 필요한 객체가 사용.
    public void SubscribeStockDataChange(Action callback)
    {
        onStockDataChanged += callback;
    }

    // 재고 변경 알림이 더 이상 필요하지 않을 때 사용.
    public void UnsubscribeStockDataChange(Action callback)
    {
        onStockDataChanged -= callback;
    }

    private void NotifyStockDataChanged()
    {
        onStockDataChanged?.Invoke();
    }

    #endregion
}
