using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class PlayerStamina : NetworkBehaviour
{
    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [Tooltip("How many seconds the player can sprint before running out.")]
    [SerializeField] private float sprintDuration = 4f;
    [Tooltip("How many seconds it takes to fully refill from zero.")]
    [SerializeField] private float regenDuration = 3f;
    [Tooltip("Delay in seconds before stamina starts regenerating after stopping.")]
    [SerializeField] private float regenDelay = 0.5f;

    [Header("UI References")]
    [Tooltip("Attach a CanvasGroup to your Stamina UI parent to handle fading.")]
    [SerializeField] private CanvasGroup staminaCanvasGroup;
    [SerializeField] private Image staminaFill;

    [Header("UI Fading")]
    [SerializeField] private float fadeSpeed = 8f;
    [SerializeField] private float hideDelay = 2f;

    public float CurrentStamina { get; private set; }

    private float timeSinceLastSprint;
    private float timeAtMaxStamina;
    private float targetAlpha;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            if (staminaCanvasGroup != null)
            {
                staminaCanvasGroup.alpha = 0;
                staminaCanvasGroup.gameObject.SetActive(false);
            }
            enabled = false;
            return;
        }

        CurrentStamina = maxStamina;
        if (staminaCanvasGroup != null) staminaCanvasGroup.alpha = 0;
    }

    private void Update()
    {
        if (!IsOwner) return;
        UpdateUI();
    }

    public void ProcessStamina(bool isSprinting)
    {
        float drainRate = maxStamina / sprintDuration;
        float regenRate = maxStamina / regenDuration;

        if (isSprinting)
        {
            CurrentStamina -= drainRate * Time.deltaTime;
            timeSinceLastSprint = 0f;
            timeAtMaxStamina = 0f;
            targetAlpha = 1f; 
        }
        else
        {
            timeSinceLastSprint += Time.deltaTime;

            if (timeSinceLastSprint >= regenDelay && CurrentStamina < maxStamina)
            {
                CurrentStamina += regenRate * Time.deltaTime;
                targetAlpha = 1f; 
            }
        }


        CurrentStamina = Mathf.Clamp(CurrentStamina, 0f, maxStamina);


        if (CurrentStamina >= maxStamina && !isSprinting)
        {
            timeAtMaxStamina += Time.deltaTime;
            if (timeAtMaxStamina >= hideDelay)
            {
                targetAlpha = 0f; 
            }
        }
    }

    private void UpdateUI()
    {
        if (staminaFill != null)
        {
            staminaFill.fillAmount = CurrentStamina / maxStamina;
        }

        if (staminaCanvasGroup != null)
        {
            staminaCanvasGroup.alpha = Mathf.MoveTowards(
                staminaCanvasGroup.alpha,
                targetAlpha,
                fadeSpeed * Time.deltaTime
            );
        }
    }
}