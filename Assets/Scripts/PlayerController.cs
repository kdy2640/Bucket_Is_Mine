using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    GameObject CameraHolder;
    Rigidbody rigid;
    bool isLeftMouseHolding = false;
    bool isRightMouseHolding = false;
    bool isSelectMode = false;
    double theta = Math.PI * 1.5;

    [SerializeField]
    MarchingCubes mc;
    [SerializeField]
    public GameObject Anchor;
    [SerializeField]
    double horizontalRotationOffset = 0.05;
    [SerializeField]
    double verticalRotationOffset = 0.02;
    [SerializeField]
    double speedOffset = 10;
    [SerializeField]
    float anchorRadius = 1f;
    [SerializeField]
    float editPower = 0.3f;
    


    private Vector2 inputDirection;
    Vector2 mousePosition = Vector2.zero;
    private Vector3 mouseHit = Vector3.zero;


    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            transform.position += Vector3.up * 3f;
            rigid.velocity = Vector3.zero;
        }
    }
    public void OnMove(InputAction.CallbackContext context)
    {
        if (context.performed || context.started)
        {
            inputDirection = context.ReadValue<Vector2>();
        }
        else if (context.canceled)
        {
            inputDirection = Vector2.zero;
        }
    }
    public void OnRotationCamera(InputAction.CallbackContext context)
    {
        if (!isSelectMode)
        {
            Vector2 input = context.ReadValue<Vector2>();

            float diffHorizontal = input.x * (float)horizontalRotationOffset;
            float diffVertical = input.y * (float)verticalRotationOffset;
             

            Vector3 nowRot = CameraHolder.transform.localEulerAngles;
            CameraHolder.transform.localRotation = Quaternion.Euler(nowRot.x - diffVertical, nowRot.y + diffHorizontal, 0f);
        }
    }
    public void OnLeftMouseHandle(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isLeftMouseHolding = true;
        }
        else if (context.performed)
        {   
            mc.AddHeightSphere(mouseHit, anchorRadius, editPower);
        }
        else if (context.canceled)
        {
            isLeftMouseHolding = false;
        }
    }
    public void OnRightMouseHandle(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isRightMouseHolding = true;
        }
        else if (context.performed)
        { 
            mc.AddHeightSphere(mouseHit, anchorRadius, -editPower);
        }
        else if (context.canceled)
        {
            isRightMouseHolding = false;
        } 
    }

    public void OnChageEditMode(InputAction.CallbackContext context)
    { 
        if (context.performed)
        { 
            isSelectMode = !isSelectMode;
            SetTerrarianMode(isSelectMode);
        }
    }
    public void OnChageShadingMode(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            mc.isSmoothShading = !mc.isSmoothShading;
            mc.MarchCubes();
        }
    }
    public void OnMouseMove(InputAction.CallbackContext context)
    {
        if (isSelectMode)
        {
            Vector2 input = context.ReadValue<Vector2>();

            CastRay(input);
        }
    }


    private void SetTerrarianMode(bool SelectMode)
    {
        if (SelectMode)
        { 
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {     
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Anchor.transform.position += Vector3.down * 100;
        } 
    }
    private void CastRay(Vector2 MousePosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(MousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f,LayerMask.GetMask("Plane")))
        { 

            if (hit.collider.CompareTag("Plane"))
            {
                if(mc == null)
                {
                    mc = hit.transform.gameObject.GetComponentInParent<MarchingCubes>();
                }
                Anchor.transform.position = hit.point;
                Anchor.transform.localScale = Vector3.one * anchorRadius;
                mouseHit = hit.point;
            } 
        }
        else
        { 
        }
    }


    private void Start()
    {
        CameraHolder = Camera.main.transform.parent.gameObject;
        rigid = GetComponent<Rigidbody>();
        SetTerrarianMode(isSelectMode);
    }
    private void Update()
    {
        if (inputDirection != Vector2.zero)
        {
            float diffRight = inputDirection.x * (float)speedOffset;
            float diffFront = inputDirection.y * (float)speedOffset;

            Vector3 Front = Vector3.ProjectOnPlane(CameraHolder.transform.forward, transform.up).normalized;
            Vector3 Right = CameraHolder.transform.right.normalized;

            transform.Translate((Front * diffFront + Right * diffRight) * Time.deltaTime);
        }
    }


}
