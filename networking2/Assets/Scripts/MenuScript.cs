using UnityEngine;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;

public class MenuScript : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField iPInput;
    [SerializeField] private TMP_Text statusText;

    [Header("Defaults")]
    [SerializeField] private string defaultIP = "127.0.0.1";
    [SerializeField] private ushort defaultPort = 7777;

    [Header("Networking")]
    [SerializeField] private UnityTransport transport;
    [SerializeField] private NetworkManager networkManager;

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "SiyaTest";

    private void Start()
    {
        if (iPInput) iPInput.text = defaultIP;

        if (networkManager != null)
        {
            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }

        SetStatus("Idle");
    }

    private void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.OnClientConnectedCallback -= OnClientConnected;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    public void StartHost()
    {
        transport.SetConnectionData("0.0.0.0", defaultPort);

        SetStatus($"Starting host on port {defaultPort}...");

        bool started = networkManager.StartHost();

        if (!started)
        {
            SetStatus("Failed to start host.");
            return;
        }

        SetStatus("Host started. Loading game scene...");
        networkManager.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    public void JoinGame()
    {
        string ip = GetIP();

        transport.SetConnectionData(ip, defaultPort);

        SetStatus($"Connecting to {ip}:{defaultPort}...");

        bool started = networkManager.StartClient();

        if (!started)
        {
            SetStatus("Failed to start client.");
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (networkManager == null) return;

        ulong localClientId = networkManager.LocalClientId;

        if (networkManager.IsHost && clientId == localClientId)
        {
            SetStatus("Host connected successfully.");
            return;
        }

        if (networkManager.IsClient && clientId == localClientId)
        {
            SetStatus("Connected to host successfully.");
            return;
        }

        if (networkManager.IsServer)
        {
            SetStatus($"Client {clientId} connected.");
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (networkManager == null) return;

        if (clientId == networkManager.LocalClientId)
        {
            SetStatus("Disconnected from session.");
            return;
        }

        if (networkManager.IsServer)
        {
            SetStatus($"Client {clientId} disconnected.");
        }
    }

    private string GetIP()
    {
        if (!iPInput || string.IsNullOrWhiteSpace(iPInput.text))
        {
            return defaultIP;
        }

        return iPInput.text.Trim();
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }

        Debug.Log($"[MenuScript] {message}");
    }
}