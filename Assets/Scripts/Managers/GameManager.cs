using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private InputManager inputManager;
    [SerializeField] private GameLoopManager gameLoopManager;
    [SerializeField] private SceneController sceneController;

    public InputManager InputManager => inputManager;
    public GameLoopManager GameLoopManager => gameLoopManager;
    public SceneController SceneController => sceneController;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (inputManager == null)
        {
            inputManager = GetComponent<InputManager>();
        }

        if (gameLoopManager == null)
        {
            gameLoopManager = GetComponent<GameLoopManager>();
        }

        if (sceneController == null)
        {
            sceneController = GetComponent<SceneController>();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
