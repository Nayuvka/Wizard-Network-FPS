using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class ControlSchemeUI : MonoBehaviour
{
    public enum ControlScheme
    {
        KeyboardMouse,
        Xbox,
        PlayStation
    }

    [Header("UI Groups")]
    [SerializeField] private GameObject keyboardMouseUI;
    [SerializeField] private GameObject xboxUI;
    [SerializeField] private GameObject playStationUI;

    [Header("Settings")]
    [SerializeField] private bool detectOnStart = true;
    [SerializeField] private ControlScheme defaultScheme = ControlScheme.KeyboardMouse;

    public ControlScheme CurrentScheme { get; private set; }

    private bool hasInitializedScheme;

    private void OnEnable()
    {
        InputSystem.onEvent += OnInputEvent;
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void Start()
    {
        if (detectOnStart)
        {
            DetectCurrentDevice();
        }
        else
        {
            ForceSetScheme(defaultScheme);
        }
    }

    private void OnDisable()
    {
        InputSystem.onEvent -= OnInputEvent;
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
    {
        if (device == null)
            return;

        if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
            return;

        if (HasAnyGamepadConnected())
        {
            if (device is Gamepad gamepad)
            {
                DetectGamepadScheme(gamepad);
            }

            return;
        }

  
        if (device is Keyboard || device is Mouse)
        {
            SetScheme(ControlScheme.KeyboardMouse);
        }
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device == null)
            return;

        switch (change)
        {
            case InputDeviceChange.Added:
            case InputDeviceChange.Reconnected:
            case InputDeviceChange.Removed:
            case InputDeviceChange.Disconnected:
                DetectCurrentDevice();
                break;
        }
    }

    private void DetectCurrentDevice()
    {
        Gamepad connectedGamepad = GetConnectedGamepad();

        if (connectedGamepad != null)
        {
            DetectGamepadScheme(connectedGamepad);
            return;
        }

        ForceSetScheme(ControlScheme.KeyboardMouse);
    }

    private Gamepad GetConnectedGamepad()
    {
        foreach (Gamepad gamepad in Gamepad.all)
        {
            if (gamepad != null && gamepad.added)
            {
                return gamepad;
            }
        }

        return null;
    }

    private bool HasAnyGamepadConnected()
    {
        return GetConnectedGamepad() != null;
    }

    private void DetectGamepadScheme(Gamepad gamepad)
    {
        if (gamepad == null)
            return;

        string deviceName = "";
        string displayName = "";

        if (!string.IsNullOrEmpty(gamepad.name))
            deviceName = gamepad.name.ToLower();

        if (!string.IsNullOrEmpty(gamepad.displayName))
            displayName = gamepad.displayName.ToLower();

        string combinedName = deviceName + " " + displayName;

        if (
            combinedName.Contains("dualshock") ||
            combinedName.Contains("dualsense") ||
            combinedName.Contains("playstation") ||
            combinedName.Contains("ps4") ||
            combinedName.Contains("ps5") ||
            combinedName.Contains("sony")
        )
        {
            SetScheme(ControlScheme.PlayStation);
        }
        else
        {
            SetScheme(ControlScheme.Xbox);
        }
    }

    public void SetScheme(ControlScheme newScheme)
    {
        if (hasInitializedScheme && CurrentScheme == newScheme)
            return;

        CurrentScheme = newScheme;
        hasInitializedScheme = true;
        UpdateUI();
    }

    private void ForceSetScheme(ControlScheme newScheme)
    {
        CurrentScheme = newScheme;
        hasInitializedScheme = true;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (keyboardMouseUI != null)
            keyboardMouseUI.SetActive(CurrentScheme == ControlScheme.KeyboardMouse);

        if (xboxUI != null)
            xboxUI.SetActive(CurrentScheme == ControlScheme.Xbox);

        if (playStationUI != null)
            playStationUI.SetActive(CurrentScheme == ControlScheme.PlayStation);
    }

    public void ShowKeyboard()
    {
        ForceSetScheme(ControlScheme.KeyboardMouse);
    }

    public void ShowXbox()
    {
        ForceSetScheme(ControlScheme.Xbox);
    }

    public void ShowPlayStation()
    {
        ForceSetScheme(ControlScheme.PlayStation);
    }
}