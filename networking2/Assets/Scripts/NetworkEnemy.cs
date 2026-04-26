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

    [Header("UI")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private CanvasGroup healthCanvasGroup;

    [Header("SFX")]
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private AudioClip spawnClip;

    [Header("VFX")]
    [SerializeField] private GameObject deathVfx;

    [Header("Hit Pop Settings")]
    [SerializeField] private float popScale = 1.2f;
    [SerializeField] private float popDuration = 0.12f;

    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NavMeshAgent agent;
    private Rigidbody rb;
    private Transform targetPlayer;
    private float nextAttackTime;
    private bool isDead;
    private Vector3 originalScale;

    public override void OnNetworkSpawn()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
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

    private void Update()
    {
        if (healthCanvasGroup != null && healthCanvasGroup.alpha > 0)
        {
            if (Camera.main != null)
            {
                healthCanvasGroup.transform.LookAt(
                    healthCanvasGroup.transform.position + Camera.main.transform.forward
                );
            }
        }

        if (!IsServer || isDead) return;

        FindNearestPlayer();

        if (targetPlayer != null)
        {
            float distance = Vector3.Distance(transform.position, targetPlayer.position);

            if (distance <= attackRange)
            {
                if (agent != null) agent.isStopped = true;
                DoDamage();
            }
            else
            {
                if (agent != null)
                {
                    agent.isStopped = false;
                    agent.SetDestination(targetPlayer.position);
                }
            }
        }
        else
        {
            if (agent != null) agent.isStopped = true;
        }
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

                if (pHealth != null &&
                    pHealth.currentHealth.Value > 0 &&
                    (pRespawn == null || !pRespawn.isRespawning.Value))
                {
                    float distance = Vector3.Distance(
                        transform.position,
                        client.PlayerObject.transform.position
                    );

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        nearest = client.PlayerObject.transform;
                    }
                }
            }
        }

        targetPlayer = nearest;
    }

    public void TakeDamage(float damage)
    {
        if (!IsServer || isDead) return;

        currentHealth.Value -= damage;

        HitFeedbackClientRpc();

        if (currentHealth.Value <= 0)
        {
            isDead = true;
            Die();
        }
    }

    public void TakeDamage(float damage, Vector3 hitDirection, float knockBackForce)
    {
        if (!IsServer || isDead) return;

        currentHealth.Value -= damage;

        ApplyKnockback(hitDirection, knockBackForce);

        HitFeedbackClientRpc();

        if (currentHealth.Value <= 0)
        {
            isDead = true;
            Die();
        }
    }

    private void ApplyKnockback(Vector3 direction, float knockBackForce)
    {
        if (!IsServer) return;

        if (rb != null)
        {
            rb.AddForce(direction * knockBackForce, ForceMode.Impulse);
        }
    }

    private void Die()
    {
        if (!IsServer) return;

        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.EnemyDeath(GetComponent<NetworkObject>());
        }

        DeathFeedbackClientRpc();

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

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
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
    private void DeathFeedbackClientRpc()
    {
        HideEnemyVisualsAndColliders();

        if (deathVfx != null)
        {
            Instantiate(deathVfx, transform.position, Quaternion.identity);
        }

        if (deathClip != null)
        {
            AudioSource.PlayClipAtPoint(deathClip, transform.position);
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