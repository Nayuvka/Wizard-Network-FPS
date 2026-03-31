using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthScript : NetworkBehaviour
{
    [Header("Health Settings")]
    [Space(5)]
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(100f);
    public float maxHealth = 100f;
    [SerializeField] RespawnScript respawn;

    [Header("Health UI")]
    [Space(5)]

    public Image healthBarFill;
    public Color fullhealthColour = Color.green;
    public Color lowhealthColour = Color.red;

    public float lerpSpeed = 3f;

    public override void OnNetworkSpawn()
    {
        respawn = GetComponent<RespawnScript>();
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }
        
    }

    private void Update()
    {
        UpdateHealthUI();
    }



    public void HealthBarFill()
    {
        
    }

    public void TakeDamage(float damage)
    {
        if (!IsServer) return;

        currentHealth.Value -= damage;

        if(currentHealth.Value <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        respawn.RespawnPlayer();
        currentHealth.Value = maxHealth;
    }

    private void UpdateHealthUI()
    {
        if (healthBarFill == null) return;

        float targetFill = currentHealth.Value / maxHealth;
        healthBarFill.fillAmount = Mathf.Lerp(healthBarFill.fillAmount, targetFill, lerpSpeed * Time.deltaTime);
        healthBarFill.color = Color.Lerp(lowhealthColour, fullhealthColour, targetFill);


    }
}
