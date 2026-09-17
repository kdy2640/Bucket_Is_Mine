using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/StoneDataSO")]
public sealed class StoneDataSO : ScriptableObject
{
    [SerializeField] private int id;
    [SerializeField] private int tier;
    [SerializeField, Min(0.01f)] private float maxHealth;
    [SerializeField] private GameObject modelPrefab;
    [SerializeField] private List<OreAmount> rewardList = new();

    public int Id => id;
    public int Tier => tier;
    public float MaxHealth => maxHealth;
    public GameObject ModelPrefab => modelPrefab;
    public IReadOnlyList<OreAmount> RewardList => rewardList;
}
