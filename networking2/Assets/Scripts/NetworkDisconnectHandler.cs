using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class NetworkDisconnectHandler : MonoBehaviour
{
    [SerializeField] private GameObject hostLeftPopup;
    [SerializeField] private float returnDelay = 5f;

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        if (hostLeftPopup != null)
        {
            hostLeftPopup.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton == null)
            return;

        // Host disconnected
        if (!NetworkManager.Singleton.IsHost &&
            clientId == NetworkManager.ServerClientId)
        {
            StartCoroutine(HostLeftRoutine());
        }
    }

    private IEnumerator HostLeftRoutine()
    {
        if (hostLeftPopup != null)
        {
            hostLeftPopup.SetActive(true);
        }

        yield return new WaitForSeconds(returnDelay);

        SceneManager.LoadScene("MainMenu");
    }
}