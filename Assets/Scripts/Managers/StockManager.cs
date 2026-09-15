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

    #region Ingredient

    // 식재료 하나를 획득했을 때 사용.
    public void AddIngredient(IngredientAmount ingredientAmount)
    {
        AddIngredient(new List<IngredientAmount> { ingredientAmount });
    }

    // 식재료 여러 개를 한 번에 획득했을 때 사용.
    public void AddIngredient(List<IngredientAmount> ingredientAmounts)
    {
        if (ingredientAmounts == null)
        {
            Debug.LogWarning("StockManager.AddIngredient에는 null이 아닌 0 이상의 식재료 수량만 전달할 수 있습니다.");
            return;
        }

        foreach (IngredientAmount ingredientAmount in ingredientAmounts)
        {
            if (ingredientAmount == null || ingredientAmount.amount < 0)
            {
                Debug.LogWarning("StockManager.AddIngredient에는 null이 아닌 0 이상의 식재료 수량만 전달할 수 있습니다.");
                return;
            }
        }

        bool hasChanged = false;

        foreach (IngredientAmount ingredientAmount in ingredientAmounts)
        {
            if (ingredientAmount.amount == 0)
                continue;

            IngredientAmount target = stockData.ingredients.Find(
                entry => entry != null && entry.ingredient == ingredientAmount.ingredient);

            if (target == null)
            {
                stockData.ingredients.Add(new IngredientAmount(
                    ingredientAmount.ingredient,
                    ingredientAmount.amount));
                hasChanged = true;
                continue;
            }

            int addedAmount = (int)Math.Min(
                (long)Mathf.Max(0, target.amount) + ingredientAmount.amount,
                int.MaxValue);

            if (target.amount == addedAmount)
                continue;

            target.amount = addedAmount;
            hasChanged = true;
        }

        if (hasChanged)
            NotifyStockDataChanged();
    }

    // 식재료 하나를 사용할 수 있는지 확인할 때 사용.
    public bool CanConsumeIngredient(IngredientAmount ingredientAmount)
    {
        return CanConsumeIngredient(new List<IngredientAmount> { ingredientAmount });
    }

    // 여러 식재료를 모두 사용할 수 있는지 확인할 때 사용.
    public bool CanConsumeIngredient(List<IngredientAmount> ingredientAmounts)
    {
        if (ingredientAmounts == null)
            return false;

        foreach (IngredientAmount ingredientAmount in ingredientAmounts)
        {
            if (ingredientAmount == null || ingredientAmount.amount < 0)
                return false;

            long requiredAmount = 0;

            foreach (IngredientAmount requestedAmount in ingredientAmounts)
            {
                if (requestedAmount != null
                    && requestedAmount.ingredient == ingredientAmount.ingredient)
                {
                    requiredAmount += requestedAmount.amount;
                }
            }

            if (CalculateIngredientAmount(ingredientAmount.ingredient) < requiredAmount)
                return false;
        }

        return true;
    }

    // 식재료 하나를 확인하고 실제로 사용할 때 사용.
    public bool TryConsumeIngredient(IngredientAmount ingredientAmount)
    {
        return TryConsumeIngredient(new List<IngredientAmount> { ingredientAmount });
    }

    // 여러 식재료를 확인하고 한 번에 사용할 때 사용.
    public bool TryConsumeIngredient(List<IngredientAmount> ingredientAmounts)
    {
        if (!CanConsumeIngredient(ingredientAmounts))
            return false;

        bool hasChanged = false;

        foreach (IngredientAmount ingredientAmount in ingredientAmounts)
        {
            int remainingAmount = ingredientAmount.amount;

            if (remainingAmount == 0)
                continue;

            hasChanged = true;

            foreach (IngredientAmount stockIngredientAmount in stockData.ingredients)
            {
                if (remainingAmount == 0)
                    break;

                if (stockIngredientAmount == null
                    || stockIngredientAmount.ingredient != ingredientAmount.ingredient)
                    continue;

                int consumableAmount = Mathf.Min(
                    Mathf.Max(0, stockIngredientAmount.amount),
                    remainingAmount);
                stockIngredientAmount.amount -= consumableAmount;
                remainingAmount -= consumableAmount;
            }
        }

        if (hasChanged)
            NotifyStockDataChanged();

        return true;
    }

    private int CalculateIngredientAmount(IngredientType ingredient)
    {
        long total = 0;

        foreach (IngredientAmount ingredientAmount in stockData.ingredients)
        {
            if (ingredientAmount != null && ingredientAmount.ingredient == ingredient)
                total += Mathf.Max(0, ingredientAmount.amount);
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

        foreach (IngredientAmount ingredientAmount in stockData.ingredients)
        {
            if (ingredientAmount == null)
                continue;

            saveData.ingredients.Add(new IngredientAmount(
                ingredientAmount.ingredient,
                ingredientAmount.amount));
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

            if (saveData.ingredients != null)
            {
                foreach (IngredientAmount ingredientAmount in saveData.ingredients)
                {
                    if (ingredientAmount == null
                        || (int)ingredientAmount.ingredient < 0
                        || (int)ingredientAmount.ingredient >= ((int)IngredientType.Cookie + 1))
                        continue;

                    stockData.ingredients.Add(new IngredientAmount(
                        ingredientAmount.ingredient,
                        Mathf.Max(0, ingredientAmount.amount)));
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
