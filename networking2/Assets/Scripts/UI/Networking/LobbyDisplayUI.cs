using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;

public class LobbyDisplayUI : MonoBehaviour
{
    [Header("UI Layout Container")]
    [SerializeField] private Transform container;
    [SerializeField] private GameObject playerRowPrefab;

    [Header("Bottom Info")]
    [SerializeField] private TMP_Text roomCodeText;

    [Header("Buttons")]
    [SerializeField] private GameObject startMatchButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyButtonText;

    [Header("Lobby UI")]
    [SerializeField] private GameObject lobbyPanel;

    private bool localReadyState = false;

    private NetworkPlayerController localPlayerController;

    private void Start()
    {
        FindLocalPlayerController();

        if (readyButton != null)
        {
            readyButton.onClick.AddListener(ToggleLocalPlayerReady);
        }

        if (startMatchButton != null)
        {
            Button btn = startMatchButton.GetComponent<Button>();

            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnStartMatchButtonClicked);
            }
        }

        RenderLobbyList();
    }

    private void Update()
    {
        if (localPlayerController == null)
        {
            FindLocalPlayerController();
        }
    }

    private void FindLocalPlayerController()
    {
        NetworkPlayerController[] players =
            FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);

        foreach (NetworkPlayerController player in players)
        {
            if (player.IsOwner)
            {
                localPlayerController = player;
                break;
            }
        }
    }

    public void OpenLobbyUI()
    {
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(true);
        }

        if (localPlayerController != null)
        {
            localPlayerController.EnterUIMode();
        }

        RenderLobbyList();
    }

    public void CloseLobbyUI()
    {
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(false);
        }

        if (localPlayerController != null)
        {
            localPlayerController.ExitUIMode();
        }
    }

    public void ToggleLobbyUI()
    {
        if (lobbyPanel == null)
            return;

        bool isActive = lobbyPanel.activeSelf;

        if (isActive)
        {
            CloseLobbyUI();
        }
        else
        {
            OpenLobbyUI();
        }
    }

    private void OnStartMatchButtonClicked()
    {
        if (LobbyManager.Instance != null)
        {
            CloseLobbyUI();

            LobbyManager.Instance.StartGame();
        }
        else
        {
            Debug.LogError("[LobbyDisplayUI] Cannot start! LobbyManager.Instance is NULL!");
        }
    }

    public void RenderLobbyList()
    {
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        if (LobbyManager.Instance == null)
            return;

        LobbyManager.Instance.RefreshPlayerList();

        foreach (LobbyPlayer player in LobbyManager.Instance.GetPlayers())
        {
            GameObject row = Instantiate(playerRowPrefab, container);

            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>();

            if (texts.Length >= 2)
            {
                texts[0].text = player.playerName.Value.ToString();

                texts[1].text =
                    player.isReady.Value
                    ? "<color=green>READY</color>"
                    : "<color=red>NOT READY</color>";
            }
        }

        if (roomCodeText != null)
        {
            roomCodeText.text =
                $"Room Code: {LobbyManager.Instance.currentRoomCode.Value}";
        }

        if (startMatchButton != null)
        {
            startMatchButton.SetActive(NetworkManager.Singleton.IsServer);
        }
    }

    private void ToggleLocalPlayerReady()
    {
        localReadyState = !localReadyState;

        LobbyPlayer[] allPlayers =
            FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None);

        foreach (LobbyPlayer p in allPlayers)
        {
            if (p.IsOwner)
            {
                p.SetReadyRpc(localReadyState);
                break;
            }
        }

        if (readyButtonText != null)
        {
            readyButtonText.text =
                localReadyState
                ? "Unready"
                : "Ready Up";
        }
    }

    private void OnDisable()
    {
        if (localPlayerController != null)
        {
            localPlayerController.ExitUIMode();
        }
    }
}