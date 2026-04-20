using Unity.Netcode;
using UnityEngine;
using TMPro;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    private TMP_Text healthDisplay;

    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        100f, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            GameObject uiObj = GameObject.FindWithTag("HealthUI");
            if (uiObj != null)
            {
                healthDisplay = uiObj.GetComponent<TMP_Text>();
            }
        }

        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        UpdateHealthUI(0, currentHealth.Value);
        currentHealth.OnValueChanged += UpdateHealthUI;
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= UpdateHealthUI;
    }

    private void UpdateHealthUI(float previousValue, float newValue)
    {
        if (IsOwner && healthDisplay != null)
        {
            healthDisplay.text = $"{Mathf.CeilToInt(newValue)} / {Mathf.CeilToInt(maxHealth)}";
        }
    }

    public void TakeDamage(float damage)
    {
        if (!IsServer) return;

        currentHealth.Value -= damage;

        if (currentHealth.Value <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (IsServer)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }
}