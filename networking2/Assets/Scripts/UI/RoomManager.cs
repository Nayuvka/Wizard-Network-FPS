using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class RoomManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text roomStatusText;
    [SerializeField] private GameObject startGameButton;

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "SiyaTest";

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientChanged;
        }

        UpdateRoomUI();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientChanged;
        }
    }

    private void OnClientChanged(ulong clientId)
    {
        UpdateRoomUI();
    }

    private void UpdateRoomUI()
    {
        if (NetworkManager.Singleton == null) return;

        int playerCount = NetworkManager.Singleton.ConnectedClientsList.Count;

        if (roomStatusText != null)
        {
            roomStatusText.text = $"Players in room: {playerCount}";
        }

        if (startGameButton != null)
        {
            startGameButton.SetActive(NetworkManager.Singleton.IsHost);
        }
    }

    public void StartGame()
    {
        if (!NetworkManager.Singleton.IsHost) return;

        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }
}