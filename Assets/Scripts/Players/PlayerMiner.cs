using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
public sealed class PlayerMiner : MonoBehaviour
{
    bool isMiningMode = true;
     
    [SerializeField]
    [FormerlySerializedAs("Anchor")]
    private GameObject anchor;
    [SerializeField]
    float anchorRadius = 1f;
    [SerializeField]
    float editPower = 0.3f;
    [SerializeField, Min(0.01f)]
    float editInterval = 0.1f;

    [SerializeField] private TerrainManager terrainManager;
    InputManager inputManager;

    float nextLeftEditTime;
    float nextRightEditTime;
    bool isLeftMouseHolding;
    bool isRightMouseHolding;
    PlayerController playerController;
    CameraController cameraController;
    bool isInputSubscribed;

    private Vector3 mouseHit = Vector3.zero;

    private void OnChangeEditMode(InputAction.CallbackContext context)
    { 
        if (context.performed)
        { 
            isMiningMode = !isMiningMode;
            SetMiningMode(isMiningMode);
        }
    }
    private void OnChangeShadingMode(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            terrainManager.IsSmoothShading = !terrainManager.IsSmoothShading;
            terrainManager.RegenerateAllChunks();
        }
    }

    private void OnLeftMouse(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isLeftMouseHolding = true;
            nextLeftEditTime = 0f;
            if (!isMiningMode || terrainManager == null || cameraController == null) return;

            UpdateMiningTarget();
            if (!anchor.activeSelf) return;

            terrainManager.AddDensitySphere(mouseHit, anchorRadius, -editPower);
            nextLeftEditTime = Time.time + Mathf.Max(0.01f, editInterval);
        }
        else if (context.canceled)
        {
            isLeftMouseHolding = false;
        }
    }

    private void OnRightMouse(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isRightMouseHolding = true;
            nextRightEditTime = 0f;
            if (!isMiningMode || terrainManager == null || cameraController == null) return;

            UpdateMiningTarget();
            if (!anchor.activeSelf) return;

            terrainManager.AddDensitySphere(mouseHit, anchorRadius, editPower);
            nextRightEditTime = Time.time + Mathf.Max(0.01f, editInterval);
        }
        else if (context.canceled)
        {
            isRightMouseHolding = false;
        }
    }

    private void SetMiningMode(bool enabled)
    {
        cameraController?.SetMiningMode(enabled);

        if (enabled)
        { 
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            UpdateMiningTarget();
        }
        else
        {
            isLeftMouseHolding = false;
            isRightMouseHolding = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            anchor.SetActive(false);
        }
    }

    private void UpdateMiningTarget()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            anchor.SetActive(false);
            return;
        }

        Vector2 screenCenter = new(Screen.width * 0.5f, Screen.height * 0.5f);
        Ray ray = mainCamera.ScreenPointToRay(screenCenter);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Plane")) &&
            hit.collider.CompareTag("Plane"))
        { 

            anchor.SetActive(true);
            anchor.transform.position = hit.point;
            anchor.transform.localScale = Vector3.one * (anchorRadius * 2f);
            mouseHit = hit.point;
            return;
        }

        anchor.SetActive(false);
    }


    private void Start()
    {
        inputManager = GameManager.Instance.InputManager;

        playerController = GetComponent<PlayerController>();
        cameraController = playerController.CameraController;
        SubscribeInputEvents();
        SetMiningMode(isMiningMode);
    }

    private void OnEnable()
    {
        SubscribeInputEvents();
    }

    private void OnDisable()
    {
        UnsubscribeInputEvents();
        isLeftMouseHolding = false;
        isRightMouseHolding = false;
    }

    private void SubscribeInputEvents()
    {
        if (isInputSubscribed || GameManager.Instance == null)
        {
            return;
        }

        if (inputManager == null)
        {
            inputManager = GameManager.Instance.InputManager;
        }

        if (inputManager == null)
        {
            return;
        }

        inputManager.Subscribe(InputEvent.ChangeEditMode, OnChangeEditMode);
        inputManager.Subscribe(InputEvent.ChangeShadingMode, OnChangeShadingMode);
        inputManager.Subscribe(InputEvent.LeftMouseClick, OnLeftMouse);
        inputManager.Subscribe(InputEvent.RightMouseClick, OnRightMouse);
        isInputSubscribed = true;
    }

    private void UnsubscribeInputEvents()
    {
        if (!isInputSubscribed || inputManager == null)
        {
            return;
        }

        inputManager.Unsubscribe(InputEvent.ChangeEditMode, OnChangeEditMode);
        inputManager.Unsubscribe(InputEvent.ChangeShadingMode, OnChangeShadingMode);
        inputManager.Unsubscribe(InputEvent.LeftMouseClick, OnLeftMouse);
        inputManager.Unsubscribe(InputEvent.RightMouseClick, OnRightMouse);
        inputManager = null;
        isInputSubscribed = false;
    }
    private void Update()
    {
        if (isMiningMode)
        {
            UpdateMiningTarget();
        }

        UpdateTerrainEditing();
    }

    private void UpdateTerrainEditing()
    {
        if (!isMiningMode || terrainManager == null || cameraController == null || !anchor.activeSelf)
        {
            return;
        }

        float interval = Mathf.Max(0.01f, editInterval);

        if (isLeftMouseHolding)
        {
            if (Time.time >= nextLeftEditTime)
            {
                terrainManager.AddDensitySphere(mouseHit, anchorRadius, -editPower);
                nextLeftEditTime = Time.time + interval;
            }
        }
        else
        {
            nextLeftEditTime = 0f;
        }

        if (isRightMouseHolding)
        {
            if (Time.time >= nextRightEditTime)
            {
                terrainManager.AddDensitySphere(mouseHit, anchorRadius, editPower);
                nextRightEditTime = Time.time + interval;
            }
        }
        else
        {
            nextRightEditTime = 0f;
        }
    }
}
