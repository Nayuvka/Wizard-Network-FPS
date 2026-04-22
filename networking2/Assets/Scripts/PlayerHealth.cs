using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float maxHealth = 100f;

    [Header("UI References")]
    [SerializeField] private GameObject healthUIParent;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthText;

    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        100f, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    private RespawnScript respawnScript;

    public override void OnNetworkSpawn()
    {
        respawnScript = GetComponent<RespawnScript>();

        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        if (healthUIParent != null)
        {
            healthUIParent.SetActive(IsOwner);
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
        if (!IsOwner) return;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = newValue;
        }

        if (healthText != null)
        {
            healthText.text = $"HP: {Mathf.CeilToInt(newValue)} / {Mathf.CeilToInt(maxHealth)}";
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
        if (!IsServer) return;

        if (respawnScript != null)
        {
            currentHealth.Value = maxHealth; 
            respawnScript.RespawnPlayer();
        }
        else
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }
}