using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class OreAmount
{
    public int oreId;
    [Min(0)] public int amount;

    public OreAmount()
    {
    }

    // 코드에서 광석 수량을 만들 때 사용.
    public OreAmount(int oreId, int amount)
    {
        this.oreId = oreId;
        this.amount = amount;
    }
}

public interface IReadableStockData
{
    // 현재 재화를 표시할 때 사용.
    int Currency { get; }

    // 현재 광석 재고를 표시할 때 사용.
    IReadOnlyList<OreAmount> Ores { get; }

}

[Serializable]
public class StockData : IReadableStockData
{
    [Min(0)] public int currency;
    public List<OreAmount> ores = new();

    public int Currency => currency;
    public IReadOnlyList<OreAmount> Ores => ores;
}
