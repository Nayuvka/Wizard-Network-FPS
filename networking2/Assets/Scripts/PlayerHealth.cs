using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerHealth : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float maxHealth = 100f;

    [Header("UI References")]
    [SerializeField] private GameObject healthUIParent;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthText;

    [Header("Low Health Vignette")]
    [SerializeField] private Volume globalVolume;
    [SerializeField] private float lowHealthThreshold = 60f;
    [SerializeField] private float vignetteIntensity = 0.45f;

    private Vignette vignette;

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

        SetupVignette();

        UpdateHealthUI(0, currentHealth.Value);
        currentHealth.OnValueChanged += UpdateHealthUI;
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= UpdateHealthUI;
    }

    private void SetupVignette()
    {
        if (!IsOwner) return;

        if (globalVolume == null)
        {
            globalVolume = FindFirstObjectByType<Volume>();
        }

        if (globalVolume == null)
        {
            Debug.LogWarning("PlayerHealth: No Global Volume found in the scene.");
            return;
        }

        globalVolume.profile = Instantiate(globalVolume.profile);

        if (!globalVolume.profile.TryGet(out vignette))
        {
            Debug.LogWarning("PlayerHealth: No Vignette override found on the Global Volume profile.");
            return;
        }

        vignette.active = true;

        vignette.color.overrideState = true;
        vignette.intensity.overrideState = true;

        vignette.color.value = Color.red;
        vignette.intensity.value = 0f;
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

        UpdateLowHealthVignette(newValue);
    }

    private void UpdateLowHealthVignette(float health)
    {
        if (!IsOwner || vignette == null) return;

        bool isLowHealth = health <= lowHealthThreshold && health > 0;

        vignette.intensity.value = isLowHealth ? vignetteIntensity : 0f;
    }

    public void TakeDamage(float damage)
    {
        if (!IsServer) return;

        if (respawnScript != null && respawnScript.isRespawning.Value) return;

        currentHealth.Value -= damage;
        currentHealth.Value = Mathf.Clamp(currentHealth.Value, 0, maxHealth);

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
            respawnScript.RespawnPlayer();
        }
        else
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }

    public void ResetHealth()
    {
        if (!IsServer) return;

        currentHealth.Value = maxHealth;
    }
}