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

    [Header("Defaults")]
    [Space(5)]

    [SerializeField] private string defaultIP = "127.0.0.1";
    [SerializeField] private ushort defaultPort = 7777;

    [SerializeField] private UnityTransport transport;
    [SerializeField] private NetworkManager networkManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(iPInput) iPInput.text = defaultIP;
        if(portInput) portInput.text = defaultPort.ToString();
    }

    public void StartHost()
    {
        ushort port = GetPort();
        transport.SetConnectionData("0.0.0.0", port);

        networkManager.StartHost();

        networkManager.SceneManager.LoadScene("SiyaTest", LoadSceneMode.Single);
    }

    public void JoinGame()
    {
        string ip = GetIP();
        ushort port = GetPort();

        transport.SetConnectionData(ip, port);
        networkManager.StartClient();
    }

    public void StartServerOnly()
    {
        ushort port = GetPort();
        transport.SetConnectionData("0.0.0.0", port);
        networkManager.StartServer();
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
        if(!portInput || !ushort.TryParse(portInput.text, out ushort port))
        {
            return defaultPort;
        }

        return port;
    }


}
