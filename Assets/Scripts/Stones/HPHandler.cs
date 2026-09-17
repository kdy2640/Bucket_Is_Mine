using System;
using UnityEngine;

public sealed class HPHandler : MonoBehaviour
{
    private Action<float> onHPUpdate;
    private Action onDied;

    public float MaxHp { get; private set; }
    public float NowHp { get; private set; }
    public bool IsDead => NowHp <= 0f;

    public void SetMaxHealth(float maxValue)
    {
        MaxHp = maxValue;
        NowHp = maxValue;
        onHPUpdate?.Invoke(NowHp);
    }

    public void TakeHeal(float heal)
    {
        if (IsDead)
            return;

        NowHp = Mathf.Min(NowHp + heal, MaxHp);
        onHPUpdate?.Invoke(NowHp);
    }

    public void TakeDamage(float damage)
    {
        if (IsDead)
            return;

        NowHp = Mathf.Max(NowHp - damage, 0f);
        onHPUpdate?.Invoke(NowHp);

        if (IsDead)
            onDied?.Invoke();
    }

    public void SubscribeHPUpdate(Action<float> callback)
    {
        onHPUpdate += callback;
    }

    public void UnSubscribeHPUpdate(Action<float> callback)
    {
        onHPUpdate -= callback;
    }

    public void SubscribeDying(Action callback)
    {
        onDied += callback;
    }

    public void UnSubscribeDying(Action callback)
    {
        onDied -= callback;
    }
}
