using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerMover playerMover;
    [SerializeField] private PlayerMiner playerMiner;
    [SerializeField] private CameraController cameraController;

    public PlayerMover PlayerMover => playerMover;
    public PlayerMiner PlayerMiner => playerMiner;
    public CameraController CameraController => cameraController;

    private void Awake()
    {
        if (playerMover == null)
        {
            playerMover = GetComponent<PlayerMover>();
        }

        if (playerMiner == null)
        {
            playerMiner = GetComponent<PlayerMiner>();
        }

        if (cameraController == null)
        {
            cameraController = GetComponent<CameraController>();
        }
    }
}
