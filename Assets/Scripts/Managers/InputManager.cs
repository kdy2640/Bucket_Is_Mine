using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public enum InputEvent
{
    Move,
    RotationCamera,
    RightMouseClick,
    LeftMouseClick,
    ChangeEditMode,
    MoveMouse,
    Jump,
    ChangeShadingMode
}



[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInput))]
public sealed class InputManager : MonoBehaviour
{
    private readonly Dictionary<Enum, Action<InputAction.CallbackContext>> inputEvents = new();

    private PlayerInput playerInput;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

        if (playerInput != null)
        {
            playerInput.onActionTriggered += ProvideInputEvent;
        }
    }

    public void Subscribe(Enum inputEvent, Action<InputAction.CallbackContext> listener)
    {
        if (inputEvent == null || listener == null)
        {
            return;
        }

        if (inputEvents.TryGetValue(inputEvent, out Action<InputAction.CallbackContext> currentListeners))
        {
            inputEvents[inputEvent] = currentListeners + listener;
            return;
        }

        inputEvents.Add(inputEvent, listener);
    }

    public void Unsubscribe(Enum inputEvent, Action<InputAction.CallbackContext> listener)
    {
        if (inputEvent == null || listener == null ||
            !inputEvents.TryGetValue(inputEvent, out Action<InputAction.CallbackContext> currentListeners))
        {
            return;
        }

        currentListeners -= listener;

        if (currentListeners == null)
        {
            inputEvents.Remove(inputEvent);
            return;
        }

        inputEvents[inputEvent] = currentListeners;
    }

    private void ProvideInputEvent(InputAction.CallbackContext context)
    {
        if (!Enum.TryParse(context.action.name, out InputEvent inputEvent))
        {
            return;
        }

        if (inputEvents.TryGetValue(inputEvent, out Action<InputAction.CallbackContext> listeners))
        {
            listeners?.Invoke(context);
        }
    }

    private void OnDestroy()
    {
        ReleasePlayerInput();
        inputEvents.Clear();
    }

    private void ReleasePlayerInput()
    {
        if (playerInput != null)
        {
            playerInput.onActionTriggered -= ProvideInputEvent;
            playerInput = null;
        }
    }
}
