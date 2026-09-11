using System.Collections;
using DG.Tweening;
using UnityEngine;

public class UI_Loading : MonoBehaviour
{
    [SerializeField] private GameObject solidCover;

    [Header("Loading Tween")]
    [SerializeField] private float openDuration = 0.25f;
    [SerializeField] private float closeDuration = 0.25f;
    [SerializeField] private Ease openEase = Ease.OutCubic;
    [SerializeField] private Ease closeEase = Ease.InCubic;

    private Tween currentTween;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        solidCover.SetActive(false);
        solidCover.transform.localScale = Vector3.zero;
    }

    public IEnumerator OpenLoading()
    {
        KillCurrentTween();
         
        solidCover.SetActive(true);
        solidCover.transform.localScale = Vector3.zero;

        currentTween = solidCover.transform
            .DOScale(Vector3.one, openDuration)
            .SetEase(openEase)
            .SetUpdate(true);

        yield return currentTween.WaitForCompletion();

        currentTween = null;
    }

    public IEnumerator CloseLoading()
    {
        KillCurrentTween();
         
        solidCover.transform.localScale = Vector3.one;

        currentTween = solidCover.transform
            .DOScale(Vector3.zero, closeDuration)
            .SetEase(closeEase)
            .SetUpdate(true);

        yield return currentTween.WaitForCompletion();

        solidCover.SetActive(false);
        currentTween = null;
    }

    private void KillCurrentTween()
    {
        currentTween?.Kill();
        currentTween = null;
    }

    private void OnDestroy()
    {
        KillCurrentTween();
    }
}