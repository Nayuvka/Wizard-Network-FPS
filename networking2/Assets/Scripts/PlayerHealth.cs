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

        if (IsOwner)
        {
            SetupVignette();
        }

        UpdateHealthUI(0, currentHealth.Value);
        currentHealth.OnValueChanged += UpdateHealthUI;
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= UpdateHealthUI;
    }

    private void Update()
    {
        // Re-attempt setup if it failed initially (common in solo/start-up)
        if (IsOwner && vignette == null)
        {
            SetupVignette();
        }

        // Apply visual updates every frame for the local player
        if (IsOwner && vignette != null)
        {
            UpdateLowHealthVignette(currentHealth.Value);
        }
    }

    private void SetupVignette()
    {
        if (globalVolume == null)
        {
            globalVolume = FindFirstObjectByType<Volume>();
        }

        if (globalVolume != null)
        {
            // We use the sharedProfile to ensure we are editing what the camera sees
            if (globalVolume.profile.TryGet(out vignette))
            {
                vignette.intensity.overrideState = true;
                vignette.color.overrideState = true;
                vignette.color.value = Color.red;
            }
        }
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

    private void UpdateLowHealthVignette(float health)
    {
        // If respawn script says we are dead/respawning, force vignette off
        if (respawnScript != null && respawnScript.isRespawning.Value)
        {
            vignette.intensity.value = 0f;
            return;
        }

        bool isLowHealth = health <= lowHealthThreshold && health > 0;
        float target = isLowHealth ? vignetteIntensity : 0f;

        // Smoothly transition the effect
        vignette.intensity.value = Mathf.MoveTowards(vignette.intensity.value, target, Time.deltaTime * 2f);
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

        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.CheckForLose();
        }
    }

    public void ResetHealth()
    {
        if (!IsServer) return;
        currentHealth.Value = maxHealth;
    }
}