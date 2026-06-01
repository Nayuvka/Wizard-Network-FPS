using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerCombatStats : NetworkBehaviour
{
    [SerializeField] private TMP_Text killText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text playerCountText;

    public NetworkVariable<int> kills = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> score = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> hasHealthPerk = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> playerCount = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        kills.OnValueChanged += UpdateKillUI;
        score.OnValueChanged += UpdateScoreUI;
        playerCount.OnValueChanged += UpdatePlayerCountUI; 

        UpdateKillUI(0, kills.Value);
        UpdateScoreUI(0, score.Value);
        UpdatePlayerCountUI(0, playerCount.Value);


        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;

            UpdateServerPlayerCount();
        }
    }

    public override void OnNetworkDespawn()
    {
        kills.OnValueChanged -= UpdateKillUI;
        score.OnValueChanged -= UpdateScoreUI;
        playerCount.OnValueChanged -= UpdatePlayerCountUI;

        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        UpdateServerPlayerCount();
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        UpdateServerPlayerCount();
    }

    private void UpdateServerPlayerCount()
    {
        if (!IsServer) return;

        playerCount.Value = NetworkManager.Singleton.ConnectedClientsIds.Count;
    }

    private void UpdatePlayerCountUI(int previous, int current)
    {

        if (playerCountText != null)
        {
            playerCountText.text = current.ToString();
        }
    }

    private void UpdateKillUI(int previous, int current)
    {
        if (!IsOwner) return;

        if (killText != null)
        {
            killText.text = current.ToString();
        }
    }

    private void UpdateScoreUI(int previous, int current)
    {
        if (!IsOwner) return;

        if (scoreText != null)
        {
            scoreText.text = $"Score: {current}";
        }
    }

    public void AddKill()
    {
        if (!IsServer) return;
        kills.Value++;
    }

    public void AddScore(int points)
    {
        if (!IsServer) return;
        score.Value += points;
    }

    public bool TryBuyHealthPerk(int cost, float targetHealthLevel)
    {
        if (!IsServer) return false;
        if (hasHealthPerk.Value) return false;
        if (score.Value < cost) return false;

        score.Value -= cost;
        hasHealthPerk.Value = true;

        if (TryGetComponent(out PlayerHealth health))
        {
            health.ApplyHealthUpgrade(targetHealthLevel);
        }

        return true;
    }
}
