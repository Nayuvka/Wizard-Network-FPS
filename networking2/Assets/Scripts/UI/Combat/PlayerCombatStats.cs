using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCombatStats : NetworkBehaviour
{
    [SerializeField] private TMP_Text killText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text playerCountText;

    [Header("Potion UI")]
    [SerializeField] private Image[] potionIcons;
    [SerializeField] private Sprite[] potionSprites;

    public NetworkVariable<int> kills = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> playerCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> hasVitalityVial = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> hasSwiftSyrup = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> hasFrenzyFlask = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> hasPhoenixPhial = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkList<int> purchasedPotionIndices;

    public event System.Action<PotionType> OnPotionPurchased;

    private void Awake()
    {
        purchasedPotionIndices = new NetworkList<int>();
    }

    public override void OnNetworkSpawn()
    {
        kills.OnValueChanged += UpdateKillUI;
        score.OnValueChanged += UpdateScoreUI;
        playerCount.OnValueChanged += UpdatePlayerCountUI;
        purchasedPotionIndices.OnListChanged += OnPurchasedListChanged;

        UpdateKillUI(0, kills.Value);
        UpdateScoreUI(0, score.Value);
        UpdatePlayerCountUI(0, playerCount.Value);

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
            UpdateServerPlayerCount();
        }
        else
        {
            RefreshUIFromList();
        }
    }

    public override void OnNetworkDespawn()
    {
        kills.OnValueChanged -= UpdateKillUI;
        score.OnValueChanged -= UpdateScoreUI;
        playerCount.OnValueChanged -= UpdatePlayerCountUI;
        purchasedPotionIndices.OnListChanged -= OnPurchasedListChanged;

        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    private void OnPurchasedListChanged(NetworkListEvent<int> changeEvent)
    {
        RefreshUIFromList();
    }

    private void RefreshUIFromList()
    {
        for (int i = 0; i < potionIcons.Length; i++)
        {
            if (i < purchasedPotionIndices.Count)
            {
                int potionIndex = purchasedPotionIndices[i];
                potionIcons[i].sprite = potionSprites[potionIndex];
                potionIcons[i].enabled = true;
            }
            else
            {
                potionIcons[i].enabled = false;
            }
        }
    }

    private void HandleClientConnected(ulong clientId) => UpdateServerPlayerCount();
    private void HandleClientDisconnected(ulong clientId) => UpdateServerPlayerCount();

    private void UpdateServerPlayerCount()
    {
        if (!IsServer) return;
        playerCount.Value = NetworkManager.Singleton.ConnectedClientsIds.Count;
    }

    private void UpdatePlayerCountUI(int previous, int current)
    {
        if (playerCountText != null) playerCountText.text = current.ToString();
    }

    private void UpdateKillUI(int previous, int current)
    {
        if (!IsOwner) return;
        if (killText != null) killText.text = current.ToString();
    }

    private void UpdateScoreUI(int previous, int current)
    {
        if (!IsOwner) return;
        if (scoreText != null) scoreText.text = $"Score: {current}";
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

    public bool TryBuyPotion(int cost, PotionType type)
    {
        if (!IsServer) return false;
        if (score.Value < cost) return false;

        int typeInt = (int)type;
        if (purchasedPotionIndices.Contains(typeInt)) return false;

        purchasedPotionIndices.Add(typeInt);

        switch (type)
        {
            case PotionType.VitalityVial:
                hasVitalityVial.Value = true;
                if (TryGetComponent(out PlayerHealth health)) health.ApplyHealthUpgrade(175f);
                break;
            case PotionType.SwiftSyrup:
                hasSwiftSyrup.Value = true;
                if (TryGetComponent(out NetworkPlayerController controller)) controller.ApplySpeedUpgradeClientRpc();
                break;
            case PotionType.FrenzyFlask:
                hasFrenzyFlask.Value = true;
                if (TryGetComponent(out NetworkShoot shoot)) shoot.ApplyFireRateUpgradeClientRpc();
                break;
            case PotionType.PhoenixPhial:
                hasPhoenixPhial.Value = true;
                break;
        }

        score.Value -= cost;
        TriggerPurchaseNotificationClientRpc(type);
        return true;
    }

    public void ConsumePhoenixPhial()
    {
        if (!IsServer) return;
        hasPhoenixPhial.Value = false;
        int phoenixIndex = (int)PotionType.PhoenixPhial;
        if (purchasedPotionIndices.Contains(phoenixIndex))
        {
            purchasedPotionIndices.Remove(phoenixIndex);
        }
    }

    [ClientRpc]
    private void TriggerPurchaseNotificationClientRpc(PotionType type)
    {
        OnPotionPurchased?.Invoke(type);
    }
}