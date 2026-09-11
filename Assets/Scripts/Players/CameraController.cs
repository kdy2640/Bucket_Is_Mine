using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
public sealed class CameraController : MonoBehaviour
{
    [SerializeField] private float horizontalRotationOffset = 0.05f;
    [SerializeField] private float verticalRotationOffset = 0.02f;
    [SerializeField, Range(-89f, 0f)] private float minimumVerticalAngle = -80f;
    [SerializeField, Range(0f, 89f)] private float maximumVerticalAngle = 80f;

    private Transform cameraHolder;
    private InputManager inputManager;
    private bool isCursorMode;
    private bool isInputSubscribed;
    private float verticalAngle;

    private void Start()
    {
        Camera mainCamera = Camera.main;
        cameraHolder = mainCamera != null ? mainCamera.transform.parent : null;

        if (cameraHolder != null)
        {
            verticalAngle = NormalizeAngle(cameraHolder.localEulerAngles.x);
            verticalAngle = Mathf.Clamp(verticalAngle, minimumVerticalAngle, maximumVerticalAngle);
        }

        SubscribeInputEvents();
    }

    private void OnEnable()
    {
        SubscribeInputEvents();
    }

    private void OnDisable()
    {
        UnsubscribeInputEvents();
    }

    public void SetMiningMode(bool enabled)
    {
        isCursorMode = !enabled;
    }

    private void OnRotationCamera(InputAction.CallbackContext context)
    {
        if (isCursorMode || cameraHolder == null)
        {
            return;
        }

        Vector2 input = context.ReadValue<Vector2>();
        float horizontal = input.x * horizontalRotationOffset;
        float vertical = input.y * verticalRotationOffset;
        Vector3 currentRotation = cameraHolder.localEulerAngles;
        verticalAngle = Mathf.Clamp(
            verticalAngle - vertical,
            minimumVerticalAngle,
            maximumVerticalAngle);

        cameraHolder.localRotation = Quaternion.Euler(
            verticalAngle,
            currentRotation.y + horizontal,
            0f);
    }

    private static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }

    private void SubscribeInputEvents()
    {
        if (isInputSubscribed || GameManager.Instance == null)
        {
            return;
        }

        inputManager = GameManager.Instance.InputManager;
        if (inputManager == null)
        {
            return;
        }

        inputManager.Subscribe(InputEvent.RotationCamera, OnRotationCamera);
        isInputSubscribed = true;
    }

    private void UnsubscribeInputEvents()
    {
        if (!isInputSubscribed || inputManager == null)
        {
            return;
        }

        inputManager.Unsubscribe(InputEvent.RotationCamera, OnRotationCamera);
        inputManager = null;
        isInputSubscribed = false;
    }
}
