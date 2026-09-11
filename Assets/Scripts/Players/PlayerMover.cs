using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public sealed class PlayerMover : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField, Min(0f)] private float groundAcceleration = 50f;
    [SerializeField, Min(0f)] private float airAcceleration = 15f;
    [SerializeField, Min(0f)]
    [FormerlySerializedAs("jumpHeight")]
    private float jumpVelocity = 6f;
    [SerializeField, Range(0f, 1f)] private float minimumGroundNormalY = 0.6f;

    private Rigidbody rigid;
    private Transform cameraHolder;
    private InputManager inputManager;
    private Vector2 inputDirection;
    private readonly HashSet<Collider> groundContacts = new();
    private bool jumpRequested;
    private bool isInputSubscribed;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        rigid.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Start()
    {
        Camera mainCamera = Camera.main;
        cameraHolder = mainCamera != null ? mainCamera.transform.parent : null;
        SubscribeInputEvents();
    }

    private void OnEnable()
    {
        SubscribeInputEvents();
    }

    private void OnDisable()
    {
        UnsubscribeInputEvents();
        inputDirection = Vector2.zero;
        jumpRequested = false;
        groundContacts.Clear();
    }

    private void FixedUpdate()
    {
        groundContacts.RemoveWhere(collider => collider == null);

        Vector3 up = transform.up;
        Vector3 targetVelocity = GetMoveDirection(up) * speed;
        Vector3 currentVelocity = rigid.linearVelocity;
        Vector3 verticalVelocity = Vector3.Project(currentVelocity, up);
        Vector3 horizontalVelocity = currentVelocity - verticalVelocity;
        float acceleration = groundContacts.Count > 0 ? groundAcceleration : airAcceleration;

        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            targetVelocity,
            acceleration * Time.fixedDeltaTime);

        rigid.linearVelocity = horizontalVelocity + verticalVelocity;

        if (!jumpRequested)
        {
            return;
        }

        jumpRequested = false;
        if (groundContacts.Count == 0)
        {
            return;
        }

        float currentVerticalSpeed = Vector3.Dot(rigid.linearVelocity, up);
        rigid.linearVelocity += up * (jumpVelocity - currentVerticalSpeed);
        groundContacts.Clear();
    }

    private Vector3 GetMoveDirection(Vector3 up)
    {
        if (inputDirection == Vector2.zero || cameraHolder == null)
        {
            return Vector3.zero;
        }

        Vector3 front = Vector3.ProjectOnPlane(cameraHolder.forward, up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cameraHolder.right, up).normalized;
        Vector3 direction = front * inputDirection.y + right * inputDirection.x;
        return Vector3.ClampMagnitude(direction, 1f);
    }

    private void OnMove(InputAction.CallbackContext context)
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

    private void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            jumpRequested = true;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        UpdateGroundContact(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        UpdateGroundContact(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        groundContacts.Remove(collision.collider);
    }

    private void UpdateGroundContact(Collision collision)
    {
        Vector3 up = transform.up;

        for (int i = 0; i < collision.contactCount; i++)
        {
            if (Vector3.Dot(collision.GetContact(i).normal, up) >= minimumGroundNormalY)
            {
                groundContacts.Add(collision.collider);
                return;
            }
        }

        groundContacts.Remove(collision.collider);
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

        inputManager.Subscribe(InputEvent.Move, OnMove);
        inputManager.Subscribe(InputEvent.Jump, OnJump);
        isInputSubscribed = true;
    }

    private void UnsubscribeInputEvents()
    {
        if (!isInputSubscribed || inputManager == null)
        {
            return;
        }

        inputManager.Unsubscribe(InputEvent.Move, OnMove);
        inputManager.Unsubscribe(InputEvent.Jump, OnJump);
        inputManager = null;
        isInputSubscribed = false;
    }
}
