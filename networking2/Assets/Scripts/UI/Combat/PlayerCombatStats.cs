using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerCombatStats : NetworkBehaviour
{
    [SerializeField] private TMP_Text killText;

    public NetworkVariable<int> kills = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        kills.OnValueChanged += UpdateKillUI;

        UpdateKillUI(0, kills.Value);
    }

    public override void OnNetworkDespawn()
    {
        kills.OnValueChanged -= UpdateKillUI;
    }

    private void UpdateKillUI(int previous, int current)
    {
        if (!IsOwner) return;

        if (killText != null)
        {
            killText.text = current.ToString();
        }
    }

    public void AddKill()
    {
        if (!IsServer) return;

        kills.Value++;
    }
}