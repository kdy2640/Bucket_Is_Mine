using UnityEngine;

public sealed class UtilityManager : MonoBehaviour
{
    public AudioManager Audio { get; private set; }
    public TutorialManager Tutorial { get; private set; }
    public SaveManager Save { get; private set; }
    public ToastManager Toast { get; private set; }

    private void Awake()
    {
        Audio = GetComponentInChildren<AudioManager>(true);
        Tutorial = GetComponentInChildren<TutorialManager>(true);
        Save = GetComponentInChildren<SaveManager>(true);
        Toast = GetComponentInChildren<ToastManager>(true);
    }
}
