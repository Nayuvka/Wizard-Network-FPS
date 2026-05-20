using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class MainMenuUI : MonoBehaviour
{
    [Header("References")]
    [Space(5)]
    private PlayerControls playerControls;

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject networkPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject howToPlayPanel;

    [SerializeField] private CanvasGroup menuCanvasGroup;

    [Header("Optional Menu Elements")]
    [SerializeField] private bool hasMenuElements;
    [SerializeField] private GameObject[] menuUIElements;

    [Header("First Selected Objects")]
    [SerializeField] private GameObject mainMenuFirstSelected;
    [SerializeField] private GameObject networkFirstSelected;
    [SerializeField] private GameObject settingsFirstSelected;
    [SerializeField] private GameObject howToPlayFirstSelected;

    private GameObject lastSelectedBeforeSubMenu;
    private Coroutine selectCoroutine;
    private System.Action<InputAction.CallbackContext> backAction;

    private void Awake()
    {
        playerControls = new PlayerControls();
    }

    private void OnEnable()
    {
        playerControls.UI.Enable();

        backAction = ctx => Back();
        playerControls.UI.Back.performed += backAction;
    }

    private void OnDisable()
    {
        if (playerControls != null)
        {
            playerControls.UI.Back.performed -= backAction;
            playerControls.UI.Disable();
        }
    }

    private void Start()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        networkPanel.SetActive(false);
        settingsPanel.SetActive(false);
        howToPlayPanel.SetActive(false);

        SetMenuCanvasVisible(true);
        SetMenuElementsVisible(true);

        SetSelected(mainMenuFirstSelected);
    }

    public void OpenNetworkMenu()
    {
        mainMenuPanel.SetActive(false);
        networkPanel.SetActive(true);
        settingsPanel.SetActive(false);
        howToPlayPanel.SetActive(false);

        SetMenuCanvasVisible(true);
        SetMenuElementsVisible(false);

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
        SetMenuElementsVisible(true);

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
        SetMenuElementsVisible(false);

        SetSelected(howToPlayFirstSelected);
    }

    public void CloseHowToPlay()
    {
        howToPlayPanel.SetActive(false);

        mainMenuPanel.SetActive(true);
        networkPanel.SetActive(false);
        settingsPanel.SetActive(false);

        SetMenuCanvasVisible(true);
        SetMenuElementsVisible(true);

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
        SetMenuElementsVisible(true);

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

    private void SetMenuElementsVisible(bool visible)
    {
        if (!hasMenuElements || menuUIElements == null) return;

        foreach (GameObject elem in menuUIElements)
        {
            if (elem != null)
            {
                elem.SetActive(visible);
            }
        }
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