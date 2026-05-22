using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;

    [Header("Gameplay")]
    [SerializeField] private int gameplaySceneIndex = 2;

    [Header("Lobby")]
    [SerializeField] private int minimumPlayers = 1;

    private readonly List<LobbyPlayer> connectedPlayers =
        new List<LobbyPlayer>();

    public NetworkVariable<Unity.Collections.FixedString32Bytes> currentRoomCode =
        new NetworkVariable<Unity.Collections.FixedString32Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback
                += OnClientConnected;

            NetworkManager.Singleton.OnClientDisconnectCallback
                += OnClientDisconnected;
        }
    }

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback
            -= OnClientConnected;

        NetworkManager.Singleton.OnClientDisconnectCallback
            -= OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        RefreshPlayerList();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        RefreshPlayerList();
    }

    public void RefreshPlayerList()
    {
        connectedPlayers.Clear();

        LobbyPlayer[] players =
            FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None);

        connectedPlayers.AddRange(players);
    }

    public bool AreAllPlayersReady()
    {
        if (connectedPlayers.Count < minimumPlayers)
            return false;

        foreach (LobbyPlayer player in connectedPlayers)
        {
            if (!player.isReady.Value)
            {
                return false;
            }
        }

        return true;
    }

    public void StartGame()
    {
        if (!IsServer) return;

        RefreshPlayerList();

        if (!AreAllPlayersReady())
        {
            Debug.Log("Cannot start: Someone is not ready!");
            return;
        }

        if (LanDiscovery.Instance != null)
        {
            LanDiscovery.Instance.StopBroadcasting();
        }

        string targetSceneName = GetSceneNameFromIndex(gameplaySceneIndex);

        NetworkManager.Singleton.SceneManager.LoadScene(
            targetSceneName,
            LoadSceneMode.Single
        );
    }

    private string GetSceneNameFromIndex(int buildIndex)
    {
        string path = SceneUtility.GetScenePathByBuildIndex(buildIndex);
        int slash = path.LastIndexOf('/');
        int dot = path.LastIndexOf('.');
        return path.Substring(slash + 1, dot - slash - 1);
    }

    public List<LobbyPlayer> GetPlayers()
    {
        return connectedPlayers;
    }


}