using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class LobbyPlayer : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> playerName =
        new NetworkVariable<FixedString32Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> isReady =
        new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        // 1. Register state change callbacks so UI updates dynamically
        playerName.OnValueChanged += OnProfileChanged;
        isReady.OnValueChanged += OnReadyStateChanged;

        // 2. Register this newly spawned player into the Lobby Manager list
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.RefreshPlayerList();
        }

        // 3. Only the local owner requests their initial setup
        if (IsOwner)
        {
            SubmitPlayerNameRpc($"Player {OwnerClientId}");
        }
    }

    public override void OnNetworkDespawn()
    {
        // Clean up events to prevent memory leaks
        playerName.OnValueChanged -= OnProfileChanged;
        isReady.OnValueChanged -= OnReadyStateChanged;

        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.RefreshPlayerList();
        }
    }

    [Rpc(SendTo.Server)]
    private void SubmitPlayerNameRpc(string newName)
    {
        playerName.Value = newName;
    }

    [Rpc(SendTo.Server)]
    public void SetReadyRpc(bool ready)
    {
        isReady.Value = ready;
    }

    // Callbacks triggered on ALL clients when any player's data alters
    private void OnProfileChanged(FixedString32Bytes oldVal, FixedString32Bytes newVal)
    {
        UpdateLobbyVisuals();
    }

    private void OnReadyStateChanged(bool oldVal, bool newVal)
    {
        UpdateLobbyVisuals();
    }

    private void UpdateLobbyVisuals()
    {
        // Tell your UI manager to refresh the visual player cards
        // (We will create this UI handling script next)
        LobbyDisplayUI display = FindFirstObjectByType<LobbyDisplayUI>();
        if (display != null)
        {
            display.RenderLobbyList();
        }
    }
}
