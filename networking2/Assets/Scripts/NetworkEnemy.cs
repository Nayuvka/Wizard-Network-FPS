using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections;

public class NetworkEnemy : NetworkBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackInterval = 1.5f;
    [SerializeField] private GameObject deathVfx;

    [Header("Knockback Settings")]
    [SerializeField] private float knockbackDuration = 0.5f;
    [SerializeField] private float knockbackIntensity = 2.0f;

    [SerializeField] private Slider healthSlider;
    [SerializeField] private CanvasGroup healthCanvasGroup;

    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NavMeshAgent agent;
    private Transform targetPlayer;
    private float nextAttackTime;
    private bool isKnockedBack = false;

    public override void OnNetworkSpawn()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        if (healthCanvasGroup != null) healthCanvasGroup.alpha = 0;
        if (healthSlider != null) { healthSlider.maxValue = maxHealth; healthSlider.value = maxHealth; }
        if (IsServer) currentHealth.Value = maxHealth;
        currentHealth.OnValueChanged += UpdateHealthUI;
    }

    public override void OnNetworkDespawn() => currentHealth.OnValueChanged -= UpdateHealthUI;

    private void UpdateHealthUI(float prev, float next)
    {
        if (healthSlider != null) healthSlider.value = next;
        if (next < maxHealth && healthCanvasGroup != null) healthCanvasGroup.alpha = 1;
    }

    void Update()
    {
        if (healthCanvasGroup != null && healthCanvasGroup.alpha > 0 && Camera.main != null)
            healthCanvasGroup.transform.LookAt(healthCanvasGroup.transform.position + Camera.main.transform.forward);

        if (!IsServer) return;

        if (isKnockedBack)
        {
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            return;
        }

        FindNearestPlayer();

        if (targetPlayer != null)
        {
            float distance = Vector3.Distance(transform.position, targetPlayer.position);
            if (distance <= attackRange)
            {
                if (agent.isActiveAndEnabled && agent.isOnNavMesh)
                    agent.isStopped = true;
                DoDamage();
            }
            else
            {
                if (agent.isActiveAndEnabled && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.SetDestination(targetPlayer.position);
                }
            }
        }
        else
        {
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
                agent.isStopped = true;
        }
    }

    public void TakeDamage(float damage, Vector3 sourcePosition = default)
    {
        if (!IsServer) return;
        currentHealth.Value -= damage;

        if (currentHealth.Value > 0)
        {
            StopAllCoroutines();
            StartCoroutine(KnockbackRoutine(sourcePosition));
        }

        if (currentHealth.Value <= 0) Die();
    }

    private IEnumerator KnockbackRoutine(Vector3 sourcePosition)
    {
        isKnockedBack = true;

        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }

        if (sourcePosition != default)
        {
            Vector3 direction = (transform.position - sourcePosition).normalized;
            direction.y = 0;
            Vector3 targetPos = transform.position + (direction * knockbackIntensity);

            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                agent.enabled = false;
                transform.position = hit.position;
                SyncPositionClientRpc(hit.position);
                agent.enabled = true;
            }
        }

        yield return new WaitForSeconds(knockbackDuration);

        isKnockedBack = false;

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            agent.isStopped = false;
    }

    [ClientRpc]
    private void SyncPositionClientRpc(Vector3 position)
    {
        if (IsServer) return;
        transform.position = position;
    }

    private void DoDamage()
    {
        if (Time.time >= nextAttackTime && targetPlayer != null)
        {
            PlayerHealth pHealth = targetPlayer.GetComponent<PlayerHealth>();
            if (pHealth != null && pHealth.currentHealth.Value > 0) pHealth.TakeDamage(attackDamage);
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
                if (pHealth != null && pHealth.currentHealth.Value > 0)
                {
                    float dist = Vector3.Distance(transform.position, client.PlayerObject.transform.position);
                    if (dist < closestDistance) { closestDistance = dist; nearest = client.PlayerObject.transform; }
                }
            }
        }
        targetPlayer = nearest;
    }

    private void Die()
    {
        if (SpawnManager.Instance != null) SpawnManager.Instance.EnemyDeath(GetComponent<NetworkObject>());
        PlayDeathVfxClientRpc(transform.position);
        GetComponent<NetworkObject>().Despawn();
    }

    [ClientRpc] private void PlayDeathVfxClientRpc(Vector3 pos) { if (deathVfx != null) Instantiate(deathVfx, pos, Quaternion.identity); }
}