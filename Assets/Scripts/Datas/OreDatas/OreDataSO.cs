using UnityEngine;

[CreateAssetMenu(menuName = "Game/OreDataSO")]
public sealed class OreDataSO : ScriptableObject
{
    [SerializeField] private int id;
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private int tier;
    [SerializeField] private Color color;

    public int Id => id;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public int Tier => tier;
    public Color Color => color;
}
