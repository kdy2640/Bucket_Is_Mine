using System;
using System.Collections.Generic;
using UnityEngine;

public enum IngredientType
{
    Vanilla = 0,
    Cherry = 1,
    Chocolate = 2,
    Almond = 3,
    Strawberry = 4,
    Cookie = 5
}

[Serializable]
public class IngredientAmount
{
    public IngredientType ingredient;
    [Min(0)] public int amount;

    public IngredientAmount()
    {
    }

    // 코드에서 식재료 수량을 만들 때 사용.
    public IngredientAmount(IngredientType ingredient, int amount)
    {
        this.ingredient = ingredient;
        this.amount = amount;
    }
}

public interface IReadableStockData
{
    // 현재 재화를 표시할 때 사용.
    int Currency { get; }

    // 현재 식재료 재고를 표시할 때 사용.
    IReadOnlyList<IngredientAmount> Ingredients { get; }

}

[Serializable]
public class StockData : IReadableStockData
{
    [Min(0)] public int currency;
    public List<IngredientAmount> ingredients = new();

    public int Currency => currency;
    public IReadOnlyList<IngredientAmount> Ingredients => ingredients;
}
