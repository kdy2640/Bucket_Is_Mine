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

    public void SetGameplayActive(bool active)
    {
        playerMover.enabled = active;
        playerMiner.enabled = active;
        cameraController.enabled = active;
        Rigidbody body = GetComponent<Rigidbody>();
        if (!active && !body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        body.isKinematic = !active;
    }

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
