using TMPro;
using UnityEngine;

public sealed class UI_ToastCanvas : MonoBehaviour
{
    [SerializeField] private CanvasGroup toastGroup;
    [SerializeField] private TMP_Text messageText;

    public void Show(string message)
    {
        messageText.text = message;
        toastGroup.alpha = 1f;
    }

    public void Hide()
    {
        toastGroup.alpha = 0f;
        toastGroup.interactable = false;
        toastGroup.blocksRaycasts = false;
    }
}
