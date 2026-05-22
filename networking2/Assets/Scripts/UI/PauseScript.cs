using System.Collections;
using System.Net;
using System.Net.Sockets;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PauseScript : NetworkBehaviour
{
    [Header("Pause Panels")]
    [SerializeField] private GameObject pausePanel;

    [SerializeField] private GameObject settingsPanel;

    [Header("Pause Canvas")]
    [SerializeField] private GameObject pauseCanvas;

    [Header("First Selected Objects")]
    [SerializeField] private GameObject pauseFirstSelected;

    [SerializeField] private GameObject settingsFirstSelected;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "Main";


    private bool isPaused;

    private GameObject lastSelectedBeforeSubMenu;
    public bool isLobby = false;
    public GameObject lobbyFirstSelected;

    private Coroutine selectCoroutine;

    public bool IsPaused()
    {
        return isPaused;
    }

    public override void OnNetworkSpawn()
    {
        ForceResume();
        if (!isLobby)
        {
            SetSelected(null);
        }
    }

    #region Pause Logic

    public void TogglePause()
    {
        if (!IsSpawned) return;

        if (GameOverManager.IsGameOver) return;

        if (settingsPanel != null &&
            settingsPanel.activeSelf)
        {
            Back();
            return;
        }

        if (isPaused)
        {
            ResumeGame();
            SetSelected(lobbyFirstSelected);
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;

        if (pauseCanvas != null)
            pauseCanvas.SetActive(true);

        if (pausePanel != null)
            pausePanel.SetActive(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        Cursor.visible = true;

        Cursor.lockState =
            CursorLockMode.None;


        SetSelected(pauseFirstSelected);
    }

    public void ResumeGame()
    {
        if (GameOverManager.IsGameOver)
            return;

        ForceResume();
    }

    private void ForceResume()
    {
        isPaused = false;

        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        Cursor.visible = false;

        Cursor.lockState =
            CursorLockMode.Locked;

        
    }

    #endregion

    #region Settings

    public void Back()
    {
        if (settingsPanel != null &&
            settingsPanel.activeSelf)
        {
            CloseSettings();
        }
    }

    public void OpenSettings()
    {
        RememberCurrentSelection();

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        SetSelected(settingsFirstSelected);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(true);

        RestorePreviousSelection();
    }

    #endregion

    #region Scene Loading

    public void LoadMainMenu()
    {
        ForceResume();

        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void RestartLevel()
    {
        ForceResume();

        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadSceneByIndex(int index)
    {
        ForceResume();

        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene(index);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    #endregion

    #region Network Info


    #endregion

    #region UI Selection

    private void RememberCurrentSelection()
    {
        if (EventSystem.current == null)
            return;

        GameObject currentSelected =
            EventSystem.current
            .currentSelectedGameObject;

        if (currentSelected != null)
        {
            lastSelectedBeforeSubMenu =
                currentSelected;
        }
        else
        {
            lastSelectedBeforeSubMenu =
                pauseFirstSelected;
        }
    }

    private void RestorePreviousSelection()
    {
        if (lastSelectedBeforeSubMenu != null)
        {
            SetSelected(
                lastSelectedBeforeSubMenu);
        }
        else
        {
            SetSelected(pauseFirstSelected);
        }
    }

    private void SetSelected(GameObject obj)
    {
        if (EventSystem.current == null)
            return;

        if (selectCoroutine != null)
        {
            StopCoroutine(selectCoroutine);
        }

        selectCoroutine =
            StartCoroutine(
                SetSelectedNextFrame(obj));
    }

    private IEnumerator SetSelectedNextFrame(
        GameObject obj)
    {
        yield return null;

        EventSystem.current
            .SetSelectedGameObject(null);

        yield return null;

        if (obj != null)
        {
            EventSystem.current
                .SetSelectedGameObject(obj);
        }
    }

    #endregion
}