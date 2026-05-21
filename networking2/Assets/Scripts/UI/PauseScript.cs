using System.Collections;
using System.Net;
using System.Net.Sockets;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseScript : MonoBehaviour
{
    [Header("Pause Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Pause Canvas")]
    [SerializeField] private GameObject pauseCanvas;

    [Header("Network Info UI")]
    [SerializeField] private TMP_Text networkInfoText;

    [Header("First Selected Objects")]
    [SerializeField] private GameObject pauseFirstSelected;
    [SerializeField] private GameObject settingsFirstSelected;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public static bool IsGamePaused { get; private set; }

    private static string clientConnectedHostIP = "";

    private GameObject lastSelectedBeforeSubMenu;
    private Coroutine selectCoroutine;

    public void Back()
    {
        if (settingsPanel.activeSelf)
        {
            CloseSettings();
            return;
        }
    }

    public static void SetClientConnectedHostIP(string ip)
    {
        clientConnectedHostIP = ip;
    }

    private void Start()
    {
        ForceResume();
        UpdateNetworkInfoText();
    }

    public void TogglePause()
    {
        if (GameOverManager.IsGameOver) return;

        if (settingsPanel.activeSelf)
        {
            Back();
            return;
        }

        if (IsGamePaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        IsGamePaused = true;

        if (pauseCanvas != null)
            pauseCanvas.SetActive(true);

        if (pausePanel != null)
            pausePanel.SetActive(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        UpdateNetworkInfoText();

        SetSelected(pauseFirstSelected);
    }

    public void ResumeGame()
    {
        if (GameOverManager.IsGameOver) return;

        ForceResume();
    }

    private void ForceResume()
    {
        IsGamePaused = false;

        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        SetSelected(null);
    }

    public void OpenSettings()
    {
        RememberCurrentSelection();

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        UpdateNetworkInfoText();

        SetSelected(settingsFirstSelected);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(true);

        UpdateNetworkInfoText();

        RestorePreviousSelection();
    }


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

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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

    private void RememberCurrentSelection()
    {
        if (EventSystem.current == null) return;

        GameObject currentSelected =
            EventSystem.current.currentSelectedGameObject;

        if (currentSelected != null)
        {
            lastSelectedBeforeSubMenu = currentSelected;
        }
        else
        {
            lastSelectedBeforeSubMenu = pauseFirstSelected;
        }
    }

    private void RestorePreviousSelection()
    {
        if (lastSelectedBeforeSubMenu != null)
        {
            SetSelected(lastSelectedBeforeSubMenu);
        }
        else
        {
            SetSelected(pauseFirstSelected);
        }
    }

    private void UpdateNetworkInfoText()
    {
        if (networkInfoText == null) return;

        if (NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsListening)
        {
            networkInfoText.text = "Network: Offline";
            return;
        }

        if (NetworkManager.Singleton.IsHost ||
            NetworkManager.Singleton.IsServer)
        {
            networkInfoText.text =
                $"Host IP: {GetLocalIPAddress()}";

            return;
        }

        if (NetworkManager.Singleton.IsClient)
        {
            if (!string.IsNullOrWhiteSpace(clientConnectedHostIP))
            {
                networkInfoText.text =
                    $"Connected to Host: {clientConnectedHostIP}";
            }
            else
            {
                networkInfoText.text = "Connected to Host";
            }
        }
    }

    private string GetLocalIPAddress()
    {
        try
        {
            IPHostEntry host =
                Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily ==
                    AddressFamily.InterNetwork)
                {
                    string ipString = ip.ToString();

                    if (!ipString.StartsWith("127."))
                    {
                        return ipString;
                    }
                }
            }
        }
        catch
        {
            return "IP Not Found";
        }

        return "IP Not Found";
    }

    private void SetSelected(GameObject obj)
    {
        if (EventSystem.current == null) return;

        if (selectCoroutine != null)
        {
            StopCoroutine(selectCoroutine);
        }

        selectCoroutine =
            StartCoroutine(SetSelectedNextFrame(obj));
    }

    private IEnumerator SetSelectedNextFrame(GameObject obj)
    {
        yield return null;

        EventSystem.current.SetSelectedGameObject(null);

        yield return null;

        if (obj != null)
        {
            EventSystem.current.SetSelectedGameObject(obj);
        }
    }
}