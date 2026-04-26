using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections;

public class NetworkEnemy : NetworkBehaviour
{
    [Header("Enemy Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackInterval = 1.5f;

    [Header("VFX")]
    [SerializeField] private GameObject deathVfx;

    [Header("SFX")]
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private AudioClip spawnClip;

    [Header("Knockback Settings")]
    [SerializeField] private float knockbackDuration = 0.5f;
    [SerializeField] private float knockbackIntensity = 2.0f;

    [Header("Hit Pop Settings")]
    [SerializeField] private float popScale = 1.2f;
    [SerializeField] private float popDuration = 0.12f;

    [Header("UI")]
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
    private bool isKnockedBack = false;
    private bool isDead = false;
    private Vector3 originalScale;

    public override void OnNetworkSpawn()
    {
        agent = GetComponent<NavMeshAgent>();
        originalScale = transform.localScale;

        if (agent != null)
        {
            agent.speed = moveSpeed;
        }

        if (healthCanvasGroup != null)
        {
            healthCanvasGroup.alpha = 0;
        }

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

        SpawnSFXClientRpc();
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= UpdateHealthUI;
    }

    private void UpdateHealthUI(float prev, float next)
    {
        if (healthSlider != null)
        {
            healthSlider.value = next;
        }

        if (next < maxHealth && healthCanvasGroup != null)
        {
            healthCanvasGroup.alpha = 1;
        }
    }

    private void Update()
    {
        if (healthCanvasGroup != null && healthCanvasGroup.alpha > 0 && Camera.main != null)
        {
            healthCanvasGroup.transform.LookAt(
                healthCanvasGroup.transform.position + Camera.main.transform.forward
            );
        }

        if (!IsServer || isDead) return;

        if (isKnockedBack)
        {
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
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
                if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                }

                DoDamage();
            }
            else
            {
                if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.SetDestination(targetPlayer.position);
                }
            }
        }
        else
        {
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
        }
    }

    public void TakeDamage(float damage, Vector3 sourcePosition = default)
    {
        if (!IsServer || isDead) return;

        currentHealth.Value -= damage;

        HitFeedbackClientRpc();

        if (currentHealth.Value > 0)
        {
            StopCoroutineSafe();
            StartCoroutine(KnockbackRoutine(sourcePosition));
        }

        if (currentHealth.Value <= 0)
        {
            isDead = true;
            Die();
        }
    }

    private void StopCoroutineSafe()
    {
        StopAllCoroutines();
    }

    private IEnumerator KnockbackRoutine(Vector3 sourcePosition)
    {
        isKnockedBack = true;

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }

        if (sourcePosition != default)
        {
            Vector3 direction = (transform.position - sourcePosition).normalized;
            direction.y = 0;

            Vector3 targetPos = transform.position + direction * knockbackIntensity;

            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                if (agent != null)
                {
                    agent.enabled = false;
                }

                transform.position = hit.position;
                SyncPositionClientRpc(hit.position);

                if (agent != null)
                {
                    agent.enabled = true;
                }
            }
        }

        yield return new WaitForSeconds(knockbackDuration);

        isKnockedBack = false;

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }

    private IEnumerator HitPop()
    {
        Vector3 targetScale = originalScale * popScale;

        float t = 0;

        while (t < popDuration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t / popDuration);
            yield return null;
        }

        t = 0;

        while (t < popDuration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t / popDuration);
            yield return null;
        }

        transform.localScale = originalScale;
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
            RespawnScript respawn = targetPlayer.GetComponent<RespawnScript>();

            if (pHealth != null && respawn != null)
            {
                bool playerIsAlive = pHealth.currentHealth.Value > 0;
                bool playerIsRespawning = respawn.isRespawning.Value;

                if (playerIsAlive && !playerIsRespawning)
                {
                    pHealth.TakeDamage(attackDamage);

                    if (pHealth.currentHealth.Value <= 0 || respawn.isRespawning.Value)
                    {
                        targetPlayer = null;

                        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                        {
                            agent.isStopped = true;
                            agent.ResetPath();
                        }
                    }
                }
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
            if (client.PlayerObject == null) continue;

            PlayerHealth pHealth = client.PlayerObject.GetComponent<PlayerHealth>();
            RespawnScript respawn = client.PlayerObject.GetComponent<RespawnScript>();

            if (pHealth == null || respawn == null) continue;

            bool playerIsAlive = pHealth.currentHealth.Value > 0;
            bool playerIsRespawning = respawn.isRespawning.Value;

            if (!playerIsAlive || playerIsRespawning) continue;

            float dist = Vector3.Distance(
                transform.position,
                client.PlayerObject.transform.position
            );

            if (dist < closestDistance)
            {
                closestDistance = dist;
                nearest = client.PlayerObject.transform;
            }
        }

        targetPlayer = nearest;
    }

    private void Die()
    {
        if (!IsServer) return;

        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.EnemyDeath(GetComponent<NetworkObject>());
        }

        DeathFeedbackClientRpc(transform.position);

        StartCoroutine(DespawnDelay());
    }

    private IEnumerator DespawnDelay()
    {
        yield return new WaitForSeconds(1f);

        NetworkObject networkObject = GetComponent<NetworkObject>();

        if (networkObject != null && networkObject.IsSpawned)
        {
            networkObject.Despawn();
        }
    }

    private void HideEnemyVisualsAndColliders()
    {
        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = false;
        }

        if (healthCanvasGroup != null)
        {
            healthCanvasGroup.alpha = 0;
        }

        if (agent != null)
        {
            agent.enabled = false;
        }
    }

    [ClientRpc]
    private void HitFeedbackClientRpc()
    {
        if (hitClip != null)
        {
            AudioSource.PlayClipAtPoint(hitClip, transform.position);
        }

        GetComponent<EnemyHitFlash>()?.PlayFlash();

        StopAllCoroutines();
        StartCoroutine(HitPop());
    }

    [ClientRpc]
    private void DeathFeedbackClientRpc(Vector3 pos)
    {
        HideEnemyVisualsAndColliders();

        if (deathVfx != null)
        {
            Instantiate(deathVfx, pos, Quaternion.identity);
        }

        if (deathClip != null)
        {
            AudioSource.PlayClipAtPoint(deathClip, pos);
        }
    }

    [ClientRpc]
    private void SpawnSFXClientRpc()
    {
        if (spawnClip != null)
        {
            AudioSource.PlayClipAtPoint(spawnClip, transform.position);
        }
    }
}