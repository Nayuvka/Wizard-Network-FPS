using UnityEngine;

public class LobbyUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LobbyPlayer localPlayer;

    public void ToggleReady(bool ready)
    {
        if (localPlayer == null) return;

        localPlayer.SetReadyRpc(ready);
    }

    public void StartGame()
    {
        if (LobbyManager.Instance == null) return;

        LobbyManager.Instance.StartGame();
    }
}