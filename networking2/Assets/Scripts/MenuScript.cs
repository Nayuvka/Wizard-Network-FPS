using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuScript : MonoBehaviour
{
    [System.Serializable]
    private class CachedRoom
    {
        public RoomData roomData;

        public float lastSeenTime;

        public GameObject roomEntryObject;
    }

    [Header("References")]
    [SerializeField] private MainMenuUI menuUI;

    [Header("Room Browser")]
    [SerializeField] private Transform roomListContainer;

    [SerializeField] private GameObject roomEntryPrefab;

    [Header("Create Room")]
    [SerializeField] private TMP_InputField roomNameInput;

    [SerializeField] private Slider maxPlayersSlider;

    [SerializeField] private TMP_Dropdown roomPrivacyDropdown;

    [Header("Private Room Join")]
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private Image roomBorder;

    [SerializeField] private Color invalidColour = Color.red;
    [SerializeField] private Color normalColour = Color.white;

    [SerializeField] private Button roomCodeJoinButton;

    [Header("Networking")]
    [SerializeField] private UnityTransport transport;

    [SerializeField] private NetworkManager networkManager;

    [SerializeField] private ushort defaultPort = 7777;

    [Header("Room Cleanup")]
    [SerializeField] private float roomTimeout = 8f;

    [SerializeField] private float cleanupInterval = 5f;

    private readonly Dictionary<string, CachedRoom>
        discoveredRooms = new();

    private Coroutine cleanupCoroutine;

    private RoomData selectedPrivateRoom;

    private void Start()
    {
        if (LanDiscovery.Instance != null)
        {
            LanDiscovery.Instance.OnRoomFound +=
                HandleRoomFound;
        }


        menuUI.ResetMenuState();

        SetupInputValidation();

        if (roomCodeJoinButton != null)
        {
            roomCodeJoinButton.onClick
                .AddListener(JoinPrivateRoom);
        }

        if (roomCodeInput != null)
        {
            roomCodeInput.onValueChanged
                .AddListener(OnRoomCodeChanged);

            roomCodeInput.onDeselect
                .AddListener(OnRoomCodeDeselected);
        }
    }

    private void OnRoomCodeChanged(string value)
    {
        ResetRoomCodeVisuals();
    }

    private void OnRoomCodeDeselected(string value)
    {
        ResetRoomCodeVisuals();
    }

    private void OnDestroy()
    {
        if (LanDiscovery.Instance != null)
        {
            LanDiscovery.Instance.OnRoomFound -=
                HandleRoomFound;
        }
    }

    #region ROOM BROWSER

    public void OpenRoomBrowser()
    {
        menuUI.OpenFindGame();

        if (LanDiscovery.Instance != null)
        {
            LanDiscovery.Instance.StartSearching();
        }

        StartCleanupLoop();
    }

    public void CloseRoomBrowser()
    {
        menuUI.CloseFindGamePanel();

        if (LanDiscovery.Instance != null)
        {
            LanDiscovery.Instance.StopSearching();
        }

        if (cleanupCoroutine != null)
        {
            StopCoroutine(cleanupCoroutine);
        }
    }

    #region ROOM CODE VISUALS

    private void ResetRoomCodeVisuals()
    {
        if (roomBorder != null)
        {
            roomBorder.color = normalColour;
        }
    }

    private void ShowInvalidRoomCode()
    {
        if (roomBorder != null)
        {
            roomBorder.color = invalidColour;
        }
    }

    #endregion

    public void RefreshRooms()
    {
        RemoveExpiredRooms(true);
    }

    private void StartCleanupLoop()
    {
        if (cleanupCoroutine != null)
        {
            StopCoroutine(cleanupCoroutine);
        }

        cleanupCoroutine =
            StartCoroutine(CleanupLoop());
    }

    private IEnumerator CleanupLoop()
    {
        while (true)
        {
            RemoveExpiredRooms(false);

            yield return new WaitForSeconds(
                cleanupInterval);
        }
    }

    private void RemoveExpiredRooms(
        bool forceCleanup)
    {
        List<string> expiredRooms = new();

        foreach (var pair in discoveredRooms)
        {
            CachedRoom room = pair.Value;

            bool expired =
                Time.time - room.lastSeenTime >
                roomTimeout;

            if (expired || forceCleanup)
            {
                if (forceCleanup &&
                    Time.time - room.lastSeenTime <
                    roomTimeout)
                {
                    continue;
                }

                expiredRooms.Add(pair.Key);
            }
        }

        foreach (string key in expiredRooms)
        {
            if (discoveredRooms[key]
                .roomEntryObject != null)
            {
                Destroy(
                    discoveredRooms[key]
                    .roomEntryObject);
            }

            discoveredRooms.Remove(key);
        }
    }

    #endregion

    #region ROOM DISCOVERY

    private void HandleRoomFound(RoomData room)
    {
        if (discoveredRooms.ContainsKey(room.hostIP))
        {
            CachedRoom cachedRoom =
                discoveredRooms[room.hostIP];

            cachedRoom.roomData = room;

            cachedRoom.lastSeenTime = Time.time;

            UpdateRoomEntryUI(cachedRoom);

            return;
        }

        GameObject entry =
            Instantiate(
                roomEntryPrefab,
                roomListContainer);

        CachedRoom newRoom = new CachedRoom
        {
            roomData = room,
            lastSeenTime = Time.time,
            roomEntryObject = entry
        };

        discoveredRooms.Add(
            room.hostIP,
            newRoom);

        SetupRoomEntry(newRoom);
    }

    private void SetupRoomEntry(
    CachedRoom cachedRoom)
    {
        Debug.Log("SetupRoomEntry called");

        if (cachedRoom == null)
        {
            Debug.LogError("CachedRoom is NULL");
            return;
        }

        if (cachedRoom.roomEntryObject == null)
        {
            Debug.LogError("Room Entry Object is NULL");
            return;
        }

        RoomItemUI roomEntryUI =
            cachedRoom.roomEntryObject
            .GetComponent<RoomItemUI>();

        if (roomEntryUI == null)
        {
            Debug.LogError(
                "RoomItemUI missing on prefab");
            return;
        }

        roomEntryUI.SetRoom(
            cachedRoom.roomData);

        Button button =
            cachedRoom.roomEntryObject
            .GetComponentInChildren<Button>();

        if (button == null)
        {
            Debug.LogError(
                "Button missing on prefab");
            return;
        }

        button.onClick.RemoveAllListeners();

        button.onClick.AddListener(() =>
        {
            OnRoomSelected(
                cachedRoom.roomData);
        });
    }

    private void UpdateRoomEntryUI(
        CachedRoom cachedRoom)
    {
        RoomItemUI roomEntryUI =
            cachedRoom.roomEntryObject
            .GetComponent<RoomItemUI>();

        if (roomEntryUI != null)
        {
            roomEntryUI.SetRoom(
                cachedRoom.roomData);
        }
    }

    #endregion

    #region ROOM SELECTION

    private void OnRoomSelected(
        RoomData room)
    {
        if (room.isPrivate)
        {
            selectedPrivateRoom = room;

            roomCodeInput.text = "";

            menuUI.OpenRoomCodePanel();

            return;
        }

        StartCoroutine(
            JoinRoomRoutine(room.hostIP));
    }

    private void JoinPrivateRoom()
    {
        if (selectedPrivateRoom == null)
            return;

        string enteredCode =
            roomCodeInput.text.Trim();

        if (!IsValidRoomCode(
            enteredCode))
        {
            Debug.Log(
                "Invalid room code.");
            ShowInvalidRoomCode();

            return;
        }

        if (enteredCode !=
            selectedPrivateRoom.roomCode)
        {
            Debug.Log(
                "Incorrect room code.");
            ShowInvalidRoomCode();

            return;
        }

        menuUI.CloseRoomCodePanel();

        StartCoroutine(
            JoinRoomRoutine(
                selectedPrivateRoom.hostIP));
    }

    #endregion

    #region HOSTING

    public void CreateRoom()
    {
        StartCoroutine(
            CreateRoomRoutine());
    }

    private IEnumerator CreateRoomRoutine()
    {
        NetworkSessionManager.Instance
            .ShutdownSession();

        yield return new WaitForSeconds(
            0.25f);

        string roomName =
            GetValidatedRoomName();

        string hostName = "Player";

        if (PlayerProfileManager.Instance != null)
        {
            hostName =
                PlayerProfileManager
                .Instance
                .PlayerName;
        }

        bool isPrivate =
            roomPrivacyDropdown.value == 1;

        int maxPlayers =
            Mathf.RoundToInt(
                maxPlayersSlider.value);

        string roomCode = isPrivate
         ? Random.Range(1000, 9999).ToString()
    :    "";

        transport.SetConnectionData(
            "0.0.0.0",
            defaultPort);

        networkManager.StartHost();

        RoomData roomData =
            new RoomData
            {
                roomName = roomName,
                hostName = hostName,
                roomCode = roomCode,
                hostIP =
                    LanDiscovery.Instance
                    .GetLocalIPAddress(),
                isPrivate = isPrivate,
                currentPlayers = 1,
                maxPlayers = maxPlayers
            };

        LanDiscovery.Instance
            .StartBroadcasting(roomData);

        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance
                .currentRoomCode.Value =
                roomCode;
        }

        string lobbySceneName =
            GetSceneNameFromIndex(1);

        networkManager.SceneManager
            .LoadScene(
                lobbySceneName,
                LoadSceneMode.Single);
    }

    private string GetValidatedRoomName()
    {
        string roomName =
            roomNameInput.text.Trim();

        if (string.IsNullOrWhiteSpace(
            roomName))
        {
            roomName = "LAN Room";
        }

        roomName =
            roomName.Replace("\n", "");

        roomName =
            roomName.Replace("\r", "");

        if (roomName.Length > 20)
        {
            roomName =
                roomName.Substring(0, 20);
        }

        return roomName;
    }

    #endregion

    #region JOINING

    private IEnumerator JoinRoomRoutine(
        string ip)
    {
        NetworkSessionManager.Instance
            .ShutdownSession();

        yield return new WaitForSeconds(
            0.25f);

        if (LanDiscovery.Instance != null)
        {
            LanDiscovery.Instance
                .StopSearching();
        }

        transport.SetConnectionData(
            ip,
            defaultPort);

        networkManager.StartClient();
    }

    private bool IsValidRoomCode(
        string code)
    {
        if (string.IsNullOrWhiteSpace(
            code))
        {
            return false;
        }

        if (code.Length != 4)
        {
            return false;
        }

        return int.TryParse(code, out _);
    }

    #endregion

    #region INPUT SETUP

    private void SetupInputValidation()
    {
        if (roomCodeInput != null)
        {
            roomCodeInput.characterLimit = 4;

            roomCodeInput.contentType =
                TMP_InputField
                .ContentType
                .IntegerNumber;
        }

        if (roomNameInput != null)
        {
            roomNameInput.characterLimit = 20;
        }
    }

    #endregion

    #region UTILITIES

    private string GetSceneNameFromIndex(
        int buildIndex)
    {
        string path =
            SceneUtility
            .GetScenePathByBuildIndex(
                buildIndex);

        int slash =
            path.LastIndexOf('/');

        int dot =
            path.LastIndexOf('.');

        return path.Substring(
            slash + 1,
            dot - slash - 1);
    }

    #endregion
}