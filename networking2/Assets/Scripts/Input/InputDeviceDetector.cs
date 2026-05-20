using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class InputDeviceDetector : MonoBehaviour
{
    public static InputDeviceDetector Instance;

    public InputDeviceType CurrentDevice { get; private set; }

    public event Action<InputDeviceType> OnDeviceChanged;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void OnEnable()
    {
        InputSystem.onActionChange += OnActionChange;
    }

    private void OnDisable()
    {
        InputSystem.onActionChange -= OnActionChange;
    }

    private void OnActionChange(object obj, InputActionChange change)
    {
        if (change != InputActionChange.ActionPerformed)
            return;

        var action = obj as InputAction;
        if (action?.activeControl == null)
            return;

        var device = action.activeControl.device;

        InputDeviceType newDevice = GetDeviceType(device);

        if (newDevice != CurrentDevice)
        {
            CurrentDevice = newDevice;
            OnDeviceChanged?.Invoke(CurrentDevice);
        }
    }

    private InputDeviceType GetDeviceType(InputDevice device)
    {
        if (device is Keyboard || device is Mouse)
            return InputDeviceType.KeyboardMouse;

        if (device is Gamepad gamepad)
        {
            string layout = gamepad.layout.ToLower();

            if (layout.Contains("dualshock") || layout.Contains("dualsense"))
                return InputDeviceType.PlayStation;

            return InputDeviceType.Xbox;
        }

        return InputDeviceType.KeyboardMouse;
    }
}
