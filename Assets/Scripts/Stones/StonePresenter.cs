using System;
using DG.Tweening;
using UnityEngine;

public sealed class StonePresenter : MonoBehaviour
{
    [SerializeField] private float spawnDuration = 0.2f;
    [SerializeField] private float hitDuration = 0.12f;
    [SerializeField] private float hitStrength = 0.08f;
    [SerializeField] private float breakDuration = 0.2f;

    private Transform solidObject;
    private Vector3 defaultLocalPosition;
    private Vector3 defaultLocalScale;
    private Tween currentTween;

    public void Initialize(Transform model)
    {
        StopCurrentTween();
        solidObject = model;
        defaultLocalPosition = model.localPosition;
        defaultLocalScale = model.localScale;
    }

    public void PlaySpawnTween()
    {
        StopCurrentTween();
        solidObject.localPosition = defaultLocalPosition;
        solidObject.localScale = Vector3.zero;
        currentTween = solidObject.DOScale(defaultLocalScale, spawnDuration)
            .SetEase(Ease.OutBack);
    }

    public void PlayHitReactionTween()
    {
        StopCurrentTween();
        solidObject.localPosition = defaultLocalPosition;
        solidObject.localScale = defaultLocalScale;
        currentTween = solidObject.DOShakePosition(hitDuration, hitStrength, 10)
            .OnComplete(() => solidObject.localPosition = defaultLocalPosition);
    }

    public void PlayBreakTween(Action onComplete)
    {
        StopCurrentTween();
        solidObject.localPosition = defaultLocalPosition;
        solidObject.localScale = defaultLocalScale;
        currentTween = solidObject.DOScale(Vector3.zero, breakDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() => onComplete());
    }

    public void StopCurrentTween()
    {
        if (currentTween != null)
        {
            currentTween.Kill();
            currentTween = null;
        }
    }

    private void OnDestroy()
    {
        StopCurrentTween();
    }
}
