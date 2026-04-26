using UnityEngine;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;

public class MenuScript : MonoBehaviour
{
    [Header("UI")]
    [Space(5)]
    [SerializeField] private TMP_InputField iPInput;
    [SerializeField] private TMP_InputField portInput;
    [SerializeField] private TMP_Text statusText;

    [Header("Defaults")]
    [Space(5)]
    [SerializeField] private string defaultIP = "127.0.0.1";
    [SerializeField] private ushort defaultPort = 7777;

    [Header("Networking")]
    [Space(5)]
    [SerializeField] private UnityTransport transport;
    [SerializeField] private NetworkManager networkManager;

    [Header("Scene")]
    [Space(5)]
    [SerializeField] private string gameSceneName = "SiyaTest";

    void Start()
    {
        if (iPInput) iPInput.text = defaultIP;
        if (portInput) portInput.text = defaultPort.ToString();

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
        ushort port = GetPort();
        transport.SetConnectionData("0.0.0.0", port);

        SetStatus($"Starting host on port {port}...");

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
        ushort port = GetPort();

        transport.SetConnectionData(ip, port);

        SetStatus($"Connecting to {ip}:{port}...");

        bool started = networkManager.StartClient();

        if (!started)
        {
            SetStatus("Failed to start client.");
        }
    }

    public void StartServerOnly()
    {
        ushort port = GetPort();
        transport.SetConnectionData("0.0.0.0", port);

        SetStatus($"Starting server on port {port}...");

        bool started = networkManager.StartServer();

        if (!started)
        {
            SetStatus("Failed to start server.");
            return;
        }

        SetStatus("Server started. Waiting for clients...");
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

        ulong localClientId = networkManager.LocalClientId;

        if (clientId == localClientId)
        {
            string reason = networkManager.DisconnectReason;

            if (!string.IsNullOrWhiteSpace(reason))
            {
                SetStatus($"Disconnected: {reason}");
            }
            else
            {
                SetStatus("Disconnected from session.");
            }

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

    private ushort GetPort()
    {
        if (!portInput || !ushort.TryParse(portInput.text, out ushort port))
        {
            return defaultPort;
        }

        return port;
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