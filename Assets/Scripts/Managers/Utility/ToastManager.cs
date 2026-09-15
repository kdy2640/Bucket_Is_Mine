using System.Collections;
using UnityEngine;

public sealed class ToastManager : MonoBehaviour
{
    [SerializeField] private UI_ToastCanvas toastCanvasPrefab;
    [SerializeField] private float displayDuration = 2f;

    private UI_ToastCanvas toastCanvas;
    private Coroutine hideCoroutine;

    private void Awake()
    {
        EnsureCanvas();
    }

    public void Show(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || !EnsureCanvas())
            return;

        toastCanvas.Show(message);

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private bool EnsureCanvas()
    {
        if (toastCanvas != null)
            return true;

        toastCanvas = FindFirstObjectByType<UI_ToastCanvas>(FindObjectsInactive.Include);

        if (toastCanvas == null)
        {
            if (toastCanvasPrefab == null)
            {
                Debug.LogError("[ToastManager] Toast Canvas prefab is not assigned.", this);
                return false;
            }

            toastCanvas = Instantiate(toastCanvasPrefab);
        }

        DontDestroyOnLoad(toastCanvas.gameObject);
        toastCanvas.Hide();
        return true;
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(displayDuration);

        toastCanvas.Hide();
        hideCoroutine = null;
    }
}
