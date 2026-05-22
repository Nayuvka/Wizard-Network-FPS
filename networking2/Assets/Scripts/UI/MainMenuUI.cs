using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class MainMenuUI : MonoBehaviour
{
    [Header("References")]
    [Space(5)]
    private PlayerControls playerControls;

    [Header("Menu Panels")]
    [SerializeField] private GameObject startGamePanel;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject networkPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject howToPlayPanel;

    [Header("Canvas Groups")]
    [Space(10)]
    [SerializeField] CanvasGroup fadeCanvasGroup;
    [SerializeField] CanvasGroup menuCanvasGroup;


    [Header("Menu Settings")]
    [Space(5)]
    [SerializeField] private float fadeDuration;

    [Header("Optional Menu Elements")]
    [Space(5)]
    [SerializeField] private bool hasMenuElements;
    [SerializeField] private GameObject[] menuUIElements;

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

    [Header("SFX")]
    [Space(5)]
    public AudioSource startGameSource;

    private bool hasStartedGame = false;


    private void Start()
    {
        ShowStartGamePanel();
    }

    private void OnEnable()
    {
        startInput.action.Enable();
        startInput.action.performed += OnStartPressed;

        backInput.action.Enable();
        startInput.action.performed += OnBackPressed;
    }

    private void OnDisable()
    {
        startInput.action.performed -= OnStartPressed;
        startInput.action.Disable();

        backInput.action.performed -= OnBackPressed;
        startInput.action.Disable();
    }

    public void ShowStartGamePanel()
    {
        startGamePanel.SetActive(true);
        mainMenuPanel.SetActive(false);
        networkPanel.SetActive(false);
        settingsPanel.SetActive(false);
        howToPlayPanel.SetActive(false);

        SetMenuCanvasVisible(false);

        if (fadeCanvasGroup != null)
            fadeCanvasGroup.alpha = 0;

        
    }

    public void StartGame()
    {
        StartCoroutine(StartGameTransition());
    }

    private void OnStartPressed(InputAction.CallbackContext context)
    {
        if (!startGamePanel.activeSelf) return;

        if (hasStartedGame) return;

        hasStartedGame = true;
        StartGame();
    }

    private void OnBackPressed(InputAction.CallbackContext context)
    {
        Back();
    }

    private IEnumerator StartGameTransition()
    {
        if (startGameSource != null)
        {
            startGameSource.Play();
        }

        yield return Fade(1);

        startGamePanel.SetActive(false);

        mainMenuPanel.SetActive(true);
        SetMenuCanvasVisible(true);

        settingsPanel.SetActive(false);
        SetSelected(mainMenuFirstSelected);

        yield return Fade(0);
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeCanvasGroup == null) yield break;

        float startAlpha = fadeCanvasGroup.alpha;
        float timer = 0f;

        fadeCanvasGroup.blocksRaycasts = true;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
        fadeCanvasGroup.blocksRaycasts = targetAlpha > 0;
    }

    public void OpenNetworkMenu()
    {
        mainMenuPanel.SetActive(false);
        networkPanel.SetActive(true);
        settingsPanel.SetActive(false);
        howToPlayPanel.SetActive(false);

        SetMenuCanvasVisible(true);

        SetSelected(networkFirstSelected);
    }

    public void OpenSettings()
    {
        RememberCurrentSelection();

        settingsPanel.SetActive(true);
        howToPlayPanel.SetActive(false);

        SetMenuCanvasVisible(false);

        SetSelected(settingsFirstSelected);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);

        mainMenuPanel.SetActive(true);
        networkPanel.SetActive(false);
        howToPlayPanel.SetActive(false);

        SetMenuCanvasVisible(true);

        RestorePreviousSelection();
    }

    public void ToggleSettings()
    {
        if (settingsPanel.activeSelf)
        {
            CloseSettings();
        }
        else
        {
            OpenSettings();
        }
    }

    public void OpenHowToPlay()
    {
        RememberCurrentSelection();

        howToPlayPanel.SetActive(true);
        mainMenuPanel.SetActive(false);
        networkPanel.SetActive(false);
        settingsPanel.SetActive(false);

        SetMenuCanvasVisible(false);
    }

    public void CloseHowToPlay()
    {
        howToPlayPanel.SetActive(false);

        mainMenuPanel.SetActive(true);
        networkPanel.SetActive(false);
        settingsPanel.SetActive(false);

        SetMenuCanvasVisible(true);

        RestorePreviousSelection();
    }

    public void ToggleHowToPlay()
    {
        if (howToPlayPanel.activeSelf)
        {
            CloseHowToPlay();
        }
        else
        {
            OpenHowToPlay();
        }
    }

    public void BackFromNetworkMenu()
    {
        mainMenuPanel.SetActive(true);
        networkPanel.SetActive(false);
        settingsPanel.SetActive(false);
        howToPlayPanel.SetActive(false);

        SetMenuCanvasVisible(true);

        SetSelected(mainMenuFirstSelected);
    }

    public void Back()
    {
        if (settingsPanel.activeSelf)
        {
            CloseSettings();
            return;
        }

        if (networkPanel.activeSelf)
        {
            BackFromNetworkMenu();
            return;
        }

        if (howToPlayPanel.activeSelf)
        {
            CloseHowToPlay();
            return;
        }
    }

    private void RememberCurrentSelection()
    {
        if (EventSystem.current == null) return;

        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        if (currentSelected != null)
        {
            lastSelectedBeforeSubMenu = currentSelected;
        }
        else
        {
            if (networkPanel.activeSelf)
            {
                lastSelectedBeforeSubMenu = networkFirstSelected;
            }
            else
            {
                lastSelectedBeforeSubMenu = mainMenuFirstSelected;
            }
        }
    }

    private void RestorePreviousSelection()
    {
        if (lastSelectedBeforeSubMenu != null)
        {
            SetSelected(lastSelectedBeforeSubMenu);
            return;
        }

        if (networkPanel.activeSelf)
        {
            SetSelected(networkFirstSelected);
        }
        else
        {
            SetSelected(mainMenuFirstSelected);
        }
    }

    private void SetMenuCanvasVisible(bool visible)
    {
        if (menuCanvasGroup == null) return;

        menuCanvasGroup.alpha = visible ? 1f : 0f;
        menuCanvasGroup.interactable = visible;
        menuCanvasGroup.blocksRaycasts = visible;
    }



    private void SetSelected(GameObject obj)
    {
        if (obj == null || EventSystem.current == null) return;

        if (selectCoroutine != null)
        {
            StopCoroutine(selectCoroutine);
        }

        selectCoroutine = StartCoroutine(SetSelectedNextFrame(obj));
    }

    private IEnumerator SetSelectedNextFrame(GameObject obj)
    {
        yield return null;

        EventSystem.current.SetSelectedGameObject(null);

        yield return null;

        EventSystem.current.SetSelectedGameObject(obj);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}