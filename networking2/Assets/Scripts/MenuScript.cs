using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;

public class MenuScript : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject roomBrowserPanel;

    [Header("Room List UI")]
    [SerializeField] private Transform roomListContainer;
    [SerializeField] private GameObject roomEntryPrefab;
    [SerializeField] private TMP_InputField roomNameInput;

    [Header("Networking")]
    [SerializeField] private UnityTransport transport;
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private ushort defaultPort = 7777;

    private Dictionary<string, GameObject> activeRoomEntries = new Dictionary<string, GameObject>();

    private void Start()
    {
        LanDiscovery.Instance.OnRoomFound += HandleRoomFound;
        ShowMainMenu();
    }

    private void OnDestroy()
    {
        if (LanDiscovery.Instance != null) LanDiscovery.Instance.OnRoomFound -= HandleRoomFound;
    }

    public void ShowRoomBrowser()
    {
        mainMenuPanel.SetActive(false);
        roomBrowserPanel.SetActive(true);

        foreach (var entry in activeRoomEntries.Values) Destroy(entry);
        activeRoomEntries.Clear();

        LanDiscovery.Instance.StartSearching();
    }

    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        roomBrowserPanel.SetActive(false);
        LanDiscovery.Instance.StopSearching();
    }

    public void CreateRoom()
    {
        string rName = string.IsNullOrEmpty(roomNameInput.text) ? "LAN Match" : roomNameInput.text;
        string rCode = Random.Range(1000, 9999).ToString();

        transport.SetConnectionData("0.0.0.0", defaultPort);
        networkManager.StartHost();

        // Save and sync the room code across the network via LobbyManager
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.currentRoomCode.Value = rCode;
        }

        LanDiscovery.Instance.StartBroadcasting(rName, rCode, defaultPort);

        string lobbySceneName = GetSceneNameFromIndex(1);
        networkManager.SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
    }

    private void HandleRoomFound(RoomData room)
    {
        if (activeRoomEntries.ContainsKey(room.hostIP)) return;

        GameObject entry = Instantiate(roomEntryPrefab, roomListContainer);
        activeRoomEntries.Add(room.hostIP, entry);

        entry.GetComponentInChildren<TMP_Text>().text = $"{room.roomName} [Code: {room.roomCode}] ({room.currentPlayers}/{room.maxPlayers})";

        entry.GetComponentInChildren<UnityEngine.UI.Button>().onClick.AddListener(() => JoinRoom(room.hostIP));
    }

    private void JoinRoom(string ip)
    {
        LanDiscovery.Instance.StopSearching();
        transport.SetConnectionData(ip, defaultPort);
        networkManager.StartClient();
    }

    private string GetSceneNameFromIndex(int buildIndex)
    {
        string path = SceneUtility.GetScenePathByBuildIndex(buildIndex);
        int slash = path.LastIndexOf('/');
        int dot = path.LastIndexOf('.');
        return path.Substring(slash + 1, dot - slash - 1);
    }
}
