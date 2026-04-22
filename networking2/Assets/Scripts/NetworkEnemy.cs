using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class NetworkEnemy : NetworkBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackInterval = 1.5f;
    [SerializeField] private GameObject deathVfx;

    [SerializeField] private Slider healthSlider;
    [SerializeField] private CanvasGroup healthCanvasGroup;

    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        100f, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );
    
    private NavMeshAgent agent;
    private Transform targetPlayer;
    private float nextAttackTime;

    public override void OnNetworkSpawn()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;

        if (healthCanvasGroup != null) healthCanvasGroup.alpha = 0;
        
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = maxHealth;
        }

        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        currentHealth.OnValueChanged += UpdateHealthUI;
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= UpdateHealthUI;
    }

    private void UpdateHealthUI(float previousValue, float newValue)
    {
        if (healthSlider != null)
        {
            healthSlider.value = newValue;
        }

        if (newValue < maxHealth && healthCanvasGroup != null)
        {
            healthCanvasGroup.alpha = 1;
        }
    }

    void Update()
    {
        if (healthCanvasGroup != null && healthCanvasGroup.alpha > 0)
        {
            if (Camera.main != null)
            {
                healthCanvasGroup.transform.LookAt(healthCanvasGroup.transform.position + Camera.main.transform.forward);
            }
        }

        if (!IsServer) return;

        FindNearestPlayer();

        if (targetPlayer != null)
        {
            float distance = Vector3.Distance(transform.position, targetPlayer.position);
            
            if (distance <= attackRange)
            {
                agent.isStopped = true;
                DoDamage();
            }
            else
            {
                agent.isStopped = false;
                agent.SetDestination(targetPlayer.position);
            }
        }
        else
        {
            agent.isStopped = true;
        }
    }

    private void DoDamage()
    {
        if (Time.time >= nextAttackTime && targetPlayer != null)
        {
            PlayerHealth pHealth = targetPlayer.GetComponent<PlayerHealth>();
            if (pHealth != null && pHealth.currentHealth.Value > 0)
            {
                pHealth.TakeDamage(attackDamage);
            }
            nextAttackTime = Time.time + attackInterval;
        }
    }

    private void FindNearestPlayer()
    {
        float closestDistance = Mathf.Infinity;
        Transform nearest = null;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                PlayerHealth pHealth = client.PlayerObject.GetComponent<PlayerHealth>();
                RespawnScript pRespawn = client.PlayerObject.GetComponent<RespawnScript>();
                
                // Now checks if health is > 0 AND they aren't currently in the respawn middle-state
                if (pHealth != null && pHealth.currentHealth.Value > 0 && (pRespawn == null || !pRespawn.isRespawning.Value))
                {
                    float distance = Vector3.Distance(transform.position, client.PlayerObject.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        nearest = client.PlayerObject.transform;
                    }
                }
            }
            targetPlayer = nearest;
        }
    }

    public void TakeDamage(float damage)
    {
        if (!IsServer) return;

        currentHealth.Value -= damage;
        if (currentHealth.Value <= 0) Die();
    }

    private void Die()
    {
        if (!IsServer) return;
        PlayDeathVfxClientRpc(transform.position);
        GetComponent<NetworkObject>().Despawn();
    }

    [ClientRpc]
    private void PlayDeathVfxClientRpc(Vector3 pos)
    {
        if (deathVfx != null) Instantiate(deathVfx, pos, Quaternion.identity);
    }
}