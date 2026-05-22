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

    private bool localReadyState = false;

    private void Start()
    {
        if (readyButton != null)
        {
            readyButton.onClick.AddListener(ToggleLocalPlayerReady);
        }

        if (startMatchButton != null)
        {
            //print("Start button Found");
            Button btn = startMatchButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnStartMatchButtonClicked);
                //print("Listener Added");
            }
            else
            {
                //print("listner not added");
            }
        }

        RenderLobbyList();
    }

    private void OnStartMatchButtonClicked()
    {

        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.StartGame();
        }
        else
        {
            Debug.LogError("[LobbyDisplayUI] Cannot start! LobbyManager.Instance is NULL!");
        }
    }

    public void RenderLobbyList()
    {
        foreach (Transform child in container) Destroy(child.gameObject);

        if (LobbyManager.Instance == null) return;

        LobbyManager.Instance.RefreshPlayerList();

        foreach (LobbyPlayer player in LobbyManager.Instance.GetPlayers())
        {
            GameObject row = Instantiate(playerRowPrefab, container);
            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>();

            if (texts.Length >= 2)
            {
                texts[0].text = player.playerName.Value.ToString();
                texts[1].text = player.isReady.Value ? "<color=green>READY</color>" : "<color=red>NOT READY</color>";
            }
        }

        if (roomCodeText != null)
        {
            roomCodeText.text = $"Room Code: {LobbyManager.Instance.currentRoomCode.Value}";
        }

        if (startMatchButton != null)
        {
            startMatchButton.SetActive(NetworkManager.Singleton.IsServer);
        }
    }

    private void ToggleLocalPlayerReady()
    {
        localReadyState = !localReadyState;

        LobbyPlayer[] allPlayers = FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None);
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
            readyButtonText.text = localReadyState ? "Unready" : "Ready Up";
        }
    }
}
