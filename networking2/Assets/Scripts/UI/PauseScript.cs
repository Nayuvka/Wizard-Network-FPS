using System.Collections;
using System.Net;
using System.Net.Sockets;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PauseScript : MonoBehaviour
{
    [Header("Pause Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject controlsPanel;

    [Header("Pause Canvas")]
    [SerializeField] private GameObject pauseCanvas;

    [Header("Network Info UI")]
    [SerializeField] private TMP_Text networkInfoText;

    [Header("First Selected Objects")]
    [SerializeField] private GameObject pauseFirstSelected;
    [SerializeField] private GameObject settingsFirstSelected;
    [SerializeField] private GameObject controlsFirstSelected;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public static bool IsGamePaused { get; private set; }

    private static string clientConnectedHostIP = "";

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

        if (pauseCanvas != null) pauseCanvas.SetActive(true);

        if (pausePanel != null) pausePanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        UpdateNetworkInfoText();
        SetSelected(pauseFirstSelected);
    }

    public void ResumeGame()
    {
        ForceResume();
    }

    private void ForceResume()
    {
        IsGamePaused = false;

        if (pauseCanvas != null) pauseCanvas.SetActive(false);

        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        SetSelected(null);
    }

    public void OpenSettings()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (controlsPanel != null) controlsPanel.SetActive(false);

        UpdateNetworkInfoText();
        SetSelected(settingsFirstSelected);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);

        UpdateNetworkInfoText();
        SetSelected(pauseFirstSelected);
    }

    public void OpenControls()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(true);

        UpdateNetworkInfoText();
        SetSelected(controlsFirstSelected);
    }

    public void CloseControls()
    {
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);

        UpdateNetworkInfoText();
        SetSelected(pauseFirstSelected);
    }

    public void LoadMainMenu()
    {
        ForceResume();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void RestartLevel()
    {
        ForceResume();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadSceneByIndex(int index)
    {
        ForceResume();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene(index);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void UpdateNetworkInfoText()
    {
        if (networkInfoText == null) return;

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            networkInfoText.text = "Network: Offline";
            return;
        }

        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            networkInfoText.text = $"Host IP: {GetLocalIPAddress()}";
            return;
        }

        if (NetworkManager.Singleton.IsClient)
        {
            if (!string.IsNullOrWhiteSpace(clientConnectedHostIP))
            {
                networkInfoText.text = $"Connected to Host: {clientConnectedHostIP}";
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
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
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

        StartCoroutine(SetSelectedNextFrame(obj));
    }

    private IEnumerator SetSelectedNextFrame(GameObject obj)
    {
        yield return null;

        EventSystem.current.SetSelectedGameObject(null);

        if (obj != null)
        {
            EventSystem.current.SetSelectedGameObject(obj);
        }
    }
}