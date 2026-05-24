using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class MainMenuUI : MonoBehaviour
{
    [Header("References")]
    [Space(5)]
    [SerializeField] private PlayerControls playerControls;

    [Header("Menu Panels")]
    [Space(5)]
    [SerializeField] private GameObject startGamePanel;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject createGamePanel;
    [SerializeField] private GameObject findGamePanel;
    [SerializeField] private GameObject roomCodePanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Canvas Groups")]
    [Space(5)]
    [SerializeField] private CanvasGroup startCanvasGroup;

    [SerializeField] private CanvasGroup menuCanvasGroup;

    [Header("Menu Settings")]
    [Space(5)]
    [SerializeField] private float transitionDuration = 1f;

    [Header("First Selected Objects")]
    [Space(5)]
    [SerializeField] private GameObject mainMenuFirstSelected;
    [SerializeField] private GameObject networkFirstSelected;
    [SerializeField] private GameObject settingsFirstSelected;

    private GameObject lastSelectedBeforeSubMenu;
    private Coroutine selectCoroutine;

    [Header("Input")]
    [Space(5)]
    [SerializeField] private InputActionReference startInput;
    [SerializeField] private InputActionReference backInput;

    public TextMeshProUGUI startText;
    public float startFlashSpeed = 0.5f;
    private Coroutine flashCoroutine;

    [Header("Camera Transition")]
    [Space(5)]
    [SerializeField] private Transform menuCamera;
    [SerializeField] private Vector3 startRotation;
    [SerializeField] private Vector3 gameRotation;

    [Header("SFX")]
    [Space(5)]
    public AudioSource startGameSource;

    private bool hasStartedGame = false;
    private static bool hasSeenIntro = false;

    private void Awake()
    {
        if (!hasSeenIntro)
        {
            if (menuCamera != null)
                startRotation = menuCamera.rotation.eulerAngles;
        }
        else
        {
            if (menuCamera != null)
            {
                menuCamera.rotation = Quaternion.Euler(gameRotation);
            }
        } 
    }

    private void Start()
    {
        if (hasSeenIntro)
        {
            startGamePanel.SetActive(false);

            mainMenuPanel.SetActive(true);

            createGamePanel.SetActive(false );
            findGamePanel.SetActive(false );
            settingsPanel.SetActive(false);

            SetMenuCanvasVisible(true);

            SetSelected(mainMenuFirstSelected);
        }
        else
        {
            ShowStartGamePanel();
        }

        if (InputDeviceDetector.Instance != null)
            UpdateStartText(InputDeviceDetector.Instance.CurrentDevice);
    }

    private IEnumerator FlashStart()
    {
        Color baseColor = startText.color;

        while (!hasStartedGame)
        {
            float t = 0f;

            while (t < startFlashSpeed)
            {
                t += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(1f, 0f, t / startFlashSpeed);
                startText.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                yield return null;
            }

            t = 0f;

            while (t < startFlashSpeed)
            {
                t += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(0f, 1f, t / startFlashSpeed);
                startText.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                yield return null;
            }
        }

        startText.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
    }

    private void OnEnable()
    {
        startInput.action.Enable();
        startInput.action.performed += OnStartPressed;

        backInput.action.Enable();
        backInput.action.performed += OnBackPressed;

        if (InputDeviceDetector.Instance != null)
            InputDeviceDetector.Instance.OnDeviceChanged += OnDeviceChanged;
    }

    private void OnDisable()
    {
        startInput.action.performed -= OnStartPressed;
        startInput.action.Disable();

        backInput.action.performed -= OnBackPressed;
        backInput.action.Disable();

        if (InputDeviceDetector.Instance != null)
            InputDeviceDetector.Instance.OnDeviceChanged -= OnDeviceChanged;
    }

    private void OnDeviceChanged(InputDeviceType type)
    {
        UpdateStartText(type);
    }

    private void UpdateStartText(InputDeviceType type)
    {
        switch (type)
        {
            case InputDeviceType.KeyboardMouse:
                startText.text = "Press SPACE to Start";
                break;

            case InputDeviceType.Xbox:
                startText.text = "Press A to Start";
                break;

            case InputDeviceType.PlayStation:
                startText.text = "Press X to Start";
                break;
        }
    }

    public void ResetMenuState()
    {
        mainMenuPanel.SetActive(true);

        createGamePanel.SetActive(false);
        findGamePanel.SetActive(false);
        settingsPanel.SetActive(false);

        SetMenuCanvasVisible(true);

        SetSelected(mainMenuFirstSelected);
    }

    public void ShowStartGamePanel()
    {
        hasStartedGame = false;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashStart());

        startGamePanel.SetActive(true);
        mainMenuPanel.SetActive(false);
        createGamePanel.SetActive(false );
        findGamePanel.SetActive(false);
        settingsPanel.SetActive(false);

        SetStartCanvasVisible(true);
        SetMenuCanvasVisible(false);

        ResetCamera();
    }

    public void StartGame()
    {
        hasSeenIntro = true;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        startText.color = new Color(startText.color.r, startText.color.g, startText.color.b, 1f);

        StartCoroutine(StartGameTransition());
    }

    private void OnStartPressed(InputAction.CallbackContext context)
    {
        if (!startGamePanel.activeSelf) return;
        if (hasStartedGame) return;

        hasStartedGame = true;
        StartGame();
    }

    private IEnumerator StartGameTransition()
    {
        if (startGameSource != null)
            startGameSource.Play();

        yield return StartCoroutine(FadeStartCanvas(0f));
        yield return StartCoroutine(RotateCamera(gameRotation));

        startGamePanel.SetActive(false);

        SetMenuCanvasVisible(true);

        mainMenuPanel.SetActive(true);
        createGamePanel.SetActive(false);
        findGamePanel.SetActive(false);
        settingsPanel.SetActive(false);

        SetSelected(mainMenuFirstSelected);
    }

    private IEnumerator FadeStartCanvas(float targetAlpha)
    {
        if (startCanvasGroup == null) yield break;

        float startAlpha = startCanvasGroup.alpha;
        float t = 0f;

        startCanvasGroup.blocksRaycasts = true;

        while (t < transitionDuration)
        {
            t += Time.unscaledDeltaTime;

            float normalized = t / transitionDuration;

            startCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, normalized);

            yield return null;
        }

        startCanvasGroup.alpha = targetAlpha;
        startCanvasGroup.blocksRaycasts = targetAlpha > 0f;
    }

    private IEnumerator RotateCamera(Vector3 targetEuler)
    {
        if (menuCamera == null)
            yield break;

        Quaternion startRot = menuCamera.rotation;
        Quaternion targetRot = Quaternion.Euler(targetEuler);

        float t = 0f;

        while (t < transitionDuration)
        {
            t += Time.unscaledDeltaTime;

            float normalized = t / transitionDuration;

            menuCamera.rotation = Quaternion.Lerp(startRot, targetRot, normalized);

            yield return null;
        }

        menuCamera.rotation = targetRot;
    }

    private void ResetCamera()
    {
        if (menuCamera != null)
            menuCamera.rotation = Quaternion.Euler(startRotation);
    }

    private void OnBackPressed(InputAction.CallbackContext context)
    {
        Back();
    }

    public void Back()
    {
        if (settingsPanel.activeSelf)
        {
            CloseSettings();
            return;
        }

        if (createGamePanel.activeSelf)
        {
            CloseCreateGamePanel();
            return;
        }

        if (roomCodePanel.activeSelf)
        {
            CloseRoomCodePanel();
            return;
        }

        if (findGamePanel.activeSelf)
        {
            CloseFindGamePanel();
            return;
        }
    }

    public void OpenCreateGame()
    {
        mainMenuPanel.SetActive(false);
        createGamePanel.SetActive(true);

        SetMenuCanvasVisible(true);
        SetSelected(networkFirstSelected);
    }
    public void OpenFindGame()
    {
        mainMenuPanel.SetActive(false);
        findGamePanel.SetActive(true);

        SetMenuCanvasVisible(true);
        SetSelected(networkFirstSelected);
    }

    public void CloseRoomCodePanel()
    {
        roomCodePanel.SetActive(false);
    }
    public void OpenRoomCodePanel()
    {
        roomCodePanel.SetActive(true);
    }

    public void OpenSettings()
    {
        RememberCurrentSelection();

        settingsPanel.SetActive(true);

        SetMenuCanvasVisible(false);
        SetSelected(settingsFirstSelected);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);

        mainMenuPanel.SetActive(true);

        SetMenuCanvasVisible(true);
        RestorePreviousSelection();
    }

    public void CloseFindGamePanel()
    {
        findGamePanel.SetActive(false);

        mainMenuPanel.SetActive(true);

        SetMenuCanvasVisible(true);
        RestorePreviousSelection();
    }
    public void CloseCreateGamePanel()
    {
        createGamePanel.SetActive(false);

        mainMenuPanel.SetActive(true);

        SetMenuCanvasVisible(true);
        RestorePreviousSelection();
    }

    public void ToggleSettings()
    {
        if (settingsPanel.activeSelf)
            CloseSettings();
        else
            OpenSettings();
    }

    private void RememberCurrentSelection()
    {
        if (EventSystem.current == null) return;

        GameObject current = EventSystem.current.currentSelectedGameObject;

        if (current != null)
            lastSelectedBeforeSubMenu = current;
        else
            lastSelectedBeforeSubMenu = mainMenuFirstSelected;
    }

    private void RestorePreviousSelection()
    {
        if (lastSelectedBeforeSubMenu != null)
            SetSelected(lastSelectedBeforeSubMenu);
        else
            SetSelected(mainMenuFirstSelected);
    }

    private void SetSelected(GameObject obj)
    {
        if (obj == null || EventSystem.current == null) return;

        if (selectCoroutine != null)
            StopCoroutine(selectCoroutine);

        selectCoroutine = StartCoroutine(SetSelectedNextFrame(obj));
    }

    private IEnumerator SetSelectedNextFrame(GameObject obj)
    {
        yield return null;

        EventSystem.current.SetSelectedGameObject(null);
        yield return null;

        EventSystem.current.SetSelectedGameObject(obj);
    }

    private void SetMenuCanvasVisible(bool visible)
    {
        if (menuCanvasGroup == null) return;

        menuCanvasGroup.alpha = visible ? 1f : 0f;
        menuCanvasGroup.interactable = visible;
        menuCanvasGroup.blocksRaycasts = visible;
    }

    private void SetStartCanvasVisible(bool visible)
    {
        if (startCanvasGroup == null) return;

        startCanvasGroup.alpha = visible ? 1f : 0f;
        startCanvasGroup.interactable = visible;
        startCanvasGroup.blocksRaycasts = visible;
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}