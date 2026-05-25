using Unity.Netcode;
using UnityEngine;

public class NetworkSessionManager : MonoBehaviour
{
    public static NetworkSessionManager Instance;

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

    public void ShutdownSession()
    {
        if (LanDiscovery.Instance != null)
        {
            LanDiscovery.Instance.StopBroadcasting();
            LanDiscovery.Instance.StopSearching();
        }

        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.ResetLobby();
        }
    }

    public void Cleanup()
    {
        if(NetworkManager.Singleton != null)
        {
            Destroy(NetworkManager.Singleton.gameObject);
        }
    }
}