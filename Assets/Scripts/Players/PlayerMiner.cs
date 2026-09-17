using System.Collections.Generic;
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
    [SerializeField, Min(0.01f)]
    float erosionDirectionBlendTime = 0.5f;
    [SerializeField, Range(0f, 1f)]
    float erosionSideStrength = 0.2f;
    [SerializeField, Tooltip("굴착 시 브러시 중심에서 멀어질수록 강도를 줄입니다. 끄면 반경 안의 거리 가중치를 1로 사용합니다.")]
    bool useErosionDistanceFalloff = true;

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
    private Vector3 mouseHitNormal;
    private Vector3 viewDirection;
    private Vector3 erosionStartDirection;
    private float erosionElapsedTime;
    private bool hasErosionStart;
    private readonly HashSet<StoneActor> damagedStones = new();

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
            hasErosionStart = false;
            erosionElapsedTime = 0f;
            if (!isMiningMode || terrainManager == null || cameraController == null) return;

            UpdateMiningTarget();
            if (!anchor.activeSelf) return;

            UpdateTerrainEditing();
        }
        else if (context.canceled)
        {
            isLeftMouseHolding = false;
            hasErosionStart = false;
            erosionElapsedTime = 0f;
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

            terrainManager.AddDensitySphere(mouseHit, anchorRadius, editPower, Vector3.zero, 1f, false);
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
            hasErosionStart = false;
            erosionElapsedTime = 0f;
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
            hasErosionStart = false;
            erosionElapsedTime = 0f;
            return;
        }

        Vector2 screenCenter = new(Screen.width * 0.5f, Screen.height * 0.5f);
        Ray ray = mainCamera.ScreenPointToRay(screenCenter);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f,
            LayerMask.GetMask("Plane", "Stone"), QueryTriggerInteraction.Collide))
        { 

            anchor.SetActive(true);
            anchor.transform.position = hit.point;
            anchor.transform.localScale = Vector3.one * (anchorRadius * 2f);
            mouseHit = hit.point;
            mouseHitNormal = hit.normal;
            viewDirection = ray.direction;
            return;
        }

        anchor.SetActive(false);
        hasErosionStart = false;
        erosionElapsedTime = 0f;
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
        hasErosionStart = false;
        erosionElapsedTime = 0f;
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
            hasErosionStart = false;
            erosionElapsedTime = 0f;
            return;
        }

        float interval = Mathf.Max(0.01f, editInterval);

        if (isLeftMouseHolding)
        {
            if (Time.time >= nextLeftEditTime)
            {
                if (!hasErosionStart)
                {
                    erosionStartDirection = -mouseHitNormal;
                    hasErosionStart = true;
                }

                float blend = Mathf.SmoothStep(0f, 1f, erosionElapsedTime / erosionDirectionBlendTime);
                Vector3 erosionDirection = Vector3.Slerp(erosionStartDirection, viewDirection, blend);
                if (terrainManager.AddDensitySphere(
                    mouseHit, anchorRadius, -editPower, erosionDirection, erosionSideStrength, useErosionDistanceFalloff))
                {
                    // 실제 밀도가 바뀐 pass의 굴착 시간만 누적한다.
                    erosionElapsedTime = Mathf.Min(erosionElapsedTime + interval, erosionDirectionBlendTime);
                }
                DamageStones();
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
                terrainManager.AddDensitySphere(mouseHit, anchorRadius, editPower, Vector3.zero, 1f, false);
                nextRightEditTime = Time.time + interval;
            }
        }
        else
        {
            nextRightEditTime = 0f;
        }
    }

    private void DamageStones()
    {
        damagedStones.Clear();
        Collider[] hits = Physics.OverlapSphere(
            mouseHit, anchorRadius, LayerMask.GetMask("Stone"), QueryTriggerInteraction.Collide);

        foreach (Collider hit in hits)
        {
            StoneActor stone = hit.GetComponentInParent<StoneActor>();
            if (damagedStones.Add(stone))
                stone.TakeDamage(100f);
        }
    }
}
