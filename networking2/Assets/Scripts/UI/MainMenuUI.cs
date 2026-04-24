using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class MainMenuUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject networkPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject controlsPanel;

    [Header("Optional Menu Elements")]
    [SerializeField] private bool hasMenuElements;
    [SerializeField] private GameObject[] menuUIElements;

    [Header("First Selected Objects")]
    [SerializeField] private GameObject mainMenuFirstSelected;
    [SerializeField] private GameObject networkFirstSelected;
    [SerializeField] private GameObject settingsFirstSelected;
    [SerializeField] private GameObject controlsFirstSelected;

    private void Start()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        networkPanel.SetActive(false);
        settingsPanel.SetActive(false);
        controlsPanel.SetActive(false);

        SetMenuElementsVisible(true);
        SetSelected(mainMenuFirstSelected);
    }

    public void OpenNetworkMenu()
    {
        mainMenuPanel.SetActive(false);
        networkPanel.SetActive(true);
        settingsPanel.SetActive(false);
        controlsPanel.SetActive(false);

        SetMenuElementsVisible(false);
        SetSelected(networkFirstSelected);
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
        controlsPanel.SetActive(false);

        if (mainMenuPanel.activeSelf || networkPanel.activeSelf)
        {
            SetSelected(settingsFirstSelected);
        }
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);

        if (networkPanel.activeSelf)
        {
            SetSelected(networkFirstSelected);
        }
        else
        {
            SetSelected(mainMenuFirstSelected);
        }
    }

    public void ToggleSettings()
    {
        if (settingsPanel.activeSelf)
            CloseSettings();
        else
            OpenSettings();
    }

    public void OpenControls()
    {
        controlsPanel.SetActive(true);
        settingsPanel.SetActive(false);

        if (mainMenuPanel.activeSelf || networkPanel.activeSelf)
        {
            SetSelected(controlsFirstSelected);
        }
    }

    public void CloseControls()
    {
        controlsPanel.SetActive(false);

        if (networkPanel.activeSelf)
        {
            SetSelected(networkFirstSelected);
        }
        else
        {
            SetSelected(mainMenuFirstSelected);
        }
    }

    public void ToggleControls()
    {
        if (controlsPanel.activeSelf)
            CloseControls();
        else
            OpenControls();
    }

    public void BackFromNetworkMenu()
    {
        ShowMainMenu();
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
        StartCoroutine(SetSelectedNextFrame(obj));
    }

    private IEnumerator SetSelectedNextFrame(GameObject obj)
    {
        yield return null;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(obj);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}