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
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image healthBufferFill;
    [SerializeField] private TMP_Text healthText;

    [Header("Buffer Colors")]
    [SerializeField] private Color damageBufferColor = new Color(1f, 0.45f, 0.45f);
    [SerializeField] private Color healBufferColor = new Color(0.45f, 1f, 0.45f);

    [Header("Health Bar Animation")]
    [SerializeField] private float damageBufferSpeed = 2f;
    [SerializeField] private float healBufferSpeed = 8f;
    [SerializeField] private float damageDelay = 0.35f;

    [Header("Big Damage Shake")]
    [SerializeField] private RectTransform healthBarContainer;
    [SerializeField] private float bigDamageThreshold = 25f;
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeStrength = 8f;

    [Header("Low Health Vignette")]
    [SerializeField] private Volume globalVolume;
    [SerializeField] private float lowHealthThreshold = 60f;
    [SerializeField] private float vignetteIntensity = 0.45f;

    private Vignette vignette;
    private RespawnScript respawnScript;

    private float targetFillAmount;
    private float damageTimer;

    private Vector2 originalAnchoredPos;
    private float currentShakeTimer;

    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

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

        if (healthBarContainer != null)
        {
            originalAnchoredPos = healthBarContainer.anchoredPosition;
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
        if (!IsOwner) return;

        if (vignette == null)
        {
            SetupVignette();
        }

        UpdateHealthBarVisual();
        UpdateShake();

        if (vignette != null)
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

        targetFillAmount = newValue / maxHealth;

        if (newValue < previousValue)
        {
            damageTimer = 0f;

            if (healthBarFill != null)
            {
                healthBarFill.fillAmount = targetFillAmount;
            }

            if (healthBufferFill != null)
            {
                healthBufferFill.color = damageBufferColor;
            }

            float damageTaken = previousValue - newValue;

            if (damageTaken >= bigDamageThreshold)
            {
                TriggerShake();
            }
        }
        else if (newValue > previousValue)
        {
            if (healthBufferFill != null)
            {
                healthBufferFill.color = healBufferColor;
                healthBufferFill.fillAmount = targetFillAmount;
            }
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(newValue)}";
        }
    }

    private void UpdateHealthBarVisual()
    {
        if (healthBarFill == null || healthBufferFill == null) return;

        if (healthBufferFill.fillAmount > targetFillAmount)
        {
            damageTimer += Time.deltaTime;

            if (damageTimer >= damageDelay)
            {
                healthBufferFill.fillAmount = Mathf.Lerp(
                    healthBufferFill.fillAmount,
                    targetFillAmount,
                    damageBufferSpeed * Time.deltaTime
                );
            }
        }
        else if (healthBufferFill.fillAmount < targetFillAmount)
        {
            healthBufferFill.fillAmount = Mathf.Lerp(
                healthBufferFill.fillAmount,
                targetFillAmount,
                healBufferSpeed * Time.deltaTime
            );
        }
    }

    private void TriggerShake()
    {
        currentShakeTimer = shakeDuration;
    }

    private void UpdateShake()
    {
        if (healthBarContainer == null) return;

        if (currentShakeTimer > 0)
        {
            currentShakeTimer -= Time.deltaTime;

            Vector2 randomOffset = Random.insideUnitCircle * shakeStrength;
            healthBarContainer.anchoredPosition = originalAnchoredPos + randomOffset;
        }
        else
        {
            healthBarContainer.anchoredPosition = Vector2.Lerp(
                healthBarContainer.anchoredPosition,
                originalAnchoredPos,
                Time.deltaTime * 20f
            );
        }
    }

    private void UpdateLowHealthVignette(float health)
    {
        if (respawnScript != null && respawnScript.isRespawning.Value)
        {
            vignette.intensity.value = 0f;
            return;
        }

        bool isLowHealth = health <= lowHealthThreshold && health > 0;
        float target = isLowHealth ? vignetteIntensity : 0f;

        vignette.intensity.value = Mathf.MoveTowards(
            vignette.intensity.value,
            target,
            Time.deltaTime * 2f
        );
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

    public void Heal(float amount)
    {
        if (!IsServer) return;

        if (respawnScript != null && respawnScript.isRespawning.Value) return;

        currentHealth.Value += amount;
        currentHealth.Value = Mathf.Clamp(currentHealth.Value, 0, maxHealth);
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
        float targetFill = currentHealth.Value / maxHealth;
        healthBarFill.fillAmount = targetFill;
    }

    public void ApplyHealthUpgrade(float newMaxHealth)
    {
        if (!IsServer) return;

        UpdateMaxHealthClientRpc(newMaxHealth);
        currentHealth.Value = newMaxHealth;
    }

    [ClientRpc]
    private void UpdateMaxHealthClientRpc(float newMaxHealth)
    {
        maxHealth = newMaxHealth;
        UpdateHealthUI(currentHealth.Value, currentHealth.Value);
    }
}