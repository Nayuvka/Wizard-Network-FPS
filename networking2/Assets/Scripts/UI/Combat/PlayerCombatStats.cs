using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerCombatStats : NetworkBehaviour
{
    [SerializeField] private TMP_Text killText;
    [SerializeField] private TMP_Text scoreText;

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

    public override void OnNetworkSpawn()
    {
        kills.OnValueChanged += UpdateKillUI;
        score.OnValueChanged += UpdateScoreUI;

        UpdateKillUI(0, kills.Value);
        UpdateScoreUI(0, score.Value);
    }

    public override void OnNetworkDespawn()
    {
        kills.OnValueChanged -= UpdateKillUI;
        score.OnValueChanged -= UpdateScoreUI;
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
            scoreText.text = current.ToString();
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
}